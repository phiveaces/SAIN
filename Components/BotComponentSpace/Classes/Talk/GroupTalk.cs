using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Preset;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class GroupTalk : BotBase, IBotClass
    {
        public GroupTalk(BotComponent bot) : base(bot)
        {
        }

        public bool FriendIsClose
        {
            get
            {
                if (Player == null)
                {
                    return false;
                }
                if (_nextCheckFriendsTime > Time.time)
                {
                    return _friendIsClose;
                }

                _nextCheckFriendsTime = Time.time + 1f;
                updateFriendClose();
                return _friendIsClose;
            }
        }

        public void Init()
        {
            base.SubscribeToPreset(updateConfigSettings);
        }

        public void Update()
        {
            if (!Bot.Talk.CanTalk)
            {
                return;
            }
            if (!BotSquad.BotInGroup ||
                !Bot.Info.FileSettings.Mind.SquadTalk ||
                SAINPlugin.LoadedPreset.GlobalSettings.Talk.DisableBotTalkPatching)
            {
                if (Subscribed)
                    unsub();

                return;
            }

            if (!Subscribed)
                sub();

            checkGroupTalk();
        }

        private void checkGroupTalk()
        {
            if (TalkTimer < Time.time)
            {
                TalkTimer = Time.time + _groupTalkFreq;
                if (FriendIsClose)
                {
                    if (ShallReportReloading())
                    {
                        return;
                    }
                    if (Bot.Squad.IAmLeader
                        && UpdateLeaderCommand())
                    {
                        return;
                    }
                    if (CheckEnemyContact())
                    {
                        return;
                    }
                    if (TalkHurt())
                    {
                        return;
                    }
                    if (ShallTalkRetreat())
                    {
                        return;
                    }
                    if (TalkEnemyLocation())
                    {
                        return;
                    }
                    if (ShallReportLostVisual())
                    {
                        return;
                    }
                    if (ShallReportNeedHelp())
                    {
                        return;
                    }
                    if (EFTMath.RandomBool(30)
                        && TalkBotDecision(out var trigger, out _))
                    {
                        Bot.Talk.Say(trigger, null, false);
                    }
                }
            }
        }

        private bool ShallReportReloading()
        {
            if (_nextReportReloadTime < Time.time
                && Bot.Decision.CurrentSelfDecision == SelfDecision.Reload)
            {
                _nextReportReloadTime = Time.time + _reportReloadingFreq;
                return Bot.Talk.GroupSay(reloadPhrases.PickRandom(), null, false, _reportReloadingChance);
            }
            return false;
        }

        private bool ShallReportLostVisual()
        {
            var enemy = Bot.Enemy;
            if (enemy != null && enemy.Vision.ShallReportLostVisual)
            {
                enemy.Vision.ShallReportLostVisual = false;
                if (EFTMath.RandomBool(_reportLostVisualChance))
                {
                    ETagStatus mask = PersonIsClose(enemy.EnemyPlayer) ? ETagStatus.Combat : ETagStatus.Aware;
                    if (enemy.TimeSinceSeen > _reportRatTimeSinceSeen && EFTMath.RandomBool(_reportRatChance))
                    {
                        return Bot.Talk.GroupSay(EPhraseTrigger.Rat, null, false, 100);
                    }
                    else
                    {
                        return Bot.Talk.GroupSay(EPhraseTrigger.LostVisual, null, false, 100);
                    }
                }
            }
            return false;
        }

        private void EnemyConversation(EPhraseTrigger trigger, ETagStatus status, Player player)
        {
            if (player == null)
            {
                return;
            }
            if (Bot.HasEnemy || !FriendIsClose)
            {
                return;
            }
            Enemy enemy = Bot.EnemyController.GetEnemy(player.ProfileId, true);
            if (enemy == null)
            {
                return;
            }
            if (enemy.RealDistance > _reportEnemyMaxDist)
            {
                return;
            }
            Bot.Talk.GroupSay(EPhraseTrigger.OnEnemyConversation, null, false, _reportEnemyConversationChance);
        }

        public void TalkEnemySniper()
        {
            if (FriendIsClose)
            {
                Bot.Talk.TalkAfterDelay(EPhraseTrigger.SniperPhrase, ETagStatus.Combat, UnityEngine.Random.Range(0.5f, 1f));
            }
        }

        public void Dispose()
        {
            unsub();
        }

        private void unsub()
        {
            if (Subscribed)
            {
                Subscribed = false;
                var squad = Bot?.Squad?.SquadInfo;
                if (squad != null)
                {
                    squad.MemberKilled -= friendlyDown;
                    squad.OnEnemyHeard -= enemyHeard;
                }

                var botController = SAINBotController.Instance;

                if (botController != null)
                    botController.PlayerTalk -= EnemyConversation;

                if (Bot.EnemyController != null)
                {
                    Bot.EnemyController.Events.OnEnemyKilled -= OnEnemyDown;
                    Bot.EnemyController.Events.OnEnemyHealthChanged -= onHealthChanged;
                }
            }
        }

        private void sub()
        {
            var squad = Bot?.Squad?.SquadInfo;
            if (!Subscribed && squad != null)
            {
                Subscribed = true;
                squad.MemberKilled += friendlyDown;
                squad.OnEnemyHeard += enemyHeard;
                SAINBotController.Instance.PlayerTalk += EnemyConversation;
                BotOwner.DeadBodyWork.OnStartLookToBody += OnLootBody;
                Bot.EnemyController.Events.OnEnemyKilled += OnEnemyDown;
                Bot.EnemyController.Events.OnEnemyHealthChanged += onHealthChanged;
            }
        }

        private void onHealthChanged(ETagStatus health, Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }
            if (!enemy.IsCurrentEnemy)
            {
                return;
            }
            if (health != ETagStatus.Dying && health != ETagStatus.BadlyInjured)
            {
                return;
            }
            if (!EFTMath.RandomBool(_reportEnemyHealthChance))
            {
                return;
            }
            if (_nextCheckEnemyHPTime < Time.time)
            {
                _nextCheckEnemyHPTime = Time.time + _reportEnemyHealthFreq;
                Bot.Talk.GroupSay(EPhraseTrigger.OnEnemyShot, null, false, 100);
            }
        }

        private bool CheckEnemyContact()
        {
            Enemy enemy = Bot.Enemy;
            if (FriendIsClose
                && enemy != null)
            {
                if (enemy.FirstContactOccured
                    && !enemy.FirstContactReported)
                {
                    enemy.FirstContactReported = true;
                    if (EFTMath.RandomBool(40))
                    {
                        ETagStatus mask = PersonIsClose(enemy.EnemyPlayer) ? ETagStatus.Combat : ETagStatus.Aware;
                        return Bot.Talk.GroupSay(EPhraseTrigger.OnFirstContact, mask, true, 100);
                    }
                }
                if (enemy.Vision.ShallReportRepeatContact)
                {
                    enemy.Vision.ShallReportRepeatContact = false;
                    if (EFTMath.RandomBool(40))
                    {
                        ETagStatus mask = PersonIsClose(enemy.EnemyPlayer) ? ETagStatus.Combat : ETagStatus.Aware;
                        return Bot.Talk.GroupSay(EPhraseTrigger.OnRepeatedContact, mask, false, 100);
                    }
                }
            }
            return false;
        }

        private void OnEnemyDown(Player player)
        {
            if (!_reportEnemyKilledToxicSquadLeader)
            {
                var settings = player?.Profile?.Info?.Settings;
                if (settings == null || !BotOwner.BotsGroup.IsPlayerEnemyByRole(settings.Role))
                {
                    return;
                }

                if (!FriendIsClose || !PersonIsClose(player))
                {
                    return;
                }
            }

            if (EFTMath.RandomBool(_reportEnemyKilledChance))
            {
                float randomTime = UnityEngine.Random.Range(0.2f, 0.6f);
                Bot.Talk.TalkAfterDelay(EPhraseTrigger.EnemyDown, null, randomTime);

                var leader = Bot.Squad.SquadInfo?.LeaderComponent;
                if (leader?.Person?.IPlayer != null
                    && !Bot.Squad.IAmLeader
                    && EFTMath.RandomBool(_reportEnemyKilledSquadLeadChance)
                    && PersonIsClose(leader.Person.IPlayer))
                {
                    leader.Talk.TalkAfterDelay(EPhraseTrigger.GoodWork, null, randomTime + 0.75f);
                }
            }
        }

        private bool PersonIsClose(IPlayer player)
        {
            return player != null && BotOwner != null && (player.Position - BotOwner.Position).magnitude < 30f;
        }

        private bool PersonIsClose(Player player)
        {
            return player != null && BotOwner != null && (player.Position - BotOwner.Position).magnitude < 30f;
        }

        private void updateFriendClose()
        {
            float friendCloseDist = _friendCloseDist.Sqr();
            _friendIsClose = false;
            foreach (var member in Bot.Squad.Members.Values)
            {
                if (member != null
                    && !member.IsDead
                    && member.Player.ProfileId != Player.ProfileId
                    && (member.Position - Bot.Position).sqrMagnitude < friendCloseDist)
                {
                    _friendIsClose = true;
                    break;
                }
            }
            if (!_friendIsClose && Bot.Squad.HumanFriendClose)
            {
                _friendIsClose = true;
            }
        }

        private void friendlyDown(IPlayer player, DamageInfo damage, float time)
        {
            if (BotOwner.IsDead || BotOwner.BotState != EBotState.Active || !EFTMath.RandomBool(_reportFriendKilledChance))
            {
                return;
            }

            updateFriendClose();
            if (!_friendIsClose || !PersonIsClose(player))
            {
                return;
            }
            Bot.Talk.TalkAfterDelay(EPhraseTrigger.OnFriendlyDown, ETagStatus.Combat, UnityEngine.Random.Range(0.33f, 0.66f));
        }

        private void OnLootBody(float num)
        {
            if (!Bot.BotActive || !FriendIsClose)
            {
                return;
            }

            EPhraseTrigger trigger = LootPhrases.PickRandom();
            Bot.Talk.Say(trigger, null, true);
        }

        private void allMembersSay(EPhraseTrigger trigger, ETagStatus mask, EPhraseTrigger commandTrigger, float delay = 1.5f, float chance = 100f)
        {
            if (Bot.Squad.LeaderComponent == null)
            {
                return;
            }

            bool memberTalked = false;
            foreach (var member in BotSquad.Members.Values)
            {
                if (member != null && 
                    !member.IsDead && 
                    EFTMath.RandomBool(chance) &&
                    !member.Squad.IAmLeader && 
                    member.Squad.DistanceToSquadLeader <= 40f)
                {
                    memberTalked = true;

                    EPhraseTrigger myTrigger = trigger;
                    switch (commandTrigger)
                    {
                        case EPhraseTrigger.GetBack:
                        case EPhraseTrigger.HoldPosition:
                            if (member.Decision.CurrentSquadDecision == SquadDecision.GroupSearch)
                            {
                                myTrigger = EPhraseTrigger.Negative;
                                break;
                            }
                            switch (member.Decision.CurrentSoloDecision)
                            {
                                case SoloDecision.Search:
                                    myTrigger = EPhraseTrigger.Negative;
                                    break;
                                case SoloDecision.HoldInCover:
                                    myTrigger = EFTMath.RandomBool() ? EPhraseTrigger.Roger : EPhraseTrigger.OnPosition;
                                    break;
                                case SoloDecision.RushEnemy:
                                    myTrigger = EPhraseTrigger.Negative;
                                    break;

                                default:
                                    break;
                            }
                            break;

                        case EPhraseTrigger.Gogogo:
                        case EPhraseTrigger.FollowMe:
                            if (member.Decision.CurrentSquadDecision == SquadDecision.GroupSearch)
                            {
                                myTrigger = EFTMath.RandomBool() ? EPhraseTrigger.Ready : EPhraseTrigger.Going;
                                break;
                            }
                            switch (member.Decision.CurrentSoloDecision)
                            {
                                case SoloDecision.Search:
                                    myTrigger = EFTMath.RandomBool() ? EPhraseTrigger.Ready : EPhraseTrigger.Going;
                                    break;
                                case SoloDecision.HoldInCover:
                                    myTrigger = EFTMath.RandomBool() ? EPhraseTrigger.Negative : EPhraseTrigger.Covering;
                                    break;
                                case SoloDecision.RushEnemy:
                                    myTrigger = EPhraseTrigger.OnFight;
                                    break;

                                default:
                                    break;
                            }
                            break;

                        default:
                            break;
                    }

                    member.Talk.TalkAfterDelay(myTrigger, mask, delay * UnityEngine.Random.Range(0.75f, 1.25f));
                }
            }

            if (memberTalked && EFTMath.RandomBool(5))
            {
                //SAIN.Squad.LeaderComponent?.Talk.TalkAfterDelay(EPhraseTrigger.Silence, ETagStatus.Aware, 1.25f);
            }
        }

        private bool UpdateLeaderCommand()
        {
            if (LeaderComponent == null)
            {
                return false;
            }
            if (!BotSquad.IAmLeader)
            {
                return false;
            }
            if (_leadTime >= Time.time)
            {
                return false;
            }

            _leadTime = Time.time + Randomized * Bot.Info.FileSettings.Mind.SquadLeadTalkFreq;

            if (CheckIfLeaderShouldCommand())
            {
                return true;
            }

            if (CheckFriendliesTimer < Time.time &&
                CheckFriendlyLocation(out var trigger) &&
                Bot.Talk.Say(trigger))
            {
                CheckFriendliesTimer = Time.time + Bot.Info.FileSettings.Mind.SquadLeadTalkFreq * 5f;
                var mask = EFTMath.RandomBool() ? ETagStatus.Aware : ETagStatus.Unaware;
                allMembersSay(EPhraseTrigger.Roger, mask, trigger, Random.Range(0.65f, 1.25f), 50f);
                return true;
            }
            return false;
        }

        private bool TalkHurt()
        {
            if (HurtTalkTimer < Time.time)
            {
                var trigger = EPhraseTrigger.PhraseNone;
                HurtTalkTimer = Time.time + Bot.Info.FileSettings.Mind.SquadMemberTalkFreq * 5f * Random.Range(0.66f, 1.33f);

                if (Bot.HasEnemy && Bot.Enemy.RealDistance < 35f)
                {
                    return false;
                }

                var health = Bot.Memory.Health.HealthStatus;
                switch (health)
                {
                    case ETagStatus.Injured:
                        if (EFTMath.RandomBool(60))
                        {
                            trigger = EFTMath.RandomBool() ? EPhraseTrigger.Hit : EPhraseTrigger.HurtLight;
                        }
                        break;

                    case ETagStatus.BadlyInjured:
                        if (EFTMath.RandomBool(75))
                        {
                            trigger = EFTMath.RandomBool() ? EPhraseTrigger.HurtLight : EPhraseTrigger.HurtHeavy;
                        }
                        break;

                    case ETagStatus.Dying:
                        if (EFTMath.RandomBool(75))
                        {
                            trigger = EPhraseTrigger.HurtNearDeath;
                        }
                        break;

                    default:
                        trigger = EPhraseTrigger.PhraseNone;
                        break;
                }

                if (trigger != EPhraseTrigger.PhraseNone)
                {
                    return Bot.Talk.Say(trigger);
                }
            }
            return false;
        }

        private bool ShallTalkRetreat()
        {
            if (_nextCheckTalkRetreatTime < Time.time
                && EFTMath.RandomBool(_talkRetreatChance)
                && Bot.HasEnemy
                && (Bot.Enemy.IsVisible == true || Bot.Enemy.InLineOfSight)
                && SAINDecisionClass.RETREAT_DECISIONS.Contains(Bot.Decision.CurrentSoloDecision))
            {
                _nextCheckTalkRetreatTime = Time.time + _talkRetreatFreq;
                return Bot.Talk.Say(_talkRetreatTrigger, _talkRetreatMask, _talkRetreatGroupDelay);
            }
            return false;
        }

        private bool ShallReportNeedHelp()
        {
            if (_underFireNeedHelpTime < Time.time
                && EFTMath.RandomBool(_underFireNeedHelpChance)
                && Bot.Enemy != null
                && BotOwner.Memory.IsUnderFire
                && Bot.Memory.LastUnderFireSource == Bot.Enemy.EnemyIPlayer)
            {
                _underFireNeedHelpTime = Time.time + _underFireNeedHelpFreq;
                return Bot.Talk.Say(_underFireNeedHelpTrigger, _underFireNeedHelpMask, _underFireNeedHelpGroupDelay);
            }
            return false;
        }

        private void enemyHeard(EnemyPlace place, Enemy enemy, SAINSoundType soundType)
        {
            float time = Time.time;
            if (!Bot.BotActive || 
                _hearNoiseTime > time || 
                !EFTMath.RandomBool(_hearNoiseChance))
            {
                return;
            }

            if (Bot.HasEnemy && Bot.Enemy.TimeSinceSeen < 120f)
            {
                return;
            }

            if (!Bot.Talk.GroupTalk.FriendIsClose)
            {
                return;
            }

            _hearNoiseTime = time + _hearNoiseFreq;

            if (place == null || soundType.IsGunShot())
            {
                return;
            }
            if (enemy.RealDistance > _hearNoiseMaxDist)
            {
                return;
            }

            EPhraseTrigger trigger = soundType == SAINSoundType.Conversation ? EPhraseTrigger.OnEnemyConversation : EPhraseTrigger.NoisePhrase;
            Bot.Talk.TalkAfterDelay(trigger, ETagStatus.Aware, 0.33f);
        }

        private bool TalkBotDecision(out EPhraseTrigger trigger, out ETagStatus mask)
        {
            mask = ETagStatus.Combat;
            switch (Bot.Decision.CurrentSelfDecision)
            {
                case SelfDecision.FirstAid:
                case SelfDecision.Stims:
                case SelfDecision.Surgery:
                    trigger = EPhraseTrigger.CoverMe;
                    break;

                default:
                    trigger = EPhraseTrigger.PhraseNone;
                    break;
            }

            return trigger != EPhraseTrigger.PhraseNone;
        }

        public bool CheckIfLeaderShouldCommand()
        {
            if (CommandSayTimer < Time.time)
            {
                var mySquadDecision = Bot.Decision.CurrentSquadDecision;
                var myCurrentDecision = Bot.Decision.CurrentSoloDecision;

                CommandSayTimer = Time.time + Bot.Info.FileSettings.Mind.SquadLeadTalkFreq;
                var commandTrigger = EPhraseTrigger.PhraseNone;
                var trigger = EPhraseTrigger.PhraseNone;
                var gesture = EGesture.None;

                if (Bot.Squad.SquadInfo?.MemberHasDecision(SoloDecision.RushEnemy) == true)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.Gogogo;
                    trigger = EPhraseTrigger.OnFight;
                }
                if (Bot.Squad.SquadInfo?.MemberHasDecision(SquadDecision.Suppress) == true)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.Suppress;
                    trigger = EPhraseTrigger.Covering;
                }
                else if (mySquadDecision == SquadDecision.Search)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.FollowMe;
                    trigger = EPhraseTrigger.Going;
                }
                else if (Bot.Squad.MemberIsFallingBack)
                {
                    gesture = EGesture.ComeToMe;
                    commandTrigger = EFTMath.RandomBool() ? EPhraseTrigger.GetInCover : EPhraseTrigger.GetBack;
                    trigger = EPhraseTrigger.PhraseNone;
                }
                else if (BotOwner.DoorOpener.Interacting && EFTMath.RandomBool(33f))
                {
                    commandTrigger = EPhraseTrigger.OpenDoor;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (Bot.Squad.SquadInfo?.MemberIsRegrouping == true)
                {
                    gesture = EGesture.ComeToMe;
                    commandTrigger = EPhraseTrigger.Regroup;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (mySquadDecision == SquadDecision.Help)
                {
                    gesture = EGesture.ThatDirection;
                    commandTrigger = EPhraseTrigger.Gogogo;
                    trigger = EPhraseTrigger.Going;
                }
                else if (myCurrentDecision == SoloDecision.HoldInCover)
                {
                    gesture = EGesture.Stop;
                    commandTrigger = EPhraseTrigger.HoldPosition;
                    trigger = EPhraseTrigger.Roger;
                }
                else if (myCurrentDecision == SoloDecision.Retreat)
                {
                    commandTrigger = EPhraseTrigger.OnYourOwn;
                    trigger = EFTMath.RandomBool() ? EPhraseTrigger.Repeat : EPhraseTrigger.Stop;
                }

                if (commandTrigger != EPhraseTrigger.PhraseNone)
                {
                    if (gesture != EGesture.None && Bot.Squad.VisibleMembers.Count > 0 && Bot.Enemy?.IsVisible == false)
                    {
                        Player.HandsController.ShowGesture(gesture);
                    }
                    if (Bot.Squad.VisibleMembers.Count / (float)Bot.Squad.Members.Count < 0.5f)
                    {
                        Bot.Talk.Say(commandTrigger);
                        allMembersSay(trigger, ETagStatus.Aware, commandTrigger, Random.Range(0.75f, 1.5f), 35f);
                    }
                    return true;
                }
            }

            return false;
        }

        public bool TalkEnemyLocation()
        {
            if (EnemyPosTimer < Time.time && Bot.Enemy != null)
            {
                EnemyPosTimer = Time.time + _enemyLocationTalkFreq;
                var trigger = EPhraseTrigger.PhraseNone;
                var mask = ETagStatus.Aware;

                var enemy = Bot.Enemy;

                if (Bot.Enemy.IsVisible
                    && enemy.EnemyLookingAtMe
                    && EFTMath.RandomBool(_enemyNeedHelpChance))
                {
                    mask = ETagStatus.Combat;
                    bool injured = !Bot.Memory.Health.Healthy && !Bot.Memory.Health.Injured;
                    trigger = injured ? EPhraseTrigger.NeedHelp : EPhraseTrigger.OnRepeatedContact;
                }
                else if ((enemy.IsVisible || (enemy.Seen && enemy.TimeSinceSeen < _enemyLocationTalkTimeSinceSeen))
                    && EFTMath.RandomBool(_enemyLocationTalkChance))
                {
                    EnemyDirectionCheck(enemy.EnemyPosition, out trigger, out mask);
                }

                if (trigger != EPhraseTrigger.PhraseNone)
                {
                    return Bot.Talk.Say(trigger, mask, true);
                }
            }

            return false;
        }

        private bool EnemyDirectionCheck(Vector3 enemyPosition, out EPhraseTrigger trigger, out ETagStatus mask)
        {
            // Check Behind
            if (IsEnemyInDirection(enemyPosition, 180f, AngleToDot(_enemyLocationBehindAngle)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.OnSix;
                return true;
            }

            // Check Left Flank
            if (IsEnemyInDirection(enemyPosition, -90f, AngleToDot(_enemyLocationSideAngle)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.LeftFlank;
                return true;
            }

            // Check Right Flank
            if (IsEnemyInDirection(enemyPosition, 90f, AngleToDot(_enemyLocationSideAngle)))
            {
                mask = ETagStatus.Aware;
                trigger = EPhraseTrigger.RightFlank;
                return true;
            }

            // Check Front
            if (IsEnemyInDirection(enemyPosition, 0f, AngleToDot(_enemyLocationFrontAngle)))
            {
                mask = ETagStatus.Combat;
                trigger = EPhraseTrigger.InTheFront;
                return true;
            }

            trigger = EPhraseTrigger.PhraseNone;
            mask = ETagStatus.Unaware;
            return false;
        }

        private float AngleToRadians(float angle)
        {
            return (angle * (Mathf.PI)) / 180;
        }

        private float AngleToDot(float angle)
        {
            return Mathf.Cos(AngleToRadians(angle));
        }

        private bool CheckFriendlyLocation(out EPhraseTrigger trigger)
        {
            trigger = EPhraseTrigger.PhraseNone;
            if (Bot.Squad.SquadInfo?.MemberIsRegrouping == true)
            {
                trigger = EPhraseTrigger.Regroup;
                return true;
            }
            return false;
        }

        private bool IsEnemyInDirection(Vector3 enemyPosition, float angle, float threshold)
        {
            Vector3 enemyDirectionFromBot = enemyPosition - BotOwner.Transform.position;

            Vector3 enemyDirectionNormalized = enemyDirectionFromBot.normalized;
            Vector3 botLookDirectionNormalized = Player.MovementContext.PlayerRealForward.normalized;

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * botLookDirectionNormalized;

            return Vector3.Dot(enemyDirectionNormalized, direction) > threshold;
        }

        public void updateConfigSettings(SAINPresetClass preset)
        {
            var squadTalk = SAINPlugin.LoadedPreset?.GlobalSettings?.SquadTalk;
            if (squadTalk != null)
            {
                _reportReloadingChance = squadTalk._reportReloadingChance;
                _reportReloadingFreq = squadTalk._reportReloadingFreq;
                _reportLostVisualChance = squadTalk._reportLostVisualChance;
                _reportRatChance = squadTalk._reportRatChance;
                _reportRatTimeSinceSeen = squadTalk._reportRatTimeSinceSeen;
                _reportEnemyConversationChance = squadTalk._reportEnemyConversationChance;
                _reportEnemyMaxDist = squadTalk._reportEnemyMaxDist;
                _reportEnemyHealthChance = squadTalk._reportEnemyHealthChance;
                _reportEnemyHealthFreq = squadTalk._reportEnemyHealthFreq;
                _reportEnemyKilledChance = squadTalk._reportEnemyKilledChance;
                _reportEnemyKilledSquadLeadChance = squadTalk._reportEnemyKilledSquadLeadChance;
                _reportEnemyKilledToxicSquadLeader = squadTalk._reportEnemyKilledToxicSquadLeader;
                _friendCloseDist = squadTalk._friendCloseDist;
                _reportFriendKilledChance = squadTalk._reportFriendKilledChance;
                _talkRetreatChance = squadTalk._talkRetreatChance;
                _talkRetreatFreq = squadTalk._talkRetreatFreq;
                _talkRetreatTrigger = squadTalk._talkRetreatTrigger;
                _talkRetreatMask = squadTalk._talkRetreatMask;
                _talkRetreatGroupDelay = squadTalk._talkRetreatGroupDelay;
                _underFireNeedHelpChance = squadTalk._underFireNeedHelpChance;
                _underFireNeedHelpTrigger = squadTalk._underFireNeedHelpTrigger;
                _underFireNeedHelpMask = squadTalk._underFireNeedHelpMask;
                _underFireNeedHelpGroupDelay = squadTalk._underFireNeedHelpGroupDelay;
                _underFireNeedHelpFreq = squadTalk._underFireNeedHelpFreq;
                _hearNoiseChance = squadTalk._hearNoiseChance;
                _hearNoiseMaxDist = squadTalk._hearNoiseMaxDist;
                _hearNoiseFreq = squadTalk._hearNoiseFreq;
                _enemyLocationTalkChance = squadTalk._enemyLocationTalkChance;
                _enemyLocationTalkTimeSinceSeen = squadTalk._enemyLocationTalkTimeSinceSeen;
                _enemyNeedHelpChance = squadTalk._enemyNeedHelpChance;
                _enemyLocationTalkFreq = squadTalk._enemyLocationTalkFreq;
                _enemyLocationBehindAngle = squadTalk._enemyLocationBehindAngle;
                _enemyLocationSideAngle = squadTalk._enemyLocationSideAngle;
                _enemyLocationFrontAngle = squadTalk._enemyLocationFrontAngle;
            }
        }

        public SAINBotTalkClass LeaderComponent => Bot.Squad.LeaderComponent?.Talk;
        private float Randomized => Random.Range(0.75f, 1.25f);
        private SAINSquadClass BotSquad => Bot.Squad;

        private float _groupTalkFreq = 0.5f;
        private readonly List<EPhraseTrigger> LootPhrases = new List<EPhraseTrigger> { EPhraseTrigger.LootBody, EPhraseTrigger.LootGeneric, EPhraseTrigger.OnLoot, EPhraseTrigger.CheckHim };
        private readonly List<EPhraseTrigger> reloadPhrases = new List<EPhraseTrigger> { EPhraseTrigger.OnWeaponReload, EPhraseTrigger.NeedAmmo, EPhraseTrigger.OnOutOfAmmo };
        private float _nextReportReloadTime;
        private float _nextCheckEnemyHPTime;
        private bool _friendIsClose;
        private float _nextCheckFriendsTime;
        private float CheckFriendliesTimer = 0f;
        private float EnemyPosTimer = 0f;
        private float _nextCheckTalkRetreatTime;
        private float _underFireNeedHelpTime;
        private float _hearNoiseTime;
        private float CommandSayTimer = 0f;
        private float _leadTime = 0f;
        private float TalkTimer = 0f;
        private float HurtTalkTimer = 0f;
        private bool Subscribed = false;

        private float _reportReloadingChance = 33f;
        private float _reportReloadingFreq = 1f;
        private float _reportLostVisualChance = 40f;
        private float _reportRatChance = 33f;
        private float _reportRatTimeSinceSeen = 60f;
        private float _reportEnemyConversationChance = 10f;
        private float _reportEnemyMaxDist = 70f;
        private float _reportEnemyHealthChance = 40f;
        private float _reportEnemyHealthFreq = 8f;
        private float _reportEnemyKilledChance = 60f;
        private float _reportEnemyKilledSquadLeadChance = 60f;
        private bool _reportEnemyKilledToxicSquadLeader = false;
        private float _friendCloseDist = 40f;
        private float _reportFriendKilledChance = 60f;
        private float _talkRetreatChance = 60f;
        private float _talkRetreatFreq = 10f;
        private EPhraseTrigger _talkRetreatTrigger = EPhraseTrigger.CoverMe;
        private ETagStatus _talkRetreatMask = ETagStatus.Combat;
        private bool _talkRetreatGroupDelay = true;
        private float _underFireNeedHelpChance = 45f;
        private EPhraseTrigger _underFireNeedHelpTrigger = EPhraseTrigger.NeedHelp;
        private ETagStatus _underFireNeedHelpMask = ETagStatus.Combat;
        private bool _underFireNeedHelpGroupDelay = true;
        private float _underFireNeedHelpFreq = 1f;
        private float _hearNoiseChance = 40f;
        private float _hearNoiseMaxDist = 60f;
        private float _hearNoiseFreq = 1f;
        private float _enemyLocationTalkChance = 60f;
        private float _enemyLocationTalkTimeSinceSeen = 3f;
        private float _enemyNeedHelpChance = 40f;
        private float _enemyLocationTalkFreq = 1f;
        private float _enemyLocationBehindAngle = 90f;
        private float _enemyLocationSideAngle = 45f;
        private float _enemyLocationFrontAngle = 90f;
    }
}