using HarmonyLib;
using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class GeneralSettings : SAINSettingsBase<GeneralSettings>, ISAINSettings
    {
        [Name("Bots Use Grenades")]
        public bool BotsUseGrenades = true;

        [Name("SAIN Door Handling")]
        [Description("WIP")]
        public bool NewDoorOpening = true;

        [Name("No Door Animations")]
        [Description("Bots auto open doors instead of getting stuck in an animation, if fika is loaded, this is ignored and it is always disabled.")]
        public bool NoDoorAnimations = true;

        [Name("Always Push Open Doors")]
        [Description("Only applies if No Door Animations is set to on. Bots will always push open doors to avoid getting stuck. Can cause cursed looking doors sometimes, but greatly improves their ability to navigate.")]
        public bool InvertDoors = true;

        [Name("Disable All Doors")]
        [Description("Doors are hard, just turn them all off. Only targets doors that can be open/closed normally.")]
        public bool DisableAllDoors = false;

        [Name("Bot Weight Effects")]
        [Description("Bots are properly affected by the weight of their equipment and loot. Requires raid restart for existing bots, as it applies on bot creation.")]
        public bool BotWeightEffects = true;

        [Name("Random Cheater AI - Joke")]
        [Description("Emulate the real Live-Like experience! 1% of bots will be a cheater. They will move faster than they should, have 0 recoil, and perfect aim, always shoot full auto at any range if their weapon supports it, and always fire as fast as possible if they have a semi-auto weapon.")]
        public bool RandomCheaters = false;

        [Name("Random Speed Hacker Chance - Joke")]
        [Description("If for some reason you enabled random cheaters, this is the chance they will be assigned as one.")]
        [Percentage]
        public float RandomCheaterChance = 1f;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINCombatSquadLayerPriority = 22;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINExtractLayerPriority = 24;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [MinMax(0, 100)]
        [Hidden]
        [JsonIgnore]
        public int SAINCombatSoloLayerPriority = 20;

        [JsonIgnore]
        [Hidden]
        public float SprintReachDistance = 1f;

        [JsonIgnore]
        [Hidden]
        public float BaseReachDistance = 0.5f;
    }
}