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
                    bool result = incidentDef.Worker.TryExecute(parms);
                    Messages.Message($"TravelingGamblerJoin incident executed: {result}", result ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput);
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


    }
}
