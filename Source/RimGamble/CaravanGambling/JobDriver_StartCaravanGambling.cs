using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimGamble
{
    public class JobDriver_StartCaravanGambling : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(base.TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            Toil startCaravanGamble = ToilMaker.MakeToil("MakeNewToils");
            startCaravanGamble.initAction = delegate
            {
                Pawn actor = startCaravanGamble.actor;
                DoAction();
            };
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return startCaravanGamble;
        }

        protected void DoAction()
        {
            Find.WindowStack.Add(new Window_CaravanGambling());
        }
    }
}
