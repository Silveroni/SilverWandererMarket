using System.Collections.Generic;
using SilverWandererMarket.Market;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;

namespace SilverWandererMarket.Heroes
{
    /// <summary>
    /// Single source of truth for what an offer looks like, so the preview in the market and the
    /// hero the player actually ends up with cannot drift apart.
    /// </summary>
    internal static class WandererAppearance
    {
        public const string TemplatePrefix = "swm_template_";

        public static CharacterObject ResolveTemplate(WandererOffer offer)
        {
            if (offer == null)
                return null;
            string id = TemplatePrefix + offer.CultureId + (offer.IsFemale ? "_f" : "");
            CharacterObject template = CharacterObject.Find(id);
            if (template == null)
                template = CharacterObject.Find(TemplatePrefix + "empire");
            return template;
        }

        /// <summary>
        /// The age is forced to the offer's own age rather than left at whatever the template range
        /// produced, because a hero's body properties are rebuilt from Hero.Age at display time.
        /// Without this the preview and the hired hero would differ in build even with one seed.
        /// </summary>
        public static BodyProperties BuildBody(CharacterObject template, WandererOffer offer)
        {
            if (template == null || offer == null)
                return BodyProperties.Default;
            try
            {
                BodyProperties min = template.GetBodyPropertiesMin(false);
                BodyProperties max = template.GetBodyPropertiesMax(false);
                BodyProperties seeded = BodyProperties.GetRandomBodyProperties(
                    template.Race,
                    offer.IsFemale,
                    min,
                    max,
                    0,
                    offer.FaceSeed,
                    "",
                    "",
                    "");
                return new BodyProperties(
                    new DynamicBodyProperties(offer.Age, seeded.Weight, seeded.Build),
                    seeded.StaticProperties);
            }
            catch
            {
                return BodyProperties.Default;
            }
        }

        /// <summary>
        /// Picked off the template roster by the offer's seed so the clothes are varied between
        /// wanderers but identical every time the same wanderer is drawn, preview or hero.
        /// </summary>
        public static Equipment ResolveCivilianEquipment(CharacterObject template, WandererOffer offer)
        {
            if (template == null)
                return null;
            try
            {
                List<Equipment> sets = new List<Equipment>();
                if (template.CivilianEquipments != null)
                {
                    foreach (Equipment set in template.CivilianEquipments)
                    {
                        if (set != null)
                            sets.Add(set);
                    }
                }
                if (sets.Count == 0)
                    return template.FirstCivilianEquipment;
                int seed = offer != null ? offer.FaceSeed : 0;
                int index = (seed & int.MaxValue) % sets.Count;
                return sets[index];
            }
            catch
            {
                return template.FirstCivilianEquipment;
            }
        }

        /// <summary>
        /// Stamps the looks the player was shown onto the hero they just bought.
        /// CharacterObject.UpdatePlayerCharacterBodyProperties cannot be used here: it returns
        /// early for anyone who is not the player, so the hero would silently keep the random face
        /// HeroCreator gave them and stop matching the market preview.
        /// </summary>
        public static void ApplyTo(Hero hero, CharacterObject template, WandererOffer offer)
        {
            if (hero == null || template == null || offer == null)
                return;
            try
            {
                BodyProperties body = BuildBody(template, offer);
                hero.IsFemale = offer.IsFemale;
                hero.StaticBodyProperties = body.StaticProperties;
                hero.Weight = body.Weight;
                hero.Build = body.Build;

                Equipment civilian = ResolveCivilianEquipment(template, offer);
                if (civilian != null && hero.CivilianEquipment != null)
                    hero.CivilianEquipment.FillFrom(civilian, false);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Model for the character tableau. Civilian dress is used because the player is looking
        /// at someone standing in a tavern, not on a battlefield.
        /// </summary>
        public static CharacterViewModel BuildPreview(WandererOffer offer)
        {
            CharacterObject template = ResolveTemplate(offer);
            if (template == null)
                return null;
            try
            {
                CharacterViewModel model = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                model.FillFrom(template, offer.FaceSeed);
                Equipment civilian = ResolveCivilianEquipment(template, offer);
                if (civilian != null)
                    model.SetEquipment(civilian);
                model.IsFemale = offer.IsFemale;
                model.BodyProperties = BuildBody(template, offer).ToString();
                return model;
            }
            catch
            {
                return null;
            }
        }
    }
}
