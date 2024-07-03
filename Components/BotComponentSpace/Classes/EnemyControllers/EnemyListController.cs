using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using System.Collections.Generic;
using System.Diagnostics;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyListController : BotSubClass<SAINEnemyController>, IBotClass
    {
        public Dictionary<string, Enemy> Enemies { get; } = new Dictionary<string, Enemy>();

        public EnemyListController(SAINEnemyController controller) : base(controller)
        {
        }

        public void Init()
        {
            GameWorldComponent.Instance.PlayerTracker.AlivePlayers.OnPlayerComponentRemoved += RemoveEnemy;
            BotOwner.Memory.OnAddEnemy += enemyAdded;
        }

        public void Update()
        {
            compareEnemyLists();
        }

        public void Dispose()
        {
            GameWorldComponent.Instance.PlayerTracker.AlivePlayers.OnPlayerComponentRemoved -= RemoveEnemy;
            BotMemoryClass memory = BotOwner?.Memory;
            if (memory != null)
            {
                memory.OnAddEnemy -= enemyAdded;
            }

            foreach (var enemy in Enemies)
            {
                destroyEnemy(enemy.Value);
            }
            Enemies?.Clear();
        }

        public Enemy GetEnemy(string profileID, bool mustBeActive)
        {
            if (!Enemies.TryGetValue(profileID, out Enemy enemy))
            {
                return null;
            }
            if (enemy == null || !enemy.IsValid)
            {
                destroyEnemy(enemy);
                Enemies.Remove(profileID);
                return null;
            }
            if (mustBeActive && !enemy.EnemyPerson.Active)
            {
                return null;
            }
            return enemy;
        }

        private void removeEnemy(PersonClass person)
        {
            RemoveEnemy(person.ProfileId);
        }

        public void RemoveEnemy(string profileId)
        {
            if (Enemies.TryGetValue(profileId, out Enemy enemy))
            {
                destroyEnemy(enemy);
                Enemies.Remove(profileId);
            }
        }

        private void destroyEnemy(Enemy enemy)
        {
            if (enemy == null)
                return;

            BaseClass.Events.EnemyRemoved(enemy.EnemyProfileId, enemy);
            enemy.Dispose();
            removeEnemyInfo(enemy);

            if (enemy.EnemyPlayerComponent != null)
                enemy.EnemyPlayerComponent.OnComponentDestroyed -= RemoveEnemy;
            if (enemy.EnemyPerson != null)
                enemy.EnemyPerson.ActiveClass.OnPersonDeadOrDespawned -= removeEnemy;
        }

        public Enemy CheckAddEnemy(IPlayer IPlayer)
        {
            return tryAddEnemy(IPlayer);
        }

        private void enemyAdded(IPlayer player)
        {
            tryAddEnemy(player);
        }

        public bool IsBotInBotsGroup(BotOwner botOwner)
        {
            int count = BotOwner.BotsGroup.MembersCount;
            for (int i = 0; i < count; i++)
            {
                var member = BotOwner.BotsGroup.Member(i);
                if (member == null) continue;
                if (member.name == botOwner.name) return true;
            }
            return false;
        }

        private Enemy tryAddEnemy(IPlayer enemyPlayer)
        {
            if (enemyPlayer == null)
            {
                //Logger.LogDebug("Cannot add null player as an enemy.");
                return null;
            }
            if (!enemyPlayer.HealthController.IsAlive)
            {
                //Logger.LogDebug("Cannot add dead player as an enemy.");
                return null;
            }
            if (enemyPlayer.ProfileId == Bot.Player.ProfileId)
            {
                string debugString = $"Cannot add enemy {getBotInfo(enemyPlayer)} that matches this bot {getBotInfo(Player)}: ";
                debugString = findSourceDebug(debugString);
                Logger.LogDebug(debugString);
                return null;
            }

            if (enemyPlayer.IsAI)
            {
                BotOwner botOwner = enemyPlayer.AIData?.BotOwner;
                if (botOwner == null)
                {
                    Logger.LogDebug("Cannot add ai as enemy with null Botowner");
                    return null;
                }
                if (IsBotInBotsGroup(botOwner))
                {
                    return null;
                }
            }

            PlayerComponent enemyPlayerComponent = getEnemyPlayerComponent(enemyPlayer);
            if (enemyPlayerComponent == null)
            {
                Logger.LogWarning("Cannot add enemy with null Player Component.");
                return null;
            }

            if (Enemies.TryGetValue(enemyPlayer.ProfileId, out Enemy sainEnemy))
            {
                return sainEnemy;
            }

            EnemyInfo enemyInfo = getEnemyInfo(enemyPlayer);
            if (enemyInfo == null)
            {
                //string debugString = $"Player [[{enemyPlayer.Profile.Nickname}] : Side: [{enemyPlayer.Profile.Side}]] is not in Botowner's [[{Player.Profile.Nickname}] : Side: [{Player.Profile.Side}]] EnemyInfos dictionary.: ";
                //Logger.LogDebug(debugString);
                return null;
            }

            return createEnemy(enemyPlayerComponent, enemyInfo);
        }

        private PlayerComponent getEnemyPlayerComponent(IPlayer enemyPlayer)
        {
            var playerTracker = GameWorldComponent.Instance.PlayerTracker;
            PlayerComponent enemyPlayerComponent = playerTracker.GetPlayerComponent(enemyPlayer.ProfileId);
            if (enemyPlayerComponent == null)
            {
                Logger.LogDebug("Cannot add enemy with null Player Component");
                if (Enemies.TryGetValue(enemyPlayer.ProfileId, out Enemy oldEnemy))
                {
                    destroyEnemy(oldEnemy);
                    Enemies.Remove(enemyPlayer.ProfileId);
                    Logger.LogDebug($"Removed Old Enemy.");
                }
                enemyPlayerComponent = playerTracker.AddPlayerManual(enemyPlayer);
                if (enemyPlayerComponent == null)
                {
                    Logger.LogError("Failed to recreate component!");
                }
            }
            return enemyPlayerComponent;
        }

        private EnemyInfo getEnemyInfo(IPlayer enemyPlayer)
        {
            if (!BotOwner.EnemiesController.EnemyInfos.TryGetValue(enemyPlayer, out EnemyInfo enemyInfo) &&
                BotOwner.BotsGroup.Enemies.TryGetValue(enemyPlayer, out BotSettingsClass value))
            {
                Logger.LogDebug($"Got EnemyInfo from Bot's Group Enemies.");
                enemyInfo = BotOwner.EnemiesController.AddNew(BotOwner.BotsGroup, enemyPlayer, value);
                if (enemyInfo != null)
                {
                    Logger.LogDebug($"Successfully Added new EnemyInfo.");
                }
            }
            return enemyInfo;
        }

        private Enemy createEnemy(PlayerComponent enemyPlayerComponent, EnemyInfo enemyInfo)
        {
            Enemy enemy = new Enemy(Bot, enemyPlayerComponent, enemyInfo);
            enemy.Init();
            enemyPlayerComponent.Person.ActiveClass.OnPersonDeadOrDespawned += removeEnemy;
            enemyPlayerComponent.OnComponentDestroyed += RemoveEnemy;
            Enemies.Add(enemy.EnemyProfileId, enemy);
            BaseClass.Events.EnemyAdded(enemy);
            return enemy;
        }


        public bool IsPlayerAnEnemy(string profileID)
        {
            return !profileID.IsNullOrEmpty() && Enemies.ContainsKey(profileID);
        }

        public bool IsPlayerFriendly(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                return false;
            }
            if (iPlayer.ProfileId == Bot.ProfileId)
            {
                return true;
            }

            if (Enemies.ContainsKey(iPlayer.ProfileId))
            {
                return false;
            }

            // Check that the source isn't from a member of the bot's group.
            if (iPlayer.AIData.IsAI &&
                BotOwner.BotsGroup.Contains(iPlayer.AIData.BotOwner))
            {
                return true;
            }

            // Checks if the player is not an active enemy and that they are a neutral party
            if (!BotOwner.BotsGroup.IsPlayerEnemy(iPlayer)
                && BotOwner.BotsGroup.Neutrals.ContainsKey(iPlayer))
            {
                return true;
            }

            // Check that the source isn't an ally
            if (BotOwner.BotsGroup.Allies.Contains(iPlayer))
            {
                return true;
            }

            if (iPlayer.IsAI &&
                iPlayer.AIData?.BotOwner?.Memory.GoalEnemy?.ProfileId == Bot.ProfileId)
            {
                return false;
            }

            if (!BotOwner.BotsGroup.Enemies.ContainsKey(iPlayer))
            {
                return true;
            }
            return false;
        }

        private void removeEnemyInfo(Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (enemy.EnemyIPlayer != null &&
                BotOwner.EnemiesController.EnemyInfos.ContainsKey(enemy.EnemyIPlayer))
            {
                BotOwner.EnemiesController.Remove(enemy.EnemyIPlayer);
                return;
            }

            EnemyInfo badInfo = null;
            foreach (var enemyInfo in BotOwner.EnemiesController.EnemyInfos.Values)
            {
                if (enemyInfo?.Person != null &&
                    enemyInfo.ProfileId == enemy.EnemyProfileId)
                {
                    badInfo = enemyInfo;
                    break;
                }
            }

            if (badInfo?.Person != null)
            {
                BotOwner.EnemiesController.Remove(badInfo.Person);
            }
        }

        private void compareEnemyLists()
        {
            if (Enemies.Count != BotOwner.BotsGroup.Enemies.Count)
            {
                addAllEnemies();
            }
        }

        private void addAllEnemies()
        {
            foreach (var person in BotOwner.BotsGroup.Enemies.Keys)
            {
                tryAddEnemy(person);
            }
        }


        private string getBotInfo(Player player)
        {
            return $" [{player.Profile.Nickname}, {player.Profile.Info.Settings.Role}, {player.ProfileId}] ";
        }

        private string getBotInfo(IPlayer player)
        {
            return $" [{player.Profile.Nickname}, {player.Profile.Info.Settings.Role}, {player.ProfileId}] ";
        }

        private string findSourceDebug(string debugString)
        {
            StackTrace stackTrace = new StackTrace();
            debugString += $" StackTrace: [{stackTrace.ToString()}]";
            return debugString;
        }
    }
}