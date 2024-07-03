using Newtonsoft.Json;
using SAIN;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class ShootSettings : SAINSettingsBase<ShootSettings>, ISAINSettings
    {
        [Name("Global Scatter Multiplier")]
        [Description("Higher = more scattering. Modifies EFT's default scatter feature. 1.5 = 1.5x more scatter")]
        [MinMax(0.01f, 10f, 100f)]
        public float GlobalScatterMultiplier = 1f;

        [Name("Global Recoil Multiplier")]
        [Description("Higher = more recoil. Modifies SAIN's recoil scatter feature. 1.5 = 1.5x more recoilfrom a single gunshot")]
        [MinMax(0.01f, 3f, 100f)]
        public float RecoilMultiplier = 1f;

        [Name("Add or Subtract Recoil")]
        [Description("Linearly add or subtract from the final recoil result")]
        [MinMax(-20f, 20f, 100f)]
        [Advanced]
        public float AddRecoil = 5f;

        [Name("Recoil Decay Coefficient")]
        [Description("Controls the speed that bots will recover from a weapon's recoil. Higher = faster decay")]
        [MinMax(0.01f, 5f, 100f)]
        [Advanced]
        public float RecoilDecayCoef = 1;

        [Name("Recoil Barrel Rise Coefficient")]
        [Description("Controls the speed that a bot's weapon will rise after a shot occurs.  Higher = faster application")]
        [MinMax(0.01f, 20f, 100f)]
        [Advanced]
        public float RecoilRiseCoef = 8;

        [Name("Ammo Shootability" )]
        [Description(
            "Lower is BETTER. " +
            "How Shootable this ammo type is, affects semi auto firerate and full auto burst length." +
            "Value is scaled but roughly gives a plus or minus 20% to firerate depending on the value set here." +
            "For Example. 9x19 will shoot about 20% faster fire-rate on semi-auto at 50 meters" +
            ", and fire 20% longer bursts when on full auto"
            )]
        [Percentage0to1(0.01f)]
        [Advanced]
        [DefaultDictionary(nameof(AmmoCaliberShootabilityDefaults))]
        public Dictionary<ECaliber, float> AmmoCaliberShootability = new Dictionary<ECaliber, float>()
        {
            { ECaliber.Caliber9x18PM, 0.225f },
            { ECaliber.Caliber9x19PARA, 0.275f },
            { ECaliber.Caliber46x30, 0.325f },
            { ECaliber.Caliber9x21, 0.325f },
            { ECaliber.Caliber57x28, 0.35f },
            { ECaliber.Caliber762x25TT, 0.425f },
            { ECaliber.Caliber1143x23ACP, 0.425f },
            { ECaliber.Caliber9x33R, 0.65f },
            { ECaliber.Caliber545x39, 0.525f },
            { ECaliber.Caliber556x45NATO, 0.525f },
            { ECaliber.Caliber9x39, 0.6f },
            { ECaliber.Caliber762x35, 0.575f },
            { ECaliber.Caliber762x39, 0.675f },
            { ECaliber.Caliber366TKM, 0.675f },
            { ECaliber.Caliber762x51, 0.725f },
            { ECaliber.Caliber127x55, 0.775f },
            { ECaliber.Caliber762x54R, 0.85f },
            { ECaliber.Caliber86x70, 1.0f },
            { ECaliber.Caliber20g, 0.7f },
            { ECaliber.Caliber12g, 0.725f },
            { ECaliber.Caliber23x75, 0.85f },
            { ECaliber.Caliber26x75, 1f },
            { ECaliber.Caliber30x29, 1f },
            { ECaliber.Caliber40x46, 1f },
            { ECaliber.Caliber40mmRU, 1f },
            { ECaliber.Caliber127x108, 0.5f },
            { ECaliber.Caliber68x51, 0.6f },
            { ECaliber.Default, 0.5f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ECaliber, float> AmmoCaliberShootabilityDefaults = new Dictionary<ECaliber, float>()
        {
            { ECaliber.Caliber9x18PM, 0.225f },
            { ECaliber.Caliber9x19PARA, 0.275f },
            { ECaliber.Caliber46x30, 0.325f },
            { ECaliber.Caliber9x21, 0.325f },
            { ECaliber.Caliber57x28, 0.35f },
            { ECaliber.Caliber762x25TT, 0.425f },
            { ECaliber.Caliber1143x23ACP, 0.425f },
            { ECaliber.Caliber9x33R, 0.65f },
            { ECaliber.Caliber545x39, 0.525f },
            { ECaliber.Caliber556x45NATO, 0.525f },
            { ECaliber.Caliber9x39, 0.6f },
            { ECaliber.Caliber762x35, 0.575f },
            { ECaliber.Caliber762x39, 0.675f },
            { ECaliber.Caliber366TKM, 0.675f },
            { ECaliber.Caliber762x51, 0.725f },
            { ECaliber.Caliber127x55, 0.775f },
            { ECaliber.Caliber762x54R, 0.85f },
            { ECaliber.Caliber86x70, 1.0f },
            { ECaliber.Caliber20g, 0.7f },
            { ECaliber.Caliber12g, 0.725f },
            { ECaliber.Caliber23x75, 0.85f },
            { ECaliber.Caliber26x75, 1f },
            { ECaliber.Caliber30x29, 1f },
            { ECaliber.Caliber40x46, 1f },
            { ECaliber.Caliber40mmRU, 1f },
            { ECaliber.Caliber127x108, 0.5f },
            { ECaliber.Caliber68x51, 0.6f },
            { ECaliber.Default, 0.5f },
        };

        [Name("Max FullAuto Distances")]
        [Description("The maximum distance a bot using this caliber can fire it full auto. Not all values are used since some calibers don't have any full auto weapons that use it.")]
        [MinMax(10f, 150f)]
        [Advanced]
        [DefaultDictionary(nameof(AmmoCaliberFullAutoMaxDistancesDefaults))]
        public Dictionary<ECaliber, float> AmmoCaliberFullAutoMaxDistances = new Dictionary<ECaliber, float>
        {
            { ECaliber.Caliber9x18PM, 80f },
            { ECaliber.Caliber9x19PARA, 80f },
            { ECaliber.Caliber46x30, 70f },
            { ECaliber.Caliber9x21, 70f },
            { ECaliber.Caliber57x28, 70f },
            { ECaliber.Caliber762x25TT, 70f },
            { ECaliber.Caliber1143x23ACP, 75f },
            { ECaliber.Caliber9x33R, 75f },
            { ECaliber.Caliber545x39, 65f },
            { ECaliber.Caliber556x45NATO, 65f },
            { ECaliber.Caliber9x39, 60f },
            { ECaliber.Caliber762x35, 55f },
            { ECaliber.Caliber762x39, 50f },
            { ECaliber.Caliber366TKM, 45f },
            { ECaliber.Caliber762x51, 45f },
            { ECaliber.Caliber127x55, 50f },
            { ECaliber.Caliber762x54R, 50f },
            { ECaliber.Caliber86x70, 40f },
            { ECaliber.Caliber20g, 30f },
            { ECaliber.Caliber12g, 30f },
            { ECaliber.Caliber23x75, 30f },
            { ECaliber.Caliber26x75, 30f },
            { ECaliber.Caliber30x29, 30f },
            { ECaliber.Caliber40x46, 30f },
            { ECaliber.Caliber40mmRU, 30f },
            { ECaliber.Caliber127x108, 30f },
            { ECaliber.Caliber68x51, 50f },
            { ECaliber.Default, 55f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ECaliber, float> AmmoCaliberFullAutoMaxDistancesDefaults = new Dictionary<ECaliber, float>()
        {
            { ECaliber.Caliber9x18PM, 80f },
            { ECaliber.Caliber9x19PARA, 80f },
            { ECaliber.Caliber46x30, 70f },
            { ECaliber.Caliber9x21, 70f },
            { ECaliber.Caliber57x28, 70f },
            { ECaliber.Caliber762x25TT, 70f },
            { ECaliber.Caliber1143x23ACP, 75f },
            { ECaliber.Caliber9x33R, 75f },
            { ECaliber.Caliber545x39, 65f },
            { ECaliber.Caliber556x45NATO, 65f },
            { ECaliber.Caliber9x39, 60f },
            { ECaliber.Caliber762x35, 55f },
            { ECaliber.Caliber762x39, 50f },
            { ECaliber.Caliber366TKM, 45f },
            { ECaliber.Caliber762x51, 45f },
            { ECaliber.Caliber127x55, 50f },
            { ECaliber.Caliber762x54R, 50f },
            { ECaliber.Caliber86x70, 40f },
            { ECaliber.Caliber20g, 30f },
            { ECaliber.Caliber12g, 30f },
            { ECaliber.Caliber23x75, 30f },
            { ECaliber.Caliber26x75, 30f },
            { ECaliber.Caliber30x29, 30f },
            { ECaliber.Caliber40x46, 30f },
            { ECaliber.Caliber40mmRU, 30f },
            { ECaliber.Caliber127x108, 30f },
            { ECaliber.Caliber68x51, 50f },
            { ECaliber.Default, 55f },
        };

        [Name("Weapon Shootability")]
        [Description(
            "Lower is BETTER. " +
            "How Shootable this weapon type is, affects semi auto firerate and full auto burst length." +
            "Value is scaled but roughly gives a plus or minus 20% to firerate depending on the value set here." +
            "For Example. SMGs will shoot about 20% faster fire-rate on semi-auto at 50 meters" +
            ", and fire 20% longer bursts when on full auto"
            )]
        [Percentage0to1(0.01f)]
        [Advanced]
        [DefaultDictionary(nameof(WeaponClassShootabilityDefaults))]
        public Dictionary<EWeaponClass, float> WeaponClassShootability = new Dictionary<EWeaponClass, float>
        {
            { EWeaponClass.Default, 0.425f },
            { EWeaponClass.assaultCarbine, 0.5f },
            { EWeaponClass.assaultRifle, 0.5f },
            { EWeaponClass.machinegun, 0.15f },
            { EWeaponClass.smg, 0.25f },
            { EWeaponClass.pistol, 0.4f },
            { EWeaponClass.marksmanRifle, 0.75f },
            { EWeaponClass.sniperRifle, 1f },
            { EWeaponClass.shotgun, 0.75f },
            { EWeaponClass.grenadeLauncher, 1f },
            { EWeaponClass.specialWeapon, 1f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<EWeaponClass, float> WeaponClassShootabilityDefaults = new Dictionary<EWeaponClass, float>()
        {
            { EWeaponClass.Default, 0.425f },
            { EWeaponClass.assaultCarbine, 0.5f },
            { EWeaponClass.assaultRifle, 0.5f },
            { EWeaponClass.machinegun, 0.15f },
            { EWeaponClass.smg, 0.25f },
            { EWeaponClass.pistol, 0.4f },
            { EWeaponClass.marksmanRifle, 0.75f },
            { EWeaponClass.sniperRifle, 1f },
            { EWeaponClass.shotgun, 0.75f },
            { EWeaponClass.grenadeLauncher, 1f },
            { EWeaponClass.specialWeapon, 1f },
        };

        [Name("Weapon Firerate Wait Time")]
        [Description(
            "HIGHER is BETTER. " +
            "This is the time to wait inbetween shots for every meter." +
            "the number is divided by the distance to their target, to get a wait period between shots." +
            "For Example. With a setting of 100: " +
            "if a target is 50m away, they will wait 0.5 sec between shots because 50 / 100 is 0.5." +
            "This number is later modified by the Shootability multiplier, to get a final fire-rate that gets sent to a bot."
            )]
        [MinMax(30f, 250f, 1f)]
        [Advanced]
        [DefaultDictionary(nameof(WeaponPerMeterDefaults))]
        public Dictionary<EWeaponClass, float> WeaponPerMeter = new Dictionary<EWeaponClass, float>
        {
            { EWeaponClass.Default, 120f },
            { EWeaponClass.assaultCarbine, 140 },
            { EWeaponClass.assaultRifle, 130 },
            { EWeaponClass.machinegun, 135 },
            { EWeaponClass.smg, 160 },
            { EWeaponClass.pistol, 65 },
            { EWeaponClass.marksmanRifle, 75 },
            { EWeaponClass.sniperRifle, 50 },
            { EWeaponClass.shotgun, 60 },
            { EWeaponClass.grenadeLauncher, 75 },
            { EWeaponClass.specialWeapon, 80 },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<EWeaponClass, float> WeaponPerMeterDefaults = new Dictionary<EWeaponClass, float>()
        {
            { EWeaponClass.Default, 120f },
            { EWeaponClass.assaultCarbine, 140 },
            { EWeaponClass.assaultRifle, 130 },
            { EWeaponClass.machinegun, 135 },
            { EWeaponClass.smg, 160 },
            { EWeaponClass.pistol, 65 },
            { EWeaponClass.marksmanRifle, 75 },
            { EWeaponClass.sniperRifle, 50 },
            { EWeaponClass.shotgun, 60 },
            { EWeaponClass.grenadeLauncher, 75 },
            { EWeaponClass.specialWeapon, 80 },
        };

        [Name("Bot Preferred Shoot Distances")]
        [Description(
            "The distances that a bot prefers to shoot a particular weapon class. " +
            "Bots will try to close the distance if further than this."
            )]
        [MinMax(10f, 250f, 1f)]
        [Advanced]
        [DefaultDictionary(nameof(EngagementDistanceDefaults))]
        public Dictionary<EWeaponClass, float> EngagementDistance = new Dictionary<EWeaponClass, float>
        {
            { EWeaponClass.Default, 125f },
            { EWeaponClass.assaultCarbine, 125f },
            { EWeaponClass.assaultRifle, 150f },
            { EWeaponClass.machinegun, 125f },
            { EWeaponClass.smg, 70f },
            { EWeaponClass.pistol, 50f },
            { EWeaponClass.marksmanRifle, 175f },
            { EWeaponClass.sniperRifle, 300f },
            { EWeaponClass.shotgun, 50f },
            { EWeaponClass.grenadeLauncher, 100f },
            { EWeaponClass.specialWeapon, 100f },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<EWeaponClass, float> EngagementDistanceDefaults = new Dictionary<EWeaponClass, float>()
        {
            { EWeaponClass.Default, 125f },
            { EWeaponClass.assaultCarbine, 125f },
            { EWeaponClass.assaultRifle, 150f },
            { EWeaponClass.machinegun, 125f },
            { EWeaponClass.smg, 70f },
            { EWeaponClass.pistol, 50f },
            { EWeaponClass.marksmanRifle, 175f },
            { EWeaponClass.sniperRifle, 300f },
            { EWeaponClass.shotgun, 50f },
            { EWeaponClass.grenadeLauncher, 100f },
            { EWeaponClass.specialWeapon, 100f },
        };

        [JsonIgnore]
        [Hidden]
        private const string Shootability = "Affects Weapon Shootability Calculations. ";

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float WeaponClassScaling = 0.25f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float RecoilScaling = 0.35f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float ErgoScaling = 0.08f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float AmmoCaliberScaling = 0.3f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float WeaponProficiencyScaling = 0.3f;

        [Description(Shootability)]
        [Advanced]
        [Percentage01to99]
        public float DifficultyScaling = 0.3f;
    }
}