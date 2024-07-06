using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.Decision.Reasons;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Search;
using System;
using System.Text;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision.Reasons
{
    public struct BotDecision<T> where T : Enum
    {
        public BotDecision(T decision, string reason)
        {
            Decision = decision;
            Type = typeof(T);
            Reason = reason;
            TimeDecisionMade = Time.time;
        }

        public T Decision { get; }
        public Type Type { get; }
        public string Reason { get; }
        public float TimeDecisionMade { get; }
        public float TimeSinceDecisionMade => TimeDecisionMade - Time.time;
    }
}

namespace SAIN.SAINComponent.Classes.Decision
{
    public class EnemyDecisionClass : BotBase, IBotClass
    {
        public EnemyDecisionClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public StringBuilder FailedDecisionReasons { get; } = new StringBuilder();

        public BotDecision<CombatDecision>? GetDecision()
        {
            FailedDecisionReasons.Clear();
            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                return null;
            }
            string reason = string.Empty;
            if (shallDogFight(enemy, out reason))
            {
                return new BotDecision<CombatDecision>(CombatDecision.DogFight, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.DogFight} {reason}");

            if (shallStandAndShoot(enemy, out reason))
            {
                if (Bot.Decision.CurrentSoloDecision != CombatDecision.StandAndShoot)
                    Bot.Info.CalcHoldGroundDelay();

                return new BotDecision<CombatDecision>(CombatDecision.StandAndShoot, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.StandAndShoot} {reason}");

            if (shallShootDistantEnemy(enemy, out reason))
            {
                return new BotDecision<CombatDecision>(CombatDecision.ShootDistantEnemy, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.ShootDistantEnemy} {reason}");

            if (shallRushEnemy(enemy, out reason))
            {
                return new BotDecision<CombatDecision>(CombatDecision.RushEnemy, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.RushEnemy} {reason}");

            if (shallThrowGrenade(enemy, out reason))
            {
                return new BotDecision<CombatDecision>(CombatDecision.ThrowGrenade, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.ThrowGrenade} {reason}");

            if (shallSearch(enemy, out reason))
            {
                if (Bot.Decision.CurrentSoloDecision != CombatDecision.Search)
                {
                    enemy.Status.NumberOfSearchesStarted++;
                }
                return new BotDecision<CombatDecision>(CombatDecision.Search, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.Search} {reason}");

            if (shallFreezeAndWait(enemy, out reason))
            {
                return new BotDecision<CombatDecision>(CombatDecision.Freeze, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.Freeze} {reason}");

            if (shallShiftCover(enemy, out reason))
            {
                return new BotDecision<CombatDecision>(CombatDecision.ShiftCover, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.ShiftCover} {reason}");

            if (shallMoveToCover(out reason))
            {
                if (shallRunForCover(enemy, out reason))
                {
                    return new BotDecision<CombatDecision>(CombatDecision.RunToCover, reason);
                }
                FailedDecisionReasons.AppendLine($"{CombatDecision.RunToCover} {reason}");

                return new BotDecision<CombatDecision>(CombatDecision.MoveToCover, reason);
            }
            FailedDecisionReasons.AppendLine($"{CombatDecision.MoveToCover} {reason}");

            return new BotDecision<CombatDecision>(CombatDecision.HoldInCover, reason);
        }

        public bool GetDecision(out CombatDecision Decision)
        {
            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                Decision = CombatDecision.None;
                return false;
            }

            if (shallDogFight(enemy, out _))
            {
                Decision = CombatDecision.DogFight;
            }
            else if (shallStandAndShoot(enemy, out _))
            {
                if (Bot.Decision.CurrentSoloDecision != CombatDecision.StandAndShoot)
                {
                    Bot.Info.CalcHoldGroundDelay();
                }
                Decision = CombatDecision.StandAndShoot;
            }
            else if (shallShootDistantEnemy(enemy, out _))
            {
                Decision = CombatDecision.ShootDistantEnemy;
            }
            else if (shallRushEnemy(enemy, out _))
            {
                Decision = CombatDecision.RushEnemy;
            }
            else if (shallThrowGrenade(enemy, out _))
            {
                Decision = CombatDecision.ThrowGrenade;
            }
            else if (shallSearch(enemy, out _))
            {
                if (Bot.Decision.CurrentSoloDecision != CombatDecision.Search)
                {
                    enemy.Status.NumberOfSearchesStarted++;
                }
                Decision = CombatDecision.Search;
            }
            else if (shallFreezeAndWait(enemy, out _))
            {
                Decision = CombatDecision.Freeze;
            }
            //else if (shallMoveToEngage(enemy))
            //{
            //    Decision = SoloDecision.MoveToEngage;
            //}
            else if (shallShiftCover(enemy, out _))
            {
                Decision = CombatDecision.ShiftCover;
            }
            else if (shallMoveToCover(out _))
            {
                Decision = CombatDecision.MoveToCover;

                if (shallRunForCover(enemy, out _))
                {
                    Decision = CombatDecision.RunToCover;
                }
            }
            else if (shallHoldInCover(out _))
            {
                Decision = CombatDecision.HoldInCover;
            }
            else
            {
                Decision = CombatDecision.DebugNoDecision;
            }

            if (Decision != CombatDecision.MoveToCover &&
                Decision != CombatDecision.RunToCover)
            {
                StartRunCoverTimer = 0f;
            }

            return true;
        }

        private static readonly float GrenadeMaxEnemyDistance = 100f;

        private void checkFreezeTime()
        {
            if (Bot.Decision.CurrentSoloDecision != CombatDecision.Freeze)
            {
                FreezeFor = UnityEngine.Random.Range(10f, 120f);
                UnFreezeTime = Time.time + FreezeFor;
            }
        }

        private bool shallFreezeAndWait(Enemy enemy, out string reason)
        {
            if (Bot.Info.PersonalitySettings.Search.HeardFromPeaceBehavior != EHeardFromPeaceBehavior.Freeze)
            {
                reason = "wontFreeze";
                return false;
            }
            if (!enemy.Hearing.EnemyHeardFromPeace)
            {
                reason = "notHeardFromPeace";
                return false;
            }
            if (!Bot.Memory.Location.IsIndoors)
            {
                reason = "outside";
                return false;
            }
            if (!Bot.Memory.Location.IsIndoors)
            {
                reason = "outside";
                return false;
            }
            if (enemy.Seen && enemy.TimeSinceSeen < 240f)
            {
                reason = "seenRecent";
                return false;
            }
            if (enemy.TimeSinceLastKnownUpdated > 90f)
            {
                reason = "haventHeard";
                return false;
            }

            checkFreezeTime();

            if (UnFreezeTime < Time.time)
            {
                reason = "frozenTooLong";
                return false;
            }
            reason = "timeForFreeze";
            return true;
        }

        public float FreezeFor { get; private set; }
        public float UnFreezeTime { get; private set; }

        private bool shallThrowGrenade(Enemy enemy, out string reason)
        {
            if (!GlobalSettings.General.BotsUseGrenades)
            {
                reason = "grenadesDisabled";
                return false;
            }

            var grenades = BotOwner.WeaponManager.Grenades;
            if (!grenades.HaveGrenade)
            {
                if (_nextSayNeedGrenadeTime < Time.time)
                {
                    _nextSayNeedGrenadeTime = Time.time + 10;
                    Bot.Talk.GroupSay(EPhraseTrigger.NeedFrag, null, true, 5);
                }
                reason = "noNades";
                return false;
            }
            if (tryThrowGrenade())
            {
                reason = "throwing";
                return true;
            }
            if (_nextGrenadeCheckTime < Time.time &&
                canTryThrow(enemy))
            {
                //_nextGrenadeCheckTime = Time.time + 0.1f;
                if (findThrowTarget(enemy) &&
                    tryThrowGrenade())
                {
                    reason = "throwing";
                    return true;
                }
            }
            reason = "noGoodTarget";
            return false;
        }

        private float _nextSayNeedGrenadeTime;

        private bool canTryThrow(Enemy enemy)
        {
            if (this._nextPosibleAttempt > Time.time)
            {
                return false;
            }
            if (!BotOwner.Settings.FileSettings.Core.CanGrenade)
            {
                return false;
            }
            //if (!BotOwner.Settings.FileSettings.Grenade.CAN_LAY && BotOwner.BotLay.IsLay)
            //{
            //    return false;
            //}
            //if (!BotOwner.BotsGroup.GroupGrenade.CanThrow())
            //{
            //    return false;
            //}
            //if (BotOwner.AIData.PlaceInfo != null && BotOwner.AIData.PlaceInfo.BlockGrenade)
            //{
            //    return false;
            //}

            return !enemy.IsVisible
                && enemy.TimeSinceSeen > Bot.Info.FileSettings.Grenade.TimeSinceSeenBeforeThrow
                && enemy.TimeSinceLastKnownUpdated < 120f;
        }

        private bool findThrowTarget(Enemy enemy)
        {
            Vector3? lastKnown = enemy.LastKnownPosition;
            if (lastKnown == null)
            {
                return false;
            }

            if (tryThrowToPos(lastKnown.Value, "LastKnownPosition"))
            {
                return true;
            }
            Vector3? blindCorner = enemy.Path.EnemyCorners.GroundPosition(ECornerType.Blind);
            if (blindCorner != null &&
                (blindCorner.Value - lastKnown.Value).sqrMagnitude < 5f * 5f &&
                tryThrowToPos(blindCorner.Value, "BlindCornerToEnemy"))
            {
                return true;
            }
            /*

            if (throwTarget >= 3)
                throwTarget = 0;

            throwTarget++;

            switch (throwTarget)
            {
                case 1:
                    Vector3? lastCorner = enemy.Path.LastCornerToEnemy;
                    if (lastCorner != null &&
                        enemy.CanSeeLastCornerToEnemy &&
                        tryThrowToPos(lastCorner.Value, "LastCornerToEnemy"))
                    {
                        return true;
                    }
                    break;

                case 2:
                    Vector3? blindCorner = enemy.Path.BlindCornerToEnemy;
                    if (blindCorner != null &&
                        (blindCorner.Value - lastKnown.Value).sqrMagnitude < 5f * 5f &&
                        tryThrowToPos(blindCorner.Value, "BlindCornerToEnemy"))
                    {
                        return true;
                    }
                    break;

                case 3:
                    if (tryThrowToPos(lastKnown.Value, "LastKnownPosition"))
                    {
                        return true;
                    }
                    break;

                default:
                    break;
            }
            */
            return false;
        }

        private bool tryThrowToPos(Vector3 pos, string posString)
        {
            pos += Vector3.up * 0.25f;
            if (checkCanThrowToPosition(pos))
            {
                Logger.LogDebug($"{posString} Can Throw to pos");
                return true;
            }
            if (checkCanThrowToPosition(pos + (pos - Bot.Position).normalized))
            {
                Logger.LogDebug($"{posString} Can Throw to pos + (pos - SAINBot.Position).normalized");
                return true;
            }
            if (checkCanThrowToPosition(pos + Vector3.up))
            {
                Logger.LogDebug($"{posString} Can Throw to pos + vector3.up");
                return true;
            }
            return false;
        }

        private bool tryThrowGrenade()
        {
            var grenades = BotOwner.WeaponManager.Grenades;
            if (!grenades.ReadyToThrow)
            {
                return false;
            }
            if (!grenades.AIGreanageThrowData.IsUpToDate())
            {
                //Logger.LogDebug("not up2date");
                return false;
            }
            grenades.DoThrow();
            Bot.Talk.GroupSay(EPhraseTrigger.OnGrenade, null, true, 75);
            return true;
        }

        private bool checkCanThrowToPosition(Vector3 pos)
        {
            if ((pos - Bot.Position).sqrMagnitude < GrenadeMaxEnemyDistance.Sqr())
            {
                return CanThrowGrenade(BotOwner.WeaponRoot.position, pos);
            }
            return false;
        }

        public bool CanThrowGrenade(Vector3 from, Vector3 trg)
        {
            if (_nextPosibleAttempt > Time.time)
            {
                return false;
            }
            if (!checkFriendlyDistances(trg))
            {
                _nextPosibleAttempt = 1f + Time.time;
                return false;
            }
            var angles = Bot.Memory.Location.IsIndoors ? _indoorAngles : _outdoorAngles;
            AIGreandeAng greandeAng = angles.PickRandom();
            AIGreanageThrowData aigreanageThrowData = GClass493.CanThrowGrenade2(from, trg, this.MaxPower, greandeAng, -1f, BotOwner.Settings.FileSettings.Grenade.MIN_THROW_DIST_PERCENT_0_1);
            if (aigreanageThrowData.CanThrow)
            {
                _nextPosibleAttempt = 3f + Time.time;
                BotOwner.WeaponManager.Grenades.SetThrowData(aigreanageThrowData);
                //Logger.LogDebug("canThrow");
                return true;
            }
            return false;
        }

        private static AIGreandeAng[] _indoorAngles =
        {
            AIGreandeAng.ang5,
            AIGreandeAng.ang15,
            AIGreandeAng.ang25,
            AIGreandeAng.ang35,
        };

        private static AIGreandeAng[] _outdoorAngles = EnumValues.GetEnum<AIGreandeAng>();

        public float MaxPower => BotOwner.WeaponManager.Grenades.MaxPower;

        private bool checkFriendlyDistances(Vector3 trg)
        {
            for (int i = 0; i < BotOwner.BotsGroup.MembersCount; i++)
            {
                if ((BotOwner.BotsGroup.Member(i).Transform.position - trg).sqrMagnitude < BotOwner.Settings.FileSettings.Grenade.MIN_DIST_NOT_TO_THROW_SQR)
                {
                    return false;
                }
            }
            return true;
        }

        private float _nextPosibleAttempt;

        private float _nextGrenadeCheckTime;

        private static readonly float RushEnemyMaxPathDistance = 10f;
        private static readonly float RushEnemyMaxPathDistanceSprint = 20f;
        private static readonly float RushEnemyLowAmmoRatio = 0.4f;

        private bool shallRushEnemy(Enemy enemy, out string reason)
        {
            var health = Bot.Memory.Health.HealthStatus;
            if (health == ETagStatus.Dying)
            {
                reason = "imDying";
                return false;
            }
            if (enemy.Hearing.EnemyHeardFromPeace &&
                Bot.Info.PersonalitySettings.Search.HeardFromPeaceBehavior == EHeardFromPeaceBehavior.Charge)
            {
                reason = "heardFromPeaceCharge";
                return true;
            }
            if (!Bot.Info.PersonalitySettings.Rush.CanRushEnemyReloadHeal)
            {
                reason = "cantRush";
                return false;
            }
            if (Bot.Decision.SelfActionDecisions.LowOnAmmo(RushEnemyLowAmmoRatio))
            {
                reason = "lowAmmo";
                return false;
            }

            if (!checkInRangeForRush(enemy))
            {
                reason = "outOfRange";
                return false;
            }
            if (enemy.Status.VulnerableAction != EEnemyAction.None)
            {
                reason = "enemyVulnerable";
                return true;
            }
            ETagStatus enemyHealth = enemy.EnemyPlayer.HealthStatus;
            if (enemyHealth == ETagStatus.Dying)
            {
                reason = "enemyHurtBad";
                return true;
            }
            if (enemyHealth == ETagStatus.BadlyInjured && 
                enemy.EnemyPlayer.IsInPronePose)
            {
                reason = "enemyHurtAndProne";
                return true;
            }
            reason = "notGoodTimeTo";
            return false;
        }

        private bool checkInRangeForRush(Enemy enemy)
        {
            EEnemyAction vulnerableAction = enemy.Status.VulnerableAction;
            float modifier = vulnerableAction == EEnemyAction.UsingSurgery ? 2f : 1f;
            if (enemy.Path.PathDistance < RushEnemyMaxPathDistance * modifier)
            {
                return true;
            }
            if (enemy.Path.PathDistance < RushEnemyMaxPathDistanceSprint * modifier && 
                BotOwner.CanSprintPlayer)
            {
                return true;
            }
            return false;
        }

        private CoverSettings CoverSettings => SAINPlugin.LoadedPreset.GlobalSettings.Cover;
        private float ShiftCoverChangeDecisionTime => CoverSettings.ShiftCoverChangeDecisionTime;
        private float ShiftCoverTimeSinceSeen => CoverSettings.ShiftCoverTimeSinceSeen;
        private float ShiftCoverTimeSinceEnemyCreated => CoverSettings.ShiftCoverTimeSinceEnemyCreated;
        private float ShiftCoverNoEnemyResetTime => CoverSettings.ShiftCoverNoEnemyResetTime;
        private float ShiftCoverNewCoverTime => CoverSettings.ShiftCoverNewCoverTime;
        private float ShiftCoverResetTime => CoverSettings.ShiftCoverResetTime;

        private bool shallShiftCover(Enemy enemy, out string reason)
        {
            if (Bot.Info.PersonalitySettings.Cover.CanShiftCoverPosition == false)
            {
                reason = "cantShift";
                return false;
            }
            if (Bot.Suppression.IsSuppressed)
            {
                reason = "suppressed";
                return false;
            }

            if (ContinueShiftCover())
            {
                reason = "continueShift";
                return true;
            }

            var CurrentDecision = Bot.Decision.CurrentSoloDecision;

            if (CurrentDecision == CombatDecision.HoldInCover && Bot.Info.PersonalitySettings.Cover.CanShiftCoverPosition)
            {
                if (Bot.Decision.TimeSinceChangeDecision > ShiftCoverChangeDecisionTime && TimeForNewShift < Time.time)
                {
                    if (enemy != null)
                    {
                        if (enemy.Seen && !enemy.IsVisible && enemy.TimeSinceSeen > ShiftCoverTimeSinceSeen)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            reason = "enemyNotSeen";
                            return true;
                        }
                        if (!enemy.Seen && enemy.KnownPlaces.TimeSinceLastKnownUpdated > ShiftCoverTimeSinceEnemyCreated)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            reason = "lastKnownNotUpdated";
                            return true;
                        }
                    }
                    if (enemy == null && Bot.Decision.TimeSinceChangeDecision > ShiftCoverNoEnemyResetTime)
                    {
                        TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                        ShiftResetTimer = Time.time + ShiftCoverResetTime;
                        reason = "timeDecisionMade";
                        return true;
                    }
                }
            }

            reason = "dontWantTo";
            ShiftResetTimer = -1f;
            return false;
        }

        private bool ContinueShiftCover()
        {
            var CurrentDecision = Bot.Decision.CurrentSoloDecision;
            if (CurrentDecision == CombatDecision.ShiftCover)
            {
                if (ShiftResetTimer > 0f && ShiftResetTimer < Time.time)
                {
                    ShiftResetTimer = -1f;
                    return false;
                }
                if (!BotOwner.Mover.IsMoving && !Bot.Mover.SprintController.Running)
                {
                    return false;
                }
                if (!ShiftCoverComplete)
                {
                    return true;
                }
            }
            return false;
        }

        private float TimeForNewShift;

        private float ShiftResetTimer;
        public bool ShiftCoverComplete { get; set; }

        private bool shallDogFight(Enemy enemy, out string reason)
        {
            if (Bot.Decision.CurrentSelfDecision != SelfDecision.None || BotOwner.WeaponManager.Reload.Reloading)
            {
                reason = "selfDecisionOrReloading";
                return false;
            }

            if (Bot.Decision.CurrentSoloDecision == CombatDecision.RushEnemy)
            {
                reason = "rushingEnemy";
                return false;
            }

            //var currentSolo = Bot.Decision.CurrentSoloDecision;
            //if (Time.time - Bot.Cover.LastHitTime < 2f
            //    && currentSolo != SoloDecision.RunAway
            //    && currentSolo != SoloDecision.RunToCover
            //    && currentSolo != SoloDecision.Retreat
            //    && currentSolo != SoloDecision.MoveToCover)
            //{
            //    reason = "shotInCover";
            //    ShotInCover = true;
            //    return true;
            //}

            if (Bot.Cover.SpottedInCover == true)
            {
                reason = "coverSpotted";
                return true;
            }
            if ((enemy.EPathDistance == EPathDistance.VeryClose && Bot.Enemy.IsVisible))
            {
                reason = "enemyClose";
                return true;
            }

            reason = string.Empty;
            return false;
        }

        public bool ShotInCover;

        private bool shallMoveToEngage(Enemy enemy)
        {
            if (Bot.Suppression.IsSuppressed)
            {
                return false;
            }
            if (!enemy.Seen || enemy.TimeSinceSeen < 8f)
            {
                return false;
            }
            if (enemy.IsVisible && enemy.EnemyLookingAtMe)
            {
                return false;
            }
            var decision = Bot.Decision.CurrentSoloDecision;
            if (BotOwner.Memory.IsUnderFire && decision != CombatDecision.MoveToEngage)
            {
                return false;
            }
            if (decision == CombatDecision.Retreat || decision == CombatDecision.MoveToCover || decision == CombatDecision.RunToCover)
            {
                return false;
            }
            if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance
                && decision != CombatDecision.MoveToEngage)
            {
                return true;
            }
            if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance * 0.66f
                && decision == CombatDecision.MoveToEngage)
            {
                return true;
            }
            return false;
        }

        private bool shallShootDistantEnemy(Enemy enemy, out string reason)
        {
            if (_endShootDistTargetTime > Time.time
                && Bot.Decision.CurrentSoloDecision == CombatDecision.ShootDistantEnemy
                && Bot.Memory.Health.HealthStatus != ETagStatus.Dying)
            {
                reason = "shootingDistantEnemy";
                return true;
            }
            if (_nextShootDistTargetTime < Time.time
                && enemy.RealDistance > Bot.Info.FileSettings.Shoot.MaxPointFireDistance
                && enemy.IsVisible
                && enemy.CanShoot
                && (Bot.Memory.Health.HealthStatus == ETagStatus.Healthy || Bot.Memory.Health.HealthStatus == ETagStatus.Injured))
            {
                float timeAdd = 6f * UnityEngine.Random.Range(0.75f, 1.25f);
                _nextShootDistTargetTime = Time.time + timeAdd;
                _endShootDistTargetTime = Time.time + timeAdd / 3f;
                reason = "shootingDistantEnemy";
                return true;
            }
            reason = string.Empty;
            return false;
        }

        private float _nextShootDistTargetTime;
        private float _endShootDistTargetTime;

        private bool shallRunForCover(Enemy enemy, out string reason)
        {
            if (!BotOwner.CanSprintPlayer)
            {
                reason = "cantSprint";
                return false;
            }

            if (Bot.Cover.CoverPoints.Count == 0)
            {
                reason = "noCoverPoints";
                return false;
            }

            if (!enemy.IsVisible &&
                (!enemy.Seen || enemy.TimeSinceSeen > 3f))
            {
                reason = "runNow cantSeeEnemy";
                return true;
            }

            if (StartRunCoverTimer < Time.time)
            {
                reason = "timeToRun";
                return true;
            }

            reason = "dontRunYet";
            return false;
        }

        private bool _inCover;

        private static readonly float RunToCoverTime = 1.5f;
        private static readonly float RunToCoverTimeRandomMin = 0.66f;
        private static readonly float RunToCoverTimeRandomMax = 1.33f;

        private bool shallMoveToCover(out string reason)
        {
            if (Bot.Cover.InCover)
            {
                reason = "inCover";
                return false;
            }

            var CurrentDecision = Bot.Decision.CurrentSoloDecision;
            if (CurrentDecision != CombatDecision.MoveToCover && CurrentDecision != CombatDecision.RunToCover)
            {
                StartRunCoverTimer = Time.time + RunToCoverTime * UnityEngine.Random.Range(RunToCoverTimeRandomMin, RunToCoverTimeRandomMax);
            }

            reason = "notInCover";
            return true;
        }

        private bool shallSearch(Enemy enemy, out string reason)
        {
            bool shallSearch = Bot.Search.SearchDecider.ShallStartSearch(enemy, out SearchReasonsStruct reasons);
            DebugSearchReasons = reasons;
            DebugShallSearch = shallSearch;
            if (shallSearch)
            {
                reason = "wantToSearch";
            }
            else
            {
                reason = "cantSearch";
            }
            return shallSearch;
        }

        public bool? DebugShallSearch { get; set; }

        public SearchReasonsStruct DebugSearchReasons { get; private set; }

        public bool shallHoldInCover(out string reason)
        {
            if (Bot.Cover.InCover)
            {
                reason = "inCover";
                return true;
            }
            reason = "notInCover";
            return false;
        }

        private bool shallStandAndShoot(Enemy enemy, out string reason)
        {
            if (!enemy.IsVisible)
            {
                reason = "cantSeeEnemy";
                return false;
            }
            if (!enemy.CanShoot)
            {
                reason = "cantShootEnemy";
                return false;
            }
            if (BotOwner.WeaponManager?.HaveBullets == false)
            {
                reason = "noBullets";
                return false;
            }
            if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance * 1.25f)
            {
                reason = "outOfRange";
                return false;
            }

            float holdGround = Bot.Info.HoldGroundDelay;
            if (holdGround <= 0f)
            {
                reason = "wontHoldGround";
                return false;
            }

            if (!enemy.EnemyLookingAtMe)
            {
                reason = "enemyNotLooking";
                return true;
            }

            float visibleFor = Time.time - enemy.Vision.VisibleStartTime;
            if (visibleFor > holdGround)
            {
                reason = "visibleTooLong";
                return false;
            }

            if (visibleFor < holdGround / 1.5f)
            {
                reason = "holdingFromTime";
                return true;
            }
            else if (Bot.Cover.CheckLimbsForCover())
            {
                reason = "holdingHaveSomeCover";
                return true;
            }
            reason = "outOfTime";
            return false;
        }

        private float StartRunCoverTimer;
    }
}