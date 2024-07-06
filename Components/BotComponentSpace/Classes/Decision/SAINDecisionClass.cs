using EFT;
using SAIN.Helpers.Events;
using SAIN.SAINComponent.Classes.Decision.Reasons;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SAINDecisionClass : BotBase, IBotClass
    {
        public event Action<CombatDecision, SquadDecision, SelfDecision, BotComponent> OnDecisionMade;

        public bool HasDecision => HasDecisionToggle.Value;
        public ToggleEvent HasDecisionToggle { get; } = new ToggleEvent();


        public CombatDecision CurrentSoloDecision { get; private set; }
        public CombatDecision PreviousSoloDecision { get; private set; }

        public SquadDecision CurrentSquadDecision { get; private set; }
        public SquadDecision PreviousSquadDecision { get; private set; }

        public SelfDecision CurrentSelfDecision { get; private set; }
        public SelfDecision PreviousSelfDecision { get; private set; }

        public float ChangeDecisionTime { get; private set; }
        public float TimeSinceChangeDecision => Time.time - ChangeDecisionTime;

        public int TotalDecisionsMade { get; private set; }
        public int DecisionsMadeThisFight { get; private set; }

        public bool RunningToCover
        {
            get
            {
                switch (CurrentSoloDecision)
                {
                    case CombatDecision.Retreat:
                    case CombatDecision.RunAway:
                    case CombatDecision.RunToCover:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public DogFightDecisionClass DogFightDecision { get; private set; }
        public SelfActionDecisionClass SelfActionDecisions { get; private set; }
        public EnemyDecisionClass EnemyDecisions { get; private set; }
        public SquadDecisionClass SquadDecisions { get; private set; }

        public bool IsSearching => 
            CurrentSoloDecision == CombatDecision.Search || 
            CurrentSquadDecision == SquadDecision.Search || 
            CurrentSquadDecision == SquadDecision.GroupSearch;

        public SAINDecisionClass(BotComponent bot) : base(bot)
        {
            SelfActionDecisions = new SelfActionDecisionClass(bot);
            EnemyDecisions = new EnemyDecisionClass(bot);
            SquadDecisions = new SquadDecisionClass(bot);
            DogFightDecision = new DogFightDecisionClass(bot);
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
            Bot.BotActivation.BotActiveToggle.OnToggle += resetDecisions;
            DogFightDecision.Init();
        }

        public void Update()
        {
            if (_nextGetDecisionTime < Time.time)
            {
                getDecision();
                float delay = HasDecision ? DECISION_FREQUENCY : DECISION_FREQUENCY_PEACE;
                _nextGetDecisionTime = Time.time + delay;
            }
            DogFightDecision.Update();
        }

        private float _nextGetDecisionTime;
        const float DECISION_FREQUENCY = 1f / DECISION_FREQUENCY_FPS;
        const float DECISION_FREQUENCY_PEACE = 1f / DECISION_FREQUENCY_PEACE_FPS;
        const float DECISION_FREQUENCY_FPS = 30;
        const float DECISION_FREQUENCY_PEACE_FPS = 10;

        public void Dispose()
        {
            Bot.BotActivation.BotActiveToggle.OnToggle -= resetDecisions;
            DogFightDecision?.Dispose();
        }

        private void getDecision()
        {
            //if (shallAvoidGrenade())
            //{
            //    SetDecisions(SoloDecision.AvoidGrenade, SquadDecision.None, SelfDecision.None);
            //    return;
            //}

            EnemyDecisions.DebugShallSearch = null;

            if (DogFightDecision.ShallDogFight())
            {
                SetDecisions(CombatDecision.DogFight, SquadDecision.None, SelfDecision.None);
                return;
            }

            if (BotOwner.WeaponManager.IsMelee)
            {
                SetDecisions(CombatDecision.MeleeAttack, SquadDecision.None, SelfDecision.None);
                return;
            }

            if (SelfActionDecisions.GetDecision(out SelfDecision selfDecision))
            {
                var combatDecision = Bot.Cover.InCover ? CombatDecision.HoldInCover : CombatDecision.Retreat;
                SetDecisions(combatDecision, SquadDecision.None, selfDecision);
                return;
            }

            if (CheckContinueRetreat())
            {
                SetDecisions(CombatDecision.Retreat, SquadDecision.None, SelfDecision.None);
                return;
            }

            if (SquadDecisions.GetDecision(out SquadDecision squadDecision))
            {
                SetDecisions(CombatDecision.None, squadDecision, SelfDecision.None);
                return;
            }

            if (EnemyDecisions.GetDecision(out CombatDecision soloDecision))
            {
                SetDecisions(soloDecision, SquadDecision.None, SelfDecision.None);
                return;
            }

            SetDecisions(CombatDecision.None, SquadDecision.None, SelfDecision.None);
        }


        private BotDecision<SelfDecision>? _self;
        private BotDecision<CombatDecision>? _combat;
        private BotDecision<SquadDecision>? _squad;

        private void setSelf(BotDecision<SelfDecision>? decision)
        {

        }

        private void setCombat(BotDecision<CombatDecision>? decision)
        {

        }

        private void setSquad(BotDecision<SquadDecision>? decision)
        {

        }

        private void getDecisionStructs()
        {
            EnemyDecisions.DebugShallSearch = null;

            if (Bot.EnemyController.EnemyLists.KnownEnemies.Count == 0)
            {
                setCombat(null);
                setSelf(null);
                setSquad(null);
                return;
            }

            if (DogFightDecision.ShallDogFight())
            {
                setSelf(null);
                setSquad(null);
                setCombat(new BotDecision<CombatDecision>(CombatDecision.DogFight, "dogFight"));
                return;
            }

            if (BotOwner.WeaponManager.IsMelee)
            {
                setSelf(null);
                setSquad(null);
                setCombat(new BotDecision<CombatDecision>(CombatDecision.MeleeAttack, "meleeAttack"));
                return;
            }

            if (SelfActionDecisions.GetDecision(out SelfDecision selfDecision))
            {
                setSquad(null);
                setCombat(new BotDecision<CombatDecision>(CombatDecision.Retreat, "selfCare"));
                setSelf(new BotDecision<SelfDecision>(selfDecision, "selfCare"));
                return;
            }

            if (CheckContinueRetreat())
            {
                return;
            }

            if (SquadDecisions.GetDecision(out SquadDecision squadDecision))
            {
                setSelf(null);
                setCombat(null);
                setSquad(new BotDecision<SquadDecision>(squadDecision, "squadAction"));
                return;
            }

            setSelf(null);
            setSquad(null);

            if (!Bot.HasEnemy)
            {
                setCombat(null);
                return;
            }
            var combat = EnemyDecisions.GetDecision();
            setCombat(combat);
        }

        private void SetDecisions(CombatDecision solo, SquadDecision squad, SelfDecision self)
        {
            if (SAINPlugin.DebugMode)
            {
                if (SAINPlugin.ForceSoloDecision != CombatDecision.None)
                {
                    solo = SAINPlugin.ForceSoloDecision;
                }
                if (SAINPlugin.ForceSquadDecision != SquadDecision.None)
                {
                    squad = SAINPlugin.ForceSquadDecision;
                }
                if (SAINPlugin.ForceSelfDecision != SelfDecision.None)
                {
                    self = SAINPlugin.ForceSelfDecision;
                }
            }

            if (checkForNewDecision(solo, squad, self))
            {
                bool hasDecision =
                    solo != CombatDecision.None ||
                    self != SelfDecision.None ||
                    squad != SquadDecision.None;

                HasDecisionToggle.CheckToggle(hasDecision);

                TotalDecisionsMade++;
                ChangeDecisionTime = Time.time;
                OnDecisionMade?.Invoke(solo, squad, self, Bot);
            }
        }

        private bool checkForNewDecision(CombatDecision newSoloDecision, SquadDecision newSquadDecision, SelfDecision newSelfDecision)
        {
            bool newDecision = false;

            if (newSoloDecision != CurrentSoloDecision)
            {
                PreviousSoloDecision = CurrentSoloDecision;
                CurrentSoloDecision = newSoloDecision;
                newDecision = true;
            }

            if (newSquadDecision != CurrentSquadDecision)
            {
                PreviousSquadDecision = CurrentSquadDecision;
                CurrentSquadDecision = newSquadDecision;
                newDecision = true;
            }

            if (newSelfDecision != CurrentSelfDecision)
            {
                PreviousSelfDecision = CurrentSelfDecision;
                CurrentSelfDecision = newSelfDecision;
                newDecision = true;
            }

            return newDecision;
        }

        public void ResetDecisions(bool active)
        {
            bool hasDecision = HasDecision;
            resetDecisions(false);
            if (active && hasDecision)
            {
                BotOwner.CalcGoal();
            }
        }

        private void resetDecisions(bool value)
        {
            if (!value)
            {
                SetDecisions(CombatDecision.None, SquadDecision.None, SelfDecision.None);
            }
        }

        private bool shallAvoidGrenade()
        {
            Vector3? grenadePos = Bot.Grenade.GrenadeDangerPoint;
            if (grenadePos != null)
            {
                float pathDist = calcPathDist(grenadePos.Value);
                return checkDistances(pathDist, (grenadePos.Value - Bot.Position).magnitude);
            }
            return false;
        }

        private bool checkDistances(float pathDist, float straightDist)
        {
            if (pathDist > straightDist * 1.5f)
            {
                return false;
            }
            if (CurrentSoloDecision == CombatDecision.AvoidGrenade)
            {
                if (straightDist < 5f)
                {
                    //return true;
                }
                if (pathDist < 15f)
                {
                    return true;
                }
            }
            if (straightDist < 3f)
            {
                //return true;
            }
            if (pathDist < 10f)
            {
                return true;
            }
            return false;
        }

        private float calcPathDist(Vector3 point)
        {
            if (_nextCalcPathTime < Time.time)
            {
                _nextCalcPathTime = Time.time + _calcPathFreq;
                _grenadePathDist = calcPathAndReturnDist(point);
            }
            return _grenadePathDist;
        }

        private float calcPathAndReturnDist(Vector3 point)
        {
            Vector3 botPosition = Bot.Position;
            if (NavMesh.SamplePosition(point, out var hit, 5f, -1))
            {
                if (NavMesh.SamplePosition(botPosition, out var botHit, 0.5f, -1))
                {
                    botPosition = botHit.position;
                }
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(botPosition, hit.position, -1, path))
                {
                    return path.CalculatePathLength();
                }
            }
            return (botPosition - point).magnitude;
        }

        private bool CheckContinueRetreat()
        {
            bool runningToCover = CurrentSoloDecision == CombatDecision.Retreat || CurrentSoloDecision == CombatDecision.RunToCover;
            if (!runningToCover)
            {
                return false;
            }
            if (Bot.Cover.InCover)
            {
                return false;
            }

            float timeChangeDec = Bot.Decision.TimeSinceChangeDecision;
            if (timeChangeDec < 0.5f)
            {
                return true;
            }

            if (timeChangeDec > 3 && 
                !Bot.BotStuck.BotHasChangedPosition)
            {
                return false;
            }

            CoverPoint coverInUse = Bot.Cover.CoverInUse;
            if (coverInUse == null)
            {
                return false;
            }

            switch (coverInUse.PathDistanceStatus)
            {
                case CoverStatus.InCover:
                    return false;

                case CoverStatus.CloseToCover:
                    return Bot.Mover.SprintController.Running;

                default:
                    return Bot.Mover.SprintController.Running && !coverInUse.IsBad;
            }
        }

        private float _nextCalcPathTime;
        private float _calcPathFreq = 0.5f;
        private float _grenadePathDist;

        public static readonly CombatDecision[] RETREAT_DECISIONS =
        { 
            CombatDecision.Retreat,
            CombatDecision.RunToCover,
        };
    }
}