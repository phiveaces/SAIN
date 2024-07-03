using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections;
using System.Text;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class RunToCoverAction : SAINAction, ISAINAction
    {
        public RunToCoverAction(BotOwner bot) : base(bot, "Run To Cover")
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override IEnumerator ActionCoroutine()
        {
            while (true)
            {
                yield return null;

                Bot.Mover.SetTargetMoveSpeed(1f);
                Bot.Mover.SetTargetPose(1f);

                jumpToCover();

                if (shallRecalcDestination())
                {
                    _moveSuccess = moveToCover(out bool sprinting, out CoverPoint coverDestination, false);
                    if (!_moveSuccess)
                    {
                        _moveSuccess = moveToCover(out sprinting, out coverDestination, true);
                    }

                    _sprinting = sprinting;
                    Bot.Cover.CoverInUse = coverDestination;

                    if (_moveSuccess)
                    {
                        _recalcMoveTimer = Time.time + 2f;
                        _shallJumpToCover = EFTMath.RandomBool(2)
                            && _sprinting
                            && BotOwner.Memory.IsUnderFire
                            && Bot.Info.Profile.IsPMC;

                        _runDestination = coverDestination.Position;
                    }
                    else
                    {
                        _recalcMoveTimer = Time.time + 0.5f;
                    }
                }

                //if (_moveSuccess && 
                //    _sprinting && 
                //    _nextTryReloadTime < Time.time && 
                //    Bot.Decision.SelfActionDecisions.LowOnAmmo(0.5f))
                //{
                //    _nextTryReloadTime = Time.time + 2f;
                //    Bot.SelfActions.TryReload();
                //}

                if (Bot.Cover.CoverPoints.Count == 0 && !_moveSuccess)
                {
                    Bot.Mover.EnableSprintPlayer(false);
                    Bot.Cover.CoverInUse = null;
                    Bot.Mover.SprintController.CancelRun();
                    Bot.Mover.DogFight.DogFightMove(false);

                    if (!Bot.Steering.SteerByPriority(false))
                    {
                        Bot.Steering.LookToLastKnownEnemyPosition(Bot.Enemy);
                    }
                    Shoot.Update();
                    continue;
                }

                if (!Bot.Mover.SprintController.Running)
                {
                    Bot.Mover.EnableSprintPlayer(false);
                    if (!Bot.Steering.SteerByPriority(false))
                    {
                        Bot.Steering.LookToLastKnownEnemyPosition(Bot.Enemy);
                    }
                    Shoot.Update();
                    continue;
                }
                BotOwner.AimingData?.LoseTarget();
            }
        }

        public override void Update()
        {
        }

        private bool shallRecalcDestination()
        {
            return _recalcMoveTimer < Time.time &&
                (!Bot.Mover.SprintController.Running || Bot.Cover.CoverInUse?.IsBad == true);
        }

        private void jumpToCover()
        {
            if (_shallJumpToCover && 
                _moveSuccess && 
                _sprinting && 
                Bot.Player.IsSprintEnabled && 
                _jumpTimer < Time.time)
            {
                CoverPoint coverInUse = Bot.Cover.CoverInUse;
                if (coverInUse != null)
                {
                    float sqrMag = (coverInUse.Position - Bot.Position).sqrMagnitude;
                    if (sqrMag < 3f * 3f && sqrMag > 1.5f * 1.5f)
                    {
                        _jumpTimer = Time.time + 5f;
                        Bot.Mover.TryJump();
                    }
                }
            }
        }

        private bool moveToCover(out bool sprinting, out CoverPoint coverDestination, bool tryWalk)
        {
            if (Bot.Mover.SprintController.Running && Bot.Mover.SprintController.Canceling)
            {
                sprinting = true;
                coverDestination = null;
                return false;
            }
            if (tryRun(Bot.Cover.CoverInUse, out sprinting, tryWalk))
            {
                coverDestination = Bot.Cover.CoverInUse;
                return true;
            }

            CoverPoint fallback = Bot.Cover.FallBackPoint;
            SoloDecision currentDecision = Bot.Decision.CurrentSoloDecision;

            if (currentDecision == SoloDecision.Retreat &&
                fallback != null &&
                tryRun(fallback, out sprinting, tryWalk))
            {
                coverDestination = fallback;
                return true;
            }

            Bot.Cover.SortPointsByPathDist();

            sprinting = false;
            var coverPoints = Bot.Cover.CoverPoints;

            for (int i = 0; i < coverPoints.Count; i++)
            {
                CoverPoint coverPoint = coverPoints[i];
                if (tryRun(coverPoint, out sprinting, tryWalk))
                {
                    coverDestination = coverPoint;
                    return true;
                }
            }
            coverDestination = null;
            return false;
        }

        private bool checkIfPointGoodEnough(CoverPoint coverPoint, float minDot = 0.1f)
        {
            if (coverPoint == null)
            {
                return false;
            }
            if (!coverPoint.IsBad)
            {
                return true;
            }
            Vector3 target = findTarget();
            if (target == Vector3.zero)
            {
                return true;
            }
            float dot = Vector3.Dot(coverPoint.DirectionToColliderNormal, (target - coverPoint.Position).normalized);
            return dot > minDot;
        }

        private Vector3 findTarget()
        {
            Vector3 target;
            Vector3? grenade = Bot.Grenade.GrenadeDangerPoint;
            if (grenade != null)
            {
                target = grenade.Value;
            }
            else if (Bot.CurrentTargetPosition != null)
            {
                target = Bot.CurrentTargetPosition.Value;
            }
            else
            {
                target = Vector3.zero;
            }
            return target;
        }

        private bool tooCloseToGrenade(Vector3 pos)
        {
            Vector3? grenadePos = Bot.Grenade.GrenadeDangerPoint;
            if (grenadePos != null &&
                (grenadePos.Value - pos).sqrMagnitude < 3f * 3f)
            {
                return true;
            }
            return false;
        }

        private bool tryRun(CoverPoint coverPoint, out bool sprinting, bool tryWalk)
        {
            bool result = false;
            sprinting = false;

            if (!checkIfPointGoodEnough(coverPoint))
            {
                return false;
            }

            Vector3 destination = coverPoint.Position;

            if (!tryWalk &&
                coverPoint.PathLength >= Bot.Info.FileSettings.Move.RUN_TO_COVER_MIN && 
                Bot.Mover.SprintController.RunToPoint(destination, getUrgency(), false))
            {
                sprinting = true;
                return true;
            }

            if (tryWalk)
            {
                bool shallCrawl = Bot.Player.IsInPronePose || (Bot.Decision.CurrentSelfDecision != SelfDecision.None
                    && coverPoint.Status == CoverStatus.FarFromCover
                    && Bot.Mover.Prone.ShallProneHide());

                result = Bot.Mover.GoToPoint(destination, out _, 0.5f, shallCrawl, false);
            }
            return result;
        }

        private ESprintUrgency getUrgency()
        {
            bool isUrgent =
                BotOwner.Memory.IsUnderFire ||
                Bot.Suppression.IsSuppressed ||
                Bot.Decision.CurrentSelfDecision != SelfDecision.None;

            return isUrgent ? ESprintUrgency.High : ESprintUrgency.Middle;
        }

        public override void Start()
        {
            Toggle(true);

            if (Bot.Decision.CurrentSoloDecision == SoloDecision.AvoidGrenade
                && Bot.Talk.GroupTalk.FriendIsClose)
            {
                Bot.Talk.TalkAfterDelay(EPhraseTrigger.OnEnemyGrenade, ETagStatus.Combat, 0.33f);
            }

            Bot.Mover.SprintController.CancelRun(0.25f);
            _recalcMoveTimer = 0f;
            _shallJumpToCover = false;
            _sprinting = false;
            _moveSuccess = false;
        }

        public override void Stop()
        {
            Toggle(false);

            Bot.Mover.DogFight.ResetDogFightStatus();
            Bot.Mover.SprintController.CancelRun(0.25f);
            Bot.Cover.CheckResetCoverInUse();
            Bot.Mover.Sprint(false);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Run To Cover Info");

            var sprint = Bot.Mover.SprintController;
            if (sprint.Running)
            {
                stringBuilder.AppendLabeledValue("Running Status", $"{sprint.CurrentRunStatus}", Color.white, Color.yellow, true);
            }

            var cover = Bot.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);
            if (Bot.CurrentTargetPosition != null)
            {
                stringBuilder.AppendLabeledValue("Current Target Position", $"{Bot.CurrentTargetPosition.Value}", Color.white, Color.yellow, true);
            }
            else
            {
                stringBuilder.AppendLabeledValue("Current Target Position", null, Color.white, Color.yellow, true);
            }

            var _coverDestination = Bot.Cover.CoverInUse;
            if (_coverDestination != null)
            {
                stringBuilder.AppendLine("Cover Destination");
                stringBuilder.AppendLabeledValue("Status", $"{_coverDestination.Status}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{_coverDestination.CoverHeight} {_coverDestination.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{_coverDestination.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(_coverDestination.Position - Bot.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }

        private bool _moveSuccess;
        private float _recalcMoveTimer;
        private float _jumpTimer;
        private bool _shallJumpToCover;
        private bool _sprinting;
        private Vector3 _runDestination;
    }
}