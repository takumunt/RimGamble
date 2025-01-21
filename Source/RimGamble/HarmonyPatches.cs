using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using RimWorld;
using Verse.AI;
using System.Reflection;

namespace RimGamble
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("Takumunt.RimGamble");
            harmony.PatchAll();
        }
    }

    /*
     * Patch that modifies the Pawn.GetFloatMenuOptions to allow colonists to gamble with traders
     */
    [HarmonyPatch(typeof(Pawn), "GetFloatMenuOptions")]
    public static class Thing_GetFloatMenuOptions_Patch
    {
        public static void Postfix(Pawn __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (selPawn.IsColonist)
            {
                // Adding custom float menu
                List<FloatMenuOption> modifiedOptions = new List<FloatMenuOption>(__result);

                // Add custom float menu option
                FloatMenuOption option = null;
                // we first check if the pawn we target is a trader
                if (__instance.TraderKind != null)
                {
                    if (!selPawn.CanReach(__instance, PathEndMode.OnCell, Danger.Deadly))
                    {
                        option = new FloatMenuOption(__instance.LabelShort + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                    }
                    else if (selPawn.story != null && selPawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Social))
                    {
                        option = new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate("Social"), null);
                    }
                    else if (selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                    {
                        option = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("RimGamble.GambleWith".Translate() + " " + __instance.LabelShort, delegate
                        {
                            Job job = JobMaker.MakeJob(RimGamble_DefOf.RimGamble_StartCaravanGambling, __instance);
                            job.playerForced = true;
                            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }), selPawn, __instance);
                    }
                    else
                    {
                        option = new FloatMenuOption("RimGamble.CannotGambleWith".Translate() + " " + __instance.LabelShort + ": " + "Incapable".Translate().CapitalizeFirst(), null);
                    }
                    modifiedOptions.Add(option);

                }
                __result = modifiedOptions;
            }
        }
    }
}
