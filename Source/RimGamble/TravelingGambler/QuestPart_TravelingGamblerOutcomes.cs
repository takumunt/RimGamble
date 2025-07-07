using RimWorld;
using Verse;

namespace RimGamble
{
    public class QuestPart_TravelingGamblerOutcomes : QuestPart
    {
        public Pawn pawn;

        public string signalAccept;

        public string signalCapture;

        public string signalReject;

        public string signalAttacked;

        public string signalShow;

        public string signalTimeout;

        public int timeout;

        private ChoiceLetter_AcceptTravelingGambler letter;

        public void ShowOfferLetter(Pawn_TravelingGamblerTracker travelinggambler)
        {
            if (letter != null)
            {
                letter.OpenLetter();
                return;
            }

            TaggedString label = "RimGamble.LetterTravelingGamblerInviteJoins".Translate(pawn.Named("PAWN"));
            TaggedString text = travelinggambler.form.letterPrompt.Translate(pawn.Named("PAWN")).CapitalizeFirst();
            text += "\n\n" + "RimGamble.LetterTravelingGamblerInviteAppend".Translate(pawn.Named("PAWN")).CapitalizeFirst();
            letter = (ChoiceLetter_AcceptTravelingGambler)LetterMaker.MakeLetter(label, text, RimGamble_LetterDefOf.RimGamble_AcceptTravelingGambler, null, quest);
            letter.signalAccept = signalAccept;
            letter.signalCapture = signalCapture;
            letter.signalReject = signalReject;
            letter.pawn = pawn;
            letter.speaker = travelinggambler.speaker;
            Find.LetterStack.ReceiveLetter(letter);
            letter.OpenLetter();
        }

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            Pawn_TravelingGamblerTracker pawn_TravelingGamblerTracker = TravelingGamblerTrackerManager.GetTracker(pawn);
            if (pawn_TravelingGamblerTracker != null)
            {
                if (signal.tag == signalShow)
                {
                    ShowOfferLetter(pawn_TravelingGamblerTracker);
                }
                else if (signal.tag == signalAttacked)
                {
                    signal.args.TryGetArg("INSTIGATOR", out Pawn arg);
                    pawn_TravelingGamblerTracker.Notify_TravelingGamblerAttacked(arg);
                }
                else if (signal.tag == signalAccept)
                {
                    pawn_TravelingGamblerTracker.AcceptTravleingGambler();
                }
                else if (signal.tag == signalReject)
                {
                    pawn_TravelingGamblerTracker.Notify_TravelingGamblerRejected();
                }
                else if (signal.tag == signalTimeout)
                {
                    pawn_TravelingGamblerTracker.Notify_TravelingGamblerRejected();
                }
                else if (signal.tag == signalCapture)
                {
                    CaptureUtility.OrderArrest(pawn_TravelingGamblerTracker.speaker, pawn);
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
            Scribe_Values.Look(ref signalCapture, "signalCapture");
            Scribe_Values.Look(ref signalReject, "signalReject");
            Scribe_Values.Look(ref signalAttacked, "signalAttacked");
            Scribe_Values.Look(ref signalTimeout, "signalTimeout");
            Scribe_Values.Look(ref signalShow, "signalShow");
            Scribe_Values.Look(ref timeout, "timeout", 0);
        }
    }
}
