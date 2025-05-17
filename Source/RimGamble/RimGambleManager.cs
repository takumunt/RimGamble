using RimGamble;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using static UnityEngine.GraphicsBuffer;

public class RimGambleManager : GameComponent
{
    public static RimGambleManager Instance;

    // List of current bets
    public List<Bet> bets;

    // Follow-up event tracking
    private List<WarningData> warningEventTick; // Tick when the warning event happened

    private List<DelayedRaidEntry> delayedRaids = new List<DelayedRaidEntry>();

    private List<DelayedSabotageEntry> sabotageEntries = new List<DelayedSabotageEntry>();

    private List<DelayedTradeCaravanEntry> delayedTradeCaravans = new List<DelayedTradeCaravanEntry>();

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

        // Delayed raids
        for (int i = delayedRaids.Count - 1; i >= 0; i--)
        {
            var entry = delayedRaids[i];
            var tracker = TravelingGamblerTrackerManager.GetTracker(entry.pawn);

            if (entry.pawn == null || tracker == null || tracker.Disabled || tracker.Pawn.Dead || tracker.Pawn.DestroyedOrNull())
            {
                delayedRaids.RemoveAt(i);
                continue;
            }

            // Check if pawn has left and delay has passed
            bool pawnLeft = tracker.Pawn.MapHeld == null || tracker.Pawn.Dead || !tracker.Pawn.Spawned;
            if (pawnLeft && Find.TickManager.TicksGame >= entry.triggerAfterTick)
            {
                // Show custom raid letter
                TaggedString label = entry.raidLetterLabel.Formatted(entry.pawn.Named("PAWN"));
                TaggedString desc = entry.raidLetterDesc.Formatted(entry.pawn.Named("PAWN"));
                Find.LetterStack.ReceiveLetter(label, desc, entry.raidLetterDef ?? LetterDefOf.ThreatBig);

                // Trigger the raid
                tracker.DoRaid(entry.faction);
                delayedRaids.RemoveAt(i);
            }
        }

        // Delayed sabotage
        for (int i = sabotageEntries.Count - 1; i >= 0; i--)
        {
            var entry = sabotageEntries[i];
            if (Find.TickManager.TicksGame >= entry.triggerTick)
            {
                foreach (var thing in entry.targets)
                {
                    var building = thing as Building;
                    if (building == null || !building.Spawned) continue;

                    var power = building.TryGetComp<CompPowerTrader>();
                    var breakdown = building.TryGetComp<CompBreakdownable>();

                    if (power != null)
                    {
                        breakdown?.DoBreakdown();
                    }
                    else
                    {
                        FireUtility.TryStartFireIn(building.Position, building.Map, 0.5f, null);
                    }
                }

                Find.LetterStack.ReceiveLetter(entry.label, entry.desc, LetterDefOf.NegativeEvent);
                sabotageEntries.RemoveAt(i);
            }
        }

        // Delayed trade caravans
        for (int i = delayedTradeCaravans.Count - 1; i >= 0; i--)
        {
            var entry = delayedTradeCaravans[i];

            if (Find.TickManager.TicksGame >= entry.triggerAfterTick)
            {
                if (entry.map != null && entry.faction != null && entry.traderKind != null)
                {
                    IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(entry.pawn.Position, entry.pawn.Map, 10);

                    IncidentParms parms = new IncidentParms
                    {
                        target = entry.map,
                        faction = entry.faction,
                        traderKind = entry.traderKind,
                        spawnCenter = spawnCell,
                        forced = true,
                        points = 400f
                    };

                    IncidentDefOf.TraderCaravanArrival.Worker.TryExecute(parms);
                }

                delayedTradeCaravans.RemoveAt(i);
            }
        }

    }

    public void QueueDelayedRaid(Pawn pawn, Faction faction)
    {
        var tracker = TravelingGamblerTrackerManager.GetTracker(pawn);
        var def = tracker?.aggressive;

        int delayTicks = def?.raidDelayTicks ?? 0;
        int triggerAt = Find.TickManager.TicksGame + delayTicks;

        delayedRaids.Add(new DelayedRaidEntry
        {
            pawn = pawn,
            faction = faction,
            triggerAfterTick = triggerAt,
            raidLetterLabel = def?.raidLetterLabel,
            raidLetterDesc = def?.raidLetterDesc,
            raidLetterDef = def?.raidLetterDef
        });
    }

    public void QueueDelayedSabotage(Pawn pawn)
    {
        var tracker = TravelingGamblerTrackerManager.GetTracker(pawn);
        var def = tracker?.acceptance;
        if (tracker == null || def == null || tracker.sabotageTargets.NullOrEmpty()) return;

        sabotageEntries.Add(new DelayedSabotageEntry
        {
            targets = tracker.sabotageTargets,
            triggerTick = Find.TickManager.TicksGame + def.sabotageDelayTicks,
            label = def.sabotageResultLetterLabel?.Translate(pawn.Named("PAWN")) ?? "Sabotage!",
            desc = def.sabotageResultLetterDesc?.Translate(pawn.Named("PAWN")) ?? "Something was sabotaged."
        });
    }

    public void QueueDelayedTradeCaravan(Pawn pawn, Faction faction, TraderKindDef traderKind, int delayTicks)
    {
        if (pawn?.Map == null || pawn.Destroyed) return;

        int triggerTick = Find.TickManager.TicksGame + delayTicks;

        delayedTradeCaravans.Add(new DelayedTradeCaravanEntry
        {
            pawn = pawn,
            faction = faction,
            map = pawn.Map,
            traderKind = traderKind,
            triggerAfterTick = triggerTick
        });
    }

    private void givePayout(int payout)
    {
        if (Find.CurrentMap != null)
        {
            Thing silverStack = ThingMaker.MakeThing(ThingDefOf.Silver);
            silverStack.stackCount = payout;
            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            TradeUtility.SpawnDropPod(dropSpot, Find.CurrentMap, silverStack);
            Find.LetterStack.ReceiveLetter("RimGamble.PayoutArrived".Translate(), "RimGamble.TravelingGamblerDropPodDesc".Translate(), LetterDefOf.PositiveEvent, new TargetInfo(dropSpot, Find.CurrentMap));
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

    public void ApplyThoughtToColony(string thoughtDefName, Predicate<Pawn> filter = null)
    {
        ThoughtDef thoughtDef = ThoughtDef.Named(thoughtDefName);
        if (thoughtDef == null)
        {
            Log.Warning($"[RimGamble] ThoughtDef '{thoughtDefName}' not found.");
            return;
        }

        if (Find.CurrentMap == null) return;

        foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
        {
            if (pawn.needs?.mood == null || pawn.Dead) continue;
            if (filter != null && !filter(pawn)) continue;

            pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
        }
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
        Scribe_Collections.Look(ref delayedRaids, "delayedRaids", LookMode.Deep);
        Scribe_Collections.Look(ref sabotageEntries, "sabotageEntries", LookMode.Deep);
        Scribe_Collections.Look(ref delayedTradeCaravans, "delayedTradeCaravans", LookMode.Deep);
    }
}
