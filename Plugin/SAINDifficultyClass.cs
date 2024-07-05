using Comfort.Common;
using SAIN.Helpers;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System.Collections.Generic;
using UnityEngine;
using static HBAO_Core;

namespace SAIN.Plugin
{
    internal static class SAINDifficultyClass
    {
        const string PresetNameEasy = "Baby Bots";
        const string PresetNameNormal = "Less Difficult";
        const string PresetNameHard = "Default";
        const string PresetNameHarderPMCs = "Default with Harder PMCs";
        const string DefaultPresetDescription = "Bots are difficult but fair, the way SAIN was meant to played.";
        const string PresetNameVeryHard = "I Like Pain";
        const string PresetNameImpossible = "Death Wish";

        public static readonly Dictionary<SAINDifficulty, SAINPresetDefinition> DefaultPresetDefinitions = new Dictionary<SAINDifficulty, SAINPresetDefinition>();

        static SAINDifficultyClass()
        {
            DefaultPresetDefinitions.Add(
                SAINDifficulty.easy,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameEasy,
                    SAINDifficulty.easy,
                    "Bots react slowly and are incredibly inaccurate."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.lesshard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameNormal,
                    SAINDifficulty.lesshard,
                    "Bots react more slowly, and are less accurate than usual."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.hard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameHard,
                    SAINDifficulty.hard,
                    DefaultPresetDescription));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.harderpmcs, 
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameHarderPMCs,
                    SAINDifficulty.harderpmcs,
                    "Default Settings, but PMCs are harder than normal."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.veryhard,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameVeryHard,
                    SAINDifficulty.veryhard,
                    "Bots react faster, are more accurate, and can see further."));

            DefaultPresetDefinitions.Add(
                SAINDifficulty.deathwish,
                SAINPresetDefinition.CreateDefaultDefinition(
                    PresetNameImpossible,
                    SAINDifficulty.deathwish,
                    "Prepare To Die. Bots have almost no scatter, get less recoil from their weapon while shooting, are more accurate, and react deadly fast."));
        }

        public static SAINPresetClass GetDefaultPreset(SAINDifficulty difficulty)
        {
            SAINPresetClass result;
            switch (difficulty)
            {
                case SAINDifficulty.easy:
                    result = SAINDifficultyClass.CreateEasyPreset();
                    break;

                case SAINDifficulty.lesshard:
                    result = SAINDifficultyClass.CreateNormalPreset();
                    break;

                case SAINDifficulty.hard:
                    result = SAINDifficultyClass.CreateHardPreset();
                    break;

                case SAINDifficulty.harderpmcs:
                    result = SAINDifficultyClass.CreateHarderPMCsPreset();
                    break;

                case SAINDifficulty.veryhard:
                    result = SAINDifficultyClass.CreateVeryHardPreset();
                    break;

                case SAINDifficulty.deathwish:
                    result = SAINDifficultyClass.CreateImpossiblePreset();
                    break;

                default:
                    return null;
            }
            return result;
        }

        private static SAINPresetClass CreateEasyPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.easy);

            var global = preset.GlobalSettings;
            global.Shoot.RecoilMultiplier = 2.0f;
            global.Shoot.GlobalScatterMultiplier = 1.5f;
            global.Aiming.AccuracySpreadMultiGlobal = 2f;
            global.Aiming.FasterCQBReactionsGlobal = false;
            global.Look.GlobalVisionDistanceMultiplier = 0.66f;
            global.Look.GlobalVisionSpeedModifier = 1.75f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 0.5f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 120f;
                    setting.Value.Shoot.FireratMulti *= 0.6f;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateNormalPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.lesshard);

            var global = preset.GlobalSettings;
            global.Shoot.RecoilMultiplier = 1.6f;
            global.Shoot.GlobalScatterMultiplier = 1.2f;
            global.Aiming.AccuracySpreadMultiGlobal = 1.5f;
            global.Aiming.FasterCQBReactionsGlobal = false;
            global.Look.GlobalVisionDistanceMultiplier = 0.85f;
            global.Look.GlobalVisionSpeedModifier = 1.25f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 0.85f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 150f;
                    setting.Value.Shoot.FireratMulti *= 0.8f;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateHardPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.hard);
            return preset;
        }

        private static SAINPresetClass CreateHarderPMCsPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.harderpmcs);
            ApplyHarderPMCs(preset);
            return preset;
        }

        private static void ApplyHarderPMCs(SAINPresetClass preset)
        {
            var botSettings = preset.BotSettings;
            foreach (var botsetting in botSettings.SAINSettings)
            {
                if (botsetting.Key == EnumValues.WildSpawn.Usec || botsetting.Key == EnumValues.WildSpawn.Bear)
                {
                    var pmcSettings = botsetting.Value.Settings;

                    // Set for all difficulties
                    foreach (var diff in pmcSettings.Values)
                    {
                        diff.Aiming.BASE_HIT_AFFECTION_MIN_ANG = 1f;
                        diff.Aiming.BASE_HIT_AFFECTION_MAX_ANG = 3f;
                        diff.Core.ScatteringPerMeter = 0.035f;
                        diff.Core.ScatteringClosePerMeter = 0.125f;
                        diff.Core.GainSightCoef -= 0.005f;
                        diff.Mind.WeaponProficiency = 0.7f;
                        diff.Scattering.ScatterMultiplier = 0.85f;
                    }

                    var easy = pmcSettings[BotDifficulty.easy];
                    easy.Aiming.FasterCQBReactionsDistance = 20f;
                    easy.Aiming.FasterCQBReactionsMinimum = 0.3f;
                    easy.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.35f;
                    easy.Aiming.MAX_AIM_TIME = 1.5f;
                    easy.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.65f;
                    easy.Core.VisibleDistance = 200f;

                    var normal = pmcSettings[BotDifficulty.normal];
                    normal.Aiming.FasterCQBReactionsDistance = 35f;
                    normal.Aiming.FasterCQBReactionsMinimum = 0.25f;
                    normal.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.4f;
                    normal.Aiming.MAX_AIM_TIME = 1.35f;
                    normal.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.5f;
                    normal.Core.VisibleDistance = 225f;

                    var hard = pmcSettings[BotDifficulty.hard];
                    hard.Aiming.FasterCQBReactionsDistance = 50f;
                    hard.Aiming.FasterCQBReactionsMinimum = 0.2f;
                    hard.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.2f;
                    hard.Aiming.MAX_AIM_TIME = 1.15f;
                    hard.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.35f;
                    hard.Core.VisibleDistance = 250f;

                    var impossible = pmcSettings[BotDifficulty.impossible];
                    impossible.Aiming.FasterCQBReactionsDistance = 60f;
                    impossible.Aiming.FasterCQBReactionsMinimum = 0.15f;
                    impossible.Aiming.MAX_AIMING_UPGRADE_BY_TIME = 0.15f;
                    impossible.Aiming.MAX_AIM_TIME = 1.0f;
                    impossible.Aiming.BASE_HIT_AFFECTION_DELAY_SEC = 0.25f;
                    impossible.Core.VisibleDistance = 275f;
                }
            }
        }

        private static SAINPresetClass CreateVeryHardPreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.veryhard);

            var global = preset.GlobalSettings;
            global.Shoot.RecoilMultiplier = 0.66f;
            global.Shoot.GlobalScatterMultiplier = 0.85f;
            global.Aiming.AccuracySpreadMultiGlobal = 0.8f;
            global.Aiming.AimCenterMassGlobal = false;
            global.Look.GlobalVisionDistanceMultiplier = 1.33f;
            global.Look.GlobalVisionSpeedModifier = 0.8f;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Clamp(bot.Value.DifficultyModifier * 1.33f, 0.01f, 1f).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 170f;
                    setting.Value.Shoot.FireratMulti *= 1.2f;
                    //setting.Value.Core.VisibleDistance *= 1.25f;
                }
            }
            return preset;
        }

        private static SAINPresetClass CreateImpossiblePreset()
        {
            var preset = new SAINPresetClass(SAINDifficulty.deathwish);

            var global = preset.GlobalSettings;
            global.Shoot.RecoilMultiplier = 0.25f;
            global.Shoot.GlobalScatterMultiplier = 0.01f;
            global.Aiming.AccuracySpreadMultiGlobal = 0.33f;
            global.Look.GlobalVisionDistanceMultiplier = 2f;
            global.Look.GlobalVisionSpeedModifier = 0.65f;
            global.Aiming.AimCenterMassGlobal = false;
            global.Look.NotLookingToggle = false;

            foreach (var bot in preset.BotSettings.SAINSettings)
            {
                bot.Value.DifficultyModifier = Mathf.Sqrt(bot.Value.DifficultyModifier).Round100();
                foreach (var setting in bot.Value.Settings)
                {
                    setting.Value.Core.VisibleAngle = 180f;
                    setting.Value.Shoot.FireratMulti *= 2f;
                    //setting.Value.Core.VisibleDistance *= 1.25f;
                }
            }
            return preset;
        }
    }
}