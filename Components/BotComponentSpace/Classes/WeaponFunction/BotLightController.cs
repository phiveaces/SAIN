﻿using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class BotLightController : BotBase, IBotClass
    {
        public BotLightController(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
            if (BotOwner?.BotLight == null)
            {
                return;
            }
            updateLightToggle();
        }

        private void updateLightToggle()
        {
            if (Bot.SAINLayersActive &&
                IsLightEnabled != wantLightOn &&
                _nextLightChangeTime < Time.time)
            {
                _nextLightChangeTime = Time.time + _changelightFreq * UnityEngine.Random.Range(0.66f, 1.33f);
                setLight(wantLightOn);
            }
        }

        public bool IsLightEnabled => BotOwner?.BotLight?.IsEnable == true;

        private float _nextLightChangeTime;
        private float _changelightFreq = 1f;

        public void Dispose()
        {
        }

        private void setLight(bool value)
        {
            if (value)
            {
                BotOwner.BotLight.TurnOn(true);
            }
            else
            {
                BotOwner.BotLight.TurnOff(false, true);
            }
        }

        public void ToggleLight(bool value)
        {
            wantLightOn = value;
        }

        private bool wantLightOn;

        public void ToggleLaser(bool value)
        {

        }

        public void HandleLightForSearch(float distanceToCurrentCorner)
        {
            if (distanceToCurrentCorner < 30f)
            {
                _timeWithinDistanceSearch = Time.time;
                ToggleLight(true);
            }
            else if (_timeWithinDistanceSearch + 0.66f < Time.time)
            {
                ToggleLight(false);
            }
        }

        private float _timeWithinDistanceSearch;

        public void HandleLightForEnemy()
        {
            if (Bot.Decision.CurrentSoloDecision == SoloDecision.Search)
            {
                return;
            }
            if (BotOwner.ShootData.Shooting)
            {
                return;
            }

            Enemy enemy = Bot.Enemy;
            if (enemy != null)
            {
                if (!enemy.Seen)
                {
                    ToggleLight(false);
                    return;
                }

                float maxTurnOnrange = 50f;
                SoloDecision decision = Bot.Decision.CurrentSoloDecision;

                if (enemy.EnemyNotLooking && enemy.RealDistance <= maxTurnOnrange * 0.9f)
                {
                    ToggleLight(true);
                    return;
                }

                if (enemy.IsVisible && 
                    Time.time - enemy.Vision.VisibleStartTime > 0.75f)
                {
                    if (enemy.RealDistance <= maxTurnOnrange * 0.9f)
                    {
                        ToggleLight(true);
                    }
                    else if (enemy.RealDistance > maxTurnOnrange)
                    {
                        ToggleLight(false);
                    }
                    return;
                }

                if (enemy.Seen &&
                    BotOwner.BotLight?.IsEnable == true &&
                    enemy.TimeSinceSeen > randomizedTurnOffTime)
                {
                    ToggleLight(false);
                    return;
                }
            }
        }

        private float randomizedTurnOffTime
        {
            get
            {
                if (_nextRandomTime < Time.time)
                {
                    _nextRandomTime = Time.time + _randomFreq * UnityEngine.Random.Range(0.66f, 1.33f);
                    _randomTime = UnityEngine.Random.Range(_minRandom, _maxRandom);
                }
                return _randomTime;
            }
        }

        private float _nextRandomTime;
        private float _randomFreq = 2f;
        private float _randomTime;
        private float _minRandom = 1.5f;
        private float _maxRandom = 6f;
    }
}