using EFT;
using SAIN.Preset.Personalities;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SAIN.SAINComponent.Classes.Search
{
    public class SAINSearchClass : BotBase, IBotClass
    {
        public bool SearchActive { get; private set; }

        public ESearchMove NextState { get; private set; }
        public ESearchMove CurrentState { get; private set; }
        public ESearchMove LastState { get; private set; }

        public Vector3? FinalDestination => PathFinder.FinalDestination;
        public BotPeekPlan? PeekPoints => PathFinder.PeekPoints;

        public SearchDeciderClass SearchDecider { get; private set; }
        public SearchPathFinder PathFinder { get; private set; }


        public SAINSearchClass(BotComponent sain) : base(sain)
        {
            SearchDecider = new SearchDeciderClass(this);
            PathFinder = new SearchPathFinder(this);
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

        public void ToggleSearch(bool value, Enemy target)
        {
            SearchActive = value;
            if (target != null)
            {
                target.Events.OnSearch.CheckToggle(value);
            }
            if (!value)
            {
                Reset();
            }
        }

        public void Search(bool shallSprint, Enemy enemy)
        {
            PathFinder.UpdateSearchDestination(enemy);
            SwitchSearchModes(shallSprint, enemy);
            PeekPoints?.DrawDebug();
        }

        private bool WaitAtPoint()
        {
            if (_waitAtPointTimer < 0)
            {
                float baseTime = 3;
                baseTime *= Bot.Info.PersonalitySettings.Search.SearchWaitMultiplier;
                float waitTime = baseTime * Random.Range(0.25f, 1.25f);
                _waitAtPointTimer = Time.time + waitTime;
                //BotOwner.Mover.MovementPause(waitTime, false);
            }
            if (_waitAtPointTimer < Time.time)
            {
                //BotOwner.Mover.MovementResume();
                _waitAtPointTimer = -1;
                return false;
            }
            return true;
        }

        private bool moveToPoint(Vector3 destination, bool shallSprint)
        {
            var sprint = Bot.Mover.SprintController;
            if (shallSprint && 
                sprint.RunToPoint(destination, Mover.ESprintUrgency.Middle, true))
            {
                return true;
            }
            if (Bot.Mover.GoToPoint(destination, out _))
            {
                return true;
            }
            return false;
        }

        private void handleLight(bool stealthy)
        {
            if (_Running || Bot.Mover.SprintController.Running)
            {
                return;
            }
            if (stealthy || _searchSettings.Sneaky)
            {
                Bot.BotLight.ToggleLight(false);
                return;
            }
            if (BotOwner.Mover?.IsMoving == true)
            {
                Bot.BotLight.HandleLightForSearch(BotOwner.Mover.DirCurPoint.magnitude);
                return;
            }
        }

        private void SwitchSearchModes(bool shallSprint, Enemy enemy)
        {
            if (FinalDestination == null)
            {
                Logger.LogWarning($"{BotOwner.name}'s Final Destination is null, cannot search!");
                return;
            }

            bool shallBeStealthy = SearchDecider.ShallBeStealthyDuringSearch(enemy);
            getSpeedandPose(out float speed, out float pose, shallSprint, shallBeStealthy);
            handleLight(shallBeStealthy);
            checkShallWaitandReload();

            ESearchMove previousState = CurrentState;
            PeekPosition? peekPosition;
            switch (CurrentState)
            {
                case ESearchMove.None:
                    if (shallStartPeek(shallSprint))
                    {
                        CurrentState = ESearchMove.MoveToStartPeek;
                        break;
                    }

                    if (moveToPoint(FinalDestination.Value, shallSprint))
                    {
                        CurrentState = ESearchMove.DirectMove;
                        break;
                    }
                    Logger.LogWarning($"{BotOwner.name}'s cannot peek and cannot direct move!");
                    break;

                case ESearchMove.DirectMove:

                    setSpeedPose(speed, pose);
                    moveToPoint(FinalDestination.Value, shallSprint);
                    break;

                case ESearchMove.Advance:

                    if (_advanceTime < 0)
                    {
                        _advanceTime = Time.time + 5f;
                    }
                    if (_advanceTime < Time.time)
                    {
                        _advanceTime = -1f;
                        CurrentState = ESearchMove.None;
                        PathFinder.FinishedPeeking = true;
                        break;
                    }

                    setSpeedPose(speed, pose);
                    moveToPoint(FinalDestination.Value, shallSprint);
                    break;

                case ESearchMove.MoveToStartPeek:

                    peekPosition = PeekPoints?.PeekStart;
                    if (peekPosition != null && 
                        !botIsAtPoint(peekPosition.Value.Point))
                    {
                        setSpeedPose(speed, pose);
                        if (moveToPoint(peekPosition.Value.Point, shallSprint))
                        {
                            break;
                        }
                    }
                    CurrentState = ESearchMove.MoveToEndPeek;
                    break;

                case ESearchMove.MoveToEndPeek:

                    peekPosition = PeekPoints?.PeekEnd;
                    if (peekPosition != null &&
                        !botIsAtPoint(peekPosition.Value.Point))
                    {
                        setSpeedPose(speed, pose);
                        if (moveToPoint(peekPosition.Value.Point, shallSprint))
                        {
                            break;
                        }
                    }
                    CurrentState = ESearchMove.Wait;
                    NextState = ESearchMove.MoveToDangerPoint;
                    break;

                case ESearchMove.MoveToDangerPoint:

                    Vector3? danger = PeekPoints?.DangerPoint;
                    if (danger != null && 
                        !botIsAtPoint(danger.Value))
                    {
                        setSpeedPose(speed, pose);
                        if (moveToPoint(danger.Value, shallSprint))
                        {
                            break;
                        }
                    }
                    CurrentState = ESearchMove.Advance;
                    break;

                case ESearchMove.Wait:
                    if (WaitAtPoint())
                    {
                        Bot.Mover.SetTargetMoveSpeed(0f);
                        Bot.Mover.SetTargetPose(0.75f);
                        break;
                    }
                    Bot.Mover.SetTargetMoveSpeed(speed);
                    Bot.Mover.SetTargetPose(pose);
                    CurrentState = NextState;
                    break;
            }

            if (previousState != CurrentState)
            {
                LastState = previousState;
            }
        }

        private void getSpeedandPose(out float speed, out float pose, bool sprinting, bool stealthy)
        {
            speed = 1f;
            pose = 1f;
            // are we sprinting?
            if (sprinting || Player.IsSprintEnabled || _Running || Bot.Mover.SprintController.Running)
            {
                return;
            }
            // are we indoors?
            if (getIndoorsSpeedPose(stealthy, out speed, out pose))
            {
                return;
            }
            // we are outside...
            if (_searchSettings.Sneaky &&
                Bot.Cover.CoverPoints.Count > 2 &&
                Time.time - BotOwner.Memory.UnderFireTime > 30f)
            {
                speed = 0.33f;
                pose = 0.6f;
                return;
            }
            if (stealthy)
            {
                speed = 0.5f;
                pose = 0.7f;
                return;
            }
        }

        private bool getIndoorsSpeedPose(bool stealthy, out float speed, out float pose)
        {
            speed = 1f;
            pose = 1f;
            if (!Bot.Memory.Location.IsIndoors)
            {
                return false;
            }
            var searchSettings = _searchSettings;
            if (searchSettings.Sneaky)
            {
                speed = searchSettings.SneakySpeed;
                pose = searchSettings.SneakyPose;
            }
            else if (stealthy)
            {
                speed = 0f;
                pose = 0f;
            }
            return true;
        }

        private void checkShallWaitandReload()
        {
            if (BotOwner.WeaponManager?.Reload?.Reloading == true &&
                CurrentState != ESearchMove.Wait)
            {
                NextState = CurrentState;
                CurrentState = ESearchMove.Wait;
            }
        }

        private bool shallStartPeek(bool shallSprint)
        {
            if (shallSprint)
            {
                return false;
            }
            if (PeekPoints != null && moveToPoint(PeekPoints.Value.PeekStart.Point, shallSprint))
            {
                return true;
            }
            return false;
        }

        private bool shallDirectMove(bool shallSprint)
        {
            if (shallSprint)
            {
                return true;
            }
            if (PeekPoints == null)
            {
                return true;
            }
            if (moveToPoint(FinalDestination.Value, shallSprint))
            {
                return true;
            }
            return false;
        }

        private void setSpeedPose(float speed, float pose)
        {
            Bot.Mover.SetTargetMoveSpeed(speed);
            Bot.Mover.SetTargetPose(pose);
        }

        public void Reset()
        {
            ResetStates();
            PathFinder.Reset();
        }

        public void ResetStates()
        {
            CurrentState = ESearchMove.None;
            LastState = ESearchMove.None;
            NextState = ESearchMove.None;
        }

        public bool botIsAtPoint(Vector3 point, float reachDist = 0.5f)
        {
            return DistanceToDestination(point) < reachDist;
        }

        public float DistanceToDestination(Vector3 point)
        {
            return (point - Bot.Position).magnitude;
        }

        private bool _Running => Bot.Mover.SprintController.Running;
        private float _waitAtPointTimer = -1;
        private float _advanceTime;
        private PersonalitySearchSettings _searchSettings => Bot.Info.PersonalitySettings.Search;
    }
}