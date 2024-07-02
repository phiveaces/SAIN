using SPT.Reflection.Patching;
using EFT;
using HarmonyLib;
using System.Reflection;
using System;
using SAIN.SAINComponent;
using SAIN.Components.BotController;
using UnityEngine;
using SAIN.Components.PlayerComponentSpace;
using UnityEngine.UIElements;
using SAIN.Components;

namespace SAIN.Patches.Components
{
    internal class AddComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "method_10");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref BotOwner __instance)
        {
            try
            {
                if (__instance.BotState != EBotState.ActiveFail)
                {
                    BotSpawnController.Instance.AddBot(__instance);
                }
                else
                {
                    Logger.LogDebug($"{__instance.name} failed EFT Init, skipping adding SAIN components");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($" SAIN Add Bot Error: {ex}");
            }
        }
    }

    internal class AddGameWorldPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorldUnityTickListener), "Create");
        }

        [PatchPostfix]
        public static void PatchPostfix(GameObject gameObject)
        {
            try
            {
                GameWorldHandler.Create(gameObject);
            }
            catch (Exception ex)
            {
                Logger.LogError($" SAIN Init Gameworld Error: {ex}");
            }
        }
    }

    internal class GetBotController : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "method_0");
        }

        [PatchPrefix]
        public static void PatchPrefix(BotsController __instance)
        {
            var controller = SAINBotController.Instance;
            if (controller != null && controller.DefaultController == null)
            {
                controller.DefaultController = __instance;
            }
        }
    }

    internal class GetBotSpawner : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "AddPlayer");
        }

        [PatchPostfix]
        public static void PatchPostfix(BotSpawner __instance)
        {
            var controller = SAINBotController.Instance;
            if (controller != null && controller.BotSpawner == null)
            {
                controller.BotSpawner = __instance;
            }
        }
    }
}
