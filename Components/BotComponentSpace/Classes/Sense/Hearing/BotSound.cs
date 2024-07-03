using JetBrains.Annotations;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class BotSound
    {
        public BotSound(PlayerComponent playerComponent, Vector3 originalPos, float power, float volume, SAINSoundType soundType, Enemy enemy)
        {
            PlayerComponent = playerComponent;
            IsAI = playerComponent.IsAI;
            OriginalPosition = originalPos;
            Power = power;
            Volume = volume;
            SoundType = soundType;
            Enemy = enemy;
        }

        public readonly SoundInfoData Info;

        public readonly bool IsAI;
        public readonly PlayerComponent PlayerComponent;
        public readonly Vector3 OriginalPosition;
        public readonly SAINSoundType SoundType;
        public readonly float Power;
        public readonly float Volume;
        public readonly Enemy Enemy;

        public Vector3 ProjectionPoint;
        public float ProjectionPointDistance;
        public Vector3 RandomizedPosition;

        public float SqrDistance;
        public float Distance;

        public float BaseRange;
        public float Range;
        public float OccludedRange => Range * OcclusionModifier;

        public float OcclusionModifier = 1f;
        public float BunkerReduction = 1f;
        public float RangeModifier = 1f;
        public float Dispersion = 0f;
        public float ChanceToHear = 100f;

        public bool IsGunShot;
        public bool OutOfRange;
        public bool VisibleSource;
        public bool WasHeard;
        public bool BulletFelt;
    }

    public struct BotSoundStruct
    {
        public bool Heard => Results.Heard;
        public bool InRange => Range.FinalRange >= Info.Enemy.RealDistance;

        public BotSoundStruct(SoundInfoData info, float baseRange)
        {
            Info = info;
            Results = new SoundResultsData(false);
            Range = new SoundRangeData(baseRange);
            Dispersion = new SoundDispersionData(1f);
            BulletData = new BulletData(false);
        }

        public SoundInfoData Info;
        public SoundResultsData Results;
        public SoundRangeData Range;
        public SoundDispersionData Dispersion;
        public BulletData BulletData;
    }

    public struct SoundInfoData
    {
        public bool IsAI;
        public PlayerComponent EnemyPlayer;
        public Vector3 OriginalPosition;
        public SAINSoundType SoundType;
        public bool IsGunShot;
        public float Power;
        public float Volume;
        public Enemy Enemy;
        public float EnemyDistance;
    }

    public struct SoundDispersionData
    {
        public SoundDispersionData(float defaults = 1f)
        {
            EstimatedPosition = Vector3.zero;
            DistanceDispersion = defaults;
            AngleDispersionX = defaults;
            AngleDispersionY = defaults;
            DispersionModifier = defaults;
            DispersionType = ESoundDispersionType.None;
            Dispersion = 0f;
        }

        public float Dispersion;
        public Vector3 EstimatedPosition;

        // Unused
        public float DistanceDispersion;
        public float AngleDispersionX;
        public float AngleDispersionY;
        public float DispersionModifier;
        public ESoundDispersionType DispersionType;
    }

    public struct SoundRangeData
    {
        public SoundRangeData(float baseRange)
        {
            FinalRange = baseRange;
            BaseRange = baseRange;
            Modifiers = new SoundRangeModifiers(1f);
        }

        public float FinalRange;
        public float BaseRange;
        public SoundRangeModifiers Modifiers;
    }

    public struct SoundRangeModifiers
    {
        public float PreClampedMod => EnvironmentModifier * ConditionModifier * OcclusionModifier;

        public SoundRangeModifiers(float defaults = 1f)
        {
            FinalModifier = defaults;
            EnvironmentModifier = defaults;
            ConditionModifier = defaults;
            OcclusionModifier = defaults;
        }

        public float CalcFinalModifier(float min, float max)
        {
            return Mathf.Clamp(PreClampedMod, min, max);
        }

        public float FinalModifier;
        public float EnvironmentModifier;
        public float ConditionModifier;
        public float OcclusionModifier;
    }

    public struct SoundResultsData
    {
        public SoundResultsData(bool defaults = false)
        {
            Heard = defaults;
            VisibleSource = false;
            LimitedByAI = false;
            SoundFarFromPlayer = false;
            ChanceToHear = 100f;
            EstimatedPosition = Vector3.zero;
        }

        public bool Heard;
        public bool VisibleSource;
        public bool LimitedByAI;
        public bool SoundFarFromPlayer;
        public float ChanceToHear;
        public Vector3 EstimatedPosition;
    }

    public struct BulletData
    {
        public BulletData(bool defaults = false)
        {
            BulletFelt = defaults;
            BulletFiredAtMe = defaults;
            ProjectionPoint = Vector3.zero;
            ProjectionPointDistance = float.MaxValue;
            Suppressed = defaults;
        }

        public bool BulletFelt;
        public bool BulletFiredAtMe;
        public Vector3 ProjectionPoint;
        public float ProjectionPointDistance;
        public bool Suppressed;
    }
}