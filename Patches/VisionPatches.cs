﻿using SPT.Reflection.Patching;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Reflection;
using UnityEngine;

namespace SAIN.Patches.Vision
{
    public class UpdateLightEnablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotLight), "UpdateLightEnable");
        }

        [PatchPrefix]
        public static bool PatchPrefix(
            BotOwner ___botOwner_0, 
            float curLightDist, 
            ref float __result, 
            bool ____haveLight, 
            ref float ____curLightDist, 
            ref bool ____canUseNow, 
            BotLight __instance)
        {
            __result = curLightDist;
            if (___botOwner_0.FlashGrenade.IsFlashed)
            {
                return false;
            }
            if (!____haveLight)
            {
                return false;
            }
            ____curLightDist = curLightDist;

            float timeModifier = SAINBotController.Instance.TimeVision.TimeVisionDistanceModifier;
            var lookSettings = GlobalSettingsClass.Instance.Look;
            float turnOnRatio = lookSettings.LightOnRatio;
            float turnOffRatio = lookSettings.LightOffRatio;

            bool isOn = __instance.IsEnable;
            bool wantOn = !isOn && timeModifier <= turnOnRatio && ___botOwner_0.Memory.IsPeace;
            bool wantOff = isOn && timeModifier >= turnOffRatio;
            ____canUseNow = timeModifier < turnOffRatio;

            if (wantOn)
            {
                try
                {
                    __instance.TurnOn(true);
                }
                catch (Exception e)
                {
                    if (SAINPlugin.DebugMode)
                    {
                        Logger.LogError(e);
                    }
                }
            }
            if (wantOff)
            {
                try
                {
                    __instance.TurnOff(true, true);
                }
                catch (Exception e)
                {
                    if (SAINPlugin.DebugMode)
                    {
                        Logger.LogError(e);
                    }
                }
            }

            if (__instance.IsEnable)
            {
                var gameworld = GameWorldComponent.Instance;
                if (gameworld == null)
                {
                    Logger.LogError($"GameWorldComponent is null, cannot check if bot has flashlight on!");
                    return false;
                }
                PlayerComponent playerComponent = gameworld.PlayerTracker.GetPlayerComponent(___botOwner_0.ProfileId);
                if (playerComponent == null)
                {
                    Logger.LogError($"Player Component is null, cannot check if bot has flashlight on!");
                    return false;
                }
                if (playerComponent.Flashlight.WhiteLight || 
                    (___botOwner_0.NightVision.UsingNow && playerComponent.Flashlight.IRLight))
                {
                    float min = ___botOwner_0.Settings.FileSettings.Look.VISIBLE_DISNACE_WITH_LIGHT;
                    __result = Mathf.Clamp(curLightDist, min, float.MaxValue);
                }
            }

            return false;
        }
    }

    public class UpdateLightEnablePatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotLight), "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotLight __instance)
        {
            if (!__instance.IsEnable)
            {
                return false;
            }
            float timeModifier = SAINBotController.Instance.TimeVision.TimeVisionDistanceModifier;
            float turnOffRatio = GlobalSettingsClass.Instance.Look.LightOffRatio;
            bool wantOff = timeModifier >= turnOffRatio;
            if (wantOff)
            {
                __instance.TurnOff(true, true);
            }
            return false;
        }
    }

    public class ToggleNightVisionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotNightVisionData), "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, bool ____nightVisionAtPocket, BotNightVisionData __instance)
        {
            if (___botOwner_0.FlashGrenade.IsFlashed)
            {
                return false;
            }

            float timeModifier = SAINBotController.Instance.TimeVision.TimeVisionDistanceModifier;
            var lookSettings = GlobalSettingsClass.Instance.Look;
            float turnOnRatio = lookSettings.NightVisionOnRatio;
            float turnOffRatio = lookSettings.NightVisionOffRatio;

            if (____nightVisionAtPocket)
            {
                if (timeModifier < turnOnRatio)
                {
                    __instance.method_4();
                    return false;
                }
            }
            else
            {
                if (timeModifier < turnOnRatio)
                {
                    __instance.method_5();
                }
                if (timeModifier >= turnOffRatio)
                {
                    __instance.method_1();
                }
            }
            return false;
        }
    }

    public class SetPartPriorityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "UpdatePartsByPriority");
        }

        [PatchPrefix]
        public static bool PatchPrefix(EnemyInfo __instance)
        {
            bool isAI = __instance?.Person?.IsAI == true;
            bool visible = __instance.IsVisible;

            if (isAI)
            {
                if (!__instance.HaveSeenPersonal || Time.time - __instance.TimeLastSeenReal > 5f)
                {
                    __instance.SetFarParts();
                }
                else
                {
                    __instance.SetMiddleParts();
                }
                return false;
            }

            if (!isAI && 
                SAINEnableClass.GetSAIN(__instance.Owner, out BotComponent botComponent))
            {
                Enemy enemy = botComponent.EnemyController.CheckAddEnemy(__instance.Person);
                if (enemy != null)
                {
                    if (enemy.IsCurrentEnemy)
                    {
                        __instance.SetCloseParts();
                        return false;
                    }
                    if ((enemy.Status.ShotAtMeRecently ||
                        enemy.Status.PositionalFlareEnabled))
                    {
                        __instance.SetCloseParts();
                        return false;
                    }
                }
            }

            return true;
        }
    }

    public class DisableLookUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), "CheckAllEnemies");
        }

        [PatchPrefix]
        public static bool Patch(BotOwner ____botOwner)
        {
            return SAINEnableClass.isBotExcluded(____botOwner);
        }
    }

    public class GlobalLookSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGlobalLookData), "Update");
        }

        [PatchPostfix]
        public static void Patch(BotGlobalLookData __instance)
        {
            __instance.CHECK_HEAD_ANY_DIST = true;
            __instance.MIDDLE_DIST_CAN_SHOOT_HEAD = true;
            __instance.SHOOT_FROM_EYES = false;
        }
    }

    public class WeatherTimeVisibleDistancePatch : ModulePatch
    {
        private static PropertyInfo _clearVisibleDistProperty;
        private static PropertyInfo _visibleDistProperty;
        private static PropertyInfo _HourServerProperty;

        protected override MethodBase GetTargetMethod()
        {
            _clearVisibleDistProperty = typeof(LookSensor).GetProperty("ClearVisibleDist");
            _visibleDistProperty = typeof(LookSensor).GetProperty("VisibleDist");
            _HourServerProperty = typeof(LookSensor).GetProperty("HourServer");

            return AccessTools.Method(typeof(LookSensor), "method_2");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____botOwner, ref float ____nextUpdateVisibleDist)
        {
            if (____nextUpdateVisibleDist < Time.time)
            {
                float timeMod = 1f;
                float weatherMod = 1f;

                // Checks to make sure a date and time is present
                if (____botOwner.GameDateTime != null)
                {
                    DateTime dateTime = SAINBotController.Instance.TimeVision.GameDateTime;
                    timeMod = SAINBotController.Instance.TimeVision.TimeVisionDistanceModifier;
                    // Modify the Rounding of the "HourServer" property to the hour from the DateTime object
                    _HourServerProperty.SetValue(____botOwner.LookSensor, (int)((short)dateTime.Hour));
                }
                if (SAINBotController.Instance != null)
                {
                    weatherMod = SAINBotController.Instance.WeatherVision.VisionDistanceModifier;
                    weatherMod = Mathf.Clamp(weatherMod, 0.33f, 1f);
                }

                float currentVisionDistance = ____botOwner.Settings.Current.CurrentVisibleDistance;

                // Sets a minimum cap based on weather conditions to avoid bots having too low of a vision Distance while at peace in bad weather
                float currentVisionDistanceCapped = Mathf.Clamp(currentVisionDistance * weatherMod, 80f, currentVisionDistance);

                // Applies SeenTime Modifier to the final vision Distance results
                float finalVisionDistance = currentVisionDistanceCapped * timeMod;

                _clearVisibleDistProperty.SetValue(____botOwner.LookSensor, finalVisionDistance);

                finalVisionDistance = ____botOwner.NightVision.UpdateVision(finalVisionDistance);
                finalVisionDistance = ____botOwner.BotLight.UpdateLightEnable(finalVisionDistance);
                _visibleDistProperty.SetValue(____botOwner.LookSensor, finalVisionDistance);

                ____nextUpdateVisibleDist = Time.time + (____botOwner.FlashGrenade.IsFlashed ? 3 : 20);
            }
            // Not sure what this does, but its new, so adding it here since this patch replaces the old.
            ____botOwner.BotLight.UpdateStrope();
            return false;
        }
    }

    public class NoAIESPPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner)?.GetMethod("IsEnemyLookingAtMe", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(IPlayer) }, null);
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    public class BotLightTurnOnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotLight), "TurnOn");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, ref bool ____isInDarkPlace)
        {
            if (____isInDarkPlace
                && !SAINPlugin.LoadedPreset.GlobalSettings.Flashlight.AllowLightOnForDarkBuildings)
            {
                ____isInDarkPlace = false;
            }
            if (____isInDarkPlace || ___botOwner_0.Memory.GoalEnemy != null)
            {
                return true;
            }
            if (!shallTurnLightOff(___botOwner_0.Profile.Info.Settings.Role))
            {
                return true;
            }
            ___botOwner_0.BotLight.TurnOff(false, true);
            return false;
        }

        private static bool shallTurnLightOff(WildSpawnType wildSpawnType)
        {
            FlashlightSettings settings = SAINPlugin.LoadedPreset.GlobalSettings.Flashlight;
            if (EnumValues.WildSpawn.IsScav(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemySCAV;
            }
            if (EnumValues.WildSpawn.IsPMC(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyPMC;
            }
            if (EnumValues.WildSpawn.IsGoons(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyGOONS;
            }
            if (EnumValues.WildSpawn.IsBoss(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyBOSS;
            }
            if (EnumValues.WildSpawn.IsFollower(wildSpawnType))
            {
                return settings.TurnLightOffNoEnemyFOLLOWER;
            }
            return settings.TurnLightOffNoEnemyRAIDERROGUE;
        }
    }

    public class VisionSpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "method_5");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref float __result, EnemyInfo __instance)
        {
            if (SAINEnableClass.GetSAIN(__instance?.Owner, out var sain))
            {
                Enemy enemy = sain.EnemyController.GetEnemy(__instance.Person.ProfileId, true);
                if (enemy != null)
                {
                    __result *= enemy.Vision.GainSightCoef;
                    enemy.Vision.LastGainSightResult = __result;
                }

                float minSpeed = sain.Info.FileSettings.Look.MinimumVisionSpeed;
                if (minSpeed > 0)
                {
                    __result = Mathf.Clamp(__result, minSpeed, float.MaxValue);
                }
            }
            //__result = Mathf.Clamp(__result, 0.1f, 8888f);
        }
    }

    public class VisionDistancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "CheckVisibility");
        }

        [PatchPrefix]
        public static void PatchPrefix(ref float addVisibility, EnemyInfo __instance)
        {
            if (SAINEnableClass.GetSAIN(__instance?.Owner, out var sain))
            {
                Enemy enemy = sain.EnemyController.GetEnemy(__instance.Person.ProfileId, true);
                if (enemy != null)
                {
                    addVisibility += enemy.Vision.VisionDistance;
                }
            }
        }

    }

    public class CheckFlashlightPatch : ModulePatch
    {
        private static MethodInfo _UsingLight;

        protected override MethodBase GetTargetMethod()
        {
            _UsingLight = AccessTools.PropertySetter(typeof(AIData), "UsingLight");
            return AccessTools.Method(typeof(Player.FirearmController), "SetLightsState");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player ____player)
        {
            PlayerComponent playerComponent = GameWorldComponent.Instance?.PlayerTracker.GetPlayerComponent(____player?.ProfileId);
            if (playerComponent != null)
            {
                SAINBotController.Instance.BotHearing.PlayAISound(playerComponent, SAINSoundType.GearSound, playerComponent.Player.WeaponRoot.position, 60f, 1f, true);
                var flashLight = playerComponent.Flashlight;
                flashLight.CheckDevice();

                if (!flashLight.WhiteLight && !flashLight.Laser)
                {
                    _UsingLight.Invoke(____player.AIData, new object[] { false });
                }
            }
        }
    }
}