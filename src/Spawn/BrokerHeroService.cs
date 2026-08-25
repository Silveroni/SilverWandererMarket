using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace SilverWandererMarket.Spawn
{
    /// <summary>
    /// One Wanderer Broker hero per town, staying in that settlement (Hex Quest Giver pattern).
    /// </summary>
    internal static class BrokerHeroService
    {
        public const string TemplateId = "swm_wanderer_broker";
        public const string HeroIdPrefix = "swm_broker_";

        // settlement StringId -> hero StringId
        private static Dictionary<string, string> _map = new Dictionary<string, string>();

        public static void Unpack(string blob)
        {
            _map = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(blob))
                return;
            string[] lines = blob.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line))
                    continue;
                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                _map[line.Substring(0, eq)] = line.Substring(eq + 1);
            }
        }

        public static string Pack()
        {
            if (_map.Count == 0)
                return "";
            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, string> kv in _map)
                lines.Add(kv.Key + "=" + kv.Value);
            return string.Join("\n", lines.ToArray());
        }

        public static bool IsBroker(Hero hero)
        {
            if (hero == null || hero.CharacterObject == null)
                return false;
            string id = hero.CharacterObject.StringId;
            if (id != null && id.StartsWith(HeroIdPrefix))
                return true;
            foreach (string heroId in _map.Values)
            {
                if (heroId == id || (hero.StringId != null && hero.StringId == heroId))
                    return true;
            }
            TextObject name = hero.Name;
            return name != null && name.ToString().IndexOf("Wanderer Broker", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsBrokerCharacter(CharacterObject character)
        {
            if (character == null)
                return false;
            if (character.StringId == TemplateId)
                return true;
            if (character.StringId != null && character.StringId.StartsWith(HeroIdPrefix))
                return true;
            if (character.IsHero && character.HeroObject != null)
                return IsBroker(character.HeroObject);
            return false;
        }

        public static Hero GetBrokerForSettlement(Settlement settlement)
        {
            if (settlement == null || !settlement.IsTown)
                return null;
            string sid = settlement.StringId;
            string heroId;
            if (_map.TryGetValue(sid, out heroId))
            {
                Hero existing = Hero.FindFirst(h => h != null && (h.StringId == heroId || (h.CharacterObject != null && h.CharacterObject.StringId == heroId)));
                if (existing != null && existing.IsAlive)
                {
                    EnsurePlaced(existing, settlement);
                    EnsureHiddenFromEncyclopedia(existing);
                    return existing;
                }
            }
            if (!Market.SWMMarketHooks.AllowBrokerSpawn)
                return null;
            return CreateBroker(settlement);
        }

        public static void EnsureAllTowns()
        {
            if (!Market.SWMMarketHooks.AllowBrokerSpawn)
                return;
            if (Campaign.Current == null)
                return;
            int created = 0;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || !settlement.IsTown)
                    continue;
                Hero h = GetBrokerForSettlement(settlement);
                if (h != null)
                    created++;
            }
            SWMLog.Info("SWMBroker", "EnsureAllTowns brokers ready=" + created);
        }

        private static Hero CreateBroker(Settlement settlement)
        {
            CharacterObject template = CharacterObject.Find(TemplateId);
            if (template == null)
            {
                SWMLog.Info("SWMBroker", "template missing " + TemplateId);
                return null;
            }
            try
            {
                Hero hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 48);
                if (hero == null)
                {
                    SWMLog.Info("SWMBroker", "CreateSpecialHero returned null for " + settlement.StringId);
                    return null;
                }

                TextObject name = new TextObject("{=swm_broker_name}Wanderer Broker");
                hero.SetName(name, name);
                if (hero.Occupation != Occupation.Special)
                    hero.SetNewOccupation(Occupation.Special);
                hero.ChangeState(Hero.CharacterStates.Active);
                EnsurePlaced(hero, settlement);
                EnsureHiddenFromEncyclopedia(hero);

                string key = hero.CharacterObject != null ? hero.CharacterObject.StringId : hero.StringId;
                _map[settlement.StringId] = key;
                SWMLog.Info("SWMBroker", "created broker " + key + " at " + settlement.StringId);
                return hero;
            }
            catch (Exception ex)
            {
                SWMLog.Error("SWMBroker", "CreateBroker EXCEPTION " + ex, ex);
                return null;
            }
        }

        /// <summary>
        /// The broker is a shop front rather than someone the player should be able to look up.
        /// The template carries is_hidden_encyclopedia, which covers brokers created from now on;
        /// this also stamps the hero itself so brokers already saved in an older game are hidden.
        /// DefaultEncyclopediaHeroPage rejects the hero if either flag is set.
        /// </summary>
        private static void EnsureHiddenFromEncyclopedia(Hero hero)
        {
            if (hero == null)
                return;
            try
            {
                hero.HiddenInEncyclopedia = true;
                if (hero.CharacterObject != null)
                    hero.CharacterObject.HiddenInEncyclopedia = true;
            }
            catch (Exception ex)
            {
                SWMLog.Info("SWMBroker", "EnsureHiddenFromEncyclopedia: " + ex.Message);
            }
        }

        private static void EnsurePlaced(Hero hero, Settlement settlement)
        {
            if (hero == null || settlement == null)
                return;
            try
            {
                if (hero.StayingInSettlement != settlement)
                    hero.StayingInSettlement = settlement;
                // StayingInSettlement is enough for HeroAgentSpawnCampaignBehavior to place them;
                // AddHeroWithoutParty is not a public API on this game build.
            }
            catch (Exception ex)
            {
                SWMLog.Info("SWMBroker", "EnsurePlaced: " + ex.Message);
            }
        }
    }
}
