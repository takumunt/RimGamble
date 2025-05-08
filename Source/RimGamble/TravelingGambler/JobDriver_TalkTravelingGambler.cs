using System.Collections.Generic;
using System;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimGamble
{
    public class JobDriver_TalkTravelingGambler : JobDriver
    {
        private bool notified;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(base.TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = (Action)Delegate.Combine(toil.initAction, (Action)delegate
            {
                if (!notified)
                {
                    notified = true;
                    base.TargetPawnA.GetLord()?.ReceiveMemo("SpokenTo");
                }
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Do(delegate
            {
                Pawn rgPawn = base.TargetPawnA;

                if (rgPawn != null)
                {
                    Pawn_TravelingGamblerTracker gamblerTracker = TravelingGamblerTrackerManager.GetTracker(rgPawn);
                    if (gamblerTracker != null)
                    {
                        gamblerTracker.Notify_TravelingGamblerSpokenTo(pawn);
                    }
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref notified, "notified", defaultValue: false);
        }
    }
}
