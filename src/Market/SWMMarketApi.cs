using System.Text;
using SilverWandererMarket.Heroes;
using TaleWorlds.CampaignSystem;

namespace SilverWandererMarket.Market
{
    /// <summary>
    /// Silver Wanderer Market — host entry points for the wanderer slate and full snapshot.
    /// Auction bids: <see cref="SWMAuctionApi"/>. Flags: <see cref="SWMMarketHooks"/>.
    /// </summary>
    public static class SWMMarketApi
    {
        public static string TryBuy(string offerId, out string error)
        {
            Hero hero = SWMMarketHooks.LocalHero();
            string name = hero != null && hero.Name != null ? hero.Name.ToString() : "You";
            return TryBuy(SWMMarketHooks.LocalPlayerKey(), name, hero, offerId, out error);
        }

        /// <summary>Host-side slate purchase for any peer. Clients: <see cref="SWMMarketHooks.TrySendBuyRequest"/>.</summary>
        public static string TryBuy(string buyerKey, string buyerName, Hero buyer, string offerId, out string error)
        {
            error = null;
            if (!SWMMarketHooks.IsAuthoritative)
            {
                error = "Only the host may complete a purchase.";
                SWMLog.Warn("SWMMarket", "TryBuy rejected (not host) key=" + buyerKey + " offer=" + offerId);
                return null;
            }
            if (string.IsNullOrEmpty(offerId))
            {
                error = "That wanderer is gone.";
                return null;
            }
            if (buyer == null)
            {
                error = "No player hero.";
                SWMLog.Warn("SWMMarket", "TryBuy rejected (no hero) key=" + buyerKey + " offer=" + offerId);
                return null;
            }

            MarketState state = MarketState.Ensure();
            WandererOffer offer = FindOffer(offerId);
            if (offer == null)
            {
                error = "That wanderer is gone.";
                SWMLog.Warn("SWMMarket", "TryBuy missing offer=" + offerId + " key=" + buyerKey);
                return null;
            }

            string gate = SWMMarketHooks.CompanionGate(buyer);
            if (!string.IsNullOrEmpty(gate))
            {
                error = gate;
                SWMMarketHooks.Raise(SWMMarketHookKind.BuyRejected, buyerKey, buyerName, offerId, "", 0, gate);
                return null;
            }

            if (!SWMMarketHooks.PlayerCanAfford(buyer, offer.Price))
            {
                error = "Not enough gold.";
                SWMMarketHooks.Raise(SWMMarketHookKind.BuyRejected, buyerKey, buyerName, offerId, "", offer.Price, error);
                return null;
            }

            string debitErr = SWMAuctionEscrow.TryTake(buyer, offer.Price);
            if (debitErr != null)
            {
                error = debitErr;
                SWMMarketHooks.Raise(SWMMarketHookKind.BuyRejected, buyerKey, buyerName, offerId, "", offer.Price, debitErr);
                return null;
            }

            string hired = DeliverPaidOffer(offer, buyer, offer.Price, out error);
            if (hired == null)
            {
                SWMAuctionEscrow.Credit(buyer, buyerKey, offer.Price);
                if (string.IsNullOrEmpty(error))
                    error = "Hire failed.";
                SWMLog.Error("SWMMarket", "TryBuy hire failed, gold returned key=" + buyerKey + " offer=" + offerId + " err=" + error);
                SWMMarketHooks.Raise(SWMMarketHookKind.BuyRejected, buyerKey, buyerName, offerId, "", offer.Price, error);
                return null;
            }

            WandererOffer removed;
            state.TryRemove(offer.Id, out removed);
            state.StatusMessage = hired + " joined the party.";
            SWMLog.Info("SWMMarket", "TryBuy ok key=" + buyerKey + " hired=" + hired + " price=" + offer.Price + " offer=" + offer.Id);
            SWMMarketHooks.Raise(SWMMarketHookKind.OfferBought, buyerKey, buyerName, offer.Id, hired, offer.Price, null);
            SWMMarketHooks.Raise(SWMMarketHookKind.CompanionHired, buyerKey, buyerName, offer.Id, hired, offer.Price, null);
            MarketState.Notify();
            return hired;
        }

        public static WandererOffer FindOffer(string offerId)
        {
            MarketState state = MarketState.Ensure();
            if (state.Offers == null || string.IsNullOrEmpty(offerId))
                return null;
            for (int i = 0; i < state.Offers.Count; i++)
            {
                if (state.Offers[i] != null && state.Offers[i].Id == offerId)
                    return state.Offers[i];
            }
            return null;
        }

        public static bool TryRemoveOffer(string offerId)
        {
            WandererOffer removed;
            bool ok = MarketState.Ensure().TryRemove(offerId, out removed);
            if (ok)
                SWMMarketHooks.Raise(SWMMarketHookKind.OfferRemoved, "", "", offerId, "", 0, null);
            return ok;
        }

        public static void RefreshStock()
        {
            SWMLog.Info("SWMMarket", "RefreshStock requested authoritative=" + SWMMarketHooks.IsAuthoritative);
            MarketState.Ensure().Refresh();
        }

        public static string PackAll()
        {
            MarketState state = MarketState.Ensure();
            StringBuilder sb = new StringBuilder();
            sb.Append("SWM1\n");
            sb.Append(state.RefreshAtUtcTicks).Append('\n');
            sb.Append(Esc(state.StatusMessage ?? "")).Append('\n');
            sb.Append("---OFFERS---\n");
            sb.Append(state.PackOffers()).Append('\n');
            sb.Append("---IDENTITIES---\n");
            sb.Append(state.PackIdentities()).Append('\n');
            sb.Append("---AUCTION---\n");
            sb.Append(state.Auction != null ? state.Auction.Pack() : "");
            string blob = sb.ToString();
            int offerCount = state.Offers != null ? state.Offers.Count : 0;
            SWMLog.Verbose("SWMMarket", "PackAll bytes=" + blob.Length + " offers=" + offerCount
                + " high=" + (state.Auction != null ? state.Auction.HighBid : 0));
            return blob;
        }

