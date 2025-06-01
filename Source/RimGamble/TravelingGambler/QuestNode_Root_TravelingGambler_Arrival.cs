using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace RimGamble
{
    public class QuestNode_Root_TravelingGambler_Arrival : QuestNode
    {
        private const int TimeoutTicks = 60000;

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Pawn pawn = SpawnPawn(quest.points);
            Pawn_TravelingGamblerTracker travelinggambler = pawn.GetTravelingGamblerTracker();
            slate.Set("pawn", pawn);
            SendLetter(pawn);
            string text = QuestGenUtility.HardcodedSignalWithQuestID("Accept");
            string text2 = QuestGenUtility.HardcodedSignalWithQuestID("Reject");
            string text3 = QuestGenUtility.HardcodedSignalWithQuestID("Capture");
            string text4 = QuestGenUtility.HardcodedSignalWithQuestID("SpokeTo");
            string text5 = QuestGenUtility.HardcodedSignalWithQuestID("Timeout");
            string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("pawn.Killed");
            string text6 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.TookDamageFromPlayer");
            string inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.LeftMap");
            string inSignal3 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.Recruited");
            travelinggambler.quest = quest;
            travelinggambler.spokeToSignal = text4;
            travelinggambler.timeoutAt = GenTicks.TicksAbs + 60000;
            QuestPart_TravelingGamblerOutcomes part = new QuestPart_TravelingGamblerOutcomes
            {
                pawn = pawn,
                timeout = 60000,
                signalAccept = text,
                signalReject = text2,
                signalCapture = text3,
                signalAttacked = text6,
                signalShow = text4,
                signalTimeout = text5,
                signalListenMode = QuestPart.SignalListenMode.Always
            };
            quest.AddPart(part);
            quest.Signal(text, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Success);
            });
            quest.Signal(text2, delegate
            {
                quest.GiveDiedOrDownedThoughts(pawn, PawnDiedOrDownedThoughtsKind.DeniedJoining);
                QuestGen_End.End(quest, QuestEndOutcome.Fail);
            });
            quest.Signal(text3, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
            quest.Signal(text6, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
            quest.Signal(inSignal, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
            quest.Signal(inSignal2, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
            quest.Signal(inSignal3, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
            quest.Delay(60000, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Fail);
            }, null, null, text5);
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }

        private Pawn SpawnPawn(float combatPoints = 0f)
        {
            Slate slate = QuestGen.slate;
            if (slate.TryGet<TravelingGamblerFormKindDef>("form", out var var) && slate.TryGet<TravelingGamblerAggressiveDef>("aggressive", out var var2) && slate.TryGet<TravelingGamblerRejectionDef>("rejection", out var var3) && slate.TryGet<TravelingGamblerAcceptanceDef>("acceptance", out var var4))
            {
                return TravelingGamblerUtility.GenerateAndSpawn(var, var2, var3, var4, QuestGen_Get.GetMap());
            }

            return TravelingGamblerUtility.GenerateAndSpawn(QuestGen_Get.GetMap(), combatPoints);
        }

        private void SendLetter(Pawn pawn)
        {
            string text = pawn.GetKindLabelSingular().CapitalizeFirst();
            TaggedString taggedString = "RimGamble.LetterTravelingGamblerInviteStart".Translate(pawn.Named("PAWN")).CapitalizeFirst();
            ChoiceLetter let = LetterMaker.MakeLetter(text: taggedString + ("\n\n" + "RimGamble.LetterTravelingGamblerAppearedAppended".Translate(pawn.Named("PAWN")).CapitalizeFirst()), label: text, def: LetterDefOf.NeutralEvent, lookTargets: pawn);
            Find.LetterStack.ReceiveLetter(let);
        }
    }
}
