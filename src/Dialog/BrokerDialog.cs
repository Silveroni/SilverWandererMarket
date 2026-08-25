using SilverWandererMarket.Market;
using SilverWandererMarket.Spawn;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;

namespace SilverWandererMarket.Dialog
{
    /// <summary>
    /// One conversation tree: greet → options (browse / how it works / leave).
    /// High priority so Special-hero intro does not force a second talk.
    /// </summary>
    internal static class BrokerDialog
    {
        private const int Priority = 200;

        public static void AddDialogs(CampaignGameStarter starter)
        {
            if (starter == null)
                return;

            // Win "start" over generic Special / hero intro on first approach.
            starter.AddDialogLine(
                "swm_broker_start",
                "start",
                "swm_broker_root",
                "{=swm_broker_greeting}Coin finds steel, and steel finds work. I keep twenty wanderers on the slate, the same names in every town.",
                IsBroker,
                null,
                Priority);

            // If the game still drops into hero options after an intro, offer a direct path.
            starter.AddPlayerLine(
                "swm_broker_from_main",
                "hero_main_options",
                "swm_broker_root",
                "{=swm_broker_about}About your wanderer slate...",
                IsBroker,
                null,
                Priority);

            // --- Root options ---
            starter.AddPlayerLine(
                "swm_broker_browse",
                "swm_broker_root",
                "swm_broker_open",
                "{=swm_broker_browse}Show me the slate.",
                null,
                null,
                Priority);

            starter.AddPlayerLine(
                "swm_broker_how",
                "swm_broker_root",
                "swm_broker_how_reply",
                "{=swm_broker_how}How does this work?",
                null,
                null,
                Priority);

            starter.AddPlayerLine(
                "swm_broker_price",
                "swm_broker_root",
                "swm_broker_price_reply",
                "{=swm_broker_price}What do they cost?",
                null,
                null,
                Priority);

            starter.AddPlayerLine(
                "swm_broker_leave",
                "swm_broker_root",
                "close_window",
                "{=swm_broker_leave}Another time.",
                null,
                null,
                Priority);

            // Browse → short ack → open UI
            starter.AddDialogLine(
                "swm_broker_open",
                "swm_broker_open",
                "close_window",
                "{=swm_broker_open_ack}Aye. Pick carefully. Once they're bought, they're gone from the slate.",
                null,
                OpenMarket,
                Priority);

            // How it works → back to root
            starter.AddDialogLine(
                "swm_broker_how_reply",
                "swm_broker_how_reply",
                "swm_broker_root",
                "{=swm_broker_how_text}Twenty names. Refreshes every hour. Pay the purse, they join your party. Sold ones don't return.",
                null,
                null,
                Priority);

            // Price → back to root
            starter.AddDialogLine(
                "swm_broker_price_reply",
                "swm_broker_price_reply",
                "swm_broker_root",
                "{=swm_broker_price_text}Most are green, a couple thousand denars. Decent blades cost more. Truly gifted names? You pay through the nose.",
                null,
                null,
                Priority);
        }

        private static bool IsBroker()
        {
            if (Hero.OneToOneConversationHero != null && BrokerHeroService.IsBroker(Hero.OneToOneConversationHero))
                return true;
            return BrokerHeroService.IsBrokerCharacter(CharacterObject.OneToOneConversationCharacter);
        }

        private static void OpenMarket()
        {
            SWMMarketApi.RequestOpen();
        }
    }
}
