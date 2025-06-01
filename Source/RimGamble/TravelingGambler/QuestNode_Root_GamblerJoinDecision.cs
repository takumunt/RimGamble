using RimWorld.QuestGen;
using RimWorld;
using Verse;

namespace RimGamble
{
    public class QuestNode_Root_GamblerJoinDecision : QuestNode
    {
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            if (!slate.TryGet("pawn", out Pawn pawn) || pawn == null)
            {
                Log.Error("[Gambler] GamblerJoinDecision quest triggered without a pawn.");
                return;
            }

            Pawn_TravelingGamblerTracker travelinggambler = pawn.GetTravelingGamblerTracker();

            string acceptSignal = QuestGenUtility.HardcodedSignalWithQuestID("JoinAccept");
            string rejectSignal = QuestGenUtility.HardcodedSignalWithQuestID("JoinReject");
            string timeoutSignal = QuestGenUtility.HardcodedSignalWithQuestID("JoinTimeout");
            string showSignal = QuestGenUtility.HardcodedSignalWithQuestID("JoinShow");
            string killedSignal = QuestGenUtility.HardcodedSignalWithQuestID("pawn.Killed");
            string tookDamageSignal = QuestGenUtility.HardcodedSignalWithQuestID("pawn.TookDamageFromPlayer");
            string leftMapSignal = QuestGenUtility.HardcodedSignalWithQuestID("pawn.LeftMap");
            
            travelinggambler.quest = quest;
            travelinggambler.timeoutAt = GenTicks.TicksAbs + 60000;

            QuestPart_GamblerJoinDecision part = new QuestPart_GamblerJoinDecision
            {
                pawn = pawn,
                timeout = 60000,
                signalAccept = acceptSignal,
                signalReject = rejectSignal,
                signalShow = showSignal,
                signalAttacked = tookDamageSignal,
                signalTimeout = timeoutSignal,
                signalListenMode = QuestPart.SignalListenMode.Always
            };
            quest.AddPart(part);

            QuestPart_Pass showTrigger = new QuestPart_Pass
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID("Initiate"),
                outSignal = showSignal
            };
            quest.AddPart(showTrigger);

            quest.Signal(acceptSignal, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Success);
            });
            quest.Signal(rejectSignal, delegate
            {
                quest.GiveDiedOrDownedThoughts(pawn, PawnDiedOrDownedThoughtsKind.DeniedJoining);
                QuestGen_End.End(quest, QuestEndOutcome.Fail);
            });
            quest.Signal(tookDamageSignal, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
            quest.Signal(killedSignal, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
            quest.Signal(leftMapSignal, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            });
            quest.Delay(60000, delegate
            {
                QuestGen_End.End(quest, QuestEndOutcome.Fail);
            }, null, null, timeoutSignal);
        }

        protected override bool TestRunInt(Slate slate) => true;
    }
}
