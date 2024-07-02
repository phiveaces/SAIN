using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Search;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision
{
    public class EnemyDecisionClass : BotBaseClass, ISAINClass
    {
        public EnemyDecisionClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPresetChanges(null);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public bool GetDecision(out SoloDecision Decision)
        {
            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                Decision = SoloDecision.None;
                return false;
            }

            if (shallDogFight(enemy))
            {
                Decision = SoloDecision.DogFight;
            }
            else if (shallStandAndShoot(enemy))
            {
                if (Bot.Decision.CurrentSoloDecision != SoloDecision.StandAndShoot)
                {
                    Bot.Info.CalcHoldGroundDelay();
                }
                Decision = SoloDecision.StandAndShoot;
            }
            else if (shallShootDistantEnemy(enemy))
            {
                Decision = SoloDecision.ShootDistantEnemy;
            }
            else if (shallRushEnemy(enemy))
            {
                Decision = SoloDecision.RushEnemy;
            }
            else if (shallThrowGrenade(enemy))
            {
                Decision = SoloDecision.ThrowGrenade;
            }
            else if (shallSearch())
            {
                if (Bot.Decision.CurrentSoloDecision != SoloDecision.Search)
                {
                    enemy.Status.NumberOfSearchesStarted++;
                }
                Decision = SoloDecision.Search;
            }
            else if (shallFreezeAndWait(enemy))
            {
                Decision = SoloDecision.Freeze;
            }
            //else if (shallMoveToEngage(enemy))
            //{
            //    Decision = SoloDecision.MoveToEngage;
            //}
            else if (shallShiftCover(enemy))
            {
                Decision = SoloDecision.ShiftCover;
            }
            else if (shallMoveToCover())
            {
                Decision = SoloDecision.MoveToCover;

                if (shallRunForCover())
                {
                    Decision = SoloDecision.RunToCover;
                }
            }
            else if (shallHoldInCover())
            {
                Decision = SoloDecision.HoldInCover;
            }
            else
            {
                Decision = SoloDecision.DebugNoDecision;
            }

            if (Decision != SoloDecision.MoveToCover &&
                Decision != SoloDecision.RunToCover)
            {
                StartRunCoverTimer = 0f;
            }

            return true;
        }

        private static readonly float GrenadeMaxEnemyDistance = 100f;

        private void checkFreezeTime()
        {
            if (Bot.Decision.CurrentSoloDecision != SoloDecision.Freeze)
            {
                FreezeFor = UnityEngine.Random.Range(10f, 120f);
                UnFreezeTime = Time.time + FreezeFor;
            }
        }

        private bool shallFreezeAndWait(Enemy enemy)
        {
            if (enemy.Hearing.EnemyHeardFromPeace &&
                Bot.Memory.Location.IsIndoors &&
                (!enemy.Seen || enemy.TimeSinceSeen > 240f) &&
                enemy.TimeSinceLastKnownUpdated < 90f)
            {
                checkFreezeTime();

                if (UnFreezeTime > Time.time)
                {
                    return true;
                }
            }
            return false;
        }

        public float FreezeFor { get; private set; }
        public float UnFreezeTime { get; private set; }

        private bool shallThrowGrenade(Enemy enemy)
        {
            if (!GlobalSettings.General.BotsUseGrenades)
            {
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
                return false;
            }
            if (tryThrowGrenade())
            {
                return true;
            }
            if (_nextGrenadeCheckTime < Time.time &&
                canTryThrow(enemy))
            {
                //_nextGrenadeCheckTime = Time.time + 0.1f;
                if (findThrowTarget(enemy) &&
                    tryThrowGrenade())
                {
                    return true;
                }
            }
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

        private bool shallRushEnemy(Enemy enemy)
        {
            if (Bot.Info.PersonalitySettings?.Rush.CanRushEnemyReloadHeal == true && enemy != null)
            {
                if (!Bot.Decision.SelfActionDecisions.LowOnAmmo(RushEnemyLowAmmoRatio))
                {
                    bool inRange = false;
                    EEnemyAction vulnerableAction = enemy.Status.VulnerableAction;
                    float modifier = vulnerableAction == EEnemyAction.UsingSurgery ? 2f : 1f;
                    if (enemy.Path.PathDistance < RushEnemyMaxPathDistanceSprint
                        && BotOwner?.CanSprintPlayer == true)
                    {
                        inRange = true;
                    }
                    else if (enemy.Path.PathDistance < RushEnemyMaxPathDistance)
                    {
                        inRange = true;
                    }

                    if (inRange
                        && Bot.Memory.Health.HealthStatus != ETagStatus.Dying
                        && Bot.Memory.Health.HealthStatus != ETagStatus.BadlyInjured)
                    {
                        if (vulnerableAction != EEnemyAction.None)
                        {
                            return true;
                        }
                        ETagStatus enemyHealth = enemy.EnemyPlayer.HealthStatus;
                        if (enemyHealth == ETagStatus.Dying || enemyHealth == ETagStatus.BadlyInjured)
                        {
                            return true;
                        }
                        else if (enemyHealth == ETagStatus.Injured && enemy.EnemyPlayer.IsInPronePose)
                        {
                            return true;
                        }
                    }
                }
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

        private bool shallShiftCover(Enemy enemy)
        {
            if (Bot.Info.PersonalitySettings.Cover.CanShiftCoverPosition == false)
            {
                return false;
            }
            if (Bot.Suppression.IsSuppressed)
            {
                return false;
            }

            if (ContinueShiftCover())
            {
                return true;
            }

            var CurrentDecision = Bot.Decision.CurrentSoloDecision;

            if (CurrentDecision == SoloDecision.HoldInCover && Bot.Info.PersonalitySettings.Cover.CanShiftCoverPosition)
            {
                if (Bot.Decision.TimeSinceChangeDecision > ShiftCoverChangeDecisionTime && TimeForNewShift < Time.time)
                {
                    if (enemy != null)
                    {
                        if (enemy.Seen && !enemy.IsVisible && enemy.TimeSinceSeen > ShiftCoverTimeSinceSeen)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            return true;
                        }
                        if (!enemy.Seen && enemy.KnownPlaces.TimeSinceLastKnownUpdated > ShiftCoverTimeSinceEnemyCreated)
                        {
                            TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                            ShiftResetTimer = Time.time + ShiftCoverResetTime;
                            return true;
                        }
                    }
                    if (enemy == null && Bot.Decision.TimeSinceChangeDecision > ShiftCoverNoEnemyResetTime)
                    {
                        TimeForNewShift = Time.time + ShiftCoverNewCoverTime;
                        ShiftResetTimer = Time.time + ShiftCoverResetTime;
                        return true;
                    }
                }
            }

            ShiftResetTimer = -1f;
            return false;
        }

        private bool ContinueShiftCover()
        {
            var CurrentDecision = Bot.Decision.CurrentSoloDecision;
            if (CurrentDecision == SoloDecision.ShiftCover)
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

        private bool shallDogFight(Enemy enemy)
        {
            if (Bot.Decision.CurrentSelfDecision != SelfDecision.None || BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }

            if (Bot.Decision.CurrentSoloDecision == SoloDecision.RushEnemy)
            {
                return false;
            }

            var currentSolo = Bot.Decision.CurrentSoloDecision;
            if (Time.time - Bot.Cover.LastHitTime < 2f
                && currentSolo != SoloDecision.RunAway
                && currentSolo != SoloDecision.RunToCover
                && currentSolo != SoloDecision.Retreat
                && currentSolo != SoloDecision.MoveToCover)
            {
                ShotInCover = true;
                return true;
            }
            ShotInCover = false;

            var pathStatus = enemy.EPathDistance;
            return (pathStatus == EPathDistance.VeryClose && Bot.Enemy.IsVisible) || Bot.Cover.CoverInUse?.Spotted == true;
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
            if (BotOwner.Memory.IsUnderFire && decision != SoloDecision.MoveToEngage)
            {
                return false;
            }
            if (decision == SoloDecision.Retreat || decision == SoloDecision.MoveToCover || decision == SoloDecision.RunToCover)
            {
                return false;
            }
            if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance
                && decision != SoloDecision.MoveToEngage)
            {
                return true;
            }
            if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance * 0.66f
                && decision == SoloDecision.MoveToEngage)
            {
                return true;
            }
            return false;
        }

        private bool shallShootDistantEnemy(Enemy enemy)
        {
            if (_endShootDistTargetTime > Time.time
                && Bot.Decision.CurrentSoloDecision == SoloDecision.ShootDistantEnemy
                && Bot.Memory.Health.HealthStatus != ETagStatus.Dying)
            {
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
                return true;
            }
            return false;
        }

        private float _nextShootDistTargetTime;
        private float _endShootDistTargetTime;

        private bool shallRunForCover()
        {
            if (!BotOwner.CanSprintPlayer)
            {
                return false;
            }

            bool runNow = Bot.Enemy != null
                && !Bot.Enemy.IsVisible
                && (!Bot.Enemy.Seen || Bot.Enemy.TimeSinceSeen > 3f)
                && Bot.Cover.CoverPoints.Count > 0;

            if (StartRunCoverTimer < Time.time || runNow)
            {
                CoverPoint coverInUse = Bot.Cover.CoverInUse;
                if (coverInUse != null)
                {
                    return (coverInUse.Position - Bot.Position).sqrMagnitude >= 1f;
                }
                return true;
            }
            return false;
        }

        private static readonly float RunToCoverTime = 1.5f;
        private static readonly float RunToCoverTimeRandomMin = 0.66f;
        private static readonly float RunToCoverTimeRandomMax = 1.33f;

        private bool shallMoveToCover()
        {
            CoverPoint coverInUse = Bot.Cover.CoverInUse;
            if (coverInUse == null || coverInUse.Status != CoverStatus.InCover)
            {
                var CurrentDecision = Bot.Decision.CurrentSoloDecision;
                if (CurrentDecision != SoloDecision.MoveToCover && CurrentDecision != SoloDecision.RunToCover)
                {
                    StartRunCoverTimer = Time.time + RunToCoverTime * UnityEngine.Random.Range(RunToCoverTimeRandomMin, RunToCoverTimeRandomMax);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool shallSearch()
        {
            bool shallSearch = Bot.Search.SearchDecider.ShallStartSearch(true, out SearchReasonsStruct reasons);
            DebugSearchReasons = reasons;
            DebugShallSearch = shallSearch;
            return shallSearch;
        }

        public bool? DebugShallSearch { get; set; }

        public SearchReasonsStruct DebugSearchReasons { get; private set; }

        public bool shallHoldInCover()
        {
            var cover = Bot.Cover.CoverInUse;
            if (cover != null
                && !cover.Spotted
                && cover.Status == CoverStatus.InCover)
            {
                return true;
            }
            return false;
        }

        private bool shallStandAndShoot(Enemy enemy)
        {
            if (enemy.IsVisible &&
                enemy.CanShoot &&
                BotOwner.WeaponManager?.HaveBullets == true)
            {
                if (enemy.RealDistance > Bot.Info.WeaponInfo.EffectiveWeaponDistance * 1.25f)
                {
                    return false;
                }
                float holdGround = Bot.Info.HoldGroundDelay;

                if (holdGround <= 0f)
                {
                    return false;
                }

                if (!enemy.EnemyLookingAtMe)
                {
                    return true;
                    //CoverPoint closestPoint = SAINBot.Cover.ClosestPoint;
                    //if (!enemy.EnemyLookingAtMe && closestPoint != null && closestPoint.Status <= CoverStatus.CloseToCover)
                    //{
                    //    return true;
                    //}
                }

                float visibleFor = Time.time - enemy.Vision.VisibleStartTime;

                if (visibleFor < holdGround)
                {
                    if (visibleFor < holdGround / 1.5f)
                    {
                        return true;
                    }
                    else
                    {
                        return Bot.Cover.CheckLimbsForCover();
                    }
                }
            }
            return false;
        }

        private float StartRunCoverTimer;
    }
}