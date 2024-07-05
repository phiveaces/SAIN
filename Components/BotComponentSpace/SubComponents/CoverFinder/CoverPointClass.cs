using EFT;
using EFT.Game.Spawning;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverPoint
    {
        public CoverPoint(BotComponent sain, Vector3 point, Collider collider, NavMeshPath pathToPoint, float pathLength)
        {
            Bot = sain;
            TimeCreated = Time.time;
            Collider = collider;
            Vector3 size = collider.bounds.size;
            CoverHeight = size.y;
            CoverValue = (size.x + size.y + size.z).Round10();
            Position = point;
            PathToPoint = pathToPoint;
            PathLength = pathLength;
        }

        public bool IsCurrent => Bot.Cover.CoverInUse == this;

        private readonly BotComponent Bot;

        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                DirectionToCollider = ColliderPosition - value;
                DirectionToColliderNormal = DirectionToCollider.normalized;
                _position = value;
                TimeLastUpdated = Time.time;
            }
        }

        private Vector3 _position;

        public Vector3 DirectionToCollider { get; private set; }
        public Vector3 DirectionToColliderNormal { get; private set; }
        public Vector3 ColliderPosition => Collider.transform.position;

        public float TimeLastUpdated;
        public bool ShallUpdate => Time.time - TimeLastUpdated > (IsCurrent ? 0.2f : 0.5f);

        public int RoundedPathLength { get; private set; }

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

        private float _pathLength;

        public CoverStatus Status
        {
            get
            {
                float distance = Distance;
                float pathLength = PathLength;
                CoverStatus status;

                if (_lastStatus == CoverStatus.InCover &&
                    distance <= InCoverStayDist){
                    return CoverStatus.InCover;
                }

                if (distance <= InCoverDist) {
                    status = CoverStatus.InCover;
                }
                else if (pathLength <= InCoverDist) {
                    status = CoverStatus.InCover;
                }
                else if (pathLength <= CloseCoverDist) {
                    status = CoverStatus.CloseToCover;
                }
                else if (pathLength <= MidCoverDist) {
                    status = CoverStatus.MidRangeToCover;
                }
                else {
                    status = CoverStatus.FarFromCover;
                }

                _lastStatus = status;
                return status;
            }
        }

        private CoverStatus _lastStatus;

        public bool Spotted => HitInCoverCount > 2 || HitInCoverUnknownCount > 0 || HitInCoverCantSeeCount > 1;

        public int HitInCoverUnknownCount = 0;

        public int HitInCoverCantSeeCount = 0;

        public int HitInCoverCount = 0;

        public float Distance
        {
            get
            {
                if (_nextGetDistTime > Time.time)
                    return _distance;

                _nextGetDistTime = Time.time + 0.1f;
                _distance = (Position - Bot.Position).magnitude;
                return _distance;
            }
        }

        private float _distance;
        private float _nextGetDistTime;

        public bool IsBad { get; set; } = false;

        public float CoverValue { get; private set; }

        public NavMeshPath PathToPoint { get; private set; }

        public float CoverHeight { get; private set; }

        public bool BotInThisCover()
        {
            return Status == CoverStatus.InCover;
        }

        private const float InCoverDist = 1f;
        private const float InCoverStayDist = 1.25f;
        private const float CloseCoverDist = 10f;
        private const float MidCoverDist = 20f;

        public Collider Collider { get; private set; }
        public float TimeCreated { get; private set; }
    }
}