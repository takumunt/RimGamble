﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGamble
{
    public class Window_CaravanGambling : Window
    {
        private bool gameStart;
        private bool gameRoll;
        private bool timerStarted;
        private bool animationFinished;
        private bool concluded;
        private bool won;
        private List<Tradeable> tradeablesList;
        private Pawn traderPawn;
        Dictionary<Tradeable, WagerItem> colonyItemsWagered;
        Dictionary<Tradeable, WagerItem> traderItemsWagered;
        int colonyWagerVal;
        int traderWagerVal;
        private float timeRemaining;
        private float traderWagerUpdateInterval;
        private float traderWagerUpdateTimer;
        private float animationTimer;
        private float odds;

        public override Vector2 InitialSize => new Vector2(UI.screenWidth * 0.6f, UI.screenHeight * 0.6f);
        public static Vector2 scrollPos;
        private Texture2D rectangleTexture;

        /**
         * 
         * Constants for heights and widths of GUI elements
         * 
         */
        private static float rowHeight = 26;
        private static float rowTextHeight = 24;
        private static float scrollableAreaHeight = 400;
        private static float entryFieldWidth = 60;
        private static float cancelButtonWidth = 100;
        private static float cancelButtonHeight = 30;

        public Window_CaravanGambling(Pawn trader)
        {
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            this.traderPawn = trader;
            tradeablesList = TradeSession.deal.AllTradeables;
            Initialize();
        }

        // initializes some values that may need to be reset
        private void Initialize()
        {
            colonyItemsWagered = new Dictionary<Tradeable, WagerItem>();
            traderItemsWagered = new Dictionary<Tradeable, WagerItem>();
            gameStart = false;
            gameRoll = false;
            timerStarted = false;
            animationFinished = false;
            concluded = false;
            won = false;
            timeRemaining = 20f;
            colonyWagerVal = 0;
            traderWagerVal = 0;
            traderWagerUpdateInterval = 5f;
            traderWagerUpdateTimer = traderWagerUpdateInterval;
            animationTimer = 10f;
            odds = 0;
            PositionX = InitialSize.x / 2;
        }


        public override void DoWindowContents(Rect inRect)
        {
            /*
             * Main title and description, this will always be shown
             */
            Vector2 pos = Vector2.zero;

            // title of the window  
            Text.Font = GameFont.Medium;
            var titleSize = Text.CalcSize("RimGamble.CaravanGamblingLabel".Translate());
            Widgets.Label(new Rect(pos.x, pos.y, titleSize.x, titleSize.y), "RimGamble.CaravanGamblingLabel".Translate());

            // description of the window
            pos.y += titleSize.y;
            Text.Font = GameFont.Small;
            GUI.color = Color.grey;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width, 40), "RimGamble.CaravanGamblingDesc".Translate());

            pos.y += 40;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0f, pos.y, inRect.width);
            GUI.color = Color.white;


            // button to start the game, only appears while the game has not started
            if (!gameStart)
            {
                Rect startButtonRect = new Rect(inRect.width / 2 - 100f, inRect.height / 2 - 25f, 200f, 50f);
                if (Widgets.ButtonText(startButtonRect, "RimGamble.StartGame".Translate()))
                {
                    // Start the game
                    gameStart = true;
                }
            }
            // show the actual game once the button has been clicked
            else
            {
                if (!gameRoll) // gui to show while adding items to the item pool
                {
                    timerStarted = true;
                    pos.y += 10;
                    GamblingUI(inRect, pos);
                }
                else // gui to show once adding phase is over, and dice roll begins
                {
                    RollUI(inRect, pos, odds);
                }

            }
        }

        public override void PreClose()
        {
            // close the trade session
            TradeSession.Close();
        }

        // UI that we show once the game has actually started
        private void GamblingUI(Rect inRect, Vector2 pos)
        {
            // create scroll
            float listHeight = Math.Max(tradeablesList.Count(t => t.thingsTrader.Count > 0 && traderPawn.TraderKind.WillTrade(t.ThingDef)), tradeablesList.Count(t => t.thingsColony.Count > 0 && traderPawn.TraderKind.WillTrade(t.ThingDef))) * rowHeight;
            Rect viewRect = new Rect(pos.x, pos.y, inRect.width - 10f, scrollableAreaHeight);
            Rect scrollRect = new Rect(pos.x, pos.y, viewRect.width - 16f, listHeight); // the list's length is the number of items in it that can be traded/wagered
            Widgets.BeginScrollView(viewRect, ref scrollPos, scrollRect);
            Text.Anchor = TextAnchor.MiddleLeft;

            // draw each row
            int row = 0;
            int colonyRow = 0;
            int traderRow = 0;
            foreach (var tradeItem in tradeablesList.Where(t => traderPawn.TraderKind.WillTrade(t.ThingDef))) // only draw rows for items that can be traded/wagered
            {
                pos.x = 0;

                if (row % 2 == 1) // on odd rows, add a different highlight
                {
                    var oddRowRect = new Rect(pos.x, pos.y + (row * rowHeight), inRect.width, rowTextHeight);
                    Widgets.DrawLightHighlight(oddRowRect);
                }

                /**
                 * Left side (player side)
                 */
                
                if (tradeItem.thingsColony.Count > 0)
                {
                    int ctHeldBy = tradeItem.CountHeldBy(Transactor.Colony);

                    // add this to the dictionary if it doesnt already exist
                    if (!colonyItemsWagered.ContainsKey(tradeItem))
                    {
                        colonyItemsWagered[tradeItem] = new WagerItem(0); // the initial value of any entry is the number of that item the colony has in its possession
                    }
                    pos.x = 0;
                    // icon for item
                    Widgets.ThingIcon(new Rect(pos.x, pos.y + (colonyRow * rowHeight), rowTextHeight, rowTextHeight), tradeItem.ThingDef);
                    pos.x += rowTextHeight + 2;
                    // name of item
                    Widgets.Label(new Rect(pos.x, pos.y + (colonyRow * rowHeight), inRect.width * 0.2f, rowTextHeight), tradeItem.Label);
                    pos.x += inRect.width * 0.2f;
                    // number of item
                    Widgets.Label(new Rect(pos.x, pos.y + (colonyRow * rowHeight), inRect.width * 0.05f, rowTextHeight), ctHeldBy.ToString());
                    pos.x += inRect.width * 0.05f;

                    // entry field
                    int numItems = colonyItemsWagered[tradeItem].numItems;
                    String numItemsBuffer = colonyItemsWagered[tradeItem].numItemsBuffer;
                    colonyWagerVal -= (int)(numItems * tradeItem.BaseMarketValue); // subtract the current value of the items we are working on, we will update this later
                    
                    // decrease button
                    if (Widgets.ButtonText(new Rect(pos.x, pos.y + (colonyRow * rowHeight), rowTextHeight, rowTextHeight), "<") && numItems > 0)
                    {
                        numItems = Mathf.Max(0, numItems - (1 * GenUI.CurrentAdjustmentMultiplier()));
                    }
                    pos.x += rowTextHeight;

                    // number field
                    GUI.color = Color.white;
                    if (numItems == 0) // set the buffer to the current wager
                    {
                        numItemsBuffer = "";
                    }
                    else
                    {
                        numItemsBuffer = numItems.ToString();
                    }
                    Widgets.TextFieldNumeric<int>(new Rect(pos.x + 5, pos.y + (colonyRow * rowHeight), entryFieldWidth, rowTextHeight), ref numItems, ref numItemsBuffer);
                    if (numItems > ctHeldBy) // ensures the entry is valid (between 0 and the number of the item held by the colony)
                    {
                        numItems = ctHeldBy;
                        numItemsBuffer = numItems.ToString();
                    }
                    else if (string.IsNullOrEmpty(numItemsBuffer))
                    {
                        numItems = 0;
                    }
                    pos.x += entryFieldWidth;

                    // increase button
                    var increaseButton = new Rect(pos.x + 5, pos.y + (colonyRow * rowHeight), rowTextHeight, rowTextHeight);
                    if (Widgets.ButtonText(increaseButton, ">") && numItems <= ctHeldBy)
                    {
                        numItems = Mathf.Max(0, numItems + (1 * GenUI.CurrentAdjustmentMultiplier()));
                    }
                    colonyItemsWagered[tradeItem].numItems = numItems;
                    colonyItemsWagered[tradeItem].numItemsBuffer = numItemsBuffer;
                    // update the total value of the wager
                    colonyWagerVal += (int)(numItems * tradeItem.BaseMarketValue);

                    colonyRow++;
                }

                /**
                 * Right side (trader side)
                 */
                if (tradeItem.thingsTrader.Count > 0)
                {
                    // add this to the dictionary if it doesnt already exist
                    if (!traderItemsWagered.ContainsKey(tradeItem))
                    {
                        traderItemsWagered[tradeItem] = new WagerItem(0); // the initial value of any entry is the number of that item the colony has in its possession
                    }

                    pos.x = inRect.width * 0.5f;
                    // icon for item
                    Widgets.ThingIcon(new Rect(pos.x, pos.y + (traderRow * 26), 24, 24), tradeItem.ThingDef);
                    pos.x += 26;
                    // name of item
                    Widgets.Label(new Rect(pos.x, pos.y + (traderRow * 26), inRect.width * 0.2f, 24), tradeItem.Label);
                    pos.x += inRect.width * 0.2f;
                    // number of item
                    Widgets.Label(new Rect(pos.x, pos.y + (traderRow * 26), inRect.width * 0.05f, 24), tradeItem.CountHeldBy(Transactor.Trader).ToString());
                    pos.x += inRect.width * 0.05f;
                    // number of item being wagered
                    Widgets.Label(new Rect(pos.x, pos.y + (traderRow * 26), inRect.width * 0.05f, 24), traderItemsWagered[tradeItem].numItems.ToString());

                    traderRow++;
                }
                row++;
            }
            // end of scrolling section
            Widgets.EndScrollView();
            pos.x = 0;
            pos.y += scrollableAreaHeight;

            // label to show what the current market value of the colony's wager is (we currently use base market value)
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, rowHeight), "Total Colony Wager Value: " + colonyWagerVal);

            Text.Anchor = TextAnchor.MiddleRight;
            // label to show what the current market value of the trader's wager is (we currently use base market value)
            Widgets.Label(new Rect(inRect.width * 0.8f, pos.y, inRect.width * 0.2f, rowHeight), "Total Trader Wager Value: " + traderWagerVal);
            pos.y += rowHeight;

            // timer for betting
            if (timerStarted)
            {
                // update the timer
                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0) // once the timer reaches zero, start the rolling
                {
                    timeRemaining = 0;
                    timerStarted = false;
                    gameRoll = true;
                }

                // update the trader wager changes
                traderWagerUpdateTimer -= Time.deltaTime;
                if (traderWagerUpdateTimer <= 0)
                {
                    UpdateTraderWager();
                    traderWagerUpdateTimer = traderWagerUpdateInterval;
                }
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            pos.x = 0;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, rowHeight), "Time Remaining: " + timeRemaining);
            pos.y += rowHeight;

            // label to show our current odds
            // calculate the odds
            if (colonyWagerVal + traderWagerVal == 0)
            {
                odds = 0;
            }
            else
            {
                odds = (float)colonyWagerVal / (colonyWagerVal + traderWagerVal);
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect((inRect.width / 2) - (inRect.width * 0.1f), pos.y, inRect.width * 0.2f, rowHeight), "Odds of Winning: " + odds.ToString("P2"));
            pos.y += rowHeight;

            // button to cancel, which just sends you back to the first screen
            if (Widgets.ButtonText(new Rect(inRect.width / 2 - (cancelButtonWidth / 2), pos.y, cancelButtonWidth, cancelButtonHeight), "Cancel"))
            {
                Initialize();
            }

            // must be set back to default
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /**
         * UI elements responsible for representing the dice roll
         */
        private float PositionX; // position of pointer
        private static float PointerWidth = 10f;
        private static float PointerHeight = 20f;
        private void RollUI(Rect inRect, Vector2 pos, float odds)
        {
            if (!concluded)
            {
                rectangleTexture = CreateRectangularBox(odds, inRect);
                concluded = true;
                if (UnityEngine.Random.value < odds)
                {
                    won = true;
                }
                ConcludeDeal();
            }

            if (!animationFinished)// if the wager has not concluded yet, play the animation and calculate the odds
            {
                // draw the background box
                GUI.DrawTexture(new Rect(0, inRect.height * 0.5f - 25, inRect.width, 50), rectangleTexture);
                // draw the moving pointer
                Widgets.DrawBoxSolid(new Rect(PositionX - (PointerWidth / 2), inRect.height * 0.5f - 25, PointerWidth, PointerHeight), Color.black);
                UpdateAnimation();
            }
            // display the result
            else
            {
                if (won)
                {
                    Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, rowHeight), "You Won!");

                }
                else
                {
                    Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, rowHeight), "You Lost...");
                }
            }
        }

        private float pointerSpeed = 1f;
        private bool accelerating = true;
        private static float animationTimeTotal = 10f;
        private void UpdateAnimation()
        {
            if (pointerSpeed <= 0) // once the pointer stops, the animation is finished
            {
                animationFinished = true;
            }

            // change acceleration state based on how much time has elapsed
            if (animationTimer < (animationTimeTotal / 2))
            {
                accelerating = false;
            }

            // change pointerspeed to accelerate or deccelerate
            if (accelerating)
            {
                pointerSpeed += Time.deltaTime;
            } else
            {
                pointerSpeed -= Time.deltaTime;
            }

            // Update the animation progress
            if (PositionX >= InitialSize.x)
            {
                PositionX = 0;
            }
            else
            {
                PositionX += pointerSpeed;

            }

            animationTimer -= Time.deltaTime;
        }

        private Texture2D CreateRectangularBox(float percentage, Rect outerRect)
        {
            // Create a texture for the rectangle with settings to prevent blending of colors
            Texture2D rectTexture = new Texture2D((int)outerRect.width, (int)outerRect.height, TextureFormat.RGB24, false);
            rectTexture.filterMode = FilterMode.Point;

            // Calculate the number of pixels to fill based on the percentage
            int filledWidth = Mathf.FloorToInt(outerRect.width * percentage);
            Log.Message("filledWidth: " + filledWidth);

            // Loop through each pixel to fill the rectangle with a percentage colored
            for (int y = 0; y < rectTexture.height; y++)
            {
                for (int x = 0; x < rectTexture.width; x++)
                {
                    if (x < filledWidth)
                    {
                        rectTexture.SetPixel(x, y, Color.green);
                    }
                    else
                    {
                        rectTexture.SetPixel(x, y, Color.white);
                    }
                }
            }

            // Apply changes to the texture
            rectTexture.Apply();

            return rectTexture;
        }

        private void UpdateTraderWager()
        {
            // make sure we only try to update as long as there is an item we can wager
            if (traderItemsWagered.Count > 0)
            {
                // select a random key from the dictionary
                var keys = traderItemsWagered.Keys.ToList();
                var randomSelectedItem = keys[UnityEngine.Random.Range(0, keys.Count)];

                // update that key with a random amount, with the maximum possible value being the amount the trader posesses
                int newWagerCt = UnityEngine.Random.Range(1, randomSelectedItem.CountHeldBy(Transactor.Trader));
                float itemMarketVal = randomSelectedItem.BaseMarketValue;
                traderWagerVal += ((int)(newWagerCt * itemMarketVal) - (int)(traderItemsWagered[randomSelectedItem].numItems * itemMarketVal));
                traderItemsWagered[randomSelectedItem].numItems = newWagerCt;
            }
        }

        /*
         * Method that handles receiving or taking items once the gambling session has concluded
         */
        private void ConcludeDeal()
        {
            foreach (var tradeable in tradeablesList)
            {
                if (won) // if the player won, transfer items from the trader to the player
                {
                    if (tradeable.thingsTrader.Count > 0 && traderItemsWagered.ContainsKey(tradeable))
                    {
                        tradeable.AdjustTo(traderItemsWagered[tradeable].numItems);
                        tradeable.ResolveTrade();
                    }
                }
                else // if the trader won, transfer items from the player to the trader
                {
                    if (tradeable.thingsColony.Count > 0 && colonyItemsWagered.ContainsKey(tradeable))
                    {
                        tradeable.AdjustTo(-(colonyItemsWagered[tradeable].numItems));
                        tradeable.ResolveTrade();
                    }
                }
            }
        }

        /*
         * Object used to store item information for the dictionaries< "colonyItemsWagered" and "traderItemsWagered"
         */
        private class WagerItem
        {
            public int numItems { get; set; }
            public String numItemsBuffer { get; set; }

            public WagerItem(int numItems)
            {
                this.numItems = numItems;
                this.numItemsBuffer = "";
            }
        }
    }
}
