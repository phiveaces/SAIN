using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public enum CoverFinderState
    {
        off = 0,
        on = 1,
    }

    public class SAINCoverClass : BotBase, IBotClass
    {
        public SAINCoverClass(BotComponent bot) : base(bot)
        {
            CoverFinder = bot.GetOrAddComponent<CoverFinderComponent>();
        }

        public CoverPoint FindPointInDirection(Vector3 direction, float dotThreshold = 0.33f, float minDistance = 8f)
        {
            Vector3 botPosition = Bot.Position;
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                CoverPoint point = CoverPoints[i];
                if (point != null && 
                    !point.Spotted && 
                    !point.IsBad)
                {
                    Vector3 coverPosition = point.Position;
                    Vector3 directionToPoint = botPosition - coverPosition;

                    if (directionToPoint.sqrMagnitude > minDistance * minDistance 
                        && Vector3.Dot(directionToPoint.normalized, direction.normalized) > dotThreshold)
                    {
                        return point;
                    }
                }
            }
            return null;
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
            CoverFinder.Init(Bot);
        }

        public CoverFinderState CurrentCoverFinderState { get; private set; }

        public void Update()
        {
            if (!Bot.SAINLayersActive)
            {
                ActivateCoverFinder(false);
                return;
            }
            ActivateCoverFinder(Bot.Decision.HasDecision);
            createDebug();
        }

        private void createDebug()
        {
            if (SAINPlugin.DebugMode)
            {
                if (CoverInUse != null)
                {
                    if (debugCoverObject == null)
                    {
                        debugCoverObject = DebugGizmos.CreateLabel(CoverInUse.Position, "Cover In Use");
                        debugCoverLine = DebugGizmos.Line(CoverInUse.Position, Bot.Position + Vector3.up, 0.075f, -1, true);
                    }
                    debugCoverObject.WorldPos = CoverInUse.Position;
                    DebugGizmos.UpdatePositionLine(CoverInUse.Position, Bot.Position + Vector3.up, debugCoverLine);
                }
            }
            else if (debugCoverObject != null)
            {
                DebugGizmos.DestroyLabel(debugCoverObject);
                debugCoverObject = null;
            }
        }


        private GUIObject debugCoverObject;
        private GameObject debugCoverLine;

        public void Dispose()
        {
            try
            {
                CoverFinder?.Dispose();
            }
            catch { }
        }

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
            LastHitTime = Time.time;

            CoverPoint coverInUse = CoverInUse;
            if (coverInUse != null && coverInUse.Status == CoverStatus.InCover)
            {
                Enemy enemy = Bot.Enemy;
                bool HitInCoverKnown = enemy != null && 
                    damageInfo.Player?.iPlayer != null && 
                    enemy.EnemyPlayer.ProfileId == damageInfo.Player.iPlayer.ProfileId;

                bool HitInCoverCantSee = enemy != null && 
                    enemy.IsVisible == false;

                if (HitInCoverCantSee)
                {
                    coverInUse.HitInCoverCantSeeCount++;
                }
                else if (HitInCoverKnown)
                {
                    coverInUse.HitInCoverCount++;
                }
                else
                {
                    coverInUse.HitInCoverUnknownCount++;
                }
            }
        }

        public void ActivateCoverFinder(bool value)
        {
            if (value)
            {
                CoverFinder?.LookForCover();
                CurrentCoverFinderState = CoverFinderState.on;
            }
            if (!value)
            {
                CoverFinder?.StopLooking();
                CurrentCoverFinderState = CoverFinderState.off;
            }
        }

        public void CheckResetCoverInUse()
        {
            CoverPoint coverInUse = Bot.Cover.CoverInUse;
            if (coverInUse != null && coverInUse.IsBad)
            {
                Bot.Cover.CoverInUse = null;
                return;
            }

            SoloDecision decision = Bot.Decision.CurrentSoloDecision;
            if (decision != SoloDecision.MoveToCover
                && decision != SoloDecision.RunToCover
                && decision != SoloDecision.Retreat
                && decision != SoloDecision.HoldInCover 
                && decision != SoloDecision.ShiftCover)
            {
                Bot.Cover.CoverInUse = null;
            }
        }

        public CoverPoint ClosestPoint
        {
            get
            {
                SortPointsByPathDist();
                for (int i = 0; i < CoverPoints.Count; i++)
                {
                    CoverPoint point = CoverPoints[i];
                    if (point != null && point.Spotted == false)
                    {
                        return point;
                    }
                }
                return null;
            }
        }

        public void SortPointsByPathDist()
        {
            CoverFinderComponent.OrderPointsByPathDist(CoverPoints);
        }

        public bool DuckInCover()
        {
            var point = CoverInUse;
            if (point != null)
            {
                var move = Bot.Mover;
                var prone = move.Prone;
                bool shallProne = prone.ShallProneHide();

                if (shallProne && 
                    (Bot.Decision.CurrentSelfDecision != SelfDecision.None || Bot.Suppression.IsHeavySuppressed))
                {
                    prone.SetProne(true);
                    return true;
                }
                if (move.Pose.SetPoseToCover())
                {
                    return true;
                }
                if (shallProne && 
                    point.Collider.bounds.size.y < 0.85f)
                {
                    prone.SetProne(true);
                    return true;
                }
            }
            return false;
        }

        public bool CheckLimbsForCover()
        {
            var enemy = Bot.Enemy;
            if (enemy?.IsVisible == true)
            {
                if (CheckLimbTimer < Time.time)
                {
                    CheckLimbTimer = Time.time + 0.1f;
                    bool cover = false;
                    var target = enemy.EnemyIPlayer.WeaponRoot.position;
                    const float rayDistance = 3f;
                    if (CheckLimbForCover(BodyPartType.leftLeg, target, rayDistance) || CheckLimbForCover(BodyPartType.leftArm, target, rayDistance))
                    {
                        cover = true;
                    }
                    else if (CheckLimbForCover(BodyPartType.rightLeg, target, rayDistance) || CheckLimbForCover(BodyPartType.rightArm, target, rayDistance))
                    {
                        cover = true;
                    }
                    HasLimbCover = cover;
                }
            }
            else
            {
                HasLimbCover = false;
            }
            return HasLimbCover;
        }

        private bool HasLimbCover;
        private float CheckLimbTimer = 0f;

        private bool CheckLimbForCover(BodyPartType bodyPartType, Vector3 target, float dist = 2f)
        {
            var position = BotOwner.MainParts[bodyPartType].Position;
            Vector3 direction = target - position;
            return Physics.Raycast(position, direction, dist, LayerMaskClass.HighPolyWithTerrainMask);
        }

        public bool BotIsAtCoverPoint(CoverPoint coverPoint)
        {
            return coverPoint?.BotInThisCover() == true;
        }

        public bool BotIsAtCoverInUse()
        {
            return CoverInUse?.BotInThisCover() == true;
        }

        public CoverPoint CoverInUse { get; set; }

        public List<CoverPoint> CoverPoints => CoverFinder.CoverPoints;
        public CoverFinderComponent CoverFinder { get; private set; }
        public CoverPoint FallBackPoint => CoverFinder.FallBackPoint;

        public float LastHitTime { get; private set; }
    }
}