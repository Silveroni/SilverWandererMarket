using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace SilverWandererMarket.Market
{
    /// <summary>Event ids for <see cref="SWMMarketHooks.Changed"/>. Prefixed <c>swm.</c> so they do not collide with other mods.</summary>
    public static class SWMMarketHookKind
    {
        public const string StockRefreshed = "swm.stock-refreshed";
        public const string OfferBought = "swm.offer-bought";
        public const string OfferRemoved = "swm.offer-removed";
        public const string CompanionHired = "swm.companion-hired";
        public const string CompanionDismissed = "swm.companion-dismissed";
        public const string MarketOpened = "swm.market-opened";
        public const string MarketClosed = "swm.market-closed";
        public const string SnapshotApplied = "swm.snapshot-applied";
        public const string BuyRejected = "swm.buy-rejected";
    }

    public sealed class SWMMarketHookEvent
    {
        public string Kind;
        public string PlayerKey;
        public string PlayerName;
        public string OfferId;
        public string CompanionName;
        public int Amount;
        public string Message;
    }

    /// <summary>
    /// Silver Wanderer Market — shared coop surface (slate, auction, hire, UI, brokers).
    /// Set flags here. <see cref="SWMAuctionHooks"/> aliases the same fields.
    /// </summary>
    public static class SWMMarketHooks
    {
        /// <summary>Host / SP only. Clients: false. Gates auction tick, stock refresh, buy, settle.</summary>
        public static bool IsAuthoritative = true;

        /// <summary>Host / SP generates stock and lots. Clients unpack snapshots instead.</summary>
        public static bool AllowLocalGeneration = true;

        /// <summary>NPC auction rivals. Stripped — leave false.</summary>
        public static bool EnableSimulatedAiBidders = false;

        /// <summary>Tavern broker spawn. Leave true so each client can talk to the broker locally.</summary>
        public static bool AllowBrokerSpawn = true;

        /// <summary>Session test gold top-up. Coop: set false.</summary>
        public static bool AllowTestGold = true;

        /// <summary>rgl/console + swm_debug.log. Alias of <see cref="SWMLog.ConsoleEnabled"/> / FileEnabled.</summary>
        public static bool EnableDebugLog
        {
            get { return SWMLog.ConsoleEnabled || SWMLog.FileEnabled; }
            set
            {
                SWMLog.ConsoleEnabled = value;
                SWMLog.FileEnabled = value;
            }
        }

        /// <summary>Pack sizes, intercepts, skipped client ticks.</summary>
        public static bool EnableVerboseLog
        {
            get { return SWMLog.VerboseEnabled; }
            set { SWMLog.VerboseEnabled = value; }
        }

        public static Func<Hero> GetLocalPlayerHero;
        /// <summary>Stable peer id. Default: <c>player</c>.</summary>
        public static Func<Hero, string> GetPlayerKey;
        public static Func<string, Hero> ResolveHero;
        /// <summary>Return error or null. Default: GiveGoldAction.</summary>
        public static Func<Hero, int, string> TryDebitGold;
        public static Action<Hero, int> CreditGold;
        /// <summary>If set, used instead of <c>hero.Gold &gt;= amount</c> for UI afford checks.</summary>
        public static Func<Hero, int, bool> CanAfford;
        public static Func<Hero, string> CanReceiveCompanion;
        /// <summary>
        /// Spawn a hired wanderer. Auction calls this after escrow is captured (already paid).
        /// Slate buy calls this after gold is taken. Return name, "" if handled, null for built-in factory.
        /// </summary>
        public static Func<WandererOffer, Hero, int, string> DeliverCompanion;
        /// <summary>Clans whose SWM companions should be marked met / scanned. Default: local clan only.</summary>
        public static Func<IList<Clan>> GetPlayerClans;
        /// <summary>Return true if you opened a custom UI. False/null → built-in Gauntlet screen.</summary>
        public static Func<bool> TryOpenMarket;
        /// <summary>Client intercept: send buy to host, return true if handled.</summary>
        public static Func<string, bool> TrySendBuyRequest;
        /// <summary>Client intercept: send bid to host, return true if handled.</summary>
        public static Func<int, bool> TrySendBidRequest;

        public static event Action<SWMMarketHookEvent> Changed;

        public static Hero LocalHero()
        {
            if (GetLocalPlayerHero != null)
            {
                Hero hooked = GetLocalPlayerHero();
                if (hooked != null)
                    return hooked;
            }
            return Hero.MainHero;
        }

        public static string LocalPlayerKey()
        {
            Hero hero = LocalHero();
            if (GetPlayerKey != null && hero != null)
            {
                string key = GetPlayerKey(hero);
                if (!string.IsNullOrEmpty(key))
                    return key;
            }
            return AuctionState.PlayerKey;
        }

        public static bool IsLocalPlayer(string playerKey)
        {
            return !string.IsNullOrEmpty(playerKey) && playerKey == LocalPlayerKey();
        }

        public static Hero ResolveHeroOrNull(string playerKey)
        {
            if (string.IsNullOrEmpty(playerKey) || playerKey.StartsWith("ai:", StringComparison.Ordinal))
                return null;
            if (ResolveHero != null)
            {
                Hero hooked = ResolveHero(playerKey);
                if (hooked != null)
                    return hooked;
            }
            if (playerKey == AuctionState.PlayerKey || IsLocalPlayer(playerKey))
                return LocalHero();
            return null;
        }

        public static bool PlayerCanAfford(Hero hero, int amount)
        {
            if (amount <= 0)
                return true;
            if (hero == null)
                return false;
            if (CanAfford != null)
                return CanAfford(hero, amount);
            return hero.Gold >= amount;
        }

        public static string CompanionGate(Hero winner)
        {
            if (CanReceiveCompanion != null)
                return CanReceiveCompanion(winner);

            if (winner == null)
                winner = LocalHero();
            if (winner == null)
                return "No player hero.";
            Clan clan = winner.Clan;
            if (clan == null)
                clan = Clan.PlayerClan;
            if (clan == null)
                return "No clan.";
            if (clan.Companions != null && clan.Companions.Count >= clan.CompanionLimit)
                return "Companion limit reached.";
            MobileParty party = winner.PartyBelongedTo;
            if (party == null && winner == Hero.MainHero)
                party = MobileParty.MainParty;
            if (party == null)
                return "No party to join.";
            return null;
        }

        public static IList<Clan> PlayerClans()
        {
            if (GetPlayerClans != null)
            {
                IList<Clan> hooked = GetPlayerClans();
                if (hooked != null)
                    return hooked;
            }
            List<Clan> list = new List<Clan>();
            Hero local = LocalHero();
            Clan clan = local != null ? local.Clan : Clan.PlayerClan;
            if (clan != null)
                list.Add(clan);
            return list;
        }

        internal static void Raise(string kind, string playerKey, string playerName, string offerId, string companionName, int amount, string message)
        {
            string line = kind + " player=" + (playerKey ?? "") + " name=" + (playerName ?? "")
                + " offer=" + (offerId ?? "") + " companion=" + (companionName ?? "")
                + " amount=" + amount;
            if (!string.IsNullOrEmpty(message))
                line += " msg=" + message;
            if (kind == SWMMarketHookKind.BuyRejected)
                SWMLog.Warn("SWMMarket", line);
            else
                SWMLog.Info("SWMMarket", line);

            Action<SWMMarketHookEvent> handler = Changed;
            if (handler == null)
                return;
            SWMMarketHookEvent e = new SWMMarketHookEvent();
            e.Kind = kind;
            e.PlayerKey = playerKey ?? "";
            e.PlayerName = playerName ?? "";
            e.OfferId = offerId ?? "";
            e.CompanionName = companionName ?? "";
            e.Amount = amount;
            e.Message = message ?? "";
            handler(e);
        }
    }
}
