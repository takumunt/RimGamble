using RimGamble;
using RimWorld;
using System.Collections.Generic;
using Verse;
using static UnityEngine.GraphicsBuffer;

public class RimGambleManager : GameComponent
{
    public static RimGambleManager Instance;

    // List of current bets
    public List<Bet> bets;

    // Follow-up event tracking
    private List<WarningData> warningEventTick; // Tick when the warning event happened


    /**
     * 
     */
    public override void GameComponentTick()
    {
        // Handle betting events
        if (Rand.MTBEventOccurs(1f, 60000f, 1f) && bets.Count < 20)
        {
            var gambleSiteDef = DefDatabase<GambleSiteDef>.AllDefs.RandomElement();
            var bet = new Bet(gambleSiteDef);
            bets.Add(bet);
        }

        // Handle bets expiring
        for (var i = bets.Count - 1; i >= 0; i--)
        {
            if (Find.TickManager.TicksGame > bets[i].endTimeInTicks)
            {
                int payout = bets[i].completeBet();
                if (payout > 0)
                {
                    givePayout(payout);
                    foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
                    {
                        if (pawn.needs?.mood != null && !pawn.story.traits.HasTrait(TraitDefOf.Ascetic))
                        {
                            ThoughtDef thoughtDef = ThoughtDef.Named("RimGamble_WonWager");
                            if (thoughtDef != null)
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                            }
                        }
                    }
                }
                else if (payout == 0)
                {
                    Find.LetterStack.ReceiveLetter("RimGamble.WagerLost".Translate(), "RimGamble.WagerLostDesc".Translate(), LetterDefOf.NeutralEvent);
                    foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
                    {
                        if (pawn.needs?.mood != null && !pawn.story.traits.HasTrait(TraitDefOf.Ascetic))
                        {
                            ThoughtDef thoughtDef = ThoughtDef.Named("RimGamble_LostWager");
                            if (thoughtDef != null)
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                            }
                        }
                    }
                }
                bets.RemoveAt(i);
            }
        }
    }

    private void givePayout(int payout)
    {
        if (Find.CurrentMap != null)
        {
            Thing silverStack = ThingMaker.MakeThing(ThingDefOf.Silver);
            silverStack.stackCount = payout;
            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            TradeUtility.SpawnDropPod(dropSpot, Find.CurrentMap, silverStack);
            Find.LetterStack.ReceiveLetter("RimGamble.PayoutArrived".Translate(), "RimGamble.PayoutArrivedDesc".Translate(), LetterDefOf.PositiveEvent, new TargetInfo(dropSpot, Find.CurrentMap));
        }
        else
        {
            Log.Error("No active map found to give payout. Payout has not been made.");
        }
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        initialize();
    }

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        initialize();
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        initialize();
    }

    public void initialize()
    {
        Instance = this;
        bets = bets ?? new List<Bet>();
        warningEventTick = warningEventTick ?? new List<WarningData>();
    }

    public void SetWarningEvent(int delayTick)
    {
        warningEventTick.Add(new WarningData(Find.TickManager.TicksGame, delayTick));
    }

    public int CheckIfMagEventFire()
    {
        int eventsToFire = 0;

        for (int i = 0; i < warningEventTick.Count; i++)
        {
            var currentTick = warningEventTick[i];
            if (currentTick.warningEventTick < Find.TickManager.TicksGame)
            {
                eventsToFire++;
                warningEventTick.RemoveAt(i);
                i--; 
            }
        }

        return eventsToFire;
    }

    public RimGambleManager()
    {
        Instance = this;
    }

    public RimGambleManager(Game game)
    {
        Instance = this;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref bets, "bets", LookMode.Deep);
        Scribe_Collections.Look(ref warningEventTick, "warningEventTick", LookMode.Deep);
    }
}