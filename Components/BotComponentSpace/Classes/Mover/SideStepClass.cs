using EFT;
using SAIN.Preset.GlobalSettings;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SideStepClass : BotBase, IBotClass
    {
        public SideStepClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            float currentSideStep = CurrentSideStep;
            if (SideStepSetting != SideStepSetting.None && currentSideStep == 0f)
            {
                SideStepSetting = SideStepSetting.None;
            }

            if (!Bot.SAINLayersActive)
            {
                ResetSideStep(currentSideStep);
                return;
            }

            var enemy = Bot.Enemy;
            var CurrentDecision = Bot.Decision.CurrentSoloDecision;
            if (enemy == null || CurrentDecision != CombatDecision.HoldInCover)
            {
                ResetSideStep(currentSideStep);
                return;
            }
            if (GlobalSettingsClass.Instance.AILimit.LimitAIvsAIGlobal && 
                enemy.IsAI && 
                Bot.CurrentAILimit != AILimitSetting.None)
            {
                ResetSideStep(currentSideStep);
                return;
            }

            if (enemy.CanShoot)
            {
                if (ResetCanShoot == -1f)
                {
                    ResetCanShoot = Time.time + 2f;
                }
                if (ResetCanShoot < Time.time)
                {
                    ResetSideStep(currentSideStep);
                }
                return;
            }
            else
            {
                ResetCanShoot = -1f;
            }

            if (SideStepTimer > Time.time)
            {
                return;
            }

            float value;
            switch (Bot.Mover.Lean.LeanDirection)
            {
                case LeanSetting.Left:
                    value = -1f;
                    SideStepSetting = SideStepSetting.Left;
                    break;

                case LeanSetting.Right:
                    value = 1f;
                    SideStepSetting = SideStepSetting.Right;
                    break;

                default:
                    value = 0f;
                    SideStepSetting = SideStepSetting.None;
                    break;
            }

            if (value != 0f)
            {
                SideStepTimer = Time.time + 2f;
            }
            else
            {
                SideStepTimer = Time.time + 0.5f;
            }

            SetSideStep(value, currentSideStep);
        }

        public void Dispose()
        {
        }

        public SideStepSetting SideStepSetting { get; private set; }

        public void ResetSideStep(float current)
        {
            SideStepSetting = SideStepSetting.None;
            if (current != 0f)
            {
                Player.MovementContext.SetSidestep(0f);
            }
        }

        private float ResetCanShoot;

        public bool SideStepActive => SideStepSetting != SideStepSetting.None && CurrentSideStep != 0f;

        private float SideStepTimer = 0f;

        public void SetSideStep(float value, float current)
        {
            if (current != value)
            {
                Player.MovementContext.SetSidestep(value);
            }
        }

        public float CurrentSideStep => Player.MovementContext.GetSidestep();
    }
}