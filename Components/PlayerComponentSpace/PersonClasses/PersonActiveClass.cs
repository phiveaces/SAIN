using EFT;
using System;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonActiveClass
    {
        public void CheckActive()
        {
            if (IsAlive)
                IsAlive = checkAlive();

            if (!IsAlive)
                playerKilledOrNull();

            bool wasGameObjectActive = GameObjectActive;
            GameObjectActive = IsAlive && checkGameObjectActive();
            if (wasGameObjectActive != GameObjectActive)
            {
                OnGameObjectActiveChanged?.Invoke(GameObjectActive);
                //Logger.LogDebug($"GameObject {_person.Nickname} Active [{GameObjectActive}]");
            }

            bool wasActive = PlayerActive;
            PlayerActive = IsAlive && GameObjectActive && checkPlayerExists();
            if (wasActive != PlayerActive)
            {
                OnPlayerActiveChanged?.Invoke(PlayerActive);
                //Logger.LogDebug($"Player {_person.Nickname} Active [{PlayerActive}]");
            }

            bool wasAIActive = BotActive;
            BotActive = PlayerActive && checkBotActive();
            if (wasAIActive != BotActive)
            {
                OnBotActiveChanged?.Invoke(BotActive);
                //Logger.LogDebug($"Bot {_person.Nickname} Active [{BotActive}]");
            }
        }

        public void Disable()
        {
            bool wasGameObjectActive = GameObjectActive;
            GameObjectActive = false;
            if (wasGameObjectActive != GameObjectActive)
            {
                OnGameObjectActiveChanged?.Invoke(GameObjectActive);
            }

            bool wasActive = PlayerActive;
            PlayerActive = false;
            if (wasActive != PlayerActive)
            {
                OnPlayerActiveChanged?.Invoke(PlayerActive);
            }

            bool wasAIActive = BotActive;
            BotActive = false;
            if (wasAIActive != BotActive)
            {
                OnBotActiveChanged?.Invoke(BotActive);
            }
        }

        private void botStateChanged(EBotState state)
        {
            if (state == EBotState.Disposed)
            {
                if (_person.BotOwner != null)
                    _person.BotOwner.OnBotStateChange -= botStateChanged;

                IsAlive = false;
                playerKilledOrNull();
            }
        }

        public PersonActiveClass(PersonClass person)
        {
            _person = person;
            person.Player.OnPlayerDeadOrUnspawn += playerDeadOrUnspawn;
            IsAlive = true;
        }

        private readonly PersonClass _person;

        public event Action<bool> OnGameObjectActiveChanged;

        public event Action<bool> OnPlayerActiveChanged;

        public event Action<bool> OnBotActiveChanged;

        public event Action<PersonClass> OnPersonDeadOrDespawned;

        public bool Active => PlayerActive && (!_person.IsAI || BotActive);
        public bool PlayerActive { get; private set; }
        public bool BotActive { get; private set; }
        public bool GameObjectActive { get; private set; }
        public bool IsAlive { get; private set; } = true;

        private bool checkAlive()
        {
            IPlayer iPlayer = _person.IPlayer;
            if (iPlayer == null)
            {
                return false;
            }
            if (iPlayer.HealthController?.IsAlive == false)
            {
                return false;
            }

            if (_person.IsAI)
            {
                BotOwner botOwner = _person.BotOwner;
                if (botOwner == null ||
                    botOwner.gameObject == null ||
                    botOwner.Transform?.Original == null)
                {
                    return false;
                }
            }
            return true;
        }

        private bool checkGameObjectActive()
        {
            GameObject gameObject = _person.GameObject;
            if (gameObject == null)
            {
                return false;
            }
            if (!_person.Player.isActiveAndEnabled)
            {
                return false;
            }
            return gameObject.activeInHierarchy;
        }

        private void playerKilledOrNull()
        {
            if (!_playerNullOrDead)
            {
                //Logger.LogDebug($"Person {_person.Nickname} Dead");
                _playerNullOrDead = true;

                var player = _person.Player;
                if (player != null)
                {
                    player.OnPlayerDeadOrUnspawn -= playerDeadOrUnspawn;
                }

                OnPersonDeadOrDespawned?.Invoke(_person);
            }
        }

        private void playerDeadOrUnspawn(Player player)
        {
            IsAlive = false;
            playerKilledOrNull();
        }

        private bool checkBotActive()
        {
            if (!IsAlive)
            {
                return false;
            }
            if (_person.IsAI)
            {
                BotOwner botOwner = _person.BotOwner;
                if (botOwner == null)
                {
                    return false;
                }
                if (botOwner.BotState != EBotState.Active)
                {
                    return false;
                }
                if (botOwner.StandBy?.StandByType != BotStandByType.active)
                {
                    //return false;
                }
            }
            return true;
        }

        private bool checkPlayerExists()
        {
            Player player = _person.Player;
            return player != null && player.gameObject != null && player.Transform?.Original != null;
        }

        public void InitBotOwner(BotOwner botOwner)
        {
            botOwner.OnBotStateChange += botStateChanged;
        }

        private bool _playerNullOrDead;
    }
}