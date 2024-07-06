using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverFinderComponent : BotComponentBase
    {
        public CoverFinderStatus CurrentStatus { get; private set; }

        public Vector3 OriginPoint
        {
            get
            {
                if (_sampleOriginTime < Time.time)
                {
                    _sampleOriginTime = Time.time + 0.1f;
                    Vector3 botPos = Bot.Position;
                    if (NavMesh.SamplePosition(botPos, out var hit, 0.5f, -1))
                    {
                        botPos = hit.position;
                    }
                    _origin = botPos;
                }
                return _origin;
            }
        }

        public Vector3 TargetPoint
        {
            get
            {
                if (_sampleTargetTime < Time.time)
                {
                    _sampleTargetTime = Time.time + 0.1f;
                    if (getTargetPosition(out Vector3? target))
                    {
                        Vector3 targetValue = target.Value;
                        if (NavMesh.SamplePosition(targetValue, out var hit, 0.5f, -1))
                        {
                            targetValue = hit.position;
                        }
                        _targetPoint = targetValue;
                    }
                }
                return _targetPoint;
            }
        }

        public BotComponent Bot { get; private set; }
        public List<CoverPoint> CoverPoints { get; } = new List<CoverPoint>();
        private CoverAnalyzer CoverAnalyzer { get; set; }
        private ColliderFinder ColliderFinder { get; set; }
        public bool ProcessingLimited { get; private set; }
        public CoverPoint FallBackPoint { get; private set; }
        public List<SpottedCoverPoint> SpottedCoverPoints { get; private set; } = new List<SpottedCoverPoint>();

        private int _targetCoverCount
        {
            get
            {
                if (_nextUpdateTargetTime < Time.time)
                {
                    _nextUpdateTargetTime = Time.time + 0.1f;

                    int targetCount;
                    if (PerformanceMode)
                    {
                        if (Bot.Enemy != null)
                        {
                            targetCount = Bot.Enemy.IsAI ? 2 : 4;
                        }
                        else
                        {
                            targetCount = 2;
                        }
                    }
                    else
                    {
                        if (Bot.Enemy != null)
                        {
                            targetCount = Bot.Enemy.IsAI ? 4 : 8;
                        }
                        else
                        {
                            targetCount = 2;
                        }
                    }
                    _targetCount = targetCount;
                }
                return _targetCount;
            }
        }

        public void Init(BotComponent botComponent)
        {
            base.Init(botComponent.Person);
            Bot = botComponent;
            BotName = botComponent.name;

            ColliderFinder = new ColliderFinder(this);
            CoverAnalyzer = new CoverAnalyzer(botComponent, this);

            botComponent.BotActivation.BotActiveToggle.OnToggle += botEnabled;
            botComponent.BotActivation.BotStandByToggle.OnToggle += botInStandBy;
            botComponent.OnDispose += botDisposed;
        }

        private void botInStandBy(bool value)
        {
            if (value)
            {
                ToggleCoverFinder(false);
            }
        }

        private void botEnabled(bool value)
        {
            ToggleCoverFinder(value);
        }

        public void ToggleCoverFinder(bool value)
        {
            switch (value)
            {
                case true:
                    LookForCover();
                    break;

                case false:
                    StopLooking();
                    break;
            }
        }

        private void Update()
        {
            if (DebugCoverFinder)
            {
                if (CoverPoints.Count > 0)
                {
                    DebugGizmos.Line(CoverPoints.PickRandom().Position, Bot.Transform.HeadPosition, Color.yellow, 0.035f, true, 0.1f);
                }
            }
        }

        private bool getTargetPosition(out Vector3? target)
        {
            if (Bot.Grenade.GrenadeDangerPoint != null)
            {
                target = Bot.Grenade.GrenadeDangerPoint;
                return true;
            }
            target = Bot.CurrentTargetPosition;
            return target != null;
        }

        public void LookForCover()
        {
            if (_findCoverPointsCoroutine == null)
            {
                _findCoverPointsCoroutine = StartCoroutine(findCoverLoop());
            }
            if (_recheckCoverPointsCoroutine == null)
            {
                _recheckCoverPointsCoroutine = StartCoroutine(recheckCoverLoop());
            }
        }

        public void StopLooking()
        {
            if (_findCoverPointsCoroutine != null)
            {
                CurrentStatus = CoverFinderStatus.None;
                StopCoroutine(_findCoverPointsCoroutine);
                _findCoverPointsCoroutine = null;

                StopCoroutine(_recheckCoverPointsCoroutine);
                _recheckCoverPointsCoroutine = null;

                CoverPoints.Clear();

                if (Bot != null)
                {
                    Bot.Cover.CoverInUse = null;
                }

                FallBackPoint = null;
            }
        }

        private IEnumerator recheckCoverPoints(List<CoverPoint> coverPoints, bool limit = true)
        {
            // if (!limit || (limit && HavePositionsChanged()))
            if (havePositionsChanged())
            {
                bool shallLimit = limit && shallLimitProcessing();
                WaitForSeconds wait = new WaitForSeconds(0.05f);

                CoverFinderStatus lastStatus = CurrentStatus;
                CurrentStatus = shallLimit ? CoverFinderStatus.RecheckingPointsWithLimit : CoverFinderStatus.RecheckingPointsNoLimit;

                CoverPoint coverInUse = Bot.Cover.CoverInUse;
                bool updated = false;
                if (coverInUse != null)
                {
                    if (!PointStillGood(coverInUse, out updated, out ECoverFailReason failReason))
                    {
                        //Logger.LogWarning(failReason);
                        coverInUse.IsBad = true;
                    }
                    if (updated)
                    {
                        yield return shallLimit ? wait : null;
                    }
                }

                for (int i = coverPoints.Count - 1; i >= 0; i--)
                {
                    var coverPoint = coverPoints[i];
                    if (!PointStillGood(coverPoint, out updated, out ECoverFailReason failReason))
                    {
                        //Logger.LogWarning(failReason);
                        coverPoint.IsBad = true;
                    }
                    if (updated)
                    {
                        yield return shallLimit ? wait : null;
                    }
                }
                CurrentStatus = lastStatus;

                yield return null;
            }
        }

        private bool havePositionsChanged()
        {
            float recheckThresh = 0.5f;
            if (PerformanceMode)
            {
                recheckThresh = 1.5f;
            }
            if ((_lastRecheckTargetPosition - TargetPoint).sqrMagnitude < recheckThresh * recheckThresh
                && (_lastRecheckBotPosition - OriginPoint).sqrMagnitude < recheckThresh * recheckThresh)
            {
                return false;
            }

            _lastRecheckTargetPosition = TargetPoint;
            _lastRecheckBotPosition = OriginPoint;

            return true;
        }

        private bool shallLimitProcessing()
        {
            ProcessingLimited =
                Bot.Enemy?.IsAI == true ||
                limitProcessingFromDecision(Bot.Decision.CurrentSoloDecision);

            return ProcessingLimited;
        }

        private static bool limitProcessingFromDecision(CombatDecision decision)
        {
            switch (decision)
            {
                case CombatDecision.MoveToCover:
                case CombatDecision.RunToCover:
                case CombatDecision.Retreat:
                case CombatDecision.RunAway:
                    return false;

                case CombatDecision.HoldInCover:
                case CombatDecision.Search:
                    return true;

                default:
                    return PerformanceMode;
            }
        }

        private bool colliderAlreadyUsed(Collider collider)
        {
            for (int i = 0; i < CoverPoints.Count; i++)
            {
                if (collider == CoverPoints[i].Collider)
                {
                    return true;
                }
            }
            return false;
        }

        private bool filterColliderByName(Collider collider)
        {
            return collider != null &&
                _excludedColliderNames.Contains(collider.transform?.parent?.name);
        }

        private IEnumerator recheckCoverLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
                ClearSpotted();
                _tempRecheckList.AddRange(CoverPoints);
                yield return StartCoroutine(recheckCoverPoints(_tempRecheckList, false));
                yield return StartCoroutine(finishRechecking(_tempRecheckList));
                yield return wait;
            }
        }

        private IEnumerator finishRechecking(List<CoverPoint> list)
        {
            foreach (var point in list)
            {
                if (point == null || point.IsBad)
                {
                    CoverPoints.Remove(point);
                }
            }
            list.Clear();
            OrderPointsByPathDist(CoverPoints);
            yield return null;
        }

        private bool needToFindCover(int coverCount, out int max)
        {
            const float distThreshold = 5f;
            const float distThresholdSqr = distThreshold * distThreshold;
            max = _targetCoverCount;
            bool needToFindCover =
                coverCount < max / 2
                || (coverCount <= 1 && coverCount < max)
                || (_lastPositionChecked - OriginPoint).sqrMagnitude >= distThresholdSqr;
            return needToFindCover;
        }

        private IEnumerator findCoverLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(0.1f);
            while (true)
            {
                int coverCount = CoverPoints.Count;
                if (needToFindCover(coverCount, out int max))
                {
                    CurrentStatus = CoverFinderStatus.SearchingColliders;
                    _lastPositionChecked = OriginPoint;
                    bool debug = DebugCoverFinder;

                    Stopwatch fullStopWatch = debug ? Stopwatch.StartNew() : null;
                    Stopwatch findFirstPointStopWatch = coverCount == 0 && debug ? Stopwatch.StartNew() : null;

                    Collider[] colliders = _colliderArray;
                    yield return StartCoroutine(ColliderFinder.GetNewColliders(colliders));
                    ColliderFinder.SortArrayBotDist(colliders);
                    yield return StartCoroutine(findCoverPoints(colliders, ColliderFinder.HitCount, max, findFirstPointStopWatch));

                    logAndFinish(debug, findFirstPointStopWatch, fullStopWatch);
                }

                CurrentStatus = CoverFinderStatus.None;
                yield return wait;
            }
        }

        private void logAndFinish(bool debug, Stopwatch a, Stopwatch b)
        {
            int coverCount = CoverPoints.Count;
            if (coverCount > 1)
            {
                OrderPointsByPathDist(CoverPoints);
            }
            if (coverCount > 0)
            {
                FallBackPoint = FindFallbackPoint(CoverPoints);
                if (_debugLogTimer < Time.time && debug)
                {
                    _debugLogTimer = Time.time + 1f;
                    Logger.LogInfo($"[{BotOwner.name}] - Found [{coverCount}] CoverPoints. Colliders checked: [{_totalChecked}] Collider Array Size = [{ColliderFinder.HitCount}]");
                }
            }
            else
            {
                FallBackPoint = null;
                if (_debugLogTimer < Time.time && debug)
                {
                    _debugLogTimer = Time.time + 1f;
                    Logger.LogWarning($"[{BotOwner.name}] - No Cover Found! Valid Colliders checked: [{_totalChecked}] Collider Array Size = [{ColliderFinder.HitCount}]");
                }
            }

            if (a?.IsRunning == true)
            {
                a.Stop();
            }
            b?.Stop();
            if (_debugTimer2 < Time.time && debug)
            {
                _debugTimer2 = Time.time + 5;
                Logger.LogAndNotifyDebug($"Time to Complete Coverfinder Loop: [{b.ElapsedMilliseconds}ms]");
            }
        }

        private IEnumerator findCoverPoints(Collider[] colliders, int hits, int max, Stopwatch debugStopWatch)
        {
            _totalChecked = 0;
            int waitCount = 0;
            int coverCount = CoverPoints.Count;

            for (int i = 0; i < hits; i++)
            {
                if (coverCount >= max)
                {
                    break;
                }

                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                // Main Optimization, scales with the amount of points a bot currently has, and slows down the rate as it grows.
                if (coverCount > 2)
                {
                    yield return null;
                }
                else if (coverCount > 0)
                {
                    // How long did it take to find at least 1 point?
                    endStopWatch(debugStopWatch);

                    if (waitCount >= 3 || shallLimitProcessing())
                    {
                        waitCount = 0;
                        yield return null;
                    }
                }
                else if (waitCount >= 5)
                {
                    waitCount = 0;
                    yield return null;
                }

                _totalChecked++;

                if (filterColliderByName(collider))
                {
                    continue;
                }
                if (colliderAlreadyUsed(collider))
                {
                    continue;
                }
                // The main Calculations
                if (CoverAnalyzer.CheckCollider(collider, out CoverPoint newPoint, out _))
                {
                    CoverPoints.Add(newPoint);
                    coverCount++;
                }

                waitCount++;
            }
        }

        private void endStopWatch(Stopwatch debugStopWatch)
        {
            if (debugStopWatch?.IsRunning == true)
            {
                debugStopWatch.Stop();
                if (_debugTimer < Time.time)
                {
                    _debugTimer = Time.time + 5;
                    Logger.LogAndNotifyDebug($"Time to Find First CoverPoint: [{debugStopWatch.ElapsedMilliseconds}ms]");
                }
            }
        }

        public static void OrderPointsByPathDist(List<CoverPoint> points)
        {
            points.Sort((x, y) => x.RoundedPathLength.CompareTo(y.RoundedPathLength));
        }

        private CoverPoint FindFallbackPoint(List<CoverPoint> points)
        {
            CoverPoint result = null;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];

                if (result == null
                    || point.Collider.bounds.size.y > result.Collider.bounds.size.y)
                {
                    result = point;
                }
            }
            return result;
        }

        public bool PointStillGood(CoverPoint coverPoint, out bool updated, out ECoverFailReason failReason)
        {
            updated = false;
            failReason = ECoverFailReason.None;
            if (coverPoint == null || coverPoint.IsBad)
            {
                failReason = ECoverFailReason.NullOrBad;
                return false;
            }
            if (!coverPoint.ShallUpdate)
            {
                return true;
            }
            if (PointIsSpotted(coverPoint))
            {
                failReason = ECoverFailReason.Spotted;
                return false;
            }
            updated = true;
            return CoverAnalyzer.RecheckCoverPoint(coverPoint, out failReason);
        }

        private void ClearSpotted()
        {
            if (_nextClearSpottedTime < Time.time)
            {
                _nextClearSpottedTime = Time.time + 0.5f;
                SpottedCoverPoints.RemoveAll(x => x.IsValidAgain);
            }
        }

        private bool PointIsSpotted(CoverPoint point)
        {
            if (point == null)
            {
                return true;
            }

            ClearSpotted();

            foreach (var spottedPoint in SpottedCoverPoints)
            {
                Vector3 spottedPointPos = spottedPoint.CoverPoint.Position;
                if (spottedPoint.TooClose(spottedPointPos, point.Position))
                {
                    return true;
                }
            }
            if (point.Spotted)
            {
                SpottedCoverPoints.Add(new SpottedCoverPoint(point));
            }
            return point.Spotted;
        }

        private void OnDestroy()
        {
            StopLooking();
            StopAllCoroutines();
        }

        private void botDisposed()
        {
            Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();
            StopLooking();
            StopAllCoroutines();
            if (Bot != null)
            {
                Bot.OnDispose -= botDisposed;
                Bot.BotActivation.BotActiveToggle.OnToggle -= ToggleCoverFinder;
            }
            Destroy(this);
        }

        private float _sampleOriginTime;
        private Vector3 _origin;
        private float _sampleTargetTime;
        private readonly Collider[] _colliderArray = new Collider[300];
        private int _targetCount;
        private float _nextUpdateTargetTime;
        private Vector3 _targetPoint;
        private Vector3 _lastPositionChecked = Vector3.zero;
        private Vector3 _lastRecheckTargetPosition;
        private Vector3 _lastRecheckBotPosition;
        private int _totalChecked;
        private static float _debugTimer;
        private static float _debugTimer2;
        private float _debugLogTimer = 0f;
        private float _nextClearSpottedTime;
        private string BotName;
        private Coroutine _findCoverPointsCoroutine;
        private Coroutine _recheckCoverPointsCoroutine;
        private readonly List<CoverPoint> _tempRecheckList = new List<CoverPoint>();

        private static bool AllCollidersAnalyzed;
        private const int _maxOldPoints = 10;
        public static bool PerformanceMode { get; private set; } = false;
        public static float CoverMinHeight { get; private set; } = 0.5f;
        public static float CoverMinEnemyDist { get; private set; } = 5f;
        public static float CoverMinEnemyDistSqr { get; private set; } = 25f;
        public static bool DebugCoverFinder { get; private set; } = false;

        private static readonly List<string> _excludedColliderNames = new List<string>
        {
            "metall_fence_2",
            "metallstolb",
            "stolb",
            "fonar_stolb",
            "fence_grid",
            "metall_fence_new",
            "ladder_platform",
            "frame_L",
            "frame_small_collider",
            "bump2x_p3_set4x",
            "bytovka_ladder",
            "sign",
            "sign17_lod",
            "ograda1",
            "ladder_metal"
        };

        static CoverFinderComponent()
        {
            PresetHandler.OnPresetUpdated += updateSettings;
            updateSettings(SAINPresetClass.Instance);
        }

        private static void updateSettings(SAINPresetClass preset)
        {
            PerformanceMode = SAINPlugin.LoadedPreset.GlobalSettings.Performance.PerformanceMode;
            CoverMinHeight = SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverMinHeight;
            CoverMinEnemyDist = SAINPlugin.LoadedPreset.GlobalSettings.Cover.CoverMinEnemyDistance;
            CoverMinEnemyDistSqr = CoverMinEnemyDist * CoverMinEnemyDist;
            DebugCoverFinder = SAINPlugin.LoadedPreset.GlobalSettings.Cover.DebugCoverFinder;
        }

        private static void AnalyzeAllColliders()
        {
            if (!AllCollidersAnalyzed)
            {
                AllCollidersAnalyzed = true;
                float minHeight = CoverFinderComponent.CoverMinHeight;
                const float minX = 0.1f;
                const float minZ = 0.1f;

                Collider[] allColliders = new Collider[500000];
                int hits = Physics.OverlapSphereNonAlloc(Vector3.zero, 1000f, allColliders);

                int hitReduction = 0;
                for (int i = 0; i < hits; i++)
                {
                    Vector3 size = allColliders[i].bounds.size;
                    if (size.y < CoverFinderComponent.CoverMinHeight
                        || size.x < minX && size.z < minZ)
                    {
                        allColliders[i] = null;
                        hitReduction++;
                    }
                }
                Logger.LogError($"All Colliders Analyzed. [{hits - hitReduction}] are suitable out of [{hits}] colliders");
            }
        }
    }
}