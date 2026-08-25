using SilverWandererMarket.Behaviors;
using SilverWandererMarket.Spawn;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SilverWandererMarket
{
    public class SWMSubModule : MBSubModuleBase
    {
        private WandererMarketCampaignBehavior _behavior;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            SWMLog.Info("SWMModule", "Silver Wanderer Market loaded");
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            CampaignGameStarter campaignStarter = gameStarterObject as CampaignGameStarter;
            if (campaignStarter == null)
                return;
            SWMLog.Info("SWMModule", "Campaign start — registering SWM behavior + tavern broker model");
            // So SettlementOverlay / HeroesWithoutParty place brokers in tavern (portrait strip).
            campaignStarter.AddModel(new SWMHeroAgentLocationModel());
            _behavior = new WandererMarketCampaignBehavior();
            campaignStarter.AddBehavior(_behavior);
            Market.SWMMarketHooks.ApplyDetectedSession();
        }

        public override void OnGameEnd(Game game)
        {
            UI.SWMMarketScreen.Close();
            Market.MarketState.Current = null;
            _behavior = null;
            Market.SWMMarketHooks.ResetSessionFlags();
            base.OnGameEnd(game);
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (_behavior != null)
                _behavior.RealtimeTick(dt);
        }
    }
}
