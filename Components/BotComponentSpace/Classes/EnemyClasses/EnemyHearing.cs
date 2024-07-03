using SAIN.Helpers;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyHearing : EnemyBase, ISAINEnemyClass
    {
        public bool Heard { get; private set; }
        public bool EnemyHeardFromPeace { get; set; }
        public float TimeSinceHeard => Time.time - _timeLastHeard;
        public BotSoundStruct? LastSoundHeard { get; set; }
        public Vector3? LastHeardPosition { get; private set; }

        private const float REPORT_HEARD_FREQUENCY = 1f;

        public EnemyHearing(Enemy enemy) : base(enemy)
        {

        }

        public void Init()
        {
            Enemy.Events.OnFirstSeen += resetHeardFromPeace;
        }

        public void Update()
        {

        }

        private void resetHeardFromPeace(Enemy enemy)
        {
            EnemyHeardFromPeace = false;
        }

        public void Dispose()
        {
            Enemy.Events.OnFirstSeen -= resetHeardFromPeace;
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
            if (!known)
            {
                Heard = false;
                LastSoundHeard = null;
                LastHeardPosition = null;
                _timeLastHeard = 0f;
                EnemyHeardFromPeace = false;
            }
        }

        public EnemyPlace SetHeard(Vector3 pos, SAINSoundType soundType, bool shallReport)
        {
            if (Enemy.IsVisible)
            {
                pos = Enemy.EnemyPosition;
            }

            bool wasGunfire = soundType.IsGunShot();
            Enemy.Status.HeardRecently = true;
            _timeLastHeard = Time.time;

            EnemyPlace place = UpdateHeardPosition(pos, wasGunfire, shallReport);

            if (!Bot.HasEnemy)
                EnemyHeardFromPeace = true;

            Enemy.Events.EnemyHeard(soundType, wasGunfire);
            if (wasGunfire || !shallReport)
            {
                return place;
            }
            updateEnemyAction(soundType, pos);
            return place;
        }

        private void updateEnemyAction(SAINSoundType soundType, Vector3 soundPosition)
        {
            EEnemyAction action;
            switch (soundType)
            {
                case SAINSoundType.GrenadeDraw:
                case SAINSoundType.GrenadePin:
                    action = EEnemyAction.HasGrenade;
                    break;

                case SAINSoundType.Reload:
                case SAINSoundType.DryFire:
                    action = EEnemyAction.Reloading;
                    break;

                case SAINSoundType.Looting:
                    action = EEnemyAction.Looting;
                    break;

                case SAINSoundType.Heal:
                    action = EEnemyAction.Healing;
                    break;

                case SAINSoundType.Surgery:
                    action = EEnemyAction.UsingSurgery;
                    break;

                default:
                    action = EEnemyAction.None;
                    break;
            }

            if (action != EEnemyAction.None)
            {
                Enemy.Status.SetVulnerableAction(action);
                Bot.Squad.SquadInfo.UpdateSharedEnemyStatus(EnemyIPlayer, action, Bot, soundType, soundPosition);
            }
        }

        public EnemyPlace UpdateHeardPosition(Vector3 position, bool wasGunfire, bool shallReport)
        {
            EnemyPlace place = Enemy.KnownPlaces.UpdatePersonalHeardPosition(position, wasGunfire);
            if (shallReport &&
                place != null &&
                _nextReportHeardTime < Time.time)
            {
                _nextReportHeardTime = Time.time + REPORT_HEARD_FREQUENCY;
                Bot.Squad?.SquadInfo?.ReportEnemyPosition(Enemy, place, false);
            }
            return place;
        }

        private float _nextReportHeardTime;

        public void OnEnemyKnownChanged(Enemy enemy, bool known)
        {

        }


        public float DispersionModifier
        {
            get
            {
                return 1f;
            }
        }

        private float angleModifier()
        {
            return 1f;
        }

        private float distanceModifier()
        {
            return 1f;
        }

        private float soundTypeModifier(SAINSoundType soundType)
        {
            return 1f;
        }

        private float weaponTypeModifier()
        {
            return 1f;
        }

        private float _timeLastHeard;
    }
}
