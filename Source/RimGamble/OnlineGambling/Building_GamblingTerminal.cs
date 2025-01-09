using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimGamble
{
    public class Building_GamblingTerminal : Building
    {
        // taken from base game commsconsole (with edits)
        private CompPowerTrader powerComp;

        // taken from base game commsconsole (with edits)
        public bool CanUseTerminalNow
        {
            get
            {
                if (base.Spawned && base.Map.gameConditionManager.ElectricityDisabled(base.Map))
                {
                    return false;
                }

                if (powerComp != null)
                {
                    return powerComp.PowerOn;
                }

                return true;
            }
        }

        // taken from base game commsconsole (with edits)
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        // taken from base game commsconsole (with edits)
        private FloatMenuOption GetFailureReason(Pawn myPawn)
        {
            if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Some))
            {
                return new FloatMenuOption("CannotUseNoPath".Translate(), null);
            }

            if (base.Spawned && base.Map.gameConditionManager.ElectricityDisabled(base.Map))
            {
                return new FloatMenuOption("CannotUseSolarFlare".Translate(), null);
            }

            if (powerComp != null && !powerComp.PowerOn)
            {
                return new FloatMenuOption("CannotUseNoPower".Translate(), null);
            }

            if (!myPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
            {
                return new FloatMenuOption("CannotUseReason".Translate("IncapableOfCapacity".Translate(PawnCapacityDefOf.Talking.label, myPawn.Named("PAWN"))), null);
            }

            if (!CanUseTerminalNow)
            {
                Log.Error(string.Concat(myPawn, " could not use comm console for unknown reason."));
                return new FloatMenuOption("Cannot use now", null);
            }
            return null;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            FloatMenuOption failureReason = GetFailureReason(myPawn);
            if (failureReason != null)
            {
                yield return failureReason;
                yield break;
            }

            // custom gambling menu option
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ConnectNetworkGamble".Translate(), 
                () => OpenOnlineGamblingMenuJob(myPawn), MenuOptionPriority.Default), myPawn, this); ;
        }

        // opens up the online gambling menu
        private void OpenOnlineGamblingMenuJob(Pawn myPawn)
        {
            if (myPawn.IsColonistPlayerControlled)
            {
                // use my own jobdef
                Job job = JobMaker.MakeJob(RimGamble_DefOf.RimGamble_StartOnlineGambling, this);
                myPawn.jobs.TryTakeOrderedJob(job);
            }
        }
    }
}
