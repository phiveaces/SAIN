using EFT;
using SAIN.Components.PlayerComponentSpace.Classes;
using SAIN.Components.PlayerComponentSpace.Classes.Equipment;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Helpers;
using SAIN.SAINComponent;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components.PlayerComponentSpace
{
    public class PlayerComponent : MonoBehaviour
    {
        private void Update()
        {
            Person.Update();

            if (!Person.ActiveClass.PlayerActive)
            {
                return;
            }

            if (!IsAI || Person.ActiveClass.BotActive)
            {
                drawTransformGizmos();
                Flashlight.Update();
                Equipment.Update();
            }
        }

        private void LateUpdate()
        {
            Person.LateUpdate();
        }

        private void drawTransformGizmos()
        {
            if (SAINPlugin.DebugMode &&
                SAINPlugin.DrawDebugGizmos &&
                SAINPlugin.DebugSettings.DrawTransformGizmos)
            {
                DebugGizmos.Sphere(Transform.EyePosition, 0.05f, Color.white, true, 0.1f);
                DebugGizmos.Ray(Transform.EyePosition, Transform.HeadLookDirection, Color.white, Transform.HeadLookDirection.magnitude, 0.025f, true, 0.1f);

                DebugGizmos.Sphere(Transform.HeadPosition, 0.075f, Color.yellow, true, 0.1f);
                DebugGizmos.Ray(Transform.HeadPosition, Transform.LookDirection, Color.yellow, Transform.LookDirection.magnitude, 0.025f, true, 0.1f);

                DebugGizmos.Sphere(Transform.WeaponFirePort, 0.075f, Color.green, true, 0.1f);
                DebugGizmos.Ray(Transform.WeaponFirePort, Transform.WeaponPointDirection, Color.green, Transform.WeaponPointDirection.magnitude, 0.05f, true, 0.1f);

                DebugGizmos.Sphere(Transform.BodyPosition, 0.1f, Color.blue, true, 0.1f);
                DebugGizmos.Ray(Transform.BodyPosition, Transform.LookDirection, Color.blue, Transform.LookDirection.magnitude, 0.05f, true, 0.1f);
            }
        }

        private void startCoroutines()
        {
            if (_gearCoroutine == null)
            {
                _gearCoroutine = StartCoroutine(Equipment.GearInfo.GearUpdateLoop());
            }
        }

        private void stopCoroutines()
        {
            if (_gearCoroutine != null)
            {
                StopCoroutine(_gearCoroutine);
                _gearCoroutine = null;
            }
            StopAllCoroutines();
        }

        private Coroutine _gearCoroutine;

        public float DistanceToClosestHuman
        {
            get
            {
                return 0f;
            }
        }

        private void navRayCastAllDir()
        {
            if (!SAINPlugin.DebugMode ||
                !SAINPlugin.DrawDebugGizmos ||
                !Player.IsYourPlayer)
            {
                return;
            }

            Vector3 origin = Position;
            if (NavMesh.SamplePosition(origin, out var hit, 1f, -1))
            {
                origin = hit.position;
            }

            Vector3 direction;
            int max = 5;
            for (int i = 0; i < max; i++)
            {
                direction = UnityEngine.Random.onUnitSphere;
                direction.y = 0;
                direction = direction.normalized * 30f;
                Vector3 target = origin + direction;
                if (NavMesh.Raycast(origin, target, out var hit2, -1))
                {
                    target = hit2.position;
                }
                DebugGizmos.Line(origin, target, 0.05f, 0.25f, true);
            }
        }

        public string ProfileId { get; private set; }
        public FlashLightClass Flashlight { get; private set; }
        public PersonClass Person { get; private set; }
        public SAINAIData AIData { get; private set; }
        public SAINEquipmentClass Equipment { get; private set; }

        public bool IsActive => Person.Active;
        public Vector3 Position => Person.Transform.Position;
        public Vector3 LookDirection => Person.Transform.LookDirection;
        public Vector3 LookSensorPosition => Transform.EyePosition;

        public PersonTransformClass Transform => Person.Transform;
        public Player Player => Person.Player;
        public IPlayer IPlayer => Person.IPlayer;
        public string Name => Person.Name;
        public BotOwner BotOwner => Person.BotOwner;
        public BotComponent BotComponent => Person.BotComponent;
        public bool IsAI => Person.IsAI;
        public bool IsSAINBot => Person.IsSAINBot;

        public bool Init(IPlayer iPlayer, Player player)
        {
            ProfileId = iPlayer.ProfileId;

            try
            {
                Person = new PersonClass(iPlayer, player, this);
                Flashlight = new FlashLightClass(this);
                Equipment = new SAINEquipmentClass(this);
                AIData = new SAINAIData(Equipment.GearInfo, this);

                Person.ActiveClass.OnPlayerActiveChanged += handleCoroutines;
                handleCoroutines(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                return false;
            }
            Logger.LogDebug($"{Person.Nickname} Player Component Created");
            StartCoroutine(delayInit());
            return true;
        }

        private void handleCoroutines(bool active)
        {
            if (active)
                startCoroutines();
            else
                stopCoroutines();
        }

        private IEnumerator delayInit()
        {
            yield return null;
            Equipment.Init();
        }

        public void InitBotOwner(BotOwner botOwner)
        {
            Person.ActiveClass.OnPlayerActiveChanged -= handleCoroutines;
            Person.ActiveClass.OnBotActiveChanged += handleCoroutines;
            Person.InitBotOwner(botOwner);
        }

        public void InitBotComponent(BotComponent bot)
        {
            Person.InitBotComponent(bot);
        }

        private void OnDisable()
        {
            Person.ActiveClass.Disable();
            stopCoroutines();
        }

        public void Dispose()
        {
            Logger.LogDebug($"Destroying Playing Component for [Name: {Person?.Name} : Nickname: {Person?.Nickname}, ProfileID: {Person?.ProfileId}, at time: {Time.time}]");
            OnComponentDestroyed?.Invoke(ProfileId);
            stopCoroutines();
            Person.ActiveClass.OnBotActiveChanged -= handleCoroutines;
            Person.ActiveClass.OnPlayerActiveChanged -= handleCoroutines;
            Equipment?.Dispose();
            Destroy(this);
        }

        public event Action<string> OnComponentDestroyed;
    }
}