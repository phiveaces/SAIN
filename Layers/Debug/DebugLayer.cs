using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System.Text;
using SAIN.SAINComponent;
using SAIN.Layers.Combat.Solo.Cover;
using System.Collections.Generic;
using SAIN.Layers.Combat.Solo;

namespace SAIN.Layers.Combat.Run
{
    internal class DebugLayer : SAINLayer
    {
        public DebugLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.Run)
        {
        }

        public static readonly string Name = BuildLayerName("SAIN Debug");

        public override Action GetNextAction()
        {
            if (SAINPlugin.DebugSettings.ForceBotsToRunAround)
                return new Action(typeof(RunningAction), $"RUNNING");

            if (SAINPlugin.DebugSettings.ForceBotsToTryCrawl)
                return new Action(typeof(CrawlAction), $"CRAWL");

            return new Action(typeof(RunningAction), $"RUNNING");
        }

        public override bool IsActive()
        {
            bool active = SAINPlugin.DebugSettings.ForceBotsToRunAround || SAINPlugin.DebugSettings.ForceBotsToTryCrawl;
            setLayer(active);
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            if (Bot == null) return true;

            return false;
        }

        public SoloDecision CurrentDecision => Bot.Decision.CurrentSoloDecision;
    }
}