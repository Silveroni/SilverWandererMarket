# Silver Wanderer Market (SWM) — server integration

This module is a Bannerlord campaign wanderer shop: a shared **slate** (buy now) and one **wanderer auction**. It does not talk to the network by itself. Your coop server owns the socket; SWM owns the market rules.

**Host** is the only machine that mutates stock, gold escrow, bids, and hires.  
**Clients** send a request, then apply the host snapshot. They never generate lots or tick the auction.

NPC fake bidders auto-run in singleplayer only. Coop host/clients keep them off so real players contest the lot. Auction gold is taken on bid and returned on outbid. A win keeps the stake (no second charge at hire). A slate buy takes gold immediately.

SWM auto-detects SP vs coop/MP (`GameNetwork` + Coop/Hex reflection, including Coop Realm). Host still generates the wanderer slate; clients do not. Call `SWMMarketHooks.LockSession(authoritative, allowLocalGeneration)` if a pack must pin flags. `EnableSimulatedAiBidders` follows the detected role unless the session is locked.

All public types are prefixed `SWM`. Logs start with `[SilverWandererMarket]`. Event ids start with `swm.` so they do not collide with other auctions on the same server.

---

## 1. Startup (do this before campaign session launch)

### Host

```csharp
SWMMarketHooks.IsAuthoritative = true;
SWMMarketHooks.AllowLocalGeneration = true;
SWMMarketHooks.EnableSimulatedAiBidders = false;
SWMMarketHooks.AllowBrokerSpawn = true;

SWMMarketHooks.GetLocalPlayerHero = () => HostPlayerHero;
SWMMarketHooks.GetPlayerKey = hero => YourPeerId(hero);
SWMMarketHooks.ResolveHero = key => HeroForPeer(key); // null if that peer is gone
```

Subscribe once, then fan the snapshot out:

```csharp
SWMMarketHooks.Changed += e =>
{
    BroadcastToAll("swm.snapshot", SWMMarketApi.PackAll());
};

SWMAuctionHooks.Changed += e =>
{
    // Optional: smaller packet. PackAll already includes the auction.
    BroadcastToAll("swm.snapshot", SWMMarketApi.PackAll());
};
```

### Every client

```csharp
SWMMarketHooks.IsAuthoritative = false;
SWMMarketHooks.AllowLocalGeneration = false;
SWMMarketHooks.AllowBrokerSpawn = true; // still spawn the tavern NPC locally

SWMMarketHooks.GetLocalPlayerHero = () => ThisClientHero;
SWMMarketHooks.GetPlayerKey = hero => YourPeerId(hero);

// UI clicks must not run local buy/bid — send to host instead.
SWMMarketHooks.TrySendBuyRequest = offerId =>
{
    SendToHost("swm.buy", new { playerKey = SWMMarketHooks.LocalPlayerKey(), offerId });
    return true; // handled
};

SWMMarketHooks.TrySendBidRequest = amount =>
{
    SendToHost("swm.bid", new { playerKey = SWMMarketHooks.LocalPlayerKey(), amount });
    return true;
};
```

If `TrySendBuyRequest` / `TrySendBidRequest` are left null, a client click is rejected (`Only the host may complete a purchase`).

---

## 2. Runtime flow

```
Client UI (Buy / Bid)
    → TrySendBuyRequest / TrySendBidRequest
    → net message to host

Host
    → SWMMarketApi.TryBuy(...)  or  SWMAuctionApi.TryPlaceBid(...)
    → gold + stock mutate on host
    → Changed event
    → PackAll()
    → broadcast swm.snapshot to everyone

Every client (and host UI)
    → SWMMarketApi.UnpackAll(blob)
    → Gauntlet screen refreshes
```

Brokers stay local. Do not replicate tavern NPC spawn. Do not let clients call `RefreshStock`, `Tick`, `StartNewLot`, or `TryBuy` / `TryPlaceBid` except the host.

---

## 3. Host message handlers

Use your own packet types. Suggested ids: `swm.buy`, `swm.bid`, `swm.snapshot`.

