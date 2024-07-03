using EFT;
using System.Collections;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo
{
    public class ThrowGrenadeAction : SAINAction, ISAINAction
    {
        public ThrowGrenadeAction(BotOwner bot) : base(bot, nameof(ThrowGrenadeAction))
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
                if (!Stopped && Time.time - StartTime > 1f || Bot.Cover.CheckLimbsForCover())
                {
                    Stopped = true;
                    BotOwner.StopMove();
                }

                if (Bot.Squad.BotInGroup && Bot.Talk.GroupTalk.FriendIsClose)
                {
                    Bot.Talk.Say(EPhraseTrigger.OnGrenade);
                }

                yield return null;
            }
        }

        public override void Update()
        {
        }

        private float StartTime = 0f;
        private bool Stopped = false;

        public override void Start()
        {
            StartTime = Time.time;
            Toggle(true);
        }

        public override void Stop()
        {
            Toggle(false);
        }
    }
}