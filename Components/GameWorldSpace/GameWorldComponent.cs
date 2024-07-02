using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Patches.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components
{
    public class GameWorldComponent : MonoBehaviour
    {
        public bool WinterActive => WinterStatus == ESeasonStatus.Winter;

        public static GameWorldComponent Instance { get; set; }
        public ELocation Location { get; private set; }
        public GameWorld GameWorld { get; private set; }
        public PlayerSpawnTracker PlayerTracker { get; private set; }
        public SAINBotController SAINBotController { get; private set; }
        public Extract.ExtractFinderComponent ExtractFinder { get; private set; }
        public SpawnPointMarker[] SpawnPointMarkers { get; private set; }

        private void Update()
        {
            findWeather();
            findLocation();
            findSpawnPointMarkers();
        }

        private void findWeather()
        {
            if (_weatherFound)
            {
                return;
            }
            if (_nextCheckWeatherTime > Time.time)
            {
                return;
            }
            _nextCheckWeatherTime = Time.time + 0.5f;

            var weather = (AccessTools.Field(typeof(GameWorld), "ginterface26_0").GetValue(GameWorld) as GInterface26);
            if (weather == null)
            {
                WinterStatus = ESeasonStatus.Summer;
            }
            else
            {
                Logger.LogWarning($"Got Weather! {weather.Status}");
                WinterStatus = weather.Status;
                _weatherFound = true;
            }
        }

        private bool _weatherFound;
        private float _nextCheckWeatherTime;

        private void findLocation()
        {
            if (!_foundLocation)
            {
                Location = parseLocation();
            }
        }

        private ELocation parseLocation()
        {
            ELocation Location = ELocation.None;
            string locationString = GameWorld?.LocationId;
            if (locationString.IsNullOrEmpty())
            {
                return Location;
            }

            switch (locationString.ToLower())
            {
                case "bigmap":
                    Location = ELocation.Customs;
                    break;

                case "factory4_day":
                    Location = ELocation.Factory;
                    break;

                case "factory4_night":
                    Location = ELocation.FactoryNight;
                    break;

                case "interchange":
                    Location = ELocation.Interchange;
                    break;

                case "laboratory":
                    Location = ELocation.Labs;
                    break;

                case "lighthouse":
                    Location = ELocation.Lighthouse;
                    break;

                case "rezervbase":
                    Location = ELocation.Reserve;
                    break;

                case "sandbox":
                    Location = ELocation.GroundZero;
                    break;

                case "shoreline":
                    Location = ELocation.Shoreline;
                    break;

                case "tarkovstreets":
                    Location = ELocation.Streets;
                    break;

                case "terminal":
                    Location = ELocation.Terminal;
                    break;

                case "town":
                    Location = ELocation.Town;
                    break;

                default:
                    Location = ELocation.None;
                    break;
            }

            _foundLocation = true;
            return Location;
        }

        public ESeasonStatus WinterStatus { get; private set; }

        private void findSpawnPointMarkers()
        {
            if ((SpawnPointMarkers != null) || (Camera.main == null))
            {
                return;
            }

            SpawnPointMarkers = UnityEngine.Object.FindObjectsOfType<SpawnPointMarker>();

            if (SAINPlugin.DebugMode)
                Logger.LogInfo($"Found {SpawnPointMarkers.Length} spawn point markers");
        }

        public IEnumerable<Vector3> GetAllSpawnPointPositionsOnNavMesh()
        {
            List<Vector3> spawnPointPositions = new List<Vector3>();
            foreach (SpawnPointMarker spawnPointMarker in SpawnPointMarkers)
            {
                // Try to find a point on the NavMesh nearby the spawn point
                Vector3? spawnPointPosition = NavMeshHelpers.GetNearbyNavMeshPoint(spawnPointMarker.Position, 2);
                if (spawnPointPosition.HasValue && !spawnPointPositions.Contains(spawnPointPosition.Value))
                {
                    spawnPointPositions.Add(spawnPointPosition.Value);
                }
            }

            return spawnPointPositions;
        }

        private void Awake()
        {
            Instance = this;
            GameWorld = this.GetComponent<GameWorld>();
            if (GameWorld == null)
            {
                Logger.LogWarning($"GameWorld is null from GetComponent");
            }
            StartCoroutine(Init());
        }

        private IEnumerator Init()
        {
            yield return getGameWorld();

            if (GameWorld == null)
            {
                Logger.LogWarning("GameWorld Null, cannot Init SAIN Gameworld! Check 2. Disposing Component...");
                Dispose();
                yield break;
            }

            PlayerTracker = new PlayerSpawnTracker(this);
            SAINBotController = this.GetOrAddComponent<SAINBotController>();
            ExtractFinder = this.GetOrAddComponent<Extract.ExtractFinderComponent>();
            GameWorld.OnDispose += Dispose;

            Logger.LogWarning("SAIN GameWorld Created.");
        }

        private IEnumerator getGameWorld()
        {
            if (GameWorld != null)
            {
                yield break;
            }
            if (GameWorld == null)
            {
                yield return new WaitForEndOfFrame();
                GameWorld = findGameWorld();
                if (GameWorld != null)
                {
                    Logger.LogWarning("Found GameWorld at EndOfFrame");
                    yield break;
                }
            }
            for (int i = 0; i < 30; i++)
            {
                if (GameWorld == null)
                {
                    yield return null;
                    GameWorld = findGameWorld();
                }
                if (GameWorld != null)
                {
                    break;
                }
            }
        }

        private GameWorld findGameWorld()
        {
            GameWorld gameWorld = this.GetComponent<GameWorld>();
            if (gameWorld == null)
            {
                gameWorld = Singleton<GameWorld>.Instance;
            }
            return gameWorld;
        }

        private void OnDestroy()
        {
            Instance = null;
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                PlayerTracker?.Dispose();
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose GameWorld Component Class Error: {e}");
            }

            try
            {
                ComponentHelpers.DestroyComponent(SAINBotController);
            }
            catch (Exception e)
            {
                Logger.LogError($"Dispose GameWorld SubComponent Error: {e}");
            }

            Instance = null;
            GameWorld.OnDispose -= Dispose;
            Destroy(this);
            Logger.LogWarning("SAIN GameWorld Destroyed.");
        }

        private bool _foundLocation = false;
    }
}