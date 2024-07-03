using EFT;
using SAIN.Components;
using SAIN.SAINComponent.SubComponents;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class SAINBotGrenadeClass : BotBase, IBotClass
    {
        public GrenadeTracker DangerGrenade { get; private set; }
        public Vector3? GrenadeDangerPoint => DangerGrenade?.DangerPoint;
        public Dictionary<int, GrenadeTracker> ActiveGrenades { get; private set; } = new Dictionary<int, GrenadeTracker>();

        public SAINBotGrenadeClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            SAINBotController.Instance.OnGrenadeCollision += grenadeCollision;
        }

        public void Update()
        {
            updateActiveGrenades();
        }

        private void updateActiveGrenades()
        {
            foreach (var tracker in ActiveGrenades.Values)
            {
                tracker?.Update();
            }
        }

        public void Dispose()
        {
            SAINBotController.Instance.OnGrenadeCollision -= grenadeCollision;
        }

        public void EnemyGrenadeThrown(Grenade grenade, Vector3 dangerPoint)
        {
            if (Bot.BotActive)
            {
                float reactionTime = GetReactionTime(Bot.Info.Profile.DifficultyModifier);
                ActiveGrenades.Add(grenade.Id, new GrenadeTracker(Bot, grenade, dangerPoint, reactionTime));
                grenade.DestroyEvent += removeGrenade;
            }
        }

        private void grenadeCollision(Grenade grenade, float maxRange)
        {
            foreach (var tracker in ActiveGrenades.Values)
            {
                if (tracker.Grenade == grenade)
                {
                    tracker.CheckHeardGrenadeCollision(maxRange);
                }
            }
        }

        private void removeGrenade(Throwable grenade)
        {
            if (grenade != null)
            {
                grenade.DestroyEvent -= removeGrenade;
                ActiveGrenades.Remove(grenade.Id);
            }
        }

        private static float GetReactionTime(float diffMod)
        {
            float reactionTime = 0.25f;
            reactionTime /= diffMod;
            reactionTime *= Random.Range(0.75f, 1.25f);

            float min = 0.1f;
            float max = 0.5f;

            return Mathf.Clamp(reactionTime, min, max);
        }

        public GrenadeThrowType GetThrowType(out GrenadeThrowDirection direction, out Vector3 ThrowAtPoint)
        {
            ThrowAtPoint = default;

            if (AllowCheck())
            {
                if (CanThrowOverObstacle(out ThrowAtPoint))
                {
                    direction = GrenadeThrowDirection.Over;
                }
                else if (CanThrowAroundObstacle(out ThrowAtPoint))
                {
                    direction = GrenadeThrowDirection.Around;
                }
                else
                {
                    direction = GrenadeThrowDirection.None;
                    return GrenadeThrowType.None;
                }

                float distance = (BotOwner.Position - Bot.Enemy.EnemyPosition).magnitude;

                if (distance <= 10f)
                {
                    return GrenadeThrowType.Close;
                }
                if (distance <= 30f)
                {
                    return GrenadeThrowType.Mid;
                }
                else
                {
                    return GrenadeThrowType.Far;
                }
            }
            else
            {
                direction = GrenadeThrowDirection.None;
                return GrenadeThrowType.None;
            }
        }

        private bool CanThrowAroundObstacle(out Vector3 ThrowAtPoint)
        {
            ThrowAtPoint = Vector3.zero;

            if (!AllowCheck())
            {
                return false;
            }

            Vector3 headPos = Bot.Transform.HeadPosition;
            Vector3 direction = Bot.Enemy.EnemyHeadPosition - headPos;

            float distance = direction.magnitude;

            if (distance > 50f)
            {
                return false;
            }

            if (Bot.Enemy.KnownPlaces.LastKnownPosition == null)
            {
                return false;
            }
            Vector3 lastKnownPos = Bot.Enemy.KnownPlaces.LastKnownPosition.Value;
            lastKnownPos.y += 1.45f;

            Vector3 lastKnownDirection = lastKnownPos - headPos;

            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            if (Physics.Raycast(headPos, lastKnownDirection, out var hit, lastKnownDirection.magnitude + 5f, mask) && (hit.point - headPos).magnitude > lastKnownDirection.magnitude)
            {
                ThrowAtPoint = hit.point;
                return true;
            }

            return false;
        }

        private bool CanThrowOverObstacle(out Vector3 ThrowAtPoint)
        {
            ThrowAtPoint = Vector3.zero;

            if (!AllowCheck())
            {
                return false;
            }

            var enemyHead = Bot.Enemy.EnemyHeadPosition;
            var botHead = Bot.Transform.HeadPosition;
            var direction = enemyHead - botHead;
            float distance = direction.magnitude;

            if (distance > 50f)
            {
                return false;
            }

            var mask = LayerMaskClass.HighPolyWithTerrainMask;

            if (Physics.Raycast(botHead, direction, out var hit, distance, mask))
            {
                if (Vector3.Distance(hit.point, botHead) < 0.33f)
                {
                    return false;
                }

                float height = hit.collider.bounds.size.y;
                var objectPos = hit.collider.transform.position;
                objectPos.y += height + 0.5f;

                var directionToHeight = objectPos - botHead;

                if (directionToHeight.magnitude > 30f)
                {
                    return false;
                }

                if (!Physics.Raycast(botHead, directionToHeight, directionToHeight.magnitude, mask))
                {
                    ThrowAtPoint = objectPos;
                    return true;
                }
            }

            return false;
        }

        private bool AllowCheck()
        {
            if (!BotOwner.WeaponManager.Grenades.HaveGrenade)
            {
                return false;
            }

            var enemy = Bot.Enemy;

            if (enemy == null)
            {
                return false;
            }
            if (enemy.IsVisible && enemy.CanShoot)
            {
                return false;
            }

            return true;
        }
    }
}