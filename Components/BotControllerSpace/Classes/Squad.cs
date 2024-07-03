using EFT;
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
using static UnityEngine.EventSystems.EventTrigger;

namespace SAIN.BotController.Classes
{
    public class Squad
    {
        public readonly Dictionary<ESquadRole, BotComponent> Roles
            = new Dictionary<ESquadRole, BotComponent>();

        public Squad()
        {
            CheckSquadTimer = Time.time + 10f;
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings(SAINPresetClass.Instance);
        }

        private void updateSettings(SAINPresetClass preset)
        {
            maxReportActionRangeSqr = preset.GlobalSettings.Hearing.MaxRangeToReportEnemyActionNoHeadset.Sqr();
        }

        private float maxReportActionRangeSqr;

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

        public List<PlaceForCheck> GroupPlacesForCheck => EFTBotGroup?.PlacesForCheck;

        public enum ESearchPointType
        {
            Hearing,
            Flashlight,
        }

        public Action<EnemyPlace, Enemy, SAINSoundType> OnEnemyHeard { get; set; }

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
            if (EFTBotGroup == null)
            {
                EFTBotGroup = bot.BotOwner.BotsGroup;
            }

            AISoundType baseSoundType = soundType.Convert();
            bool isDanger = baseSoundType == AISoundType.step ? false : true;
            PlaceForCheckType checkType = isDanger ? PlaceForCheckType.danger : PlaceForCheckType.simple;
            PlaceForCheck newPlace = AddNewPlaceForCheck(bot.BotOwner, position, checkType, enemy.EnemyIPlayer);
            Vector3 pos = newPlace?.Position ?? position;
            EnemyPlace place = enemy.Hearing.SetHeard(pos, soundType, true);
            if (heard)
                OnEnemyHeard?.Invoke(place, enemy, soundType);
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

        public readonly Dictionary<string, PlaceForCheck> PlayerPlaceChecks = new Dictionary<string, PlaceForCheck>();

        private PlaceForCheck AddNewPlaceForCheck(BotOwner botOwner, Vector3 position, PlaceForCheckType checkType, IPlayer player)
        {
            const float navSampleDist = 10f;
            const float dontLerpDist = 50f;

            if (FindNavMesh(position, out Vector3 hitPosition, navSampleDist))
            {
                // Too many places were being sent to a bot, causing confused behavior.
                // This way I'm tying 1 placeforcheck to each player and updating it based on new info.
                if (PlayerPlaceChecks.TryGetValue(player.ProfileId, out PlaceForCheck oldPlace))
                {
                    if (oldPlace != null
                        && (oldPlace.BasePoint - position).sqrMagnitude <= dontLerpDist * dontLerpDist)
                    {
                        Vector3 averagePosition = averagePosition = Vector3.Lerp(oldPlace.BasePoint, hitPosition, 0.5f);

                        if (FindNavMesh(averagePosition, out hitPosition, navSampleDist)
                            && CanPathToPoint(hitPosition, botOwner) != NavMeshPathStatus.PathInvalid)
                        {
                            GroupPlacesForCheck.Remove(oldPlace);
                            PlaceForCheck replacementPlace = new PlaceForCheck(hitPosition, checkType);
                            GroupPlacesForCheck.Add(replacementPlace);
                            PlayerPlaceChecks[player.ProfileId] = replacementPlace;
                            CalcGoalForBot(botOwner);
                            return replacementPlace;
                        }
                    }
                }

                if (CanPathToPoint(hitPosition, botOwner) != NavMeshPathStatus.PathInvalid)
                {
                    PlaceForCheck newPlace = new PlaceForCheck(position, checkType);
                    GroupPlacesForCheck.Add(newPlace);
                    AddOrUpdatePlaceForPlayer(newPlace, player);
                    CalcGoalForBot(botOwner);
                    return newPlace;
                }
            }
            return null;
        }

