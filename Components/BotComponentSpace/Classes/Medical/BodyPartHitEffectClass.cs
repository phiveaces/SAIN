using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class BodyPartHitEffectClass : BotMedicalBase, IBotClass
    {
        public EInjurySeverity LeftArmInjury { get; private set; }
        public EInjurySeverity RightArmInjury { get; private set; }
        public EHitReaction HitReaction { get; private set; }

        public BodyPartHitEffectClass(SAINBotMedicalClass medical) : base(medical)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (_updateHealthTime < Time.time)
            {
                checkArmInjuries();
            }
        }

        private void checkArmInjuries()
        {
            _updateHealthTime = Time.time + 1f;
            LeftArmInjury = Medical.HitReaction.BodyParts[EBodyPart.LeftArm].InjurySeverity;
            RightArmInjury = Medical.HitReaction.BodyParts[EBodyPart.RightArm].InjurySeverity;
        }

        public void Dispose()
        {

        }

        public void GetHit(DamageInfo damageInfo, EBodyPart bodyPart, float floatVal)
        {
            switch (bodyPart)
            {
                case EBodyPart.Head:
                    GetHitInHead(damageInfo);
                    break;

                case EBodyPart.Chest:
                case EBodyPart.Stomach:
                    GetHitInCenter(damageInfo);
                    break;

                case EBodyPart.LeftLeg:
                case EBodyPart.RightLeg:
                    GetHitInLegs(damageInfo);
                    break;

                default:
                    GetHitInArms(damageInfo);
                    break;
            }
        }

        private void GetHitInLegs(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Legs;
        }

        private void GetHitInArms(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Arms;
            checkArmInjuries();
        }

        private void GetHitInCenter(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Center;
        }

        private void GetHitInHead(DamageInfo damageInfo)
        {
            HitReaction = EHitReaction.Head;
        }

        private float _updateHealthTime;
    }
}