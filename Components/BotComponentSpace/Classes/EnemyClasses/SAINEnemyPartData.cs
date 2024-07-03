using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyPartData
    {
        public SAINEnemyPartData(EBodyPart bodyPart, BifacialTransform transform, List<BodyPartCollider> colliders, bool isYourPlayer)
        {
            BodyPart = bodyPart;
            Transform = transform;
            Colliders = colliders;
            _indexMax = colliders.Count - 1;
            IsYourPlayer = isYourPlayer;
        }

        public Vector3 Position
        {
            get
            {
                if (Time.frameCount != this._posCachedForFrame && 
                    Transform != null && 
                    Transform.Original != null)
                {
                    this._position = this.Transform.position;
                    this._posCachedForFrame = Time.frameCount;
                }
                return this._position;
            }
        }

        public Vector3? LastSuccessPoint => _lastSuccessCastPoint;

        private bool IsYourPlayer;
        private Vector3 _position;

        private int _posCachedForFrame;

        public readonly EBodyPart BodyPart;
        public readonly List<BodyPartCollider> Colliders;
        public readonly BifacialTransform Transform;

        public bool LineOfSight => _lastSuccessTime + 0.25f > Time.time;

        private float _nextCheckTime;

        private BodyPartCollider getCollider()
        {
            if (_lastSuccessPart != null)
            {
                return _lastSuccessPart;
            }

            BodyPartCollider collider = Colliders[_index];
            _index++;
            if (_index > _indexMax)
            {
                _index = 0;
            }
            return collider;
        }

        private int _index;
        private readonly int _indexMax;

        public bool CheckLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            if (LineOfSight)
            {
                successPoint = _lastSuccessCastPoint;
                return true;
            }

            //if (_nextCheckTime > Time.time)
            //{
            //    successPoint = null;
            //    return false;
            //}
            //_nextCheckTime = Time.time + 0.1f;

            BodyPartCollider collider = getCollider();
            Vector3 castPoint = getCastPoint(origin, collider);
            //Vector3 castPoint = _lastSuccessCastPoint ?? getCastPoint(origin, collider);

            Vector3 direction = castPoint - origin;

            float maxRayDistance = Mathf.Clamp(direction.magnitude, 0f, maxRange);
            bool lineOfSight = !Physics.Raycast(origin, direction, maxRayDistance, LayerMaskClass.HighPolyWithTerrainMask);

            if (lineOfSight)
            {
                _lastSuccessTime = Time.time;
                _lastSuccessPart = collider;
                _lastSuccessCastPoint = castPoint;
            }
            else
            {
                _lastSuccessTime = 0f;
                _lastSuccessPart = null;
                _lastSuccessCastPoint = null;
            }

            successPoint = _lastSuccessCastPoint;

            if (SAINPlugin.DebugMode &&
                IsYourPlayer &&
                _nextdrawTime < Time.time)
            {
                _nextdrawTime = Time.time + 0.1f;
                if (lineOfSight)
                {
                    DebugGizmos.Sphere(castPoint, 0.025f, Color.red, true, 10f);
                    DebugGizmos.Sphere(origin, 0.025f, Color.red, true, 1f);
                    DebugGizmos.Line(castPoint, origin, Color.red, 0.005f, true, 0.5f);
                    Logger.LogDebug($"{BodyPart} : {direction.magnitude} : {castPoint} : Is Sphere? {_isSphereCollider}");
                }
                else
                {
                    DebugGizmos.Sphere(castPoint, 0.025f, Color.white, true, 10f);
                    DebugGizmos.Sphere(origin, 0.025f, Color.white, true, 1f);
                    DebugGizmos.Line(castPoint, origin, Color.white, 0.005f, true, 0.5f);
                }
            }

            return lineOfSight;
        }

        private float _nextdrawTime;

        private float _lastSuccessTime;

        private Vector3 getCastPoint(Vector3 origin, BodyPartCollider collider)
        {
            SphereCollider sphere;
            _isSphereCollider = collider.Collider != null && 
                (sphere = collider.Collider as SphereCollider) != null;

            if (_isSphereCollider)
            {
                return collider.GetRandomPointToCastLocal(origin);
            }
            return Position;
        }

        private bool _isSphereCollider;

        private BodyPartCollider _lastSuccessPart;
        private Vector3? _lastSuccessCastPoint;
    }
}