        private bool FindNavMesh(Vector3 position, out Vector3 hitPosition, float navSampleDist = 5f)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, navSampleDist, -1))
            {
                hitPosition = hit.position;
                return true;
            }
            hitPosition = Vector3.zero;
            return false;
        }

        private NavMeshPathStatus CanPathToPoint(Vector3 point, BotOwner botOwner)
        {
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(botOwner.Position, point, -1, path);
            return path.status;
        }

        private void CalcGoalForBot(BotOwner botOwner)
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
                            EFTBotGroup?.CalcGoalForBot(bot.BotOwner);
                        }
                        catch
                        {
                            // Was throwing error with Project fika, causing players to not be able to extract
                        }
                    }
                }
            }
        }

        private BotsGroup EFTBotGroup;

        public string Id { get; private set; } = string.Empty;

        public readonly string GUID = Guid.NewGuid().ToString("N");

        public bool SquadReady { get; private set; }

        public Action<IPlayer, DamageInfo, float> LeaderKilled { get; set; }

        public Action<IPlayer, DamageInfo, float> MemberKilled { get; set; }

        public Action<BotComponent, float> NewLeaderFound { get; set; }

        public bool LeaderIsDeadorNull => LeaderComponent?.Player == null || LeaderComponent?.Player?.HealthController.IsAlive == false;

        public float TimeThatLeaderDied { get; private set; }

        public const float FindLeaderAfterKilledCooldown = 60f;

        public BotComponent LeaderComponent { get; private set; }
        public string LeaderId { get; private set; }

        public float LeaderPowerLevel { get; private set; }

        public bool MemberIsFallingBack
        {
            get
            {
                return MemberHasDecision(SoloDecision.Retreat, SoloDecision.RunAway, SoloDecision.RunToCover);
            }
        }

        public bool MemberIsRegrouping
        {
            get
            {
                return MemberHasDecision(SquadDecision.Regroup);
            }
        }

        public bool MemberHasDecision(params SoloDecision[] decisionsToCheck)
        {
            foreach (var member in MemberInfos.Values)
            {
                if (member != null && member.SAIN != null)
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
                if (member != null && member.SAIN != null)
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
                if (member != null && member.SAIN != null)
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

        public float SquadPowerLevel
        {
            get
            {
                float result = 0f;
                foreach (var memberInfo in MemberInfos.Values)
                {
                    if (memberInfo.SAIN != null && memberInfo.SAIN.IsDead == false)
                    {
                        result += memberInfo.PowerLevel;
                    }
                }
                return result;
            }
        }

        public ESquadPersonality SquadPersonality { get; private set; }
        public SquadPersonalitySettings SquadPersonalitySettings { get; private set; }

        private void GetSquadPersonality()
        {
            SquadPersonality = SquadPersonalityManager.GetSquadPersonality(Members, out var settings);
            SquadPersonalitySettings = settings;
        }

        public void Update()
        {
            // After 10 seconds since squad is originally created,
            // find a squad leader and activate the squad to give time for all bots to spawn in
            // since it can be staggered over a few seconds.
            if (!SquadReady && CheckSquadTimer < Time.time && Members.Count > 0)
            {
                SquadReady = true;
                FindSquadLeader();
                // Timer before starting to recheck
                RecheckSquadTimer = Time.time + 10f;
                if (Members.Count > 1)
                {
                    GetSquadPersonality();
                }
            }

            // Check happens once the squad is originally "activated" and created
            // Wait until all members are out of combat to find a squad leader, or 60 seconds have passed to find a new squad leader is they are KIA
            if (SquadReady)
            {
                if (RecheckSquadTimer < Time.time && LeaderIsDeadorNull)
                {
                    RecheckSquadTimer = Time.time + 3f;

                    if (TimeThatLeaderDied < Time.time + FindLeaderAfterKilledCooldown)
                    {
                        FindSquadLeader();
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
                            FindSquadLeader();
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
                if ((a.Position - b.Position).sqrMagnitude <= maxReportActionRangeSqr)
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

        private float RecheckSquadTimer;
        private float CheckSquadTimer;

        private void MemberWasKilled(Player player, IPlayer lastAggressor, DamageInfo lastDamageInfo, EBodyPart lastBodyPart)
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

            MemberKilled?.Invoke(lastAggressor, lastDamageInfo, Time.time);

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

        private void FindSquadLeader()
        {
            float power = 0f;
            BotComponent leadComponent = null;

            // Iterate through each memberInfo memberInfo in friendly group to see who has the highest power level or if any are bosses
            foreach (var memberInfo in MemberInfos.Values)
            {
                if (memberInfo.SAIN == null || memberInfo.SAIN.IsDead) continue;

                // If this memberInfo is a boss type, they are the squad leader
                bool isBoss = memberInfo.SAIN.Info.Profile.IsBoss;
                // or If this memberInfo has a higher power level than the last one we checked, they are the squad leader
                if (isBoss || memberInfo.PowerLevel > power)
                {
                    power = memberInfo.PowerLevel;
                    leadComponent = memberInfo.SAIN;

                    if (isBoss)
                    {
                        break;
                    }
                }
            }

            if (leadComponent != null)
            {
                AssignSquadLeader(leadComponent);
            }
        }

        private void AssignSquadLeader(BotComponent sain)
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

        public void AddMember(BotComponent sain)
        {
            // Make sure nothing is null as a safety check.
            if (sain?.Player != null && sain.BotOwner != null)
            {
                // Make sure this profile ID doesn't already exist for whatever reason
                if (!Members.ContainsKey(sain.ProfileId))
                {
                    // If this is the first member, add their side to the start of their ID for easier identifcation during debug
                    if (Members.Count == 0)
                    {
                        EFTBotGroup = sain.BotOwner.BotsGroup;
                        Id = sain.Player.Profile.Side.ToString() + "_" + GUID;
                    }

                    var memberInfo = new MemberInfo(sain);
                    MemberInfos.Add(sain.ProfileId, memberInfo);
                    Members.Add(sain.ProfileId, sain);

                    // if this new member is a boss, set them to leader automatically
                    if (sain.Info.Profile.IsBoss)
                    {
                        AssignSquadLeader(sain);
                    }
                    // If this new memberInfo has a higher power level than the existing squad leader, set them as the new squad leader if they aren't a boss type
                    else if (LeaderComponent != null && sain.Info.Profile.PowerLevel > LeaderPowerLevel && !LeaderComponent.Info.Profile.IsBoss)
                    {
                        AssignSquadLeader(sain);
                    }

                    // Subscribe when this member is killed
                    sain.Player.OnPlayerDead += MemberWasKilled;
                    if (Members.Count > 1)
                    {
                        GetSquadPersonality();
                    }
                }
            }
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
                Player player = memberInfo.SAIN?.Player;
                if (player != null)
                {
                    player.OnPlayerDead -= MemberWasKilled;
                }
                memberInfo.Dispose();
                MemberInfos.Remove(id);
            }
            if (Members.Count == 0)
            {
                OnSquadEmpty?.Invoke(this);
            }
        }

        public Action<Squad> OnSquadEmpty { get; set; }

        public readonly Dictionary<string, BotComponent> Members = new Dictionary<string, BotComponent>();
        public readonly Dictionary<string, MemberInfo> MemberInfos = new Dictionary<string, MemberInfo>();
    }
}