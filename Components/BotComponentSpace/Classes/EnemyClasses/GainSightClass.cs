﻿using EFT;
using SAIN.Components;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class GainSightClass : EnemyBase
    {
        public GainSightClass(Enemy enemy) : base(enemy)
        {
        }

        public float Value
        {
            get
            {
                if (_nextCheckVisTime < Time.time)
                {
                    _nextCheckVisTime = Time.time + 0.05f;
                    _gainSightModifier = GetGainSightModifier() * calcRepeatSeenCoef();
                }
                return _gainSightModifier;
            }
        }

        private float calcRepeatSeenCoef()
        {
            float result = calcVisionSpeedPositional(
                Enemy.KnownPlaces.EnemyDistanceFromLastSeen,
                _minSeenSpeedCoef,
                _minDistRepeatSeen,
                _maxDistRepeatSeen,
                SeenSpeedCheck.Vision);

            result *= calcVisionSpeedPositional(
                Enemy.KnownPlaces.EnemyDistanceFromLastHeard,
                _minHeardSpeedCoef,
                _minDistRepeatHeard,
                _maxDistRepeatHeard,
                SeenSpeedCheck.Audio);

            return result;
        }

        private enum SeenSpeedCheck
        {
            None = 0,
            Vision = 1,
            Audio = 2,
        }

        private float calcVisionSpeedPositional(float distance, float minSpeedCoef, float minDist, float maxDist, SeenSpeedCheck check)
        {
            if (distance <= minDist)
            {
                return minSpeedCoef;
            }
            if (distance >= maxDist)
            {
                return 1f;
            }

            float num = maxDist - minDist;
            float num2 = distance - minDist;
            float ratio = num2 / num;
            float result = Mathf.Lerp(minSpeedCoef, 1f, ratio);
            //Logger.LogInfo($"{check} Distance from Position: {distance} Result: {result}");
            return result;
        }

        private float _minSeenSpeedCoef = 0.01f;
        private float _minDistRepeatSeen = 1f;
        private float _maxDistRepeatSeen = 20f;

        private float _minHeardSpeedCoef = 0.25f;
        private float _minDistRepeatHeard = 1f;
        private float _maxDistRepeatHeard = 10f;

        private float _gainSightModifier;
        private float _nextCheckVisTime;

        private float calcGearMod()
        {
            return Enemy.EnemyPlayerComponent.AIData.AIGearModifier.StealthModifier(Enemy.RealDistance);
        }

        private float _visionSpeed_Max_Dist = 200f;
        private float _visionSpeed_Max_Dist_NVGS = 250f;
        private float _visionSpeed_Min_Dist = 10f;
        private float _visionSpeed_Min_Dist_NVGS = 65f;

        private float calcTimeModifier(bool flareEnabled)
        {
            float baseModifier = baseTimeModifier(flareEnabled);

            if (baseModifier <= 1f)
                return 1f;
            if (enemyUsingLight(out float lightModifier))
                return lightModifier;

            bool usingNVGS = BotOwner.NightVision.UsingNow;
            float enemyDist = Enemy.RealDistance;

            if (enemyInRangeOfLight(enemyDist, usingNVGS))
                return 1f;

            float max = 1f + baseModifier;
            float min = 1f;
            float maxDist = usingNVGS ? _visionSpeed_Max_Dist_NVGS : _visionSpeed_Max_Dist;
            float minDist = usingNVGS ? _visionSpeed_Min_Dist_NVGS : _visionSpeed_Min_Dist;

            if (enemyDist >= maxDist)
                return max;
            if (enemyDist < minDist)
                return min;

            float enemyVelocity = Enemy.Vision.EnemyVelocity;
            bool moving = enemyVelocity > 0.1f;
            if (!moving)
            {
                min += 0.5f;
                max += 1f;
            }

            float num = maxDist - minDist;
            float num2 = enemyDist - minDist;
            float ratio = num2 / num;
            float result = Mathf.Lerp(min, max, ratio);
            return result;
        }

        private bool enemyUsingLight(out float modifier)
        {
            var flashlight = Enemy.EnemyPlayerComponent.Flashlight;
            if (flashlight.WhiteLight)
            {
                modifier = 0.75f;
                return true;
            }
            if (flashlight.Laser)
            {
                modifier = 1f;
                return true;
            }
            bool usingNVGS = BotOwner.NightVision.UsingNow;
            if (usingNVGS && (flashlight.IRLaser || flashlight.IRLight))
            {
                modifier = 0.8f;
                return true;
            }
            modifier = 1f;
            return false;
        }

        private bool enemyInRangeOfLight(float enemyDist, bool usingNVGS)
        {
            var settings = Bot.Info.FileSettings.Look;
            if (Bot.PlayerComponent.Flashlight.WhiteLight &&
                enemyDist <= settings.VISIBLE_DISNACE_WITH_LIGHT)
            {
                return true;
            }
            if (usingNVGS && Bot.PlayerComponent.Flashlight.IRLight && enemyDist <= settings.VISIBLE_DISNACE_WITH_IR_LIGHT)
            {
                return true;
            }
            return false;
        }

        private float calcWeatherMod(bool flareEnabled)
        {
            float baseModifier = baseWeatherMod(flareEnabled);

            if (baseModifier <= 1f)
                return 1f;

            float max = 1f + baseModifier;
            float min = 1f;
            float maxDist = 150f;
            float minDist = 30f;
            float enemyDist = Enemy.RealDistance;

            if (enemyDist >= maxDist)
                return max;
            if (enemyDist < minDist)
                return min;
            if (enemyUsingLight(out _))
                return min;

            bool moving = Enemy.Vision.EnemyVelocity > 0.1f;
            if (!moving)
                max += 1f;

            float num = maxDist - minDist;
            float num2 = enemyDist - minDist;
            float ratio = num2 / num;
            float result = Mathf.Lerp(min, max, ratio);
            return result;
        }

        private float baseWeatherMod(bool flareEnabled)
        {
            if (flareEnabled && Enemy.RealDistance < 100f)
            {
                return 1f;
            }
            return SAINBotController.Instance.WeatherVision.GainSightModifier;
        }

        private float baseTimeModifier(bool flareEnabled)
        {
            if (flareEnabled)
            {
                return 1f;
            }
            return SAINBotController.Instance.TimeVision.TimeGainSightModifier;
        }

        private float GetGainSightModifier()
        {
            float partMod = calcPartsMod();
            float gearMod = calcGearMod();

            bool flareEnabled = EnemyPlayer.AIData?.GetFlare == true &&
                Enemy.EnemyPlayerComponent?.Equipment.CurrentWeapon?.HasSuppressor == false;
            float weatherMod = calcWeatherMod(flareEnabled);
            float timeMod = calcTimeModifier(flareEnabled);

            float moveMod = calcMoveModifier();
            float elevMod = calcElevationModifier();
            float thirdPartyMod = calcThirdPartyMod();
            float angleMod = calcAngleMod();

            float notLookMod = 1f;
            if (!Enemy.IsAI)
                notLookMod = SAINNotLooking.GetVisionSpeedDecrease(Enemy.EnemyInfo);

            float result = 1f * partMod * gearMod * weatherMod * timeMod * moveMod * elevMod * thirdPartyMod * angleMod * notLookMod;

            //if (EnemyPlayer.IsYourPlayer && result != 1f)
            //{
            //    Logger.LogWarning($"GainSight Time Result: [{result}] : partMod {partMod} : gearMod {gearMod} : flareMod {flareMod} : moveMod {moveMod} : elevMod {elevMod} : posFlareMod {posFlareMod} : thirdPartyMod {thirdPartyMod} : angleMod {angleMod} : notLookMod {notLookMod} ");
            //}

            return result;
        }

        // private static float _nextLogTime;

        private float calcPartsMod()
        {
            if (Enemy.IsAI)
            {
                return 1f;
            }

            float max = 1.75f;

            if (!Enemy.InLineOfSight)
            {
                return max;
            }
            float partRatio = GetRatioPartsVisible(EnemyInfo, out int visibleCount);
            if (visibleCount < 1)
            {
                return max;
            }
            float min = 0.9f;
            if (partRatio >= 1f)
            {
                return min;
            }
            float result = Mathf.Lerp(max, min, partRatio);
            return result;
        }

        private static float GetRatioPartsVisible(EnemyInfo enemyInfo, out int visibleCount)
        {
            var enemyParts = enemyInfo.AllActiveParts;
            int partCount = 0;
            visibleCount = 0;

            var bodyPartData = enemyInfo.BodyData().Value;
            if (bodyPartData.IsVisible || bodyPartData.LastVisibilityCastSucceed)
            {
                visibleCount++;
            }
            partCount++;

            foreach (var part in enemyParts)
            {
                if (part.Value.LastVisibilityCastSucceed || part.Value.IsVisible)
                {
                    visibleCount++;
                }
                partCount++;
            }

            return (float)visibleCount / (float)partCount;
        }

        private float calcMoveModifier()
        {
            LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
            return Mathf.Lerp(1, globalLookSettings.SprintingVisionModifier, Enemy.Vision.EnemyVelocity);
        }

        private float calcElevationModifier()
        {
            LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;

            Vector3 botEyeToPlayerBody = EnemyPlayer.MainParts[BodyPartType.body].Position - BotOwner.MainParts[BodyPartType.head].Position;
            var visionAngleDeviation = Vector3.Angle(new Vector3(botEyeToPlayerBody.x, 0f, botEyeToPlayerBody.z), botEyeToPlayerBody);

            if (botEyeToPlayerBody.y >= 0)
            {
                float angleFactor = Mathf.InverseLerp(0, globalLookSettings.HighElevationMaxAngle, visionAngleDeviation);
                return Mathf.Lerp(1f, globalLookSettings.HighElevationVisionModifier, angleFactor);
            }
            else
            {
                float angleFactor = Mathf.InverseLerp(0, globalLookSettings.LowElevationMaxAngle, visionAngleDeviation);
                return Mathf.Lerp(1f, globalLookSettings.LowElevationVisionModifier, angleFactor);
            }
        }

        private float calcThirdPartyMod()
        {
            if (!Enemy.IsCurrentEnemy)
            {
                Enemy activeEnemy = Enemy.Bot.Enemy;
                if (activeEnemy != null)
                {
                    Vector3? activeEnemyLastKnown = activeEnemy.LastKnownPosition;
                    if (activeEnemyLastKnown != null)
                    {
                        Vector3 currentEnemyDir = (activeEnemyLastKnown.Value - Enemy.Bot.Position).normalized;
                        Vector3 myDir = Enemy.EnemyDirection.normalized;

                        float angle = Vector3.Angle(currentEnemyDir, myDir);

                        float minAngle = 20f;
                        float maxAngle = Enemy.Vision.MaxVisionAngle;
                        if (angle > minAngle && 
                            angle < maxAngle)
                        {
                            float num = maxAngle - minAngle;
                            float num2 = angle - minAngle;

                            float maxRatio = 1.5f;
                            float ratio = num2 / num;
                            float reductionMod = Mathf.Lerp(1f, maxRatio, ratio);
                            return reductionMod;
                        }
                    }
                }
            }
            return 1f;
        }

        private static bool _reduceVisionSpeedOnPeriphVis = true;
        private static float _periphVisionStart = 30f;
        private static float _maxPeriphVisionSpeedReduction = 2.5f;

        private float calcAngleMod()
        {
            if (!_reduceVisionSpeedOnPeriphVis)
            {
                return 1f;
            }

            if (Enemy.RealDistance < 10f)
            {
                return 1f;
            }

            float angle = Enemy.Vision.AngleToEnemy;

            float minAngle = _periphVisionStart;
            if (angle < minAngle)
            {
                return 1f;
            }
            float maxAngle = Enemy.Vision.MaxVisionAngle;
            float maxRatio = _maxPeriphVisionSpeedReduction;
            if (angle > maxAngle)
            {
                return maxRatio;
            }

            float angleDiff = maxAngle - minAngle;
            float enemyAngleDiff = angle - minAngle;
            float ratio = enemyAngleDiff / angleDiff;
            return Mathf.Lerp(1f, maxRatio, ratio);
        }
    }
}