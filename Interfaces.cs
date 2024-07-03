using BepInEx.Logging;
using EFT;
using SAIN.Attributes;
using SAIN.Components.PlayerComponentSpace;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;

namespace SAIN
{
    public interface IBotClass
    {
        BotComponent Bot { get; }

        void Init();
        void Update();
        void Dispose();
    }

    public interface ISAINEnemyClass
    {
        void OnEnemyKnownChanged(bool known, Enemy enemy);

        void Init();
        void Update();
        void Dispose();
    }
}
