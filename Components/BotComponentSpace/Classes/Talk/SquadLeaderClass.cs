﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;
using SAIN.Components;
using UnityEngine.AI;
using BepInEx.Logging;
using SAIN.SAINComponent;

namespace SAIN.SAINComponent.Classes.Talk
{
    public class SquadLeaderClass : BotBase
    {
        public SquadLeaderClass(BotComponent owner) : base(owner)
        {
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (!Bot.BotActive || Bot.GameEnding)
            {
                return;
            }

            if (!Bot.Squad.BotInGroup)
            {
                return;
            }
        }

        public void Dispose()
        {
        }
    }
}
