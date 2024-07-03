using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Memory
{
    public class LocationTracker : BotBase, IBotClass
    {
        public Collider BotZoneCollider => BotZone?.Collider;
        public AIPlaceInfo BotZone => BotOwner.AIData.PlaceInfo;
        public bool IsIndoors { get; private set; }

        public LocationTracker(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (_checkIndoorsTime < Time.time)
            {
                _checkIndoorsTime = Time.time + 0.2f;
                IsIndoors = Player.AIData.EnvironmentId != 0;
            }
        }

        public void Dispose()
        {
        }

        private float _checkIndoorsTime;
    }
}
