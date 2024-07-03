using EFT;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class HearingDispersionClass : BotSubClass<SAINHearingSensorClass>, IBotClass
    {
        public HearingDispersionClass(SAINHearingSensorClass hearing) : base(hearing)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public Vector3 CalcRandomizedPosition(BotSoundStruct sound, float addDispersion)
        {
            float distance = sound.Info.EnemyDistance;
            float baseDispersion = getBaseDispersion(distance, sound.Info.SoundType);
            float dispersionMod = getDispersionModifier(sound.Info.OriginalPosition) * addDispersion;
            float finalDispersion = baseDispersion * dispersionMod;
            sound.Dispersion.Dispersion = finalDispersion;
            float min = distance < 10 ? 0f : 0.5f;
            Vector3 randomdirection = getRandomizedDirection(finalDispersion, min);

            if (SAINPlugin.DebugSettings.DebugHearing)
                Logger.LogDebug($"Dispersion: [{randomdirection.magnitude}] Distance: [{distance}] Base Dispersion: [{baseDispersion}] DispersionModifier [{dispersionMod}] Final Dispersion: [{finalDispersion}] : SoundType: [{sound.Info.SoundType}]");

            return sound.Info.OriginalPosition + randomdirection;
        }

        private float getBaseDispersion(float shooterDistance, SAINSoundType soundType)
        {
            const float dispSuppGun = 12.5f;
            const float dispGun = 17.5f;
            const float dispStep = 6.25f;

            float dispersion;
            switch (soundType)
            {
                case SAINSoundType.Shot:
                    dispersion = shooterDistance / dispGun;
                    break;

                case SAINSoundType.SuppressedShot:
                    dispersion = shooterDistance / dispSuppGun;
                    break;

                default:
                    dispersion = shooterDistance / dispStep;
                    break;
            }

            return dispersion;
        }

        private void calcNewDispersion(BotSound sound)
        {
        }

        private void calcNewDispersionAngle(BotSound sound)
        {
        }

        private void calcNewDispersionDistance(BotSound sound)
        {
        }

        private float getDispersionModifier(Vector3 soundPosition)
        {
            float dispersionModifier = 1f;
            float dotProduct = Vector3.Dot(Bot.LookDirection.normalized, (soundPosition - Bot.Position).normalized);
            // The sound originated from behind us
            if (dotProduct <= 0f)
            {
                dispersionModifier = Mathf.Lerp(1.5f, 0f, dotProduct + 1f);
            }
            // The sound originated from infront of me.
            if (dotProduct > 0)
            {
                dispersionModifier = Mathf.Lerp(1f, 0.5f, dotProduct);
            }
            //Logger.LogInfo($"Dispersion Modifier for Sound [{dispersionModifier}] Dot Product [{dotProduct}]");
            return dispersionModifier;
        }

        private float modifyDispersion(float dispersion, IPlayer person, out int soundCount)
        {
            // If a bot is hearing multiple sounds from a iPlayer, they will now be more accurate at finding the source soundPosition based on how many sounds they have heard from this particular iPlayer
            soundCount = 0;
            float finalDispersion = dispersion;
            //if (person != null && SoundsHeardFromPlayer.ContainsKey(person.ProfileId))
            //{
            //    SAINSoundCollection collection = SoundsHeardFromPlayer[person.ProfileId];
            //    if (collection != null)
            //    {
            //        soundCount = collection.Count;
            //    }
            //}
            //if (soundCount > 1)
            //{
            //    finalDispersion = (dispersion / soundCount).Round100();
            //}
            return finalDispersion;
        }

        private Vector3 getRandomizedDirection(float dispersion, float min = 0.5f)
        {
            float randomHeight = dispersion / 10f;
            float randomX = UnityEngine.Random.Range(-dispersion, dispersion);
            float randomY = UnityEngine.Random.Range(-randomHeight, randomHeight);
            float randomZ = UnityEngine.Random.Range(-dispersion, dispersion);
            Vector3 randomdirection = new Vector3(randomX, 0, randomZ);

            if (min > 0 && randomdirection.sqrMagnitude < min * min)
            {
                randomdirection = Vector3.Normalize(randomdirection) * min;
            }
            return randomdirection;
        }

        public Vector3 GetEstimatedPoint(Vector3 source, float distance)
        {
            Vector3 randomPoint = UnityEngine.Random.onUnitSphere;
            randomPoint.y = 0;
            randomPoint *= (distance / 10f);
            return source + randomPoint;
        }
    }
}