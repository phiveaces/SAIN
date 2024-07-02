using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using StaminaClass = BackendConfigSettingsClass.GClass1376;
using BotMovementControllerClass = GClass422;
using PlayerPhysicalClass = GClass681;

namespace SAIN.Patches.Movement
{
    public class CrawlPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMovementControllerClass), "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotMovementControllerClass __instance, BotOwner ___botOwner_0, Vector3 pos, bool slowAtTheEnd, bool getUpWithCheck)
        {
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return true;
            }
            if (___botOwner_0.BotLay.IsLay &&
                getUpWithCheck)
            {
                Vector3 vector = pos - ___botOwner_0.Position;
                if (vector.y < 0.5f)
                {
                    vector.y = 0f;
                }
                if (vector.sqrMagnitude > 0.2f)
                {
                    ___botOwner_0.BotLay.GetUp(getUpWithCheck);
                }
                if (___botOwner_0.BotLay.IsLay)
                {
                    return false;
                }
            }
            ___botOwner_0.WeaponManager.Stationary.StartMove();
            __instance.SlowAtTheEnd = slowAtTheEnd;
            return true;
        }
    }

    public class CrawlPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), "DoProne");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, bool val)
        {
            if (!val)
            {
                return true;
            }
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return true;
            }
            ___botOwner_0.GetPlayer.MovementContext.IsInPronePose = true;
            return false;
        }
    }

    public class EncumberedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PlayerPhysicalClass), "UpdateWeightLimits");
        }

        [PatchPrefix]
        public static bool PatchPrefix(bool ___bool_7, PlayerPhysicalClass.IObserverToPlayerBridge ___iobserverToPlayerBridge_0, PlayerPhysicalClass __instance)
        {
            if (___bool_7)
            {
                return true;
            }

            IPlayer player = ___iobserverToPlayerBridge_0.iPlayer;
            if (player == null)
            {
                Logger.LogWarning($"Player is Null, can't set weight limits for AI.");
                return true;
            }

            bool isAI = player?.IsAI == true;
            if (!isAI)
            {
                return true;
            }

            if (SAINPlugin.IsBotExluded(player.AIData.BotOwner))
            {
                return true;
            }

            StaminaClass stamina = Singleton<BackendConfigSettingsClass>.Instance.Stamina;

            float carryWeightModifier = ___iobserverToPlayerBridge_0.Skills.CarryingWeightRelativeModifier;
            float d = carryWeightModifier * carryWeightModifier;

            float absoluteWeightModifier = ___iobserverToPlayerBridge_0.iPlayer.HealthController.CarryingWeightAbsoluteModifier;
            Vector2 b = new Vector2(absoluteWeightModifier, absoluteWeightModifier);

            BackendConfigSettingsClass.InertiaSettings inertia = Singleton<BackendConfigSettingsClass>.Instance.Inertia;
            float strength = (float)___iobserverToPlayerBridge_0.Skills.Strength.SummaryLevel;
            Vector3 b2 = new Vector3(inertia.InertiaLimitsStep * strength, inertia.InertiaLimitsStep * strength, 0f);

            //Logger.LogDebug($"Strength {strength}");
            //Logger.LogDebug($"carryWeightModifier {carryWeightModifier}");
            //Logger.LogDebug($"absoluteWeightModifier {absoluteWeightModifier}");
            //Logger.LogDebug($"d {d} : b {b.magnitude} : b2 {b2.magnitude}");

            __instance.BaseInertiaLimits = inertia.InertiaLimits + b2;
            __instance.WalkOverweightLimits = stamina.WalkOverweightLimits * d + b;
            __instance.BaseOverweightLimits = stamina.BaseOverweightLimits * d + b;
            __instance.SprintOverweightLimits = stamina.SprintOverweightLimits * d + b;
            __instance.WalkSpeedOverweightLimits = stamina.WalkSpeedOverweightLimits * d + b;

            //Logger.LogDebug($"BaseInertiaLimits {__instance.BaseInertiaLimits.magnitude}");
            //Logger.LogDebug($"WalkOverweightLimits {__instance.WalkOverweightLimits.magnitude}");
            //Logger.LogDebug($"BaseOverweightLimits {__instance.BaseOverweightLimits.magnitude}");
            //Logger.LogDebug($"SprintOverweightLimits {__instance.SprintOverweightLimits.magnitude}");
            //Logger.LogDebug($"WalkSpeedOverweightLimits {__instance.WalkSpeedOverweightLimits.magnitude}");

            return false;
        }
    }

    public class DoorOpenerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotDoorOpener), nameof(BotDoorOpener.Update));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ____owner, ref bool __result)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.General.NewDoorOpening)
            {
                return true;
            }
            if (SAINPlugin.IsBotExluded(____owner))
            {
                return true;
            }

            if (SAINEnableClass.GetSAIN(____owner, out var botComponent) &&
                botComponent.HasEnemy)
            {
                __result = botComponent.DoorOpener.Update();
                return false;
            }
            return true;
        }
    }
}