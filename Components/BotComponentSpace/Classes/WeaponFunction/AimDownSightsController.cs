using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Search;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class AimDownSightsController : BotBase, IBotClass
    {
        public AimDownSightsController(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {

        }

        public void Dispose()
        {
        }

        public void UpdateADSstatus()
        {
            Vector3? targetPos = Bot.CurrentTargetPosition;

            // If a bot is sneaky, don't change ADS if their enemy is close to avoid alerting them.
            if (Bot.Info.PersonalitySettings.Search.Sneaky && targetPos != null
                && Bot.Enemy?.IsVisible != true
                && (targetPos.Value - Bot.Position).sqrMagnitude < 30f * 30f)
            {
                return;
            }

            bool shallADS = ShallAimDownSights(Bot.CurrentTargetPosition);
            SetADS(shallADS);
        }

        public bool ShallAimDownSights(Vector3? targetPosition = null)
        {
            bool result = false;
            EAimDownSightsStatus status = EAimDownSightsStatus.None;
            if (targetPosition != null)
            {
                status = GetADSStatus(targetPosition.Value);
            }
            float timeSinceChangeDecision = Bot.Decision.TimeSinceChangeDecision;
            switch (status)
            {
                case EAimDownSightsStatus.None:
                    break;

                case EAimDownSightsStatus.HoldInCover:
                    result = timeSinceChangeDecision > 3f;
                    break;

                case EAimDownSightsStatus.StandAndShoot:
                    result = Bot.Enemy != null && Bot.Enemy.RealDistance > 10f;
                    break;

                case EAimDownSightsStatus.EnemyVisible:
                    result = true;
                    break;

                case EAimDownSightsStatus.Sprinting:
                    result = false;
                    break;

                case EAimDownSightsStatus.MovingToCover:
                    result = Bot.ManualShoot.Reason == EShootReason.WalkToCoverSuppress;
                    break;

                case EAimDownSightsStatus.Suppressing:
                    result = Bot.ManualShoot.Reason == EShootReason.SquadSuppressing;
                    break;

                case EAimDownSightsStatus.DogFight:
                    result = Bot.Enemy != null && Bot.Enemy.RealDistance > 10;
                    break;

                case EAimDownSightsStatus.EnemySeenRecent:
                    result = true;
                    break;

                case EAimDownSightsStatus.EnemyHeardRecent:
                    result = true;
                    break;

                default:
                    break;
            }

            LastADSstatus = CurrentADSstatus;
            CurrentADSstatus = status;
            return result;
        }

        public bool SetADS(bool value)
        {
            var shootController = BotOwner.WeaponManager.ShootController;
            if (shootController != null && shootController.IsAiming != value)
            {
                shootController?.SetAim(value);
                AimingDownSights = value;
                return true;
            }
            return false;
        }

        public EAimDownSightsStatus CurrentADSstatus { get; private set; }
        public EAimDownSightsStatus LastADSstatus { get; private set; }

        public EAimDownSightsStatus GetADSStatus(Vector3 targetPosition)
        {
            var enemy = Bot.Enemy;
            float sqrMagToTarget = (targetPosition - Bot.Position).sqrMagnitude;

            EAimDownSightsStatus result;
            if (Bot.Player.IsSprintEnabled)
            {
                result = EAimDownSightsStatus.Sprinting;
            }
            else if (Bot.Decision.CurrentSoloDecision == CombatDecision.ShootDistantEnemy)
            {
                result = EAimDownSightsStatus.StandAndShoot;
            }
            else if (enemy != null && enemy.CanShoot && enemy.IsVisible && enemy.RealDistance > 20f)
            {
                result = EAimDownSightsStatus.EnemyVisible;
            }
            else if (enemy != null && enemy.Seen && enemy.TimeSinceSeen < 5)
            {
                result = EAimDownSightsStatus.EnemySeenRecent;
            }
            else if (enemy != null && enemy.Heard && enemy.TimeSinceHeard < 5)
            {
                result = EAimDownSightsStatus.EnemyHeardRecent;
            }
            else if (Bot.Decision.CurrentSquadDecision == SquadDecision.Suppress && Bot.ManualShoot.Reason == EShootReason.SquadSuppressing)
            {
                result = EAimDownSightsStatus.Suppressing;
            }
            else
            {
                switch (Bot.Decision.CurrentSoloDecision)
                {
                    case CombatDecision.RunToCover:
                    case CombatDecision.MoveToCover:
                        result = EAimDownSightsStatus.MovingToCover;
                        break;

                    case CombatDecision.HoldInCover:
                        result = EAimDownSightsStatus.HoldInCover;
                        break;

                    case CombatDecision.StandAndShoot:
                        result = EAimDownSightsStatus.StandAndShoot;
                        break;

                    case CombatDecision.DogFight:
                        result = EAimDownSightsStatus.DogFight;
                        break;

                    case CombatDecision.Search:
                        result = Bot.Search.CurrentState != ESearchMove.DirectMove ? EAimDownSightsStatus.SearchPeekWait : EAimDownSightsStatus.None;
                        break;

                    default:
                        result = EAimDownSightsStatus.None;
                        break;
                }
            }
            return result;
        }

        public enum EAimDownSightsStatus
        {
            None = 0,
            HoldInCover = 1,
            StandAndShoot = 2,
            EnemyVisible = 3,
            Sprinting = 4,
            MovingToCover = 5,
            Suppressing = 6,
            DogFight = 7,
            EnemySeenRecent = 8,
            EnemyHeardRecent = 9,
            SearchPeekWait = 10,
        }

        public bool AimingDownSights { get; private set; }

    }
}
