using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

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
}
