using System.Collections.Generic;
using TaleWorlds.Core;

namespace SilverWandererMarket.Market
{
    /// <summary>
    /// Picks the roles for one refresh. Each refresh gets its own demand profile, so one hour
    /// the tavern is thick with archers and the next it is caravan hands, instead of every
    /// slate looking like the same even spread. A per-role cap stops any one role flooding it.
    /// </summary>
    internal sealed class RoleDraft
    {
        private readonly Dictionary<string, float> _demand = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _taken = new Dictionary<string, int>();
        private readonly int _specialistCap;
        private readonly int _fillerCap;

        private RoleDraft(int specialistCap, int fillerCap)
        {
            _specialistCap = specialistCap;
            _fillerCap = fillerCap;
        }

        public static RoleDraft ForRefresh(MarketConfig cfg, int specialistSlots, int fillerSlots)
        {
            int wanted = cfg != null && cfg.MaxPerRole > 0 ? cfg.MaxPerRole : 3;
            RoleDraft draft = new RoleDraft(
                FeasibleCap(wanted, specialistSlots, Archetypes.SpecialistIds.Length),
                FeasibleCap(wanted, fillerSlots, Archetypes.FillerIds.Length));

            draft.SeedDemand(Archetypes.SpecialistIds);
            draft.SeedDemand(Archetypes.FillerIds);
            return draft;
        }

        public string Draw(bool specialist)
        {
            string[] pool = specialist ? Archetypes.SpecialistIds : Archetypes.FillerIds;
            int cap = specialist ? _specialistCap : _fillerCap;

            float total = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                if (Taken(pool[i]) < cap)
                    total += Demand(pool[i]);
            }

            string chosen = null;
            if (total > 0f)
            {
                float roll = MBRandom.RandomFloat * total;
                for (int i = 0; i < pool.Length; i++)
                {
                    if (Taken(pool[i]) >= cap)
                        continue;
                    roll -= Demand(pool[i]);
                    if (roll <= 0f)
                    {
                        chosen = pool[i];
                        break;
                    }
                }
            }
            if (chosen == null)
                chosen = LeastTaken(pool);

            _taken[chosen] = Taken(chosen) + 1;
            return chosen;
        }

        /// <summary>
        /// Most roles sit around ordinary demand; a couple get a surge each refresh so the
        /// slate clusters naturally rather than sampling flat.
        /// </summary>
        private void SeedDemand(string[] pool)
        {
            for (int i = 0; i < pool.Length; i++)
                _demand[pool[i]] = 0.35f + MBRandom.RandomFloat * 0.65f;

            int surges = 1 + MBRandom.RandomInt(2);
            for (int i = 0; i < surges; i++)
            {
                string hot = pool[MBRandom.RandomInt(pool.Length)];
                _demand[hot] = _demand[hot] * (1.8f + MBRandom.RandomFloat * 1.2f);
            }
        }

        /// <summary>Raise the cap only if the pool physically cannot fill the slots at it.</summary>
        private static int FeasibleCap(int wanted, int slots, int poolSize)
        {
            if (wanted < 1)
                wanted = 1;
            if (poolSize < 1)
                return slots > 0 ? slots : 1;
            while (wanted * poolSize < slots)
                wanted++;
            return wanted;
        }

        private float Demand(string id)
        {
            float v;
            return _demand.TryGetValue(id, out v) ? v : 0.5f;
        }

        private int Taken(string id)
        {
            int v;
            return _taken.TryGetValue(id, out v) ? v : 0;
        }

        private string LeastTaken(string[] pool)
        {
            string best = pool[0];
            int bestCount = Taken(best);
            for (int i = 1; i < pool.Length; i++)
            {
                int c = Taken(pool[i]);
                if (c < bestCount)
                {
                    best = pool[i];
                    bestCount = c;
                }
            }
            return best;
        }
    }
}
