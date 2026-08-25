using System;
using System.Collections.Generic;

namespace SilverWandererMarket.Market
{
    internal sealed class MarketState
    {
        public static MarketState Current;

        public MarketConfig Config = new MarketConfig();
        public List<WandererOffer> Offers = new List<WandererOffer>();
        public HashSet<string> UsedIdentities = new HashSet<string>();
        public long RefreshAtUtcTicks;
        public string StatusMessage = "";
        public AuctionState Auction = new AuctionState();

        public static event Action Changed;

        public static MarketState Ensure()
        {
            if (Current == null)
                Current = new MarketState();
            return Current;
        }

        public TimeSpan TimeUntilRefresh()
        {
            long now = DateTime.UtcNow.Ticks;
            if (RefreshAtUtcTicks <= now)
                return TimeSpan.Zero;
            return TimeSpan.FromTicks(RefreshAtUtcTicks - now);
        }

        public bool RefreshDue()
        {
            return DateTime.UtcNow.Ticks >= RefreshAtUtcTicks;
        }

        public void EnsureStock()
        {
            if (!SWMAuctionHooks.AllowLocalGeneration)
                return;
            if (Offers == null || Offers.Count == 0 || RefreshDue())
                Refresh();
            else if (Auction == null || !Auction.HasLot)
                AuctionService.StartNew(this);
        }

        public void Refresh()
        {
            if (!SWMAuctionHooks.AllowLocalGeneration)
            {
                SWMLog.Verbose("SWMMarket", "Refresh skipped (AllowLocalGeneration=false)");
                return;
            }
            if (Config == null)
                Config = MarketConfig.Load();
            Offers = OfferGenerator.GenerateStock(Config, UsedIdentities);
            RefreshAtUtcTicks = DateTime.UtcNow.AddSeconds(Config.RefreshSeconds).Ticks;
            StatusMessage = "Stock refreshed.";
            AuctionService.StartNew(this);
            SWMLog.Info("SWMMarket", "Stock refreshed count=" + (Offers != null ? Offers.Count : 0));
            SWMAuctionHooks.Raise(SWMAuctionHookKind.StockRefreshed, Auction, "", "", 0, "", null);
            SWMMarketHooks.Raise(SWMMarketHookKind.StockRefreshed, "", "", "", "", 0, null);
            Notify();
        }

        public long AuctionCloseAtUtcTicks()
        {
            int closeBefore = Config != null ? Config.AuctionCloseBeforeRefreshSeconds : 60;
            if (closeBefore < 5)
                closeBefore = 5;
            return RefreshAtUtcTicks - TimeSpan.FromSeconds(closeBefore).Ticks;
        }

        public TimeSpan TimeUntilAuctionClose()
        {
            long now = DateTime.UtcNow.Ticks;
            long closeAt = AuctionCloseAtUtcTicks();
            if (closeAt <= now)
                return TimeSpan.Zero;
            return TimeSpan.FromTicks(closeAt - now);
        }

        public bool TryRemove(string id, out WandererOffer offer)
        {
            offer = null;
            if (Offers == null)
                return false;
            for (int i = 0; i < Offers.Count; i++)
            {
                if (Offers[i] != null && Offers[i].Id == id)
                {
                    offer = Offers[i];
                    Offers.RemoveAt(i);
                    Notify();
                    return true;
                }
            }
            return false;
        }

        public static void Notify()
        {
            Action handler = Changed;
            if (handler != null)
                handler();
        }

        public string PackOffers()
        {
            if (Offers == null || Offers.Count == 0)
                return "";
            string[] lines = new string[Offers.Count];
            for (int i = 0; i < Offers.Count; i++)
                lines[i] = Offers[i] != null ? Offers[i].Serialize() : "";
            return string.Join("\n", lines);
        }

        public void UnpackOffers(string blob)
        {
            Offers = new List<WandererOffer>();
            if (string.IsNullOrEmpty(blob))
                return;
            string[] lines = blob.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                WandererOffer o = WandererOffer.Deserialize(lines[i]);
                if (o != null)
                    Offers.Add(o);
            }
        }

        public string PackIdentities()
        {
            if (UsedIdentities == null || UsedIdentities.Count == 0)
                return "";
            string[] arr = new string[UsedIdentities.Count];
            UsedIdentities.CopyTo(arr);
            return string.Join("\n", arr);
        }

        public void UnpackIdentities(string blob)
        {
            UsedIdentities = new HashSet<string>();
            if (string.IsNullOrEmpty(blob))
                return;
            string[] lines = blob.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                    UsedIdentities.Add(lines[i]);
            }
        }
    }
}
