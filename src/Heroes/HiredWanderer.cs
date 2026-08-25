using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SilverWandererMarket.Heroes
{
    /// <summary>
    /// A hired wanderer is an ordinary player companion, with two deliberate departures from
    /// vanilla: they carry no written backstory, and dismissing one erases them from the world
    /// rather than releasing a fugitive who can turn up in a tavern again later.
    /// </summary>
    internal static class HiredWanderer
    {
        private const string TemplatePrefix = "swm_template_";

        // Already out of the clan by someone else's hand; only the erasing is left to do.
        private static readonly List<Hero> Pending = new List<Hero>();

        // Dismissed through our dialogue and still in the clan, so both steps are ours to run.
        private static readonly List<Hero> PendingDismissal = new List<Hero>();

        /// <summary>
        /// Identified by the template they were stamped from, which survives saving and loading
        /// and needs no bookkeeping of our own.
        /// </summary>
        public static bool IsHired(Hero hero)
        {
            if (hero == null)
                return false;
            CharacterObject template = hero.Template;
            return template != null
                && template.StringId != null
                && template.StringId.StartsWith(TemplatePrefix);
        }

        public static bool IsInConversation()
        {
            return IsHired(Hero.OneToOneConversationHero);
        }

        /// <summary>
        /// The player paid for these hands, so the game should not treat them as a stranger and
        /// run the "who are you?" introduction. Covers companions hired before this was added.
        /// </summary>
        public static void MarkAllMet()
        {
            IList<Clan> clans = Market.SWMMarketHooks.PlayerClans();
            if (clans == null)
                return;
            for (int c = 0; c < clans.Count; c++)
            {
                Clan clan = clans[c];
                if (clan == null || clan.Companions == null)
                    continue;
                foreach (Hero companion in clan.Companions)
                {
                    if (IsHired(companion) && !companion.HasMet)
                        companion.SetHasMet();
                }
            }
        }

        public static void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
        {
            if (detail != RemoveCompanionAction.RemoveCompanionDetail.Fire)
                return;
            if (!IsHired(companion) || !companion.IsAlive)
                return;
            if (!Pending.Contains(companion))
                Pending.Add(companion);
        }

        public static void QueueDismissal(Hero hero)
        {
            if (!IsHired(hero) || !hero.IsAlive)
                return;
            if (!PendingDismissal.Contains(hero))
                PendingDismissal.Add(hero);
        }

        /// <summary>
        /// Deferred on purpose. Dismissal happens mid-conversation with this very hero, inside a
        /// settlement where their agent is standing in the scene, and taking a companion apart in
        /// that state crashes the game. So we wait for the conversation to close and for any mission
        /// to end, which means the player has left the scene the hero was standing in.
        /// </summary>
        public static void FlushPendingRemovals()
        {
            if (Pending.Count == 0 && PendingDismissal.Count == 0)
                return;
            if (Campaign.Current != null
                && Campaign.Current.ConversationManager != null
                && Campaign.Current.ConversationManager.IsConversationInProgress)
                return;
            if (Mission.Current != null)
                return;

            if (PendingDismissal.Count > 0)
            {
                List<Hero> dismissed = new List<Hero>(PendingDismissal);
                PendingDismissal.Clear();
                for (int i = 0; i < dismissed.Count; i++)
                    Dismiss(dismissed[i]);
            }

            if (Pending.Count > 0)
            {
                List<Hero> batch = new List<Hero>(Pending);
                Pending.Clear();
                for (int i = 0; i < batch.Count; i++)
                {
                    Hero hero = batch[i];
                    // A re-hire between dismissal and this flush means they are wanted after all.
                    if (hero == null || !hero.IsAlive || hero.CompanionOf != null)
                        continue;
                    Erase(hero);
                }
            }
        }

        /// <summary>
        /// ApplyAfterQuest rather than ApplyByFire: the Fire path turns them loose as a fugitive and
        /// calls ResetEquipments on wanderers, neither of which we want for a hero about to be
        /// erased, and both of which are what made dismissal in a settlement fatal.
        /// </summary>
        private static void Dismiss(Hero hero)
        {
            if (hero == null || !hero.IsAlive)
                return;
            try
            {
                if (hero.CompanionOf != null)
                    RemoveCompanionAction.ApplyAfterQuest(hero.CompanionOf, hero);
            }
            catch
            {
                return;
            }
            Erase(hero);
        }

        private static void Erase(Hero hero)
        {
            string name = hero != null && hero.Name != null ? hero.Name.ToString() : "";
            string id = hero != null ? hero.StringId : "";
            try
            {
                KillCharacterAction.ApplyByRemove(hero, false, true);
            }
            catch
            {
            }
            Market.SWMMarketHooks.Raise(Market.SWMMarketHookKind.CompanionDismissed, "", "", id, name, 0, null);
            SWMLog.Info("SWMHire", "Companion dismissed/erased id=" + id + " name=" + name);
        }
    }
}
