using SAIN.Helpers;
using System.Collections.Generic;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyListsClass : BotSubClass<SAINEnemyController>, IBotClass
    {
        public readonly Dictionary<EEnemyListType, EnemyList> EnemyLists = new Dictionary<EEnemyListType, EnemyList>();

        public EnemyListsClass(SAINEnemyController controller) : base(controller)
        {
            foreach (var type in _types)
            {
                EnemyLists.Add(type, new EnemyList(type.ToString()));
            }
        }

        public EnemyList GetEnemyList(EEnemyListType type) {
            EnemyLists.TryGetValue(type, out EnemyList enemyList);
            return enemyList;
        }

        public Enemy First(EEnemyListType type) {
            return EnemyLists[type].First();
        }

        public int HumanCount(EEnemyListType type) {
            return EnemyLists[type].Humans;
        }

        public int TotalCount(EEnemyListType type) {
            return EnemyLists[type].Count;
        }

        public int BotCount(EEnemyListType type) {
            return EnemyLists[type].Bots;
        }

        public void Init()
        {
            Bot.EnemyController.Events.OnEnemyAdded += enemyAdded;
            Bot.EnemyController.Events.OnEnemyRemoved += enemyRemoved;
        }

        private void enemyAdded(Enemy enemy)
        {
            subOrUnSub(true, enemy);
        }

        private void enemyRemoved(string profileID, Enemy enemy)
        {
            subOrUnSub(false, enemy);
            foreach (var list in EnemyLists.Values)
            {
                list.RemoveEnemy(enemy);
            }
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            var controller = Bot.EnemyController;
            if (controller != null)
            {
                controller.Events.OnEnemyAdded -= enemyAdded;
                controller.Events.OnEnemyRemoved -= enemyRemoved;
            }

            foreach (var list in EnemyLists.Values)
            {
                list.Clear();
            }
            EnemyLists.Clear();
        }


        private void subOrUnSub(bool value, Enemy enemy)
        {
            EnemyList list;
            switch (value)
            {
                case true:

                    list = GetEnemyList(EEnemyListType.Known);
                    enemy.Events.OnEnemyKnownChanged.OnToggle += list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.ActiveThreats);
                    enemy.Events.OnActiveThreatChanged.OnToggle += list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.Visible);
                    enemy.Events.OnVisionChange.OnToggle += list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.InLineOfSight);
                    enemy.Events.OnEnemyLineOfSightChanged.OnToggle += list.AddOrRemoveEnemy;

                    break;

                case false:

                    list = GetEnemyList(EEnemyListType.Known);
                    enemy.Events.OnEnemyKnownChanged.OnToggle -= list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.ActiveThreats);
                    enemy.Events.OnActiveThreatChanged.OnToggle -= list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.Visible);
                    enemy.Events.OnVisionChange.OnToggle -= list.AddOrRemoveEnemy;

                    list = GetEnemyList(EEnemyListType.InLineOfSight);
                    enemy.Events.OnEnemyLineOfSightChanged.OnToggle -= list.AddOrRemoveEnemy;

                    break;
            }
        }

        private static readonly EEnemyListType[] _types = EnumValues.GetEnum<EEnemyListType>();
    }
}