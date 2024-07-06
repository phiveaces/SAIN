using SAIN.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class BlindCornerFinder : EnemyBase
    {
        public EnemyCorner? BlindCorner => Enemy.Path.EnemyCorners.GetCorner(ECornerType.Blind);

        public BlindCornerFinder(Enemy enemy) : base(enemy)
        {
        }

        public void ClearBlindCorner()
        {
            Enemy.Path.EnemyCorners.Remove(ECornerType.Blind);
        }

        public IEnumerator FindBlindCorner(Vector3[] corners, Vector3 enemyPosition)
        {
            int count = corners.Length;
            if (count <= 1)
            {
                ClearBlindCorner();
                yield break;
            }

            Stopwatch sw = Stopwatch.StartNew();
            int totalRaycasts = 0;
            const int MAX_CASTS_PER_FRAME = 4;
            const int MAX_ITERATIONS_REAL_CORNER = 15;

            Vector3 lookPoint = Bot.Transform.EyePosition;
            Vector3 lookOffset = lookPoint - Bot.Position;
            float heightOffset = lookOffset.y;

            Vector3 notVisibleCorner = enemyPosition;
            Vector3 lastVisibleCorner = corners[1];

            int raycasts = 0;

            if (count > 2)
            {
                _corners.Clear();
                _corners.AddRange(corners);

                notVisibleCorner = _corners[2];
                lastVisibleCorner = _corners[1];

                for (int i = 1; i < count; i++)
                {
                    raycasts++;
                    Vector3 checkingCorner = _corners[i];
                    if (rayCastToCorner(checkingCorner, lookPoint, heightOffset))
                    {
                        lastVisibleCorner = _corners[i - 1];
                        notVisibleCorner = checkingCorner;
                        break;
                    }
                    if (raycasts >= MAX_CASTS_PER_FRAME)
                    {
                        totalRaycasts += raycasts;
                        raycasts = 0;
                        yield return null;
                    }
                }
                _corners.Clear();
            }

            lastVisibleCorner.y += heightOffset;
            notVisibleCorner.y += heightOffset;
            
            Vector3 pointPastCorner = RaycastPastCorner(lastVisibleCorner, lookPoint, 0f, 10f);
            raycasts++;

            if (raycasts >= MAX_CASTS_PER_FRAME)
            {
                totalRaycasts += raycasts;
                raycasts = 0;
                yield return null;
            }

            float sign = Vector.FindFlatSignedAngle(pointPastCorner, notVisibleCorner, lookPoint);
            float angle = sign <= 0 ? -15f : 15f;
            float rotationStep = angle / MAX_ITERATIONS_REAL_CORNER;

            Vector3 blindCorner = lastVisibleCorner;
            Vector3 directionToBlind = lastVisibleCorner - lookPoint;
            float rayMaxDist = (pointPastCorner - lookPoint).magnitude;

            for (int i = 0; i < MAX_ITERATIONS_REAL_CORNER; i++)
            {
                raycasts++;

                directionToBlind = Vector.Rotate(directionToBlind, 0, rotationStep, 0);

                bool hit = Physics.Raycast(lookPoint, directionToBlind, rayMaxDist, LayerMaskClass.HighPolyWithTerrainMask);
                drawDebug(lookPoint + directionToBlind, lookPoint, hit);

                if (hit)
                {
                    //Logger.LogDebug($"Angle where LOS broken [{rotationStep * i}] after [{i}] iterations");
                    break;
                }

                blindCorner = lookPoint + directionToBlind;

                if (raycasts >= MAX_CASTS_PER_FRAME)
                {
                    totalRaycasts += raycasts;
                    raycasts = 0;
                    yield return null;
                }
            }

            blindCorner.y -= heightOffset;
            Enemy.Path.EnemyCorners.AddOrReplace(ECornerType.Blind, new EnemyCorner(blindCorner, angle));

            if (raycasts > 0)
            {
                totalRaycasts += raycasts;
                raycasts = 0;
                yield return null;
            }
            sw.Stop();
            if (_nextLogTime < Time.time)
            {
                _nextLogTime = Time.time + 5f;
                //float time = (sw.ElapsedMilliseconds / 1000f).Round100();
                //Logger.LogDebug($"Total Raycasts: [{totalRaycasts}] Time To Complete: [{time}] seconds");
            }
        }

        private float _nextLogTime;

        private void drawDebug(Vector3 corner, Vector3 lookPoint, bool hit)
        {
            if (SAINPlugin.DebugMode && SAINPlugin.DebugSettings.DebugDrawBlindCorner)
            {
                Color color = hit ? Color.red : Color.green;
                float lineWidth = 0.01f;
                float expireTime = 30f;

                //float lowerHeight = (Bot.Position - Bot.Transform.EyePosition).y * 0.8f;
                //corner.y += lowerHeight;
                //lookPoint.y += lowerHeight;

                DebugGizmos.Line(corner, lookPoint, color, lineWidth, true, expireTime, true);
            }
        }

        public static Vector3 RaycastPastCorner(Vector3 corner, Vector3 lookPoint, float addHeight, float addDistance = 2f)
        {
            corner.y += addHeight;
            Vector3 cornerDir = corner - lookPoint;
            Vector3 dirPastCorner = cornerDir.normalized * addDistance;

            Vector3 farPoint;
            if (Physics.Raycast(lookPoint, cornerDir, out var hit, addDistance, _mask)) {
                farPoint = hit.point;
            }
            else {
                farPoint = corner + dirPastCorner;
            }
            Vector3 midPoint = Vector3.Lerp(farPoint, corner, 0.5f);
            return midPoint;
        }

        private bool rayCastToCorner(Vector3 corner, Vector3 lookPoint, float heightOffset)
        {
            corner.y += heightOffset;
            Vector3 direction = corner - lookPoint;
            return Physics.Raycast(lookPoint, direction, direction.magnitude, _mask);
        }

        private static readonly LayerMask _mask = LayerMaskClass.HighPolyWithTerrainMask;
        private readonly List<Vector3> _corners = new List<Vector3>();
    }
}