using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.Debug;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using SAIN.SAINComponent.Classes.Memory;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes.Search;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System;
using UnityEngine;

namespace SAIN.SAINComponent
{
    public class BotComponent : BotComponentBase
    {
        public bool Initialized { get; private set; }
        public float LastAimTime { get; set; }
        public bool IsCheater { get; private set; }

        public bool BotActive => BotActivation.BotActive;
        public bool BotInStandBy => BotActivation.BotInStandBy;
        public AILimitSetting CurrentAILimit => AILimit.CurrentAILimit;

        public bool HasEnemy => EnemyController.ActiveEnemy?.EnemyPerson.Active == true;
        public bool HasLastEnemy => EnemyController.LastEnemy?.EnemyPerson.Active == true;
        public Enemy Enemy => HasEnemy ? EnemyController.ActiveEnemy : null;
        public Enemy LastEnemy => HasLastEnemy ? EnemyController.LastEnemy : null;

        public Vector3? CurrentTargetPosition => CurrentTarget.CurrentTargetPosition;
        public Vector3? CurrentTargetDirection => CurrentTarget.CurrentTargetDirection;
        public float CurrentTargetDistance => CurrentTarget.CurrentTargetDistance;

        public BotWeightManagement WeightManagement { get; private set; }
        public SAINBotMedicalClass Medical { get; private set; }
        public SAINActivationClass BotActivation { get; private set; }
        public DoorOpener DoorOpener { get; private set; }
        public ManualShootClass ManualShoot { get; private set; }
        public CurrentTargetClass CurrentTarget { get; private set; }
        public BotBackpackDropClass BackpackDropper { get; private set; }
        public BotLightController BotLight { get; private set; }
        public SAINBotSpaceAwareness SpaceAwareness { get; private set; }
        public AimDownSightsController AimDownSightsController { get; private set; }
        public SAINAILimit AILimit { get; private set; }
        public SAINBotSuppressClass Suppression { get; private set; }
        public SAINVaultClass Vault { get; private set; }
        public SAINSearchClass Search { get; private set; }
        public SAINMemoryClass Memory { get; private set; }
        public SAINEnemyController EnemyController { get; private set; }
        public SAINNoBushESP NoBushESP { get; private set; }
        public SAINFriendlyFireClass FriendlyFire { get; private set; }
        public SAINVisionClass Vision { get; private set; }
        public SAINMoverClass Mover { get; private set; }
        public SAINBotUnstuckClass BotStuck { get; private set; }
        public FlashLightClass FlashLight => PlayerComponent.Flashlight;
        public SAINHearingSensorClass Hearing { get; private set; }
        public SAINBotTalkClass Talk { get; private set; }
        public SAINDecisionClass Decision { get; private set; }
        public SAINCoverClass Cover { get; private set; }
        public SAINBotInfoClass Info { get; private set; }
        public SAINSquadClass Squad { get; private set; }
        public SAINSelfActionClass SelfActions { get; private set; }
        public SAINBotGrenadeClass Grenade { get; private set; }
        public SAINSteeringClass Steering { get; private set; }
        public AimClass Aim { get; private set; }
        public CoroutineManager<BotComponent> CoroutineManager { get; private set; }

        public bool IsDead => !Person.ActiveClass.IsAlive;
        public bool GameEnding => BotActivation.GameEnding;
        public bool SAINLayersActive => BotActivation.SAINLayersActive;

        public float DistanceToAimTarget
        {
            get
            {
                if (BotOwner.AimingData != null)
                {
                    return BotOwner.AimingData.LastDist2Target;
                }
                return CurrentTarget.CurrentTargetDistance;
            }
        }

        public float LastCheckVisibleTime;

        public ESAINLayer ActiveLayer
        {
            get
            {
                return BotActivation.ActiveLayer;
            }
            set
            {
                BotActivation.SetActiveLayer(value);
            }
        }

        public bool InitializeBot(PersonClass person)
        {
            if (base.Init(person) == false)
            {
                return false;
            }

            if (createClasses(person) == false)
            {
                return false;
            }

            if (initClasses() == false)
            {
                return false;
            }

            if (finishInit() == false)
            {
                return false;
            }

            Initialized = true;
            return true;
        }

