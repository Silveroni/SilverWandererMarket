using System;
using TaleWorlds.Core;

namespace SilverWandererMarket.Market
{
    internal static class PriceCalculator
    {
        /// <summary>
        /// Smooth exponential in the offer's tier: floor * (top/floor)^(tier^curve).
        /// Cheap stock stays near the floor, the top end climbs steeply, and nothing
        /// jumps at a tier boundary because there are no boundaries.
        /// </summary>
        public static int Calculate(WandererOffer offer, MarketConfig cfg)
        {
            if (offer == null || cfg == null)
                return 2000;

            double floor = Math.Max(1, cfg.PriceFloor);
            double top = Math.Max(floor * 2.0, cfg.PriceTop);
            double curve = cfg.PriceCurve < 0.5f ? 0.5f : cfg.PriceCurve;

            double tier = offer.Tier;
            if (tier < 0) tier = 0;
            if (tier > 1) tier = 1;

            // Start a little above the floor, otherwise haggling on the cheapest stock is
            // all clipped away and the bottom of the slate shows one identical price.
            double start = floor * 1.2;
            double price = start * Math.Pow(top / start, Math.Pow(tier, curve));
            price *= Haggle(offer, cfg);
            if (Archetypes.IsSpecialist(offer.ArchetypeId))
                price *= cfg.SpecialistMultiplier;

            if (price < cfg.PriceFloor)
                price = cfg.PriceFloor;
            if (price > int.MaxValue)
                price = int.MaxValue;
            return (int)Math.Round(price);
        }

        /// <summary>
        /// Nudge within priceVariance so two wanderers of the same tier rarely cost the same.
        /// Half of the swing tracks how well the specialisation actually rolled, half is the
        /// broker's mood, which keeps price loosely honest without being readable.
        /// </summary>
        private static double Haggle(WandererOffer offer, MarketConfig cfg)
        {
            float spread = cfg.PriceVariance;
            if (spread < 0f) spread = 0f;
            if (spread > 0.5f) spread = 0.5f;
            if (spread == 0f)
                return 1.0;

            int expected = QualityCurve.TopSkillFor(offer.Tier);
            int actual;
            offer.Skills.TryGetValue(Archetypes.PrimarySkill(offer.ArchetypeId), out actual);
            double realized = expected > 0 ? (double)actual / expected : 1.0;
            if (realized < 0.85) realized = 0.85;
            if (realized > 1.15) realized = 1.15;

            double skillPart = (realized - 1.0) / 0.15;
            double moodPart = MBRandom.RandomFloat * 2.0 - 1.0;
            return 1.0 + spread * (0.5 * skillPart + 0.5 * moodPart);
        }
    }
}
