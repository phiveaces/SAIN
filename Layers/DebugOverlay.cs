using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SAIN.Layers
{
    public static class DebugOverlay
    {
        static DebugOverlay()
        {
            TimeToAim = AccessTools.Field(Helpers.HelpersGClass.AimDataType, "float_7");
            timeAiming = AccessTools.Field(Helpers.HelpersGClass.AimDataType, "float_5");

            int count = 0;
            foreach (var name in _overlayNames)
            {
                _overlays.Add(count, name);
                count++;
            }
        }

        private static FieldInfo TimeToAim;
        private static FieldInfo timeAiming;

        private static readonly Dictionary<int, string> _overlays = new Dictionary<int, string>();

        private static readonly string[] _overlayNames =
        {
            "Bot Info",
            "Bot Properties",
            "Current Enemy Info",
            "All Enemies Info",
        };

        private static bool _changedOverlay;

        public static void UpdateSelectedOverlay()
        {
            if (_changedOverlay && 
                SAINPlugin.NextDebugOverlay.Value.IsUp() && 
                SAINPlugin.PreviousDebugOverlay.Value.IsUp())
            {
                _changedOverlay = false;
            }
            if (_changedOverlay)
            {
                return;
            }

            if (SAINPlugin.NextDebugOverlay.Value.IsDown())
            {
                changeOverlay(true);
                _changedOverlay = true;
                return;
            }

            if (SAINPlugin.PreviousDebugOverlay.Value.IsDown())
            {
                changeOverlay(false);
                _changedOverlay = true;
                return;
            }
        }

        private static void changeOverlay(bool next)
        {
            int count = _overlays.Count - 1;
            if (next)
            {
                _selectedOverlay++;
                if (_selectedOverlay > count)
                {
                    _selectedOverlay = 0;
                }
            }
            else
            {
                _selectedOverlay--;
                if (_selectedOverlay < 0)
                {
                    _selectedOverlay = count;
                }
            }
            Logger.LogDebug($"{_selectedOverlay} : {next}");
        }

        private static int _selectedOverlay;

        public static void AddBaseInfo(BotComponent sain, BotOwner botOwner, StringBuilder stringBuilder)
        {
            UpdateSelectedOverlay();

            try
            {
                var debug = SAINPlugin.DebugSettings;

                var info = sain.Info; 
                if (debug.Overlay_Info)
                {
                    stringBuilder.AppendLine($"Name: [{sain.Person.Name}] Nickname: [{sain.Player.Profile.Nickname}] Personality: [{info.Personality}] Type: [{info.Profile.WildSpawnType}] PowerLevel: [{info.Profile.PowerLevel}]");
                    stringBuilder.AppendLabeledValue("Active SAIN Layer", $"{sain.ActiveLayer}", Color.white, Color.yellow);
                    stringBuilder.AppendLabeledValue("Steering", $"{sain.Steering.CurrentSteerPriority}", Color.white, Color.yellow);
                    stringBuilder.AppendLabeledValue("Last EnemySteer", $"{sain.Steering.EnemySteerDir}", Color.white, Color.yellow);
                    stringBuilder.AppendLabeledValue("AI Limited", $"{sain.CurrentAILimit}", Color.white, Color.yellow);
                    stringBuilder.AppendLabeledValue("Closest Human Distance", $"{sain.AILimit.ClosestPlayerDistanceSqr.Sqrt()}", Color.white, Color.yellow);

                    if (debug.Overlay_Info_Expanded)
                    {
                        stringBuilder.AppendLine($"Cover Points Count: [{sain.Cover.CoverPoints.Count}]");
                        stringBuilder.AppendLabeledValue("Start Search + Hold Ground Time", $"{info.TimeBeforeSearch} + {info.HoldGroundDelay}", Color.white, Color.yellow, true);
                        stringBuilder.AppendLine($"Suppression Num: [{sain.Suppression?.SuppressionNumber}] IsSuppressed: [{sain.Suppression?.IsSuppressed}] IsHeavySuppressed: [{sain.Suppression?.IsHeavySuppressed}]");
                        stringBuilder.AppendLine($"Indoors? {sain.Memory.Location.IsIndoors} EnvironmentID: {sain.Player?.AIData.EnvironmentId} In Bunker? {sain.PlayerComponent.AIData.PlayerLocation.InBunker}");
                        var members = sain.Squad.SquadInfo?.Members;
                        if (members != null && members.Count > 1)
                        {
                            stringBuilder.AppendLine($"Squad Personality: [{sain.Squad.SquadInfo.SquadPersonality}]");
                        }
                    }
                    stringBuilder.AppendLine();

                    if (debug.Overlay_Decisions)
                    {
                        stringBuilder.AppendLabeledValue("Main Decision", $"Current: {sain.Decision.CurrentSoloDecision} Last: {sain.Decision.PreviousSoloDecision}", Color.white, Color.yellow, true);
                        stringBuilder.AppendLabeledValue("Squad Decision", $"Current: {sain.Decision.CurrentSquadDecision} Last: {sain.Decision.PreviousSquadDecision}", Color.white, Color.yellow, true);
                        stringBuilder.AppendLabeledValue("Self Decision", $"Current: {sain.Decision.CurrentSelfDecision} Last: {sain.Decision.PreviousSelfDecision}", Color.white, Color.yellow, true);
                        stringBuilder.AppendLine();
                    }
                }
                if (debug.Overlay_EnemyLists)
                {
                    var lists = sain.EnemyController.EnemyLists;
                    stringBuilder.AppendLine($"Known Enemies: [{lists.GetEnemyList(EEnemyListType.Known).Count}] Humans: [{lists.GetEnemyList(EEnemyListType.Known).Humans}]");
                    stringBuilder.AppendLine($"Visible Enemies: [{lists.GetEnemyList(EEnemyListType.Visible).Count}] Humans: [{lists.GetEnemyList(EEnemyListType.Visible).Humans}]");
                    stringBuilder.AppendLine($"InLineOfSight Enemies: [{lists.GetEnemyList(EEnemyListType.InLineOfSight).Count}] Humans: [{lists.GetEnemyList(EEnemyListType.InLineOfSight).Humans}]");
                    stringBuilder.AppendLine($"ActiveThreats: [{lists.GetEnemyList(EEnemyListType.ActiveThreats).Count}] Humans: [{lists.GetEnemyList(EEnemyListType.ActiveThreats).Humans}]");
                    stringBuilder.AppendLine();
                }
                if (debug.OverLay_AimInfo)
                {
                    if (sain.BotOwner.AimingData != null)
                    {
                        stringBuilder.AppendLine($"Aim Status {sain.Aim.AimStatus} Last Aim Time: {sain.LastAimTime}");
                        stringBuilder.AppendLine($"Aim Time {timeAiming.GetValue(sain.BotOwner.AimingData)} TimeToAim: {TimeToAim.GetValue(sain.BotOwner.AimingData)}");
                        stringBuilder.AppendLine($"Aim Offset Magnitude {(sain.BotOwner.AimingData.RealTargetPoint - sain.BotOwner.AimingData.EndTargetPoint).magnitude}");
                        stringBuilder.AppendLine($"Friendly Fire Status {sain.FriendlyFire.FriendlyFireStatus} No Bush ESP Status: {sain.NoBushESP.NoBushESPActive}");
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.AppendLine();
                }
                if (sain.HasEnemy && debug.Overlay_EnemyInfo)
                {
                    CreateEnemyInfo(stringBuilder, sain.Enemy);
                    stringBuilder.AppendLine();
                }
                if (debug.Overlay_Search)
                {
                    var enemyDecisions = sain.Decision.EnemyDecisions;
                    var shallSearch = enemyDecisions.DebugShallSearch;
                    if (shallSearch != null)
                    {
                        if (shallSearch == true)
                            stringBuilder.AppendLabeledValue("Searching",
                                $"Current State: {sain.Search.CurrentState} " +
                                $"Next: {sain.Search.NextState} " +
                                $"Last: {sain.Search.LastState}",
                                Color.white, Color.yellow, true);
                        else
                        {
                            var reasons = enemyDecisions.DebugSearchReasons;
                            var wantReasons = reasons.WantSearchReasons;
                            stringBuilder.AppendLabeledValue("Want Search Reasons",
                                $"[WantToSearchReason : {wantReasons.WantToSearchReason}] " +
                                $"[NotWantToSearchReason: {wantReasons.NotWantToSearchReason}] " +
                                $"[CantStartReason: {wantReasons.CantStartReason}]",
                                Color.white, Color.yellow, true);

                            stringBuilder.AppendLabeledValue("Not Search Reason",
                                $"{reasons.NotSearchReason}",
                                Color.white, Color.yellow, true);

                            stringBuilder.AppendLabeledValue("CalcPath Fail Reason",
                                $"{reasons.PathCalcFailReason}",
                                Color.white, Color.yellow, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private static void CreateEnemyInfo(StringBuilder stringBuilder, Enemy enemy)
        {
            stringBuilder.AppendLine("Enemy Info");

            stringBuilder.AppendLabeledValue("Enemy Name", $"{enemy.EnemyPlayer?.Profile.Nickname}", Color.white, Color.red, true);
            stringBuilder.AppendLabeledValue("Enemy Power Level ", $"{enemy.EnemyIPlayer?.AIData?.PowerOfEquipment}", Color.white, Color.red, true);

            stringBuilder.AppendLabeledValue("Last GainSight Result", $"{enemy.Vision.LastGainSightResult}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("SeenCoef", $"{enemy.EnemyInfo.SeenCoef}", Color.white, Color.yellow, true);
            //stringBuilder.AppendLabeledValue("PercentSpotted", $"{enemy.EnemyInfo.BodyData().Value?.PercentSpotted(out _)}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Seen?", $"{enemy.Seen}", Color.white, enemy.Seen ? Color.red : Color.white, true);
            if (enemy.Seen)
            {
                stringBuilder.AppendLabeledValue("Time Since Seen", $"{enemy.TimeSinceSeen}", Color.white, Color.yellow, true);
            }
            stringBuilder.AppendLabeledValue("Currently Visible", $"{enemy.IsVisible}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Can Shoot", $"{enemy.CanShoot}", Color.white, Color.yellow, true);
            stringBuilder.AppendLabeledValue("In Line of Sight", $"{enemy.InLineOfSight}", Color.white, Color.yellow, true);

            stringBuilder.AppendLabeledValue("Heard?", $"{enemy.Heard}", Color.white, enemy.Seen ? Color.red : Color.white, true);
            if (enemy.Heard)
            {
                stringBuilder.AppendLabeledValue("Time Since Heard", $"{enemy.TimeSinceHeard}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Heard Recently?", $"{enemy.Status.HeardRecently}", Color.white, Color.yellow, true);
            }

            var lastKnown = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnown != null)
            {
                stringBuilder.AppendLabeledValue("Time Since Last Known Position Updated", $"{lastKnown.TimeSincePositionUpdated}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Enemy Distance from Last Known Position", $"{(enemy.EnemyPosition - lastKnown.Position).magnitude}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Has Arrived?", $"Personal: {lastKnown.HasArrivedPersonal} / Squad: {lastKnown.HasArrivedSquad}", Color.white, Color.yellow, true);
                stringBuilder.AppendLabeledValue("Has Seen?", $"Personal: {lastKnown.HasSeenPersonal} / Squad: {lastKnown.HasSeenSquad}", Color.white, Color.yellow, true);
            }
        }
    }
}