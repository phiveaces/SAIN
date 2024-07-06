using EFT;
using EFT.Game.Spawning;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverPoint
    {
        public event Action<Vector3> OnPositionUpdated;

        private const int SPOTTED_HITINCOVER_COUNT_TOTAL = 3;
        private const int SPOTTED_HITINCOVER_COUNT_THIRDPARTY = 1;
        private const int SPOTTED_HITINCOVER_COUNT_CANTSEE = 2;
        private const int SPOTTED_HITINCOVER_COUNT_LEGS = 2;
        private const int SPOTTED_HITINCOVER_COUNT_UNKNOWN = 1;
        private const float HITINCOVER_MAX_DAMAGE = 120f;
        private const float HITINCOVER_MIN_DAMAGE = 40f;
        private const float HITINCOVER_DAMAGE_COEF = 3f;
        private const float CHECKDIST_MAX_DIST = 50;
        private const float CHECKDIST_MIN_DIST = 10;
        private const float CHECKDIST_MAX_DELAY = 1f;
        private const float CHECKDIST_MIN_DELAY = 0.1f;
        private const float DIST_COVER_INCOVER = 1f;
        private const float DIST_COVER_INCOVER_STAY = 1.25f;
        private const float DIST_COVER_CLOSE = 10f;
        private const float DIST_COVER_MID = 20f;

        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                if ((value - _position).sqrMagnitude < 0.001)
                {
                    //Logger.LogWarning($"new Pos is the same as old pos!");
                    return;
                }
                updateDirAndPos(value);
                OnPositionUpdated?.Invoke(value);
            }
        }
        public float Distance
        {
            get
            {
                if (_nextGetDistTime < Time.time)
                {
                    float dist = (Position - Bot.Position).magnitude;
                    _distance = dist;
                    _nextGetDistTime = Time.time + calcMagnitudeDelay(dist);
                }
                return _distance;
            }
        }
        public float PathLength
        {
            get
            {
                return _pathLength;
            }
            set
            {
                RoundedPathLength = Mathf.RoundToInt(value);
                _pathLength = value;
            }
        }

        public bool Spotted
        {
            get
            {
                // are we already spotted? check if it has expired
                if (_spotted)
                {
                    if (Time.time - _timeSpotted > SpottedCoverPoint.SPOTTED_PERIOD)
                        ResetGetHit();

                    return _spotted;
                }

                // we aren't currently spotted, check to make sure we weren't hit too many times
                _spotted = checkSpotted();
                if (_spotted)
                    _timeSpotted = Time.time;

                return _spotted;
            }
        }
        public CoverStatus StraightDistanceStatus
        {
            get
            {
                float distance = Distance;
                if (_lastStraightDistStatus == CoverStatus.InCover &&
                    distance <= DIST_COVER_INCOVER_STAY)
                {
                    return CoverStatus.InCover;
                }
                _lastStraightDistStatus = checkStatus(distance);
                return _lastStraightDistStatus;
            }
        }

        public CoverStatus PathDistanceStatus
        {
            get
            {
                float pathLength = PathLength;
                if (_lastPathDistStatus == CoverStatus.InCover &&
                    pathLength <= DIST_COVER_INCOVER_STAY)
                {
                    return CoverStatus.InCover;
                }
                _lastPathDistStatus = checkStatus(pathLength);
                return _lastPathDistStatus;
            }
        }

        public float CoverValue { get; private set; }
        public NavMeshPath PathToPoint { get; private set; }
        public float CoverHeight { get; private set; }
        public Collider Collider { get; private set; }
        public float TimeCreated { get; private set; }
        public bool IsBad { get; set; } = false;
        public float LastHitInCoverTime { get; private set; }
        public int Id { get; }
        public bool IsCurrent => Bot.Cover.CoverInUse == this;
        public bool ShallUpdate => Time.time - _timeUpdated > (IsCurrent ? 0.2f : 0.5f);
        public Vector3 DirectionToCollider { get; private set; }
        public Vector3 DirectionToColliderNormal { get; private set; }
        public Vector3 ColliderPosition { get; }
        public int RoundedPathLength { get; private set; }
        public bool BotInThisCover => IsCurrent && (StraightDistanceStatus == CoverStatus.InCover || PathDistanceStatus == CoverStatus.InCover);

        public void GetHit(DamageInfo damageInfo, EBodyPart partHit, Enemy currentEnemy)
        {
            int hitCount = calcHitCount(damageInfo);
            bool islegs = partHit.isLegs();

            var hits = _hitsInCover;
            LastHitInCoverTime = Time.time;
            hits.Total += hitCount;

            IPlayer hitFrom = damageInfo.Player?.iPlayer;
            if (currentEnemy == null || hitFrom == null)
            {
                hits.Unknown += hitCount;
                return;
            }

            bool sameEnemy = currentEnemy.EnemyPlayer.ProfileId == hitFrom.ProfileId;
            if (!sameEnemy)
            {
                Enemy thirdParty = Bot.EnemyController.GetEnemy(hitFrom.ProfileId, false);
                if (thirdParty == null)
                {
                    hits.Unknown += hitCount;
                    return;
                }

                // Did I get shot in the legs and can't see them?
                if (islegs && thirdParty.IsVisible == false)
                    hits.Legs += hitCount;

                // Did the player who shot me shoot me from a direction that this cover doesn't protect from?
                if (Vector3.Dot(thirdParty.EnemyDirectionNormal, DirectionToColliderNormal) < 0.25f)
                    hits.ThirdParty += hitCount;

                return;
            }

            if (!currentEnemy.IsVisible)
            {
                if (islegs)
                    hits.Legs += hitCount;

                hits.CantSee += hitCount;
            }
        }

        public void ResetGetHit()
        {
            _spotted = false; 
            _hitsInCover = new CoverHitCounts();
        }

        public CoverPoint(BotComponent sain, Vector3 point, Collider collider, NavMeshPath pathToPoint, float pathLength)
        {
            Id = _count;
            _count++;

            Bot = sain;
            TimeCreated = Time.time;

            Collider = collider;
            ColliderPosition = collider.transform.position;
            Vector3 size = collider.bounds.size;
            CoverHeight = size.y;
            CoverValue = (size.x + size.y + size.z).Round10();

            PathToPoint = pathToPoint;
            PathLength = pathLength;

            updateDirAndPos(point);
        }

        private void updateDirAndPos(Vector3 value)
        {
            Vector3 dir = ColliderPosition - value;
            dir.y = 0;
            DirectionToCollider = dir;
            _distance = dir.magnitude;
            DirectionToColliderNormal = dir.normalized;
            _position = value;
            _timeUpdated = Time.time;
        }

        private CoverStatus checkStatus(float distance)
        {
            if (distance <= DIST_COVER_INCOVER)
                return CoverStatus.InCover;

            if (distance <= DIST_COVER_CLOSE)
                return CoverStatus.CloseToCover;

            if (distance <= DIST_COVER_MID)
                return CoverStatus.MidRangeToCover;

            return CoverStatus.FarFromCover;
        }

        private bool checkSpotted()
        {
            var hits = _hitsInCover;
            int total = hits.Total;
            if (total == 0) return false;

            if (total >= SPOTTED_HITINCOVER_COUNT_TOTAL)
                return true;

            if (hits.CantSee >= SPOTTED_HITINCOVER_COUNT_CANTSEE)
                return true;

            if (hits.Unknown >= SPOTTED_HITINCOVER_COUNT_UNKNOWN)
                return true;

            if (hits.ThirdParty >= SPOTTED_HITINCOVER_COUNT_THIRDPARTY)
                return true;

            if (hits.Legs >= SPOTTED_HITINCOVER_COUNT_LEGS)
                return true;

            return false;
        }

        private int calcHitCount(DamageInfo damageInfo)
        {
            float received = damageInfo.Damage;
            float max = HITINCOVER_MAX_DAMAGE;
            float maxCoef = HITINCOVER_DAMAGE_COEF;
            if (received >= max)
            {
                return Mathf.RoundToInt(maxCoef);
            }
            float min = HITINCOVER_MIN_DAMAGE;
            float minCoef = 1f;
            if (received <= min)
            {
                return Mathf.RoundToInt(minCoef);
            }

            float num = max - min;
            float diff = received - min;
            float result = Mathf.Lerp(min, max, diff / num);
            return Mathf.RoundToInt(result);
        }

        private float calcMagnitudeDelay(float dist)
        {
            float maxDelay = CHECKDIST_MAX_DELAY;
            float maxDist = CHECKDIST_MAX_DIST;
            if (dist >= maxDist)
            {
                return maxDelay;
            }
            float minDelay = CHECKDIST_MIN_DELAY;
            float minDist = CHECKDIST_MIN_DIST;
            if (dist <= minDist)
            {
                return minDelay;
            }
            float num = maxDist - minDist;
            float diff = dist - minDist;
            float result = Mathf.Lerp(minDelay, maxDelay, diff / num);
            return result;
        }

        private float _distance;
        private float _nextGetDistTime;

        private CoverHitCounts _hitsInCover = new CoverHitCounts();

        private static int _count;
        private readonly BotComponent Bot;
        private Vector3 _position;
        private float _timeUpdated;
        private float _pathLength;
        private CoverStatus _lastPathDistStatus;
        private CoverStatus _lastStraightDistStatus;
        private float _timeSpotted;
        private bool _spotted;
    }

    public struct CoverHitCounts
    {
        public int Total;
        public int Unknown;
        public int ThirdParty;
        public int CantSee;
        public int Legs;
    }
}