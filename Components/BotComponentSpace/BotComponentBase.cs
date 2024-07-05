using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using System;
using UnityEngine;

namespace SAIN.Components
{
    public abstract class BotComponentBase : MonoBehaviour
    {
        public event Action OnDispose;

        public string ProfileId { get; private set; }
        public PersonClass Person { get; private set; }

        public PlayerComponent PlayerComponent => Person.PlayerComponent;
        public BotOwner BotOwner => PlayerComponent.BotOwner;
        public Player Player => PlayerComponent.Player;
        public PersonTransformClass Transform => PlayerComponent.Transform;

        public Vector3 Position => PlayerComponent.Position;
        public Vector3 LookDirection => PlayerComponent.LookDirection;

        public virtual bool Init(PersonClass person)
        {
            if (person == null || person.Player == null)
            {
                return false;
            }
            Person = person;
            ProfileId = person.ProfileId;
            person.Player.ActiveHealthController.SetDamageCoeff(1f);
            return true;
        }

        public virtual void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}