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
                    option = (!selPawn.CanReach(__instance, PathEndMode.OnCell, Danger.Deadly)) ? new FloatMenuOption("RimGamble.CannotGambleWith".Translate(__instance) + ": " + "NoPath".Translate().CapitalizeFirst(), null) : (selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("RimGamble.GambleWith".Translate(__instance), delegate
                    {
                        Job job = JobMaker.MakeJob(RimGamble_DefOf.RimGamble_StartCaravanGambling, __instance);
                        job.playerForced = true;
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }), selPawn, __instance) : new FloatMenuOption("RimGamble.CannotGambleWith".Translate(__instance) + ": " + "Incapable".Translate().CapitalizeFirst(), null));
                    modifiedOptions.Add(option);
                }

                // check if the pawn is a warningpawn
                if (__instance.kindDef == PawnKindDef.Named("RimGamble_WarningPawn"))
                {
                    option = (!selPawn.CanReach(__instance, PathEndMode.OnCell, Danger.Deadly)) ? new FloatMenuOption("CannotTalkTo".Translate(__instance) + ": " + "NoPath".Translate().CapitalizeFirst(), null) : (selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TalkTo".Translate(__instance), delegate
                    {
                        Job job = JobMaker.MakeJob(RimGamble_DefOf.RimGamble_TalkWarningPawn, __instance);
                        job.playerForced = true;
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }), selPawn, __instance) : new FloatMenuOption("CannotTalkTo".Translate(__instance) + ": " + "Incapable".Translate().CapitalizeFirst(), null));
                    modifiedOptions.Add(option);
                }

                __result = modifiedOptions;
            }
        }
    }
}
