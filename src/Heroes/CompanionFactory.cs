using SilverWandererMarket.Market;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SilverWandererMarket.Heroes
{
    internal static class CompanionFactory
    {
        public static string TryHire(WandererOffer offer, out string error)
        {
            return TryHire(offer, null, null, null, true, out error);
        }

        public static string TryHire(WandererOffer offer, Hero buyer, Clan clan, MobileParty party, bool chargeGold, out string error)
        {
            error = null;
            if (offer == null)
            {
                error = "That wanderer is gone.";
                return null;
            }
            if (buyer == null)
                buyer = SWMMarketHooks.LocalHero();
            if (clan == null && buyer != null)
                clan = buyer.Clan;
            if (clan == null)
                clan = Clan.PlayerClan;
            if (party == null && buyer != null)
                party = buyer.PartyBelongedTo;
            if (party == null)
                party = MobileParty.MainParty;
            if (buyer == null || clan == null || party == null)
            {
                error = "No party to join.";
                return null;
            }
            if (clan.Companions != null && clan.Companions.Count >= clan.CompanionLimit)
            {
                error = "Companion limit reached.";
                return null;
            }
            if (chargeGold)
            {
                string debitErr = SWMAuctionEscrow.TryTake(buyer, offer.Price);
                if (debitErr != null)
                {
                    error = debitErr;
                    return null;
                }
            }

            CharacterObject template = WandererAppearance.ResolveTemplate(offer);
            if (template == null)
            {
                if (chargeGold)
                    SWMAuctionEscrow.Credit(buyer, "", offer.Price);
                error = "Wanderer template missing.";
                return null;
            }

            // Null clan: CreateSpecialHero(..., PlayerClan, ...) puts them in Family/Lords.
            // Companions are clanless wanderers until AddCompanionAction attaches them.
            Hero hero = HeroCreator.CreateSpecialHero(template, Settlement.CurrentSettlement, null, null, offer.Age);
            if (hero == null)
            {
                if (chargeGold)
                    SWMAuctionEscrow.Credit(buyer, "", offer.Price);
                error = "Could not create companion.";
                return null;
            }

            TextObject first = new TextObject(offer.FirstName);
            TextObject full = new TextObject("{FIRSTNAME} the {ROLE}");
            full.SetTextVariable("FIRSTNAME", first);
            full.SetTextVariable("ROLE", offer.RoleTitle);
            hero.SetName(full, first);
            ApplySkills(hero, offer);
            WandererAppearance.ApplyTo(hero, template, offer);
            if (hero.Occupation != Occupation.Wanderer)
                hero.SetNewOccupation(Occupation.Wanderer);
            hero.ChangeState(Hero.CharacterStates.Active);
            // Bought and paid for, so skip the vanilla stranger introduction on first talk.
            hero.SetHasMet();

            AddCompanionAction.Apply(clan, hero);
            AddHeroToPartyAction.Apply(hero, party, true);
            return hero.Name != null ? hero.Name.ToString() : offer.DisplayName;
        }

        private static void ApplySkills(Hero hero, WandererOffer offer)
        {
            hero.ClearSkills();
            foreach (var kv in offer.Skills)
            {
                SkillObject skill = FindSkill(kv.Key);
                if (skill != null)
                    hero.SetSkillValue(skill, kv.Value);
            }
        }

        private static SkillObject FindSkill(string id)
        {
            if (DefaultSkills.OneHanded != null && DefaultSkills.OneHanded.StringId == id) return DefaultSkills.OneHanded;
            switch (id)
            {
                case "OneHanded": return DefaultSkills.OneHanded;
                case "TwoHanded": return DefaultSkills.TwoHanded;
                case "Polearm": return DefaultSkills.Polearm;
                case "Bow": return DefaultSkills.Bow;
                case "Crossbow": return DefaultSkills.Crossbow;
                case "Throwing": return DefaultSkills.Throwing;
                case "Riding": return DefaultSkills.Riding;
                case "Athletics": return DefaultSkills.Athletics;
                case "Crafting": return DefaultSkills.Crafting;
                case "Tactics": return DefaultSkills.Tactics;
                case "Scouting": return DefaultSkills.Scouting;
                case "Roguery": return DefaultSkills.Roguery;
                case "Charm": return DefaultSkills.Charm;
                case "Trade": return DefaultSkills.Trade;
                case "Steward": return DefaultSkills.Steward;
                case "Medicine": return DefaultSkills.Medicine;
                case "Engineering": return DefaultSkills.Engineering;
                case "Leadership": return DefaultSkills.Leadership;
            }
            return null;
        }

    }
}
