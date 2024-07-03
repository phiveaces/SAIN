using BepInEx.Logging;
using EFT;
using SAIN.Components;
using UnityEngine;
using static SAIN.Helpers.HelpersGClass;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using static UnityEngine.EventSystems.EventTrigger;

namespace SAIN.SAINComponent.Classes.Sense
{
    public class FlashLightDazzleClass : BotBase
    {
        private TemporaryStatModifiers Modifiers = new TemporaryStatModifiers(1f, 1f, 1f, 1f, 1f);

        public FlashLightDazzleClass(BotComponent owner) : base(owner)
        {
        }

        public void CheckIfDazzleApplied(Enemy enemy)
        {
            if (enemy?.IsValid == true && 
                enemy.IsVisible)
            {
                // If modifier is already applied, don't re-apply it
                if (Modifiers.Modifiers.IsApplyed)
                {
                    return;
                }

                FlashLightClass flashlight = enemy?.EnemyPlayerComponent?.Flashlight;
                if (flashlight != null)
                {
                    bool usingNVGs = BotOwner.NightVision.UsingNow;
                    if ((flashlight.WhiteLight || (usingNVGs && flashlight.IRLight)) &&
                        enemyWithFlashlight(enemy))
                    {
                        return;
                    }
                    else if ((flashlight.Laser || (usingNVGs && flashlight.IRLaser)) &&
                        enemyWithLaser(enemy))
                    {
                        return;
                    }
                }
            }
        }

        private bool enemyWithFlashlight(Enemy enemy)
        {
            float dist = enemy.RealDistance;
            if (dist < 80f && 
                flashlightVisionCheck(enemy.EnemyIPlayer))
            {
                Vector3 botPos = BotOwner.MyHead.position;
                Vector3 weaponRoot = enemy.EnemyPlayer.WeaponRoot.position;
                if (!Physics.Raycast(weaponRoot, (botPos - weaponRoot).normalized, (botPos - weaponRoot).magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    float gainSight = 0.66f;
                    float dazzlemodifier = dist < MaxDazzleRange ? getDazzleModifier(enemy) : 1f;

                    ApplyDazzle(dazzlemodifier, gainSight);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Applies dazzle to the enemy if they are within the Max dazzle range and the raycast between the BotOwner and the enemy is not blocked.
        /// </summary>
        private bool enemyWithLaser(Enemy enemy)
        {
            float dist = enemy.RealDistance;
            if (dist < 100f &&
                laserVisionCheck(enemy.EnemyIPlayer))
            {
                Vector3 botPos = BotOwner.MyHead.position;
                Vector3 weaponRoot = enemy.EnemyPlayer.WeaponRoot.position;
                if (!Physics.Raycast(weaponRoot, (botPos - weaponRoot).normalized, (botPos - weaponRoot).magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    float gainSight = 0.66f;
                    float dazzlemodifier = dist < MaxDazzleRange ? getDazzleModifier(enemy) : 1f;
                    ApplyDazzle(dazzlemodifier, gainSight);
                }
                return true;
            }
            return false;
        }

        private static float MaxDazzleRange => SAINPlugin.LoadedPreset.GlobalSettings.Flashlight.MaxDazzleRange;
        private static float Effectiveness => SAINPlugin.LoadedPreset.GlobalSettings.Flashlight.DazzleEffectiveness;

        public void ApplyDazzle(float dazzleModif, float gainSightModif)
        {
            Modifiers.Modifiers.PrecicingSpeedCoef = Mathf.Clamp(dazzleModif, 1f, 5f) * Effectiveness;
            Modifiers.Modifiers.AccuratySpeedCoef = Mathf.Clamp(dazzleModif, 1f, 5f) * Effectiveness;
            Modifiers.Modifiers.GainSightCoef = gainSightModif;
            Modifiers.Modifiers.ScatteringCoef = Mathf.Clamp(dazzleModif, 1f, 5f) * Effectiveness * 3;
            Modifiers.Modifiers.PriorityScatteringCoef = Mathf.Clamp(dazzleModif, 1f, 2.5f) * Effectiveness;

            BotOwner.Settings.Current.Apply(Modifiers.Modifiers, 0.1f);
        }

        private bool flashlightVisionCheck(IPlayer person)
        {
            float flashAngle = 0.9770526f;
            return enemyLookAtMe(person, flashAngle);
        }

        private bool laserVisionCheck(IPlayer person)
        {
            float laserAngle = 0.990f;
            return enemyLookAtMe(person, laserAngle);
        }

        private bool enemyLookAtMe(IPlayer person, float num)
        {
            Vector3 position = BotOwner.MyHead.position;
            Vector3 weaponRoot = person.WeaponRoot.position;
            bool enemylookatme = Vector.IsAngLessNormalized(Vector.NormalizeFastSelf(position - weaponRoot), person.LookDirection, num);
            return enemylookatme;
        }

        private float getDazzleModifier(Enemy enemy)
        {
            float enemyDist = enemy.RealDistance;
            float max = MaxDazzleRange;
            float min = max / 2f;

            float num = max - min;
            float num2 = enemy.RealDistance - num;
            float ratio = (num2 / num);
            float result = Mathf.InverseLerp(1f, 2f, ratio);

            if (BotOwner.NightVision.UsingNow && 
                (enemy.EnemyPlayerComponent.Flashlight.WhiteLight || enemy.EnemyPlayerComponent.Flashlight.Laser))
            {
                result *= 1.5f;
            }

            return result;
        }
    }
}