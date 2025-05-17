using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimGamble
{
    public class ChoiceLetter_GamblerJoinDecision : ChoiceLetter
    {
        public string signalAccept;

        public string signalReject;

        public Pawn pawn;

        public override bool CanDismissWithRightClick => false;

        public override bool CanShowInLetterStack
        {
            get
            {
                if (base.CanShowInLetterStack && quest != null)
                {
                    if (quest.State != QuestState.Ongoing)
                    {
                        return quest.State == QuestState.NotYetAccepted;
                    }

                    return true;
                }

                return false;
            }
        }   

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (base.ArchivedOnly)
                {
                    yield return base.Option_Close;
                    yield break;
                }

                DiaOption diaOption = new DiaOption("AcceptTravelingGambler".Translate(pawn.Named("PAWN")));
                DiaOption optionReject = new DiaOption("RejectTravelingGambler".Translate(pawn.Named("PAWN")));
                diaOption.action = delegate
                {
                    Find.SignalManager.SendSignal(new Signal(signalAccept));
                    Find.LetterStack.RemoveLetter(this);
                };
                diaOption.resolveTree = true;

                optionReject.action = delegate
                {
                    Find.SignalManager.SendSignal(new Signal(signalReject));
                    Find.LetterStack.RemoveLetter(this);
                };
                optionReject.resolveTree = true;
                yield return diaOption;
                yield return optionReject;
                if (lookTargets.IsValid())
                {
                    yield return base.Option_JumpToLocationAndPostpone;
                }

                yield return base.Option_Postpone;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref signalAccept, "signalAccept");
            Scribe_Values.Look(ref signalReject, "signalReject");
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
}
