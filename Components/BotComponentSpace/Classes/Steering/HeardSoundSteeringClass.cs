using EFT;
using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class HeardSoundSteeringClass : BotSubClass<SAINSteeringClass>
    {
        public HeardSoundSteeringClass(SAINSteeringClass steering) : base(steering)
        {
            _hearingPath = new NavMeshPath();
        }

        public void LookToHeardPosition(Vector3 soundPos, bool visionCheck = false)
        {
            if ((soundPos - Bot.Position).sqrMagnitude > 125f.Sqr())
            {
                BaseClass.LookToPoint(soundPos);
                return;
            }

            findCorner(soundPos);

            if (_lastHeardSoundCorner != null)
            {
                BaseClass.LookToPoint(_lastHeardSoundCorner.Value);
            }
            else
            {
                BaseClass.LookToPoint(soundPos);
            }
        }

        private void findCorner(Vector3 soundPos)
        {
            if (_lastHeardSoundTimer < Time.time || (_lastHeardSoundCheckedPos - soundPos).magnitude > 1f)
            {
                _lastHeardSoundTimer = Time.time + 1f;
                _lastHeardSoundCheckedPos = soundPos;
                _hearingPath.ClearCorners();
                if (NavMesh.CalculatePath(Bot.Position, soundPos, -1, _hearingPath))
                {
                    if (_hearingPath.corners.Length > 2)
                    {
                        Vector3 headPos = BotOwner.LookSensor._headPoint;
                        for (int i = _hearingPath.corners.Length - 1; i >= 0; i--)
                        {
                            Vector3 corner = _hearingPath.corners[i] + Vector3.up;
                            Vector3 cornerDir = corner - headPos;
                            if (!Physics.Raycast(headPos, cornerDir.normalized, cornerDir.magnitude, LayerMaskClass.HighPolyWithTerrainMask))
                            {
                                _lastHeardSoundCorner = corner;
                                return;
                            }
                        }
                    }
                }
                _lastHeardSoundCorner = null;
            }
        }

        private float _lastHeardSoundTimer;
        private Vector3 _lastHeardSoundCheckedPos;
        private Vector3? _lastHeardSoundCorner;
        private NavMeshPath _hearingPath;
    }
}