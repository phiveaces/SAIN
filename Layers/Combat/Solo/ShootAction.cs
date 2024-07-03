using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Collections;

namespace SAIN.Layers.Combat.Solo
{
    internal class ShootAction : SAINAction, ISAINAction
    {
        public ShootAction(BotOwner bot) : base(bot, nameof(ShootAction))
        {
        }

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }

        public override IEnumerator ActionCoroutine()
        {
            while (true)
            {
                Bot.Steering.SteerByPriority();
                Shoot.Update();
                yield return null;
            }
        }

        public override void Start()
        {
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);
        }

        public override void Update()
        {
        }
    }
}