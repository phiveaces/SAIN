using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;

namespace SAIN.SAINComponent.Classes
{
    public class SAINActivationClass : BotBase, IBotClass
    {
        public event Action<bool> OnBotStateChanged;

        public bool BotActive { get; private set; }

        public bool GameEnding { get; private set; } = false;

        public bool SAINLayersActive => ActiveLayer != ESAINLayer.None;

        public ESAINLayer ActiveLayer { get; private set; }

        public void SetActive(bool value)
        {
            if (BotActive == value) return;

            switch (value)
            {
                case true:
                    //Logger.LogDebug($"Trying to set Bot Active... [{Bot.Info.Profile.Name}]");
                    if (Bot.CoroutineManager.StartCoroutines())
                    {
                        BotActive = true;
                        //Logger.LogDebug($"Bot Active [{Bot.Info.Profile.Name}]");
                    }
                    break;

                case false:
                    BotActive = false;
                    ActiveLayer = ESAINLayer.None;
                    Bot.CoroutineManager.StopCoroutines();
                    break;
            }

            OnBotStateChanged?.Invoke(value);
        }

        public void SetActiveLayer(ESAINLayer layer)
        {
            ActiveLayer = layer;
        }

        public void Update()
        {
            checkActive();

            if (!BotActive && 
                Bot.Person.ActiveClass.BotActive)
            {
                //Logger.LogWarning($"Bot Component not active but should be!");
                SetActive(true);
            }

            if (BotActive && 
                BotOwner.StandBy.StandByType != BotStandByType.active && 
                Bot.CurrentTarget.HasTarget)
            {
                //Logger.LogWarning($"Had to activate bot manually because they were in stand by.");
                BotOwner.StandBy.Activate();
            }
        }

        public void LateUpdate()
        {
            checkSpeedReset(); 
            checkActive();
        }

        private void checkActive()
        {
            checkBotGame();

            if (GameEnding && BotActive)
            {
                SetActive(false);
            }
        }

        private void checkSpeedReset()
        {
            if (SAINLayersActive)
            {
                if (_speedReset)
                    _speedReset = false;

                return;
            }

            if (!_speedReset)
            {
                _speedReset = true;
                BotOwner.SetTargetMoveSpeed(1f);
                BotOwner.Mover.SetPose(1f);
                Bot.Mover.SetTargetMoveSpeed(1f);
                Bot.Mover.SetTargetPose(1f);
            }
        }

        public SAINActivationClass(BotComponent botComponent) : base(botComponent)
        {
        }

        public void Init()
        {
            SetActive(true);
            Bot.Person.ActiveClass.OnBotActiveChanged += SetActive;
        }

        private void checkBotGame()
        {
            var botGame = Singleton<IBotGame>.Instance;
            GameEnding = botGame == null || botGame.Status == GameStatus.Stopping;
        }

        public void Dispose()
        {
        }

        private bool _speedReset;
    }
}