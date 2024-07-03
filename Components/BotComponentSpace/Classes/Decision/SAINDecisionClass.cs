using EFT;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class SAINDecisionClass : BotBase, IBotClass
    {
        public event Action<SoloDecision, SquadDecision, SelfDecision, float> OnDecisionMade;

        public event Action<bool> OnSAINStatusChanged;

        public bool HasDecision { get; private set; }

        public SoloDecision CurrentSoloDecision { get; private set; }
        public SoloDecision PreviousSoloDecision { get; private set; }

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
                    case SoloDecision.Retreat:
                    case SoloDecision.RunAway:
                    case SoloDecision.RunToCover:
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
            CurrentSoloDecision == SoloDecision.Search || 
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
            Bot.BotActivation.OnBotStateChanged += resetDecisions;
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
        const float DECISION_FREQUENCY_FPS = 10;
        const float DECISION_FREQUENCY_PEACE_FPS = 5;

        public void Dispose()
        {
            Bot.BotActivation.OnBotStateChanged -= resetDecisions;
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
                SetDecisions(SoloDecision.DogFight, SquadDecision.None, SelfDecision.None);
                return;
            }

            if (BotOwner.WeaponManager.IsMelee)
            {
                SetDecisions(SoloDecision.MeleeAttack, SquadDecision.None, SelfDecision.None);
                return;
            }

            if (SelfActionDecisions.GetDecision(out SelfDecision selfDecision))
            {
                SetDecisions(SoloDecision.Retreat, SquadDecision.None, selfDecision);
                return;
            }

            if (CheckContinueRetreat())
            {
                return;
            }

            if (SquadDecisions.GetDecision(out SquadDecision squadDecision))
            {
                SetDecisions(SoloDecision.None, squadDecision, SelfDecision.None);
                return;
            }

            if (EnemyDecisions.GetDecision(out SoloDecision soloDecision))
            {
                SetDecisions(soloDecision, SquadDecision.None, SelfDecision.None);
                return;
            }

            SetDecisions(SoloDecision.None, SquadDecision.None, SelfDecision.None);
        }

        private void SetDecisions(SoloDecision solo, SquadDecision squad, SelfDecision self)
        {
            if (SAINPlugin.DebugMode)
            {
                if (SAINPlugin.ForceSoloDecision != SoloDecision.None)
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

            if (updateLastDecisions(solo, squad, self))
            {
                HasDecision =
                    CurrentSoloDecision != SoloDecision.None ||
                    CurrentSelfDecision != SelfDecision.None ||
                    CurrentSquadDecision != SquadDecision.None;

                _hasLastDecision =
                    PreviousSoloDecision != SoloDecision.None ||
                    PreviousSelfDecision != SelfDecision.None ||
                    PreviousSquadDecision != SquadDecision.None;

                TotalDecisionsMade++;
                DecisionsMadeThisFight++;
                ChangeDecisionTime = Time.time;
                OnDecisionMade?.Invoke(solo, squad, self, Time.time);
                checkSAINStartAndEnd(); 
            }
        }

        private void checkSAINStartAndEnd()
        {
            bool hasDecision = HasDecision;
            bool hasLastDecision = _hasLastDecision;

            // If previously all decisions were none, sain has now started.
            if (!hasLastDecision && 
                hasDecision)
            {
                OnSAINStatusChanged?.Invoke(true);
                //Logger.LogDebug($"{BotOwner.name} Has Decision. SAIN Awake.");
            }
            if (hasLastDecision && 
                !hasDecision)
            {
                OnSAINStatusChanged?.Invoke(false);
                //Logger.LogDebug($"{BotOwner.name} Has no Decision. SAIN Sleep.");
                DecisionsMadeThisFight = 0;
            }
        }

        private bool updateLastDecisions(SoloDecision newSoloDecision, SquadDecision newSquadDecision, SelfDecision newSelfDecision)
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
            if (!value && HasDecision)
            {
                SetDecisions(SoloDecision.None, SquadDecision.None, SelfDecision.None);
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
            if (CurrentSoloDecision == SoloDecision.AvoidGrenade)
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
            bool runningToCover = CurrentSoloDecision == SoloDecision.Retreat || CurrentSoloDecision == SoloDecision.RunToCover;
            if (!runningToCover)
            {
                return false;
            }

            float timeChangeDec = Bot.Decision.TimeSinceChangeDecision;
            if (timeChangeDec < 1)
            {
                return true;
            }

            if (timeChangeDec > 5 && 
                !Bot.BotStuck.BotHasChangedPosition)
            {
                return false;
            }

            //if (Running && !Bot.BotStuck.BotHasChangedPosition && Bot.BotStuck.TimeSpentNotMoving > 1f && timeChangeDec > 2f)
            //{
            //    return false;
            //}

            CoverPoint coverInUse = Bot.Cover.CoverInUse;
            if (coverInUse == null)
            {
                return false;
            }

            switch (coverInUse.Status)
            {
                case CoverStatus.InCover:
                    return false;

                case CoverStatus.CloseToCover:
                    return Bot.Mover.SprintController.Running || BotOwner.Mover.IsMoving;

                default:
                    return !coverInUse.IsBad;
            }
        }

        private float _nextCalcPathTime;
        private float _calcPathFreq = 0.5f;
        private float _grenadePathDist;

        private bool _hasLastDecision;

        public static readonly SoloDecision[] RETREAT_DECISIONS =
        { 
            SoloDecision.Retreat 
        };
    }
}