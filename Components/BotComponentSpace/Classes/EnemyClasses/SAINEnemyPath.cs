using EFT;
using SAIN.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class SAINEnemyPath : EnemyBase, ISAINEnemyClass
    {
        public EPathDistance EPathDistance
        {
            get
            {
                float distance = PathDistance;
                if (distance <= ENEMY_DISTANCE_VERYCLOSE)
                {
                    return EPathDistance.VeryClose;
                }
                if (distance <= ENEMY_DISTANCE_CLOSE)
                {
                    return EPathDistance.Close;
                }
                if (distance <= ENEMY_DISTANCE_MID)
                {
                    return EPathDistance.Mid;
                }
                if (distance <= ENEMY_DISTANCE_FAR)
                {
                    return EPathDistance.Far;
                }
                return EPathDistance.VeryFar;
            }
        }

        const float ENEMY_DISTANCE_VERYCLOSE = 10f;
        const float ENEMY_DISTANCE_CLOSE = 20f;
        const float ENEMY_DISTANCE_MID = 80f;
        const float ENEMY_DISTANCE_FAR = 150f;

        public float PathDistance { get; private set; } = float.MaxValue;

        public EnemyCornerDictionary EnemyCorners { get; private set; }

        public bool CanSeeLastCornerToEnemy
        {
            get
            {
                var last = EnemyCorners.EyeLevelPosition(ECornerType.Last);
                if (last == null)
                {
                    return false;
                }

                if (_nextCheckLast > Time.time)
                {
                    return _canSeeLast;
                }
                _nextCheckLast = Time.time + 0.2f;

                Vector3 cornerTarget = last.Value + Vector3.up;
                Vector3 headPos = Bot.Transform.EyePosition;
                Vector3 direction = cornerTarget - headPos;
                _canSeeLast = !Physics.Raycast(headPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);

                return _canSeeLast;
            }
        }

        public Vector3? BlindCornerToEnemyOld
        {
            get
            {
                Vector3? blindCorner = _blindCornerFinder.BlindCorner?.EyeLevelCorner(Bot.Transform.EyePosition, Bot.Position);

                if (blindCorner != null &&
                    SAINPlugin.DebugMode)
                {
                    if (blindcornerGUIObject == null)
                    {
                        blindcornerGUIObject = DebugGizmos.CreateLabel(blindCorner.Value, $"Blind Corner for {BotOwner.name} => {Enemy.EnemyPlayer?.name}");
                    }
                    else
                    {
                        blindcornerGUIObject.WorldPos = blindCorner.Value;
                    }

                    if (blindcornerGameObject != null)
                    {
                        blindcornerGameObject.transform.position = blindCorner.Value;
                    }
                    else
                    {
                        blindcornerGameObject = DebugGizmos.Sphere(blindCorner.Value, 0.1f, Color.red, false);
                    }
                }

                if (!SAINPlugin.DebugMode && blindcornerGUIObject != null)
                {
                    DebugGizmos.DestroyLabel(blindcornerGUIObject);
                    GameObject.Destroy(blindcornerGameObject);
                    blindcornerGUIObject = null;
                }
                return blindCorner;
            }
        }

        public NavMeshPath PathToEnemy { get; private set; }
        public NavMeshPathStatus PathToEnemyStatus { get; private set; }

        public SAINEnemyPath(Enemy enemy) : base(enemy)
        {
            PathToEnemy = new NavMeshPath();
            EnemyCorners = new EnemyCornerDictionary(enemy.Bot.Transform, enemy.BotOwner.WeaponRoot);
            _blindCornerFinder = new BlindCornerFinder(enemy);
        }

        private string CoroutineName;

        public void Init()
        {
            CoroutineName = $"{nameof(calcPathLoop)}:{Enemy.EnemyProfileId}";
            Enemy.Events.OnEnemyKnownChanged.OnToggle += OnEnemyKnownChanged;
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
            toggleCoroutine(known);
        }

        private void toggleCoroutine(bool value)
        {
            switch (value)
            {
                case true:
                    if (!_started)
                    {
                        _started = true;
                        Bot.CoroutineManager.Add(calcPathLoop(), CoroutineName);
                    }
                    break;

                case false:
                    if (_started)
                    {
                        _started = false;
                        Bot.CoroutineManager.Remove(CoroutineName);
                        Clear();
                    }
                    break;
            }
        }

        private bool _started;

        public void Update()
        {
            if (!_started && Enemy.EnemyKnown)
            {
                Logger.LogWarning($"Enemy Known but coroutine was not started!");
                toggleCoroutine(true);
            }
        }

        public void Dispose()
        {
            toggleCoroutine(false);
            EnemyCorners?.Clear();
            Enemy.Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;
        }

        private IEnumerator calcPathLoop()
        {
            while (true)
            {
                float timeAdd = calcDelayOnDistance();
                if (_calcPathTime + timeAdd > Time.time)
                {
                    yield return null;
                    continue;
                }

                _calcPathTime = Time.time;

                bool isCurrentEnemy = Enemy.IsCurrentEnemy;
                if (!isCurrentEnemy && !isEnemyInRange())
                {
                    yield return null;
                    continue;
                }

                // We should always have a not null LastKnownPosition here, but have the real position as a fallback just in case
                Vector3 enemyPosition = Enemy.KnownPlaces.LastKnownPosition ?? EnemyCurrentPosition;
                Vector3 botPosition = Bot.Position;

                // Did we already check the current enemy position and has the bot not moved? dont recalc path then
                if (!checkPositionsChanged(botPosition, enemyPosition))
                {
                    yield return null;
                    continue;
                }

                PathToEnemy.ClearCorners();
                NavMesh.CalculatePath(botPosition, enemyPosition, -1, PathToEnemy);
                PathToEnemyStatus = PathToEnemy.status;

                Vector3[] corners = PathToEnemy.corners;
                PathDistance = CalculatePathLength(corners);

                yield return null;

                switch (PathToEnemyStatus)
                {
                    case NavMeshPathStatus.PathInvalid:
                        EnemyCorners.Clear();
                        break;

                    case NavMeshPathStatus.PathPartial:
                    case NavMeshPathStatus.PathComplete:
                        findCorners(enemyPosition, PathToEnemyStatus, corners);
                        if (isCurrentEnemy)
                            yield return Bot.StartCoroutine(_blindCornerFinder.FindBlindCorner(corners, enemyPosition));
                        else
                            EnemyCorners.Remove(ECornerType.Blind);

                        break;
                }

                Enemy.Events.PathUpdated(PathToEnemyStatus);
                yield return null;
            }
        }

        private void findCorners(Vector3 enemyPosition, NavMeshPathStatus status, Vector3[] corners)
        {
            EnemyCorner? first = findFirstCorner(enemyPosition, corners);
            EnemyCorners.AddOrReplace(ECornerType.First, first);

            EnemyCorner? last = findLastCorner(enemyPosition, status, corners);
            EnemyCorners.AddOrReplace(ECornerType.Last, last);

            EnemyCorner? lastKnown = createLastKnownCorner(enemyPosition);
            EnemyCorners.AddOrReplace(ECornerType.LastKnown, lastKnown);
        }

        public void Clear()
        {
            PathToEnemy.ClearCorners();
            PathToEnemyStatus = NavMeshPathStatus.PathInvalid;
            EnemyCorners.Clear();
            PathDistance = float.MaxValue;
        }

        private float calcDelayOnDistance()
        {
            bool performanceMode = SAINPlugin.LoadedPreset.GlobalSettings.Performance.PerformanceMode;
            bool currentEnemy = Enemy.IsCurrentEnemy;
            bool isAI = Enemy.IsAI;
            bool searchingForEnemy = Enemy.Events.OnSearch.Value;
            float distance = Enemy.RealDistance;

            float maxDelay = isAI ? MAX_FREQ_CALCPATH_AI : MAX_FREQ_CALCPATH;
            if (currentEnemy)
                maxDelay *= CURRENTENEMY_COEF;
            if (performanceMode)
                maxDelay *= PERFORMANCE_MODE_COEF;
            if (searchingForEnemy)
                maxDelay *= ACTIVE_SEARCH_COEF;

            if (distance > MAX_FREQ_CALCPATH_DISTANCE)
            {
                return maxDelay;
            }

            float minDelay = isAI ? MIN_FREQ_CALCPATH_AI : MIN_FREQ_CALCPATH;
            if (currentEnemy)
                minDelay *= CURRENTENEMY_COEF;
            if (performanceMode)
                minDelay *= PERFORMANCE_MODE_COEF;
            if (searchingForEnemy)
                minDelay *= ACTIVE_SEARCH_COEF;

            if (distance < MIN_FREQ_CALCPATH_DISTANCE)
            {
                return minDelay;
            }

            float difference = distance - MIN_FREQ_CALCPATH_DISTANCE;
            float distanceRatio = difference / DISTANCE_DIFFERENCE;
            float delayDifference = maxDelay - minDelay;

            float result = distanceRatio * delayDifference + minDelay;
            float clampedResult = Mathf.Clamp(result, minDelay, maxDelay);

            if (_nextLogTime < Time.time)
            {
                _nextLogTime = Time.time + 10f;
                //Logger.LogDebug($"{BotOwner.name} calcPathFreqResults for [{Enemy.EnemyPerson.Nickname}] Result: [{result}] preClamped: [[{result}] [{distanceRatio} * {delayDifference} + {minDelay}]] : Distance: [{distance}] : IsAI? [{isAI}] : Current Enemy? [{currentEnemy}] : MinDelay [{minDelay}] : MaxDelay [{maxDelay}]");
            }

            return clampedResult;
        }

        private float _nextLogTime;

        private const float ACTIVE_SEARCH_COEF = 0.5f;

        private const float MAX_FREQ_CALCPATH = 2f;
        private const float MAX_FREQ_CALCPATH_AI = 4f;
        private const float MAX_FREQ_CALCPATH_DISTANCE = 250f;

        private const float MIN_FREQ_CALCPATH = 0.5f;
        private const float MIN_FREQ_CALCPATH_AI = 1f;
        private const float MIN_FREQ_CALCPATH_DISTANCE = 50f;

        private const float DISTANCE_DIFFERENCE = MAX_FREQ_CALCPATH_DISTANCE - MIN_FREQ_CALCPATH_DISTANCE;
        private const float PERFORMANCE_MODE_COEF = 1.5f;
        private const float CURRENTENEMY_COEF = 0.5f;

        private bool isEnemyInRange()
        {
            return Enemy.IsAI && Enemy.RealDistance <= MAX_CALCPATH_RANGE_AI || 
                !Enemy.IsAI && Enemy.RealDistance <= MAX_CALCPATH_RANGE;
        }

        private const float MAX_CALCPATH_RANGE = 500f;
        private const float MAX_CALCPATH_RANGE_AI = 300f;

        private bool checkPositionsChanged(Vector3 botPosition, Vector3 enemyPosition)
        {
            if (Enemy.Events.OnSearch.Value)
            {
                return true;
            }
            // Did we already check the current enemy position and has the bot not moved? dont recalc path then
            if (_enemyLastPosChecked != null
                && (_enemyLastPosChecked.Value - enemyPosition).sqrMagnitude < 0.1f
                && (_botLastPosChecked - botPosition).sqrMagnitude < 0.05f)
            {
                return false;
            }

            // cache the positions we are currently checking
            _enemyLastPosChecked = enemyPosition;
            _botLastPosChecked = botPosition;
            return true;
        }

        public float CalculatePathLength(Vector3[] corners)
        {
            if (corners == null)
            {
                return float.MaxValue;
            }
            float result = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[i + 1];
                result += (a - b).magnitude;
            }
            return result;
        }

        private EnemyCorner? findLastCorner(Vector3 enemyPosition, NavMeshPathStatus pathStatus, Vector3[] corners)
        {
            int length = corners.Length;
            // find the last corner before arriving at an enemy position, and then check if we can see it.
            Vector3 lastCorner;
            if (pathStatus == NavMeshPathStatus.PathComplete &&
                length > 2)
            {
                lastCorner = corners[length - 2];
            }
            else
            {
                lastCorner = corners[length - 1];
            }

            float signedAngle = Vector.FindFlatSignedAngle(lastCorner, enemyPosition, Bot.Transform.EyePosition);
            return new EnemyCorner(lastCorner, signedAngle);
        }

        private EnemyCorner? findFirstCorner(Vector3 enemyPosition, Vector3[] corners)
        {
            if (corners.Length < 2)
            {
                return null;
            }

            Vector3 firstCorner = corners[1];
            float signedAngle = Vector.FindFlatSignedAngle(firstCorner, enemyPosition, Bot.Transform.EyePosition);
            return new EnemyCorner(firstCorner, signedAngle);
        }

        private EnemyCorner? createLastKnownCorner(Vector3 enemyPosition)
        {
            return new EnemyCorner(enemyPosition, 0f);
        }

        private GUIObject blindcornerGUIObject;
        private GameObject blindcornerGameObject;
        private bool _canSeeLast;
        private float _nextCheckLast;
        private Vector3? _enemyLastPosChecked;
        private Vector3 _botLastPosChecked;
        private readonly BlindCornerFinder _blindCornerFinder;
        private float _calcPathTime = 0f;
    }
}