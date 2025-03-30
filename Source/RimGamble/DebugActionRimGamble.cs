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


    }
}
