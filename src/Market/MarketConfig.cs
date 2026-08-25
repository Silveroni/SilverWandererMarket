using System.Collections.Generic;

namespace SilverWandererMarket.Market
{
    internal sealed class MarketConfig
    {
        public int StockSize = 20;
        public int RefreshSeconds = 3600;
        public int SpecialistMin = 2;
        public int SpecialistMax = 4;
        /// <summary>Cap on repeats of one role per refresh. Auto-raised if the role pool is too small to fill the slate.</summary>
        public int MaxPerRole = 3;
        public int PriceFloor = 1500;
        /// <summary>Price of a perfect-tier wanderer; everything below scales toward PriceFloor.</summary>
        public int PriceTop = 1500000;
        /// <summary>Higher = flatter at the cheap end and steeper at the top.</summary>
        public float PriceCurve = 1.45f;
        public float SpecialistMultiplier = 1.2f;
        public float PriceVariance = 0.25f;
        public int SkillCap = 330;
        /// <summary>Skew of the quality roll. 1 = flat, higher = ordinary is common and standouts are rare.</summary>
        public float QualityCurve = 2.2f;
        /// <summary>Above the best the slate can roll (~270), so the lot is always the standout.</summary>
        public int AuctionTopSkillMin = 290;
        public int AuctionTopSkillMax = 320;
        public int AuctionMinBid = 1000;
        public int AuctionMinRaise = 1000;
        public int AuctionBidCooldownSeconds = 10;
        public int AuctionCloseBeforeRefreshSeconds = 60;
        /// <summary>NPC rival bids in SP. Coop auto-detect forces this off regardless.</summary>
        public bool AuctionAiEnabled = true;
        /// <summary>Lowest an NPC will spend on a lot.</summary>
        public int AuctionAiBudgetMin = 40000;
        /// <summary>Highest an NPC will spend on a lot.</summary>
        public int AuctionAiBudgetMax = 320000;

        public static MarketConfig Load()
        {
            MarketConfig cfg = new MarketConfig();
            try
            {
                string path = System.IO.Path.Combine(TaleWorlds.Library.BasePath.Name, "Modules", "SilverWandererMarket", "ModuleData", "market-config.json");
                if (!System.IO.File.Exists(path))
                    return cfg;
                string json = System.IO.File.ReadAllText(path);
                Dictionary<string, string> map = FlatJson.Parse(json);
                cfg.StockSize = FlatJson.Int(map, "stockSize", cfg.StockSize);
                cfg.RefreshSeconds = FlatJson.Int(map, "refreshSeconds", cfg.RefreshSeconds);
                cfg.SpecialistMin = FlatJson.Int(map, "specialistMin", cfg.SpecialistMin);
                cfg.SpecialistMax = FlatJson.Int(map, "specialistMax", cfg.SpecialistMax);
                cfg.MaxPerRole = FlatJson.Int(map, "maxPerRole", cfg.MaxPerRole);
                cfg.PriceFloor = FlatJson.Int(map, "priceFloor", cfg.PriceFloor);
                cfg.PriceTop = FlatJson.Int(map, "priceTop", cfg.PriceTop);
                cfg.PriceCurve = FlatJson.Float(map, "priceCurve", cfg.PriceCurve);
                cfg.SpecialistMultiplier = FlatJson.Float(map, "specialistMultiplier", cfg.SpecialistMultiplier);
                cfg.PriceVariance = FlatJson.Float(map, "priceVariance", cfg.PriceVariance);
                cfg.SkillCap = FlatJson.Int(map, "skillCap", cfg.SkillCap);
                cfg.QualityCurve = FlatJson.Float(map, "qualityCurve", cfg.QualityCurve);
                cfg.AuctionTopSkillMin = FlatJson.Int(map, "auctionTopSkillMin", cfg.AuctionTopSkillMin);
                cfg.AuctionTopSkillMax = FlatJson.Int(map, "auctionTopSkillMax", cfg.AuctionTopSkillMax);
                cfg.AuctionMinBid = FlatJson.Int(map, "auctionMinBid", cfg.AuctionMinBid);
                cfg.AuctionMinRaise = FlatJson.Int(map, "auctionMinRaise", cfg.AuctionMinRaise);
                cfg.AuctionBidCooldownSeconds = FlatJson.Int(map, "auctionBidCooldownSeconds", cfg.AuctionBidCooldownSeconds);
                cfg.AuctionCloseBeforeRefreshSeconds = FlatJson.Int(map, "auctionCloseBeforeRefreshSeconds", cfg.AuctionCloseBeforeRefreshSeconds);
                string aiFlag;
                if (map.TryGetValue("auctionAiEnabled", out aiFlag))
                    cfg.AuctionAiEnabled = aiFlag == "1" || string.Equals(aiFlag, "true", System.StringComparison.OrdinalIgnoreCase);
                cfg.AuctionAiBudgetMin = FlatJson.Int(map, "auctionAiBudgetMin", cfg.AuctionAiBudgetMin);
                cfg.AuctionAiBudgetMax = FlatJson.Int(map, "auctionAiBudgetMax", cfg.AuctionAiBudgetMax);
            }
            catch
            {
            }
            if (cfg.SpecialistMax < cfg.SpecialistMin)
                cfg.SpecialistMax = cfg.SpecialistMin;
            if (cfg.MaxPerRole < 1)
                cfg.MaxPerRole = 3;
            if (cfg.StockSize < 1)
                cfg.StockSize = 20;
            if (cfg.RefreshSeconds < 10)
                cfg.RefreshSeconds = 10;
            if (cfg.AuctionMinBid < 1)
                cfg.AuctionMinBid = 1000;
            if (cfg.AuctionMinRaise < 1)
                cfg.AuctionMinRaise = 1000;
            if (cfg.AuctionBidCooldownSeconds < 1)
                cfg.AuctionBidCooldownSeconds = 10;
            if (cfg.AuctionCloseBeforeRefreshSeconds < 5)
                cfg.AuctionCloseBeforeRefreshSeconds = 60;
            if (cfg.SkillCap < 50)
                cfg.SkillCap = 330;
            if (cfg.QualityCurve < 1f)
                cfg.QualityCurve = 1f;
            if (cfg.PriceCurve < 0.5f)
                cfg.PriceCurve = 0.5f;
            if (cfg.PriceTop < cfg.PriceFloor * 2)
                cfg.PriceTop = cfg.PriceFloor * 2;
            if (cfg.AuctionTopSkillMax < cfg.AuctionTopSkillMin)
                cfg.AuctionTopSkillMax = cfg.AuctionTopSkillMin;
            if (cfg.AuctionAiBudgetMin < cfg.AuctionMinBid)
                cfg.AuctionAiBudgetMin = cfg.AuctionMinBid;
            if (cfg.AuctionAiBudgetMax < cfg.AuctionAiBudgetMin)
                cfg.AuctionAiBudgetMax = cfg.AuctionAiBudgetMin;
            return cfg;
        }
    }

