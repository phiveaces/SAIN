using EFT;
using System.Collections;

namespace SAIN.Layers.Combat.Solo
{
    internal class MeleeAttackAction : SAINAction, ISAINAction
    {
        public MeleeAttackAction(BotOwner bot) : base(bot, "Melee Attack")
        {
        }

        public override void Update()
        {
        }

        public override IEnumerator ActionCoroutine()
        {
            while (true)
            {
                BotOwner.WeaponManager.Melee.RunToEnemyUpdate();
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

        public void Toggle(bool value)
        {
            ToggleAction(value);
        }
    }
}