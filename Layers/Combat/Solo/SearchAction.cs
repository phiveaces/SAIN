using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Search;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Solo
{
    internal class SearchAction : SAINAction, ISAINAction
    {
        public SearchAction(BotOwner bot) : base(bot, "Search")
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override IEnumerator ActionCoroutine()
        {
            while (true)
            {
                if (_searchTarget == null)
                {
                    var enemy = Bot.Enemy;
                    if (enemy == null)
                    {
                        yield return null;
                        continue;
                    }
                    _searchTarget = enemy;
                    Search.ToggleSearch(true, enemy);
                }

                bool isBeingStealthy = _searchTarget.Hearing.EnemyHeardFromPeace;
                if (isBeingStealthy)
                {
                    _sprintEnabled = false;
                }
                else
                {
                    CheckShouldSprint();
                    talk();
                }

                if (_nextUpdateSearchTime < Time.time)
                {
                    _nextUpdateSearchTime = Time.time + 0.1f;
                    Search.Search(_sprintEnabled);
                }

                Steer();

                if (!_sprintEnabled)
                {
                    Shoot.Update();
                    if (!isBeingStealthy)
                        checkWeapon();
                }

                yield return null;
            }
        }

        private Enemy _searchTarget;

        public override void Start()
        {
            _searchTarget = Bot.Enemy;
            Search.ToggleSearch(true, _searchTarget);
            Toggle(true);
        }

        public override void Stop()
        {
            Search.ToggleSearch(false, _searchTarget);
            _searchTarget = null;
            Toggle(false);
            BotOwner.Mover.MovementResume();
            HaveTalked = false;
        }

        private float _nextCheckTime;

        public override void Update()
        {
        }

        private float _nextUpdateSearchTime;

        private void talk()
        {
            if (Search.FinalDestination == null) 
            {
                return; 
            }

            // Scavs will speak out and be more vocal
            if (!HaveTalked &&
                Bot.Info.Profile.IsScav &&
                (BotOwner.Position - Search.FinalDestination.Value).sqrMagnitude < 50f * 50f)
            {
                HaveTalked = true;
                if (EFTMath.RandomBool(40))
                {
                    Bot.Talk.Say(EPhraseTrigger.OnMutter, ETagStatus.Aware, true);
                }
            }
        }

        private void checkWeapon()
        {
            if (_nextCheckTime < Time.time)
            {
                _nextCheckTime = Time.time + 180f * Random.Range(0.5f, 1.5f);

                if (Bot.Enemy != null && Bot.Enemy.TimeSinceLastKnownUpdated > 20f)
                {
                    if (EFTMath.RandomBool())
                        Bot.Player.HandsController.FirearmsAnimator.CheckAmmo();
                    else
                        Bot.Player.HandsController.FirearmsAnimator.CheckChamber();
                }
            }
        }

        private bool HaveTalked = false;

        private SAINSearchClass Search => Bot.Search;

        private void CheckShouldSprint()
        {
            if (Search.CurrentState == ESearchMove.MoveToEndPeek || Search.CurrentState == ESearchMove.Wait || Search.CurrentState == ESearchMove.MoveToDangerPoint)
            {
                _sprintEnabled = false;
                return;
            }

            if (Bot.Enemy?.IsVisible == true || Bot.Enemy?.InLineOfSight == true)
            {
                _sprintEnabled = false;
                return;
            }

            if (Bot.Decision.CurrentSquadDecision == SquadDecision.Help)
            {
                _sprintEnabled = true;
                return;
            }

            var persSettings = Bot.Info.PersonalitySettings;
            float chance = persSettings.Search.SprintWhileSearchChance;
            if (RandomSprintTimer < Time.time && chance > 0)
            {
                float myPower = Bot.Info.Profile.PowerLevel;
                if (Bot.Enemy?.EnemyPlayer != null && Bot.Enemy.EnemyPlayer.AIData.PowerOfEquipment < myPower * 0.5f)
                {
                    chance = 100f;
                }

                _sprintEnabled = EFTMath.RandomBool(chance);
                float timeAdd;
                if (_sprintEnabled)
                {
                    timeAdd = 2f * Random.Range(0.75f, 4.00f);
                }
                else
                {
                    timeAdd = 4f * Random.Range(0.5f, 1.5f);
                }
                RandomSprintTimer = Time.time + timeAdd;
            }
        }

        private bool _sprintEnabled = false;
        private float RandomSprintTimer = 0f;

        private void Steer()
        {
            if (!SteerByPriority(false)
                && !Bot.Steering.LookToLastKnownEnemyPosition(Bot.Enemy))
            {
                LookToMovingDirection();
            }
        }

        private bool CanSeeDangerOrCorner(out Vector3 point)
        {
            point = Vector3.zero;

            if (Search.PeekPoints == null || Search.CurrentState == ESearchMove.MoveToDangerPoint)
            {
                LookPoint = Vector3.zero;
                return false;
            }

            if (CheckSeeTimer < Time.time)
            {
                LookPoint = Vector3.zero;
                CheckSeeTimer = Time.time + 0.5f;
                var headPosition = Bot.Transform.HeadPosition;

                var canSeePoint = !Vector.Raycast(headPosition,
                    Search.PeekPoints.Value.DangerPoint,
                    LayerMaskClass.HighPolyWithTerrainMaskAI);

                if (canSeePoint)
                {
                    LookPoint = Search.PeekPoints.Value.DangerPoint + Vector3.up;
                }
                else
                {
                    canSeePoint = !Vector.Raycast(headPosition,
                        Search.PeekPoints.Value.Corner,
                        LayerMaskClass.HighPolyWithTerrainMaskAI);
                    if (canSeePoint)
                    {
                        LookPoint = Search.PeekPoints.Value.Corner + Vector3.up;
                    }
                }

                if (LookPoint != Vector3.zero)
                {
                    //LookPoint.y = 0;
                    //LookPoint += headPosition;
                }
            }

            point = LookPoint;
            return point != Vector3.zero;
        }

        private Vector3 LookPoint;
        private float CheckSeeTimer;

        private bool SteerByPriority(bool value) => Bot.Steering.SteerByPriority(value);

        private void LookToMovingDirection() => Bot.Steering.LookToMovingDirection();

        public NavMeshPath Path = new NavMeshPath();
    }
}