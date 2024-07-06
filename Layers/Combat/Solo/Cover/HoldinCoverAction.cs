using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class HoldinCoverAction : SAINAction, ISAINAction
    {
        public HoldinCoverAction(BotOwner bot) : base(bot, nameof(HoldinCoverAction))
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override IEnumerator ActionCoroutine()
        {
            while (true)
            {
                yield return null;

                Bot.Steering.SteerByPriority();
                Shoot.Update();

                CoverPoint coverInUse = CoverInUse;
                if (coverInUse == null)
                {
                    Bot.Mover.DogFight.DogFightMove(false);
                    continue;
                }

                adjustPosition();
                Bot.Cover.DuckInCover();
                checkSetProne();
                checkSetLean();
            }
        }

        public override void Update()
        {
        }

        private void adjustPosition()
        {
            if (_nextCheckPosTime < Time.time)
            {
                _nextCheckPosTime = Time.time + 1f;
                Vector3 coverPos = CoverInUse.Position;
                if (!Bot.Player.IsInPronePose && (coverPos - _position).sqrMagnitude > 0.25f)
                {
                    _position = coverPos;
                    Bot.Mover.GoToPoint(coverPos, out _, 0.25f);
                    return;
                }
                Bot.Mover.StopMove();
            }
        }

        private float _nextCheckPosTime;
        private Vector3 _position;

        private void checkSetProne()
        {
            if (Bot.Enemy != null
                && Bot.Player.MovementContext.CanProne
                && Bot.Player.PoseLevel <= 0.1
                && Bot.Enemy.IsVisible
                && BotOwner.WeaponManager.Reload.Reloading)
            {
                Bot.Mover.Prone.SetProne(true);
            }
        }

        private void checkSetLean()
        {
            if (Bot.Suppression.IsSuppressed)
            {
                Bot.Mover.FastLean(LeanSetting.None);
                CurrentLean = LeanSetting.None;
                return;
            }

            if (CurrentLean != LeanSetting.None && ShallHoldLean())
            {
                Bot.Mover.FastLean(CurrentLean);
                ChangeLeanTimer = Time.time + 0.5f;
                return;
            }

            if (ChangeLeanTimer < Time.time)
            {
                setLean();
            }
        }

        private void setLean()
        {
            LeanSetting newLean;
            switch (CurrentLean)
            {
                case LeanSetting.Left:
                case LeanSetting.Right:
                    newLean = LeanSetting.None;
                    ChangeLeanTimer = Time.time + 1f * Random.Range(0.66f, 1.33f);
                    break;

                default:
                    newLean = EFTMath.RandomBool() ? LeanSetting.Left : LeanSetting.Right;
                    ChangeLeanTimer = Time.time + 2f * Random.Range(0.66f, 1.33f);
                    break;
            }

            if (checkLeanIntoObject(newLean))
            {
                return;
            }
            CurrentLean = newLean;
            Bot.Mover.FastLean(newLean);
        }

        private const float RAYCAST_LEAN_HITOBJECT_DIST = 0.5f;

        private bool checkLeanIntoObject(LeanSetting lean)
        {
            Vector3 headPos = Bot.Transform.HeadPosition;
            Vector3 rayEnd = lean == LeanSetting.Right ? Bot.Transform.Right() : Bot.Transform.Left();
            switch (lean)
            {
                case LeanSetting.Right:
                case LeanSetting.Left:
                    return Vector.Raycast(headPos, headPos + (rayEnd * RAYCAST_LEAN_HITOBJECT_DIST), LayerMaskClass.HighPolyWithTerrainMask);

                default:
                    return false;
            }
        }

        private bool ShallHoldLean()
        {
            if (Bot.Suppression.IsSuppressed)
            {
                return false;
            }
            Enemy enemy = Bot.Enemy;
            if (enemy == null || !enemy.Seen)
            {
                return false;
            }
            if (enemy.IsVisible && enemy.CanShoot)
            {
                return true;
            }
            if (enemy.TimeSinceSeen < 3f)
            {
                return true;
            }
            return false;
        }

        private void Lean(LeanSetting setting, bool holdLean)
        {
            if (holdLean)
            {
                return;
            }
            CurrentLean = setting;
            Bot.Mover.FastLean(setting);
        }

        private LeanSetting CurrentLean;
        private float ChangeLeanTimer;

        private CoverPoint CoverInUse;

        public override void Start()
        {
            Toggle(true);
            ChangeLeanTimer = Time.time + 2f;
            CoverInUse = Bot.Cover.CoverInUse;
        }

        public override void Stop()
        {
            Toggle(false);
            Bot.Cover.CheckResetCoverInUse();
            Bot.Mover.Prone.SetProne(false);
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Hold In Cover Info");
            var cover = Bot.Cover;
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Current Cover Status", $"{CoverInUse?.StraightDistanceStatus}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Current Cover Height", $"{CoverInUse?.CoverHeight}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Current Cover Value", $"{CoverInUse?.CoverValue}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("CoverFinder State", $"{cover.CurrentCoverFinderState}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Cover Count", $"{cover.CoverPoints.Count}", Color.white, Color.yellow, true);
            if (Bot.CurrentTargetPosition != null)
            {
                stringBuilder.AppendLabeledValue("Current Target Position", $"{Bot.CurrentTargetPosition.Value}", Color.white, Color.yellow, true);
            }
            else
            {
                stringBuilder.AppendLabeledValue("Current Target Position", null, Color.white, Color.yellow, true);
            }

            if (CoverInUse != null)
            {
                stringBuilder.AppendLine("Cover In Use");
                stringBuilder.AppendLabeledValue("Status", $"{CoverInUse.StraightDistanceStatus}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Height / Value", $"{CoverInUse.CoverHeight} {CoverInUse.CoverValue}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Path Length", $"{CoverInUse.PathLength}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Straight Distance", $"{(CoverInUse.Position - Bot.Position).magnitude}", Color.white, Color.yellow, true);
            }
        }
    }
}