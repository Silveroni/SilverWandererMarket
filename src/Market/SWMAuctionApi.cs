using TaleWorlds.CampaignSystem;

namespace SilverWandererMarket.Market
{
    /// <summary>
    /// Silver Wanderer Market — wanderer-lot auction host API.
    /// Clients: <see cref="SWMMarketHooks.IsAuthoritative"/> = false, apply <see cref="PackAuction"/> / <see cref="PackMarket"/>, do not Tick.
    /// </summary>
    public static class SWMAuctionApi
    {
        public static string TryPlaceBid(int amount, out string error)
        {
            string ok = AuctionService.TryPlayerBid(MarketState.Ensure(), amount, out error);
            if (ok == null)
                SWMLog.Warn("SWMAuction", "TryPlaceBid local failed amount=" + amount + " err=" + (error ?? ""));
            return ok;
        }

        /// <summary>Host-side bid for any peer. Skips local UI cooldown.</summary>
        public static string TryPlaceBid(string bidderKey, string bidderName, Hero hero, int amount, out string error)
        {
            SWMLog.Verbose("SWMAuction", "TryPlaceBid host key=" + bidderKey + " amount=" + amount);
            string ok = AuctionService.TryBid(MarketState.Ensure(), bidderKey, bidderName, hero, amount, false, out error);
            if (ok == null)
                SWMLog.Warn("SWMAuction", "TryPlaceBid host failed key=" + bidderKey + " amount=" + amount + " err=" + (error ?? ""));
            return ok;
        }

        public static void Tick()
        {
            AuctionService.Tick(MarketState.Ensure());
        }

        public static void StartNewLot()
        {
            SWMLog.Info("SWMAuction", "StartNewLot");
            AuctionService.StartNew(MarketState.Ensure());
        }

        public static void SettleNow()
        {
            SWMLog.Info("SWMAuction", "SettleNow");
            AuctionService.TrySettle(MarketState.Ensure(), true);
        }

        public static void RefreshStock()
        {
            SWMMarketApi.RefreshStock();
        }

        public static string PackMarket()
        {
            return SWMMarketApi.PackAll();
        }

        public static void UnpackMarket(string blob)
        {
            SWMMarketApi.UnpackAll(blob);
        }

        public static string PackAuction()
        {
            MarketState state = MarketState.Ensure();
            return state.Auction != null ? state.Auction.Pack() : "";
        }

        public static void UnpackAuction(string blob)
        {
            MarketState state = MarketState.Ensure();
            if (state.Auction == null)
                state.Auction = new AuctionState();
            state.Auction.Unpack(blob);
            SWMLog.Verbose("SWMAuction", "UnpackAuction high=" + state.Auction.HighBid + " key=" + (state.Auction.HighBidderKey ?? ""));
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

        public static string PackIdentities()
        {
            return MarketState.Ensure().PackIdentities();
        }

        public static void UnpackIdentities(string blob)
        {
            MarketState.Ensure().UnpackIdentities(blob);
        }

        public static long RefreshAtUtcTicks
        {
            get { return MarketState.Ensure().RefreshAtUtcTicks; }
            set { MarketState.Ensure().RefreshAtUtcTicks = value; }
        }

        public static int EscrowAmount
        {
            get
            {
                AuctionState a = MarketState.Ensure().Auction;
                return a != null ? a.EscrowAmount : 0;
            }
        }

        public static string EscrowBidderKey
        {
            get
            {
                AuctionState a = MarketState.Ensure().Auction;
                return a != null ? (a.EscrowBidderKey ?? "") : "";
            }
        }

        public static int HighBid
        {
            get
            {
                AuctionState a = MarketState.Ensure().Auction;
                return a != null ? a.HighBid : 0;
            }
        }

        public static string HighBidderKey
        {
            get
            {
                AuctionState a = MarketState.Ensure().Auction;
                return a != null ? (a.HighBidderKey ?? "") : "";
            }
        }
    }
}
