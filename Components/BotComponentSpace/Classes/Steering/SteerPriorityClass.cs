using EFT;
using HarmonyLib;
using SAIN.Plugin;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.WeaponFunction;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SteerPriorityClass : BotSubClass<SAINSteeringClass>
    {
        public SteerPriority CurrentSteerPriority { get; private set; }
        public SteerPriority LastSteerPriority { get; private set; }
        public PlaceForCheck LastHeardSound { get; private set; }
        public Enemy EnemyWhoLastShotMe { get; private set; }

        // How long a bot will look at where they last saw an enemy instead of something they hear
        private readonly float Steer_TimeSinceLocationKnown_Threshold = 3f;
        // How long a bot will look at where they last saw an enemy if they don't hear any other threats
        private readonly float Steer_TimeSinceSeen_Long = 60f;
        // How far a sound can be for them to react by looking toward it.
        private readonly float Steer_HeardSound_Dist = 60f;
        // How old a sound can be, in seconds, for them to react by looking toward it.
        private readonly float Steer_HeardSound_Age = 3f;

        public AimStatus AimStatus
        {
            get
            {
                object aimStatus = aimStatusField.GetValue(BotOwner.AimingData);
                if (aimStatus == null)
                {
                    return AimStatus.NoTarget;
                }

                var status = (AimStatus)aimStatus;

                if (status != AimStatus.NoTarget && 
                    Bot.Enemy?.IsVisible == false && 
                    Bot.LastEnemy?.IsVisible == false)
                {
                    return AimStatus.NoTarget;
                }
                return (AimStatus)aimStatus;
            }
        }

        public SteerPriorityClass(SAINSteeringClass steering) : base(steering)
        {

        }

        public SteerPriority GetCurrentSteerPriority(bool lookRandom, bool ignoreRunningPath)
        {
            var lastPriority = CurrentSteerPriority;
            CurrentSteerPriority = findSteerPriority(lookRandom, ignoreRunningPath);

            if (CurrentSteerPriority != lastPriority)
                LastSteerPriority = lastPriority;

            return CurrentSteerPriority;
        }

        private SteerPriority findSteerPriority(bool lookRandom, bool ignoreRunningPath)
        {
            SteerPriority result = strickChecks(ignoreRunningPath);

            if (result != SteerPriority.None)
            {
                return result;
            }

            result = reactiveSteering();

            if (result != SteerPriority.None)
            {
                return result;
            }

            result = senseSteering();

            if (result != SteerPriority.None)
            {
                return result;
            }

            if (lookRandom)
            {
                return SteerPriority.RandomLook;
            }
            return SteerPriority.None;
        }

        private SteerPriority strickChecks(bool ignoreRunningPath)
        {
            if (!ignoreRunningPath && Bot.Mover.SprintController.Running)
                return SteerPriority.RunningPath;

            if (Player.IsSprintEnabled)
                return SteerPriority.Sprinting;

            if (lookToAimTarget())
                return SteerPriority.Aiming;

            if (Bot.ManualShoot.Reason != EShootReason.None
                && Bot.ManualShoot.ShootPosition != Vector3.zero)
                return SteerPriority.ManualShooting;

            if (enemyVisible())
                return SteerPriority.EnemyVisible;

            return SteerPriority.None;
        }

        private SteerPriority reactiveSteering()
        {
            if (enemyShotMe())
            {
                return SteerPriority.LastHit;
            }

            if (BotOwner.Memory.IsUnderFire && !Bot.Memory.LastUnderFireEnemy.IsCurrentEnemy)
                return SteerPriority.UnderFire;

            return SteerPriority.None;
        }

        private SteerPriority senseSteering()
        {
            EnemyPlace lastKnownPlace = Bot.Enemy?.KnownPlaces?.LastKnownPlace;

            if (lastKnownPlace != null && lastKnownPlace.TimeSincePositionUpdated < Steer_TimeSinceLocationKnown_Threshold)
                return SteerPriority.EnemyLastKnown;

            if (heardThreat(out PlaceForCheck placeForCheck))
            {
                LastHeardSound = placeForCheck;
                return SteerPriority.HeardThreat;
            }

            LastHeardSound = null;

            if (lastKnownPlace != null && lastKnownPlace.TimeSincePositionUpdated < Steer_TimeSinceSeen_Long)
                return SteerPriority.EnemyLastKnownLong;

            return SteerPriority.None;
        }

        private bool heardThreat(out PlaceForCheck placeForCheck)
        {
            placeForCheck = BotOwner.BotsGroup.YoungestFastPlace(BotOwner, Steer_HeardSound_Dist, Steer_HeardSound_Age);
            if (placeForCheck != null)
            {
                Enemy enemy = Bot.Enemy;
                if (enemy == null)
                {
                    return true;
                }
                if (Bot.Squad.SquadInfo?.PlayerPlaceChecks.TryGetValue(enemy.EnemyProfileId, out PlaceForCheck enemyPlace) == true &&
                    enemyPlace != placeForCheck)
                {
                    return true;
                }
            }
            return false;
        }

        private bool enemyShotMe()
        {
            float timeSinceShot = Bot.Medical.TimeSinceShot;
            if (timeSinceShot > 3f || timeSinceShot < 0.2f)
            {
                EnemyWhoLastShotMe = null;
                return false;
            }

            Enemy enemy = Bot.Medical.HitByEnemy.EnemyWhoLastShotMe;
            if (enemy != null && 
                enemy.IsValid && 
                enemy.EnemyPerson.Active && 
                !enemy.IsCurrentEnemy)
            {
                EnemyWhoLastShotMe = enemy;
                return true;
            }
            EnemyWhoLastShotMe = null;
            return false;
        }

        private bool lookToAimTarget()
        {
            if (BotOwner.WeaponManager.Reload?.Reloading == true)
            {
                return false;
            }
            if (Bot.Aim.AimStatus == AimStatus.NoTarget)
            {
                return false;
            }
            return canSeeAndShoot(Bot.Enemy) || canSeeAndShoot(Bot.LastEnemy);
        }

        private bool canSeeAndShoot(Enemy enemy)
        {
            return enemy != null && enemy.IsVisible && enemy.CanShoot;
        }

        private bool enemyVisible()
        {
            Enemy enemy = Bot.Enemy;

            if (enemy != null)
            {
                if (enemy.IsVisible)
                {
                    return true;
                }

                if (enemy.Seen &&
                    enemy.TimeSinceSeen < 0.5f)
                {
                    return true;
                }
            }
            return false;
        }

        static SteerPriorityClass()
        {
            aimStatusField = AccessTools.Field(Helpers.HelpersGClass.AimDataType, "aimStatus_0");
        }

        private void updateSettings()
        {
        }

        private static FieldInfo aimStatusField;
    }
}