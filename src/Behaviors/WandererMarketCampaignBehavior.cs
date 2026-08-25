using System.Collections.Generic;
using SilverWandererMarket.Dialog;
using SilverWandererMarket.Heroes;
using SilverWandererMarket.Market;
using SilverWandererMarket.Spawn;
using SilverWandererMarket.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace SilverWandererMarket.Behaviors
{
    public sealed class WandererMarketCampaignBehavior : CampaignBehaviorBase
    {
        private string _offerBlob = "";
        private string _identityBlob = "";
        private string _brokerBlob = "";
        private string _auctionBlob = "";
        private long _refreshAtUtcTicks;
        private bool _loaded;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameReady);
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, OnLocationCharactersReady);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
            CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, HiredWanderer.OnCompanionRemoved);
            CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            MarketState state = MarketState.Ensure();
            if (dataStore.IsSaving)
            {
                _offerBlob = state.PackOffers();
                _identityBlob = state.PackIdentities();
                _refreshAtUtcTicks = state.RefreshAtUtcTicks;
                _brokerBlob = BrokerHeroService.Pack();
                _auctionBlob = state.Auction != null ? state.Auction.Pack() : "";
            }
            dataStore.SyncData("_swm_offers", ref _offerBlob);
            dataStore.SyncData("_swm_identities", ref _identityBlob);
            dataStore.SyncData("_swm_refresh_at", ref _refreshAtUtcTicks);
            dataStore.SyncData("_swm_brokers", ref _brokerBlob);
            dataStore.SyncData("_swm_auction", ref _auctionBlob);
            if (dataStore.IsLoading)
            {
                state.UnpackOffers(_offerBlob);
                state.UnpackIdentities(_identityBlob);
                state.RefreshAtUtcTicks = _refreshAtUtcTicks;
                BrokerHeroService.Unpack(_brokerBlob);
                if (state.Auction == null)
                    state.Auction = new AuctionState();
                state.Auction.Unpack(_auctionBlob);
                _loaded = true;
            }
        }

        private void OnNewGameReady(CampaignGameStarter starter)
        {
            BrokerHeroService.EnsureAllTowns();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            SWMLog.Info("SWMSession", "OnSessionLaunched authoritative=" + SWMMarketHooks.IsAuthoritative
                + " generate=" + SWMMarketHooks.AllowLocalGeneration
                + " brokers=" + SWMMarketHooks.AllowBrokerSpawn
                + " testGold=" + SWMMarketHooks.AllowTestGold);
            MarketState state = MarketState.Ensure();
            state.Config = MarketConfig.Load();
            if (SWMAuctionHooks.AllowLocalGeneration)
            {
                if (!_loaded || state.Offers == null || state.Offers.Count == 0)
                    state.EnsureStock();
                else if (state.RefreshDue())
                    state.Refresh();
                else if (state.Auction == null || !state.Auction.HasLot)
                    AuctionService.StartNew(state);
            }
            BrokerDialog.AddDialogs(starter);
            HiredWandererDialog.AddDialogs(starter);
            BrokerHeroService.EnsureAllTowns();
            HiredWanderer.MarkAllMet();
            TryGrantTestGold(state.Config);
        }

        private void OnConversationEnded(IEnumerable<CharacterObject> characters)
        {
            HiredWanderer.FlushPendingRemovals();
        }

        private void OnTick(float dt)
        {
            HiredWanderer.FlushPendingRemovals();
        }

        private static void TryGrantTestGold(MarketConfig cfg)
        {
            if (!SWMAuctionHooks.IsAuthoritative || !SWMMarketHooks.AllowTestGold)
                return;
            if (cfg == null || cfg.TestGrantGold <= 0)
                return;
            Hero hero = SWMMarketHooks.LocalHero();
            if (hero == null)
                return;
            int have = hero.Gold;
            if (have >= cfg.TestGrantGold)
                return;
            int add = cfg.TestGrantGold - have;
            hero.ChangeHeroGold(add);
            SWMLog.Info("SWMSession", "Test gold granted +" + add + " now=" + cfg.TestGrantGold);
            InformationManagerDisplay("SWM test gold: +" + add.ToString("N0") + " (now " + cfg.TestGrantGold.ToString("N0") + "). Set testGrantGold to 0 in market-config.json when done.");
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (settlement != null && settlement.IsTown && party == MobileParty.MainParty)
                BrokerHeroService.GetBrokerForSettlement(settlement);
        }

        private void OnGameMenuOpened(MenuCallbackArgs args)
        {
            // Menu portrait strip reads MenuLocations; ensure broker is placed before/while tavern UI shows.
            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null || !settlement.IsTown)
                return;
            string menuId = args != null && args.MenuContext != null ? args.MenuContext.GameMenu.StringId : null;
            if (menuId == null)
                return;
            // town_backstreet / town_wait / town / etc. — any town menu that can show tavern characters
            if (menuId.IndexOf("town", System.StringComparison.OrdinalIgnoreCase) < 0
                && menuId.IndexOf("tavern", System.StringComparison.OrdinalIgnoreCase) < 0
                && menuId.IndexOf("backstreet", System.StringComparison.OrdinalIgnoreCase) < 0)
                return;
            TavernBrokerSpawner.EnsureListedInTavern(settlement);
        }

        private void OnLocationCharactersReady(Dictionary<string, int> unusedUsablePointCount)
        {
            TavernBrokerSpawner.TrySpawn(unusedUsablePointCount);
        }

        public void RealtimeTick(float dt)
        {
            MarketState state = MarketState.Ensure();
            if (Campaign.Current == null)
                return;
            if (SWMAuctionHooks.IsAuthoritative)
            {
                AuctionService.Tick(state);
                if (state.RefreshDue())
                {
                    state.Refresh();
                    if (SWMMarketScreen.IsOpen)
                        InformationManagerDisplay("Wanderer market stock refreshed.");
                }
            }
            SWMMarketScreen.Tick(dt);
        }

        private static void InformationManagerDisplay(string text)
        {
            TaleWorlds.Library.InformationManager.DisplayMessage(new TaleWorlds.Library.InformationMessage(text));
        }
    }
}
