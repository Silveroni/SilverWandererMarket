using System.Collections.Generic;

namespace SilverWandererMarket.Market
{
    /// <summary>
    /// Role registry. Titles follow the flavour of vanilla wanderer names ("the Surgeon",
    /// "the Tracker", "the Robber", "the Smith"), and every role owns a different primary
    /// skill so two roles never read the same on the slate.
    /// </summary>
    internal static class Archetypes
    {
        public const string Trader = "trader";
        public const string SpiceVendor = "spice";
        public const string Steward = "steward";
        public const string Scholar = "scholar";
        public const string Surgeon = "surgeon";
        public const string Engineer = "engineer";
        public const string Warrior = "warrior";
        public const string Brawler = "brawler";
        public const string Lancer = "lancer";
        public const string Archer = "archer";
        public const string Crossbowman = "crossbow";
        public const string Skirmisher = "skirmisher";
        public const string Tracker = "tracker";
        public const string Robber = "robber";
        public const string Smith = "smith";
        public const string Outrider = "outrider";
        public const string Generic = "generic";

        public static readonly string[] AllSkills =
        {
            "OneHanded", "TwoHanded", "Polearm", "Bow", "Crossbow", "Throwing",
            "Riding", "Athletics", "Crafting", "Tactics", "Scouting", "Roguery",
            "Charm", "Trade", "Steward", "Medicine", "Engineering", "Leadership"
        };

        private sealed class Def
        {
            public string Display;
            public string Primary;
            public bool Specialist;
            public string[] Support;
            public float[] SupportWeight;
        }

        private const float BaseWeight = 0.35f;

        private static readonly Dictionary<string, Def> Defs = Build();

        /// <summary>Commercially valuable roles, kept scarce and priced up.</summary>
        public static readonly string[] SpecialistIds =
        {
            Trader, SpiceVendor, Steward, Scholar, Surgeon, Engineer
        };

        public static readonly string[] FillerIds =
        {
            Warrior, Brawler, Lancer, Archer, Crossbowman, Skirmisher,
            Tracker, Robber, Smith, Outrider, Generic
        };

        public static bool IsSpecialist(string id)
        {
            Def d;
            return Defs.TryGetValue(id ?? "", out d) && d.Specialist;
        }

        public static string DisplayRole(string id)
        {
            Def d;
            return Defs.TryGetValue(id ?? "", out d) ? d.Display : "Wanderer";
        }

        public static string PrimarySkill(string id)
        {
            Def d;
            return Defs.TryGetValue(id ?? "", out d) ? d.Primary : "Athletics";
        }

        public static Dictionary<string, float> Weights(string id)
        {
            Dictionary<string, float> w = new Dictionary<string, float>();
            for (int i = 0; i < AllSkills.Length; i++)
                w[AllSkills[i]] = BaseWeight;

            Def d;
            if (!Defs.TryGetValue(id ?? "", out d))
                d = Defs[Generic];

            w[d.Primary] = 2.0f;
            for (int i = 0; i < d.Support.Length; i++)
                w[d.Support[i]] = d.SupportWeight[i];
            return w;
        }

        private static Dictionary<string, Def> Build()
        {
            Dictionary<string, Def> m = new Dictionary<string, Def>();

            Add(m, Trader, "Trader", "Trade", true,
                new[] { "Charm", "Steward", "Riding", "Roguery" },
                new[] { 1.00f, 0.85f, 0.70f, 0.55f });

            Add(m, SpiceVendor, "Spice Vendor", "Trade", true,
                new[] { "Roguery", "Scouting", "Charm", "Riding" },
                new[] { 1.20f, 1.10f, 0.80f, 0.75f });

            Add(m, Steward, "Steward", "Steward", true,
                new[] { "Leadership", "Charm", "Trade", "Tactics" },
                new[] { 1.05f, 0.90f, 0.85f, 0.70f });

            Add(m, Scholar, "Scholar", "Charm", true,
                new[] { "Engineering", "Medicine", "Steward", "Tactics" },
                new[] { 1.15f, 1.05f, 0.95f, 0.90f });

            Add(m, Surgeon, "Surgeon", "Medicine", true,
                new[] { "Charm", "Steward", "Athletics", "Roguery" },
                new[] { 1.00f, 0.85f, 0.65f, 0.55f });

            Add(m, Engineer, "Engineer", "Engineering", true,
                new[] { "Crafting", "Tactics", "Steward", "Crossbow" },
                new[] { 1.20f, 0.95f, 0.80f, 0.60f });

            Add(m, Warrior, "Sellsword", "OneHanded", false,
                new[] { "Athletics", "Riding", "Polearm", "Tactics" },
                new[] { 1.15f, 0.90f, 0.85f, 0.65f });

            Add(m, Brawler, "Brawler", "TwoHanded", false,
                new[] { "Athletics", "OneHanded", "Throwing", "Roguery" },
                new[] { 1.20f, 0.95f, 0.70f, 0.60f });

            Add(m, Lancer, "Lancer", "Polearm", false,
                new[] { "Riding", "Athletics", "OneHanded", "Tactics" },
                new[] { 1.25f, 0.90f, 0.80f, 0.70f });

            Add(m, Archer, "Archer", "Bow", false,
                new[] { "Athletics", "Scouting", "OneHanded", "Throwing" },
                new[] { 1.05f, 0.85f, 0.70f, 0.65f });

            Add(m, Crossbowman, "Crossbowman", "Crossbow", false,
                new[] { "Athletics", "OneHanded", "Engineering", "Crafting" },
                new[] { 1.00f, 0.85f, 0.70f, 0.60f });

            Add(m, Skirmisher, "Skirmisher", "Throwing", false,
                new[] { "Athletics", "OneHanded", "Scouting", "Riding" },
                new[] { 1.20f, 0.90f, 0.75f, 0.65f });

            Add(m, Tracker, "Tracker", "Scouting", false,
                new[] { "Bow", "Riding", "Athletics", "Roguery" },
                new[] { 1.05f, 1.00f, 0.85f, 0.70f });

            Add(m, Robber, "Robber", "Roguery", false,
                new[] { "Athletics", "OneHanded", "Scouting", "Throwing" },
                new[] { 1.10f, 0.95f, 0.85f, 0.65f });

            Add(m, Smith, "Smith", "Crafting", false,
                new[] { "TwoHanded", "Engineering", "Athletics", "Trade" },
                new[] { 0.95f, 0.90f, 0.80f, 0.65f });

            Add(m, Outrider, "Outrider", "Riding", false,
                new[] { "Scouting", "Polearm", "Bow", "Athletics" },
                new[] { 1.10f, 0.95f, 0.80f, 0.70f });

            Add(m, Generic, "Wanderer", "Athletics", false,
                new[] { "OneHanded", "Riding", "Scouting", "Roguery" },
                new[] { 0.70f, 0.60f, 0.55f, 0.50f });

            return m;
        }

        private static void Add(Dictionary<string, Def> m, string id, string display, string primary,
            bool specialist, string[] support, float[] weight)
        {
            Def d = new Def();
            d.Display = display;
            d.Primary = primary;
            d.Specialist = specialist;
            d.Support = support;
            d.SupportWeight = weight;
            m[id] = d;
        }
    }
}
