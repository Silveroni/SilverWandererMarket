using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace SilverWandererMarket.Market
{
    /// <summary>SWM wanderer-auction escrow: take gold on bid, refund previous high bidder.</summary>
    internal static class SWMAuctionEscrow
    {
        public static string TryTake(Hero hero, int amount)
        {
            if (amount <= 0)
                return null;
            if (hero == null)
            {
                SWMLog.Warn("SWMEscrow", "TryTake failed: no hero amount=" + amount);
                return "No player hero.";
            }
            if (SWMAuctionHooks.TryDebitGold != null)
            {
                string hooked = SWMAuctionHooks.TryDebitGold(hero, amount);
                if (hooked != null)
                    SWMLog.Warn("SWMEscrow", "TryDebitGold rejected hero=" + HeroId(hero) + " amount=" + amount + " err=" + hooked);
                else
                    SWMLog.Info("SWMEscrow", "TryDebitGold ok hero=" + HeroId(hero) + " amount=" + amount);
                return hooked;
            }
            if (hero.Gold < amount)
            {
                SWMLog.Warn("SWMEscrow", "TryTake not enough gold hero=" + HeroId(hero) + " have=" + hero.Gold + " need=" + amount);
                return "Not enough gold.";
            }
            GiveGoldAction.ApplyBetweenCharacters(hero, null, amount, true);
            SWMLog.Info("SWMEscrow", "Take vanilla gold hero=" + HeroId(hero) + " amount=" + amount + " remaining=" + hero.Gold);
            return null;
        }

        public static void Credit(Hero hero, string bidderKey, int amount)
        {
            if (amount <= 0)
                return;
            if (hero == null)
            {
                SWMLog.Error("SWMEscrow", "Credit unresolved: no hero for bidderKey=" + bidderKey + " amount=" + amount);
                SWMAuctionHooks.Raise(SWMAuctionHookKind.RefundUnresolved, MarketState.Ensure().Auction, bidderKey, "", amount, bidderKey,
                    "Escrow refund pending: no hero for bidder " + bidderKey);
                return;
            }
            if (SWMAuctionHooks.CreditGold != null)
            {
                SWMAuctionHooks.CreditGold(hero, amount);
                SWMLog.Info("SWMEscrow", "CreditGold hook hero=" + HeroId(hero) + " key=" + bidderKey + " amount=" + amount);
                return;
            }
            GiveGoldAction.ApplyBetweenCharacters(null, hero, amount, true);
            SWMLog.Info("SWMEscrow", "Credit vanilla gold hero=" + HeroId(hero) + " key=" + bidderKey + " amount=" + amount + " now=" + hero.Gold);
        }

        public static void RefundHeld(AuctionState a)
        {
            if (a == null || a.EscrowAmount <= 0 || string.IsNullOrEmpty(a.EscrowBidderKey))
            {
                if (a != null)
                {
                    a.EscrowAmount = 0;
                    a.EscrowBidderKey = "";
                }
                return;
            }

            string key = a.EscrowBidderKey;
            int amount = a.EscrowAmount;
            Hero hero = SWMAuctionHooks.ResolveHeroOrNull(key);
            a.EscrowAmount = 0;
            a.EscrowBidderKey = "";
            if (SWMAuctionHooks.IsLocalBidder(key))
                a.PlayerBid = 0;

            SWMLog.Info("SWMEscrow", "RefundHeld key=" + key + " amount=" + amount + " resolved=" + (hero != null));
            Credit(hero, key, amount);
            string name = hero != null && hero.Name != null ? hero.Name.ToString() : key;
            a.AddLog(name + " is outbid. " + amount.ToString("N0") + " denars returned.");
            SWMAuctionHooks.Raise(SWMAuctionHookKind.OutbidRefund, a, key, name, amount, key, null);
        }

        public static void CaptureHeld(AuctionState a)
        {
            if (a == null)
                return;
            SWMLog.Info("SWMEscrow", "CaptureHeld (win keep) key=" + (a.EscrowBidderKey ?? "") + " amount=" + a.EscrowAmount);
            a.EscrowAmount = 0;
            a.EscrowBidderKey = "";
        }

        private static string HeroId(Hero hero)
        {
            if (hero == null)
                return "?";
            if (hero.StringId != null)
                return hero.StringId;
            return hero.Name != null ? hero.Name.ToString() : "?";
        }
    }
}
