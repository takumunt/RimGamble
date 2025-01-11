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
    public class JoyGiver_PlaySlotMachine : JoyGiver_InteractBuildingInteractionCell
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
            return JobMaker.MakeJob(RimGamble_DefOf.RimGamble_PlaySlotMachine, t);
        }
    }
}