        private bool createClasses(PersonClass person)
        {
            try
            {
                CoroutineManager = new CoroutineManager<BotComponent>(this);

                // Must be first, other classes use it
                Squad = new SAINSquadClass(this);

                WeightManagement = new BotWeightManagement(this);
                NoBushESP = this.gameObject.AddComponent<SAINNoBushESP>();
                Info = new SAINBotInfoClass(this);
                Memory = new SAINMemoryClass(this);
                BotStuck = new SAINBotUnstuckClass(this);
                Hearing = new SAINHearingSensorClass(this);
                Talk = new SAINBotTalkClass(this);
                Decision = new SAINDecisionClass(this);
                Cover = new SAINCoverClass(this);
                SelfActions = new SAINSelfActionClass(this);
                Steering = new SAINSteeringClass(this);
                Grenade = new SAINBotGrenadeClass(this);
                Mover = new SAINMoverClass(this);
                EnemyController = new SAINEnemyController(this);
                FriendlyFire = new SAINFriendlyFireClass(this);
                Vision = new SAINVisionClass(this);
                Search = new SAINSearchClass(this);
                Vault = new SAINVaultClass(this);
                Suppression = new SAINBotSuppressClass(this);
                AILimit = new SAINAILimit(this);
                AimDownSightsController = new AimDownSightsController(this);
                SpaceAwareness = new SAINBotSpaceAwareness(this);
                DoorOpener = new DoorOpener(this);
                Medical = new SAINBotMedicalClass(this);
                BotLight = new BotLightController(this);
                BackpackDropper = new BotBackpackDropClass(this);
                CurrentTarget = new CurrentTargetClass(this);
                ManualShoot = new ManualShootClass(this);
                BotActivation = new SAINActivationClass(this);
                Aim = new AimClass(this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Creating Classes, Disposing... : {ex}");
                return false;
            }
            return true;
        }

        private bool initClasses()
        {
            try
            {
                Info.Init();
                BotActivation.Init();
                NoBushESP.Init(Person.BotOwner, this);
                WeightManagement.Init();
                Search.Init();
                Memory.Init();
                EnemyController.Init();
                FriendlyFire.Init();
                Vision.Init();
                Mover.Init();
                BotStuck.Init();
                Hearing.Init();
                Talk.Init();
                Decision.Init();
                Cover.Init();
                Squad.Init();
                SelfActions.Init();
                Grenade.Init();
                Steering.Init();
                Vault.Init();
                Suppression.Init();
                AILimit.Init();
                AimDownSightsController.Init();
                SpaceAwareness.Init();
                Medical.Init();
                BotLight.Init();
                BackpackDropper.Init();
                Aim.Init();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Initializing Classes, Disposing... : {ex}");
                return false;
            }
            return true;
        }

        private bool finishInit()
        {
            try
            {
                if (!verifyBrain(Person))
                {
                    Logger.LogError("Init SAIN ERROR, Disposing...");
                    return false;
                }

                var settings = GlobalSettingsClass.Instance.General;
                if (settings.RandomCheaters &&
                    EFTMath.RandomBool(settings.RandomCheaterChance))
                {
                    IsCheater = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Finishing Bot Initialization, Disposing... : {ex}");
                return false;
            }
            return true;
        }

        private bool verifyBrain(PersonClass person)
        {
            if (Info.Profile.IsPMC &&
                person.BotOwner.Brain.BaseBrain.ShortName() != Brain.PMC.ToString())
            {
                Logger.LogAndNotifyError($"{BotOwner.name} is a PMC but does not have [PMC] Base Brain! Current Brain Assignment: [{person.BotOwner.Brain.BaseBrain.ShortName()}] : SAIN Server mod is either missing or another mod is overwriting it. Destroying SAIN for this bot...");
                return false;
            }
            return true;
        }

        private void OnDisable()
        {
            BotActivation.SetActive(false);
            StopAllCoroutines();
        }

        private void Update()
        {
            if (!Initialized)
            {
                return;
            }

            BotActivation.Update();
            if (!BotActive)
            {
                return;
            }

            EnemyController.Update();
            AILimit.Update();
            CurrentTarget.Update();
            BotStuck.Update();
            Decision.Update();

            if (BotInStandBy)
            {
                return;
            }

            Info.Update();
            WeightManagement.Update();
            DoorOpener.Update();
            Aim.Update();
            Search.Update();
            Memory.Update();
            FriendlyFire.Update();
            Vision.Update();
            Mover.Update();
            Hearing.Update();
            Talk.Update();
            Cover.Update();
            Squad.Update();
            SelfActions.Update();
            Grenade.Update();
            Steering.Update();
            Vault.Update();
            Suppression.Update();
            AimDownSightsController.Update();
            SpaceAwareness.Update();
            Medical.Update();
            BotLight.Update();
            ManualShoot.Update();

            handleDumbShit();
        }

        private void LateUpdate()
        {
            if (!Initialized)
            {
                return;
            }
            BotActivation.LateUpdate();
        }

        private void handleDumbShit()
        {
            if (IsCheater)
            {
                if (defaultMoveSpeed == 0)
                {
                    defaultMoveSpeed = Player.MovementContext.MaxSpeed;
                    defaultSprintSpeed = Player.MovementContext.SprintSpeed;
                }
                Player.Grounder.enabled = Enemy == null;
                if (Enemy != null)
                {
                    Player.MovementContext.SetCharacterMovementSpeed(350, true);
                    Player.MovementContext.SprintSpeed = 50f;
                    Player.ChangeSpeed(100f);
                    Player.UpdateSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    Player.MovementContext.ChangeSpeedLimit(100f, Player.ESpeedLimit.SurfaceNormal);
                    BotOwner.SetTargetMoveSpeed(100f);
                }
                else
                {
                    Player.MovementContext.SetCharacterMovementSpeed(defaultMoveSpeed, false);
                    Player.MovementContext.SprintSpeed = defaultSprintSpeed;
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            OnDisable();

            try
            {
                CoroutineManager?.Dispose();
                Search?.Dispose();
                Memory?.Dispose();
                EnemyController?.Dispose();
                FriendlyFire?.Dispose();
                Vision?.Dispose();
                Mover?.Dispose();
                BotStuck?.Dispose();
                Hearing?.Dispose();
                Talk?.Dispose();
                Decision?.Dispose();
                Cover?.Dispose();
                Info?.Dispose();
                Squad?.Dispose();
                SelfActions?.Dispose();
                Grenade?.Dispose();
                Steering?.Dispose();
                Enemy?.Dispose();
                Vault?.Dispose();
                Suppression?.Dispose();
                AILimit?.Dispose();
                AimDownSightsController?.Dispose();
                SpaceAwareness?.Dispose();
                Medical?.Dispose();
                BotLight?.Dispose();
                BackpackDropper?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose Classes Error: {ex}");
            }

            try
            {
                GameObject.Destroy(NoBushESP);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Dispose Components Error: {ex}");
            }

            if (BotOwner != null)
            {
                BotOwner.OnBotStateChange -= resetBot;
            }
            Destroy(this);
        }

        private void resetBot(EBotState state)
        {
            Decision.ResetDecisions(false);
        }

        private float defaultMoveSpeed;
        private float defaultSprintSpeed;
    }
}