using EFT;
using EFT.InventoryLogic;
using SAIN.Helpers;
using System.Collections;
using System.Text;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class Recoil : BotBase, IBotClass
    {
        public Vector3 CurrentRecoilOffset { get; private set; } = Vector3.zero;
        private Vector3 _lookDir => Player.LookDirection * 10f;

        public float ArmInjuryModifier => calcModFromInjury(Bot.Medical.HitReaction.LeftArmInjury) * calcModFromInjury(Bot.Medical.HitReaction.RightArmInjury);

        public Recoil(BotComponent sain) : base(sain)
        {
        }

        private readonly StringBuilder _debugString = new StringBuilder();

        public void Init()
        {
            Bot.BotActivation.OnBotStateChanged += removeCoroutine;
        }

        private float calcModFromInjury(EInjurySeverity severity)
        {
            switch (severity)
            {
                default:
                    return 1f;

                case EInjurySeverity.Injury:
                    return 1.15f;

                case EInjurySeverity.HeavyInjury:
                    return 1.35f;

                case EInjurySeverity.Destroyed:
                    return 1.65f;
            }
        }

        public void Update()
        {
        }

        private void removeCoroutine(bool value)
        {
            if (!value)
            {
                CurrentRecoilOffset = Vector3.zero;
                _recoilActive = false;
                Bot.CoroutineManager.Remove("RecoilLoop");
                _recoilFinished = true;
                _barrelRising = false;
                _debugString.Clear();
            }
        }

        private bool _recoilActive;

        private static bool _debugRecoilLogs => SAINPlugin.DebugSettings.DebugRecoilCalculations;

        private IEnumerator RecoilLoop()
        {
            while (true)
            {
                calcBarrelRise();
                calcDecay();
                if (_recoilFinished)
                {
                    if (_debugRecoilLogs)
                    {
                        Logger.LogDebug(_debugString.ToString());
                        _debugString.Clear();
                    }
                    removeCoroutine(false);
                    break;
                }
                yield return null;
            }
        }

        private void calcBarrelRise()
        {
            if (!_barrelRising)
            {
                return;
            }

            float riseTime = Time.deltaTime * _barrelRiseCoef;
            _barrelRiseTime += riseTime;

            if (_debugRecoilLogs)
                _debugString.AppendLine($"Barrel Rise Progress [{_barrelRiseTime}] Rise Step: {riseTime}");

            if (_barrelRiseTime > 1)
            {
                _barrelRising = false;
                _barrelRiseTime = 1f;
            }

            CurrentRecoilOffset = Vector3.Lerp(CurrentRecoilOffset, _recoilOffsetTarget, _barrelRiseTime);

            if (!_barrelRising)
            {
                _barrelRiseTime = 0f;
                _recoilOffsetTarget = Vector3.zero;
            }
        }

        private static float _barrelRiseCoef => SAINPlugin.LoadedPreset.GlobalSettings.Shoot.RecoilRiseCoef;
        private float _barrelRiseTime;

        private void calcDecay()
        {
            if (_recoilFinished)
            {
                return;
            }

            float decayTime = Time.deltaTime * _recoilDecayCoef;
            _barrelRecoveryTime += decayTime;

            if (_debugRecoilLogs)
                _debugString.AppendLine($"Recoil Decay Progress [{_barrelRecoveryTime}] Decay Step: {decayTime}");

            if (_barrelRecoveryTime > 1)
            {
                _barrelRecoveryTime = 1;
                _recoilFinished = true;
            }

            CurrentRecoilOffset = Vector3.Lerp(CurrentRecoilOffset, Vector3.zero, _barrelRecoveryTime);
            if (_barrelRising )
            {
                _recoilOffsetTarget = Vector3.Lerp(_recoilOffsetTarget, Vector3.zero, _barrelRecoveryTime);
            }

            if (_recoilFinished)
            {
                _barrelRecoveryTime = 0;
            }
        }

        private static float _recoilDecayCoef => SAINPlugin.LoadedPreset.GlobalSettings.Shoot.RecoilDecayCoef;
        private float _barrelRecoveryTime;

        public void Dispose()
        {
            Bot.BotActivation.OnBotStateChanged -= removeCoroutine;
        }

        public void WeaponShot()
        {
            calculateRecoil();
            _barrelRising = true;
            _recoilFinished = false;

            if (!_recoilActive)
            {
                _recoilActive = true;
                Bot.CoroutineManager.Add(RecoilLoop(), "RecoilLoop");
            }
        }

        private void calculateRecoil()
        {
            Weapon weapon = Bot.Info?.WeaponInfo?.CurrentWeapon;
            if (weapon == null)
            {
                return;
            }

            float addRecoil = SAINPlugin.LoadedPreset.GlobalSettings.Shoot.AddRecoil;
            float recoilMod = calcRecoilMod();
            float recoilTotal = weapon.RecoilTotal;
            float recoilNum = calcRecoilNum(recoilTotal) + addRecoil;
            float calcdRecoil = recoilNum * recoilMod;

            float randomvertRecoil = Random.Range(calcdRecoil / 2f, calcdRecoil) * randomSign();
            float randomHorizRecoil = Random.Range(calcdRecoil / 2f, calcdRecoil) * randomSign();

            Vector3 result = Vector.Rotate(_lookDir, randomHorizRecoil, randomvertRecoil, randomHorizRecoil);
            result -= _lookDir;
            _recoilOffsetTarget += result;

            if (_debugRecoilLogs)
                _debugString.AppendLine($"Recoil! New Recoil: [{result.magnitude}] " +
                $"Current Total Recoil Magnitude: [{_recoilOffsetTarget.magnitude}] " +
                $"recoilNum: [{recoilNum}] calcdRecoil: [{calcdRecoil}] : " +
                $"Randomized Vert [{randomvertRecoil}] : Randomized Horiz [{randomHorizRecoil}] " +
                $"Modifiers [ Add: [{addRecoil}] Multi: [{recoilMod}] Weapon RecoilTotal [{recoilTotal}]]");
        }

        private float randomSign()
        {
            return EFTMath.RandomBool() ? -1 : 1;
        }

        private float calcRecoilMod()
        {
            float recoilMod = 1f * RecoilMultiplier;

            if (Player.IsInPronePose)
            {
                recoilMod *= 0.7f;
            }
            else if (Player.Pose == EPlayerPose.Duck)
            {
                recoilMod *= 0.9f;
            }

            if (BotOwner.WeaponManager?.ShootController?.IsAiming == true)
            {
                recoilMod *= 0.9f;
            }
            if (Player.Velocity.magnitude < 0.5f)
            {
                recoilMod *= 0.85f;
            }
            if (_armsInjured)
            {
                recoilMod *= Mathf.Sqrt(ArmInjuryModifier);
            }

            return recoilMod;
        }

        float calcRecoilNum(float recoilVal)
        {
            float result = recoilVal / 100;
            if (ModDetection.RealismLoaded)
            {
                result = recoilVal / 150;
            }
            result *= Bot.Info.WeaponInfo.FinalModifier;
            result *= UnityEngine.Random.Range(0.8f, 1.2f);
            return result;
        }

        private bool _barrelRising;
        private Vector3 _recoilOffsetTarget;
        private bool _recoilFinished;
        private bool _armsInjured => Bot.Medical.HitReaction.ArmsInjured;
        private float RecoilMultiplier => Mathf.Round(Bot.Info.FileSettings.Shoot.RecoilMultiplier * GlobalSettings.Shoot.RecoilMultiplier * 100f) / 100f;

        private float RecoilBaseline
        {
            get
            {
                if (ModDetection.RealismLoaded)
                {
                    return 225f;
                }
                else
                {
                    return 112f;
                }
            }
        }
    }
}
