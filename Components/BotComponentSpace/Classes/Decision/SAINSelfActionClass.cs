﻿using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SAINSelfActionClass : BotBase, IBotClass
    {
        public SAINSelfActionClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        private float _handsBusyTimer;
        private float _nextCheckTime;

        public void Update()
        {
            if (!Bot.SAINLayersActive)
            {
                return;
            }
            if (UsingMeds)
            {
                return;
            }
            var decision = Bot.Decision.CurrentSelfDecision;
            if (decision == SelfDecision.None)
            {
                return;
            }
            if (_nextCheckTime > Time.time)
            {
                return;
            }
            _nextCheckTime = Time.time + 0.1f;

            if (decision == SelfDecision.Reload)
            {
                Bot.Info.WeaponInfo.Reload.TryReload();
                return;
            }

            if (_handsBusyTimer > Time.time)
            {
                return;
            }
            if (Player.HandsController.IsInInteractionStrictCheck())
            {
                _handsBusyTimer = Time.time + 0.25f;
                return;
            }

            if (_healTime > Time.time)
            {
                return;
            }

            bool didAction = false;
            switch (decision)
            {
                case SelfDecision.FirstAid:
                    didAction = DoFirstAid();
                    break;

                case SelfDecision.Stims:
                    didAction = DoStims();
                    break;

                default:
                    break;
            }

            if (didAction)
            {
                _healTime = Time.time + 1f;
            }
        }

        public void Dispose()
        {
        }

        private bool UsingMeds => BotOwner.Medecine?.Using == true;

        public bool DoFirstAid()
        {
            var heal = BotOwner.Medecine?.FirstAid;
            if (heal == null)
            {
                return false;
            }
            if (_firstAidTimer < Time.time &&
                heal.ShallStartUse())
            {
                _firstAidTimer = Time.time + 5f;
                heal.TryApplyToCurrentPart();
                return true;
            }
            return false;
        }

        private float _firstAidTimer;

        public bool DoSurgery()
        {
            var surgery = BotOwner.Medecine?.SurgicalKit;
            if (surgery == null)
            {
                return false;
            }
            if (_trySurgeryTime < Time.time &&
                surgery.ShallStartUse())
            {
                _trySurgeryTime = Time.time + 5f;
                surgery.ApplyToCurrentPart();
                return true;
            }
            return false;
        }

        private float _trySurgeryTime;

        public bool DoStims()
        {
            var stims = BotOwner.Medecine?.Stimulators;
            if (stims == null)
            {
                return false;
            }
            if (_stimTimer < Time.time &&
                stims.CanUseNow())
            {
                _stimTimer = Time.time + 3f;
                try { stims.TryApply(); }
                catch { }
                return true;
            }
            return false;
        }

        private float _stimTimer;

        private bool HaveStimsToHelp()
        {
            return false;
        }

        public void BotCancelReload()
        {
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                BotOwner.WeaponManager.Reload.TryStopReload();
            }
        }

        private float _healTime = 0f;
    }
}