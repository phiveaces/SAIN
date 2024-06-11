﻿using SAIN.SAINComponent.Classes.Sense;

namespace SAIN.SAINComponent.Classes
{
    public class SAINVisionClass : SAINBase, ISAINClass
    {
        public SAINVisionClass(BotComponent component) : base(component)
        {
            FlashLightDazzle = new FlashLightDazzleClass(component);
            BotLook = new SAINBotLookClass(component);
        }

        public void Init()
        {
            BotLook.Init();
        }

        public void Update()
        {
            FlashLightDazzle.CheckIfDazzleApplied(Bot.Enemy);
        }

        public void Dispose()
        {
        }

        public FlashLightDazzleClass FlashLightDazzle { get; private set; }

        public SAINBotLookClass BotLook { get; private set; }
    }
}