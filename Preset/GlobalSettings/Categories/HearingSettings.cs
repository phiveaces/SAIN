using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.SAINComponent.Classes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class HearingSettings : SAINSettingsBase<HearingSettings>, ISAINSettings
    {
        [Name("Max Footstep Audio Distance")]
        [Description("The Maximum Range that a bot can hear footsteps, sprinting, and jumping, turning, gear sounds, and any movement related sounds, in meters.")]
        [MinMax(10f, 150f, 1f)]
        public float MaxFootstepAudioDistance = 70f;

        [Name("Max Footstep Audio Distance without Headphones")]
        [Description("The Maximum Range that a bot can hear footsteps, sprinting, and jumping, turning, gear sounds, and any movement related sounds, in meters when not wearing headphones.")]
        [MinMax(10f, 150f, 1f)]
        public float MaxFootstepAudioDistanceNoHeadphones = 50f;

        [Hidden]
        [JsonIgnore]
        public DispersionDictionary DispersionValues = new DispersionDictionary
        {
            {
                ESoundDispersionType.Footstep, new DispersionValues
                {
                    DistanceModifier = 1f,
                    MinAngle = 1f,
                    MaxAngle = 45,
                    VerticalModifier = 0f
                }
            },
            {
                ESoundDispersionType.HeardShot, new DispersionValues
                {
                    DistanceModifier = 1f,
                    MinAngle = 1f,
                    MaxAngle = 30,
                    VerticalModifier = 0f
                }
            },
            {
                ESoundDispersionType.UnheardShot, new DispersionValues
                {
                    DistanceModifier = 1f,
                    MinAngle = 5f,
                    MaxAngle = 120,
                    VerticalModifier = 0f
                }
            },
            {
                ESoundDispersionType.HeardSuppressedShot, new DispersionValues
                {
                    DistanceModifier = 1f,
                    MinAngle = 1f,
                    MaxAngle = 45,
                    VerticalModifier = 0f
                }
            },
            {
                ESoundDispersionType.UnheardSuppressedShot, new DispersionValues
                {
                    DistanceModifier = 1f,
                    MinAngle = 15,
                    MaxAngle = 160,
                    VerticalModifier = 0f
                }
            },
        };

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Looting = 60f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_MovementTurnSkid = 30f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_GrenadePinDraw = 35f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Prone = 50f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Healing = 40f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Reload = 30f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_Surgery = 55f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_DryFire = 10f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float MaxSoundRange_FallLanding = 70;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_AimingandGearRattle = 35f;

        [MinMax(1f, 150f, 100f)]
        [Advanced]
        public float BaseSoundRange_EatDrink = 40f;

        [MinMax(1f, 150f, 100f)]
        public float MaxRangeToReportEnemyActionNoHeadset = 50f;

        [Name("Hearing Delay / Reaction Time with Active Enemy")]
        [MinMax(0.0f, 1f, 100f)]
        public float BaseHearingDelayWithEnemy = 0.2f;

        [Name("Hearing Delay / Reaction Time while At Peace")]
        [MinMax(0.0f, 1f, 100f)]
        public float BaseHearingDelayAtPeace = 0.35f;

        [Name("Global Gunshot Audible Range Multiplier")]
        [MinMax(0.1f, 2f, 100f)]
        public float GunshotAudioMultiplier = 1f;

        [Name("Global Footstep Audible Range Multiplier")]
        [MinMax(0.1f, 2f, 100f)]
        public float FootstepAudioMultiplier = 1f;

        [Name("Suppressed Sound Modifier")]
        [Description("Audible Gun Range is multiplied by this number when using a suppressor")]
        [MinMax(0.1f, 0.95f, 100f)]
        public float SuppressorModifier = 0.6f;

        [Name("Subsonic Sound Modifier")]
        [Description("Audible Gun Range is multiplied by this number when using a suppressor and subsonic ammo")]
        [MinMax(0.1f, 0.95f, 100f)]
        public float SubsonicModifier = 0.33f;

        [Name("Hearing Distances by Ammo Type")]
        [Description("How far a bot can hear a gunshot when fired from each specific caliber listed here.")]
        [MinMax(30f, 400f, 10f)]
        [Advanced]
        [DefaultDictionary(nameof(HearingDistancesDefaults))]
        public Dictionary<ECaliber, float> HearingDistances = new Dictionary<ECaliber, float>
        {
            { ECaliber.Caliber9x18PM, 110f },
            { ECaliber.Caliber9x19PARA, 110f },
            { ECaliber.Caliber46x30, 120f },
            { ECaliber.Caliber9x21, 120f },
            { ECaliber.Caliber57x28, 120f },
            { ECaliber.Caliber762x25TT, 120f },
            { ECaliber.Caliber1143x23ACP, 115f },
            { ECaliber.Caliber9x33R, 125 },
            { ECaliber.Caliber545x39, 160 },
            { ECaliber.Caliber556x45NATO, 160 },
            { ECaliber.Caliber9x39, 160 },
            { ECaliber.Caliber762x35, 175 },
            { ECaliber.Caliber762x39, 175 },
            { ECaliber.Caliber366TKM, 175 },
            { ECaliber.Caliber762x51, 200f },
            { ECaliber.Caliber127x55, 200f },
            { ECaliber.Caliber762x54R, 225f },
            { ECaliber.Caliber86x70, 250f },
            { ECaliber.Caliber20g, 185 },
            { ECaliber.Caliber12g, 185 },
            { ECaliber.Caliber23x75, 210 },
            { ECaliber.Caliber26x75, 50 },
            { ECaliber.Caliber30x29, 50 },
            { ECaliber.Caliber40x46, 50 },
            { ECaliber.Caliber40mmRU, 50 },
            { ECaliber.Caliber127x108, 300 },
            { ECaliber.Caliber68x51, 200f },
            { ECaliber.Default, 125 },
        };

        [JsonIgnore]
        [Hidden]
        public static readonly Dictionary<ECaliber, float> HearingDistancesDefaults = new Dictionary<ECaliber, float>()
        {
            { ECaliber.Caliber9x18PM, 110f },
            { ECaliber.Caliber9x19PARA, 110f },
            { ECaliber.Caliber46x30, 120f },
            { ECaliber.Caliber9x21, 120f },
            { ECaliber.Caliber57x28, 120f },
            { ECaliber.Caliber762x25TT, 120f },
            { ECaliber.Caliber1143x23ACP, 115f },
            { ECaliber.Caliber9x33R, 125 },
            { ECaliber.Caliber545x39, 160 },
            { ECaliber.Caliber556x45NATO, 160 },
            { ECaliber.Caliber9x39, 160 },
            { ECaliber.Caliber762x35, 175 },
            { ECaliber.Caliber762x39, 175 },
            { ECaliber.Caliber366TKM, 175 },
            { ECaliber.Caliber762x51, 200f },
            { ECaliber.Caliber127x55, 200f },
            { ECaliber.Caliber762x54R, 225f },
            { ECaliber.Caliber86x70, 250f },
            { ECaliber.Caliber20g, 185 },
            { ECaliber.Caliber12g, 185 },
            { ECaliber.Caliber23x75, 210 },
            { ECaliber.Caliber26x75, 50 },
            { ECaliber.Caliber30x29, 50 },
            { ECaliber.Caliber40x46, 50 },
            { ECaliber.Caliber40mmRU, 50 },
            { ECaliber.Caliber127x108, 300 },
            { ECaliber.Caliber68x51, 200f },
            { ECaliber.Default, 125 },
        };
    }
}