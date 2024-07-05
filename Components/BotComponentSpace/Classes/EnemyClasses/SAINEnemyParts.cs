using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyParts
    {
        public bool LineOfSight => TimeSinceInLineOfSight < 0.2f;
        public float TimeSinceInLineOfSight => Time.time - _timeLastInSight;
        public Vector3 LastSuccessPosition { get; private set; }
        public Dictionary<EBodyPart, SAINEnemyPartData> Parts { get; } = new Dictionary<EBodyPart, SAINEnemyPartData>();

        public void Update()
        {
            var visiblePart = findPartInLOS();
            if (visiblePart != null)
            {
                _timeLastInSight = Time.time;
                if (visiblePart.LastSuccessPoint != null)
                    LastSuccessPosition = visiblePart.LastSuccessPoint.Value;

                return;
            }
        }


        private SAINEnemyPartData findPartInLOS()
        {
            foreach (var part in Parts.Values)
                if (part.LineOfSight)
                    return part;
            return null;
        }

        public bool CheckBodyLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            if (LineOfSight)
            {
                successPoint = LastSuccessPosition;
                return true;
            }

            SAINEnemyPartData checkingPart = Parts[EBodyPart.Chest];
            if (checkingPart.CheckLineOfSight(origin, maxRange, out successPoint))
            {
                if (successPoint != null)
                    LastSuccessPosition = successPoint.Value;

                _lastSuccessTime = Time.time;
                return true;
            }
            successPoint = null;
            return false;
        }

        public bool CheckHeadLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            if (LineOfSight)
            {
                successPoint = LastSuccessPosition;
                return true;
            }

            SAINEnemyPartData checkingPart = Parts[EBodyPart.Head];
            if (checkingPart.CheckLineOfSight(origin, maxRange, out successPoint))
            {
                if (successPoint != null)
                    LastSuccessPosition = successPoint.Value;

                _lastSuccessTime = Time.time;
                return true;
            }
            successPoint = null;
            return false;
        }

        public bool CheckRandomPartLineOfSight(Vector3 origin, float maxRange, out Vector3? successPoint)
        {
            if (LineOfSight)
            {
                successPoint = LastSuccessPosition;
                return true;
            }

            if (_lastCheckSuccessPart != null)
            {
                if (_lastCheckSuccessPart.CheckLineOfSight(origin, maxRange, out successPoint))
                {
                    if (successPoint != null)
                        LastSuccessPosition = successPoint.Value;

                    _lastSuccessTime = Time.time;
                    return true;
                }
                _lastCheckSuccessPart = null;
            }

            SAINEnemyPartData checkingPart = getNextPart();
            if (checkingPart.CheckLineOfSight(origin, maxRange, out successPoint))
            {
                if (successPoint != null)
                    LastSuccessPosition = successPoint.Value;

                _lastCheckSuccessPart = checkingPart;
                _lastSuccessTime = Time.time;
                return true;
            }

            successPoint = null;
            _lastSuccessTime = 0f;
            return false;
        }

        public SAINEnemyParts(PlayerBones bones, bool isYourPlayer)
        {
            IsYourPlayer = isYourPlayer;
            createPartDatas(bones);
            _indexMax = Parts.Count;
        }

        private SAINEnemyPartData getNextPart()
        {
            SAINEnemyPartData result = null;
            EBodyPart epart = (EBodyPart)_index;
            if (!Parts.TryGetValue(epart, out result))
            {
                _index = 0;
                result = Parts[EBodyPart.Chest];
            }

            _index++;
            if (_index > _indexMax)
            {
                _index = 0;
            }

            return result;
        }

        private void createPartDatas(PlayerBones bones)
        {
            foreach (EBodyPart bodyPart in EnumValues.GetEnum<EBodyPart>())
            {
                List<BodyPartCollider> colliders = new List<BodyPartCollider>();
                findParts(bodyPart, out BifacialTransform transform, colliders, bones);
                Parts.Add(bodyPart, new SAINEnemyPartData(bodyPart, transform, colliders, IsYourPlayer));
            }
        }

        private void findParts(EBodyPart bodyPart, out BifacialTransform transform, List<BodyPartCollider> partColliders, PlayerBones bones)
        {
            switch (bodyPart)
            {
                default:
                    transform = bones.BifacialTransforms[PlayerBoneType.Spine];
                    if (transform == null)
                    {
                        Logger.LogError($"Transform Null {PlayerBoneType.Spine}");
                        transform = bones.BifacialTransforms[PlayerBoneType.Body];
                        if (transform == null)
                        {
                            Logger.LogError($"Transform Null {PlayerBoneType.Body}");
                        }
                    }

                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.SpineDown]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.SpineTop]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RibcageUp]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RibcageLow]);
                    break;

                case EBodyPart.Head:
                    transform = bones.BifacialTransforms[PlayerBoneType.Head];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.ParietalHead]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.HeadCommon]);
                    break;

                case EBodyPart.LeftArm:
                    transform = bones.BifacialTransforms[PlayerBoneType.LeftShoulder];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.LeftForearm]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.LeftUpperArm]);
                    break;

                case EBodyPart.RightArm:
                    transform = bones.BifacialTransforms[PlayerBoneType.RightShoulder];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RightForearm]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RightUpperArm]);
                    break;

                case EBodyPart.LeftLeg:
                    transform = bones.BifacialTransforms[PlayerBoneType.LeftThigh1];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.LeftThigh]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.LeftCalf]);
                    break;

                case EBodyPart.RightLeg:
                    transform = bones.BifacialTransforms[PlayerBoneType.RightThigh1];
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RightThigh]);
                    partColliders.Add(bones.BodyPartCollidersDictionary[EBodyPartColliderType.RightCalf]);
                    break;
            }
        }
        private bool IsYourPlayer;
        private float _lastSuccessTime;
        private float _timeLastInSight;
        private int _index;
        private readonly int _indexMax;
        private SAINEnemyPartData _lastCheckSuccessPart;
    }
}