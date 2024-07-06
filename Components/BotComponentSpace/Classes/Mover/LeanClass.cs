using EFT;
using SAIN.Preset.GlobalSettings;
using System.Linq;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class LeanClass : BotBase, IBotClass
    {
        public LeanClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        private static readonly CombatDecision[] DontLean =
        {
            CombatDecision.Retreat,
            CombatDecision.RunToCover,
            CombatDecision.RunAway,
        };

        public void Update()
        {
            if (!Bot.SAINLayersActive)
            {
                ResetLean();
                return;
            }
            if (IsHoldingLean)
            {
                return;
            }
            var CurrentDecision = Bot.Decision.CurrentSoloDecision;
            var enemy = Bot.Enemy;
            if (enemy == null || Player.IsSprintEnabled || DontLean.Contains(CurrentDecision) || Bot.Suppression.IsSuppressed)
            {
                ResetLean();
                return;
            }
            if (GlobalSettingsClass.Instance.AILimit.LimitAIvsAIGlobal 
                && enemy.IsAI 
                && Bot.CurrentAILimit != AILimitSetting.None)
            {
                ResetLean();
                return;
            }
            if (CurrentDecision == CombatDecision.HoldInCover)
            {
                return;
            }
            if (LeanTimer < Time.time)
            {
                var lastKnownPlace = enemy.KnownPlaces.LastKnownPlace;
                if (lastKnownPlace != null)
                {
                    FindLeanDirectionRayCast(lastKnownPlace.Position);
                }
                float timeAdd = LeanDirection == LeanSetting.None ? 0.25f : 1f;
                LeanTimer = Time.time + timeAdd;
            }
        }

        private float _stopHoldLeanTime;

        public bool IsHoldingLean => _stopHoldLeanTime > Time.time;

        public void HoldLean(float duration)
        {
            if (LeanDirection != LeanSetting.None)
            {
                _stopHoldLeanTime = Time.time + duration;
            }
        }

        public void Dispose()
        {
        }

        public LeanSetting LeanDirection { get; private set; }
        public LeanSetting LastLeanDirection { get; private set; }

        private float LeanTimer = 0f;

        public void ResetLean()
        {
            LastLeanDirection = LeanDirection;
            LeanDirection = LeanSetting.None;
            Bot.Mover.FastLean(0f);
        }

        public void FindLeanDirectionRayCast(Vector3 targetPos)
        {
            DirectLineOfSight = CheckOffSetRay(targetPos, 0f, 0f, out var direct);

            RightLos = CheckOffSetRay(targetPos, 90f, 0.66f, out var rightOffset);
            if (!RightLos)
            {
                RightLosPos = rightOffset;

                rightOffset.y = BotOwner.Position.y;
                float halfDist1 = (rightOffset - BotOwner.Position).magnitude / 2f;

                RightHalfLos = CheckOffSetRay(targetPos, 90f, halfDist1, out var rightHalfOffset);
                if (!RightHalfLos)
                {
                    RightHalfLosPos = rightHalfOffset;
                }
                else
                {
                    RightHalfLosPos = null;
                }
            }
            else
            {
                RightLosPos = null;
                RightHalfLosPos = null;
            }

            LeftLos = CheckOffSetRay(targetPos, -90f, 0.66f, out var leftOffset);
            if (!LeftLos)
            {
                LeftLosPos = leftOffset;

                leftOffset.y = BotOwner.Position.y;
                float halfDist2 = (leftOffset - BotOwner.Position).magnitude / 2f;

                LeftHalfLos = CheckOffSetRay(targetPos, -90f, halfDist2, out var leftHalfOffset);

                if (!LeftHalfLos)
                {
                    LeftHalfLosPos = leftHalfOffset;
                }
                else
                {
                    LeftHalfLosPos = null;
                }
            }
            else
            {
                LeftLosPos = null;
                LeftHalfLosPos = null;
            }
            var setting = GetSettingFromResults();
            LastLeanDirection = LeanDirection;
            LeanDirection = setting;
            Bot.Mover.FastLean(setting);
        }

        public LeanSetting GetSettingFromResults()
        {
            LeanSetting setting;

            if (DirectLineOfSight)
            {
                return LeanSetting.None;
            }

            if ((LeftLos || LeftHalfLos) && !RightLos)
            {
                setting = LeanSetting.Left;
            }
            else if (!LeftLos && (RightLos || RightHalfLos))
            {
                setting = LeanSetting.Right;
            }
            else
            {
                setting = LeanSetting.None;
            }

            return setting;
        }

        private bool CheckOffSetRay(Vector3 targetPos, float angle, float dist, out Vector3 Point)
        {
            Vector3 startPos = BotOwner.Position;
            startPos.y = Bot.Transform.HeadPosition.y;

            if (dist > 0f)
            {
                var dirToEnemy = (targetPos - BotOwner.Position).normalized;

                Quaternion rotation = Quaternion.Euler(0, angle, 0);

                Vector3 direction = rotation * dirToEnemy;

                Point = FindOffset(startPos, direction, dist);

                if ((Point - startPos).magnitude < dist / 3f)
                {
                    return true;
                }
            }
            else
            {
                Point = startPos;
            }

            bool LOS = LineOfSight(Point, targetPos);

            Point.y = BotOwner.Position.y;

            return LOS;
        }

        private bool LineOfSight(Vector3 start, Vector3 target)
        {
            var direction = target - start;
            float distance = Mathf.Clamp(direction.magnitude, 0f, 8f);
            return !Physics.Raycast(start, direction, distance, LayerMaskClass.HighPolyWithTerrainMask);
        }

        private Vector3 FindOffset(Vector3 start, Vector3 direction, float distance)
        {
            if (Physics.Raycast(start, direction, out var hit, distance, LayerMaskClass.HighPolyWithTerrainMask))
            {
                return hit.point;
            }
            else
            {
                return start + direction.normalized * distance;
            }
        }

        public bool DirectLineOfSight { get; set; }

        public bool LeftLos { get; set; }
        public Vector3? LeftLosPos { get; set; }

        public bool LeftHalfLos { get; set; }
        public Vector3? LeftHalfLosPos { get; set; }

        public bool RightLos { get; set; }
        public Vector3? RightLosPos { get; set; }

        public bool RightHalfLos { get; set; }
        public Vector3? RightHalfLosPos { get; set; }
    }
}