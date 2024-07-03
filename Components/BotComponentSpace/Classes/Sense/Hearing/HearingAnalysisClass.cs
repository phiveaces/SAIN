using EFT;
using SAIN.Helpers;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class HearingAnalysisClass : BotSubClass<SAINHearingSensorClass>, IBotClass
    {
        public HearingAnalysisClass(SAINHearingSensorClass hearing) : base(hearing)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(updateSettings);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public bool CheckIfSoundHeard(BotSoundStruct sound)
        {
            if (shallLimitAI(sound))
            {
                sound.Results.LimitedByAI = true;
                return false;
            }
            if (!doIDetectFootsteps(sound))
            {
                return false;
            }

            bool farFromPlayer = checkDistToPlayer(sound);
            sound.Results.SoundFarFromPlayer = farFromPlayer;
            if (farFromPlayer)
            {
                return false;
            }

            sound.Range.FinalRange = sound.Info.EnemyDistance * calcModifiers(sound);
            if (sound.Info.EnemyDistance > sound.Range.FinalRange)
            {
                return false;
            }
            return true;
        }

        private float calcModifiers(BotSoundStruct sound)
        {
            var mods = sound.Range.Modifiers;
            mods.EnvironmentModifier = calcEnvironmentMod(sound);
            mods.ConditionModifier = calcConditionMod(sound);
            mods.OcclusionModifier = calcOcclusionMod(sound);
            mods.FinalModifier = mods.CalcFinalModifier(HEAR_MODIFIER_MIN_CLAMP, HEAR_MODIFIER_MAX_CLAMP);
            return mods.FinalModifier;
        }

        private bool checkDistToPlayer(BotSoundStruct sound)
        {
            // The sound originated from somewhere far from the player's position, typically from a grenade explosion, which is handled elsewhere
            return (sound.Info.OriginalPosition - sound.Info.EnemyPlayer.Position).sqrMagnitude > 5f * 5f;
        }

        private float calcBunkerVolumeReduction(BotSoundStruct sound)
        {
            var botLocation = Bot.PlayerComponent.AIData.PlayerLocation;
            var enemyLocation = sound.Info.EnemyPlayer.AIData.PlayerLocation;

            bool botinBunker = botLocation.InBunker;
            bool playerinBunker = enemyLocation.InBunker;
            if (botinBunker != playerinBunker)
            {
                return BUNKER_REDUCTION_COEF;
            }
            if (botinBunker)
            {
                float diff = Mathf.Abs(botLocation.BunkerDepth - enemyLocation.BunkerDepth);
                if (diff > 0)
                {
                    return BUNKER_ELEV_DIFF_COEF;
                }
            }
            return 1f;
        }

        private const float BUNKER_REDUCTION_COEF = 0.2f;
        private const float BUNKER_ELEV_DIFF_COEF = 0.66f;

        private bool doIDetectFootsteps(BotSoundStruct sound)
        {
            if (sound.Info.IsGunShot)
            {
                return true;
            }

            bool hasheadPhones = Bot.PlayerComponent.Equipment.GearInfo.HasEarPiece;
            float closehearing = hasheadPhones ? 1f : 0.25f;
            float distance = sound.Info.EnemyDistance;
            if (distance <= closehearing)
            {
                return true;
            }

            float farhearing = hasheadPhones ? SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxFootstepAudioDistance : SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxFootstepAudioDistanceNoHeadphones;
            if (distance > farhearing)
            {
                return false;
            }

            float minimumChance = 0f;
            if (hasheadPhones)
            {
                if (distance < farhearing * 0.66f)
                {
                    minimumChance += 10f;
                }
                else
                {
                    minimumChance += 5f;
                }
                if (sound.Info.SoundType != SAINSoundType.FootStep)
                {
                    minimumChance += 10f;
                }
            }

            if (Bot.PlayerComponent.Transform.VelocityMagnitudeNormal < 0.1f)
            {
                minimumChance += hasheadPhones ? 5f : 3f;
            }

            if (Bot.HasEnemy &&
                Bot.Enemy.EnemyProfileId == sound.Info.EnemyPlayer.ProfileId)
            {
                minimumChance += hasheadPhones ? 10f : 5f;
            }

            float num = farhearing - closehearing;
            float num2 = distance - closehearing;
            float chanceToHear = 1f - num2 / num;
            chanceToHear *= 100f;

            chanceToHear = Mathf.Clamp(chanceToHear, minimumChance, 100f);
            sound.Results.ChanceToHear = chanceToHear;
            return EFTMath.RandomBool(chanceToHear);
        }

        private float calcOcclusionMod(BotSoundStruct sound)
        {
            var info = sound.Info;
            if (info.IsAI)
                return 1f;

            Vector3 position = info.OriginalPosition;
            bool isFootStep = info.SoundType == SAINSoundType.FootStep;
            bool isGunshot = info.IsGunShot;
            if (!isFootStep && !isGunshot)
                return 1f;

            if (isFootStep)
                position.y += 0.5f;

            Vector3 botheadpos = BotOwner.MyHead.position;
            float rayDist = sound.Range.BaseRange * sound.Range.Modifiers.ConditionModifier * sound.Range.Modifiers.EnvironmentModifier;
            if (!Physics.Raycast(botheadpos, position - botheadpos, rayDist, LayerMaskClass.HighPolyWithTerrainNoGrassMask))
            {
                sound.Results.VisibleSource = true;
                return 1f;
            }

            return isGunshot ? GUNSHOT_OCCLUSION_MOD : FOOTSTEP_OCCLUSION_MOD;
        }

        private float calcEnvironmentMod(BotSoundStruct sound)
        {
            if (Player.AIData.EnvironmentId == sound.Info.EnemyPlayer.Player.AIData.EnvironmentId)
            {
                return 1f;
            }
            float envMod = sound.Info.IsGunShot ? GUNSHOT_ENVIR_MOD : FOOTSTEP_ENVIR_MOD;
            float bunkerMod = calcBunkerVolumeReduction(sound);
            float result = envMod * bunkerMod;
            result = Mathf.Clamp(result, 0.1f, 1f);
            return result;
        }

        private bool shallLimitAI(BotSoundStruct sound)
        {
            if (!sound.Info.IsAI)
                return false;

            var aiLimit = GlobalSettingsClass.Instance.AILimit;
            if (!aiLimit.LimitAIvsAIGlobal)
                return false;

            if (!aiLimit.LimitAIvsAIHearing)
                return false;

            var enemyPlayer = sound.Info.EnemyPlayer;
            if (Bot.Enemy?.EnemyProfileId == enemyPlayer.ProfileId)
                return false;

            var enemyBot = enemyPlayer.BotComponent;
            float maxRange;
            if (enemyBot == null)
            {
                if (enemyPlayer.BotOwner?.Memory.GoalEnemy.ProfileId == Bot.ProfileId)
                {
                    return false;
                }
                maxRange = getMaxRange(Bot.CurrentAILimit);
            }
            else
            {
                if (enemyBot.Enemy?.EnemyProfileId == Bot.ProfileId)
                {
                    return false;
                }
                maxRange = getMaxRange(enemyBot.CurrentAILimit);
            }

            if (sound.Info.Enemy.RealDistance <= maxRange)
            {
                return false;
            }
            return true;
        }

        private float getMaxRange(AILimitSetting aiLimit)
        {
            switch (aiLimit)
            {
                case AILimitSetting.Far:
                    return _farDistance;

                case AILimitSetting.VeryFar:
                    return _veryFarDistance;

                case AILimitSetting.Narnia:
                    return _narniaDistance;

                default:
                    return float.MaxValue;
            }
        }

        private float calcConditionMod(BotSoundStruct sound)
        {
            // this is clumsy, not sure how to extract a modifier that would be clamped to be below the max affect distance, so im just returning 1f.
            var mods = sound.Range.Modifiers;
            float dist = sound.Info.EnemyDistance * mods.EnvironmentModifier;
            float maxAffectDist = HEAR_MODIFIER_MAX_AFFECT_DIST;
            if (dist <= maxAffectDist)
            {
                return 1f;
            }

            float modifier = 1f;
            if (!sound.Info.IsGunShot)
            {
                modifier *= GlobalSettings.Hearing.FootstepAudioMultiplier;
            }
            else
            {
                modifier *= GlobalSettings.Hearing.GunshotAudioMultiplier;
            }
            modifier *= Bot.Info.FileSettings.Core.HearingSense;

            if (dist * modifier < maxAffectDist)
                return 1f;

            if (!sound.Info.IsGunShot && sound.Info.EnemyDistance > HEAR_MODIFIER_MAX_AFFECT_DIST)
            {
                if (!Bot.PlayerComponent.Equipment.GearInfo.HasEarPiece)
                {
                    modifier *= HEAR_MODIFIER_NO_EARS;
                }
                if (Bot.PlayerComponent.Equipment.GearInfo.HasHeavyHelmet)
                {
                    modifier *= HEAR_MODIFIER_HEAVY_HELMET;
                }
                if (Bot.Memory.Health.Dying &&
                    !Bot.Memory.Health.OnPainKillers)
                {
                    modifier *= HEAR_MODIFIER_DYING;
                }
                if (Player.IsSprintEnabled)
                {
                    modifier *= HEAR_MODIFIER_SPRINT;
                }
                if (Player.HeavyBreath)
                {
                    modifier *= HEAR_MODIFIER_HEAVYBREATH;
                }
            }

            if (dist * modifier < maxAffectDist)
                return 1f;

            return modifier;
        }

        private void updateSettings(SAINPresetClass preset)
        {
            int frame = Time.frameCount;
            if (_lastCalcFrame == frame)
            {
                return;
            }
            _lastCalcFrame = frame;
            var maxHeadRanges = preset.GlobalSettings.AILimit.MaxHearingRanges;
            _farDistance = maxHeadRanges[AILimitSetting.Far];
            _veryFarDistance = maxHeadRanges[AILimitSetting.VeryFar];
            _narniaDistance = maxHeadRanges[AILimitSetting.Narnia];
        }

        private static int _lastCalcFrame;
        private static float _farDistance;
        private static float _veryFarDistance;
        private static float _narniaDistance;

        private const float GUNSHOT_OCCLUSION_MOD = 0.8f;
        private const float FOOTSTEP_OCCLUSION_MOD = 0.8f;
        private const float GUNSHOT_ENVIR_MOD = 0.65f;
        private const float FOOTSTEP_ENVIR_MOD = 0.8f;

        private const float HEAR_MODIFIER_NO_EARS = 0.65f;
        private const float HEAR_MODIFIER_HEAVY_HELMET = 0.8f;
        private const float HEAR_MODIFIER_DYING = 0.8f;
        private const float HEAR_MODIFIER_SPRINT = 0.85f;
        private const float HEAR_MODIFIER_HEAVYBREATH = 0.65f;

        private const float HEAR_MODIFIER_MIN_CLAMP = 0.1f;
        private const float HEAR_MODIFIER_MAX_CLAMP = 5f;
        private const float HEAR_MODIFIER_MAX_AFFECT_DIST = 10f;
    }
}