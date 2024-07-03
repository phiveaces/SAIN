using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Search
{
    public enum EPathCalcFailReason
    {
        None,
        NullDestination,
        NoTarget,
        NullPlace,
        TooClose,
        SampleStart,
        SampleEnd,
        CalcPath,
        LastCorner,
    }

    public class SearchPathFinder : BotSubClass<SAINSearchClass>
    {
        public Vector3? FinalDestination { get; private set; }
        public EnemyPlace TargetPlace { get; private set; }
        public BotPeekPlan? PeekPoints { get; private set; }
        public bool SearchedTargetPosition { get; private set; }
        public bool FinishedPeeking { get; set; }

        private EPathCalcFailReason _failReason;

        public SearchPathFinder(SAINSearchClass searchClass) : base(searchClass)
        {
            _searchPath = new NavMeshPath();
        }

        public bool HasPathToSearchTarget(out EPathCalcFailReason failReason, bool needTarget = true)
        {
            if (_nextCheckSearchTime < Time.time)
            {
                _nextCheckSearchTime = Time.time + 1f;
                _canStartSearch = hasPath(needTarget, out failReason);
                _failReason = failReason;
            }
            failReason = _failReason;
            return _canStartSearch;
        }

        private bool hasPath(bool needTarget, out EPathCalcFailReason failReason)
        {
            EnemyPlace destination = FindTargetPlace(out bool hasTarget);
            if (destination == null)
            {
                failReason = EPathCalcFailReason.NullDestination;
                return false;
            }
            if (needTarget && !hasTarget)
            {
                failReason = EPathCalcFailReason.NoTarget;
                return false;
            }
            if (!CalculatePath(destination, out failReason))
            {
                return false;
            }
            return true;
        }

        private bool checkBotZone(Vector3 target)
        {
            if (Bot.Memory.Location.BotZoneCollider != null)
            {
                Vector3 closestPointInZone = Bot.Memory.Location.BotZoneCollider.ClosestPointOnBounds(target);
                float distance = (target - closestPointInZone).sqrMagnitude;
                if (distance > 50f * 50f)
                {
                    return false;
                }
            }
            return true;
        }

        public void UpdateSearchDestination()
        {
            checkFinishedSearch();

            if (_nextCheckPosTime < Time.time || SearchedTargetPosition || FinishedPeeking || FinalDestination == null)
            {
                _nextCheckPosTime = Time.time + 4f;

                EnemyPlace newPlace = FindTargetPlace(out bool hasTarget);
                if (newPlace == null || !hasTarget)
                {
                    FinalDestination = null;
                    return;
                }
                if (PeekPoints != null && 
                    FinalDestination != null &&
                    (newPlace.Position - FinalDestination.Value).sqrMagnitude < 2f * 2f)
                {
                    return;
                }
                if (!CalculatePath(newPlace, out EPathCalcFailReason failReason))
                {
                    Logger.LogDebug($"Failed to calc path during search for reason: [{failReason}]");
                    FinalDestination = null;
                }
            }
        }

        private void checkFinishedSearch()
        {
            if (FinalDestination == null)
            {
                SearchedTargetPosition = true;
                return;
            }
            if (_nextCheckFinishTime > Time.time)
            {
                return;
            }
            _nextCheckFinishTime = Time.time + 0.25f;
            if (!SearchedTargetPosition && (FinalDestination.Value - Bot.Position).sqrMagnitude < 1f)
            {
                SearchedTargetPosition = true;
            }
        }

        public EnemyPlace FindTargetPlace(out bool hasTarget, bool randomSearch = false)
        {
            hasTarget = false;
            var enemy = Bot.Enemy;
            if (enemy != null && (enemy.Seen || enemy.Heard))
            {
                if (enemy.IsVisible)
                {
                    hasTarget = true;
                    return enemy.KnownPlaces.LastSeenPlace;
                }

                var knownPlaces = enemy.KnownPlaces.AllEnemyPlaces;
                for (int i = 0; i < knownPlaces.Count; i++)
                {
                    EnemyPlace enemyPlace = knownPlaces[i];
                    if (enemyPlace != null &&
                        !enemyPlace.HasArrivedPersonal)
                    {
                        hasTarget = true;
                        return enemyPlace;
                    }
                }
                if (randomSearch)
                {
                    //return RandomSearch();
                }
            }
            return null;
        }

        private Vector3 RandomSearch()
        {
            float dist = (RandomSearchPoint - BotOwner.Position).sqrMagnitude;
            if (dist < ComeToRandomDist * ComeToRandomDist || dist > 60f * 60f)
            {
                RandomSearchPoint = GenerateSearchPoint();
            }
            return RandomSearchPoint;
        }

        private Vector3 RandomSearchPoint = Vector3.down * 300f;

        private Vector3 GenerateSearchPoint()
        {
            Vector3 start = Bot.Position;
            float dispersion = 30f;
            for (int i = 0; i < 10; i++)
            {
                float dispNum = EFTMath.Random(-dispersion, dispersion);
                Vector3 vector = new Vector3(start.x + dispNum, start.y, start.z + dispNum);
                if (NavMesh.SamplePosition(vector, out var hit, 10f, -1))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(hit.position, start, -1, path))
                    {
                        return path.corners[path.corners.Length - 1];
                    }
                }
            }
            return start;
        }

        public void Reset()
        {
            _searchPath.ClearCorners();
            PeekPoints?.DisposeDebug();
            FinalDestination = null;
            PeekPoints = null;
            TargetPlace = null;
            SearchedTargetPosition = false;
            FinishedPeeking = false;
        }

        public bool CalculatePath(EnemyPlace place, out EPathCalcFailReason failReason)
        {
            if (place == null)
            {
                failReason = EPathCalcFailReason.NullPlace;
                return false;
            }

            Vector3 point = place.Position;
            Vector3 start = Bot.Position;
            if ((point - start).sqrMagnitude <= 0.5f)
            {
                failReason = EPathCalcFailReason.TooClose;
                return false;
            }

            if (!NavMesh.SamplePosition(point, out var hit, 5f, -1))
            {
                failReason = EPathCalcFailReason.SampleEnd;
                return false;
            }
            if (!NavMesh.SamplePosition(start, out var hit2, 1f, -1))
            {
                failReason = EPathCalcFailReason.SampleStart;
                return false;
            }
            _searchPath.ClearCorners();
            if (!NavMesh.CalculatePath(hit2.position, hit.position, -1, _searchPath))
            {
                failReason = EPathCalcFailReason.CalcPath;
                return false;
            }
            Vector3? lastCorner = _searchPath.LastCorner();
            if (lastCorner == null)
            {
                failReason = EPathCalcFailReason.LastCorner;
                return false;
            }

            BaseClass.Reset();
            FinalDestination = lastCorner;
            PeekPoints = findPeekPosition(hit2.position);
            TargetPlace = place;
            failReason = EPathCalcFailReason.None;
            return true;
        }

        private BotPeekPlan? findPeekPosition(Vector3 start)
        {
            findNewCorners();
            int count = newCorners.Count;

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 A = newCorners[i];
                Vector3 ADirection = A - start;
                Vector3 B = newCorners[i + 1];
                Vector3 BDirection = B - start;

                Vector3 startPeekPos = GetPeekStartAndEnd(A, B, ADirection, BDirection, out var endPeekPos);
                if (NavMesh.SamplePosition(startPeekPos, out var hit3, 5f, -1))
                {
                    newCorners.Clear();
                    return new BotPeekPlan(hit3.position, endPeekPos, B, A);
                }
            }
            newCorners.Clear();
            return null;
        }

        private void findNewCorners()
        {
            var corners = _searchPath.corners;
            int cornerLength = corners.Length;
            newCorners.Clear();

            for (int i = 0; i < cornerLength - 1; i++)
            {
                Vector3 corner = corners[i];
                if ((corner - corners[i + 1]).sqrMagnitude > 1.5f)
                {
                    newCorners.Add(corner);
                }
            }

            Vector3? last = corners.LastElement();
            if (last != null)
                newCorners.Add(last.Value);
        }

        private readonly List<Vector3> newCorners = new List<Vector3>();

        private Vector3 GetPeekStartAndEnd(Vector3 blindCorner, Vector3 dangerPoint, Vector3 dirToBlindCorner, Vector3 dirToBlindDest, out Vector3 peekEnd)
        {
            const float maxMagnitude = 6f;
            const float minMagnitude = 1f;
            const float OppositePointMagnitude = 3f;

            Vector3 directionToStart = BotOwner.Position - blindCorner;

            Vector3 cornerStartDir;
            if (directionToStart.magnitude > maxMagnitude)
            {
                cornerStartDir = directionToStart.normalized * maxMagnitude;
            }
            else if (directionToStart.magnitude < minMagnitude)
            {
                cornerStartDir = directionToStart.normalized * minMagnitude;
            }
            else
            {
                cornerStartDir = Vector3.zero;
            }

            Vector3 PeekStartPosition = blindCorner + cornerStartDir;
            Vector3 directionToDangerPoint = dangerPoint - PeekStartPosition;

            // Rotate to the opposite side depending on the angle of the danger point to the start.
            float signAngle = GetSignedAngle(dirToBlindCorner.normalized, directionToDangerPoint.normalized);
            float rotationAngle = signAngle > 0 ? -90f : 90f;
            Quaternion rotation = Quaternion.Euler(0f, rotationAngle, 0f);

            var direction = rotation * dirToBlindDest.normalized;
            direction *= OppositePointMagnitude;

            CheckForObstacles(PeekStartPosition, direction, out Vector3 result);
            peekEnd = result;
            return PeekStartPosition;
        }

        private float GetSignedAngle(Vector3 dirCenter, Vector3 dirOther)
        {
            return Vector3.SignedAngle(dirCenter, dirOther, Vector3.up);
        }

        private void CheckForObstacles(Vector3 start, Vector3 direction, out Vector3 result)
        {
            if (!NavMesh.SamplePosition(start, out var startHit, 5f, -1))
            {
                result = start + direction;
                return;
            }
            direction.y = 0f;
            if (!NavMesh.Raycast(startHit.position, direction, out var rayHit, -1))
            {
                result = startHit.position + direction;
                if (NavMesh.SamplePosition(result, out var endHit, 5f, -1))
                {
                    result = endHit.position;
                }
                return;
            }
            result = rayHit.position;
        }

        private float _nextCheckFinishTime;
        private const float ComeToRandomDist = 1f;
        private bool _canStartSearch;
        private float _nextCheckSearchTime;
        private float _nextCheckPosTime;
        private NavMeshPath _searchPath;
    }
}