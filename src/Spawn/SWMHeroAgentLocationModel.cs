using SilverWandererMarket.Spawn;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace SilverWandererMarket.Spawn
{
    /// <summary>
    /// Puts Wanderer Brokers in the town tavern so SettlementOverlay lists them
    /// (same path Hex uses via HeroAgentLocationModel for quest givers).
    /// </summary>
    public sealed class SWMHeroAgentLocationModel : DefaultHeroAgentLocationModel
    {
        public override Location GetLocationForHero(
            Hero hero,
            Settlement settlement,
            out HeroAgentLocationModel.HeroLocationDetail heroLocationDetail)
        {
            if (BrokerHeroService.IsBroker(hero)
                && settlement != null
                && settlement.IsTown
                && settlement.LocationComplex != null)
            {
                Location tavern = settlement.LocationComplex.GetLocationWithId(TavernBrokerSpawner.TavernLocationId);
                if (tavern != null)
                {
                    heroLocationDetail = HeroAgentLocationModel.HeroLocationDetail.Wanderer;
                    return tavern;
                }
            }

            return base.GetLocationForHero(hero, settlement, out heroLocationDetail);
        }
    }
}
