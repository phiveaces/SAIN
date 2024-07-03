using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;

// Found in Botowner.Looksensor
using EnemyVisionCheck = GClass500;
using LookAllData = GClass521;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotLookClass : BotBase
    {
        public SAINBotLookClass(BotComponent component) : base(component)
        {
            LookData = new LookAllData();
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
            _enemies = Bot.EnemyController.Enemies;
        }

        public void Dispose()
        {
        }

        private Dictionary<string, Enemy> _enemies;
        public readonly LookAllData LookData;

        public int UpdateLook()
        {
            if (BotOwner.LeaveData == null ||
                BotOwner.LeaveData.LeaveComplete)
            {
                return 0;
            }

            int numUpdated = UpdateLookForEnemies(LookData);
            UpdateLookData(LookData);
            return numUpdated;
        }

        public void UpdateLookData(LookAllData lookData)
        {
            for (int i = 0; i < lookData.reportsData.Count; i++) {
                EnemyVisionCheck enemyVision = lookData.reportsData[i];
                BotOwner.BotsGroup.ReportAboutEnemy(enemyVision.enemy, enemyVision.VisibleOnlyBuSence);
            }

            if (lookData.reportsData.Count > 0)
                BotOwner.Memory.SetLastTimeSeeEnemy();

            if (lookData.shallRecalcGoal)
                BotOwner.CalcGoal();

            lookData.Reset();
        }

        private int UpdateLookForEnemies(LookAllData lookAll)
        {
            int updated = 0;
            _cachedList.Clear();
            _cachedList.AddRange(_enemies.Values);
            foreach (Enemy enemy in _cachedList) {

                if (!shallCheckEnemy(enemy))
                    continue;

                if (checkEnemy(enemy, lookAll)) {
                    updated++;
                }
            }
            _cachedList.Clear();
            return updated;
        }

        private readonly List<Enemy> _cachedList = new List<Enemy>();

        private bool shallCheckEnemy(Enemy enemy)
        {
            if (enemy?.IsValid != true)
                return false;

            if (!enemy.InLineOfSight)
            {
                if (enemy.EnemyInfo.IsVisible)
                    enemy.EnemyInfo.SetVisible(false);
                return false;
            }
            return true;
        }

        private bool checkEnemy(Enemy enemy, LookAllData lookAll)
        {
            float delay = getDelay(enemy);
            if (enemy.LastCheckLookTime + delay > Time.time)
            {
                return false;
            }

            enemy.LastCheckLookTime = Time.time;
            enemy.EnemyInfo.CheckLookEnemy(lookAll);
            return true;
        }

        private float getDelay(Enemy enemy)
        {
            float delay;
            if (enemy.EnemyPerson.Active) {
                delay = enemy.IsAI ? 0.2f : 0.1f;
            }
            else {
                delay = 0.5f;
            }
            return delay * UnityEngine.Random.Range(0.9f, 1.1f);
        }

    }
}