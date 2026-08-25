using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace SilverWandererMarket.Market
{
    internal static class OfferGenerator
    {
        private static readonly string[] Cultures = { "empire", "vlandia", "sturgia", "battania", "khuzait", "aserai" };

        public static List<WandererOffer> GenerateStock(MarketConfig cfg, HashSet<string> usedIdentities)
        {
            List<WandererOffer> list = new List<WandererOffer>();
            if (cfg == null)
                cfg = new MarketConfig();
            if (usedIdentities == null)
                usedIdentities = new HashSet<string>();

            int specCount = MBRandom.RandomInt(cfg.SpecialistMin, cfg.SpecialistMax + 1);
            if (specCount > cfg.StockSize)
                specCount = cfg.StockSize;

            HashSet<int> specSlots = new HashSet<int>();
            while (specSlots.Count < specCount)
                specSlots.Add(MBRandom.RandomInt(cfg.StockSize));

            RoleDraft roles = RoleDraft.ForRefresh(cfg, specCount, cfg.StockSize - specCount);

            for (int i = 0; i < cfg.StockSize; i++)
            {
                bool specialist = specSlots.Contains(i);
                WandererOffer offer = GenerateOne(cfg, usedIdentities, roles.Draw(specialist));
                if (offer == null)
                    continue;
                usedIdentities.Add(offer.IdentityKey);
                list.Add(offer);
            }
            return list;
        }

        /// <summary>One-off exceptional specialist for the auction booth — above slate legendary.</summary>
        public static WandererOffer GenerateAuctionLot(MarketConfig cfg, HashSet<string> usedIdentities)
        {
            if (cfg == null)
                cfg = new MarketConfig();
            if (usedIdentities == null)
                usedIdentities = new HashSet<string>();

            string archetype = Archetypes.SpecialistIds[MBRandom.RandomInt(Archetypes.SpecialistIds.Length)];
            string culture = Cultures[MBRandom.RandomInt(Cultures.Length)];
            bool female = MBRandom.RandomFloat < 0.5f;
            CultureObject cultureObj = MBObjectManager.Instance.GetObject<CultureObject>(culture);
            string first = UniqueFirstName(cultureObj, female, usedIdentities, culture);

            WandererOffer offer = new WandererOffer();
            offer.Id = Guid.NewGuid().ToString("N");
            offer.FirstName = first;
            offer.RoleTitle = Archetypes.DisplayRole(archetype);
            offer.CultureId = culture;
            offer.IsFemale = female;
            offer.Age = 28 + MBRandom.RandomInt(18);
            offer.ArchetypeId = archetype;
            offer.Tier = 1f;
            offer.QualityId = "auction";
            offer.FaceSeed = MBRandom.RandomInt();
            // Deliberately above anything the slate can roll, and rounded out across more skills.
            int top = cfg.AuctionTopSkillMin + MBRandom.RandomInt(Math.Max(1, cfg.AuctionTopSkillMax - cfg.AuctionTopSkillMin + 1));
            offer.Skills = QualityCurve.BuildSkills(archetype, top, 13 + MBRandom.RandomInt(3), cfg.SkillCap);
            // Display-only estimate; real price is the winning bid.
            offer.Price = Math.Max(cfg.AuctionMinBid, cfg.PriceTop);
            return offer;
        }

        private static WandererOffer GenerateOne(MarketConfig cfg, HashSet<string> usedIdentities, string archetype)
        {
            float tier = QualityCurve.Roll(cfg);
            string culture = Cultures[MBRandom.RandomInt(Cultures.Length)];
            bool female = MBRandom.RandomFloat < 0.5f;
            CultureObject cultureObj = MBObjectManager.Instance.GetObject<CultureObject>(culture);
            string first = UniqueFirstName(cultureObj, female, usedIdentities, culture);

            WandererOffer offer = new WandererOffer();
            offer.Id = Guid.NewGuid().ToString("N");
            offer.FirstName = first;
            offer.RoleTitle = Archetypes.DisplayRole(archetype);
            offer.CultureId = culture;
            offer.IsFemale = female;
            offer.Age = 22 + MBRandom.RandomInt(26);
            offer.ArchetypeId = archetype;
            offer.Tier = tier;
            offer.QualityId = QualityCurve.Label(tier);
            offer.FaceSeed = MBRandom.RandomInt();
            offer.Skills = QualityCurve.BuildSkills(archetype, QualityCurve.TopSkillFor(tier), QualityCurve.SkillCountFor(tier), cfg.SkillCap);
            offer.Price = PriceCalculator.Calculate(offer, cfg);
            return offer;
        }

        private static readonly string[] Bynames =
        {
            "Ashen", "Blackmane", "Crow", "Driftwood", "Farstride", "Frost", "Greyfell",
            "Ironhand", "Keeneye", "Longroad", "Marsh", "Oakenshield", "Quiet", "Raven",
            "Salt", "Stormborn", "Thorn", "Vale", "Wolfbite", "Yew", "Redwake", "Stonebrook",
            "Harrow", "Nightwell", "Dunebar", "Briar", "Skylark", "Hollow", "Grimholt", "Wavecrest"
        };

        private static string UniqueFirstName(CultureObject cultureObj, bool female, HashSet<string> used, string cultureId)
        {
            // Phase 1: keep rolling culture first names — never append numbers.
            for (int attempt = 0; attempt < 100; attempt++)
            {
                string name = SanitizeName(TryName(cultureObj, female));
                if (string.IsNullOrEmpty(name))
                    continue;
                if (IsIdentityFree(used, cultureId, name))
                    return name;
            }

            // Phase 2: first name + Bannerlord-style byname (no digits).
            for (int attempt = 0; attempt < 80; attempt++)
            {
                string first = SanitizeName(TryName(cultureObj, female));
                if (string.IsNullOrEmpty(first))
                    first = female ? "Sora" : "Kael";
                string by = Bynames[MBRandom.RandomInt(Bynames.Length)];
                string name = first + " " + by;
                if (IsIdentityFree(used, cultureId, name))
                    return name;
            }

            // Phase 3: rare fallback — compound of two first names.
            for (int attempt = 0; attempt < 40; attempt++)
            {
                string a = SanitizeName(TryName(cultureObj, female));
                string b = SanitizeName(TryName(cultureObj, female));
                if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b)
                    continue;
                string name = a + " " + b;
                if (IsIdentityFree(used, cultureId, name))
                    return name;
            }

            // Last resort: unique but still human-readable (no digits).
            string baseName = SanitizeName(TryName(cultureObj, female));
            if (string.IsNullOrEmpty(baseName))
                baseName = female ? "Sora" : "Kael";
            for (int i = 0; i < Bynames.Length; i++)
            {
                string name = baseName + " " + Bynames[(MBRandom.RandomInt(Bynames.Length) + i) % Bynames.Length];
                if (IsIdentityFree(used, cultureId, name))
                    return name;
            }
            return baseName + " " + Bynames[MBRandom.RandomInt(Bynames.Length)];
        }

        private static bool IsIdentityFree(HashSet<string> used, string cultureId, string name)
        {
            if (string.IsNullOrEmpty(name) || used == null)
                return !string.IsNullOrEmpty(name);
            string key = cultureId + "|" + name.ToLowerInvariant();
            return !used.Contains(key);
        }

        /// <summary>Reject empty / placeholder / digit-suffixed generator output.</summary>
        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            name = name.Trim();
            if (name.Length < 2 || name.IndexOf('{') >= 0)
                return null;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsDigit(name[i]))
                    return null;
            }
            return name;
        }

        private static string TryName(CultureObject cultureObj, bool female)
        {
            try
            {
                if (cultureObj != null && NameGenerator.Current != null)
                {
                    TextObject to = NameGenerator.Current.GenerateFirstNameForPlayer(cultureObj, female);
                    if (to != null)
                    {
                        string s = to.ToString();
                        if (!string.IsNullOrEmpty(s) && s.IndexOf('{') < 0)
                            return s;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

    }
}
