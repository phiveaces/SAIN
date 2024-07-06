﻿using EFT;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.BotController.Classes
{
    public class Squad
    {
        public event Action<CombatDecision, SquadDecision, SelfDecision, BotComponent> OnMemberDecisionMade;
        public event Action<EnemyPlace, Enemy, SAINSoundType> OnMemberHeardEnemy;
        public event Action<Squad> OnSquadEmpty;
        public event Action<IPlayer, DamageInfo, float> LeaderKilled;
        public event Action<IPlayer, DamageInfo, float> OnMemberKilled;
        public event Action<BotComponent, float> NewLeaderFound;

        public Dictionary<string, BotComponent> Members { get; } = new Dictionary<string, BotComponent>();
        public Dictionary<string, MemberInfo> MemberInfos { get; } = new Dictionary<string, MemberInfo>();
        public string Id { get; private set; } = string.Empty;
        public string GUID { get; } = Guid.NewGuid().ToString("N");
        public bool SquadReady { get; private set; }
        public ESquadPersonality SquadPersonality { get; private set; }
        public SquadPersonalitySettings SquadPersonalitySettings { get; private set; }
        public BotComponent LeaderComponent { get; private set; }
        public string LeaderId { get; private set; }
        public float LeaderPowerLevel { get; private set; }
        public bool LeaderIsDeadorNull => LeaderComponent?.Player == null || LeaderComponent?.Player?.HealthController.IsAlive == false;
        public float TimeThatLeaderDied { get; private set; }
        public List<PlaceForCheck> GroupPlacesForCheck => _botsGroup?.PlacesForCheck;
        public Dictionary<ESquadRole, BotComponent> Roles { get; } = new Dictionary<ESquadRole, BotComponent>();
        public Dictionary<string, PlaceForCheck> PlayerPlaceChecks { get; } = new Dictionary<string, PlaceForCheck>();

        public bool MemberIsFallingBack
        {
            get
            {
                return MemberHasDecision(CombatDecision.Retreat, CombatDecision.RunAway, CombatDecision.RunToCover);
            }
        }

        public bool MemberIsRegrouping
        {
            get
            {
                return MemberHasDecision(SquadDecision.Regroup);
            }
        }

        public float SquadPowerLevel
        {
            get
            {
                float result = 0f;
                foreach (var memberInfo in MemberInfos.Values)
                {
                    if (memberInfo.Bot != null && memberInfo.Bot.IsDead == false)
                    {
                        result += memberInfo.PowerLevel;
                    }
                }
                return result;
            }
        }

        public Squad()
        {
            _checkSquadTime = Time.time + 10f;
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings(SAINPresetClass.Instance);
        }

        private void updateSettings(SAINPresetClass preset)
        {
            _maxReportActionRangeSqr = preset.GlobalSettings.Hearing.MaxRangeToReportEnemyActionNoHeadset.Sqr();
        }

        public void ReportEnemyPosition(Enemy reportedEnemy, EnemyPlace place, bool seen)
        {
            if (Members == null || Members.Count <= 1)
            {
                return;
            }

            float squadCoordination = 3f;
            if (SquadPersonalitySettings != null)
            {
                squadCoordination = SquadPersonalitySettings.CoordinationLevel;
                squadCoordination = Mathf.Clamp(squadCoordination, 1f, 5f);
            }
            float baseChance = 25f;
            float finalChance = baseChance + (squadCoordination * 15f);

            foreach (var member in Members.Values)
            {
                if (EFTMath.RandomBool(finalChance))
                {
                    if (member?.Player != null
                        && reportedEnemy.Player != null
                        && reportedEnemy.EnemyPlayer != null
                        && reportedEnemy.Player.ProfileId != member.ProfileId)
                    {
                        member.EnemyController.GetEnemy(reportedEnemy.EnemyPlayer.ProfileId, true)?.EnemyPositionReported(place, seen);
                    }
                }
            }
        }

        public string GetId()
        {
            if (Id.IsNullOrEmpty())
            {
                return GUID;
            }
            else
            {
                return Id;
            }
        }

        public bool SquadIsSuppressEnemy(string profileId, out BotComponent suppressingMember)
        {
            foreach (var member in Members)
            {
                Enemy enemy = member.Value?.Enemy;
                if (enemy?.EnemyPlayer != null
                    && enemy.EnemyPlayer.ProfileId == profileId
                    && enemy.Status.EnemyIsSuppressed)
                {
                    suppressingMember = member.Value;
                    return true;
                }
            }
            suppressingMember = null;
            return false;
        }

        public enum ESearchPointType
        {
            Hearing,
            Flashlight,
        }

        public void AddPointToSearch(BotSoundStruct sound, BotComponent sain)
        {
            Enemy enemy = sound.Info.Enemy;
            if (enemy == null)
            {
                Logger.LogError($"Could not find enemy!");
                return;
            }

            enemy.Hearing.LastSoundHeard = sound;
            addPlaceForCheck(sound.Results.EstimatedPosition, sound.Info.SoundType, sain, enemy, true);
        }

        private void addPlaceForCheck(Vector3 position, SAINSoundType soundType, BotComponent bot, Enemy enemy, bool heard)
        {
            if (_botsGroup == null)
            {
                _botsGroup = bot.BotOwner.BotsGroup;
            }

            position.y = enemy.EnemyPosition.y;

            AISoundType baseSoundType = soundType.Convert();
            bool isDanger = baseSoundType == AISoundType.step ? false : true;
            PlaceForCheckType checkType = isDanger ? PlaceForCheckType.danger : PlaceForCheckType.simple;
            PlaceForCheck newPlace = addNewPlaceForCheck(bot.BotOwner, position, checkType, enemy.EnemyIPlayer);
            Vector3 pos = newPlace?.Position ?? position;
            EnemyPlace place = enemy.Hearing.SetHeard(pos, soundType, true);
            if (heard)
                OnMemberHeardEnemy?.Invoke(place, enemy, soundType);
        }

        public void AddPointToSearch(Vector3 position, float soundPower, BotComponent sain, AISoundType soundType, IPlayer player, ESearchPointType searchType = ESearchPointType.Hearing)
        {
            Enemy enemy = sain.EnemyController.CheckAddEnemy(player);
            if (enemy == null)
            {
                Logger.LogError($"Could not find enemy!");
                return;
            }

            addPlaceForCheck(position, soundType.Convert(), sain, enemy, false);
        }

        private PlaceForCheck addNewPlaceForCheck(BotOwner botOwner, Vector3 position, PlaceForCheckType checkType, IPlayer player)
        {
            const float navSampleDist = 10f;
            const float dontLerpDist = 50f;

            if (findNavMesh(position, out Vector3 hitPosition, navSampleDist))
            {
                // Too many places were being sent to a bot, causing confused behavior.
                // This way I'm tying 1 placeforcheck to each player and updating it based on new info.
                if (PlayerPlaceChecks.TryGetValue(player.ProfileId, out PlaceForCheck oldPlace))
                {
                    if (oldPlace != null
                        && (oldPlace.BasePoint - position).sqrMagnitude <= dontLerpDist * dontLerpDist)
                    {
                        Vector3 averagePosition = averagePosition = Vector3.Lerp(oldPlace.BasePoint, hitPosition, 0.5f);

                        if (findNavMesh(averagePosition, out hitPosition, navSampleDist)
                            && canPathToPoint(hitPosition, botOwner) != NavMeshPathStatus.PathInvalid)
                        {
                            GroupPlacesForCheck.Remove(oldPlace);
                            PlaceForCheck replacementPlace = new PlaceForCheck(hitPosition, checkType);
                            GroupPlacesForCheck.Add(replacementPlace);
                            PlayerPlaceChecks[player.ProfileId] = replacementPlace;
                            calcGoalForBot(botOwner);
                            return replacementPlace;
                        }
                    }
                }

                if (canPathToPoint(hitPosition, botOwner) != NavMeshPathStatus.PathInvalid)
                {
                    PlaceForCheck newPlace = new PlaceForCheck(position, checkType);
                    GroupPlacesForCheck.Add(newPlace);
                    AddOrUpdatePlaceForPlayer(newPlace, player);
                    calcGoalForBot(botOwner);
                    return newPlace;
                }
            }
            return null;
        }

        private bool findNavMesh(Vector3 position, out Vector3 hitPosition, float navSampleDist = 2f)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, navSampleDist, -1))
            {
                hitPosition = hit.position;
                return true;
            }
            hitPosition = Vector3.zero;
            return false;
        }

        private NavMeshPathStatus canPathToPoint(Vector3 point, BotOwner botOwner)
        {
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(botOwner.Position, point, -1, path);
            return path.status;
        }

        private void calcGoalForBot(BotOwner botOwner)
        {
            try
            {
                if (!botOwner.Memory.GoalTarget.HavePlaceTarget() && botOwner.Memory.GoalEnemy == null)
                {
                    botOwner.BotsGroup.CalcGoalForBot(botOwner);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void AddOrUpdatePlaceForPlayer(PlaceForCheck place, IPlayer player)
        {
            string id = player.ProfileId;
            if (PlayerPlaceChecks.ContainsKey(id))
            {
                PlayerPlaceChecks[id] = place;
            }
            else
            {
                player.OnIPlayerDeadOrUnspawn += clearPlayerPlace;
                PlayerPlaceChecks.Add(id, place);
            }
        }

        private void clearPlayerPlace(IPlayer player)
        {
            if (player == null)
            {
                return;
            }

            player.OnIPlayerDeadOrUnspawn -= clearPlayerPlace;
            string id = player.ProfileId;

            if (PlayerPlaceChecks.ContainsKey(id))
            {
                GroupPlacesForCheck.Remove(PlayerPlaceChecks[id]);
                PlayerPlaceChecks.Remove(id);

                foreach (var bot in Members.Values)
                {
                    if (bot != null
                        && bot.BotOwner != null)
                    {
                        try
                        {
                            _botsGroup?.CalcGoalForBot(bot.BotOwner);
                        }
                        catch
                        {
                            // Was throwing error with Project fika, causing players to not be able to extract
                        }
                    }
                }
            }
        }

        public bool MemberHasDecision(params CombatDecision[] decisionsToCheck)
        {
            foreach (var member in MemberInfos.Values)
            {
                if (member != null && member.Bot != null)
                {
                    var memberDecision = member.SoloDecision;
                    foreach (var decision in decisionsToCheck)
                    {
                        if (decision == memberDecision)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool MemberHasDecision(params SquadDecision[] decisionsToCheck)
        {
            foreach (var member in MemberInfos.Values)
            {
                if (member != null && member.Bot != null)
                {
                    var memberDecision = member.SquadDecision;
                    foreach (var decision in decisionsToCheck)
                    {
                        if (decision == memberDecision)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool MemberHasDecision(params SelfDecision[] decisionsToCheck)
        {
            foreach (var member in MemberInfos.Values)
            {
                if (member != null && member.Bot != null)
                {
                    var memberDecision = member.SelfDecision;
                    foreach (var decision in decisionsToCheck)
                    {
                        if (decision == memberDecision)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void getSquadPersonality()
        {
            SquadPersonality = SquadPersonalityManager.GetSquadPersonality(Members, out var settings);
            SquadPersonalitySettings = settings;
        }

        public void Update()
        {
            // After 10 seconds since squad is originally created,
            // find a squad leader and activate the squad to give time for all bots to spawn in
            // since it can be staggered over a few seconds.
            if (!SquadReady && _checkSquadTime < Time.time && Members.Count > 0)
            {
                SquadReady = true;
                findSquadLeader();
                // Timer before starting to recheck
                _recheckSquadTime = Time.time + 10f;
                if (Members.Count > 1)
                {
                    getSquadPersonality();
                }
            }

            // Check happens once the squad is originally "activated" and created
            // Wait until all members are out of combat to find a squad leader, or 60 seconds have passed to find a new squad leader is they are KIA
            if (SquadReady)
            {
                if (_recheckSquadTime < Time.time && LeaderIsDeadorNull)
                {
                    _recheckSquadTime = Time.time + 3f;

                    if (TimeThatLeaderDied < Time.time + LEADER_KILL_COOLDOWN)
                    {
                        findSquadLeader();
                    }
                    else
                    {
                        bool outOfCombat = true;
                        foreach (var member in MemberInfos.Values)
                        {
                            if (member.HasEnemy == true)
                            {
                                outOfCombat = false;
                                break;
                            }
                        }
                        if (outOfCombat)
                        {
                            findSquadLeader();
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (MemberInfos.Count > 0)
            {
                foreach (var id in MemberInfos.Keys)
                {
                    RemoveMember(id);
                }
            }
            PresetHandler.OnPresetUpdated -= updateSettings;
            MemberInfos.Clear();
            Members.Clear();
        }

        private bool isInCommunicationRange(BotComponent a, BotComponent b)
        {
            if (a != null && b != null)
            {
                if (a.PlayerComponent.Equipment.GearInfo.HasEarPiece &&
                    b.PlayerComponent.Equipment.GearInfo.HasEarPiece)
                {
                    return true;
                }
                if ((a.Position - b.Position).sqrMagnitude <= _maxReportActionRangeSqr)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateSharedEnemyStatus(IPlayer player, EEnemyAction action, BotComponent sain, SAINSoundType soundType, Vector3 position)
        {
            if (sain == null)
            {
                return;
            }

            foreach (var member in Members.Values)
            {
                if (member == null || member.ProfileId == sain.ProfileId)
                {
                    continue;
                }
                if (!isInCommunicationRange(sain, member))
                {
                    continue;
                }
                Enemy memberEnemy = member.EnemyController.CheckAddEnemy(player);
                if (memberEnemy == null)
                {
                    continue;
                }

                memberEnemy.Hearing.SetHeard(position, soundType, false);
                if (action != EEnemyAction.None)
                {
                    memberEnemy.Status.SetVulnerableAction(action);
                }
            }
        }

        private void memberWasKilled(Player player, IPlayer lastAggressor, DamageInfo lastDamageInfo, EBodyPart lastBodyPart)
        {
            if (SAINPlugin.DebugMode)
            {
                Logger.LogInfo(
                    $"Member [{player?.Profile.Nickname}] " +
                    $"was killed for Squad: [{Id}] " +
                    $"by [{lastAggressor?.Profile.Nickname}] " +
                    $"at Time: [{Time.time}] " +
                    $"by damage type: [{lastDamageInfo.DamageType}] " +
                    $"to Body part: [{lastBodyPart}]"
                    );
            }

            OnMemberKilled?.Invoke(lastAggressor, lastDamageInfo, Time.time);

            if (MemberInfos.TryGetValue(player?.ProfileId, out var member)
                && member != null)
            {
                // If this killed Member is the squad leader then
                if (member.ProfileId == LeaderId)
                {
                    if (SAINPlugin.DebugMode)
                        Logger.LogInfo($"Leader [{player?.Profile.Nickname}] was killed for Squad: [{Id}]");

                    LeaderKilled?.Invoke(lastAggressor, lastDamageInfo, Time.time);
                    TimeThatLeaderDied = Time.time;
                    LeaderComponent = null;
                }
            }

            RemoveMember(player?.ProfileId);
        }

        public void MemberExtracted(BotComponent sain)
        {
            if (SAINPlugin.DebugMode)
                Logger.LogInfo($"Leader [{sain?.Player?.Profile.Nickname}] Extracted for Squad: [{Id}]");

            RemoveMember(sain?.ProfileId);
        }

        private void findSquadLeader()
        {
            float power = 0f;
            BotComponent leadComponent = null;

            // Iterate through each memberInfo memberInfo in friendly group to see who has the highest power level or if any are bosses
            foreach (var memberInfo in MemberInfos.Values)
            {
                if (memberInfo.Bot == null || memberInfo.Bot.IsDead) continue;

                // If this memberInfo is a boss type, they are the squad leader
                bool isBoss = memberInfo.Bot.Info.Profile.IsBoss;
                // or If this memberInfo has a higher power level than the last one we checked, they are the squad leader
                if (isBoss || memberInfo.PowerLevel > power)
                {
                    power = memberInfo.PowerLevel;
                    leadComponent = memberInfo.Bot;

                    if (isBoss)
                    {
                        break;
                    }
                }
            }

            if (leadComponent != null)
            {
                assignSquadLeader(leadComponent);
            }
        }

        private void assignSquadLeader(BotComponent sain)
        {
            if (sain?.Player == null)
            {
                Logger.LogError($"Tried to Assign Null SAIN Component or Player for Squad [{Id}], skipping");
                return;
            }

            LeaderComponent = sain;
            LeaderPowerLevel = sain.Info.Profile.PowerLevel;
            LeaderId = sain.Player?.ProfileId;

            NewLeaderFound?.Invoke(sain, Time.time);

            if (SAINPlugin.DebugMode)
            {
                Logger.LogInfo(
                    $" Found New Leader. Name [{sain.BotOwner?.Profile?.Nickname}]" +
                    $" for Squad: [{Id}]" +
                    $" at Time: [{Time.time}]" +
                    $" Group Size: [{Members.Count}]"
                    );
            }
        }

        public void AddMember(BotComponent bot)
        {
            // Make sure nothing is null as a safety check.
            if (bot?.Player != null && bot.BotOwner != null)
            {
                // Make sure this profile ID doesn't already exist for whatever reason
                if (!Members.ContainsKey(bot.ProfileId))
                {
                    // If this is the first member, add their side to the start of their ID for easier identifcation during debug
                    if (Members.Count == 0)
                    {
                        _botsGroup = bot.BotOwner.BotsGroup;
                        Id = bot.Player.Profile.Side.ToString() + "_" + GUID;
                    }

                    bot.Decision.OnDecisionMade += memberMadeDecision;

                    var memberInfo = new MemberInfo(bot, this);
                    MemberInfos.Add(bot.ProfileId, memberInfo);
                    Members.Add(bot.ProfileId, bot);

                    // if this new member is a boss, set them to leader automatically
                    if (bot.Info.Profile.IsBoss)
                    {
                        assignSquadLeader(bot);
                    }
                    // If this new memberInfo has a higher power level than the existing squad leader, set them as the new squad leader if they aren't a boss type
                    else if (LeaderComponent != null && bot.Info.Profile.PowerLevel > LeaderPowerLevel && !LeaderComponent.Info.Profile.IsBoss)
                    {
                        assignSquadLeader(bot);
                    }

                    // Subscribe when this member is killed
                    bot.Player.OnPlayerDead += memberWasKilled;
                    if (Members.Count > 1)
                    {
                        getSquadPersonality();
                    }
                }
            }
        }

        private void memberMadeDecision(CombatDecision solo, SquadDecision squad, SelfDecision self, BotComponent member)
        {
            OnMemberDecisionMade?.Invoke(solo, squad, self, member);
        }

        public void RemoveMember(BotComponent sain)
        {
            RemoveMember(sain?.ProfileId);
        }

        public void RemoveMember(string id)
        {
            if (Members.ContainsKey(id))
            {
                Members.Remove(id);
            }
            if (MemberInfos.TryGetValue(id, out var memberInfo))
            {
                Player player = memberInfo.Bot?.Player;
                if (player != null)
                {
                    player.OnPlayerDead -= memberWasKilled;
                }
                memberInfo.Bot.Decision.OnDecisionMade -= memberMadeDecision;
                memberInfo.Dispose();
                MemberInfos.Remove(id);
            }
            if (Members.Count == 0)
            {
                OnSquadEmpty?.Invoke(this);
            }
        }

        private BotsGroup _botsGroup;
        private float _recheckSquadTime;
        private float _checkSquadTime;
        private float _maxReportActionRangeSqr;
        public const float LEADER_KILL_COOLDOWN = 60f;
    }
}