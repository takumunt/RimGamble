using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimGamble
{
    public class JoyGiver_PlayGachaMachine : JoyGiver_InteractBuildingInteractionCell
    {
        protected override bool CanDoDuringGathering
        {
            get
            {
                return true;
            }
        }

        protected override Job TryGivePlayJob(Pawn pawn, Thing t)
        {
            // Check if the building is a Gahca Machine and has fuel
            CompGachaRefuelable compGachaRefuelable = t.TryGetComp<CompGachaRefuelable>();
            if (compGachaRefuelable != null && !compGachaRefuelable.HasFuel)
            {
                Log.Message("Gacha machine has no fuel.");
                return null;
            }

            return JobMaker.MakeJob(RimGamble_DefOf.RimGamble_PlayGachaMachine, t);
        }
    }
}
