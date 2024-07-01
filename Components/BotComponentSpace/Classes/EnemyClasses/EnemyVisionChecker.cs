﻿using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyVisionChecker : EnemyBase
    {
        public event Action<Enemy, bool> OnEnemyLineOfSightChanged;
        public bool LineOfSight => EnemyParts.LineOfSight;
        public SAINEnemyParts EnemyParts { get; private set; }

        public EnemyVisionChecker(Enemy enemy) : base(enemy)
        {
            EnemyParts = new SAINEnemyParts(enemy.EnemyPlayer.PlayerBones, enemy.Player.IsYourPlayer);
            _transform = enemy.Bot.Transform;
            _startVisionTime = Time.time + UnityEngine.Random.Range(0.0f, 0.33f);
        }

        private bool _visionStarted;
        private float _startVisionTime;

        private PersonTransformClass _transform;

        public void CheckVision(out bool didCheck)
        {
            if (!_visionStarted)
            {
                if (_startVisionTime > Time.time)
                {
                    didCheck = false;
                    return;
                }
                _visionStarted = true;
            }

            didCheck = true;
            bool wasInLOS = LineOfSight;
            if (checkLOS() != wasInLOS)
            {
                OnEnemyLineOfSightChanged?.Invoke(Enemy, LineOfSight);
            }
        }

        private bool checkLOS()
        {
            float maxRange = AIVisionRangeLimit();
            if (EnemyParts.CheckBodyLineOfSight(_transform.EyePosition, maxRange))
            {
                return true;
            }
            if (EnemyParts.CheckRandomPartLineOfSight(_transform.EyePosition, maxRange))
            {
                return true;
            }
            // Do an extra check if the bot has this enemy as their active primary enemy or the enemy is not AI
            if (Enemy.IsCurrentEnemy && !Enemy.IsAI &&
                EnemyParts.CheckRandomPartLineOfSight(_transform.EyePosition, maxRange))
            {
                return true;
            }
            return false;
        }

        public float AIVisionRangeLimit()
        {
            if (!Enemy.IsAI)
            {
                return float.MaxValue;
            }
            var aiLimit = GlobalSettingsClass.Instance.AILimit;
            if (!aiLimit.LimitAIvsAIGlobal)
            {
                return float.MaxValue;
            }
            if (!aiLimit.LimitAIvsAIVision)
            {
                return float.MaxValue;
            }
            var enemyBot = Enemy.EnemyPerson.BotComponent;
            if (enemyBot == null)
            {
                // if an enemy bot is not a sain bot, but has this bot as an enemy, dont limit at all.
                if (Enemy.EnemyPerson.BotOwner?.Memory.GoalEnemy?.ProfileId == Bot.ProfileId)
                {
                    return float.MaxValue;
                }
                return getMaxVisionRange(Bot.CurrentAILimit);
            }
            else
            {
                if (enemyBot.Enemy?.EnemyProfileId == Bot.ProfileId)
                {
                    return float.MaxValue;
                }
                return getMaxVisionRange(enemyBot.CurrentAILimit);
            }
        }

        private static float getMaxVisionRange(AILimitSetting aiLimit)
        {
            switch (aiLimit)
            {
                default:
                    return float.MaxValue;

                case AILimitSetting.Far:
                    return _farDistance;

                case AILimitSetting.VeryFar:
                    return _veryFarDistance;

                case AILimitSetting.Narnia:
                    return _narniaDistance;
            }
        }

        static EnemyVisionChecker()
        {
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings();
        }

        private static void updateSettings()
        {
            var aiLimit = GlobalSettingsClass.Instance.AILimit;
            _farDistance = aiLimit.MaxVisionRanges[AILimitSetting.Far].Sqr();
            _veryFarDistance = aiLimit.MaxVisionRanges[AILimitSetting.VeryFar].Sqr();
            _narniaDistance = aiLimit.MaxVisionRanges[AILimitSetting.Narnia].Sqr();

            if (SAINPlugin.DebugMode)
            {
                Logger.LogDebug($"Updated AI Vision Limit Settings: [{_farDistance.Sqrt()}, {_veryFarDistance.Sqrt()}, {_narniaDistance.Sqrt()}]");
            }
        }

        private static float _farDistance;
        private static float _veryFarDistance;
        private static float _narniaDistance;

    }
}