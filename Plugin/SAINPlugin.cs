using SPT.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using DrakiaXYZ.VersionChecker;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Patches.Generic;
using SAIN.Patches.Movement;
using SAIN.Patches.Vision;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static SAIN.AssemblyInfoClass;

namespace SAIN
{
    [BepInPlugin(SAINGUID, SAINName, SAINVersion)]
    [BepInDependency(BigBrainGUID, BigBrainVersion)]
    [BepInDependency(WaypointsGUID, WaypointsVersion)]
    [BepInDependency(SPTGUID, SPTVersion)]
    [BepInProcess(EscapeFromTarkov)]
    [BepInIncompatibility("com.dvize.BushNoESP")]
    [BepInIncompatibility("com.dvize.NoGrenadeESP")]
    public class SAINPlugin : BaseUnityPlugin
    {
        public static DebugSettings DebugSettings => LoadedPreset.GlobalSettings.Debug;
        public static bool DebugMode => DebugSettings.GlobalDebugMode;
        public static bool DrawDebugGizmos => DebugSettings.DrawDebugGizmos;
        public static PresetEditorDefaults EditorDefaults => PresetHandler.EditorDefaults;

        public static CombatDecision ForceSoloDecision = CombatDecision.None;

        public static SquadDecision ForceSquadDecision = SquadDecision.None;

        public static SelfDecision ForceSelfDecision = SelfDecision.None;

        private void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            PresetHandler.Init();
            BindConfigs();
            InitPatches();
            BigBrainHandler.Init();
            Vector.Init();
        }

        private void BindConfigs()
        {
            string category = "SAIN Editor";

            NextDebugOverlay = Config.Bind(category, "Next Debug Overlay", new KeyboardShortcut(KeyCode.LeftBracket), "Change The Debug Overlay with DrakiaXYZs Debug Overlay");
            PreviousDebugOverlay = Config.Bind(category, "Previous Debug Overlay", new KeyboardShortcut(KeyCode.RightBracket), "Change The Debug Overlay with DrakiaXYZs Debug Overlay");

            OpenEditorButton = Config.Bind(category, "Open Editor", false, "Opens the Editor on press");
            OpenEditorConfigEntry = Config.Bind(category, "Open Editor Shortcut", new KeyboardShortcut(KeyCode.F6), "The keyboard shortcut that toggles editor");
        }

