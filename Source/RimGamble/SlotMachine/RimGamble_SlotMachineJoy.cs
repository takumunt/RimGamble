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
            return JobMaker.MakeJob(this.def.jobDef, t);
        }
    }

    public class JobDriver_PlaySlotMachine : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            bool result = this.pawn.Reserve(this.job.targetA, this.job, this.job.def.joyMaxParticipants, 0, null, errorOnFailed);
            return result;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Ensure the target is valid
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);
            // Go to the slot machine
            yield return Toils_Goto.GotoCell(TargetThingA.InteractionCell, PathEndMode.OnCell);

            // Custom toil for gambling
            Toil playSlotToil = new Toil();
            playSlotToil.initAction = () =>
            {
                // Make the pawn face the slot machine
                this.pawn.rotationTracker.FaceTarget(TargetThingA.InteractionCell);
                this.job.locomotionUrgency = LocomotionUrgency.Walk;
            };
            playSlotToil.tickAction = delegate ()
            {
                JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.EndJob, 1f, (Building)base.TargetThingA);
            };

            playSlotToil.defaultCompleteMode = ToilCompleteMode.Delay; // Wait for a specified duration
            playSlotToil.defaultDuration = 2500; // Duration in ticks
            playSlotToil.AddFinishAction(() =>
            {
                JoyUtility.TryGainRecRoomThought(this.pawn);
            });

            yield return playSlotToil;
        }
    }
}