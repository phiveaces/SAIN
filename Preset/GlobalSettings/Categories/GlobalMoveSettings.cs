using Newtonsoft.Json;
using SAIN.Attributes;

namespace SAIN.Preset.GlobalSettings
{
    public class GlobalMoveSettings : SAINSettingsBase<GlobalMoveSettings>, ISAINSettings
    {
        [Advanced]
        [MinMax(150f, 500f, 1f)]
        public float AimTurnSpeed = 300f;

        [Advanced]
        public bool EditSprintSpeed = true;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintNotMovingThreshold = 0.1f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintTryVaultTime = 0.1f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintTryJumpTime = 0.45f;

        [Advanced]
        [MinMax(0.01f, 3f, 100f)]
        public float BotSprintRecalcTime = 1f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 1000f)]
        public float BotSprintFirstTurnDotThreshold = 0.925f;

        [Advanced]
        [MinMax(0.01f, 1f, 100f)]
        public float BotSprintCornerReachDist = 0.15f;

        [Advanced]
        [MinMax(0.1f, 1.5f, 100f)]
        public float BotSprintFinalDestReachDist= 0.25f;

        [Advanced]
        [MinMax(0.01f, 2f, 100f)]
        public float BotSprintDistanceToStopSprintDestination = 1f;

        [Advanced]
        [MinMax(0.01f, 1.5f, 100f)]
        public float BotSprintMinDist = 0.5f;

        //[Advanced]
        //[MinMax(1f, 90f, 1f)]
        //public float BotSprintNextCornerAngleMax = 30f;

        [Advanced]
        [MinMax(1f, 90f, 1f)]
        public float BotSprintCurrentCornerAngleMax = 25f;

        [Advanced]
        [MinMax(1f, 500f, 1f)]
        public float BotSprintTurnSpeedWhileSprint = 300f;

        [Advanced]
        [MinMax(1f, 500f, 1f)]
        public float BotSprintTurningSpeed = 400f;
    }
}