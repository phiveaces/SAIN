using SAIN.Helpers.Events;
using System;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyEvents : EnemyBase, IBotClass
    {
        public EnemyToggleEvent OnEnemyLineOfSightChanged { get; }
        public EnemyToggleEvent OnEnemyKnownChanged { get; }
        public EnemyToggleEvent OnActiveThreatChanged { get; }
        public EnemyToggleEvent OnVisionChange { get; }
        public EnemyToggleEventTimeTracked OnSearch { get; }

        public event Action<Enemy> OnEnemyInvalid;
        public event Action<Enemy> OnEnemyLocationsSearched;
        public event Action<Enemy> OnFirstSeen;
        public event Action<Enemy> OnEnemyShot;
        public event Action<Enemy> OnBeingShotByEnemy;
        public event Action<Enemy, EnemyPlace> OnPositionUpdated;
        public event Action<Enemy, SAINSoundType, bool> OnEnemyHeard;
        public event Action<Enemy, NavMeshPathStatus> OnPathUpdated;
        public event Action<Enemy, EEnemyAction> OnVulnerableStateChanged;
        public event Action<Enemy, ETagStatus> OnHealthStatusChanged;

        public EnemyEvents(Enemy enemy) : base (enemy)
        {
            OnEnemyLineOfSightChanged = new EnemyToggleEvent(enemy, false);
            OnEnemyKnownChanged = new EnemyToggleEvent(enemy, false);
            OnActiveThreatChanged = new EnemyToggleEvent(enemy, false);
            OnVisionChange = new EnemyToggleEvent(enemy, false);
            OnSearch = new EnemyToggleEventTimeTracked(enemy, false);
        }

        public void Init()
        {
            EnemyPlayer.BeingHitAction += enemyHit;
        }

        public void Update()
        {

        }

        public void Dispose()
        {
            var player = EnemyPlayer;
            if (player != null)
                player.BeingHitAction -= enemyHit;
        }

        public void EnemyLocationsSearched()
        {
            OnEnemyLocationsSearched?.Invoke(Enemy);
        }

        public void LastKnownUpdated(EnemyPlace place)
        {
            OnPositionUpdated?.Invoke(Enemy, place);
        }

        public void SetEnemyAsInvalid()
        {
            OnEnemyInvalid?.Invoke(Enemy);
        }

        public void ShotByEnemy()
        {
            OnBeingShotByEnemy?.Invoke(Enemy);
        }

        public void HealthStatusChanged(ETagStatus status)
        {
            OnHealthStatusChanged?.Invoke(Enemy, status);
        }

        public void EnemyVulnerableChanged(EEnemyAction action)
        {
            OnVulnerableStateChanged?.Invoke(Enemy, action);
        }

        public void PathUpdated(NavMeshPathStatus status)
        {
            OnPathUpdated?.Invoke(Enemy, status);
        }

        public void EnemyFirstSeen()
        {
            OnFirstSeen?.Invoke(Enemy);
        }

        public void EnemyHeard(SAINSoundType type, bool gunFire)
        {
            OnEnemyHeard?.Invoke(Enemy, type, gunFire);
        }

        private void enemyHit(DamageInfo damage, EBodyPart _, float _2)
        {
            var damageSource = damage.Player?.iPlayer;
            if (damageSource == null)
            {
                return;
            }
            if (damageSource.ProfileId == Bot.ProfileId)
            {
                OnEnemyShot?.Invoke(Enemy);
            }
        }

        public class EnemyToggleEvent : ToggleEventForObject<Enemy>
        {
            public EnemyToggleEvent(Enemy enemy, bool defaultValue) : base(enemy, defaultValue)
            {
            }
        }

        public class EnemyToggleEventTimeTracked : ToggleEventForObjectTimeTracked<Enemy>
        {
            public EnemyToggleEventTimeTracked(Enemy enemy, bool defaultValue) : base(enemy, defaultValue)
            {
            }
        }
    }
}