using EFT;
using SAIN.Helpers;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyKnownPlaces : EnemyBase, ISAINEnemyClass
    {
        public EnemyPlace LastSeenPlace { get; private set; }
        public EnemyPlace LastHeardPlace { get; private set; }
        public EnemyPlace LastSquadSeenPlace { get; private set; }
        public EnemyPlace LastSquadHeardPlace { get; private set; }
        public float TimeSinceLastKnownUpdated => Time.time - TimeLastKnownUpdated;
        public EnemyPlace LastKnownPlace { get; private set; }
        public Vector3? LastKnownPosition { get; private set; }
        public Vector3? LastSeenPosition { get; private set; }
        public Vector3? LastHeardPosition { get; private set; }

        public float EnemyDistanceFromLastKnown
        {
            get
            {
                if (LastKnownPlace == null)
                {
                    return float.MaxValue;
                }
                return LastKnownPlace.Distance(EnemyCurrentPosition);
            }
        }

        public float EnemyDistanceFromLastSeen
        {
            get
            {
                if (LastSeenPlace == null)
                {
                    return float.MaxValue;
                }
                return LastSeenPlace.Distance(EnemyCurrentPosition);
            }
        }

        public float EnemyDistanceFromLastHeard
        {
            get
            {
                if (LastHeardPlace == null)
                {
                    return float.MaxValue;
                }
                return LastHeardPlace.Distance(EnemyCurrentPosition);
            }
        }

        public bool SearchedAllKnownLocations { get; private set; }

        private void checkSearched()
        {
            if (_nextCheckSearchTime > Time.time)
            {
                return;
            }
            _nextCheckSearchTime = Time.time + 0.5f;

            bool allSearched = true;
            foreach (var place in AllEnemyPlaces)
            {
                if (place == null)
                {
                    continue;
                }

                if (!place.HasArrivedPersonal &&
                    !place.HasArrivedSquad)
                {
                    allSearched = false;
                    break;
                }
            }

            if (allSearched
                && !SearchedAllKnownLocations)
            {
                Enemy.Events.EnemyLocationsSearched();
            }

            SearchedAllKnownLocations = allSearched;
        }

        public List<EnemyPlace> AllEnemyPlaces { get; } = new List<EnemyPlace>();

        public EnemyKnownPlaces(Enemy enemy) : base(enemy)
        {
        }

        public void Init()
        {
            Enemy.Events.OnEnemyKnownChanged.OnToggle += OnEnemyKnownChanged;
        }

        public void Dispose()
        {
            clearAllPlaces();

            Enemy.Events.OnEnemyKnownChanged.OnToggle -= OnEnemyKnownChanged;

            foreach (var obj in _guiObjects)
            {
                DebugGizmos.DestroyLabel(obj.Value);
            }
            _guiObjects?.Clear();
        }

        public void OnEnemyKnownChanged(bool known, Enemy enemy)
        {
            if (known)
            {
                return;
            }
            clearAllPlaces();
        }

        private void clearAllPlaces()
        {
            AllEnemyPlaces.Clear();
            LastSeenPlace = null;
            LastHeardPlace = null;
            LastSquadSeenPlace = null;
            LastSquadHeardPlace = null;
            LastKnownPosition = null;
            LastKnownPlace = null;
        }

        public void Update()
        {
            updatePlaces();
            if (Enemy.EnemyKnown)
            {
                checkIfArrived(); 
                checkSearched();

                if (Enemy.IsCurrentEnemy)
                {
                    checkIfSeen();
                    createDebug();
                }
            }
        }

        private void createDebug()
        {
            if (SAINPlugin.DebugMode)
            {
                EnemyPlace lastKnown = LastKnownPlace;
                if (lastKnown != null)
                {
                    if (debugLastKnown == null)
                    {
                        debugLastKnown = DebugGizmos.CreateLabel(lastKnown.Position, string.Empty);
                    }
                    updateDebugString(lastKnown, debugLastKnown);
                }
            }
            else if (debugLastKnown != null)
            {
                DebugGizmos.DestroyLabel(debugLastKnown);
                debugLastKnown = null;
            }
        }

        private void checkIfSeen()
        {
            if (_nextCheckSeenTime < Time.time)
            {
                _nextCheckSeenTime = Time.time + _checkSeenFreq;

                Vector3 lookSensor = Bot.PlayerComponent.LookSensorPosition;
                string myProfileId = Bot.ProfileId;

                foreach (EnemyPlace place in AllEnemyPlaces)
                {
                    if (place == null)
                    {
                        continue;
                    }

                    bool ismyPlace = place.OwnerID == myProfileId;
                    if (ismyPlace &&
                        !place.HasSeenPersonal &&
                        place.PersonalClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI))
                    {
                        tryTalk();
                    }
                    else if (!ismyPlace &&
                        !place.HasSeenSquad &&
                        place.SquadClearLineOfSight(lookSensor, LayerMaskClass.HighPolyWithTerrainMaskAI))
                    {
                        tryTalk();
                    }
                }
            }
        }

        private void tryTalk()
        {
            if (_nextTalkClearTime < Time.time 
                && Bot.Talk.GroupSay(EFTMath.RandomBool() ? EPhraseTrigger.Clear : EPhraseTrigger.LostVisual, null, true, 33))
            {
                _nextTalkClearTime = Time.time + 5f;
            }
        }

        private void checkIfArrived()
        {
            float time = Time.time;
            if (_checkArrivedTime < time)
            {
                _checkArrivedTime = time + _checkArriveFreq;
                Vector3 myPosition = Bot.Position;
                string myProfileID = Bot.ProfileId;

                foreach (EnemyPlace place in AllEnemyPlaces)
                {
                    if (place != null)
                    {
                        bool ismyPlace = place.OwnerID == myProfileID;
                        if (ismyPlace && checkPersonalArrived(place, myPosition))
                        {
                            tryTalk();
                        }
                        else if (!ismyPlace && checkSquadArrived(place, myPosition))
                        {
                            tryTalk();
                        }
                    }
                }
            }
        }

        private bool checkPersonalArrived(EnemyPlace place, Vector3 myPosition)
        {
            if (place != null
                && !place.HasArrivedPersonal
                && checkArrived(place, myPosition))
            {
                place.HasArrivedPersonal = true;
                return true;
            }
            return false;
        }

        private bool checkSquadArrived(EnemyPlace place, Vector3 myPosition)
        {
            if (place != null
                && !place.HasArrivedSquad
                && checkArrived(place, myPosition))
            {
                place.HasArrivedSquad = true;
                return true;
            }
            return false;
        }

        private bool checkArrived(EnemyPlace place, Vector3 myPosition)
        {
            return (myPosition - place.Position).sqrMagnitude < arrivedReachDistanceSqr;
        }

        private void updateDebugString(EnemyPlace place, GUIObject obj)
        {
            obj.WorldPos = place.Position;

            StringBuilder stringBuilder = obj.StringBuilder;
            stringBuilder.Clear();

            stringBuilder.AppendLine($"Bot: {BotOwner.name}");
            stringBuilder.AppendLine($"Known Location of {EnemyPlayer.Profile.Nickname}");

            if (LastKnownPlace == place)
            {
                stringBuilder.AppendLine($"Last Known Location.");
            }

            stringBuilder.AppendLine($"Time Since Position Updated: {Time.time - place.TimePositionUpdated}");

            stringBuilder.AppendLine($"Arrived? [{place.HasArrivedPersonal}]"
                + (place.HasArrivedPersonal ? $"Time Since Arrived: [{Time.time - place.TimeArrived}]" : string.Empty));

            stringBuilder.AppendLine($"Seen? [{place.HasSeenPersonal}]"
                + (place.HasSeenPersonal ? $"Time Since Seen: [{Time.time - place.TimeSeen}]" : string.Empty));
        }

        public EnemyPlace UpdateSeenPlace(Vector3 position)
        {
            if (LastSeenPlace == null)
            {
                LastSeenPlace = new EnemyPlace(Bot.Player.ProfileId, position, 300f, true, EnemyIPlayer);
                LastSeenPlace.HasSeenPersonal = true;
                addPlace(LastSeenPlace);
            }
            else
            {
                LastSeenPlace.Position = position;
            }
            return LastSeenPlace;
        }

        private void addPlace(EnemyPlace place)
        {
            if (place != null)
            {
                SearchedAllKnownLocations = false;
                place.OnPositionUpdated += lastKnownPosUpdated;
                lastKnownPosUpdated(place);
                AllEnemyPlaces.Add(place);
            }
        }

        public void UpdateSquadSeenPlace(EnemyPlace place)
        {
            if (place == null)
            {
                return;
            }
            if (LastSquadSeenPlace == place)
            {
                return;
            }
            removePlace(LastSquadSeenPlace);
            LastSquadSeenPlace = place;
            addPlace(place);
        }

        public EnemyPlace UpdatePersonalHeardPosition(Vector3 position, bool isGunFire)
        {
            if (Enemy.IsVisible)
            {
                return null;
            }

            var lastHeard = LastHeardPlace;
            if (lastHeard != null)
            {
                lastHeard.Position = position;
                lastHeard.HasArrivedPersonal = false;
                lastHeard.HasArrivedSquad = false;
                lastHeard.HasSeenPersonal = false;
                lastHeard.HasSeenSquad = false;
                return lastHeard;
            }

            LastHeardPlace = new EnemyPlace(Bot.Player.ProfileId, position, 300f, isGunFire, EnemyIPlayer);
            addPlace(LastHeardPlace);
            return LastHeardPlace;
        }

        public void UpdateSquadHeardPlace(EnemyPlace place)
        {
            if (place == null)
            {
                return;
            }
            if (LastSquadHeardPlace == place)
            {
                return;
            }

            removePlace(LastSquadHeardPlace);
            LastSquadHeardPlace = place;
            addPlace(place);
        }

        private void updatePlaces(bool force = false)
        {
            if (force || _nextUpdatePlacesTime < Time.time)
            {
                _nextUpdatePlacesTime = Time.time + 0.5f;
                sortAndClearPlaces();
            }
        }

        private void removePlace(EnemyPlace place)
        {
            if (place != null)
            {
                place.OnPositionUpdated -= lastKnownPosUpdated;
                AllEnemyPlaces.Remove(place);
            }
        }

        private void sortAndClearPlaces()
        {
            if (LastSeenPlace?.ShallClear == true)
            {
                removePlace(LastSeenPlace);
                LastSeenPlace = null;
            }
            if (LastHeardPlace?.ShallClear == true)
            {
                removePlace(LastHeardPlace);
                LastHeardPlace = null;
            }
            if (LastSquadHeardPlace?.ShallClear == true)
            {
                removePlace(LastSquadHeardPlace);
                LastSquadHeardPlace = null;
            }
            if (LastSquadSeenPlace?.ShallClear == true)
            {
                removePlace(LastSquadSeenPlace);
                LastSquadSeenPlace = null;
            }

            if (AllEnemyPlaces.Count > 0)
            {
                AllEnemyPlaces.RemoveAll(x => x == null);
                AllEnemyPlaces.Sort((x, y) => x.TimeSincePositionUpdated.CompareTo(y.TimeSincePositionUpdated));
            }
        }

        private void lastKnownPosUpdated(EnemyPlace place)
        {
            if (place == null) return;

            SearchedAllKnownLocations = false;
            TimeLastKnownUpdated = Time.time;
            LastKnownPlace = place;
            Vector3 pos = place.Position;
            LastKnownPosition = pos;
            Enemy.Events.LastKnownUpdated(place);
        }

        public float TimeLastKnownUpdated { get; private set; }

        private EnemyPlace findMostRecentPlace(out float timeUpdated)
        {
            EnemyPlace result = null;
            timeUpdated = float.MaxValue;
            if (LastSeenPlace != null)
            {
                result = LastSeenPlace;
                timeUpdated = LastSeenPlace.TimeSincePositionUpdated;
            }
            if (Enemy.IsVisible)
            {
                return result;
            }
            foreach (var place in AllEnemyPlaces)
            {
                if (place != null &&
                    place.TimeSincePositionUpdated < timeUpdated)
                {
                    timeUpdated = place.TimeSincePositionUpdated;
                    result = place;
                }
            }
            return result;
        }

        private float _nextTalkClearTime;
        private float _checkArrivedTime;
        private const float _checkArriveFreq = 0.25f;
        private const float arrivedReachDistanceSqr = 0.5f;
        private float _nextCheckSearchTime;
        private float _nextCheckSeenTime;
        private const float _checkSeenFreq = 0.5f;
        private float _nextUpdatePlacesTime;
        private GUIObject debugLastKnown;
        private readonly Dictionary<EnemyPlace, GUIObject> _guiObjects = new Dictionary<EnemyPlace, GUIObject>();
    }
}