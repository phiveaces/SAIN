using SPT.Reflection.Patching;
using Audio.Data;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.Helpers;
using System.Reflection;
using Systems.Effects;
using UnityEngine;

namespace SAIN.Patches.Hearing
{
    public class GrenadeCollisionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Grenade), "OnCollisionHandler");
        }

        [PatchPostfix]
        public static void Patch(Grenade __instance, SoundBank ___soundBank_0)
        {
            float range = ___soundBank_0 != null ? ___soundBank_0.Rolloff : 20f;
            SAINBotController.Instance?.GrenadeCollided(__instance, range);
        }
    }

    public class TreeSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TreeInteractive), "method_0");
        }

        [PatchPostfix]
        public static void Patch(Vector3 soundPosition, BetterSource source, IPlayerOwner player, SoundBank ____soundBank)
        {
            if (player.iPlayer != null)
            {
                float baseRange = 50f;
                if (____soundBank != null)
                {
                    baseRange = ____soundBank.Rolloff * player.SoundRadius;
                }
                //Logger.LogDebug($"Playing Bush Sound Range: {baseRange}");
                SAINBotController.Instance?.BotHearing.PlayAISound(player.iPlayer.ProfileId, SAINSoundType.Bush, soundPosition, baseRange, 1f);
            }
        }
    }

    public class DoorOpenSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "StartInteraction");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player ____player)
        {
            float baseRange = 40f;
            SAINBotController.Instance?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.Door, ____player.Position, baseRange, 1f);
        }
    }

    public class DoorBreachSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "PlayBreachSound");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player ____player)
        {
            float baseRange = 70f;
            SAINBotController.Instance?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.Door, ____player.Position, baseRange, 1f);
        }
    }

    public class JumpSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "method_2");
        }

        [PatchPrefix]
        public static bool PatchPrefix(Player ____player, ref float ____nextJumpNoise)
        {
            if (____nextJumpNoise < Time.time)
            {
                ____nextJumpNoise = Time.time + 0.5f;
                float baseRange = 55f;
                SAINBotController.Instance?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.Jump, ____player.Position, baseRange, 1f);
            }
            return false;
        }
    }

    public class FootstepSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "PlayStepSound");
        }

        [PatchPostfix]
        public static void Patch(Player __instance, BetterSource ___NestedStepSoundSource)
        {
            float volume = calcVolume(__instance);
            float range = ___NestedStepSoundSource.MaxDistance;
            SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.FootStep, __instance.Position, range, volume);
        }

        private static float calcVolume(Player player)
        {
            return player.MovementContext.CovertMovementVolumeBySpeed * player.method_49();
        }

        private static float calcVolumeOld(Player player)
        {
            float num = player.Speed;
            if (player.IsSprintEnabled)
            {
                num = 2f;
            }
            float num2 = Mathf.Clamp(0.5f * player.PoseLevel + 0.5f, 0f, 1f);
            num *= num2;
            float num3 = player.IsSprintEnabled ? 1f : player.MovementContext.CovertMovementVolumeBySpeed;
            float volume = (num3 + num) / 2f;
            return volume;
        }
    }

    public class SprintSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MovementContext), "method_1");
        }

        [PatchPrefix]
        public static bool PatchPrefix(Player ____player, Vector3 motion, MovementContext __instance, ref float ____nextStepNoise)
        {
            if (____nextStepNoise < Time.time && ____player.IsSprintEnabled)
            {
                ____nextStepNoise = Time.time + 0.33f;

                if (motion.y < 0.2f && motion.y > -0.2f)
                {
                    motion.y = 0f;
                }
                if (motion.sqrMagnitude < 1E-06f)
                {
                    return false;
                }

                float num = 2f;
                float num2 = Mathf.Clamp(0.5f * ____player.PoseLevel + 0.5f, 0f, 1f);
                num *= num2;
                float volume = (1f + num) / 2f;
                float baseRange = 45f;
                SAINBotController.Instance?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.Sprint, ____player.Position, baseRange, volume);
            }
            return false;
        }
    }

    public class GenericMovementSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "DefaultPlay");
        }

        [PatchPostfix]
        public static void Patch(Player __instance, SoundBank bank, float volume, EAudioMovementState movementState)
        {
            SAINSoundType soundType;
            switch (movementState)
            {
                case EAudioMovementState.Sprint:
                    soundType = SAINSoundType.Sprint;
                    break;

                //case EAudioMovementState.Run:
                //    soundType = SAINSoundType.FootStep;
                //    break;

                case EAudioMovementState.Stop:
                    soundType = SAINSoundType.TurnSound;
                    break;

                case EAudioMovementState.None:
                case EAudioMovementState.Land:
                    soundType = SAINSoundType.Land;
                    break;

                default:
                    soundType = SAINSoundType.Generic;
                    return;
            }

            SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, soundType, __instance.Position, bank.Rolloff, volume);
        }
    }

    public class DryShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController), "DryShot");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player ____player)
        {
            float baseRange = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_DryFire;
            SAINBotController.Instance?.BotHearing.PlayAISound(____player.ProfileId, SAINSoundType.DryFire, ____player.WeaponRoot.position, baseRange, 1f);
        }
    }

    public class HearingSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotHearingSensor), "method_0");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ____botOwner)
        {
            if (!SAINPlugin.IsBotExluded(____botOwner))
            {
                return false;
            }
            if (____botOwner == null || ____botOwner.GetPlayer == null)
            {
                return false;
            }
            return true;
        }
    }

    public class TryPlayShootSoundPatch : ModulePatch
    {
        private static PropertyInfo AIFlareEnabled;

        protected override MethodBase GetTargetMethod()
        {
            AIFlareEnabled = AccessTools.Property(typeof(AIData), "Boolean_0");
            return AccessTools.Method(typeof(AIData), "TryPlayShootSound");
        }

        [PatchPrefix]
        public static bool PatchPrefix(AIData __instance)
        {
            //if (__instance.IsAI && 
            //    SAINPlugin.IsBotExluded(__instance.BotOwner))
            //{
            //    return true;
            //}
            AIFlareEnabled.SetValue(__instance, true);
            return false;
        }
    }

    public class OnMakingShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "OnMakingShot");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance)
        {
            SAINBotController.Instance?.BotHearing.PlayShootSound(__instance.ProfileId);
            if (__instance.IsAI && SAINEnableClass.GetSAIN(__instance, out var sain))
            {
                sain.Info.WeaponInfo.Recoil.WeaponShot();
            }
        }
    }

    public class SoundClipNameCheckerPatch : ModulePatch
    {
        private static MethodInfo _Player;
        private static FieldInfo _PlayerBridge;

        protected override MethodBase GetTargetMethod()
        {
            _PlayerBridge = AccessTools.Field(typeof(BaseSoundPlayer), "playersBridge");
            _Player = AccessTools.PropertyGetter(_PlayerBridge.FieldType, "iPlayer");
            return AccessTools.Method(typeof(BaseSoundPlayer), "SoundEventHandler");
        }

        [PatchPrefix]
        public static void PatchPrefix(string soundName, BaseSoundPlayer __instance)
        {
            if (SAINBotController.Instance != null)
            {
                object playerBridge = _PlayerBridge.GetValue(__instance);
                Player player = _Player.Invoke(playerBridge, null) as Player;
                SAINSoundTypeHandler.AISoundFileChecker(soundName, player);
            }
        }
    }

    public class ToggleSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "PlayToggleSound");
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance, bool previousState, bool isOn, Vector3 ___SpeechLocalPosition)
        {
            if (previousState != isOn)
            {
                float baseRange = 5f;
                SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GearSound, __instance.Position + ___SpeechLocalPosition, baseRange, 1f);
            }
        }
    }

    public class SpawnInHandsSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "SpawnInHands");
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance, Item item)
        {
            AudioClip itemClip = Singleton<GUISounds>.Instance.GetItemClip(item.ItemSound, EInventorySoundType.pickup);
            if (itemClip != null)
            {
                SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GearSound, __instance.Position, 30f, 1f);
            }
        }
    }

    public class PlaySwitchHeadlightSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "PlaySwitchHeadlightSound");
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance, Vector3 ___SpeechLocalPosition)
        {
            SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GearSound, __instance.Position + ___SpeechLocalPosition, 5f, 1f);
        }
    }

    public class LootingSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "method_43");
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance, BetterSource ____searchSource)
        {
            if (____searchSource == null)
            {
                return;
            }
            float baseRange = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Looting;
            SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.Looting, __instance.Position, ____searchSource.MaxDistance, 1f);
        }
    }

    public class ProneSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "PlaySoundBank");
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance, ref string soundBank)
        {
            if (soundBank == "Prone"
                && __instance.SinceLastStep >= 0.5f
                && __instance.CheckSurface())
            {
                float range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Prone;
                SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.Prone, __instance.Position, range, 1f);
            }
        }
    }

    public class AimSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "method_50");
        }

        [PatchPrefix]
        public static void PatchPrefix(float volume, Player __instance)
        {
            float baseRange = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_AimingandGearRattle;
            SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GearSound, __instance.Position, baseRange, volume);
        }
    }

    public class SetInHandsGrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "SetInHands",
                new[] { typeof(GrenadeClass), typeof(Callback<IHandsThrowController>) });
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance)
        {
            float range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_GrenadePinDraw;
            SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.GrenadeDraw, __instance.Position, range, 1f);
        }
    }

    public class SetInHandsFoodPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "SetInHands",
                new[] { typeof(FoodClass), typeof(float), typeof(int), typeof(Callback<GInterface142>) });
        }

        [PatchPrefix]
        public static void PatchPrefix(Player __instance)
        {
            float range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_EatDrink;
            SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, SAINSoundType.Food, __instance.Position, range, 1f);
        }
    }

    public class SetInHandsMedsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "SetInHands",
                new[] { typeof(MedsClass), typeof(EBodyPart), typeof(int), typeof(Callback<GInterface142>) });
        }

        [PatchPrefix]
        public static void PatchPrefix(MedsClass meds, Player __instance)
        {
            SAINSoundType soundType;
            float range;
            if (meds != null && meds.HealthEffectsComponent.AffectsAny(new EDamageEffectType[] { EDamageEffectType.DestroyedPart }))
            {
                soundType = SAINSoundType.Surgery;
                range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Surgery;
            }
            else
            {
                soundType = SAINSoundType.Heal;
                range = SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Healing;
            }
            SAINBotController.Instance?.BotHearing.PlayAISound(__instance.ProfileId, soundType, __instance.Position, range, 1f);
        }
    }

    public class BulletImpactPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EffectsCommutator), "PlayHitEffect");
        }

        [PatchPostfix]
        public static void PatchPostfix(EftBulletClass info)
        {
            if (SAINBotController.Instance != null)
            {
                //Vector3 position = __instance.transform.position + ___vector3_0;
                SAINBotController.Instance.BulletImpacted(info);
            }
        }
    }
}