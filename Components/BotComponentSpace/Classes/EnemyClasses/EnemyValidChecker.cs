using EFT;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses
{
    public class EnemyValidChecker : EnemyBase, IBotClass
    {
        public bool IsValid => checkValid();

        public EnemyValidChecker(Enemy enemy) : base(enemy)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        private bool checkValid()
        {
            if (!_isValid)
            {
                return false;
            }
            _isValid = isValid();
            if (!_isValid)
            {
                Enemy.Events.SetEnemyAsInvalid();
            }
            return _isValid;
        }

        private bool _isValid = true;

        private bool isValid()
        {
            var component = EnemyPlayerComponent;
            if (component == null)
            {
                Logger.LogError($"Enemy {Enemy.EnemyName} PlayerComponent is Null");
                return false;
            }
            var person = component.Person;
            if (person == null)
            {
                Logger.LogDebug("Enemy Person is Null");
                return false;
            }
            if (!person.ActiveClass.IsAlive)
            {
                Logger.LogDebug("Enemy Player Is Dead");
                return false;
            }
            // Checks specific to bots
            BotOwner botOwner = EnemyPlayer?.AIData?.BotOwner;
            if (EnemyPerson?.IsAI == true && botOwner == null)
            {
                Logger.LogDebug("Enemy is AI, but BotOwner is null. Removing...");
                return false;
            }
            if (botOwner != null && botOwner.ProfileId == BotOwner.ProfileId)
            {
                Logger.LogWarning("Enemy has same profile id as Bot? Removing...");
                return false;
            }
            return true;
        }
    }
}
