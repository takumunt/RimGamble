using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RimGamble.OnlineGambling;
using RimWorld;
using UnityEngine;
using Verse;
using static RimWorld.IdeoFoundation_Deity;

namespace RimGamble
{
    /*
     * Custom window class for the online gambling window
     */
    public class Window_OnlineGambling : Window
    {
        public Window_OnlineGambling()
        {
            silverFinder(Find.CurrentMap);
            stakeBufferReset();
            this.forcePause = true;
            this.closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(UI.screenWidth * 0.6f, UI.screenHeight * 0.6f);

        public static Vector2 scrollPos;
        public static List<Thing> silverList;
        public override void DoWindowContents(Rect inRect)
        {
            Vector2 pos = Vector2.zero;

            // title of the window  
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 30), "RimGamble.OnlineGamblingNetwork".Translate());

            // description of the window
            pos.y += 30;
            Text.Font = GameFont.Small;
            GUI.color = Color.grey;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width, 60), "RimGamble.OnlineGamblingNetworkDesc".Translate());

            /*
             * Coumn Headers
             */
            pos.y += 60;
            GUI.color = Color.white;

            // Gambling site name
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 24), "RimGamble.GamblingSiteName".Translate());
            pos.x += inRect.width * 0.2f;

            // Gambling type
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 24), "RimGamble.GamblingType".Translate());
            pos.x += inRect.width * 0.2f;

            // odds
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 24), "RimGamble.Odds".Translate());
            pos.x += inRect.width * 0.2f;

            // expiry
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 24), "RimGamble.Expiry".Translate());
            pos.x += inRect.width * 0.2f;

            // Stake
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.1f, 24), "RimGamble.Stake".Translate());
            pos.x += inRect.width * 0.1f;

            /*
             * Amount of silver in stockpiles
             */
            Widgets.ThingIcon(new Rect(pos.x, pos.y, 24, 24), ThingDefOf.Silver);
            pos.x += 24;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.1f, 24), silverList.Sum(thing => thing.stackCount).ToString());

            /*
             * Start of wager display
             */
            pos.x = 0;
            pos.y += 24;

            var wagerList = OnlineGambleScheduler.Instance.bets;
            float listHeight = wagerList.Count * 26;
            Rect viewRect = new Rect(pos.x, pos.y, inRect.width - 10, 400);
            Rect scrollRect = new Rect(pos.x, pos.y, viewRect.width - 16f, listHeight);
            Widgets.BeginScrollView(viewRect, ref scrollPos, scrollRect);
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0f, pos.y, viewRect.width);
            Text.Anchor = TextAnchor.MiddleLeft;

            // actual entries start from here
            GUI.color = Color.white;
            for (int i = 0; i < wagerList.Count; i++) // go through the list of wagers an put each of them up
            {
                pos.x = 0;
                var wager = wagerList[i];
                if (i % 2 == 1) // on odd rows, add a different highlight
                {
                    var oddRowRect = new Rect(pos.x, pos.y, inRect.width, 24);
                    Widgets.DrawLightHighlight(oddRowRect);
                }


                // gambling site name
                Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 24), wager.siteName);
                pos.x += inRect.width * 0.2f;
                // Gambling type
                Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 24), wager.betLabel);
                pos.x += inRect.width * 0.2f;
                // odds
                Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 24), wager.odds.ToString("P2"));
                pos.x += inRect.width * 0.2f;
                // expiry
                Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 24), (wager.endTimeInTicks - Find.TickManager.TicksGame).ToStringTicksToPeriod());
                pos.x += inRect.width * 0.2f;
                /*
                 * Stake entry
                 */
                //decrease button
                var decreaseButton = new Rect(pos.x, pos.y, 24, 24);
                if (Widgets.ButtonText(decreaseButton, "<") && wager.stakeBufferInt > 0)
                {
                    wager.stakeBufferInt = Mathf.Max(0, wager.stakeBufferInt - (1 * GenUI.CurrentAdjustmentMultiplier()));
                }
                // number field
                GUI.color = Color.white;
                var textEntry = new Rect(decreaseButton.xMax + 5, pos.y, 60, 24);
                if (wager.stakeBufferInt == 0) // set the buffer to the current stake
                {
                    wager.stakeBuffer = "";
                }
                else
                {
                    wager.stakeBuffer = wager.stakeBufferInt.ToString();
                }
                Widgets.TextFieldNumeric<int>(textEntry, ref wager.stakeBufferInt, ref wager.stakeBuffer);
                if (string.IsNullOrEmpty(wager.stakeBuffer))
                {
                    wager.stakeBufferInt = 0;
                }
                // increase button
                var increaseButton = new Rect(pos.x, pos.y, 24, 24);
                if (Widgets.ButtonText(increaseButton, "<"))
                {
                    wager.stakeBufferInt = Mathf.Max(0, wager.stakeBufferInt - (1 * GenUI.CurrentAdjustmentMultiplier()));
                }

                pos.y += 26;
            }
            Widgets.EndScrollView(); // end of scrollable betting window

            Rect placeBetsButtonRect = new Rect(inRect.width * 0.35f, pos.y + 5f, inRect.width * 0.3f, 30);
            if (Widgets.ButtonText(placeBetsButtonRect, "Place Bets"))
            {
                // first evaluate if the colony has enough money to make their bets
                if (silverList.Sum(thing => thing.stackCount) >= wagerList.Sum(bet => bet.stakeBufferInt))
                {
                    // Update the stakes from the buffers (number fields)
                    // Remove or add silver from our stockpiles if necessary
                    int silverDiff = 0;
                    foreach (var wager in wagerList)
                    {
                        silverDiff += (wager.stake - wager.stakeBufferInt);

                        wager.stake = wager.stakeBufferInt;
                    }

                    // based on the value of silverToReturn, remove or add silver from our stockpiles
                    if (silverDiff > 0)
                    {
                        Thing silverStack = ThingMaker.MakeThing(ThingDefOf.Silver);
                        silverStack.stackCount = silverDiff;
                        IntVec3 dropSpot = DropCellFinder.TradeDropSpot(Find.CurrentMap);
                        TradeUtility.SpawnDropPod(dropSpot, Find.CurrentMap, silverStack);
                        // tell the player
                        Find.LetterStack.ReceiveLetter("RimGamble.BetReturned".Translate(), "RimGamble.BetReturnedDesc".Translate(), LetterDefOf.PositiveEvent, new TargetInfo(dropSpot, Find.CurrentMap));
                    }
                    else
                    {
                        silverDiff *= -1;
                        // remove directly from our stockpiles
                        while (silverDiff > 0)
                        {
                            Thing silverIndividual = silverList.RandomElement();
                            silverList.Remove(silverIndividual);
                            int amtTakenFromIndividual = Math.Min(silverDiff, silverIndividual.stackCount);
                            silverIndividual.SplitOff(amtTakenFromIndividual).Destroy();   
                            silverDiff -= amtTakenFromIndividual;
                        }
                        // update the silver ct
                        silverFinder(Find.CurrentMap);
                    }
                }
            }

            // must be set back to default
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /*
         * Find all accessible silver on the map (must be in range of a powered trade beacon)
         * Like the trade ship mechanic, just with only silver
         */
        private void silverFinder(Map map)
        {
            List<Thing> newSilverList = new List<Thing>();
            foreach (var beacon in Building_OrbitalTradeBeacon.AllPowered(map))
            {
                IEnumerable<IntVec3> tradeableCells = Building_OrbitalTradeBeacon.TradeableCellsAround(beacon.Position, map);
                foreach (var cell in tradeableCells)
                {
                    if (cell.InBounds(map))
                    {
                        List<Thing> thingsInCell = cell.GetThingList(map);

                        // Add any silver or other tradeable items found in this cell
                        foreach (var thing in thingsInCell)
                        {
                            if (thing.def == ThingDefOf.Silver)
                            {
                                newSilverList.Add(thing);
                            }
                        }
                    }
                }

            }
            silverList = newSilverList;
        }

        private void stakeBufferReset()
        {
            foreach (Bet wager in OnlineGambleScheduler.Instance.bets)
            {
                if (wager.stake == 0)
                {
                    wager.stakeBufferInt = 0;
                }
                else // set this just in case
                {
                    wager.stakeBufferInt = wager.stake;
                }
            }
        }

    }
}
