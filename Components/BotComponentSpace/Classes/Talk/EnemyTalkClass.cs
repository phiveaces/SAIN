using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.BotSettings.SAINSettings;
using SAIN.Preset.Personalities;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class EnemyTalk : BotBaseClass, ISAINClass
    {
        public EnemyTalk(BotComponent bot) : base(bot)
        {
            _randomizationFactor = Random.Range(0.75f, 1.25f);
        }

        public void Init()
        {
            base.SubscribeToPresetChanges(UpdatePresetSettings);
            if (Singleton<BotEventHandler>.Instance != null)
            {
                Singleton<BotEventHandler>.Instance.OnGrenadeExplosive += tryFakeDeathGrenade;
            }
            SAINBotController.Instance.PlayerTalk += playerTalked;
            Bot.EnemyController.Events.OnEnemyKilled += enemyKilled;
        }

        private void enemyKilled(Player player)
        {
            if (Bot.Talk.CanTalk &&
                CanTaunt &&
                EFTMath.RandomBool(70))
            {
                EPhraseTrigger trigger;
                if (EFTMath.RandomBool(15) ||
                    (Bot.Memory.Health.HealthStatus == ETagStatus.Healthy && EFTMath.RandomBool(50)))
                {
                    trigger = EPhraseTrigger.GoodWork;
                }
                else if (EFTMath.RandomBool(25))
                {
                    trigger = EPhraseTrigger.BadWork;
                }
                else
                {
                    trigger = EPhraseTrigger.OnFight;
                }
                Bot.Talk.Say(trigger, ETagStatus.Combat, false);
            }
        }

        public void Update()
        {
            float time = Time.time;
            if (_nextCheckTime < time)
            {
                if (ShallBegForLife())
                {
                    _nextCheckTime = time + 1f;
                    return;
                }
                if (Bot?.Enemy != null)
                {
                    if (ShallFakeDeath())
                    {
                        _nextCheckTime = time + 15f;
                        return;
                    }
                    if (CanTaunt
                        && _tauntTimer < time)
                    {
                        _nextCheckTime = time + 1f;
                        _tauntTimer = time + TauntFreq * Random.Range(0.5f, 1.5f);

                        if (EFTMath.RandomBool(PersonalitySettings.TauntChance))
                        {
                            TauntEnemy();
                        }
                        return;
                    }
                }
                _nextCheckTime = time + 0.25f;
            }
        }

        private float _nextCheckTime;
        private float _randomizationFactor = 1f;

        public void Dispose()
        {
            if (Singleton<BotEventHandler>.Instance != null)
            {
                Singleton<BotEventHandler>.Instance.OnGrenadeExplosive -= tryFakeDeathGrenade;
            }
            SAINBotController.Instance.PlayerTalk -= playerTalked;
            if (Bot?.EnemyController != null)
            {
                Bot.EnemyController.Events.OnEnemyKilled -= enemyKilled;
            }
        }

        private PersonalityTalkSettings PersonalitySettings => Bot?.Info?.PersonalitySettings.Talk;
        private SAINSettingsClass FileSettings => Bot?.Info?.FileSettings;

        private float FakeDeathChance = 2f;

        protected void UpdatePresetSettings(SAINPresetClass preset)
        {
            if (PersonalitySettings != null && FileSettings != null)
            {
                CanFakeDeath = PersonalitySettings.CanFakeDeathRare;
                FakeDeathChance = PersonalitySettings.FakeDeathChance;
                CanBegForLife = PersonalitySettings.CanBegForLife;
                CanTaunt = PersonalitySettings.CanTaunt && FileSettings.Mind.BotTaunts;
                TauntDist = PersonalitySettings.TauntMaxDistance * _randomizationFactor;
                TauntFreq = PersonalitySettings.TauntFrequency * _randomizationFactor;
                _canRespondToEnemy = PersonalitySettings.CanRespondToEnemyVoice;
            }
            else
            {
                Logger.LogAndNotifyError("Personality settings or filesettings are null! Cannot Apply Settings!");
            }

            var talkSettings = preset.GlobalSettings.Talk;
            _friendlyResponseChance = talkSettings.FriendlyReponseChance;
            _friendlyResponseChanceAI = talkSettings.FriendlyReponseChanceAI;
            _friendlyResponseDistance = talkSettings.FriendlyReponseDistance;
            _friendlyResponseDistanceAI = talkSettings.FriendlyReponseDistanceAI;
            _friendlyResponseFrequencyLimit = talkSettings.FriendlyResponseFrequencyLimit;
            _friendlyResponseMinRandom = talkSettings.FriendlyResponseMinRandomDelay;
            _friendlyResponseMaxRandom = talkSettings.FriendlyResponseMaxRandomDelay;
        }

        private bool CanTaunt = true;
        private bool CanFakeDeath = false;
        private bool CanBegForLife = false;
        private bool _canRespondToEnemy = true;
        private float TauntDist = 40f;
        private float TauntFreq = 30f;

        private bool ShallFakeDeath()
        {
            if (CanFakeDeath
                && EFTMath.RandomBool(FakeDeathChance)
                && Bot.Enemy != null
                && !Bot.Squad.BotInGroup
                && _fakeDeathTimer < Time.time
                && (Bot.Memory.Health.HealthStatus == ETagStatus.Dying || Bot.Memory.Health.HealthStatus == ETagStatus.BadlyInjured)
                && (Bot.Enemy.EnemyPosition - BotOwner.Position).sqrMagnitude < 70f * 70f)
            {
                _fakeDeathTimer = Time.time + 30f;
                Bot.Talk.Say(EPhraseTrigger.OnDeath);
                return true;
            }
            return false;
        }

        private void tryFakeDeathGrenade(Vector3 grenadeExplosionPosition, string playerProfileID, bool isSmoke, float smokeRadius, float smokeLifeTime)
        {
            if (CanFakeDeath
                && EFTMath.RandomBool(FakeDeathChance)
                && !isSmoke
                && Bot.Enemy != null
                && _fakeDeathTimer < Time.time
                && playerProfileID != Bot.ProfileId
                && (grenadeExplosionPosition - Bot.Position).sqrMagnitude < 25f * 25f)
            {
                _fakeDeathTimer = Time.time + 30f;
                Bot.Talk.Say(EPhraseTrigger.OnDeath);
            }
        }

        private bool ShallBegForLife()
        {
            if (_begTimer > Time.time)
            {
                return false;
            }
            if (!EFTMath.RandomBool(25))
            {
                _begTimer = Time.time + 10f;
                return false;
            }
            Vector3? currentTarget = Bot.CurrentTargetPosition;
            if (currentTarget == null)
            {
                _begTimer = Time.time + 10f;
                return false;
            }

            _begTimer = Time.time + 3f;

            bool shallBeg = Bot.Info.Profile.IsPMC ? !Bot.Squad.BotInGroup : Bot.Info.Profile.IsScav;
            if (shallBeg
                && CanBegForLife
                && Bot.Memory.Health.HealthStatus != ETagStatus.Healthy
                && (currentTarget.Value - Bot.Position).sqrMagnitude < 50f * 50f)
            {
                IsBeggingForLife = true;
                Bot.Talk.Say(BegPhrases.PickRandom());
                return true;
            }
            return false;
        }

        public bool IsBeggingForLife
        {
            get
            {
                if (_isBegging && _beggingTimer < Time.time)
                {
                    _isBegging = false;
                }
                return _isBegging;
            }
            private set
            {
                if (value)
                {
                    _beggingTimer = Time.time + 60f;
                }
                _isBegging = value;
            }
        }

        private bool TauntEnemy()
        {
            bool tauntEnemy = false;
            var enemy = Bot.Enemy;

            if (!canTauntEnemy(enemy))
            {
                return false;
            }

            if (Bot.Info.PersonalitySettings.Talk.ConstantTaunt)
            {
                tauntEnemy = EFTMath.RandomBool(50);
            }
            if (!tauntEnemy &&
                (enemy.IsVisible || enemy.TimeSinceSeen < 10f))
            {
                tauntEnemy = enemy.EnemyLookingAtMe || Bot.Info.PersonalitySettings.Talk.FrequentTaunt;
            }

            if (tauntEnemy)
            {
                if (enemy != null
                    && !enemy.IsVisible
                    && enemy.Seen
                    && enemy.TimeSinceSeen > 30f
                    && EFTMath.RandomBool(20))
                {
                    Bot.Talk.Say(EPhraseTrigger.OnLostVisual, ETagStatus.Combat, true);
                }
                else
                {
                    EPhraseTrigger trigger = EFTMath.RandomBool(95) ? EPhraseTrigger.OnFight : EPhraseTrigger.BadWork;
                    Bot.Talk.Say(trigger, ETagStatus.Combat, false);
                }
            }

            return tauntEnemy;
        }

        private bool canTauntEnemy(Enemy enemy)
        {
            if (enemy == null)
            {
                return false;
            }
            if (!enemy.Seen && !enemy.Heard)
            {
                return false;
            }
            if (enemy.KnownPlaces.EnemyDistanceFromLastKnown > TauntDist)
            {
                return false;
            }
            if (Bot.Decision.IsSearching)
            {
                return true;
            }
            if (!enemy.Status.ShotByEnemyRecently && !enemy.Status.ShotAtMeRecently)
            {
                if (!enemy.Seen)
                {
                    return false;
                }
                if (enemy.TimeSinceSeen > 90f)
                {
                    return false;
                }
                if (enemy.TimeSinceHeard > 20f)
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerator RespondToVoice(EPhraseTrigger trigger, ETagStatus mask, float delay, Player sourcePlayer, float responseDist, float chance = 100f, bool friendly = false)
        {
            if (!EFTMath.RandomBool(chance))
            {
                yield break;
            }
            if (sourcePlayer == null ||
                BotOwner == null ||
                Player == null ||
                Bot == null ||
                sourcePlayer.ProfileId == Bot.ProfileId)
            {
                yield break;
            }
            if ((sourcePlayer.Position - Bot.Position).sqrMagnitude > responseDist * responseDist)
            {
                yield break;
            }

            yield return new WaitForSeconds(delay);

            if (sourcePlayer == null ||
                BotOwner == null ||
                Player == null ||
                Bot == null)
            {
                yield break;
            }
            if (!sourcePlayer.HealthController.IsAlive
                || !Player.HealthController.IsAlive)
            {
                yield break;
            }

            if (friendly)
            {
                if (!BotOwner.Memory.IsPeace)
                {
                    Bot.Talk.Say(trigger, null, false);
                    yield break;
                }
                if (_nextGestureTime < Time.time)
                {
                    _nextGestureTime = Time.time + 6f;
                    Player.HandsController.ShowGesture(EGesture.Hello);
                    Bot.Steering.LookToPoint(sourcePlayer.Position + Vector3.up * 1.4f);
                }
                Bot.Talk.Say(trigger, mask, false);
                yield break;
            }

            if (Bot.Talk.Say(trigger, mask, false))
            {
            }
        }

        public void SetEnemyTalk(Player player)
        {
            if (_canRespondToEnemy &&
                _nextResponseTime < Time.time)
            {
                float chance = 60;
                EPhraseTrigger trigger = EFTMath.RandomBool(92.5f) ? EPhraseTrigger.OnFight : EPhraseTrigger.BadWork;

                //if (player.IsYourPlayer)
                //    Logger.LogInfo($"Starting Responding To Voice! Min Dist: {TauntDist} Chance: {chance}");

                _nextResponseTime = Time.time + 2f;

                Bot.StartCoroutine(RespondToVoice(
                    trigger,
                    ETagStatus.Combat,
                    Random.Range(0.4f, 0.75f),
                player,
                TauntDist,
                chance
                ));
            }
        }

        private void playerTalked(EPhraseTrigger phrase, ETagStatus mask, Player player)
        {
            if (Bot == null || !Bot.BotActive || player == null)
            {
                return;
            }
            if (Bot.ProfileId == player.ProfileId)
            {
                return;
            }

            bool isPain = phrase == EPhraseTrigger.OnAgony || phrase == EPhraseTrigger.OnBeingHurt;
            float painRange = 50f;
            float breathRange = player.HeavyBreath ? 35f : 15f;

            Enemy enemy = Bot.EnemyController.GetEnemy(player.ProfileId, true);
            if (enemy == null)
            {
                if (!isPain &&
                    phrase != EPhraseTrigger.OnBreath &&
                    phrase != EPhraseTrigger.OnFight)
                {
                    SetFriendlyTalked(player);
                }
                return;
            }

            if (isPain)
            {
                if (enemy.RealDistance <= painRange)
                {
                    Vector3 randomizedPos = randomizePos(player.Position, enemy.RealDistance, 20f);
                    enemy.Hearing.SetHeard(randomizedPos, SAINSoundType.Pain, true);
                }
                return;
            }
            if (phrase == EPhraseTrigger.OnBreath)
            {
                if (enemy.RealDistance <= breathRange)
                {
                    Vector3 randomizedPos = randomizePos(player.Position, enemy.RealDistance, 20f);
                    enemy.Hearing.SetHeard(randomizedPos, SAINSoundType.Breathing, true);
                }
                return;
            }

            if (enemy.RealDistance <= 65f)
            {
                Vector3 randomizedPos = randomizePos(player.Position, enemy.RealDistance, 20f);
                enemy.Hearing.SetHeard(randomizedPos, SAINSoundType.Breathing, true);
            }

            if (phrase == EPhraseTrigger.OnFight)
            {
                SetEnemyTalk(player);
            }
        }

        private Vector3 randomizePos(Vector3 position, float distance, float dispersionFactor = 20f)
        {
            float disp = distance / dispersionFactor;
            Vector3 random = UnityEngine.Random.insideUnitSphere * disp;
            random.y = 0;
            return position + random;
        }

        private float _nextGestureTime;

        private bool shallBeChatty()
        {
            if (Bot.Info.Profile.IsScav)
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeScavs;
            }
            if (Bot.Info.Profile.IsPMC)
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativePMCs;
            }
            var role = Bot.Info.Profile.WildSpawnType;
            if ((Bot.Info.Profile.IsBoss || Bot.Info.Profile.IsFollower))
            {
                if ((role == WildSpawnType.bossKnight || role == WildSpawnType.followerBirdEye || role == WildSpawnType.followerBigPipe))
                {
                    return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeGoons;
                }
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeBosses;
            }
            if ((role == WildSpawnType.pmcBot || role == WildSpawnType.exUsec))
            {
                return SAINPlugin.LoadedPreset.GlobalSettings.Talk.TalkativeRaidersRogues;
            }
            return false;
        }

        public void SetFriendlyTalked(Player player)
        {
            float maxDist = player.IsAI ? _friendlyResponseDistanceAI : _friendlyResponseDistance;
            if ((player.Position - Bot.Position).sqrMagnitude > maxDist * maxDist)
            {
                return;
            }

            if ((BotOwner.Memory.IsPeace || (Bot.Squad.HumanFriendClose && !player.IsAI))
                && _nextResponseTime < Time.time)
            {
                _nextResponseTime = Time.time + _friendlyResponseFrequencyLimit;

                if (player.IsAI == false || shallBeChatty())
                {
                    float chance = player.IsAI ? _friendlyResponseChanceAI : _friendlyResponseChance;

                    Bot.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.MumblePhrase,
                        Bot.EnemyController.AtPeace ? ETagStatus.Unaware : ETagStatus.Combat,
                        Random.Range(_friendlyResponseMinRandom, _friendlyResponseMaxRandom),
                        player,
                        maxDist,
                        chance
                    ));
                }
            }
            else if (Bot?.Squad.SquadInfo != null
                && Bot.Talk.GroupTalk.FriendIsClose
                && (Bot.Squad.SquadInfo.SquadPersonality != BotController.Classes.ESquadPersonality.GigaChads
                    || Bot.Squad.SquadInfo.SquadPersonality != BotController.Classes.ESquadPersonality.Elite)
                && (Bot.Info.Personality == EPersonality.GigaChad
                    || Bot.Info.Personality == EPersonality.Chad))
            {
                if (_saySilenceTime < Time.time)
                {
                    _saySilenceTime = Time.time + 20f;
                    Bot.StartCoroutine(RespondToVoice(
                        EPhraseTrigger.Silence,
                        Bot.EnemyController.AtPeace ? ETagStatus.Combat : ETagStatus.Aware,
                        Random.Range(0.2f, 0.5f),
                        player,
                        20f,
                        33f
                        ));
                }
            }
        }

        private float _friendlyResponseFrequencyLimit = 1f;
        private float _friendlyResponseMinRandom = 0.33f;
        private float _friendlyResponseMaxRandom = 0.75f;
        private float _saySilenceTime;
        private float _beggingTimer;
        private bool _isBegging;
        private float _fakeDeathTimer = 0f;
        private float _begTimer = 0f;

        private static readonly EPhraseTrigger[] BegPhrases =
        {
            EPhraseTrigger.Stop,
            EPhraseTrigger.HoldFire,
            EPhraseTrigger.GetBack
        };

        private float _friendlyResponseDistance = 60f;
        private float _friendlyResponseDistanceAI = 35f;
        private float _friendlyResponseChance = 85f;
        private float _friendlyResponseChanceAI = 80f;
        private float _nextResponseTime;
        private float _tauntTimer = 0f;
    }
}