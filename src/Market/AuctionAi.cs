using System;
using TaleWorlds.Core;

namespace SilverWandererMarket.Market
{
    /// <summary>
    /// SP-only NPC rivals. Coop host/clients skip this so real players contest the lot.
    /// </summary>
    internal static class AuctionAi
    {
        private struct Rival
        {
            public string Key;
            public string Name;

            public Rival(string key, string name)
            {
                Key = key;
                Name = name;
            }
        }

        private static readonly Rival[] Rivals =
        {
            new Rival("ai:alarys", "Alarys the Clothier"),
            new Rival("ai:temun", "Temun of Chaikand"),
            new Rival("ai:sora", "Sora the Spice Factor"),
            new Rival("ai:gundar", "Gundar Ice-Brow"),
            new Rival("ai:larien", "Larien of Galend"),
            new Rival("ai:nasuh", "Nasuh of Quyaz")
        };

        public static bool ShouldRun(MarketState state)
        {
            if (!SWMAuctionHooks.IsAuthoritative)
                return false;
            if (!SWMAuctionHooks.EnableSimulatedAiBidders)
                return false;
            if (state == null || state.Auction == null || !state.Auction.HasLot)
                return false;
            if (state.Auction.Closed || state.Auction.Settled)
                return false;
            MarketConfig cfg = state.Config;
            if (cfg != null && !cfg.AuctionAiEnabled)
                return false;
            return true;
        }

        public static void OnLotStarted(MarketState state)
        {
            if (state == null || state.Auction == null)
                return;
            if (!ShouldRun(state))
            {
                state.Auction.NextAiBidUtcTicks = 0;
                return;
            }
            state.Auction.NextAiBidUtcTicks = DateTime.UtcNow.AddSeconds(12 + MBRandom.RandomInt(28)).Ticks;
        }

        public static void OnPlayerBid(MarketState state)
        {
            if (!ShouldRun(state))
                return;
            long soon = DateTime.UtcNow.AddSeconds(6 + MBRandom.RandomInt(14)).Ticks;
            if (state.Auction.NextAiBidUtcTicks <= 0 || soon < state.Auction.NextAiBidUtcTicks)
                state.Auction.NextAiBidUtcTicks = soon;
        }

        public static void Tick(MarketState state)
        {
            if (!ShouldRun(state))
                return;

            AuctionState a = state.Auction;
            long now = DateTime.UtcNow.Ticks;
            if (a.NextAiBidUtcTicks <= 0)
                a.NextAiBidUtcTicks = now + DelayTicks(state);
            if (now < a.NextAiBidUtcTicks)
                return;

            if (!TryPlaceRivalBid(state))
            {
                a.NextAiBidUtcTicks = DateTime.MaxValue.Ticks;
                return;
            }
            a.NextAiBidUtcTicks = now + DelayTicks(state);
        }

        private static bool TryPlaceRivalBid(MarketState state)
        {
            AuctionState a = state.Auction;
            MarketConfig cfg = state.Config ?? new MarketConfig();
            int minBid = cfg.AuctionMinBid < 1 ? 1000 : cfg.AuctionMinBid;
            int minRaise = cfg.AuctionMinRaise < 1 ? 1000 : cfg.AuctionMinRaise;
            int need = a.HighBid <= 0 ? minBid : a.HighBid + minRaise;

            Rival pick = default(Rival);
            int pickCeiling = 0;
            int found = 0;
            for (int i = 0; i < Rivals.Length; i++)
            {
                Rival rival = Rivals[i];
                if (a.HighBidderKey == rival.Key)
                    continue;
                int ceiling = CeilingFor(rival.Key, a.Lot, cfg);
                if (need > ceiling)
                    continue;
                found++;
                if (MBRandom.RandomInt(found) == 0)
                {
                    pick = rival;
                    pickCeiling = ceiling;
                }
            }
            if (found <= 0)
                return false;

            int bid = need;
            if (MBRandom.RandomFloat < 0.28f)
                bid += minRaise * (1 + MBRandom.RandomInt(3));
            if (bid > pickCeiling)
                bid = pickCeiling;
            if (bid < need)
                return found > 1;

            string err;
            string ok = AuctionService.TryBid(state, pick.Key, pick.Name, null, bid, false, out err);
            if (ok == null)
                SWMLog.Verbose("SWMAuction", "AI bid skipped key=" + pick.Key + " amount=" + bid + " err=" + (err ?? ""));
            return true;
        }

        private static long DelayTicks(MarketState state)
        {
            double secondsLeft = state.TimeUntilAuctionClose().TotalSeconds;
            int min;
            int span;
            if (secondsLeft <= 90)
            {
                min = 8;
                span = 14;
            }
            else if (secondsLeft <= 600)
            {
                min = 18;
                span = 32;
            }
            else
            {
                min = 70;
                span = 140;
            }
            return TimeSpan.FromSeconds(min + MBRandom.RandomInt(span)).Ticks;
        }

        private static int CeilingFor(string aiKey, WandererOffer lot, MarketConfig cfg)
        {
            int lo = cfg.AuctionAiBudgetMin;
            int hi = cfg.AuctionAiBudgetMax;
            if (lo < cfg.AuctionMinBid)
                lo = cfg.AuctionMinBid;
            if (hi < lo)
                hi = lo;
            int hash = (aiKey + "|" + (lot != null ? lot.Id : "")).GetHashCode() & int.MaxValue;
            int span = hi - lo;
            if (span <= 0)
                return lo;
            int stepped = lo + (hash % (span + 1));
            int raise = cfg.AuctionMinRaise < 1 ? 1000 : cfg.AuctionMinRaise;
            int aligned = (stepped / raise) * raise;
            if (aligned < lo)
                aligned = lo;
            if (aligned > hi)
                aligned = hi;
            return aligned;
        }
    }
}
