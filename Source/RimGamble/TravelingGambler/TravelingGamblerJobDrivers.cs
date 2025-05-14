using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimGamble
{
    public class JobDriver_LeaveAfterSabotage : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil leaveToil = new Toil();
            leaveToil.initAction = () =>
            {
                try
                {
                    var tracker = TravelingGamblerTrackerManager.GetTracker(pawn);
                    if (tracker != null)
                    {
                        tracker.DoLeave();
                    }
                    else
                    {
                        Log.Warning("[RimGamble] Tracker was null.");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error("[RimGamble] Error in DoLeave: " + ex);
                }

                ReadyForNextToil(); // marks this toil as done
            };

            leaveToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return leaveToil;
        }
    }

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

    public class JobDriver_StealSilver : JobDriver
    {
        public int totalSilverEstimate = 999999;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Pawn pawn = this.pawn;
            Thing targetSilver = job.targetA.Thing;

            this.FailOnDespawnedOrNull(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            yield return Toils_General.Wait(180).WithProgressBarToilDelay(TargetIndex.A);

            yield return new Toil
            {
                initAction = () =>
                {
                    var tracker = TravelingGamblerTrackerManager.GetTracker(pawn);
                    if (tracker != null)
                    {
                        int stolen = tracker.DoTheft(job.count); // Use passed value
                        if (stolen == 0)
                        {
                            tracker.SetAlternativeLetter(true);
                        }
                        tracker.DoLeave();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }

    public class JobDriver_SpawnTradeCaravan : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil leaveToil = new Toil();
            leaveToil.initAction = () =>
            {
                try
                {
                    var tracker = TravelingGamblerTrackerManager.GetTracker(pawn);
                    if (tracker != null)
                    {
                        tracker.DoLeave();
                    }
                    else
                    {
                        Log.Warning("[RimGamble] Tracker was null.");
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error("[RimGamble] Error in DoLeave: " + ex);
                }

                ReadyForNextToil(); // marks this toil as done
            };

            leaveToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return leaveToil;
        }
    }
}
