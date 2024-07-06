using EFT;
using SAIN.SAINComponent;

namespace SAIN.BotController.Classes
{
    public class MemberInfo
    {
        private readonly Squad _squad;
        public MemberInfo(BotComponent sain, Squad squad)
        {
            _squad = squad;
            Bot = sain;
            Player = sain.Player;
            ProfileId = sain.ProfileId;
            Nickname = sain.Player?.Profile?.Nickname;

            HealthStatus = sain.Memory.Health.HealthStatus;

            sain.Decision.OnDecisionMade += UpdateDecisions;
            sain.Memory.Health.HealthStatusChanged += UpdateHealth;
            sain.OnDispose += removeMe;

            UpdatePowerLevel();
        }

        private void removeMe()
        {
            _squad?.RemoveMember(ProfileId);
        }

        private void UpdateDecisions(CombatDecision solo, SquadDecision squad, SelfDecision self, BotComponent member)
        {
            SoloDecision = solo;
            SquadDecision = squad;
            SelfDecision = self;

            // Update power level here just to see if equipment changed.
            UpdatePowerLevel();
        }

        public void UpdatePowerLevel()
        {
            var aiData = Bot?.Player?.AIData;
            if (aiData != null)
            {
                PowerLevel = aiData.PowerOfEquipment;
            }
        }

        private void UpdateHealth(ETagStatus healthStatus)
        {
            HealthStatus = healthStatus;
        }

        public readonly BotComponent Bot;
        public readonly Player Player;
        public readonly string ProfileId;
        public readonly string Nickname;

        public bool HasEnemy => Bot?.HasEnemy == true;

        public ETagStatus HealthStatus;

        public CombatDecision SoloDecision { get; private set; }
        public SquadDecision SquadDecision { get; private set; }
        public SelfDecision SelfDecision { get; private set; }
        public float PowerLevel { get; private set; }

        public void Dispose()
        {
            if (Bot != null)
            {
                Bot.OnDispose -= removeMe;
                Bot.Decision.OnDecisionMade -= UpdateDecisions;
                Bot.Memory.Health.HealthStatusChanged -= UpdateHealth;
            }
        }
    }
}