    internal static class FlatJson
    {
        public static Dictionary<string, string> Parse(string json)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json))
                return map;
            int i = 0;
            Skip(json, ref i);
            if (i >= json.Length || json[i] != '{')
                return map;
            i++;
            while (i < json.Length)
            {
                Skip(json, ref i);
                if (i < json.Length && json[i] == '}')
                    break;
                if (i >= json.Length || json[i] != '"')
                    break;
                string key = ReadString(json, ref i);
                Skip(json, ref i);
                if (i >= json.Length || json[i] != ':')
                    break;
                i++;
                Skip(json, ref i);
                string value;
                if (i < json.Length && json[i] == '"')
                    value = ReadString(json, ref i);
                else
                    value = ReadBare(json, ref i);
                map[key] = value;
                Skip(json, ref i);
                if (i < json.Length && json[i] == ',')
                    i++;
            }
            return map;
        }

        public static int Int(Dictionary<string, string> map, string key, int fallback)
        {
            string s;
            int n;
            return map.TryGetValue(key, out s) && int.TryParse(s, out n) ? n : fallback;
        }

        public static float Float(Dictionary<string, string> map, string key, float fallback)
        {
            string s;
            float n;
            return map.TryGetValue(key, out s) && float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out n) ? n : fallback;
        }

        private static void Skip(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
        }

        private static string ReadString(string json, ref int i)
        {
            i++;
            int start = i;
            while (i < json.Length && json[i] != '"')
            {
                if (json[i] == '\\' && i + 1 < json.Length)
                    i += 2;
                else
                    i++;
            }
            string s = json.Substring(start, i - start);
            if (i < json.Length)
                i++;
            return s;
        }

        private static string ReadBare(string json, ref int i)
        {
            int start = i;
            while (i < json.Length && json[i] != ',' && json[i] != '}' && !char.IsWhiteSpace(json[i]))
                i++;
            return json.Substring(start, i - start);
        }
    }
}
