using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System.Text;
using SAIN.SAINComponent;
using SAIN.Layers.Combat.Solo.Cover;
using System.Collections.Generic;
using SAIN.Layers.Combat.Solo;

namespace SAIN.Layers
{
    internal class SAINAvoidThreatLayer : SAINLayer
    {
        public SAINAvoidThreatLayer(BotOwner bot, int priority) : base(bot, priority, Name, ESAINLayer.AvoidThreat)
        {
        }

        public static readonly string Name = BuildLayerName("Avoid Threat");

        public override Action GetNextAction()
        {
            _lastActionDecision = CurrentDecision;
            switch (_lastActionDecision)
            {
                case SoloDecision.DogFight:
                    if (Bot.Decision.DogFightDecision.DogFightTarget != null)
                    {
                        return new Action(typeof(DogFightAction), $"Dog Fight - Enemy Close!");
                    }
                    else if (Bot.Cover.CoverInUse?.Spotted == true)
                    {
                        return new Action(typeof(DogFightAction), $"Dog Fight - My Cover is Spotted!");
                    }
                    else if (Bot.Decision.EnemyDecisions.ShotInCover)
                    {
                        return new Action(typeof(DogFightAction), $"Dog Fight - Shot while in cover!");
                    }
                    else
                    {
                        return new Action(typeof(DogFightAction), $"Dog Fight - No Reason");
                    }

                case SoloDecision.AvoidGrenade:
                    return new Action(typeof(RunToCoverAction), $"Avoid Grenade");

                default:
                    return new Action(typeof(DogFightAction), $"NO DECISION - ERROR IN LOGIC");
            }
        }

        public override bool IsActive()
        {
            bool active = 
                Bot?.BotActive == true &&
                (CurrentDecision == SoloDecision.DogFight ||
                CurrentDecision == SoloDecision.AvoidGrenade);

            setLayer(active);
            return active;
        }

        public override bool IsCurrentActionEnding()
        {
            return Bot?.BotActive == true && _lastActionDecision != CurrentDecision;
        }

        private SoloDecision _lastActionDecision;
        public SoloDecision CurrentDecision => Bot.Decision.CurrentSoloDecision;
    }
}