        public static ConfigEntry<KeyboardShortcut> NextDebugOverlay { get; private set; }
        public static ConfigEntry<KeyboardShortcut> PreviousDebugOverlay { get; private set; }
        public static ConfigEntry<bool> OpenEditorButton { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OpenEditorConfigEntry { get; private set; }

        private List<Type> patches => new List<Type>() {

                typeof(Patches.Generic.StopRefillMagsPatch),
                typeof(Patches.Generic.SetEnvironmentPatch),
                typeof(Patches.Generic.SetPanicPointPatch),
                typeof(Patches.Generic.AddPointToSearchPatch),
                typeof(Patches.Generic.TurnDamnLightOffPatch),
                typeof(Patches.Generic.GrenadeThrownActionPatch),
                typeof(Patches.Generic.GrenadeExplosionActionPatch),
                typeof(Patches.Generic.ShallKnowEnemyPatch),
                typeof(Patches.Generic.ShallKnowEnemyLatePatch),
                typeof(Patches.Generic.HaveSeenEnemyPatch),

                //typeof(Patches.Generic.Fixes.HealCancelPatch),
                typeof(Patches.Generic.Fixes.StopSetToNavMeshPatch),
                typeof(Patches.Generic.Fixes.FightShallReloadFixPatch),
                typeof(Patches.Generic.Fixes.BotMemoryAddEnemyPatch),
                typeof(Patches.Generic.Fixes.BotGroupAddEnemyPatch),
                //typeof(Patches.Generic.Fixes.NoTeleportPatch),
                typeof(Patches.Generic.Fixes.FixItemTakerPatch),
                typeof(Patches.Generic.Fixes.FixItemTakerPatch2),
                typeof(Patches.Generic.Fixes.FixPatrolDataPatch),
                typeof(Patches.Generic.Fixes.RotateClampPatch),

                typeof(Patches.Movement.EncumberedPatch),
                typeof(Patches.Movement.DoorOpenerPatch),
                typeof(Patches.Movement.DoorDisabledPatch),
                typeof(Patches.Movement.CrawlPatch),
                typeof(Patches.Movement.CrawlPatch2),

                typeof(Patches.Hearing.TryPlayShootSoundPatch),
                typeof(Patches.Hearing.OnMakingShotPatch),
                typeof(Patches.Hearing.HearingSensorPatch),

                typeof(Patches.Hearing.ToggleSoundPatch),
                typeof(Patches.Hearing.SpawnInHandsSoundPatch),
                typeof(Patches.Hearing.PlaySwitchHeadlightSoundPatch),
                typeof(Patches.Hearing.BulletImpactPatch),
                typeof(Patches.Hearing.TreeSoundPatch),
                typeof(Patches.Hearing.DoorBreachSoundPatch),
                typeof(Patches.Hearing.DoorOpenSoundPatch),
                typeof(Patches.Hearing.FootstepSoundPatch),
                typeof(Patches.Hearing.SprintSoundPatch),
                typeof(Patches.Hearing.GenericMovementSoundPatch),
                typeof(Patches.Hearing.JumpSoundPatch),
                typeof(Patches.Hearing.DryShotPatch),
                typeof(Patches.Hearing.ProneSoundPatch),
                typeof(Patches.Hearing.SoundClipNameCheckerPatch),
                typeof(Patches.Hearing.AimSoundPatch),
                typeof(Patches.Hearing.LootingSoundPatch),
                typeof(Patches.Hearing.SetInHandsGrenadePatch),
                typeof(Patches.Hearing.SetInHandsFoodPatch),
                typeof(Patches.Hearing.SetInHandsMedsPatch),

                typeof(Patches.Talk.JumpPainPatch),
                typeof(Patches.Talk.PlayerHurtPatch),
                typeof(Patches.Talk.PlayerTalkPatch),
                typeof(Patches.Talk.BotTalkPatch),
                typeof(Patches.Talk.BotTalkManualUpdatePatch),

                typeof(Patches.Vision.DisableLookUpdatePatch),
                typeof(Patches.Vision.UpdateLightEnablePatch),
                typeof(Patches.Vision.UpdateLightEnablePatch2),
                typeof(Patches.Vision.ToggleNightVisionPatch),
                typeof(Patches.Vision.SetPartPriorityPatch),
                typeof(Patches.Vision.GlobalLookSettingsPatch),
                typeof(Patches.Vision.WeatherTimeVisibleDistancePatch),
                typeof(Patches.Vision.NoAIESPPatch),
                typeof(Patches.Vision.BotLightTurnOnPatch),
                typeof(Patches.Vision.VisionSpeedPatch),
                typeof(Patches.Vision.VisionDistancePatch),
                typeof(Patches.Vision.CheckFlashlightPatch),

                typeof(Patches.Shoot.Aim.AimOffsetPatch),
                typeof(Patches.Shoot.Aim.AimTimePatch),
                typeof(Patches.Shoot.Aim.ScatterPatch),
                //typeof(Patches.Shoot.Aim.WeaponPresetPatch),
                typeof(Patches.Shoot.Aim.ForceNoHeadAimPatch),
                typeof(Patches.Shoot.Aim.AimRotateSpeedPatch),

                typeof(Patches.Shoot.RateOfFire.FullAutoPatch),
                typeof(Patches.Shoot.RateOfFire.SemiAutoPatch),
                typeof(Patches.Shoot.RateOfFire.SemiAutoPatch2),
                typeof(Patches.Shoot.RateOfFire.SemiAutoPatch3),

                typeof(Patches.Components.AddComponentPatch),
                typeof(Patches.Components.AddGameWorldPatch),
                typeof(Patches.Components.GetBotController),
                typeof(Patches.Components.GetBotSpawner),
            };

        private void InitPatches()
        {
            // Reflection go brrrrrrrrrrrrrr
            MethodInfo enableMethod = AccessTools.Method(typeof(ModulePatch), "Enable");
            foreach (var patch in patches)
            {
                if (!typeof(ModulePatch).IsAssignableFrom(patch))
                {
                    Logger.LogError($"Type {patch.Name} is not a ModulePatch");
                    continue;
                }

                try
                {
                    enableMethod.Invoke(Activator.CreateInstance(patch), null);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        public static SAINPresetClass LoadedPreset => PresetHandler.LoadedPreset;
        public static SAINBotController BotController => SAINBotController.Instance;

        private void Update()
        {
            ModDetection.Update();
            SAINEditor.Update();
        }

        private void Start() => SAINEditor.Init();

        private void LateUpdate() => SAINEditor.LateUpdate();

        private void OnGUI() => SAINEditor.OnGUI();

        public static bool IsBotExluded(BotOwner botOwner) => SAINEnableClass.IsSAINDisabledForBot(botOwner);
    }
}
