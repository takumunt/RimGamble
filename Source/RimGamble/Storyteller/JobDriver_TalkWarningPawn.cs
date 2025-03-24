using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace RimGamble
{
    public class JobDriver_TalkWarningPawn : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(base.TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            Toil talkToPawn = ToilMaker.MakeToil("MakeNewToils");
            talkToPawn.initAction = delegate
            {
                Pawn actor = talkToPawn.actor;
                DoAction();
            };
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return talkToPawn;
        }

        protected void DoAction()
        {
            Pawn warningPawn = (Pawn)TargetA.Thing;

            Find.WindowStack.Add(new Dialog_MessageBox(
                "The mysterious figure delivers their message. After speaking, they quickly depart.", "OK", () =>
                    {
                        if (warningPawn.GetLord() != null)
                        {
                            warningPawn.GetLord().RemovePawn(warningPawn);
                        }
                        warningPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);

                        if (warningPawn.Spawned)
                        {
                            LordMaker.MakeNewLord(warningPawn.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog), warningPawn.Map).AddPawn(warningPawn);
                        }
                    }
            ));
        }
    }
}
