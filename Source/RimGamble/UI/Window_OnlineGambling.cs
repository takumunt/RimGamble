﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RimGamble;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGamble
{
    /*
     * Custom window class for the online gambling window
     */
    public class Window_OnlineGambling : Window
    {
        // runs on initial opening of window 
        public Window_OnlineGambling()
        {
            silverFinder(Find.CurrentMap);
            stakeBufferReset();
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            currSilverUsed = 0;
        }

        public override Vector2 InitialSize => new Vector2(UI.screenWidth * 0.6f, UI.screenHeight * 0.6f);

        public static Vector2 scrollPos;
        public List<Thing> silverList;
        public int silverHeld;
        public int currSilverUsed;
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
            pos.x += inRect.width * 0.1f;

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
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0f, pos.y, inRect.width);
            pos.y += 10;

            GUI.color = Color.white;
            var wagerList = RimGambleManager.Instance.bets;
            float listHeight = wagerList.Count * 24;
            Rect viewRect = new Rect(pos.x, pos.y, inRect.width - 10, 400);
            Rect scrollRect = new Rect(pos.x, pos.y, viewRect.width - 16f, listHeight);
            Widgets.BeginScrollView(viewRect, ref scrollPos, scrollRect);
            Text.Anchor = TextAnchor.MiddleLeft;

            // actual entries start from here
            for (int i = 0; i < wagerList.Count; i++) // go through the list of wagers and put each of them up
            {
                pos.x = 0;
                var wager = wagerList[i];
                if (i % 2 == 1) // on odd rows, add a different highlight
                {
                    var oddRowRect = new Rect(pos.x, pos.y, inRect.width, 24);
                    Widgets.DrawLightHighlight(oddRowRect);
                }
                currSilverUsed -= wager.stakeBufferInt;

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
                var stakeEntry = new Rect(decreaseButton.xMax + 5, pos.y, 60, 24);
                if (wager.stakeBufferInt == 0) // set the buffer to the current stake
                {
                    wager.stakeBuffer = "";
                }
                else
                {
                    wager.stakeBuffer = wager.stakeBufferInt.ToString();
                }
                Widgets.TextFieldNumeric<int>(stakeEntry, ref wager.stakeBufferInt, ref wager.stakeBuffer);
                if (wager.stakeBufferInt > (silverHeld - currSilverUsed))
                {
                    wager.stakeBufferInt = (silverHeld - currSilverUsed);
                }
                if (string.IsNullOrEmpty(wager.stakeBuffer))
                {
                    wager.stakeBufferInt = 0;
                }
                // increase button
                var increaseButton = new Rect(stakeEntry.xMax + 5, pos.y, 24, 24);
                if (Widgets.ButtonText(increaseButton, ">"))
                {
                    int maxPossibleIncrease = silverHeld - currSilverUsed;
                    if (maxPossibleIncrease > 0)
                    {
                        wager.stakeBufferInt = Mathf.Min(wager.stakeBufferInt + (1 * GenUI.CurrentAdjustmentMultiplier()), maxPossibleIncrease);
                    }
                }

                currSilverUsed += wager.stakeBufferInt;
                pos.y += 26;
            }
            Widgets.EndScrollView(); // end of scrollable betting window

            Rect placeBetsButtonRect = new Rect(inRect.width * 0.35f, 540, inRect.width * 0.3f, 40);
            if (Widgets.ButtonText(placeBetsButtonRect, "Place Bets"))
            {
                // add up the difference in total silver bet

                if (checkEnoughSilver(wagerList, out int silverDiff))
                {
                    // update the stakes
                    foreach (var wager in wagerList)
                    {
                        wager.stake = wager.stakeBufferInt;
                    }
                    // Remove or add silver from our stockpiles if necessary
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

        private bool checkEnoughSilver(List<Bet> wagerList, out int silverDiff)
        {
            silverDiff = 0;
            foreach (var wager in wagerList)
            {
                silverDiff += (wager.stake - wager.stakeBufferInt);
            }
            // first evaluate if the colony has enough money to make their bets
            if (silverHeld >= -silverDiff)
            {
                return true;
            }
            return false;
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
            silverHeld = silverList.Sum(thing => thing.stackCount);
        }

        private void stakeBufferReset()
        {
            foreach (Bet wager in RimGambleManager.Instance.bets)
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
