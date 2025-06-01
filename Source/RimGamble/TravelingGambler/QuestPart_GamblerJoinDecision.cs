using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace RimGamble
{
    public class QuestPart_GamblerJoinDecision : QuestPart
    {
        public Pawn pawn;

        public string signalAccept;

        public string signalReject;

        public string signalAttacked;

        public string signalShow;

        public string signalTimeout;

        public int timeout;

        private ChoiceLetter_GamblerJoinDecision letter;

        public void ShowOfferLetter()
        {
            if (letter != null)
            {
                letter.OpenLetter();
                return;
            }

            TaggedString label = "RimGamble.LetterTravelingGamblerInviteJoinsAccept".Translate(pawn.Named("PAWN"));
            TaggedString text = "RimGamble.LetterTravelingGamblerInviteStartAccept".Translate(pawn.Named("PAWN")).CapitalizeFirst();
            letter = (ChoiceLetter_GamblerJoinDecision)LetterMaker.MakeLetter(label, text, RimGamble_LetterDefOf.RimGamble_GamblerJoinDecision, null, quest);
            letter.signalAccept = signalAccept;
            letter.signalReject = signalReject;
            letter.pawn = pawn;
            Find.LetterStack.ReceiveLetter(letter);
            letter.OpenLetter();
        }

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            if (signal.tag == signalShow)
            {
                ShowOfferLetter();
            }

            Pawn_TravelingGamblerTracker pawn_TravelingGamblerTracker = TravelingGamblerTrackerManager.GetTracker(pawn);
            if (pawn_TravelingGamblerTracker != null)
            {
                if (signal.tag == signalShow)
                {
                    ShowOfferLetter();
                }
                else if (signal.tag == signalAttacked)
                {
                    signal.args.TryGetArg("INSTIGATOR", out Pawn arg);
                    pawn_TravelingGamblerTracker.Notify_TravelingGamblerAttacked(arg);
                }
                else if (signal.tag == signalAccept)
                {
                    List<TargetInfo> list = new List<TargetInfo> { pawn };
                    List<NamedArgument> list2 = new List<NamedArgument> { pawn.Named("PAWN") };
                    TaggedString label = "RimGamble.LetterTravelingGamblerJoinColonyLabelAccept".Translate(pawn.Named("PAWN"));
                    TaggedString text = "RimGamble.LetterTravelingGamblerJoinColonyDescAccept".Translate(pawn.Named("PAWN"));
                    Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, list);
                    pawn_TravelingGamblerTracker.JoinColony();
                }
                else if (signal.tag == signalReject)
                {
                    List<TargetInfo> list = new List<TargetInfo> { pawn };
                    List<NamedArgument> list2 = new List<NamedArgument> { pawn.Named("PAWN") };
                    TaggedString label = "RimGamble.LetterTravelingGamblerJoinColonyLabelReject".Translate(pawn.Named("PAWN"));
                    TaggedString text = "RimGamble.LetterTravelingGamblerJoinColonyDescReject".Translate(pawn.Named("PAWN"));
                    Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, list);
                    pawn_TravelingGamblerTracker.DoLeave();
                }
                else if (signal.tag == signalTimeout)
                {
                    List<TargetInfo> list = new List<TargetInfo> { pawn };
                    List<NamedArgument> list2 = new List<NamedArgument> { pawn.Named("PAWN") };
                    TaggedString label = "RimGamble.LetterTravelingGamblerJoinColonyLabelTimeout".Translate(pawn.Named("PAWN"));
                    TaggedString text = "RimGamble.LetterTravelingGamblerJoinColonyDescTimeout".Translate(pawn.Named("PAWN"));
                    Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, list);
                    pawn_TravelingGamblerTracker.DoLeave();
                }
            }
        }

        public override void Cleanup()
        {
            CloseLetter();
        }

        private void CloseLetter()
        {
            if (letter != null && Find.LetterStack.LettersListForReading.Contains(letter))
            {
                Find.LetterStack.RemoveLetter(letter);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref letter, "letter");
            Scribe_Values.Look(ref signalAccept, "signalAccept");
            Scribe_Values.Look(ref signalReject, "signalReject");
            Scribe_Values.Look(ref signalAttacked, "signalAttacked");
            Scribe_Values.Look(ref signalTimeout, "signalTimeout");
            Scribe_Values.Look(ref signalShow, "signalShow");
            Scribe_Values.Look(ref timeout, "timeout", 0);
        }
    }
}
