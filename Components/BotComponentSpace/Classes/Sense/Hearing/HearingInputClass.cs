using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class HearingInputClass : BotSubClass<SAINHearingSensorClass>, IBotClass
    {
        public BotSoundStruct? LastHeardSound { get; private set; }
        public bool IgnoreUnderFire { get; private set; }
        public bool IgnoreHearing { get; private set; }

        private const float IMPACT_HEAR_FREQUENCY = 0.5f;
        private const float IMPACT_HEAR_FREQUENCY_FAR = 0.05f;
        private const float IMPACT_MAX_HEAR_DISTANCE = 50f * 50f;
        private const float IMPACT_DISPERSION = 5f * 5f;

        public HearingInputClass(SAINHearingSensorClass hearing) : base(hearing)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
            SAINBotController.Instance.AISoundPlayed += soundHeard;
            SAINBotController.Instance.BulletImpact += bulletImpacted;
        }

        public void Update()
        {
            if (IgnoreHearing &&
                _ignoreUntilTime > 0 &&
                _ignoreUntilTime < Time.time)
            {
                IgnoreHearing = false;
                IgnoreUnderFire = false;
            }
        }

        public void Dispose()
        {
            SAINBotController.Instance.AISoundPlayed -= soundHeard;
            SAINBotController.Instance.BulletImpact -= bulletImpacted;
        }

        private void soundHeard(
            SAINSoundType soundType,
            Vector3 soundPosition,
            PlayerComponent playerComponent,
            float power,
            float volume)
        {
            if (volume <= 0 || !_canHearSounds)
            {
                return;
            }
            bool isGunshot = soundType.IsGunShot();
            if (IgnoreHearing && !isGunshot)
            {
                return;
            }
            Enemy enemy = Bot.EnemyController.GetEnemy(playerComponent.ProfileId, true);
            if (enemy == null)
            {
                return;
            }
            float baseRange = power * volume;
            if (!isGunshot &&
                enemy.RealDistance > baseRange)
            {
                return;
            }

            var info = new SoundInfoData
            {
                EnemyPlayer = playerComponent,
                IsAI = playerComponent.IsAI,
                OriginalPosition = soundPosition,
                Power = power,
                Volume = volume,
                SoundType = soundType,
                IsGunShot = isGunshot,
                Enemy = enemy,
                EnemyDistance = enemy.RealDistance,
            };
            BotSoundStruct sound = new BotSoundStruct(info, baseRange);
            BaseClass.ReactToHeardSound(sound);
        }

        private void bulletImpacted(EftBulletClass bullet)
        {
            if (!_canHearSounds)
            {
                return;
            }
            if (_nextHearImpactTime > Time.time)
            {
                return;
            }
            if (Bot.HasEnemy)
            {
                return;
            }
            var player = bullet.Player?.iPlayer;
            if (player == null)
            {
                return;
            }
            var enemy = Bot.EnemyController.GetEnemy(player.ProfileId, true);
            if (enemy == null)
            {
                return;
            }
            if (Bot.PlayerComponent.AIData.PlayerLocation.InBunker != enemy.EnemyPlayerComponent.AIData.PlayerLocation.InBunker)
            {
                return;
            }
            float distance = (bullet.CurrentPosition - Bot.Position).sqrMagnitude;
            if (distance > IMPACT_MAX_HEAR_DISTANCE)
            {
                _nextHearImpactTime = Time.time + IMPACT_HEAR_FREQUENCY_FAR;
                return;
            }
            _nextHearImpactTime = Time.time + IMPACT_HEAR_FREQUENCY;

            float dispersion = distance / IMPACT_DISPERSION;
            Vector3 random = UnityEngine.Random.onUnitSphere;
            random.y = 0;
            random = random.normalized * dispersion;
            Vector3 estimatedPos = enemy.EnemyPosition + random;
            enemy.Hearing.SetHeard(estimatedPos, SAINSoundType.BulletImpact, true);
        }

        private bool _canHearSounds => Bot.BotActive && !Bot.GameEnding;

        public bool SetIgnoreHearingExternal(bool value, bool ignoreUnderFire, float duration, out string reason)
        {
            if (Bot.Enemy?.IsVisible == true)
            {
                reason = "Enemy Visible";
                return false;
            }
            if (BotOwner.Memory.IsUnderFire && !ignoreUnderFire)
            {
                reason = "Under Fire";
                return false;
            }

            IgnoreUnderFire = ignoreUnderFire;
            IgnoreHearing = value;
            if (value && duration > 0f)
            {
                _ignoreUntilTime = Time.time + duration;
            }
            else
            {
                _ignoreUntilTime = -1f;
            }
            reason = string.Empty;
            return true;
        }

        private float _nextHearImpactTime;
        private float _ignoreUntilTime;
    }
}