        public static void UnpackAll(string blob)
        {
            MarketState state = MarketState.Ensure();
            if (string.IsNullOrEmpty(blob))
            {
                SWMLog.Warn("SWMMarket", "UnpackAll empty blob");
                return;
            }
            string[] lines = blob.Replace("\r\n", "\n").Split('\n');
            if (lines.Length < 3 || lines[0] != "SWM1")
            {
                SWMLog.Error("SWMMarket", "UnpackAll bad header line0=" + (lines.Length > 0 ? lines[0] : ""));
                return;
            }
            long ticks;
            long.TryParse(lines[1], out ticks);
            state.RefreshAtUtcTicks = ticks;
            state.StatusMessage = Unesc(lines[2]);
            string offers = Slice(lines, "---OFFERS---", "---IDENTITIES---");
            string ids = Slice(lines, "---IDENTITIES---", "---AUCTION---");
            string auction = Slice(lines, "---AUCTION---", null);
            state.UnpackOffers(offers);
            state.UnpackIdentities(ids);
            if (state.Auction == null)
                state.Auction = new AuctionState();
            state.Auction.Unpack(auction);
            int offerCount = state.Offers != null ? state.Offers.Count : 0;
            SWMLog.Info("SWMMarket", "UnpackAll ok offers=" + offerCount + " refreshTicks=" + ticks
                + " high=" + state.Auction.HighBid + " highKey=" + (state.Auction.HighBidderKey ?? ""));
            SWMMarketHooks.Raise(SWMMarketHookKind.SnapshotApplied, "", "", "", "", 0, null);
            MarketState.Notify();
        }

        public static string PackOffers()
        {
            return MarketState.Ensure().PackOffers();
        }

        public static void UnpackOffers(string blob)
        {
            MarketState.Ensure().UnpackOffers(blob);
            MarketState.Notify();
        }

        public static string StatusMessage
        {
            get { return MarketState.Ensure().StatusMessage ?? ""; }
            set { MarketState.Ensure().StatusMessage = value ?? ""; }
        }

        public static long RefreshAtUtcTicks
        {
            get { return MarketState.Ensure().RefreshAtUtcTicks; }
            set { MarketState.Ensure().RefreshAtUtcTicks = value; }
        }

        public static bool CanBuy(WandererOffer offer, out string reason)
        {
            reason = "";
            if (offer == null)
            {
                reason = "That wanderer is gone.";
                return false;
            }
            Hero hero = SWMMarketHooks.LocalHero();
            reason = SWMMarketHooks.CompanionGate(hero);
            if (!string.IsNullOrEmpty(reason))
                return false;
            if (!SWMMarketHooks.PlayerCanAfford(hero, offer.Price))
            {
                reason = "Not enough gold.";
                return false;
            }
            return true;
        }

        public static void RequestOpen()
        {
            SWMMarketHooks.ApplyDetectedSession();
            if (SWMMarketHooks.TryOpenMarket != null && SWMMarketHooks.TryOpenMarket())
            {
                SWMLog.Info("SWMMarket", "RequestOpen handled by custom TryOpenMarket");
                SWMMarketHooks.Raise(SWMMarketHookKind.MarketOpened, SWMMarketHooks.LocalPlayerKey(), "", "", "", 0, "custom");
                return;
            }
            SWMLog.Info("SWMMarket", "RequestOpen built-in Gauntlet UI");
            UI.SWMMarketScreen.RequestOpen();
            SWMMarketHooks.Raise(SWMMarketHookKind.MarketOpened, SWMMarketHooks.LocalPlayerKey(), "", "", "", 0, null);
        }

        public static void Close()
        {
            UI.SWMMarketScreen.Close();
        }

        internal static string DeliverPaidOffer(WandererOffer offer, Hero buyer, int paid, out string error)
        {
            error = null;
            if (SWMMarketHooks.DeliverCompanion != null)
            {
                string hooked = SWMMarketHooks.DeliverCompanion(offer, buyer, paid);
                if (hooked != null)
                {
                    SWMLog.Info("SWMHire", "DeliverCompanion hook handled paid=" + paid + " result=" + hooked);
                    return hooked.Length > 0 ? hooked : (offer != null ? offer.DisplayName : "Companion");
                }
            }
            string hired = CompanionFactory.TryHire(offer, buyer, null, null, false, out error);
            if (hired == null)
                SWMLog.Error("SWMHire", "CompanionFactory.TryHire failed err=" + (error ?? "") + " paid=" + paid);
            else
                SWMLog.Info("SWMHire", "CompanionFactory.TryHire ok " + hired + " paid=" + paid);
            return hired;
        }

        private static string Slice(string[] lines, string startMark, string endMark)
        {
            int start = -1;
            int end = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == startMark)
                    start = i + 1;
                else if (endMark != null && lines[i] == endMark && start >= 0)
                {
                    end = i;
                    break;
                }
            }
            if (start < 0)
                return "";
            StringBuilder sb = new StringBuilder();
            for (int i = start; i < end; i++)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.Append(lines[i]);
            }
            return sb.ToString();
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\n", "\\n");
        }

        private static string Unesc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\n", "\n").Replace("\\\\", "\\");
        }
    }
}
