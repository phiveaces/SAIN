using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
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
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent
{
    public class BotComponent : MonoBehaviour
    {
        public bool BotActive => BotActivation.BotActive;
        public string ProfileId { get; private set; }
        public PersonClass Person { get; private set; }

        public float LastAimTime { get; set; }

        public Vector3 Position => PlayerComponent.Position;
        public Vector3 LookDirection => PlayerComponent.LookDirection;
        public BotOwner BotOwner => PlayerComponent.BotOwner;
        public PlayerComponent PlayerComponent => Person.PlayerComponent;
        public Player Player => PlayerComponent.Player;
        public PersonTransformClass Transform => PlayerComponent.Transform;

        public AILimitSetting CurrentAILimit => AILimit.CurrentAILimit;

        public bool HasEnemy => EnemyController.ActiveEnemy?.EnemyPerson.Active == true;
        public bool HasLastEnemy => EnemyController.LastEnemy?.EnemyPerson.Active == true;
        public Enemy Enemy => HasEnemy ? EnemyController.ActiveEnemy : null;
        public Enemy LastEnemy => HasLastEnemy ? EnemyController.LastEnemy : null;

        public Vector3? CurrentTargetPosition => CurrentTarget.CurrentTargetPosition;
        public Vector3? CurrentTargetDirection => CurrentTarget.CurrentTargetDirection;
        public float CurrentTargetDistance => CurrentTarget.CurrentTargetDistance;

        public SAINBotMedicalClass Medical { get; private set; }
        public SAINActivationClass BotActivation { get; private set; }
        public SAINDoorOpener DoorOpener { get; private set; }
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

        public Action OnSAINDisposed { get; set; }

        public CoroutineManager<BotComponent> CoroutineManager { get; private set; }

        public bool IsDead => !Person.ActiveClass.IsAlive;
        public bool GameEnding => BotActivation.GameEnding;

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

        private float getBotTotalWeight()
        {
            _slots.Clear();
            foreach (var slot in _botEquipmentSlots)
            {
                _slots.Add(Player.Equipment.GetSlot(slot));
            }
            float result = Player.Equipment.method_10(_slots);
            _slots.Clear();
           // Logger.LogWarning(result);
            return result;
        }

        private readonly List<Slot> _slots = new List<Slot>();

        public static readonly EquipmentSlot[] _botEquipmentSlots = new EquipmentSlot[]
        {
            EquipmentSlot.Backpack,
            EquipmentSlot.TacticalVest,
            EquipmentSlot.ArmorVest,
            EquipmentSlot.Eyewear,
            EquipmentSlot.FaceCover,
            EquipmentSlot.Headwear,
            EquipmentSlot.Earpiece,
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster,
            EquipmentSlot.Pockets,
        };

        public bool Init(PersonClass person)
        {
            Person = person;
            ProfileId = person.ProfileId;

            try
            {
                person.Player.ActiveHealthController.SetDamageCoeff(1f);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error Updating Player Values for bot: {ex}");
            }

            try
            {
                person.Player.InventoryControllerClass.Inventory.TotalWeight = new GClass755<float>(new Func<float>(this.getBotTotalWeight));
                person.Player.Physical.EncumberDisabled = false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error Updating Weight: {ex}");
            }

            try
            {
                NoBushESP = this.gameObject.AddComponent<SAINNoBushESP>();
                NoBushESP.Init(person.BotOwner, this);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Creating SubComponents, Disposing... : {ex}");
                return false;
            }

            try
            {
                createClasses(person);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Creating Classes, Disposing... : {ex}");
                return false;
            }

            try
            {
                initializeClasses();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Initializing Classes, Disposing... : {ex}");
                return false;
            }

            try
            {
                if (!verifyBrain(Person))
                {
                    Logger.LogError("Init SAIN ERROR, Disposing...");
                    return false;
                }

                if (EFTMath.RandomBool(1) &&
                    SAINPlugin.LoadedPreset.GlobalSettings.General.RandomSpeedHacker)
                {
                    IsSpeedHacker = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Finishing Bot Initialization, Disposing... : {ex}");
                return false;
            }

            return true;
        }

        private void createClasses(PersonClass person)
        {
            CoroutineManager = new CoroutineManager<BotComponent>(this);

            // Must be first, other classes use it
            Squad = new SAINSquadClass(this);

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
            DoorOpener = new SAINDoorOpener(this, person.BotOwner);
            Medical = new SAINBotMedicalClass(this);
            BotLight = new BotLightController(this);
            BackpackDropper = new BotBackpackDropClass(this);
            CurrentTarget = new CurrentTargetClass(this);
            ManualShoot = new ManualShootClass(this);
            BotActivation = new SAINActivationClass(this);
            Aim = new AimClass(this);
        }

        private void initializeClasses()
        {
            Info.Init();
            BotActivation.Init();

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

        public bool IsSpeedHacker { get; private set; }
        public bool SAINLayersActive => BotActivation.SAINLayersActive;

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

        private void resetBot(EBotState state)
        {
            Decision.ResetDecisions(false);
        }

        private void Update()
        {
            BotActivation.Update();

            if (!BotActive)
            {
                return;
            }
            if (BotOwner == null)
            {
                Logger.LogError("Botowner null");
                return;
            }

            EnemyController.Update();
            AILimit.Update();
            CurrentTarget.Update();
            Decision.Update();
            Info.Update();

            if (BotOwner.Mover.IsMoving || Mover.SprintController.Running)
            {
                DoorOpener.Update();
            }

            Aim.Update();
            Search.Update();
            Memory.Update();
            FriendlyFire.Update();
            Vision.Update();
            Mover.Update();
            BotStuck.Update();
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
            BotActivation.LateUpdate();
        }

        private void handleDumbShit()
        {
            if (IsSpeedHacker)
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

        private float defaultMoveSpeed;
        private float defaultSprintSpeed;

        public void Dispose()
        {
            BotActivation?.SetActive(false);
            OnSAINDisposed?.Invoke();
            CoroutineManager?.Dispose();

            try
            {
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
    }
}