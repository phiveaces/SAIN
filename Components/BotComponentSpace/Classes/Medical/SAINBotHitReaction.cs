using EFT;
using EFT.HealthSystem;
using RootMotion.FinalIK;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class BotHitByEnemyClass : BotMedicalBase, IBotClass
    {
        public Enemy EnemyWhoLastShotMe { get; private set; }

        public BotHitByEnemyClass(SAINBotMedicalClass medical) : base(medical)
        {
        }

        public void Init()
        {
            Bot.EnemyController.Events.OnEnemyRemoved += clearEnemy;
        }

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
            var player = damageInfo.Player?.iPlayer;
            if (player == null)
            {
                return;
            }
            Enemy enemy = Bot.EnemyController.GetEnemy(player.ProfileId, true);
            if (enemy == null)
            {
                return;
            }
            EnemyWhoLastShotMe = enemy;
            enemy.Status.GetHit(damageInfo);
        }

        private void clearEnemy(string profileId, Enemy enemy)
        {
            if (enemy == EnemyWhoLastShotMe)
                EnemyWhoLastShotMe = null;
        }

        public void Update()
        {

        }

        public void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= clearEnemy;
        }
    }

    public class SAINBotHitReaction : BotMedicalBase, IBotClass
    {
        public EHitReaction HitReaction { get; private set; }
        public IHealthController HealthController => Player.HealthController;
        public BodyPartHitEffectClass HitEffects { get; private set; }

        public SAINBotHitReaction(SAINBotMedicalClass medical) : base(medical)
        {
            HitEffects = new BodyPartHitEffectClass(medical);

            addPart(EBodyPart.Head);
            addPart(EBodyPart.Chest);
            addPart(EBodyPart.LeftArm);
            addPart(EBodyPart.RightArm);
            addPart(EBodyPart.LeftLeg);
            addPart(EBodyPart.RightLeg);
            addPart(EBodyPart.Stomach);
        }

        private void addPart(EBodyPart part) {
            BodyParts.Add(part, new BodyPartStatus(part, this));
        }

        public void Init()
        {
            HitEffects.Init();
        }

        public void Update()
        {
            HitEffects.Update();
        }

        public EInjurySeverity LeftArmInjury { get; private set; }
        public EInjurySeverity RightArmInjury { get; private set; }

        public bool ArmsInjured => HitEffects.LeftArmInjury != EInjurySeverity.None || HitEffects.RightArmInjury != EInjurySeverity.None;

        public void Dispose()
        {
            HitEffects?.Dispose();
            BodyParts.Clear();
        }

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
            HitEffects.GetHit(damageInfo, bodyPart, floatVal);
        }

        const float StunDamageThreshold = 50;
        const float BaseStunTime = 3f;

        private float TimeStunHappened;
        private float StunTime;

        public bool IsStunned 
        { 
            get
            {
                if (_isStunned && StunTime < Time.time)
                {
                    _isStunned = false;
                }
                return _isStunned;
            }
            set
            {
                if (value)
                {
                    TimeStunHappened = Time.time;
                    StunTime = Time.time + BaseStunTime * UnityEngine.Random.Range(0.75f, 1.25f);
                }
                _isStunned = value;
            }
        }

        private bool _isStunned;

        private bool IsStunnedFromDamage(DamageInfo damageInfo)
        {
            return false;
        }

        public Dictionary<EBodyPart, BodyPartStatus> BodyParts = new Dictionary<EBodyPart, BodyPartStatus>();
    }
}