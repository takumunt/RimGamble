using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGamble
{
    public class Window_CaravanGambling : Window
    {
        /**
         * Flags to determine what UI elements need to be rendered
         */
        private bool gameStart;
        private bool gameRoll;
        private bool timerStarted;
        private bool animationFinished;
        private bool concluded;
        private bool won;
        private bool accelerating;


        private List<Tradeable> tradeablesListInit;
        private Dictionary<Tradeable, SingleItem> itemList;
        private Pawn traderPawn;
        private Dictionary<Tradeable, WagerItem> colonyItemsWagered;
        private Dictionary<Tradeable, WagerItem> traderItemsWagered;
        private int colonyWagerVal;
        private int traderWagerVal;
        private float timeRemaining;
        private static float timeInitial = 20f;
        private float traderWagerUpdateTimer;
        private float odds;
        private float pointerSpeed;
        private float accelRate;
        private float decelRate;

        /**
         * AI related stuff
         */
        private float traderWagerUpdateInterval;
        private CaravanGambleAI aiType;
        private float aiBias;
        private float variance = 0.1f; // stdDev to be used for gaussian distribution of updateintervals

        /**
         * 
         * Constants for heights and widths of GUI elements
         * 
         */
        private static float rowHeight = 30;
        private static float rowTextHeight = 24;
        private static float numWidth = 60;
        private static float scrollableAreaHeight = 400;
        private static float entryFieldWidth = 60;
        private static float cancelButtonWidth = 100;
        private static float cancelButtonHeight = 30;
        private static float exitButtonWidth = 100;
        private static float exitButtonHeight = 30;
        private static float retryButtonWidth = 100;
        private static float retryButtonHeight = 30;
        public override Vector2 InitialSize => new Vector2(UI.screenWidth * 0.6f, UI.screenHeight * 0.6f);

        public Window_CaravanGambling(Pawn trader)
        {
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            this.traderPawn = trader;
            tradeablesListInit = TradeSession.deal.AllTradeables.Where(t => traderPawn.TraderKind.WillTrade(t.ThingDef)).ToList();
            itemList = new Dictionary<Tradeable, SingleItem>();
            // initialize the itemList by going through tradeableslistinit and adding each unqiue entry
            foreach (Tradeable item in tradeablesListInit)
            {
                itemList[item] = new SingleItem(item.CountHeldBy(Transactor.Colony), item.CountHeldBy(Transactor.Trader));
            }

            Initialize();
        }

        // initializes some values that may need to be reset upon cancellation
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
            accelerating = true;
            timeRemaining = timeInitial;
            colonyWagerVal = 0;
            traderWagerVal = 0;
            odds = 0;
            pointerX = InitialSize.x / 2;
            pointerSpeed = 1f;
            accelRate = 0.1f;
            decelRate = 0.05f;

            // pick an ai behavior
            aiType = AIPicker.PickRandomAI();

            aiBias = aiType.wagerBias;
            traderWagerUpdateInterval = aiType.wagerInterval;
            traderWagerUpdateTimer = wagerIntervalAdjust();
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
                    DrawNegotiationUI(inRect, pos);
                }
                else // gui to show once adding phase is over, and dice roll begins
                {
                    DrawRollUI(inRect, pos, odds);
                }

            }
        }

        public override void PreClose()
        {
            // conclude all wagers
            ConcludeDeal();
            // close the trade session
            TradeSession.Close();
        }

        /**
         * UI elements to show the negotiation phase of the gamble game
         */
        public static Vector2 scrollPos;
        private void DrawNegotiationUI(Rect innerRect, Vector2 pos)
        {
            pos.x = 0;

            // create scroll
            float listHeight = itemList.Count * rowHeight;
            Rect viewRect = new Rect(pos.x, pos.y + rowTextHeight, innerRect.width - 10f, scrollableAreaHeight);
            Rect scrollRect = new Rect(pos.x, pos.y + rowTextHeight, viewRect.width - 16f, listHeight); // the list's length is the number of items in it that can be traded/wagered
            // top header labels
            Widgets.Label(new Rect(0, pos.y, viewRect.width * 0.1f, rowTextHeight), "RimGamble.ColonyItems".Translate());
            Widgets.Label(new Rect(viewRect.width * 0.45f - rowTextHeight - rowTextHeight - numWidth - entryFieldWidth - 15, pos.y, viewRect.width * 0.1f, rowTextHeight), "RimGamble.ColonyAmtHeld".Translate());
            Widgets.Label(new Rect(viewRect.width * 0.55f, pos.y, viewRect.width * 0.2f, rowTextHeight), "RimGamble.TraderItems".Translate());
            Widgets.Label(new Rect(viewRect.width - 20f - numWidth - numWidth, pos.y, numWidth, rowTextHeight), "RimGamble.TraderItemCount".Translate());
            Widgets.Label(new Rect(viewRect.width - 10f - numWidth, pos.y, numWidth, rowTextHeight), "RimGamble.TraderItemWagered".Translate());
            pos.y += rowTextHeight;

            Widgets.BeginScrollView(viewRect, ref scrollPos, scrollRect);
            Text.Anchor = TextAnchor.MiddleLeft;

            // draw each row
            int row = 0;
            int colonyRow = 0;
            int traderRow = 0;
            foreach (var tradeItem in itemList) // only draw rows for items that can be traded/wagered
            {
                pos.x = 0;

                if (row % 2 == 1) // on odd rows, add a different highlight
                {
                    var oddRowRect = new Rect(pos.x, pos.y + (row * rowHeight), viewRect.width, rowHeight);
                    Widgets.DrawLightHighlight(oddRowRect);
                }

                /**
                 * Left side (player side)
                 */
                
                if (tradeItem.Value.numItemsColony > 0)
                {
                    int ctHeldBy = tradeItem.Value.numItemsColony;
                    // add this to the dictionary if it doesnt already exist
                    if (!colonyItemsWagered.ContainsKey(tradeItem.Key))
                    {
                        colonyItemsWagered[tradeItem.Key] = new WagerItem(0); // the initial value of any entry is the number of that item the colony has in its possession
                    }

                    Rect colonyRowRect = new Rect(0, pos.y + (colonyRow * rowHeight), viewRect.width * 0.45f, rowHeight);
                    Widgets.BeginGroup(colonyRowRect);
                    float width = colonyRowRect.width;

                    /**
                     * Entry Field
                     */
                    int numItems = colonyItemsWagered[tradeItem.Key].numItems;
                    String numItemsBuffer = colonyItemsWagered[tradeItem.Key].numItemsBuffer;
                    colonyWagerVal -= (int)(numItems * tradeItem.Key.BaseMarketValue); // subtract the current value of the items we are working on, we will update this later

                    // increase button
                    Rect increaseButtonRect = new Rect(width - rowTextHeight, 0, rowTextHeight, rowHeight);
                    if (Widgets.ButtonText(increaseButtonRect, ">") && numItems <= ctHeldBy)
                    {
                        numItems = Mathf.Max(0, numItems + (1 * GenUI.CurrentAdjustmentMultiplier()));
                    }
                    width -= rowTextHeight;

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
                    Rect entryFieldRect = new Rect(increaseButtonRect.xMin - 5 - entryFieldWidth, 0, entryFieldWidth, rowHeight);
                    Widgets.TextFieldNumeric<int>(entryFieldRect, ref numItems, ref numItemsBuffer);
                    if (numItems > ctHeldBy) // ensures the entry is valid (between 0 and the number of the item held by the colony)
                    {
                        numItems = ctHeldBy;
                        numItemsBuffer = numItems.ToString();
                    }
                    else if (string.IsNullOrEmpty(numItemsBuffer))
                    {
                        numItems = 0;
                    }

                    // decrease button
                    Rect decreaseButtonRect = new Rect(entryFieldRect.xMin - 5 - rowTextHeight, 0, rowTextHeight, rowHeight);
                    if (Widgets.ButtonText(decreaseButtonRect, "<") && numItems > 0)
                    {
                        numItems = Mathf.Max(0, numItems - (1 * GenUI.CurrentAdjustmentMultiplier()));
                    }

                    Rect colonyItemAmtRect = new Rect(decreaseButtonRect.xMin - 5 - numWidth, 0, rowTextHeight, rowHeight);
                    Widgets.Label(colonyItemAmtRect, ctHeldBy.ToString());

                    colonyItemsWagered[tradeItem.Key].numItems = numItems;
                    colonyItemsWagered[tradeItem.Key].numItemsBuffer = numItemsBuffer;
                    // update the total value of the wager
                    colonyWagerVal += (int)(numItems * tradeItem.Key.BaseMarketValue);

                    Rect infoRect = new Rect(0, 0, colonyItemAmtRect.xMin - 5, rowHeight);
                    TransferableUIUtility.DrawTransferableInfo(tradeItem.Key, infoRect, Color.white); // now draw the icon and descriptions
                    Widgets.EndGroup();
                    colonyRow++;
                }

                /**
                 * Right side (trader side)
                 */
                if (tradeItem.Value.numItemsTrader > 0)
                {
                    // add this to the dictionary if it doesnt already exist
                    if (!traderItemsWagered.ContainsKey(tradeItem.Key))
                    {
                        traderItemsWagered[tradeItem.Key] = new WagerItem(0); // the initial value of any entry is the number of that item the colony has in its possession
                    }

                    Rect traderRowRect = new Rect(viewRect.width * 0.55f, pos.y + (traderRow * rowHeight), viewRect.width * 0.45f - 10f, rowHeight);
                    Widgets.BeginGroup(traderRowRect);
                    float width = traderRowRect.width;

                    // number of item being wagered
                    Rect numItemWageredRect = new Rect(width - numWidth, 0, numWidth, rowHeight);
                    Widgets.Label(numItemWageredRect, traderItemsWagered[tradeItem.Key].numItems.ToString());
                    width -= numWidth;
                    // number of item
                    Rect numTotItemLabelRect = new Rect(width - numWidth - 10, 0, numWidth, rowHeight);
                    Widgets.Label(numTotItemLabelRect, tradeItem.Value.numItemsTrader.ToString());


                    // information and description about the item
                    Rect infoRect = new Rect(0, 0, numTotItemLabelRect.xMin - 5, rowHeight);
                    TransferableUIUtility.DrawTransferableInfo(tradeItem.Key, infoRect, Color.white);
                    Widgets.EndGroup();
                    traderRow++;
                }
                row++;
            }
            // end of scrolling section
            Widgets.EndScrollView();
            pos.x = 0;
            pos.y += scrollableAreaHeight;

            // label to show what the current market value of the colony's wager is (we currently use base market value)
            Widgets.Label(new Rect(pos.x, pos.y, innerRect.width * 0.2f, rowHeight), "Total Colony Wager Value: " + colonyWagerVal);

            Text.Anchor = TextAnchor.MiddleRight;
            // label to show what the current market value of the trader's wager is (we currently use base market value)
            Widgets.Label(new Rect(innerRect.width * 0.8f, pos.y, innerRect.width * 0.2f, rowHeight), "Total Trader Wager Value: " + traderWagerVal);
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
                    traderWagerUpdateTimer = wagerIntervalAdjust();
                }
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            pos.x = 0;
            Widgets.Label(new Rect(pos.x, pos.y, innerRect.width * 0.2f, rowHeight), "RimGamble.TimeRemaining".Translate() + timeRemaining);
            pos.y -= rowHeight;

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
            Widgets.Label(new Rect((innerRect.width / 2) - (innerRect.width * 0.1f), pos.y, innerRect.width * 0.2f, rowHeight), "RimGamble.OddsofWinning".Translate() + odds.ToString("P2"));
            pos.y += rowHeight;

            // button to cancel, which just sends you back to the first screen
            if (Widgets.ButtonText(new Rect(innerRect.width / 2 - (cancelButtonWidth / 2), pos.y, cancelButtonWidth, cancelButtonHeight), "RimGamble.Cancel".Translate()))
            {
                Initialize();
            }

            // must be set back to default
            GenUI.ResetLabelAlign();
        }

        /**
         * UI elements responsible for representing the dice roll
         */
        private float pointerX; // position of pointer
        private static float PointerWidth = 10f;
        private static float PointerHeight = 20f;
        private Texture2D rectangleTexture;
        private void DrawRollUI(Rect inRect, Vector2 pos, float odds)
        {
            if (!concluded) // calculation of odds and payout *this runs before and separate from the animation so that players cannot exploit the system
            {
                rectangleTexture = CreateRectangularBox(odds, inRect);
                concluded = true;
                if (UnityEngine.Random.value < odds)
                {
                    won = true;
                }

                // update itemList
                if (won)
                {
                    foreach (var item in traderItemsWagered.Where(t => t.Value.numItems > 0))
                    {
                        itemList[item.Key].numItemsColony += item.Value.numItems;
                        itemList[item.Key].numItemsTrader -= item.Value.numItems;
                    } 
                }
                else
                {
                    foreach (var item in colonyItemsWagered.Where(t => t.Value.numItems > 0))
                    {
                        itemList[item.Key].numItemsTrader += item.Value.numItems;
                        itemList[item.Key].numItemsColony -= item.Value.numItems;

                    }
                }

            }

            // draw the background box and outlines
            Rect barRect = new Rect(0, inRect.height * 0.5f - 25, inRect.width - 2, 50);
            GUI.DrawTexture(barRect, rectangleTexture);
            int outlineWidth = 1;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(barRect.xMin, barRect.yMax - outlineWidth, barRect.width);
            Widgets.DrawLineHorizontal(barRect.xMin, barRect.yMin, barRect.width);
            Widgets.DrawLineVertical(barRect.xMin, barRect.yMin, barRect.height);
            Widgets.DrawLineVertical(barRect.xMax - outlineWidth, barRect.yMin, barRect.height);
            GUI.color = Color.white;

            // draw the moving pointer
            Widgets.DrawBoxSolid(new Rect(pointerX - (PointerWidth / 2), inRect.height * 0.5f - 25, PointerWidth, PointerHeight), new Color(0.82f, 0.61f, 0.21f));
            if (!animationFinished)
            {
                UpdateAnimation(odds, inRect);
            }
            else
            {
                if (won)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(new Rect(inRect.width * 0.4f, barRect.yMin - 50f, inRect.width * 0.2f, rowHeight), "RimGamble.Win".Translate());
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(new Rect(inRect.width * 0.4f, barRect.yMin - 50f, inRect.width * 0.2f, rowHeight), "RimGamble.Loss".Translate());
                }

                // once the roll ends, create a button to exit
                if (Widgets.ButtonText(new Rect(inRect.width / 2 - exitButtonWidth - 5, barRect.yMax + 50f, exitButtonWidth, exitButtonHeight), "RimGamble.Exit".Translate()))
                {
                    this.Close();
                }

                // retry button
                if (Widgets.ButtonText(new Rect(inRect.width / 2 + 5, barRect.yMax + 50f, retryButtonWidth, retryButtonHeight), "RimGamble.ReGamble".Translate()))
                {
                    this.Initialize();
                }

            }
            GenUI.ResetLabelAlign();
        }


        /**
         * Method to update the position of the moving pointer in the final rolling animation
         */
        private float pointerSpeedUpperLimit = 30f;
        private float pointerSpeedLowerBound = 2f;
        private void UpdateAnimation(float odds, Rect inRect)
        {
            float targetLeftBound;
            float targetRightBound;
            if (won)
            {
                targetLeftBound = 0f;
                targetRightBound = odds * inRect.width;
            } else
            {
                targetLeftBound = odds * inRect.width;
                targetRightBound = inRect.width;
            }

            // Handle acceleration and deceleration=-
            if (accelerating)
            {
                // Gradually increase pointer speed
                pointerSpeed += accelRate;
                accelRate += 0.001f; // Gradually increase the acceleration rate

                if (pointerSpeed >= pointerSpeedUpperLimit)
                {
                    accelerating = false; // Stop accelerating when max speed is reached
                }
            }
            else
            {
                // Gradually decrease pointer speed for smooth deceleration
                if (!(!(pointerX >= targetLeftBound && pointerX <= targetRightBound) && pointerSpeed <= pointerSpeedLowerBound))
                {
                    pointerSpeed -= decelRate;
                    if (decelRate >= 0.05) // Gradually increase the deceleration rate
                    {
                        decelRate -= 0.001f;
                    }
                }

                // Ensure the pointer slows down smoothly before stopping
                if (pointerSpeed <= 0)
                {
                    pointerSpeed = 0f;
                    animationFinished = true; // End the animation
                }
            }

            // Update the pointer's position
            pointerX += pointerSpeed;

            // Wrap around if the pointer goes past the right edge
            if (pointerX >= InitialSize.x)
            {
                pointerX = 0;
            }

        }

        /**
         * Creates a colored box to represent the chances of winning the wager
         */
        private Texture2D CreateRectangularBox(float percentage, Rect outerRect)
        {
            int textureWidth = (int)outerRect.width; // leave some space on the sides for the outline

            // Create a texture for the rectangle with settings to prevent blending of colors
            Texture2D rectTexture = new Texture2D(textureWidth, (int)outerRect.height, TextureFormat.RGB24, false);
            rectTexture.filterMode = FilterMode.Point;

            // Calculate the number of pixels to fill based on the percentage
            int filledWidth = Mathf.FloorToInt(outerRect.width * percentage);
            int borderWidth = 1;

            // Loop through each pixel to fill the rectangle with a percentage colored
            for (int y = 0; y < rectTexture.height; y++)
            {
                for (int x = 0; x < rectTexture.width; x++)
                {
                    // Fill based on percentage
                    if (x - borderWidth < filledWidth)
                    {
                        rectTexture.SetPixel(x, y, new Color(0.18f, 0.45f, 0.6f)); // Filled section
                    }
                    else
                    {
                        rectTexture.SetPixel(x, y, new Color(0.23f, 0.23f, 0.23f)); // Empty section
                    }
                    
                }
            }

            // Apply changes to the texture
            rectTexture.Apply();

            return rectTexture;
        }

        /**
         * Behavior for the AI to choose items to add to the wager pool
         * This method handles an addition of a new item(s) to the wager (or not, if the chosen behavior decides against it)
         */
        private void UpdateTraderWager()
        {
            if (traderItemsWagered.Count > 0)
            {
                List<StakeItem> itemsToWager = aiType.addTraderWager(traderItemsWagered.Keys.ToList(), colonyWagerVal, traderWagerVal, traderItemsWagered);

                if (itemsToWager != null)
                {
                    foreach (StakeItem itemToWagerInd in itemsToWager)
                    {
                        traderWagerVal += ((int)(itemToWagerInd.wagerCt * itemToWagerInd.item.BaseMarketValue) - (int)(traderItemsWagered[itemToWagerInd.item].numItems * itemToWagerInd.item.BaseMarketValue));
                        traderItemsWagered[itemToWagerInd.item].numItems = itemToWagerInd.wagerCt;
                    }
                }
            }
        }

        /**
         * Method that handles receiving or taking items once the gambling session has concluded
         */
        private void ConcludeDeal()
        {
            foreach (var item in itemList)
            {
                // figure out how much to transfer
                // do this by comparing the original amount of the item found in the Tradeable object with the number that exists in the SingleItem object
                // we will perform all transfers from the persepctive of the colony
                item.Key.AdjustTo(item.Value.numItemsColony - item.Key.CountHeldBy(Transactor.Colony));
                item.Key.ResolveTrade();
            }
        }

        /**
         * Helper method that adjusts the time intervals between AI wager updates
         */
        private float wagerIntervalAdjust()
        {
            float adjustedInterval = traderWagerUpdateInterval * (aiBias + 1 * (timeRemaining / timeInitial));
            return Mathf.Max(variance, Rand.Gaussian(adjustedInterval, adjustedInterval * variance));
        }


        private class SingleItem
        {
            public int numItemsColony { get; set; }
            public int numItemsTrader { get; set; }

            public SingleItem(int numItemsColony, int numItemsTrader)
            {
                this.numItemsColony = numItemsColony;
                this.numItemsTrader = numItemsTrader;
            }
        }
    }
}
