using EFT;
using SAIN.Layers.Combat.Solo;
using SAIN.SAINComponent;

namespace SAIN.Layers.Combat.Squad
{
    internal class CombatSquadLayer : SAINLayer
    {
        public static readonly string Name = BuildLayerName("Squad Layer");

        public CombatSquadLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.Squad)
        {
        }

        public override Action GetNextAction()
        {
            var Decision = SquadDecision;
            LastActionDecision = Decision;
            switch (Decision)
            {
                case SquadDecision.Regroup:
                    return new Action(typeof(RegroupAction), $"{Decision}");

                case SquadDecision.Suppress:
                    return new Action(typeof(SuppressAction), $"{Decision}");

                case SquadDecision.Search:
                    return new Action(typeof(SearchAction), $"{Decision}");

                case SquadDecision.GroupSearch:
                    if (Bot.Squad.IAmLeader)
                    {
                        return new Action(typeof(SearchAction), $"{Decision} : Lead Search Party");
                    }
                    return new Action(typeof(FollowSearchParty), $"{Decision} : Follow Squad Leader");

                case SquadDecision.Help:
                    return new Action(typeof(SearchAction), $"{Decision}");

                case SquadDecision.PushSuppressedEnemy:
                    return new Action(typeof(RushEnemyAction), $"{Decision}");

                default:
                    return new Action(typeof(RegroupAction), $"DEFAULT!");
            }
        }

        public override bool IsActive()
        {
            bool active =
                Bot?.BotActive == true &&
                SquadDecision != SquadDecision.None &&
                Bot.Decision.CurrentSelfDecision == SelfDecision.None;

            if (active && Bot.Cover.CoverInUse != null)
            {
                Bot.Cover.CoverInUse = null;
            }

            setLayer(active);

            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            return Bot?.BotActive == true && 
                SquadDecision != LastActionDecision;
        }

        private SquadDecision LastActionDecision = SquadDecision.None;
        public SquadDecision SquadDecision => Bot.Decision.CurrentSquadDecision;
    }
}