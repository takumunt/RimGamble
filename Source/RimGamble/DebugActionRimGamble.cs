using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace RimGamble
{
    public static class DebugActionRimGamble
    {
        [DebugAction("RimGamble", "Spawn Traveling Gambler...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnGamblerWithLetter()
        {
            Map map = Find.CurrentMap;

            List<DebugMenuOption> formOptions = new List<DebugMenuOption>
    {
        new DebugMenuOption("--- Select Gambler Form ---", DebugMenuOptionMode.Action, null)
    };

            formOptions.AddRange(DefDatabase<TravelingGamblerFormKindDef>.AllDefs
                .Select(def => new DebugMenuOption(def.label.CapitalizeFirst(), DebugMenuOptionMode.Action, () =>
                {
                    var selectedForm = def;

                    List<DebugMenuOption> acceptanceOptions = new List<DebugMenuOption>
                    {
                new DebugMenuOption("--- Select Acceptance ---", DebugMenuOptionMode.Action, null)
                    };

                    acceptanceOptions.AddRange(DefDatabase<TravelingGamblerAcceptanceDef>.AllDefs
                        .Select(acc => new DebugMenuOption(acc.label.CapitalizeFirst(), DebugMenuOptionMode.Action, () =>
                        {
                            var selectedAcceptance = acc;

                            List<DebugMenuOption> rejectionOptions = new List<DebugMenuOption>
                            {
                        new DebugMenuOption("--- Select Rejection ---", DebugMenuOptionMode.Action, null)
                            };

                            rejectionOptions.AddRange(DefDatabase<TravelingGamblerRejectionDef>.AllDefs
                                .Select(rej => new DebugMenuOption(rej.label.CapitalizeFirst(), DebugMenuOptionMode.Action, () =>
                                {
                                    var selectedRejection = rej;

                                    List<DebugMenuOption> aggressiveOptions = new List<DebugMenuOption>
                                    {
                                new DebugMenuOption("--- Select Aggressive ---", DebugMenuOptionMode.Action, null)
                                    };

                                    aggressiveOptions.AddRange(DefDatabase<TravelingGamblerAggressiveDef>.AllDefs
                                        .Select(agg => new DebugMenuOption(agg.label.CapitalizeFirst(), DebugMenuOptionMode.Action, () =>
                                        {
                                            var selectedAggressive = agg;

                                            QuestScriptDef questDef = DefDatabase<QuestScriptDef>.GetNamed("TravelingGamblerArrival");

                                            Slate slate = new Slate();
                                            slate.Set("form", selectedForm);
                                            slate.Set("acceptance", selectedAcceptance);
                                            slate.Set("rejection", selectedRejection);
                                            slate.Set("aggressive", selectedAggressive);

                                            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
                                        })));

                                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(aggressiveOptions));
                                })));

                            Find.WindowStack.Add(new Dialog_DebugOptionListLister(rejectionOptions));
                        })));

                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(acceptanceOptions));
                })));

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(formOptions));
        }

        [DebugAction("RimGamble", "Quick Spawn Random Traveling Gambler", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void QuickSpawnRandomGambler()
        {
            try
            {
                QuestScriptDef questDef = DefDatabase<QuestScriptDef>.GetNamed("TravelingGamblerArrival");
                if (questDef != null)
                {
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, new Slate());
                    Messages.Message("Traveling Gambler quest generated!", MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("ERROR: Could not find TravelingGamblerArrival quest script!", MessageTypeDefOf.RejectInput);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimGamble] Error spawning traveling gambler: {ex}");
                Messages.Message($"ERROR: Failed to spawn traveling gambler: {ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }

        [DebugAction("RimGamble", "Test Incident: TravelingGamblerJoin", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TestTravelingGamblerIncident()
        {
            try
            {
                IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed("TravelingGamblerJoin");
                if (incidentDef != null)
                {
                    IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.CurrentMap);
                    
                    // Show current behavior configuration for testing
                    var enabledBehaviors = GetEnabledBehaviors();
                    
                    bool result = incidentDef.Worker.TryExecute(parms);
                    
                    // Show detailed messages for testing
                    if (result)
                    {
                        Messages.Message($"✅ TravelingGamblerJoin spawned! Active behaviors: {enabledBehaviors}", MessageTypeDefOf.PositiveEvent);
                    }
                    else if (Options.RimGamble_Settings.enableTravelingGambler)
                    {
                        // Show helpful failure message with current settings
                        var wealth = Find.CurrentMap?.wealthWatcher?.WealthTotal ?? 0;
                        Messages.Message($"❌ Spawn failed. Wealth: {wealth:N0} (req: {Options.RimGamble_Settings.gamblerMinWealth:N0}). Behaviors: {enabledBehaviors}", MessageTypeDefOf.CautionInput);
                    }
                    // If disabled in settings, show no message (expected behavior)
                }
                else
                {
                    Messages.Message("ERROR: Could not find TravelingGamblerJoin incident!", MessageTypeDefOf.RejectInput);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimGamble] Error testing traveling gambler incident: {ex}");
                Messages.Message($"ERROR: Failed to test incident: {ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }

        [DebugAction("RimGamble", "TEST CURRENT BEHAVIOR SETUP", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void TestCurrentBehaviorSetup()
        {
            try
            {
                var enabledBehaviors = GetEnabledBehaviors();
                var wealth = Find.CurrentMap?.wealthWatcher?.WealthTotal ?? 0;
                var canSpawn = Options.RimGamble_Settings.enableTravelingGambler && wealth >= Options.RimGamble_Settings.gamblerMinWealth;
                
                StringBuilder report = new StringBuilder();
                report.AppendLine("🎯 BEHAVIOR TEST REPORT");
                report.AppendLine($"Traveling Gambler Enabled: {Options.RimGamble_Settings.enableTravelingGambler}");
                report.AppendLine($"Current Wealth: {wealth:N0} / Required: {Options.RimGamble_Settings.gamblerMinWealth:N0}");
                report.AppendLine($"Can Spawn: {(canSpawn ? "✅ YES" : "❌ NO")}");
                report.AppendLine();
                
                if (Options.RimGamble_Settings.enableAcceptanceBehaviors)
                {
                    report.AppendLine("📋 ENABLED ACCEPTANCE BEHAVIORS:");
                    var enabledAcceptance = Options.RimGamble_Settings.GetEnabledAcceptanceBehaviors();
                    if (enabledAcceptance.Any())
                    {
                        foreach (var behavior in enabledAcceptance)
                        {
                            report.AppendLine($"  ✅ {behavior}");
                        }
                    }
                    else
                    {
                        report.AppendLine("  ❌ NONE ENABLED!");
                    }
                }
                else
                {
                    report.AppendLine("❌ ALL ACCEPTANCE BEHAVIORS DISABLED");
                }
                
                if (Options.RimGamble_Settings.enableAggressiveBehaviors)
                {
                    report.AppendLine();
                    report.AppendLine("⚔️ ENABLED AGGRESSIVE BEHAVIORS:");
                    var enabledAggressive = Options.RimGamble_Settings.GetEnabledAggressiveBehaviors();
                    if (enabledAggressive.Any())
                    {
                        foreach (var behavior in enabledAggressive)
                        {
                            report.AppendLine($"  ✅ {behavior}");
                        }
                    }
                    else
                    {
                        report.AppendLine("  ❌ NONE ENABLED!");
                    }
                }
                else
                {
                    report.AppendLine();
                    report.AppendLine("❌ ALL AGGRESSIVE BEHAVIORS DISABLED");
                }
                
                if (Options.RimGamble_Settings.enableRejectionBehaviors)
                {
                    report.AppendLine();
                    report.AppendLine("🚪 ENABLED REJECTION BEHAVIORS:");
                    var enabledRejection = Options.RimGamble_Settings.GetEnabledRejectionBehaviors();
                    if (enabledRejection.Any())
                    {
                        foreach (var behavior in enabledRejection)
                        {
                            report.AppendLine($"  ✅ {behavior}");
                        }
                    }
                    else
                    {
                        report.AppendLine("  ❌ NONE ENABLED!");
                    }
                }
                else
                {
                    report.AppendLine();
                    report.AppendLine("❌ ALL REJECTION BEHAVIORS DISABLED");
                }
                
                report.AppendLine();
                report.AppendLine("🔄 Both 'Test Incident' and natural spawning will now use these same settings!");
                
                Messages.Message(report.ToString(), MessageTypeDefOf.NeutralEvent);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimGamble] Error testing behavior setup: {ex}");
                Messages.Message($"ERROR: Failed to test setup: {ex.Message}", MessageTypeDefOf.RejectInput);
            }
        }
        
        private static string GetEnabledBehaviors()
        {
            var behaviors = new List<string>();
            
            if (Options.RimGamble_Settings.enableAcceptanceBehaviors)
            {
                if (Options.RimGamble_Settings.allowPeacefulDeparture) behaviors.Add("Peaceful");
                if (Options.RimGamble_Settings.allowHumanBomb) behaviors.Add("Bomb");
                if (Options.RimGamble_Settings.allowSabotage) behaviors.Add("Sabotage");
                if (Options.RimGamble_Settings.allowPartyMood) behaviors.Add("Party");
                if (Options.RimGamble_Settings.allowRumorSpread) behaviors.Add("Rumors");
                if (Options.RimGamble_Settings.allowTheftAccept) behaviors.Add("Theft");
                if (Options.RimGamble_Settings.allowTradeCaravan) behaviors.Add("Trade");
                if (Options.RimGamble_Settings.allowDropPod) behaviors.Add("DropPod");
                if (Options.RimGamble_Settings.allowTeachSkill) behaviors.Add("Teach");
                if (Options.RimGamble_Settings.allowGiveInspiration) behaviors.Add("Inspire");
                if (Options.RimGamble_Settings.allowAskJoin) behaviors.Add("Join");
            }
            
            if (behaviors.Count == 0) return "None";
            if (behaviors.Count > 3) return $"{behaviors.Count} behaviors";
            return string.Join(", ", behaviors);
        }


    }
}
