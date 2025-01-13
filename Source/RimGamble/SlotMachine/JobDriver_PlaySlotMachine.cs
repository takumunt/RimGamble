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
    public class JobDriver_PlaySlotMachine : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, this.job.def.joyMaxParticipants, 0, null, errorOnFailed);
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
            playSlotToil.defaultDuration = job.def.joyDuration; // Duration in ticks
            playSlotToil.AddFinishAction(() =>
            {
                JoyUtility.TryGainRecRoomThought(this.pawn);
            });

            yield return playSlotToil;
        }

        public override object[] TaleParameters()
        {
            return new object[2]
            {
            pawn,
            base.TargetA.Thing.def
            };
        }
    }
}
