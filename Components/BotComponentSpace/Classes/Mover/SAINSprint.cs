using EFT;
using EFT.Interactive;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public enum RunStatus
    {
        None,
        FirstTurn,
        Running,
        Turning,
        ShortCorner,
        NoStamina,
        InteractingWithDoor,
        ArrivingAtDestination,
        CantSprint,
        LookAtEnemyNoSprint,
        Canceling,
    }

    public class SAINSprint : BotBase, IBotClass
    {
        public SAINSprint(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public bool Running => _runToPointCoroutine != null;

        public bool Canceling { get; private set; }

        public void CancelRun(float afterTime = -1f)
        {
            if (Running)
            {
                if (Canceling)
                {
                    return;
                }
                if (afterTime > 0f)
                {
                    Canceling = true;
                    Bot.StartCoroutine(cancelRunAfterTime(afterTime));
                    return;
                }
                stopRunCoroutine();
            }
        }

        private void stopRunCoroutine()
        {
            if (!Running)
            {
                return;
            }
            Canceling = false;
            Bot.StopCoroutine(_runToPointCoroutine);
            _runToPointCoroutine = null;
            Bot.Mover.Sprint(false);
            _path.Clear();
        }

        private IEnumerator cancelRunAfterTime(float afterTime)
        {
            yield return new WaitForSeconds(afterTime);
            Canceling = false;
            CancelRun(-1f);
        }

        public bool RunToPointByWay(NavMeshPath way, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, System.Action callback = null)
        {
            if (!getLastCorner(way, out Vector3 point))
            {
                return false;
            }
            ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
            if (checkSameWay && IsPointSameWay(point))
            {
                return true;
            }
            startRun(way, point, urgency, callback);
            return true;
        }

        private bool getLastCorner(NavMeshPath way, out Vector3 result)
        {
            Vector3[] corners = way?.corners;
            if (corners == null)
            {
                result = Vector3.zero;
                return false;
            }
            if (way.status != NavMeshPathStatus.PathComplete)
            {
                result = Vector3.zero;
                return false;
            }

            Vector3? last = corners.LastElement();
            if (last == null)
            {
                result = Vector3.zero;
                return false;
            }

            result = last.Value;
            return true;
        }

        public bool RunToPoint(Vector3 point, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, System.Action callback = null)
        {
            if (checkSameWay && IsPointSameWay(point))
            {
                return true;
            }

            if (!Bot.Mover.CanGoToPoint(point, out NavMeshPath path))
            {
                return false;
            }
            ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
            startRun(path, point, urgency, callback);
            return true;
        }

        private bool ShallStopSprintWhenSeeEnemy;

        private bool IsPointSameWay(Vector3 point, float minDistSqr = 0.5f)
        {
            return Running && (LastRunDestination - point).sqrMagnitude < minDistSqr;
        }

        private void startRun(NavMeshPath path, Vector3 point, ESprintUrgency urgency, System.Action callback)
        {
            stopRunCoroutine();
            BotOwner.AimingData?.LoseTarget();
            LastRunDestination = point;
            CurrentPath = path;
            _lastUrgency = urgency;
            _runToPointCoroutine = Bot.StartCoroutine(runToPointCoroutine(path.corners, urgency, callback));
        }

        private float _timeStartRun;

        private ESprintUrgency _lastUrgency;

        public NavMeshPath CurrentPath;

        public bool RecalcPath()
        {
            return RunToPoint(LastRunDestination, _lastUrgency, false);
        }

        public Vector3 LastRunDestination { get; private set; }

        private Coroutine _runToPointCoroutine;

        public RunStatus CurrentRunStatus { get; private set; }

        public Vector3 CurrentCornerDestination()
        {
            if (_path.Count <= _currentIndex)
            {
                return Vector3.zero;
            }
            return _path[_currentIndex];
        }

        private int _currentIndex = 0;

        private IEnumerator runToPointCoroutine(Vector3[] corners, ESprintUrgency urgency, System.Action callback = null)
        {
            _path.Clear();
            _path.AddRange(corners);

            isShortCorner = false;
            _timeStartCorner = Time.time;
            positionMoving = true;
            _timeNotMoving = -1f;
            _timeStartRun = Time.time;

            BotOwner.Mover.Stop();
            _currentIndex = 1;

            // First step, look towards the path we want to run
            //yield return firstTurn(path.corners[1]);

            // Start running!
            yield return runPath(urgency);

            callback?.Invoke();

            CurrentRunStatus = RunStatus.None;
            stopRunCoroutine();
        }

        private readonly List<Vector3> _path = new List<Vector3>();

        private void moveToNextCorner()
        {
            if (totalCorners() > _currentIndex)
            {
                checkCornerLength();
                _currentIndex++;
            }
        }

        private void checkCornerLength()
        {
            Vector3 current = _path[_currentIndex];
            Vector3 next = _path[_currentIndex + 1];
            isShortCorner = (current - next).magnitude < 0.25f;
            _timeStartCorner = Time.time;
        }

        private float _timeStartCorner;

        private int totalCorners()
        {
            return _path.Count - 1;
        }

        private bool onLastCorner()
        {
            return totalCorners() <= _currentIndex;
        }

        private Vector3 lastCorner()
        {
            int count = _path.Count;
            if (count == 0)
            {
                return Vector3.zero;
            }
            return _path[count - 1];
        }

        private static GlobalMoveSettings _moveSettings => SAINPlugin.LoadedPreset.GlobalSettings.Move;

        private IEnumerator runPath(ESprintUrgency urgency)
        {
            int total = totalCorners();
            for (int i = 1; i <= total; i++)
            {
                // Track distance to target corner in the path.
                float distToCurrent = float.MaxValue;
                while (distToCurrent > _moveSettings.BotSprintCornerReachDist)
                {
                    distToCurrent = distanceToCurrentCornerSqr();
                    DistanceToCurrentCorner = distToCurrent;

                    Vector3 current = CurrentCornerDestination();
                    if (SAINPlugin.DebugMode)
                    {
                        DebugGizmos.Sphere(current, 0.1f);
                        DebugGizmos.Line(current, Bot.Position, 0.1f, 0.1f);
                    }

                    Bot.DoorOpener.Update();
                    trackMovement();

                    // Start or stop sprinting with a buffer
                    handleSprinting(distToCurrent, urgency);

                    if (BotOwner.DoorOpener.Interacting)
                    {
                        yield return null;
                        continue;
                    }

                    float timeSinceNoMove = timeSinceNotMoving;
                    if (timeSinceNoMove > _moveSettings.BotSprintRecalcTime && Time.time - _timeStartRun > 2f)
                    {
                        RecalcPath();
                        yield break;
                    }
                    //else if (timeSinceNoMove > _moveSettings.BotSprintTryJumpTime)
                    //{
                    //    SAINBot.Mover.TryJump();
                    //}
                    else if (timeSinceNoMove > _moveSettings.BotSprintTryVaultTime)
                    {
                        Bot.Mover.TryVault();
                    }

                    Vector3 destination = CurrentCornerDestination();
                    float speed = IsSprintEnabled ? _moveSettings.BotSprintTurnSpeed : _moveSettings.BotSprintFirstTurnSpeed;
                    float dotProduct = steer(destination, speed);
                    Vector3 moveDir = (destination - Bot.Position);
                    if (!Bot.IsSpeedHacker)
                    {
                        moveDir = moveDir.normalized;
                    }
                    move(moveDir);

                    //if (onLastCorner() && 
                    //    distToCurrent <= _moveSettings.BotSprintFinalDestReachDist)
                    //{
                    //    yield break;
                    //}

                    yield return null;
                }
                moveToNextCorner();
            }
        }

        private bool isShortCorner;
        public float DistanceToCurrentCorner { get; private set; }

        private float findStartSprintStamina(ESprintUrgency urgency)
        {
            switch (urgency)
            {
                case ESprintUrgency.None:
                case ESprintUrgency.Low:
                    return 0.75f;

                case ESprintUrgency.Middle:
                    return 0.5f;

                case ESprintUrgency.High:
                    return 0.2f;

                default:
                    return 0.5f;
            }
        }

        private float findEndSprintStamina(ESprintUrgency urgency)
        {
            switch (urgency)
            {
                case ESprintUrgency.None:
                case ESprintUrgency.Low:
                    return 0.4f;

                case ESprintUrgency.Middle:
                    return 0.2f;

                case ESprintUrgency.High:
                    return 0.01f;

                default:
                    return 0.25f;
            }
        }

        private bool shallLookAtEnemy()
        {
            return ShallStopSprintWhenSeeEnemy && Bot.Enemy?.IsVisible == true;
        }

        private void handleSprinting(float distToCurrent, ESprintUrgency urgency)
        {
            // I cant sprint :(
            if (!Player.MovementContext.CanSprint)
            {
                CurrentRunStatus = RunStatus.CantSprint;
                return;
            }

            if (Canceling)
            {
                CurrentRunStatus = RunStatus.Canceling;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (shallLookAtEnemy())
            {
                CurrentRunStatus = RunStatus.LookAtEnemyNoSprint;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (isShortCorner)
            {
                CurrentRunStatus = RunStatus.ShortCorner;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            bool isSprinting = Player.IsSprintEnabled;

            if (isSprinting && _moveSettings.EditSprintSpeed)
            {
                if (Bot.IsSpeedHacker)
                {
                    Player.MovementContext.SprintSpeed = 50f;
                }
                else
                {
                    Player.MovementContext.SprintSpeed = 1.5f;
                }
            }

            // Were messing with a door, dont sprint
            if (BotOwner.DoorOpener.Interacting)
            {
                CurrentRunStatus = RunStatus.InteractingWithDoor;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            // We are arriving to our destination, stop sprinting when you get close.
            if ((lastCorner() - BotPosition).magnitude <= _moveSettings.BotSprintDistanceToStopSprintDestination)
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = RunStatus.ArrivingAtDestination;
                return;
            }

            float staminaValue = Player.Physical.Stamina.NormalValue;

            // We are out of stamina, stop sprinting.
            if (shallPauseSprintStamina(staminaValue, urgency))
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = RunStatus.NoStamina;
                return;
            }

            // We are approaching a sharp corner, or we are currently not looking in the direction we need to go, stop sprinting
            if (shallPauseSprintAngle())
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = RunStatus.Turning;
                return;
            }

            // If we arne't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
            if (shallStartSprintStamina(staminaValue, urgency) && 
                _timeStartCorner + 0.25f < Time.time)
            {
                Bot.Mover.EnableSprintPlayer(true);
                CurrentRunStatus = RunStatus.Running;
                return;
            }
        }

        private bool shallPauseSprintStamina(float stamina, ESprintUrgency urgency) => stamina <= findEndSprintStamina(urgency);

        private bool shallStartSprintStamina(float stamina, ESprintUrgency urgency) => stamina >= findStartSprintStamina(urgency);

        private bool shallPauseSprintAngle()
        {
            Vector3? currentCorner = this.CurrentCornerDestination();
            return currentCorner != null && checkShallPauseSprintFromTurn(currentCorner.Value, _moveSettings.BotSprintCurrentCornerAngleMax);
        }

        private bool shallPauseMoveAngle()
        {
            Vector3? currentCorner = this.CurrentCornerDestination();
            return currentCorner != null &&
                checkShallPauseSprintFromTurn(currentCorner.Value, 60f);
        }

        private bool checkShallPauseSprintFromTurn(Vector3 currentCorner, float angleThresh = 25f)
        {
            return findAngleFromLook(currentCorner) >= angleThresh;
        }

        private float findAngle(Vector3 start, Vector3 end)
        {
            Vector3 origin = Bot.Position;
            Vector3 aDir = start - origin;
            Vector3 bDir = end - origin;
            aDir.y = 0;
            bDir.y = 0;
            return Vector3.Angle(aDir, bDir);
        }

        private float findAngleFromLook(Vector3 end)
        {
            Vector3 origin = BotOwner.WeaponRoot.position;
            Vector3 aDir = Bot.LookDirection;
            Vector3 bDir = end - origin;
            aDir.y = 0;
            bDir.y = 0;
            return Vector3.Angle(aDir, bDir);
        }

        private void trackMovement()
        {
            if (nextCheckPosTime < Time.time)
            {
                nextCheckPosTime = Time.time + 0.5f;
                Vector3 botPos = BotPosition;
                positionMoving = (botPos - lastCheckPos).sqrMagnitude > _moveSettings.BotSprintNotMovingThreshold;
                if (positionMoving)
                {
                    _timeNotMoving = -1f;
                    lastCheckPos = botPos;
                }
                else if (_timeNotMoving < 0)
                {
                    _timeNotMoving = Time.time;
                }
            }
        }

        private bool positionMoving;
        private Vector3 lastCheckPos;
        private float nextCheckPosTime;
        private float timeSinceNotMoving => positionMoving ? 0f : Time.time - _timeNotMoving;
        private float _timeNotMoving;

        private void updateVoxel()
        {
            if (this._nextCheckVoxel < Time.time)
            {
                this._nextCheckVoxel = Time.time + 0.5f;
                BotOwner.AIData.SetPosToVoxel(Bot.Position);
            }
        }

        private float _nextCheckVoxel;

        private Vector3 BotPosition
        {
            get
            {
                Vector3 botPos = Bot.Position;
                if (NavMesh.SamplePosition(botPos, out var hit, 0.5f, -1))
                {
                    botPos = hit.position;
                }
                return botPos;
            }
        }

        private float distanceToCurrentCornerSqr()
        {
            Vector3 dir = CurrentCornerDestination() - BotPosition;
            //dir.y = 0f;
            return dir.sqrMagnitude;
        }

        private IEnumerator firstTurn(Vector3 firstCorner)
        {
            CurrentRunStatus = RunStatus.FirstTurn;
            Bot.Mover.EnableSprintPlayer(false);

            // First step, look towards the path we want to run
            float dotProduct = 0f;
            while (dotProduct < _moveSettings.BotSprintFirstTurnDotThreshold)
            {
                BotOwner.Mover.IsMoving = true;
                Vector3 targetLookDir = firstCorner - Bot.Position;
                targetLookDir.y = 0f;
                dotProduct = steer(firstCorner, _moveSettings.BotSprintFirstTurnSpeed);
                if (!BotOwner.DoorOpener.Interacting)
                {
                    move(targetLookDir);
                }
                yield return null;
            }
        }

        private float steer(Vector3 target, float turnSpeed)
        {
            Vector3 playerPosition = Bot.Position;
            Vector3 currentLookDirNormal = Bot.Person.Transform.LookDirection.normalized;
            target += Vector3.up;
            Vector3 targetLookDir = (target - playerPosition);
            Vector3 targetLookDirNormal = targetLookDir.normalized;

            if (!BotOwner.DoorOpener.Interacting)
            {
                if (shallLookAtEnemy())
                {
                    Bot.Steering.LookToEnemy(Bot.Enemy);
                }
                else if (!shallSteerbyPriority() || !Bot.Steering.SteerByPriority(false, true))
                {
                    Bot.Steering.LookToDirection(targetLookDirNormal, true, turnSpeed);
                }
            }
            float dotProduct = Vector3.Dot(targetLookDirNormal, currentLookDirNormal);
            return dotProduct;
        }

        private bool shallSteerbyPriority()
        {
            switch (CurrentRunStatus)
            {
                case RunStatus.Turning:
                case RunStatus.FirstTurn:
                case RunStatus.Running:
                case RunStatus.ShortCorner:
                    return false;

                default:
                    return true;
            }
        }

        private void move(Vector3 direction)
        {
            updateVoxel();
            Player.CharacterController.SetSteerDirection(direction);
            BotOwner.AimingData?.Move(Player.Speed);
            Player.Move(findMoveDirection(direction));
        }

        public Vector2 findMoveDirection(Vector3 direction)
        {
            Vector3 vector = Quaternion.Euler(0f, 0f, Player.Rotation.x) * new Vector2(direction.x, direction.z);
            return new Vector2(vector.x, vector.y);
        }

        public void Dispose()
        {
        }

        private bool IsSprintEnabled => Player.IsSprintEnabled;
    }

    public enum ESprintUrgency
    {
        None = 0,
        Low = 1,
        Middle = 2,
        High = 3,
    }
}