```csharp
// Incoming from a client
void OnSwmBuy(string playerKey, string offerId)
{
    Hero buyer = SWMMarketHooks.ResolveHero(playerKey);
    string name = buyer != null && buyer.Name != null ? buyer.Name.ToString() : playerKey;
    string err;
    string hired = SWMMarketApi.TryBuy(playerKey, name, buyer, offerId, out err);
    if (hired == null)
        SendTo(playerKey, "swm.error", err);
    // success: Changed already broadcast PackAll
}

void OnSwmBid(string playerKey, int amount)
{
    Hero bidder = SWMMarketHooks.ResolveHero(playerKey);
    string name = bidder != null && bidder.Name != null ? bidder.Name.ToString() : playerKey;
    string err;
    string ok = SWMAuctionApi.TryPlaceBid(playerKey, name, bidder, amount, out err);
    if (ok == null)
        SendTo(playerKey, "swm.error", err);
}

// Incoming snapshot on clients
void OnSwmSnapshot(string blob)
{
    SWMMarketApi.UnpackAll(blob);
}
```

`PackAll` blob starts with `SWM1`. It holds slate offers, used names, refresh clock, and the live auction (high bid, escrow, log).

When a client first joins, host should send one `swm.snapshot` immediately so their UI is not empty.

---

## 4. Gold

Default path uses vanilla `Hero.Gold` via `GiveGoldAction`. If your server already owns gold:

```csharp
SWMMarketHooks.TryDebitGold = (hero, amount) =>
    ServerTryRemoveGold(hero, amount) ? null : "Not enough gold.";

SWMMarketHooks.CreditGold = (hero, amount) => ServerAddGold(hero, amount);

SWMMarketHooks.CanAfford = (hero, amount) => ServerGetGold(hero) >= amount;
```

Auction:

1. Bid accepted → debit **delta** if this bidder already holds the lot, else debit the full amount.
2. Previous high bidder (if different) is refunded automatically.
3. Win → gold stays taken. Hire does **not** charge again.
4. Outbid / no-sale / failed hire → refund.

If `ResolveHero` returns null on refund, SWM does not invent gold. You get `swm.auction.refund-unresolved` — credit that peer from the server.

Also replicate `Hero.Gold` to other clients the way you already sync purses, or the UI gold label will lag.

---

## 5. Companions

Built-in hire attaches the wanderer to the winner’s clan/party. To spawn it yourselves:

```csharp
SWMMarketHooks.DeliverCompanion = (offer, winner, paid) =>
{
    // gold is already paid
    return HostSpawnWanderer(offer, winner); // display name
    // return "" if handled with no name
    // return null to use the built-in factory
};

SWMMarketHooks.CanReceiveCompanion = hero =>
    PartyHasCompanionRoom(hero) ? null : "Companion limit reached.";
```

---

## 6. What to grep when something breaks

Search rgl log or `Modules/SilverWandererMarket/swm_debug.log` for `[SilverWandererMarket]`.

| Area | Meaning |
|---|---|
| `SWMSession` | Host/client flags at launch |
| `SWMMarket` | Slate buy, snapshot pack/unpack |
| `SWMAuction` | Bid / close / settle |
| `SWMEscrow` | Gold take, refund, unresolved |
| `SWMHire` | Companion create |
| `SWMBroker` | Tavern NPC |

```csharp
SWMMarketHooks.EnableDebugLog = true;
SWMMarketHooks.EnableVerboseLog = true;  // set false once stable
```

---

## 7. API cheat sheet

| Call | Who | Purpose |
|---|---|---|
| `SWMMarketApi.TryBuy(key, name, hero, offerId, out err)` | Host | Slate purchase |
| `SWMAuctionApi.TryPlaceBid(key, name, hero, amount, out err)` | Host | Auction bid |
| `SWMMarketApi.PackAll()` / `UnpackAll(blob)` | Host pack / everyone unpack | Full sync |
| `SWMAuctionApi.Tick()` | Host only | Close/settle clock (already run from SWM’s realtime tick if `IsAuthoritative`) |
| `SWMMarketApi.RequestOpen()` | Local | Open UI (broker already calls this) |

Event ids (market): `swm.stock-refreshed`, `swm.offer-bought`, `swm.offer-removed`, `swm.companion-hired`, `swm.companion-dismissed`, `swm.market-opened`, `swm.market-closed`, `swm.snapshot-applied`, `swm.buy-rejected`.

Event ids (auction): `swm.auction.lot-started`, `swm.auction.bid-placed`, `swm.auction.escrow-taken`, `swm.auction.outbid-refund`, `swm.auction.closed`, `swm.auction.settle-win`, `swm.auction.settle-no-sale`, `swm.auction.refund-unresolved`, `swm.auction.stock-refreshed`.

Files: `src/Market/SWMMarketHooks.cs`, `SWMMarketApi.cs`, `SWMAuctionHooks.cs`, `SWMAuctionApi.cs`, `src/SWMLog.cs`.
