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
    public class JobDriver_PlayGachaMachine : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn == null)
            {
                Log.Error("TryMakePreToilReservations failed because the pawn is null.");
                return false;
            }

            LocalTargetInfo target = job.GetTarget(TargetIndex.A);

            if (!target.IsValid)
            {
                Log.Warning($"{pawn.Name} attempted to reserve an invalid target for job {job.def.defName}");
                return false;
            }

            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
            {
                Log.Warning($"{pawn.Name} failed to reserve {target.Label} for job {job.def.defName}");
                return false;
            }

            return true;
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
                //JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.EndJob, 1f, (Building)base.TargetThingA);
                this.pawn.rotationTracker.FaceTarget(TargetThingA.InteractionCell);
            };

            playSlotToil.defaultDuration = 1000; // Duration in ticks
            playSlotToil.defaultCompleteMode = ToilCompleteMode.Delay; // Wait for a specified duration

            playSlotToil.AddFinishAction(() =>
            {
                Building building = TargetThingA as Building;
                if (building != null)
                {
                    CompGachaRefuelable compGachaRefuelable = building.GetComp<CompGachaRefuelable>();
                    if (compGachaRefuelable != null && compGachaRefuelable.HasFuel)
                    {
                        compGachaRefuelable.ConsumeFuel(1);

                        Need_Joy joyNeed = this.pawn.needs.TryGetNeed<Need_Joy>();
                        if (joyNeed != null)
                        {
                            joyNeed.GainJoy(0.55f, this.job.def.joyKind);
                        }
                        else
                        {
                            Log.Warning($"{pawn.Name} does not have a Joy need.");
                        }
                    }
                    else
                    {
                        Log.Message("The machine does not have fuel or is not refuelable.");
                    }
                }
                else
                {
                    Log.Warning("The target is not a building.");
                }
                //JoyUtility.TryGainRecRoomThought(this.pawn);
            });

            yield return playSlotToil;
        }
    }
}
