using System;
using TaleWorlds.CampaignSystem;

namespace SilverWandererMarket.Market
{
    /// <summary>Wanderer-auction event ids for <see cref="SWMAuctionHooks.Changed"/>. Prefixed <c>swm.auction.</c>.</summary>
    public static class SWMAuctionHookKind
    {
        public const string LotStarted = "swm.auction.lot-started";
        public const string BidPlaced = "swm.auction.bid-placed";
        public const string EscrowTaken = "swm.auction.escrow-taken";
        public const string OutbidRefund = "swm.auction.outbid-refund";
        public const string AuctionClosed = "swm.auction.closed";
        public const string SettleWin = "swm.auction.settle-win";
        public const string SettleNoSale = "swm.auction.settle-no-sale";
        public const string RefundUnresolved = "swm.auction.refund-unresolved";
        public const string StockRefreshed = "swm.auction.stock-refreshed";
    }

    public sealed class SWMAuctionHookEvent
    {
        public string Kind;
        public string BidderKey;
        public string BidderName;
        public string RefundedBidderKey;
        public int Amount;
        public int HighBid;
        public string HighBidderKey;
        public string LotId;
        public string Message;
    }

    /// <summary>
    /// Silver Wanderer Market — wanderer-lot auction events.
    /// Identity / gold / authority flags alias <see cref="SWMMarketHooks"/> (set them there).
    /// </summary>
    public static class SWMAuctionHooks
    {
        public static bool EnableSimulatedAiBidders
        {
            get { return SWMMarketHooks.EnableSimulatedAiBidders; }
            set { SWMMarketHooks.EnableSimulatedAiBidders = value; }
        }

        public static bool IsAuthoritative
        {
            get { return SWMMarketHooks.IsAuthoritative; }
            set { SWMMarketHooks.IsAuthoritative = value; }
        }

        public static bool AllowLocalGeneration
        {
            get { return SWMMarketHooks.AllowLocalGeneration; }
            set { SWMMarketHooks.AllowLocalGeneration = value; }
        }

        public static Func<Hero> GetLocalPlayerHero
        {
            get { return SWMMarketHooks.GetLocalPlayerHero; }
            set { SWMMarketHooks.GetLocalPlayerHero = value; }
        }

        public static Func<Hero, string> GetBidderKey
        {
            get { return SWMMarketHooks.GetPlayerKey; }
            set { SWMMarketHooks.GetPlayerKey = value; }
        }

        public static Func<string, Hero> ResolveHero
        {
            get { return SWMMarketHooks.ResolveHero; }
            set { SWMMarketHooks.ResolveHero = value; }
        }

        public static Func<Hero, int, string> TryDebitGold
        {
            get { return SWMMarketHooks.TryDebitGold; }
            set { SWMMarketHooks.TryDebitGold = value; }
        }

        public static Action<Hero, int> CreditGold
        {
            get { return SWMMarketHooks.CreditGold; }
            set { SWMMarketHooks.CreditGold = value; }
        }

        public static Func<Hero, string> CanReceiveCompanion
        {
            get { return SWMMarketHooks.CanReceiveCompanion; }
            set { SWMMarketHooks.CanReceiveCompanion = value; }
        }

        public static Func<WandererOffer, Hero, int, string> DeliverCompanion
        {
            get { return SWMMarketHooks.DeliverCompanion; }
            set { SWMMarketHooks.DeliverCompanion = value; }
        }

        public static event Action<SWMAuctionHookEvent> Changed;

        public static Hero LocalHero()
        {
            return SWMMarketHooks.LocalHero();
        }

        public static string LocalBidderKey()
        {
            return SWMMarketHooks.LocalPlayerKey();
        }

        public static bool IsLocalBidder(string bidderKey)
        {
            return SWMMarketHooks.IsLocalPlayer(bidderKey);
        }

        public static bool IsSimulatedAiKey(string bidderKey)
        {
            return !string.IsNullOrEmpty(bidderKey) && bidderKey.StartsWith("ai:", StringComparison.Ordinal);
        }

        public static Hero ResolveHeroOrNull(string bidderKey)
        {
            return SWMMarketHooks.ResolveHeroOrNull(bidderKey);
        }

        public static string CompanionGate(Hero winner)
        {
            return SWMMarketHooks.CompanionGate(winner);
        }

        internal static void Raise(string kind, AuctionState auction, string bidderKey, string bidderName, int amount, string refundedKey, string message)
        {
            string lotId = auction != null && auction.Lot != null ? auction.Lot.Id : "";
            int high = auction != null ? auction.HighBid : 0;
            string highKey = auction != null ? (auction.HighBidderKey ?? "") : "";
            string line = kind + " bidder=" + (bidderKey ?? "") + " name=" + (bidderName ?? "")
                + " amount=" + amount + " high=" + high + " highKey=" + highKey
                + " lot=" + lotId + " refunded=" + (refundedKey ?? "");
            if (!string.IsNullOrEmpty(message))
                line += " msg=" + message;
            if (kind == SWMAuctionHookKind.RefundUnresolved || kind == SWMAuctionHookKind.SettleNoSale)
                SWMLog.Warn("SWMAuction", line);
            else
                SWMLog.Info("SWMAuction", line);

            Action<SWMAuctionHookEvent> handler = Changed;
            if (handler == null)
                return;
            SWMAuctionHookEvent e = new SWMAuctionHookEvent();
            e.Kind = kind;
            e.BidderKey = bidderKey ?? "";
            e.BidderName = bidderName ?? "";
            e.RefundedBidderKey = refundedKey ?? "";
            e.Amount = amount;
            e.HighBid = high;
            e.HighBidderKey = highKey;
            e.LotId = lotId;
            e.Message = message ?? "";
            handler(e);
        }
    }
}
