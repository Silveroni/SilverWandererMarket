using SilverWandererMarket.Heroes;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace SilverWandererMarket.Dialog
{
    /// <summary>
    /// Vanilla builds a wanderer's introduction from per-template text (backstory_a.[template],
    /// response_1.[template] and so on) defined in wanderer_strings.xml. Our templates have no
    /// such entries, so every vanilla backstory line would resolve to nothing and the
    /// conversation would show blank or placeholder text.
    ///
    /// These lines outrank the vanilla ones on the same tokens and hand the player straight back
    /// to the normal companion options, so a hired wanderer simply declines to discuss a past
    /// they were never written to have. Everything else about talking to them stays vanilla.
    /// </summary>
    internal static class HiredWandererDialog
    {
        // Above the vanilla wanderer lines, which sit at the default 100 and at 110 for the
        // first-meeting line.
        private const int Priority = 150;

        private const string Decline =
            "{=swm_hired_no_backstory}My past is my own business. I am here to work, and I will do it well.";

        private const string Dismiss =
            "{=swm_hired_dismiss}Your service ends here. Take what you have earned and go.";

        private const string DismissReply =
            "{=swm_hired_dismiss_reply}As you say. I will trouble you no further.";

        public static void AddDialogs(CampaignGameStarter starter)
        {
            if (starter == null)
                return;

            starter.AddDialogLine(
                "swm_hired_preintro",
                "wanderer_preintroduction",
                "hero_main_options",
                Decline,
                HiredWanderer.IsInConversation,
                null,
                Priority);

            // Reached from the "What's your story again?" option, which vanilla offers for any
            // hero with the Wanderer occupation.
            starter.AddDialogLine(
                "swm_hired_intro",
                "wanderer_introduction_a",
                "hero_main_options",
                Decline,
                HiredWanderer.IsInConversation,
                null,
                Priority);

            // Sits beside the vanilla role options, under "About your position in the clan...".
            starter.AddPlayerLine(
                "swm_hired_dismiss",
                "companion_role",
                "swm_hired_dismissed",
                Dismiss,
                CanDismiss,
                null,
                Priority);

            starter.AddDialogLine(
                "swm_hired_dismissed",
                "swm_hired_dismissed",
                "close_window",
                DismissReply,
                null,
                OnDismissed);
        }

        /// <summary>
        /// Vanilla already offers dismissal, but only out on the map. This covers the settlements
        /// where the player actually meets a hire, so the two never appear side by side.
        /// </summary>
        private static bool CanDismiss()
        {
            Hero hero = Hero.OneToOneConversationHero;
            return HiredWanderer.IsHired(hero)
                && hero.IsPlayerCompanion
                && hero.PartyBelongedTo == MobileParty.MainParty
                && Settlement.CurrentSettlement != null;
        }

        /// <summary>
        /// Nothing is done to the hero here on purpose. We are mid-conversation, inside a settlement,
        /// with their agent standing in the scene, and removing a companion in that state crashes the
        /// game. The work is queued and carried out once the scene is clear.
        /// </summary>
        private static void OnDismissed()
        {
            HiredWanderer.QueueDismissal(Hero.OneToOneConversationHero);
        }
    }
}
