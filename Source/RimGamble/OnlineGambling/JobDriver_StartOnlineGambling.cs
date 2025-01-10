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
    public class JobDriver_StartOnlineGambling : JobDriver
    {

        public Building_GamblingTerminal GamblingTerminal => this.TargetA.Thing as Building_GamblingTerminal;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        // taken from base game comms console jobdriver
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn((Toil to) => !GamblingTerminal.CanUseTerminalNow);
            Toil openGamblingTerminal = new Toil();
            openGamblingTerminal.initAction = delegate
            {
                Pawn actor = openGamblingTerminal.actor;
                DoAction(actor);
            };
            yield return openGamblingTerminal;
        }

        protected void DoAction(Pawn actor)
        {
            Find.WindowStack.Add(new Window_OnlineGambling());
        }
    }
}
