using SAIN.Helpers;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverAnalyzer : BotBase
    {
        public CoverAnalyzer(BotComponent botOwner, CoverFinderComponent coverFinder) : base(botOwner)
        {
            CoverFinder = coverFinder;
        }

        private readonly CoverFinderComponent CoverFinder;

        public bool CheckCollider(Collider collider, out CoverPoint newPoint, out ECoverFailReason failReason)
        {
            if (!GetPlaceToMove(collider, TargetPoint, OriginPoint, out Vector3 place))
            {
                failReason = ECoverFailReason.NoPlaceToMove;
                newPoint = null;
                return false;
            }

            if (!CheckPosition(place, collider.transform.position))
            {
                failReason = ECoverFailReason.BadPosition;
                newPoint = null;
                return false;
            }

            NavMeshPath path = new NavMeshPath();
            if (!CheckPath(place, path, out float pathLength))
            {
                failReason = ECoverFailReason.BadPath;
                newPoint = null;
                return false;
            }

            failReason = ECoverFailReason.None;
            newPoint = new CoverPoint(Bot, place, collider, path, pathLength);

            return true;
        }

        public bool RecheckCoverPoint(CoverPoint coverPoint, out ECoverFailReason failReason)
        {
            failReason = ECoverFailReason.None;

            if (!GetPlaceToMove(coverPoint.Collider, TargetPoint, OriginPoint, out Vector3 newPosition)) {
                failReason = ECoverFailReason.NoPlaceToMove;
                return false;
            }

            coverPoint.Position = newPosition;

            if (coverPoint.StraightDistanceStatus == CoverStatus.InCover) {
                return true;
            }

            if (!CheckPosition(newPosition, coverPoint.Collider.transform.position)) {
                failReason = ECoverFailReason.BadPosition;
                return false;
            }

            NavMeshPath path = coverPoint.PathToPoint;
            path.ClearCorners();
            if (!CheckPath(newPosition, path, out float pathLength))
            {
                failReason = ECoverFailReason.BadPath;
                return false;
            }
            coverPoint.PathLength = pathLength;
            return true;
        }

        public static bool GetPlaceToMove(Collider collider, Vector3 targetPosition, Vector3 botPosition, out Vector3 place, float navSampleRange = 1f)
        {
            if (collider == null)
            {
                place = Vector3.zero;
                return false;
            }

            if (!CheckColliderDirection(collider, targetPosition, botPosition))
            {
                place = Vector3.zero;
                return false;
            }

            Vector3 colliderPos = collider.transform.position;

            // The direction from the target to the collider
            Vector3 colliderDir = (colliderPos - targetPosition).normalized;
            colliderDir.y = 0f;

            // a farPoint on opposite side of the target
            if (!NavMesh.SamplePosition(colliderPos + colliderDir, out var hit, navSampleRange, -1))
            {
                place = Vector3.zero;
                return false;
            }

            place = hit.position;

            // the closest edge to that farPoint
            if (NavMesh.FindClosestEdge(place, out var edge, -1)
                && NavMesh.SamplePosition(edge.position + colliderDir, out var hit2, navSampleRange, -1))
            {
                //Logger.LogDebug("Found Edge");
                place = hit2.position;
            }
            return true;
        }

        private static bool CheckColliderDirection(Collider collider, Vector3 targetPosition, Vector3 botPosition)
        {
            Vector3 pos = collider.transform.position;

            Vector3 directionToTarget = targetPosition - botPosition;
            float targetDist = directionToTarget.magnitude;

            Vector3 directionToCollider = pos - botPosition;
            float colliderDist = directionToCollider.magnitude;

            float dot = Vector3.Dot(directionToTarget.normalized, directionToCollider.normalized);

            if (dot <= 0.33f)
            {
                return true;
            }
            if (dot <= 0.6f)
            {
                return colliderDist < targetDist * 0.75f;
            }
            if (dot <= 0.8f)
            {
                return colliderDist < targetDist * 0.5f;
            }
            return colliderDist < targetDist * 0.25f;
        }

        private bool CheckPosition(Vector3 position, Vector3 colliderPosition)
        {
            return (position - TargetPoint).sqrMagnitude > CoverMinEnemyDistSqr &&
                !isPositionSpotted(position) &&
                checkPositionVsOtherBots(position) &&
                visibilityCheck(position, TargetPoint, colliderPosition);
        }

        private bool isPositionSpotted(Vector3 position)
        {
            foreach (var point in CoverFinder.SpottedCoverPoints)
            {
                Vector3 coverPos = point.CoverPoint.Position;
                if (!point.IsValidAgain &&
                    point.TooClose(coverPos, position))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckPath(Vector3 position, NavMeshPath pathToPoint, out float pathLength)
        {
            NavMesh.CalculatePath(OriginPoint, position, -1, pathToPoint);

            //if (!NavMesh.CalculatePath(OriginPoint, position, -1, pathToPoint))
            //{
            //    pathLength = 0;
            //    return false;
            //}

            //if (pathToPoint.status == NavMeshPathStatus.PathPartial &&
            //    (position - pathToPoint.corners[pathToPoint.corners.Length - 1]).sqrMagnitude > 1f)
            //{
            //    pathLength = 0;
            //    return false;
            //}

            if (pathToPoint.status != NavMeshPathStatus.PathComplete)
            {
                pathLength = 0;
                return false;
            }

            pathLength = pathToPoint.CalculatePathLength();
            if (pathLength > SAINPlugin.LoadedPreset.GlobalSettings.Cover.MaxCoverPathLength)
            {
                return false;
            }

            if (!checkPathToEnemy(pathToPoint))
            {
                return false;
            }

            return true;
        }

        private bool checkPathToEnemy(NavMeshPath path)
        {
            Enemy enemy = Bot.Enemy;
            if (enemy != null
                && enemy.Path.PathToEnemy != null
                && !SAINBotSpaceAwareness.ArePathsDifferent(path, enemy.Path.PathToEnemy, 0.5f, 0.1f))
            {
                return false;
            }
            for (int i = 1; i < path.corners.Length - 1; i++)
            {
                var corner = path.corners[i];
                Vector3 cornerToTarget = TargetPoint - corner;
                Vector3 botToTarget = TargetPoint - OriginPoint;
                Vector3 botToCorner = corner - OriginPoint;

                if (cornerToTarget.magnitude < 0.5f)
                {
                    if (DebugCoverFinder)
                    {
                        //DrawDebugGizmos.Ray(OriginPoint, corner - OriginPoint, Color.red, (corner - OriginPoint).magnitude, 0.05f, true, 30f);
                    }

                    return false;
                }

                if (i == 1)
                {
                    if (Vector3.Dot(botToCorner.normalized, botToTarget.normalized) > 0.5f)
                    {
                        if (DebugCoverFinder)
                        {
                            //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                        }
                        return false;
                    }
                }
                else if (i < path.corners.Length - 2)
                {
                    Vector3 cornerB = path.corners[i + 1];
                    Vector3 directionToNextCorner = cornerB - corner;

                    if (Vector3.Dot(cornerToTarget.normalized, directionToNextCorner.normalized) > 0.5f)
                    {
                        if (directionToNextCorner.magnitude > cornerToTarget.magnitude)
                        {
                            if (DebugCoverFinder)
                            {
                                //DrawDebugGizmos.Ray(corner, cornerToTarget, Color.red, cornerToTarget.magnitude, 0.05f, true, 30f);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool checkPositionVsOtherBots(Vector3 position)
        {
            if (Bot.Squad.Members == null || Bot.Squad.Members.Count < 2)
            {
                return true;
            }

            string profileID = Bot.ProfileId;
            foreach (var member in Bot.Squad.Members.Values)
            {
                if (member == null || member.ProfileId == profileID)
                    continue;

                if (isDistanceToClose(member.Cover.CoverInUse, position))
                    return false;
            }

            return true;
        }

        private bool isDistanceToClose(CoverPoint point, Vector3 position)
        {
            const float DistanceToBotCoverThresh = 1f;
            return point != null && (position - point.Position).sqrMagnitude < DistanceToBotCoverThresh;
        }

        private static bool visibilityCheck(Vector3 position, Vector3 target, Vector3 colliderPosition)
        {
            const float offset = 0.1f;

            float distanceToCollider = (colliderPosition - position).magnitude * 1.25f;
            //Logger.LogDebug($"visCheck: Dist To Collider: {distanceToCollider}");

            if (!checkRaycastToCoverCollider(position, target, out RaycastHit hit, distanceToCollider))
            {
                return false;
            }

            Vector3 enemyDirection = target - position;
            enemyDirection = enemyDirection.normalized * offset;
            Quaternion right = Quaternion.Euler(0f, 90f, 0f);
            Vector3 rightPoint = right * enemyDirection;

            if (!checkRaycastToCoverCollider(position + rightPoint, target, out hit, distanceToCollider))
            {
                return false;
            }

            if (!checkRaycastToCoverCollider(position - rightPoint, target, out hit, distanceToCollider))
            {
                return false;
            }

            return true;
        }

        private static bool checkRaycastToCoverCollider(Vector3 point, Vector3 target, out RaycastHit hit, float distance)
        {
            point.y += 0.5f;
            target.y += 1.25f;
            Vector3 direction = target - point;
            bool hitObject = Physics.Raycast(point, direction, out hit, distance, LayerMaskClass.HighPolyWithTerrainMask);

            if (DebugCoverFinder)
            {
                if (hitObject)
                {
                    DebugGizmos.Line(point, hit.point, Color.white, 0.1f, true, 10f);
                }
                else
                {
                    Vector3 testPoint = direction.normalized * distance + point;
                    DebugGizmos.Line(point, testPoint, Color.red, 0.1f, true, 10f);
                }
            }
            return hitObject;
        }

        private Vector3 OriginPoint => CoverFinder.OriginPoint;
        private Vector3 TargetPoint => CoverFinder.TargetPoint;
        private float CoverMinEnemyDistSqr => CoverFinderComponent.CoverMinEnemyDistSqr;
        private static bool DebugCoverFinder => CoverFinderComponent.DebugCoverFinder;
        private static float CoverMinHeight => CoverFinderComponent.CoverMinHeight;
    }
}