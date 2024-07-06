using EFT;
using EFT.InventoryLogic;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class ShootClass : BaseNodeAbstractClass
    {
        public ShootClass(BotOwner owner) : base(owner)
        {
            Shoot = new BotShoot(owner);
        }

        private readonly BotShoot Shoot;
        private BotOwner BotOwner => botOwner_0;

        private BotComponent Bot
        {
            get
            {
                if (_bot == null)
                {
                    if (SAINBotController.Instance?.GetSAIN(BotOwner, out var bot) == true)
                    {
                        _bot = bot;
                    }
                    else if (BotOwner.TryGetComponent(out bot))
                    {
                        _bot = bot;
                    }
                }
                return _bot;
            }
        }

        private BotComponent _bot;

        private float changeAimTimer;

        public override void Update()
        {
            if (BotOwner == null || Bot == null)
            {
                return;
            }

            if (BotOwner.WeaponManager.Selector.EquipmentSlot == EquipmentSlot.Holster
                && !BotOwner.WeaponManager.HaveBullets
                && !BotOwner.WeaponManager.Selector.TryChangeToMain())
            {
                selectWeapon();
            }

            if (!Bot.Aim.CanAim)
            {
                return;
            }

            if (changeAimTimer < Time.time)
            {
                changeAimTimer = Time.time + 0.5f;
                Bot.AimDownSightsController.UpdateADSstatus();
            }

            Bot.BotLight.HandleLightForEnemy();

            aimAtEnemy();
        }

        public void AllowUnpauseMove(bool value)
        {
            _shallUnpause = value;
        }

        private bool _shallUnpause = true;

        private bool tryPauseForShoot(bool shallUnpause)
        {
            if (ShallPauseForShoot())
            {
                if (_nextPauseMoveTime < Time.time &&
                    !IsMovementPaused)
                {
                    _nextPauseMoveTime = Time.time + Random.Range(_pauseMoveFrequencyMin, _pauseMoveFrequencyMax);
                    Bot.Mover.PauseMovement(Random.Range(_pauseMoveDurationMin, _pauseMoveDurationMax));
                }
                if (!IsMovementPaused)
                {
                    BotOwner.AimingData?.LoseTarget();
                    return false;
                }
            }
            else if (IsMovementPaused && _shallUnpause)
            {
                //BotOwner.Mover.MovementResume();
            }
            return true;
        }

        public bool ShallPauseForShoot()
        {
            float maxPointFireDist = Bot.Info.FileSettings.Shoot.MaxPointFireDistance;
            return
                Bot.Enemy != null &&
                Bot.Enemy.RealDistance > maxPointFireDist &&
                IsAiming;
        }

        public bool IsAiming
        {
            get
            {
                return BotOwner?.ShootData?.ShootController?.IsAiming == true || BotOwner?.AimingData?.IsReady == true;
            }
        }

        public bool IsMovementPaused => BotOwner?.Mover.Pause == true;

        private float _nextPauseMoveTime;
        private float _pauseMoveFrequencyMin = 2f;
        private float _pauseMoveFrequencyMax = 4f;
        private float _pauseMoveDurationMin = 0.5f;
        private float _pauseMoveDurationMax = 1f;

        private void selectWeapon()
        {
            EquipmentSlot optimalSlot = findOptimalWeaponForDistance(getDistance());
            if (currentSlot != optimalSlot)
            {
                tryChangeWeapon(optimalSlot);
            }
        }

        private EquipmentSlot currentSlot => BotOwner.WeaponManager.Selector.EquipmentSlot;

        private void tryChangeWeapon(EquipmentSlot slot)
        {
            if (_nextChangeWeaponTime < Time.time)
            {
                var selector = BotOwner?.WeaponManager?.Selector;
                if (selector != null)
                {
                    _nextChangeWeaponTime = Time.time + 1f;
                    switch (slot)
                    {
                        case EquipmentSlot.FirstPrimaryWeapon:
                            selector.TryChangeToMain();
                            break;

                        case EquipmentSlot.SecondPrimaryWeapon:
                            selector.ChangeToSecond();
                            break;

                        case EquipmentSlot.Holster:
                            selector.TryChangeWeapon(true);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private float _nextChangeWeaponTime;

        private float getDistance()
        {
            if (_nextGetDistTime < Time.time)
            {
                _nextGetDistTime = Time.time + 0.5f;
                Vector3? target = Bot.CurrentTargetPosition;
                if (target != null)
                {
                    _lastDistance = Bot.CurrentTargetDistance;
                }
            }
            return _lastDistance;
        }

        private float _lastDistance;
        private float _nextGetDistTime;

        private EquipmentSlot findOptimalWeaponForDistance(float distance)
        {
            if (_nextCheckOptimalTime < Time.time)
            {
                _nextCheckOptimalTime = Time.time + 0.5f;

                var equipment = Bot.PlayerComponent.Equipment;

                float? primaryEngageDist = null;
                var primary = equipment.PrimaryWeapon;
                if (isWeaponDurableEnough(primary))
                {
                    primaryEngageDist = primary.EngagementDistance;
                }

                float? secondaryEngageDist = null;
                var secondary = equipment.SecondaryWeapon;
                if (isWeaponDurableEnough(secondary))
                {
                    secondaryEngageDist = secondary.EngagementDistance;
                }

                float? holsterEngageDist = null;
                var holster = equipment.HolsterWeapon;
                if (isWeaponDurableEnough(holster))
                {
                    holsterEngageDist = holster.EngagementDistance;
                }

                float minDifference = Mathf.Abs(distance - primaryEngageDist ?? 0);
                optimalSlot = EquipmentSlot.FirstPrimaryWeapon;

                float difference = Mathf.Abs(distance - secondaryEngageDist ?? 0);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    optimalSlot = EquipmentSlot.SecondPrimaryWeapon;
                }

                if (!BotOwner.WeaponManager.HaveBullets)
                {
                    difference = Mathf.Abs(distance - holsterEngageDist ?? 0);
                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        optimalSlot = EquipmentSlot.Holster;
                    }
                }
            }
            return optimalSlot;
        }

        private bool isWeaponDurableEnough(WeaponInfo info, float min = 0.5f)
        {
            return info != null &&
                info.Durability > min &&
                info.Weapon.ChamberAmmoCount > 0;
        }

        private EquipmentSlot optimalSlot;
        private float _nextCheckOptimalTime;

        private void aimAtEnemy()
        {
            Vector3? pointToShoot = GetPointToShoot();
            if (pointToShoot == null)
            {
                return;
            }

            BotOwner.AimingData.SetTarget(pointToShoot.Value);
            if (!BotOwner.AimingData.IsReady && !checkSteerDirection(20f))
            {
                Bot.Steering.LookToPoint(BotOwner.AimingData.EndTargetPoint, 250f);
                return;
            }

            BotOwner.AimingData.NodeUpdate();
            if (!BotOwner.AimingData.IsReady)
            {
                return;
            }
            if (Bot.NoBushESP.NoBushESPActive)
            {
                return;
            }
            if (!FriendlyFire.CheckFriendlyFire())
            {
                BotOwner.ShootData.EndShoot();
                return;
            }
            ReadyToShoot();
            Shoot.Update();
        }

        private bool checkSteerDirection(float maxAngle)
        {
            Vector3 target = BotOwner.AimingData.EndTargetPoint;
            Vector3 weaponPointDir = Bot.LookDirection;
            Vector3 directionToTarget = (target - BotOwner.WeaponRoot.position).normalized;
            float angle = Vector3.Angle(directionToTarget, weaponPointDir);
            return angle <= maxAngle;
        }

        private Vector3? blindShootTarget(Enemy enemy)
        {
            Vector3? result = null;
            if (!enemy.IsVisible
                    && enemy.Status.HeardRecently
                    && enemy.InLineOfSight)
            {
                EnemyPlace lastKnown = enemy.KnownPlaces.LastKnownPlace;
                if (lastKnown != null && lastKnown.PersonalClearLineOfSight(BotOwner.LookSensor._headPoint, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    result = lastKnown.Position + Vector3.up + UnityEngine.Random.onUnitSphere;
                }
            }
            return result;
        }

        protected virtual void ReadyToShoot()
        {
        }

        protected virtual Vector3? GetTarget()
        {
            Vector3? target = getAimTarget(Bot.Enemy);

            if (target == null)
            {
                target = getAimTarget(Bot.LastEnemy);
            }
            return target;
        }

        private Vector3? getAimTarget(Enemy enemy)
        {
            if (enemy != null && enemy.IsVisible && enemy.CanShoot)
            {
                Vector3? centerMass = findCenterMassPoint(enemy);
                Vector3? partToShoot = getEnemyPartToShoot(enemy.EnemyInfo);
                Vector3? modifiedTarget = checkYValue(centerMass, partToShoot);
                return modifiedTarget ?? partToShoot ?? centerMass;
            }
            return null;
        }

        private Vector3? checkYValue(Vector3? centerMass, Vector3? partTarget)
        {
            if (centerMass != null &&
                partTarget != null &&
                centerMass.Value.y < partTarget.Value.y)
            {
                Vector3 newTarget = partTarget.Value;
                newTarget.y = centerMass.Value.y;
                return new Vector3?(newTarget);
            }
            return null;
        }

        private Vector3? findCenterMassPoint(Enemy enemy)
        {
            if (enemy.IsAI)
            {
                return null;
            }
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Aiming.AimCenterMassGlobal)
            {
                return null;
            }
            if (!Bot.Info.FileSettings.Aiming.AimCenterMass)
            {
                return null;
            }
            return enemy.CenterMass;
        }

        private Vector3? getEnemyPartToShoot(EnemyInfo enemy)
        {
            if (enemy != null)
            {
                Vector3 value;
                if (enemy.Distance < 6f)
                {
                    value = enemy.GetCenterPart();
                }
                else
                {
                    value = enemy.GetPartToShoot();
                }
                return new Vector3?(value);
            }
            return null;
        }

        protected virtual Vector3? GetPointToShoot()
        {
            Vector3? target = GetTarget();
            if (target != null)
            {
                Target = target.Value;
            }
            return target;
        }

        protected Vector3 Target;

        public SAINFriendlyFireClass FriendlyFire => Bot.FriendlyFire;
    }

    public class BotShoot : BaseNodeAbstractClass
    {
        public BotShoot(BotOwner bot)
            : base(bot)
        {
        }

        public override void Update()
        {
            if (!this.botOwner_0.WeaponManager.HaveBullets)
            {
                return;
            }
            if (this.botOwner_0.ShootData.Shoot())
            {
                this.botOwner_0.Memory.GoalEnemy?.SetLastShootTime();
            }
        }
    }
}