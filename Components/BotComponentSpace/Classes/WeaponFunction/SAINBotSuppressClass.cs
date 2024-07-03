using BepInEx.Logging;
using EFT;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.SubComponents;
using SAIN.Components;
using System.Collections.Generic;
using UnityEngine;
using SAIN.Helpers;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class SAINBotSuppressClass : BotBase, IBotClass
    {
        public ESuppressionStatus SuppressionStatus { get; private set; }

        public SAINBotSuppressClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
            UpdateSuppressedStatus();
            applySuppressionStatModifiers();
        }

        private void applySuppressionStatModifiers()
        {
            setModifier(suppressedHeavyMod, IsHeavySuppressed);
            setModifier(suppressedMod, IsSuppressed && !IsHeavySuppressed);
        }

        private void setModifier(TemporaryStatModifiers modifiers, bool value)
        {
            if (value && !modifiers.Modifiers.IsApplyed)
            {
                BotOwner.Settings.Current.Apply(modifiers.Modifiers, -1f);
            }
            else if (!value && modifiers.Modifiers.IsApplyed)
            {
                BotOwner.Settings.Current.Dismiss(modifiers.Modifiers);
            }
        }

        TemporaryStatModifiers suppressedMod = new TemporaryStatModifiers(1.25f, 1.35f, 1.35f, 1.5f, 1.5f);
        TemporaryStatModifiers suppressedHeavyMod = new TemporaryStatModifiers(1.5f, 1.65f, 1.65f, 1.75f, 1.75f);

        public void Dispose()
        {
        }

        public float SuppressionNumber { get; private set; }
        public float SuppressionAmount => IsSuppressed ? SuppressionNumber - SuppressionThreshold : 0;
        public float SuppressionStatModifier => 1 + SuppressionAmount * SuppressionSpreadMultiPerPoint;
        public bool IsSuppressed => SuppressionNumber > SuppressionThreshold;
        public bool IsHeavySuppressed => SuppressionNumber > SuppressionHeavyThreshold;

        public readonly float SuppressionSpreadMultiPerPoint = 0.15f;
        private readonly float SuppressionThreshold = 5f;
        private readonly float SuppressionHeavyThreshold = 10f;
        private readonly float SuppressionDecayAmount = 0.25f;
        private readonly float SuppressionDecayUpdateFreq = 0.25f;
        private readonly float SuppressionAddDefault = 2f;
        private float SuppressionDecayTimer;

        public void AddSuppression(float distance, float num = -1)
        {
            if (num < 0)
            {
                num = SuppressionAddDefault;
            }
            SuppressionNumber += num;
            if (SuppressionNumber > 15)
            {
                SuppressionNumber = 15;
            }
        }

        public void UpdateSuppressedStatus()
        {
            if (SuppressionDecayTimer < Time.time && SuppressionNumber > 0)
            {
                SuppressionDecayTimer = Time.time + SuppressionDecayUpdateFreq;
                SuppressionNumber -= SuppressionDecayAmount;
                if (SuppressionNumber < 0)
                {
                    SuppressionNumber = 0;
                }
            }
        }
    }
}