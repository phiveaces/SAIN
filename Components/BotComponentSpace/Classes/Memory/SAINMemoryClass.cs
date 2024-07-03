using EFT;
using HarmonyLib;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class SAINMemoryClass : BotBase, IBotClass
    {
        public IPlayer LastUnderFireSource { get; private set; }
        public Enemy LastUnderFireEnemy { get; private set; }
        public Vector3 UnderFireFromPosition { get; set; }

        public EnemyTargetsClass EnemyTargets { get; } = new EnemyTargetsClass();
        public SAINExtract Extract { get; } = new SAINExtract();
        public HealthTracker Health { get; private set; }
        public LocationTracker Location { get; private set; }

        public SAINMemoryClass(BotComponent sain) : base(sain)
        {
            Health = new HealthTracker(sain);
            Location = new LocationTracker(sain);
        }

        public void Init()
        {
            Bot.EnemyController.Events.OnEnemyRemoved += clearEnemy;
        }

        public void Update()
        {
            Health.Update();
            Location.Update();
            checkResetUnderFire();
        }

        public void Dispose()
        {
            Bot.EnemyController.Events.OnEnemyRemoved -= clearEnemy;
        }

        public void SetUnderFire(IPlayer source, Vector3 position)
        {
            if (source != null)
            {
                try
                {
                    BotOwner.Memory.SetUnderFire(source);
                }
                catch { }

                LastUnderFireSource = source;
                UnderFireFromPosition = position;

                var enemy = Bot.EnemyController.GetEnemy(source.ProfileId, true);

                if (enemy != null)
                    LastUnderFireEnemy = enemy;
            }
        }

        private void clearEnemy(string profileId, Enemy enemy)
        {
            if (LastUnderFireEnemy == enemy)
            {
                LastUnderFireEnemy = null;
                resetUnderFire();
            }
        }

        private void checkResetUnderFire()
        {
            if (_nextCheckDeadTime < Time.time)
            {
                _nextCheckDeadTime = Time.time + 0.5f; 
                resetUnderFire();
            }
        }

        private void resetUnderFire()
        {
            if (BotOwner.Memory.IsUnderFire &&
                (LastUnderFireSource == null || LastUnderFireSource.HealthController.IsAlive == false))
            {
                _underFireTimeField.SetValue(BotOwner.Memory, Time.time);
            }
        }

        static SAINMemoryClass()
        {
            _underFireTimeField = AccessTools.Field(typeof(BotMemoryClass), "float_4");
        }

        private float _nextCheckDeadTime;
        private static FieldInfo _underFireTimeField;
    }
}