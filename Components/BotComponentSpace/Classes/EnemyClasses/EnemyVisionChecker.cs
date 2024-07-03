using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyVisionChecker : EnemyBase, IBotClass
    {
        public Vector3 LastSeenPoint { get; private set; }
        public bool LineOfSight => EnemyParts.LineOfSight;
        public SAINEnemyParts EnemyParts { get; }

        public EnemyVisionChecker(Enemy enemy) : base(enemy)
        {
            EnemyParts = new SAINEnemyParts(enemy.EnemyPlayer.PlayerBones, enemy.Player.IsYourPlayer);
            _transform = enemy.Bot.Transform;
            _startVisionTime = Time.time + UnityEngine.Random.Range(0.0f, 0.33f);
        }

        public void Init()
        {
        }

        public void Update()
        {
            EnemyParts.Update();
            if (Enemy.Events.OnEnemyLineOfSightChanged.CheckToggle(LineOfSight))
            {
                //Logger.LogDebug($"los changed");
            }
        }

        public void Dispose()
        {
        }

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
            checkLOS(out Vector3? successPoint);
            Enemy.Events.OnEnemyLineOfSightChanged.CheckToggle(LineOfSight);
            if (successPoint != null)
            {
                LastSeenPoint = successPoint.Value;
            }
        }

        private bool checkLOS(out Vector3? seenPosition)
        {
            Vector3 lookPoint = _transform.EyePosition;
            float maxRange = AIVisionRangeLimit();
            if (EnemyParts.CheckBodyLineOfSight(lookPoint, maxRange, out seenPosition))
            {
                return true;
            }

            float headMaxRange = Mathf.Clamp(maxRange, 0f, 100f);

            if (!Enemy.IsAI && 
                EnemyParts.CheckHeadLineOfSight(lookPoint, headMaxRange, out seenPosition))
            {
                return true;
            }
            if (EnemyParts.CheckRandomPartLineOfSight(lookPoint, maxRange, out seenPosition))
            {
                return true;
            }
            // Do an extra check if the bot has this enemy as their active primary enemy or the enemy is not AI
            if (Enemy.IsCurrentEnemy && 
                !Enemy.IsAI &&
                EnemyParts.CheckRandomPartLineOfSight(lookPoint, maxRange, out seenPosition))
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

        public void UpdatePresetSettings(SAINPresetClass preset)
        {
            var aiLimit = preset.GlobalSettings.AILimit;
            _farDistance = aiLimit.MaxVisionRanges[AILimitSetting.Far].Sqr();
            _veryFarDistance = aiLimit.MaxVisionRanges[AILimitSetting.VeryFar].Sqr();
            _narniaDistance = aiLimit.MaxVisionRanges[AILimitSetting.Narnia].Sqr();

            if (SAINPlugin.DebugMode)
            {
                Logger.LogDebug($"Updated AI Vision Limit Settings: [{_farDistance.Sqrt()}, {_veryFarDistance.Sqrt()}, {_narniaDistance.Sqrt()}]");
            }
        }

        private bool _visionStarted;
        private float _startVisionTime;
        private PersonTransformClass _transform;
        private static float _farDistance;
        private static float _veryFarDistance;
        private static float _narniaDistance;
    }
}