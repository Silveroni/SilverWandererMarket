using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SilverWandererMarket.Market
{
    public sealed class WandererOffer
    {
        public string Id;
        public string FirstName;
        public string RoleTitle;
        public string CultureId;
        public bool IsFemale;
        public int Age;
        public string ArchetypeId;
        public string QualityId;
        /// <summary>Continuous quality in [0,1] that drove this offer's skills and price.</summary>
        public float Tier;
        public int Price;
        public int FaceSeed;
        public Dictionary<string, int> Skills = new Dictionary<string, int>();

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(FirstName) ? RoleTitle : FirstName + " the " + RoleTitle; }
        }

        public string IdentityKey
        {
            get { return (CultureId ?? "") + "|" + (FirstName ?? "").ToLowerInvariant(); }
        }

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Esc(Id)).Append('\t');
            sb.Append(Esc(FirstName)).Append('\t');
            sb.Append(Esc(RoleTitle)).Append('\t');
            sb.Append(Esc(CultureId)).Append('\t');
            sb.Append(IsFemale ? "1" : "0").Append('\t');
            sb.Append(Age).Append('\t');
            sb.Append(Esc(ArchetypeId)).Append('\t');
            sb.Append(Esc(QualityId)).Append('\t');
            sb.Append(Price).Append('\t');
            sb.Append(FaceSeed).Append('\t');
            bool first = true;
            foreach (KeyValuePair<string, int> kv in Skills)
            {
                if (!first)
                    sb.Append(';');
                first = false;
                sb.Append(kv.Key).Append('=').Append(kv.Value);
            }
            sb.Append('\t').Append(Tier.ToString("R", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        public static WandererOffer Deserialize(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;
            string[] p = line.Split('\t');
            if (p.Length < 11)
                return null;
            WandererOffer o = new WandererOffer();
            o.Id = Unesc(p[0]);
            o.FirstName = Unesc(p[1]);
            o.RoleTitle = Unesc(p[2]);
            o.CultureId = Unesc(p[3]);
            o.IsFemale = p[4] == "1";
            int age;
            int.TryParse(p[5], out age);
            o.Age = age;
            o.ArchetypeId = Unesc(p[6]);
            o.QualityId = Unesc(p[7]);
            int price;
            int.TryParse(p[8], out price);
            o.Price = price;
            int seed;
            int.TryParse(p[9], out seed);
            o.FaceSeed = seed;
            if (!string.IsNullOrEmpty(p[10]))
            {
                string[] parts = p[10].Split(';');
                for (int i = 0; i < parts.Length; i++)
                {
                    int eq = parts[i].IndexOf('=');
                    if (eq <= 0)
                        continue;
                    int val;
                    if (int.TryParse(parts[i].Substring(eq + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                        o.Skills[parts[i].Substring(0, eq)] = val;
                }
            }
            float tier;
            if (p.Length < 12 || !float.TryParse(p[11], NumberStyles.Float, CultureInfo.InvariantCulture, out tier))
                tier = QualityCurve.TierFromLabel(o.QualityId);
            o.Tier = tier;
            return o;
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n");
        }

        private static string Unesc(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\\\", "\\");
        }
    }
}
