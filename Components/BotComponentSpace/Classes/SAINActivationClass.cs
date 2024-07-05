using Comfort.Common;
using EFT;
using SAIN.Helpers.Events;

namespace SAIN.SAINComponent.Classes
{
    public class SAINActivationClass : BotBase, IBotClass
    {
        public ESAINLayer ActiveLayer { get; private set; }

        public bool BotActive => BotActiveToggle.Value;
        public ToggleEvent BotActiveToggle { get; } = new ToggleEvent();
        public bool BotInStandBy => BotStandByToggle.Value;
        public ToggleEvent BotStandByToggle { get; } = new ToggleEvent();
        public bool GameEnding => GameEndingToggle.Value;
        public ToggleEvent GameEndingToggle { get; } = new ToggleEvent();
        public bool SAINLayersActive => SAINLayersActiveToggle.Value;
        public ToggleEvent SAINLayersActiveToggle { get; } = new ToggleEvent();

        public void SetActive(bool botActive)
        {
            BotActiveToggle.CheckToggle(botActive);
            setCoroutines(botActive);
            if (!botActive)
            {
                BotStandByToggle.CheckToggle(true);
                ActiveLayer = ESAINLayer.None;
                SAINLayersActiveToggle.CheckToggle(false);
            }
        }

        private void setCoroutines(bool value)
        {
            bool started = Bot.CoroutineManager.CoroutinesStarted;
            if (value && !started)
            {
                Bot.CoroutineManager.StartCoroutines();
            }
            else if (!value && started)
            {
                Bot.CoroutineManager.StopCoroutines();
            }
        }

        public void SetActiveLayer(ESAINLayer layer)
        {
            ActiveLayer = layer;
        }

        public void Update()
        {
            checkActive();
            //checkSpeedReset();
        }

        public void LateUpdate()
        {
            checkActive();
        }

        private void checkActive()
        {
            checkGameEnding(); 
            checkBotActive();
            checkStandBy();
            checkLayersActive();
        }

        private void checkLayersActive()
        {
            SAINLayersActiveToggle.CheckToggle(ActiveLayer != ESAINLayer.None);
        }

        private void checkBotActive()
        {
            if (GameEnding && BotActive)
                SetActive(false);

            if (!GameEnding &&
                !BotActive &&
                Bot.Person.ActiveClass.BotActive)
            {
                Logger.LogWarning($"Bot not active but should be!");
                SetActive(true);
            }
        }

        private void checkStandBy()
        {
            bool standby = _botInStandby;
            if (BotActive &&
                standby &&
                Bot.HasEnemy)
            {
                //Logger.LogWarning($"Had to activate bot manually because they were in stand by.");
                BotOwner.StandBy.Activate();
                standby = false;
            }

            BotStandByToggle.CheckToggle(standby);
        }

        private void checkGameEnding()
        {
            var botGame = Singleton<IBotGame>.Instance;
            bool gameEnding = botGame == null || botGame.Status == GameStatus.Stopping;
            GameEndingToggle.CheckToggle(gameEnding);
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

        public void Dispose()
        {
        }

        private bool _botInStandby => BotOwner.StandBy.StandByType != BotStandByType.active;
        private bool _speedReset;
    }
}