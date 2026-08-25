using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SilverWandererMarket.Spawn
{
    internal static class TavernBrokerSpawner
    {
        public const string BrokerId = BrokerHeroService.TemplateId;
        public const string TavernLocationId = "tavern";

        /// <summary>
        /// Add broker to the town's tavern Location list (menu portrait strip + mission).
        /// Safe to call without an active mission.
        /// </summary>
        public static void EnsureListedInTavern(Settlement settlement)
        {
            try
            {
                if (!Market.SWMMarketHooks.AllowBrokerSpawn)
                    return;
                if (settlement == null || !settlement.IsTown || settlement.LocationComplex == null)
                    return;

                Hero broker = BrokerHeroService.GetBrokerForSettlement(settlement);
                if (broker == null || !broker.IsAlive || broker.CharacterObject == null)
                    return;

                Location location = settlement.LocationComplex.GetLocationWithId(TavernLocationId);
                if (location == null)
                {
                SWMLog.Info("SWMBroker", "EnsureListed: no tavern location @ " + settlement.StringId);
                    return;
                }

                if (location.GetLocationCharacter(broker) != null)
                    return;

                AddBrokerToLocation(location, broker, "npc_common");
                SWMLog.Info("SWMBroker", "EnsureListed broker in tavern UI list @ " + settlement.StringId);
            }
            catch (Exception ex)
            {
                SWMLog.Error("SWMBroker", "EnsureListed EXCEPTION: " + ex, ex);
            }
        }

        public static void TrySpawn(Dictionary<string, int> unusedUsablePointCount)
        {
            try
            {
                if (!Market.SWMMarketHooks.AllowBrokerSpawn)
                    return;
                ICampaignMission mission = CampaignMission.Current;
                if (mission == null || mission.Location == null)
                    return;
                Location location = mission.Location;
                if (location.StringId != TavernLocationId)
                    return;

                Settlement settlement = Settlement.CurrentSettlement;
                if (settlement == null || !settlement.IsTown)
                {
                    SWMLog.Info("SWMBroker", "abort: not in a town settlement");
                    return;
                }

                Hero broker = BrokerHeroService.GetBrokerForSettlement(settlement);
                if (broker == null || !broker.IsAlive || broker.CharacterObject == null)
                {
                    SWMLog.Info("SWMBroker", "abort: no broker hero for " + settlement.StringId);
                    InformationManager.DisplayMessage(new InformationMessage("SWM: could not create Wanderer Broker for this town."));
                    return;
                }

                if (location.GetLocationCharacter(broker) != null)
                {
                    SWMLog.Info("SWMBroker", "hero already in tavern location list: " + settlement.StringId);
                    return;
                }

                string spawnTag = PickSpawnTag(unusedUsablePointCount);
                ConsumeSpawnTag(unusedUsablePointCount, spawnTag);
                AddBrokerToLocation(location, broker, spawnTag);
                SWMLog.Info("SWMBroker", "spawned broker HERO " + broker.CharacterObject.StringId + " tag=" + spawnTag + " @ " + settlement.StringId);
            }
            catch (Exception ex)
            {
                SWMLog.Error("SWMBroker", "EXCEPTION: " + ex, ex);
                try
                {
                    InformationManager.DisplayMessage(new InformationMessage("SWM broker spawn failed: " + ex.GetType().Name + ", see swm_debug.log"));
                }
                catch
                {
                }
            }
        }

        private static void AddBrokerToLocation(Location location, Hero broker, string spawnTag)
        {
            Monster monster = GetSettlementMonster(broker.CharacterObject);
            AgentData agentData = new AgentData(new SimpleAgentOrigin(broker.CharacterObject, -1))
                .Monster(monster)
                .NoHorses(true);

            LocationCharacter locChar = new LocationCharacter(
                agentData,
                new LocationCharacter.AddBehaviorsDelegate(AddBehaviors),
                spawnTag,
                true,
                LocationCharacter.CharacterRelations.Neutral,
                null,
                true,
                false,
                null,
                false,
                false,
                true,
                null,
                false);

            location.AddCharacter(locChar);
        }

        private static string PickSpawnTag(Dictionary<string, int> points)
        {
            if (points == null)
                return "npc_common";
            string[] preferred = { "npc_common", "npc_common_limited", "sp_notable", "spawnpoint_npc_common" };
            for (int i = 0; i < preferred.Length; i++)
            {
                int n;
                if (points.TryGetValue(preferred[i], out n) && n > 0)
                    return preferred[i];
            }
            foreach (KeyValuePair<string, int> kv in points)
            {
                if (kv.Value > 0)
                    return kv.Key;
            }
            return "npc_common";
        }

        private static void ConsumeSpawnTag(Dictionary<string, int> points, string tag)
        {
            if (points == null || tag == null)
                return;
            int n;
            if (points.TryGetValue(tag, out n) && n > 0)
                points[tag] = n - 1;
        }

        private static void AddBehaviors(IAgent agent)
        {
            try
            {
                if (Campaign.Current != null && Campaign.Current.SandBoxManager != null && Campaign.Current.SandBoxManager.AgentBehaviorManager != null)
                    Campaign.Current.SandBoxManager.AgentBehaviorManager.AddFixedCharacterBehaviors(agent);
            }
            catch (Exception ex)
            {
                SWMLog.Info("SWMBroker", "AddBehaviors: " + ex.Message);
            }
        }

        private static Monster GetSettlementMonster(CharacterObject character)
        {
            try
            {
                return FaceGen.GetMonsterWithSuffix(character.Race, "_settlement");
            }
            catch
            {
                try { return FaceGen.GetBaseMonsterFromRace(character.Race); }
                catch { return null; }
            }
        }
    }
}
