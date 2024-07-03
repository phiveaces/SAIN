using EFT;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Components.PlayerComponentSpace.PersonClasses;
using SAIN.Preset;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public abstract class EnemyBase : BotBase
    {
        public EnemyBase(Enemy enemy) : base (enemy.Bot)
        {
            Enemy = enemy;
        }

        protected override void SubscribeToPreset(Action<SAINPresetClass> func)
        {
            if (func != null)
            {
                func.Invoke(SAINPresetClass.Instance);
                _autoUpdater.Subscribe(func);
                Enemy.OnEnemyDisposed += this.UnSubscribeToPreset;
            }
        }

        protected override void UnSubscribeToPreset()
        {
            if (_autoUpdater.Subscribed)
            {
                _autoUpdater.UnSubscribe();
                Enemy.OnEnemyDisposed -= this.UnSubscribeToPreset;
            }
        }

        protected Enemy Enemy { get; }
        protected EnemyInfo EnemyInfo => Enemy.EnemyInfo;
        protected PersonClass EnemyPerson => Enemy.EnemyPerson;
        protected PlayerComponent EnemyPlayerComponent => EnemyPerson.PlayerComponent;
        protected Player EnemyPlayer => EnemyPerson.Player;
        protected IPlayer EnemyIPlayer => EnemyPerson.IPlayer;
        protected PersonTransformClass EnemyTransform => EnemyPerson.Transform;
        protected Vector3 EnemyCurrentPosition => EnemyTransform.Position;
    }
}