using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace SilverWandererMarket.Market
{
    /// <summary>
    /// Continuous wanderer quality. A single tier value in [0,1] drives skills, price and label,
    /// so the slate spreads smoothly instead of snapping to fixed percentage buckets.
    /// Skill shapes are fitted to the 67 vanilla wanderer templates in spspecialcharacters.xml:
    /// top skill min 70 / p25 110 / median 120 / p75 140 / p90 160 / max 210, 8-12 non-zero skills.
    /// </summary>
    internal static class QualityCurve
    {
        /// <summary>Tier anchors mapping tier to the wanderer's best skill, fitted to vanilla percentiles.</summary>
        private static readonly float[] AnchorTier = { 0.00f, 0.12f, 0.30f, 0.60f, 0.82f, 0.94f, 1.00f };
        private static readonly float[] AnchorTop = { 72f, 108f, 124f, 145f, 168f, 200f, 250f };

        /// <summary>
        /// Falloff from the best skill down the roster. Vanilla wanderers taper rather than
        /// sitting flat, e.g. 120/105/95/85/80/60/50/40/30/20.
        /// </summary>
        private static readonly float[] Taper =
        {
            1.00f, 0.84f, 0.74f, 0.66f, 0.58f, 0.49f, 0.41f,
            0.33f, 0.25f, 0.18f, 0.12f, 0.08f, 0.05f, 0.03f
        };

        /// <summary>Skewed continuous roll: most wanderers are ordinary, standouts get rarer smoothly.</summary>
        public static float Roll(MarketConfig cfg)
        {
            float curve = cfg != null ? cfg.QualityCurve : 2.2f;
            if (curve < 1f)
                curve = 1f;
            float u = MBRandom.RandomFloat;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return (float)Math.Pow(u, curve);
        }

        public static int TopSkillFor(float tier)
        {
            float t = Clamp01(tier);
            for (int i = 1; i < AnchorTier.Length; i++)
            {
                if (t <= AnchorTier[i])
                {
                    float span = AnchorTier[i] - AnchorTier[i - 1];
                    float f = span <= 0f ? 0f : (t - AnchorTier[i - 1]) / span;
                    return (int)Math.Round(AnchorTop[i - 1] + (AnchorTop[i] - AnchorTop[i - 1]) * f);
                }
            }
            return (int)Math.Round(AnchorTop[AnchorTop.Length - 1]);
        }

        /// <summary>Vanilla wanderers fill 8-12 of the 18 skills; better ones are rounded out further.</summary>
        public static int SkillCountFor(float tier)
        {
            int count = 8 + (int)Math.Floor(Clamp01(tier) * 3f) + MBRandom.RandomInt(3);
            if (count < 8) count = 8;
            if (count > 13) count = 13;
            return count;
        }

        /// <summary>
        /// Lays the taper over the archetype's skill priority so the specialisation is always the
        /// best skill, while which supporting skills come through varies run to run.
        /// </summary>
        public static Dictionary<string, int> BuildSkills(string archetype, int topSkill, int skillCount, int cap)
        {
            if (cap < 1)
                cap = 330;
            string primary = Archetypes.PrimarySkill(archetype);
            Dictionary<string, float> weights = Archetypes.Weights(archetype);

            List<string> order = new List<string>();
            List<float> score = new List<float>();
            for (int i = 0; i < Archetypes.AllSkills.Length; i++)
            {
                string skill = Archetypes.AllSkills[i];
                if (skill == primary)
                    continue;
                float w;
                weights.TryGetValue(skill, out w);
                order.Add(skill);
                score.Add(w * (0.75f + MBRandom.RandomFloat * 0.5f));
            }
            for (int i = 0; i < order.Count - 1; i++)
            {
                for (int j = i + 1; j < order.Count; j++)
                {
                    if (score[j] > score[i])
                    {
                        float sf = score[i]; score[i] = score[j]; score[j] = sf;
                        string sk = order[i]; order[i] = order[j]; order[j] = sk;
                    }
                }
            }
            order.Insert(0, primary);

            Dictionary<string, int> skills = new Dictionary<string, int>();
            for (int rank = 0; rank < order.Count; rank++)
            {
                int value = 0;
                if (rank < skillCount)
                {
                    float ratio = rank < Taper.Length ? Taper[rank] : Taper[Taper.Length - 1];
                    float jitter = 0.92f + MBRandom.RandomFloat * 0.16f;
                    value = Round5(topSkill * ratio * jitter);
                    if (value < 5)
                        value = 5;
                    if (value > cap)
                        value = cap;
                }
                skills[order[rank]] = value;
            }
            return skills;
        }

        /// <summary>Display band. Descriptive only — generation and pricing read the tier directly.</summary>
        public static string Label(float tier)
        {
            float t = Clamp01(tier);
            if (t < 0.20f) return "low";
            if (t < 0.45f) return "mid";
            if (t < 0.72f) return "high";
            if (t < 0.92f) return "elite";
            return "legendary";
        }

        /// <summary>Approximate tier from a legacy band id, for offers saved before tiers existed.</summary>
        public static float TierFromLabel(string quality)
        {
            if (quality == "auction") return 1f;
            if (quality == "legendary") return 0.95f;
            if (quality == "elite") return 0.80f;
            if (quality == "high") return 0.58f;
            if (quality == "mid") return 0.32f;
            return 0.10f;
        }

        private static int Round5(double v)
        {
            return (int)(Math.Round(v / 5.0) * 5.0);
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
