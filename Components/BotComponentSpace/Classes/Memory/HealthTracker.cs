﻿using EFT;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class HealthTracker : BotBase, IBotClass
    {
        public event Action<ETagStatus> HealthStatusChanged;
        public bool Healthy => HealthStatus == ETagStatus.Healthy;
        public bool Injured => HealthStatus == ETagStatus.Injured;
        public bool BadlyInjured => HealthStatus == ETagStatus.BadlyInjured;
        public bool Dying => HealthStatus == ETagStatus.Dying;
        public ETagStatus HealthStatus { get; private set; }
        public bool OnPainKillers { get; private set; }

        public HealthTracker(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (_nextHealthUpdateTime < Time.time)
            {
                _nextHealthUpdateTime = Time.time + 0.5f;

                var oldStatus = HealthStatus;
                HealthStatus = Player.HealthStatus;
                if (HealthStatus != oldStatus)
                {
                    HealthStatusChanged?.Invoke(HealthStatus);
                }

                OnPainKillers = Player.MovementContext?.PhysicalConditionIs(EPhysicalCondition.OnPainkillers) == true;
            }
        }

        public void Dispose()
        {
        }

        private float _nextHealthUpdateTime = 0f;
    }
}
