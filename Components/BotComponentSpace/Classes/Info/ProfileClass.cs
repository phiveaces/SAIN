﻿using EFT;
using SAIN.Helpers;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Info
{
    public class ProfileClass : BotBase
    {
        public ProfileClass(BotComponent sain) : base(sain)
        {
            Name = sain.BotOwner.name;

            var profile = sain.BotOwner.Profile;
            Faction = profile.Side;
            NickName = profile.Nickname;
            WildSpawnType = profile.Info.Settings.Role;
            BotDifficulty = profile.Info.Settings.BotDifficulty;
            PlayerLevel = profile.Info.Level;

            IsBoss = EnumValues.WildSpawn.IsBoss(WildSpawnType);
            IsFollower = EnumValues.WildSpawn.IsFollower(WildSpawnType);
            IsScav = EnumValues.WildSpawn.IsScav(WildSpawnType);
            IsPMC = EnumValues.WildSpawn.IsPMC(WildSpawnType);
            IsPlayerScav = IsScav && SAINEnableClass.isPlayerScav(NickName);
            SetDiffModifier(BotDifficulty);
        }

        public float DifficultyModifier { get; private set; }
        public float DifficultyModifierSqrt { get; private set; }
        public float PowerLevel => BotOwner.AIData.PowerOfEquipment;

        public readonly string Name;
        public readonly string NickName;
        public readonly bool IsBoss;
        public readonly bool IsFollower;
        public readonly bool IsScav;
        public readonly bool IsPMC;
        public readonly bool IsPlayerScav;
        public readonly BotDifficulty BotDifficulty;
        public readonly WildSpawnType WildSpawnType;
        public readonly EPlayerSide Faction;
        public readonly int PlayerLevel;

        private void SetDiffModifier(BotDifficulty difficulty)
        {
            float modifier = 1f;

            var sainSettings = SAINPlugin.LoadedPreset.BotSettings.SAINSettings;
            if (sainSettings.ContainsKey(WildSpawnType))
            {
                modifier = sainSettings[WildSpawnType].DifficultyModifier;
            }

            switch (difficulty)
            {
                case BotDifficulty.easy:
                    modifier *= 0.5f;
                    break;

                case BotDifficulty.normal:
                    modifier *= 1.0f;
                    break;

                case BotDifficulty.hard:
                    modifier *= 1.5f;
                    break;

                case BotDifficulty.impossible:
                    modifier *= 1.75f;
                    break;

                default:
                    break;
            }

            DifficultyModifier = modifier.Round100();
            DifficultyModifierSqrt = Mathf.Sqrt(modifier).Round100();
        }
    }
}