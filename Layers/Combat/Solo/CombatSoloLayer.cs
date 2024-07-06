using EFT;
using SAIN.Layers.Combat.Solo.Cover;
using SAIN.SAINComponent;

namespace SAIN.Layers.Combat.Solo
{
    internal class CombatSoloLayer : SAINLayer
    {
        public static readonly string Name = BuildLayerName("Combat Layer");

        public CombatSoloLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.Combat)
        {
        }

        public override Action GetNextAction()
        {
            _lastSelfDecision = _currentSelfDecision;
            _lastDecision = _currentDecision;

            if (_doSurgeryAction)
            {
                _doSurgeryAction = false;
                return new Action(typeof(DoSurgeryAction), $"Surgery");
            }

            switch (_lastDecision)
            {
                case CombatDecision.MoveToEngage:
                    return new Action(typeof(MoveToEngageAction), $"{_lastDecision}");

                case CombatDecision.MeleeAttack:
                    return new Action(typeof(MeleeAttackAction), $"{_lastDecision}");

                case CombatDecision.RushEnemy:
                    return new Action(typeof(RushEnemyAction), $"{_lastDecision}");

                case CombatDecision.ThrowGrenade:
                    return new Action(typeof(ThrowGrenadeAction), $"{_lastDecision}");

                case CombatDecision.ShiftCover:
                    return new Action(typeof(ShiftCoverAction), $"{_lastDecision}");

                case CombatDecision.RunToCover:
                    return new Action(typeof(RunToCoverAction), $"{_lastDecision}");

                case CombatDecision.Retreat:
                    return new Action(typeof(RunToCoverAction), $"{_lastDecision} + {_lastSelfDecision}");

                case CombatDecision.MoveToCover:
                    return new Action(typeof(WalkToCoverAction), $"{_lastDecision}");

                case CombatDecision.DogFight:
                    return new Action(typeof(DogFightAction), $"{_lastDecision}");

                case CombatDecision.ShootDistantEnemy:
                case CombatDecision.StandAndShoot:
                    return new Action(typeof(StandAndShootAction), $"{_lastDecision}");

                case CombatDecision.HoldInCover:
                    string label;
                    if (_lastSelfDecision != SelfDecision.None)
                        label = $"{_lastDecision} + {_lastSelfDecision}";
                    else
                        label = $"{_lastDecision}";
                    return new Action(typeof(HoldinCoverAction), label);

                case CombatDecision.Search:
                    return new Action(typeof(SearchAction), $"{_lastDecision}");

                case CombatDecision.Freeze:
                    return new Action(typeof(FreezeAction), $"{_lastDecision}");

                default:
                    return new Action(typeof(StandAndShootAction), $"DEFAULT! {_lastDecision}");
            }
        }

        public override bool IsActive()
        {
            if (Bot == null)
            {
                return false;
            }
            bool active = _currentDecision != CombatDecision.None;
            setLayer(active);
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            // this is dumb im sorry
            if (!_doSurgeryAction
                && _currentSelfDecision == SelfDecision.Surgery
                && Bot.Cover.BotIsAtCoverInUse())
            {
                _doSurgeryAction = true;
                return true;
            }

            if (_lastSelfDecision == SelfDecision.Surgery && 
                _currentSelfDecision != SelfDecision.Surgery )
            {
                return true;
            }

            return _currentDecision != _lastDecision;
        }

        private bool _doSurgeryAction;

        private CombatDecision _lastDecision = CombatDecision.None;
        private SelfDecision _lastSelfDecision = SelfDecision.None;
        public CombatDecision _currentDecision => Bot.Decision.CurrentSoloDecision;
        public SelfDecision _currentSelfDecision => Bot.Decision.CurrentSelfDecision;
    }
}