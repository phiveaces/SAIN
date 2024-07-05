using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using static SAIN.SAINComponent.Classes.Search.SearchReasonsStruct;

namespace SAIN.SAINComponent.Classes.Search
{
    public class SearchDeciderClass : BotSubClass<SAINSearchClass>
    {
        public SearchDeciderClass(SAINSearchClass searchClass) : base(searchClass)
        {
        }

        public bool ShallStartSearch(bool mustHaveTarget, out SearchReasonsStruct failReasons)
        {
            calcSearchTime();
            Enemy enemy = Bot.Enemy;

            failReasons = new SearchReasonsStruct();

            if (!WantToSearch(enemy, out failReasons.WantSearchReasons))
            {
                failReasons.NotSearchReason = ENotSearchReason.DontWantTo;
                return false;
            }

            if (Bot.Decision.CurrentSoloDecision == SoloDecision.Search)
            {
                if (BaseClass.FinalDestination == null)
                {
                    failReasons.NotSearchReason = ENotSearchReason.NullDestination;
                    return false;
                }
                return true;
            }

            if (!BaseClass.PathFinder.HasPathToSearchTarget(out failReasons.PathCalcFailReason, mustHaveTarget))
            {
                failReasons.NotSearchReason = ENotSearchReason.PathCalcFailed;
                return false;
            }
            return true;
        }

        private void calcSearchTime()
        {
            if (Bot.Decision.CurrentSoloDecision != SoloDecision.Search
                && _nextRecalcSearchTime < Time.time)
            {
                _nextRecalcSearchTime = Time.time + 120f;
                Bot.Info.CalcTimeBeforeSearch();
            }
        }

        public bool WantToSearch(Enemy enemy, out WantSearchReasonsStruct reasons)
        {
            reasons = new WantSearchReasonsStruct();
            if (enemy == null)
            {
                reasons.NotWantToSearchReason = ENotWantToSearchReason.NullEnemy;
                return false;
            }
            if (enemy.LastKnownPosition == null)
            {
                reasons.NotWantToSearchReason = ENotWantToSearchReason.NullLastKnown;
                return false;
            }
            if (!enemy.Seen && !Bot.Info.PersonalitySettings.Search.WillSearchFromAudio)
            {
                reasons.NotWantToSearchReason = ENotWantToSearchReason.WontSearchFromAudio;
                return false;
            }
            if (!canStartSearch(enemy, out reasons.CantStartReason))
            {
                reasons.NotWantToSearchReason = ENotWantToSearchReason.CantStart;
                return false;
            }
            if (!shallSearch(enemy, out reasons.WantToSearchReason))
            {
                reasons.NotWantToSearchReason = ENotWantToSearchReason.ShallNotSearch;
                return false;
            }
            reasons.NotWantToSearchReason = ENotWantToSearchReason.None;
            return true;
        }

        private bool shallSearch(Enemy enemy, out EWantToSearchReason reason)
        {
            if (ShallBeStealthyDuringSearch(enemy) &&
                Bot.Decision.EnemyDecisions.UnFreezeTime > Time.time &&
                enemy.TimeSinceLastKnownUpdated > 10f)
            {
                reason = EWantToSearchReason.BeingStealthy;
                return true;
            }

            float timeBeforeSearch = Bot.Info.TimeBeforeSearch;
            if (enemy.Events.OnSearch.Value)
            {
                return shallContinueSearch(enemy, timeBeforeSearch, out reason);
            }
            return shallBeginSearch(enemy, timeBeforeSearch, out reason);
        }

        public bool ShallBeStealthyDuringSearch(Enemy enemy)
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.Mind.SneakyBots)
            {
                return false;
            }
            if (SAINPlugin.LoadedPreset.GlobalSettings.Mind.OnlySneakyPersonalitiesSneaky &&
                !Bot.Info.PersonalitySettings.Search.Sneaky)
            {
                return false;
            }
            if (!enemy.Hearing.EnemyHeardFromPeace)
            {
                return false;
            }

            float maxDist = SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaximumDistanceToBeSneaky;
            return enemy.RealDistance < maxDist;
        }

        private bool shallBeginSearchCauseLooting(Enemy enemy)
        {
            if (!enemy.Status.EnemyIsLooting)
            {
                return false;
            }
            if (_nextCheckLootTime < Time.time)
            {
                _nextCheckLootTime = Time.time + _checkLootFreq;
                return EFTMath.RandomBool(_searchLootChance);
            }
            return false;
        }

        private bool shallBeginSearch(Enemy enemy, float timeBeforeSearch, out EWantToSearchReason reason)
        {
            if (shallBeginSearchCauseLooting(enemy))
            {
                enemy.Status.SearchingBecauseLooting = true;
                reason = EWantToSearchReason.NewSearch_Looting;
                return true;
            }
            float myPower = Bot.Info.Profile.PowerLevel;
            if (enemy.EnemyPlayer.AIData.PowerOfEquipment < myPower * 0.5f)
            {
                reason = EWantToSearchReason.NewSearch_PowerLevel;
                return true;
            }
            if (enemy.Seen && enemy.TimeSinceSeen >= timeBeforeSearch)
            {
                reason = EWantToSearchReason.NewSearch_EnemyNotSeen;
                return true;
            }
            if (enemy.Heard &&
                Bot.Info.PersonalitySettings.Search.WillSearchFromAudio &&
                enemy.TimeSinceHeard >= timeBeforeSearch)
            {
                reason = EWantToSearchReason.NewSearch_EnemyNotHeard;
                return true;
            }
            reason = EWantToSearchReason.None;
            return false;
        }

        private bool canStartSearch(Enemy enemy, out ECantStartReason reason)
        {
            var searchSettings = Bot.Info.PersonalitySettings.Search;
            if (!searchSettings.WillSearchForEnemy)
            {
                reason = ECantStartReason.WontSearchForEnemy;
                return false;
            }
            if (Bot.Suppression.IsHeavySuppressed)
            {
                reason = ECantStartReason.Suppressed;
                return false;
            }
            if (enemy.IsVisible)
            {
                reason = ECantStartReason.EnemyVisible;
                return false;
            }
            reason = ECantStartReason.None;
            return true;
        }

        private bool shallContinueSearch(Enemy enemy, float timeBeforeSearch, out EWantToSearchReason reason)
        {
            if (enemy.Status.SearchingBecauseLooting)
            {
                reason = EWantToSearchReason.ContinueSearch_Looting;
                return true;
            }

            float myPower = Bot.Info.Profile.PowerLevel;
            if (enemy.EnemyPlayer.AIData.PowerOfEquipment < myPower * 0.5f)
            {
                reason = EWantToSearchReason.ContinueSearch_PowerLevel;
                return true;
            }

            if (enemy.Seen)
            {
                timeBeforeSearch = Mathf.Clamp(timeBeforeSearch / 3f, 0f, 120f);
                if (enemy.TimeSinceSeen >= timeBeforeSearch)
                {
                    reason = EWantToSearchReason.ContinueSearch_EnemyNotSeen;
                    return true;
                }
            }

            if (enemy.Heard && Bot.Info.PersonalitySettings.Search.WillSearchFromAudio)
            {
                reason = EWantToSearchReason.ContinueSearch_EnemyNotHeard;
                return true;
            }

            reason = EWantToSearchReason.None;
            return false;
        }

        private float _nextRecalcSearchTime;
        private float _nextCheckLootTime;
        private float _checkLootFreq = 1f;
        private float _searchLootChance = 40f;
    }
}