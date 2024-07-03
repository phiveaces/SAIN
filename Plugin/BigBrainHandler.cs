using SPT.Reflection.Patching;
using Comfort.Common;
using Dissonance;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.Layers;
using SAIN.Layers.Combat.Run;
using SAIN.Layers.Combat.Solo;
using SAIN.Layers.Combat.Squad;
using SAIN.Preset.GlobalSettings;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections.Generic;
using System.Reflection;

namespace SAIN
{
    public class BigBrainHandler
    {
        public static void Init()
        {
            BrainAssignment.Init();
        }

        public static bool BigBrainInitialized;

        public class BrainAssignment
        {
            public static void Init()
            {
                handlePMCandRaiders();
                handleScavs();
                handleRogues();
                handleBloodHounds();
                handleBosses();
                handleFollowers();
                handleGoons();
                handleOthers();
            }

            private static void handlePMCandRaiders()
            {
                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                List<string> pmcBrain = new List<string>();
                pmcBrain.Add(Brain.PMC.ToString());

                BrainManager.AddCustomLayer(typeof(DebugLayer), pmcBrain, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), pmcBrain, 80);
                BrainManager.AddCustomLayer(typeof(ExtractLayer), pmcBrain, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), pmcBrain, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), pmcBrain, settings.SAINCombatSoloLayerPriority); 

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Request",
                    "FightReqNull",
                    "PeacecReqNull",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "PtrlBirdEye"
                };
                BrainManager.RemoveLayers(LayersToRemove, pmcBrain);
            }

            private static void handleScavs()
            {
                if (SAINEnabled.VanillaScavs)
                {
                    return;
                }

                List<string> brainList = getBrainList(AIBrains.Scavs);
                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;

                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(DebugLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(ExtractLayer), brainList, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, settings.SAINCombatSoloLayerPriority);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "FightReqNull",
                    "PeacecReqNull",
                    "AssaultHaveEnemy",
                    "Assault Building",
                    "Enemy Building",
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleOthers()
            {
                List<string> brainList = getBrainList(AIBrains.Others);

                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(DebugLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(ExtractLayer), brainList, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, settings.SAINCombatSoloLayerPriority);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Request",
                    "FightReqNull",
                    "PeacecReqNull",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "PtrlBirdEye"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleRogues()
            {
                if (SAINEnabled.VanillaRogues)
                {
                    return;
                }

                List<string> brainList = new List<string>();
                brainList.Add(Brain.ExUsec.ToString());

                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(DebugLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(ExtractLayer), brainList, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, settings.SAINCombatSoloLayerPriority);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Request",
                    "FightReqNull",
                    "PeacecReqNull",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "PtrlBirdEye"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleBloodHounds()
            {
                if (SAINEnabled.VanillaBloodHounds)
                {
                    return;
                }

                List<string> brainList = new List<string>();
                brainList.Add(Brain.ArenaFighter.ToString());

                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(DebugLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(ExtractLayer), brainList, settings.SAINExtractLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, settings.SAINCombatSquadLayerPriority);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, settings.SAINCombatSoloLayerPriority);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Request",
                    "FightReqNull",
                    "PeacecReqNull",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleBosses()
            {
                if (SAINEnabled.VanillaBosses)
                {
                    return;
                }

                List<string> brainList = getBrainList(AIBrains.Bosses);

                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(DebugLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, 70);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, 69);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "BirdEyeFight",
                    "BossBoarFight"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleFollowers()
            {
                if (SAINEnabled.VanillaFollowers)
                {
                    return;
                }

                List<string> brainList = getBrainList(AIBrains.Followers);

                var settings = SAINPlugin.LoadedPreset.GlobalSettings.General;
                //BrainManager.AddCustomLayer(typeof(BotUnstuckLayer), stringList, 98);
                BrainManager.AddCustomLayer(typeof(DebugLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, 70);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, 69);

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "BoarGrenadeDanger"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static void handleGoons()
            {
                if (SAINEnabled.VanillaGoons)
                {
                    return;
                }

                List<string> brainList = getBrainList(AIBrains.Goons);

                BrainManager.AddCustomLayer(typeof(DebugLayer), brainList, 99);
                BrainManager.AddCustomLayer(typeof(SAINAvoidThreatLayer), brainList, 80);
                BrainManager.AddCustomLayer(typeof(CombatSquadLayer), brainList, 64);
                BrainManager.AddCustomLayer(typeof(CombatSoloLayer), brainList, 62); 

                List<string> LayersToRemove = new List<string>
                {
                    "Help",
                    "AdvAssaultTarget",
                    "Hit",
                    "Simple Target",
                    "Pmc",
                    "AssaultHaveEnemy",
                    //"FightReqNull",
                    //"PeacecReqNull",
                    "Assault Building",
                    "Enemy Building",
                    "KnightFight",
                    "BirdEyeFight",
                    "Kill logic"
                };
                BrainManager.RemoveLayers(LayersToRemove, brainList);
            }

            private static List<string> getBrainList(List<Brain> brains)
            {
                List<string> brainList = new List<string>();
                for (int i = 0; i < brains.Count; i++)
                {
                    brainList.Add(brains[i].ToString());
                }
                return brainList;
            }

            private static VanillaBotSettings SAINEnabled => SAINPlugin.LoadedPreset.GlobalSettings.VanillaBots;
        }
    }
}