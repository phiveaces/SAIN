﻿using SPT.Reflection.Patching;
using EFT;
using HarmonyLib;
using SAIN.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using Comfort.Common;
using SAIN.Helpers;
using UnityEngine.UI;

namespace SAIN.Patches.Talk
{
    public class PlayerHurtPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "ApplyHitDebuff");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, float damage)
        {
            if (__instance?.HealthController?.IsAlive == true && 
                __instance.IsAI &&
                (!__instance.MovementContext.PhysicalConditionIs(EPhysicalCondition.OnPainkillers) || damage > 4f))
            {
                __instance.Speaker?.Play(EPhraseTrigger.OnBeingHurt, __instance.HealthStatus, true, null);
            }
        }
    }

    public class JumpPainPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "method_102");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, EPlayerState nextState)
        {
            if (nextState != EPlayerState.Jump || !__instance.IsAI)
            {
                return;
            }

            if (!__instance.MovementContext.PhysicalConditionIs(EPhysicalCondition.OnPainkillers))
            {
                if (__instance.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftLegDamaged) ||
                    __instance.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightLegDamaged))
                {
                    __instance.Say(EPhraseTrigger.OnBeingHurt, true, 0f, (ETagStatus)0, 100, false);
                }
            }
        }
    }

    public class PlayerTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "Say");
        }

        [PatchPrefix]
        public static bool PatchPrefix(Player __instance, EPhraseTrigger @event, ETagStatus mask, bool aggressive)
        {
            switch (@event)
            {
                case EPhraseTrigger.OnDeath:
                case EPhraseTrigger.OnBeingHurt:
                case EPhraseTrigger.OnAgony:
                case EPhraseTrigger.OnBreath:
                    SAINBotController.Instance?.BotHearing.PlayerTalked(@event, mask, __instance);
                    return true;

                default:
                    break;
            }

            if (__instance.IsAI)
            {
                if (SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching ||
                    SAINPlugin.IsBotExluded(__instance.AIData?.BotOwner))
                {
                    SAINBotController.Instance?.BotHearing.PlayerTalked(@event, mask, __instance);
                    return true;
                }
                return false;
            }

            SAINBotController.Instance?.BotHearing.PlayerTalked(@event, mask, __instance);
            return true;
        }
    }

    public class BotTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotTalk), "Say");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0, EPhraseTrigger type, ETagStatus? additionalMask = null)
        {
            bool skipCheck = false;
            switch (type)
            {
                case EPhraseTrigger.OnDeath:
                case EPhraseTrigger.OnBeingHurt:
                case EPhraseTrigger.OnAgony:
                case EPhraseTrigger.OnBreath:
                    skipCheck = true;
                    break;

                default:
                    break;
            }

            // If handling of bots talking is disabled, let the original method run
            if (skipCheck || 
                SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching || 
                ___botOwner_0?.HealthController?.IsAlive == false || 
                SAINPlugin.IsBotExluded(___botOwner_0))
            {
                SAINBotController.Instance?.BotHearing.PlayerTalked(type, additionalMask ?? ETagStatus.Combat, ___botOwner_0.GetPlayer);
                return true;
            }
            return false;
        }
    }

    public class BotTalkManualUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotTalk), "ManualUpdate");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            // If handling of bots talking is disabled, let the original method run
            return SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching || 
                SAINPlugin.IsBotExluded(___botOwner_0);
        }
    }
}