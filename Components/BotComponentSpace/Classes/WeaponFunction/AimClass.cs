using HarmonyLib;
using SAIN.Preset;
using System;
using System.Reflection;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class AimClass : BotBase, IBotClass
    {
        public event Action<bool> OnAimAllowedOrBlocked;

        public bool CanAim { get; private set; }

        public bool IsAiming { get; private set; }

        public AimStatus AimStatus
        {
            get
            {
                object aimStatus = aimStatusField.GetValue(BotOwner.AimingData);
                if (aimStatus == null)
                {
                    return AimStatus.NoTarget;
                }

                var status = (AimStatus)aimStatus;

                if (status != AimStatus.NoTarget &&
                    Bot.Enemy?.IsVisible == false &&
                    Bot.LastEnemy?.IsVisible == false)
                {
                    return AimStatus.NoTarget;
                }
                return status;
            }
        }

        public AimClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            checkCanAim();
            checkLoseTarget();
        }

        public void LateUpdate()
        {

        }

        private void checkCanAim()
        {
            bool couldAim = CanAim;
            CanAim = canAim();
            if (couldAim != CanAim)
            {
                OnAimAllowedOrBlocked?.Invoke(CanAim);
            }
        }

        private bool canAim()
        {
            var aimData = BotOwner.AimingData;
            if (aimData == null)
            {
                return false;
            }
            if (Player.IsSprintEnabled)
            {
                return false;
            }
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                return false;
            }
            if (!Bot.HasEnemy)
            {
                return false;
            }
            return true;
        }

        private void checkLoseTarget()
        {
            if (!CanAim)
            {
                LoseTarget();
                return;
            }
        }

        public void LoseTarget()
        {
            BotOwner.AimingData?.LoseTarget();
        }

        public void Dispose()
        {

        }

        static AimClass()
        {
            aimStatusField = AccessTools.Field(Helpers.HelpersGClass.AimDataType, "aimStatus_0");
        }

        private static FieldInfo aimStatusField;
    }
}
