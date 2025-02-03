using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble.OnlineGambling
{
    public class RimGambleManager : GameComponent
    {
        public static RimGambleManager Instance;

        // list of all current bets
        public List<Bet> bets;
            
        // allows us to generate events periodically
        public override void GameComponentTick()
        {
            // we will generate a new betting event occasionally as long as there are less than 20 bets
            if (Rand.MTBEventOccurs(1f, 60000f, 1f) && bets.Count < 20)
            {
                // when a new betting event occurs
                // randomly pick one of the gambling organizations and generate an event
                var gambleSiteDef = DefDatabase<GambleSiteDef>.AllDefs.RandomElement();

                // now make a new bet using this info
                var bet = new Bet(gambleSiteDef);
                // add it to the list
                bets.Add(bet);
            }

            // Remove or complete the bet once it expires
            for (var i = bets.Count - 1; i >= 0; i--)
            { 
                if (Find.TickManager.TicksGame > bets[i].endTimeInTicks)
                {
                    // evaluate the result of the bet
                    int payout = bets[i].completeBet();
                    if (payout > 0)
                    { 
                        // send in a droppod with the payout
                        givePayout(payout);
                        // also give a mood buff
                        foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
                        {
                            // Check if the pawn has a mood need
                            if (pawn.needs?.mood != null && !pawn.story.traits.HasTrait(TraitDefOf.Ascetic))
                            {
                                // Try adding the "RimGamble_WonWager" thought
                                ThoughtDef thoughtDef = ThoughtDef.Named("RimGamble_WonWager");
                                if (thoughtDef != null)
                                {
                                    pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                                }
                            }
                        }
                    }
                    // a payout of zero means the player lost the wager
                    else if (payout == 0)
                    {
                        // tell the player
                        Find.LetterStack.ReceiveLetter("RimGamble.WagerLost".Translate(), "RimGamble.WagerLostDesc".Translate(), LetterDefOf.NeutralEvent);
                        foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
                        {
                            // Check if the pawn has a mood need
                            if (pawn.needs?.mood != null && !pawn.story.traits.HasTrait(TraitDefOf.Ascetic))
                            {
                                // Try adding the "RimGamble_WonWager" thought
                                ThoughtDef thoughtDef = ThoughtDef.Named("RimGamble_LostWager");
                                if (thoughtDef != null)
                                {
                                    pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                                }
                            }
                        }
                    }
                    // in either case, remove it
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
                // tell the player
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

        // call this method to initialize the list
        public void initialize()
        {
            Instance = this;
            bets = bets ?? new List<Bet>();
        }

        // constructor
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
            Scribe_Collections.Look(ref bets, "bets");
        }
    }
}
