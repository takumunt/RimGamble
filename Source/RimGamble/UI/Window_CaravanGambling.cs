using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGamble
{
    public class Window_CaravanGambling : Window
    {
        private bool gameStart = false;
        private List<Tradeable> tradeablesList;
        private Pawn traderPawn;
        private int colonyWagerVal;
        private int traderWagerVal;
        Dictionary<Tradeable, WagerItem> colonyItemsWagered;
        Dictionary<Tradeable, WagerItem> traderItemsWagered;
        public Window_CaravanGambling(Pawn trader)
        {
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            this.traderPawn = trader;
            tradeablesList = TradeSession.deal.AllTradeables;
            colonyWagerVal = 0;
            traderWagerVal = 0;
            colonyItemsWagered = new Dictionary<Tradeable, WagerItem>();
            traderItemsWagered = new Dictionary<Tradeable, WagerItem>();
            concludeDeal();
        }

        public override Vector2 InitialSize => new Vector2(UI.screenWidth * 0.6f, UI.screenHeight * 0.6f);
        public static Vector2 scrollPos;
        public override void DoWindowContents(Rect inRect)
        {
            /*
             * Main title and description, this will always be shown
             */
            Vector2 pos = Vector2.zero;

            // title of the window  
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width * 0.2f, 30), "RimGamble.CaravanGamblingLabel".Translate());

            // description of the window
            pos.y += 30;
            Text.Font = GameFont.Small;
            GUI.color = Color.grey;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width, 60), "RimGamble.CaravanGamblingDesc".Translate());

            pos.x = 0;
            pos.y += 40;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0f, pos.y, inRect.width);
            pos.y += 10;

            GUI.color = Color.white;

            pos.y += 60;

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
                pos.y -= 60;
                gamblingUI(inRect, pos);
            }
        }

        public override void PreClose()
        {
            // close the trade session
            TradeSession.Close();
        }

        // UI that we show once the game has actually started
        private void gamblingUI(Rect inRect, Vector2 pos)
        {
            // create scroll
            Rect viewRect = new Rect(pos.x, pos.y, inRect.width - 10, 400);
            Rect scrollRect = new Rect(pos.x, pos.y, viewRect.width - 16f, tradeablesList.Count() * 26); // the list's length is the number of items in it that can be traded/wagered
            Widgets.BeginScrollView(viewRect, ref scrollPos, scrollRect);
            Text.Anchor = TextAnchor.MiddleLeft;
            // draw each row
            int row = 0;
            int colonyRow = 0;
            int traderRow = 0;



            // .Where(t => t != null && t.TraderWillTrade && t.thingsColony != null && t.thingsColony.Count > 0)
            foreach (var tradeItem in tradeablesList.Where(t => traderPawn.TraderKind.WillTrade(t.ThingDef))) // only draw rows for items that can be traded/wagered
            {
                pos.x = 0;

                if (row % 2 == 1) // on odd rows, add a different highlight
                {
                    var oddRowRect = new Rect(pos.x, pos.y + (row * 26), inRect.width, 24);
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
                    Widgets.ThingIcon(new Rect(pos.x, pos.y + (colonyRow * 26), 24, 24), tradeItem.ThingDef);
                    pos.x += 26;
                    // name of item
                    Widgets.Label(new Rect(pos.x, pos.y + (colonyRow * 26), inRect.width * 0.2f, 24), tradeItem.Label);
                    pos.x += inRect.width * 0.2f;
                    // number of item
                    Widgets.Label(new Rect(pos.x, pos.y + (colonyRow * 26), inRect.width * 0.05f, 24), ctHeldBy.ToString());
                    pos.x += inRect.width * 0.05f;

                    // entry field
                    int numItems = colonyItemsWagered[tradeItem].numItems;
                    String numItemsBuffer = colonyItemsWagered[tradeItem].numItemsBuffer;
                    // decrease button
                    var decreaseButton = new Rect(pos.x, pos.y + (colonyRow * 26), 24, 24);
                    if (Widgets.ButtonText(decreaseButton, "<") && numItems > 0)
                    {
                        numItems = Mathf.Max(0, numItems - (1 * GenUI.CurrentAdjustmentMultiplier()));
                    }
                    // number field
                    GUI.color = Color.white;
                    var wagerEntry = new Rect(decreaseButton.xMax + 5, pos.y + (colonyRow * 26), 60, 24);
                    if (numItems == 0) // set the buffer to the current wager
                    {
                        numItemsBuffer = "";
                    }
                    else
                    {
                        numItemsBuffer = numItems.ToString();
                    }
                    Widgets.TextFieldNumeric<int>(wagerEntry, ref numItems, ref numItemsBuffer);
                    if (numItems > ctHeldBy) // ensures the entry is valid (between 0 and the number of the item held by the colony)
                    {
                        numItems = ctHeldBy;
                        numItemsBuffer = numItems.ToString();
                    }
                    else if (string.IsNullOrEmpty(numItemsBuffer))
                    {
                        numItems = 0;
                    }
                    // increase button
                    var increaseButton = new Rect(wagerEntry.xMax + 5, pos.y + (colonyRow * 26), 24, 24);
                    if (Widgets.ButtonText(increaseButton, ">") && numItems <= ctHeldBy)
                    {
                        numItems = Mathf.Max(0, numItems + (1 * GenUI.CurrentAdjustmentMultiplier()));
                    }


                    colonyItemsWagered[tradeItem].numItems = numItems;
                    colonyItemsWagered[tradeItem].numItemsBuffer = numItemsBuffer;

                    colonyRow++;
                }

                /**
                 * Right side (trader side)
                 */
                if (tradeItem.thingsTrader.Count > 0)
                {
                    pos.x = inRect.width * 0.5f;
                    // icon for item
                    Widgets.ThingIcon(new Rect(pos.x, pos.y + (traderRow * 26), 24, 24), tradeItem.ThingDef);
                    pos.x += 26;
                    // name of item
                    Widgets.Label(new Rect(pos.x, pos.y + (traderRow * 26), inRect.width * 0.2f, 24), tradeItem.Label);
                    // number of item
                    Widgets.Label(new Rect(pos.x, pos.y + (traderRow * 26), inRect.width * 0.05f, 24), tradeItem.CountHeldBy(Transactor.Trader).ToString());

                    traderRow++;
                }
                row++;
            }


            // end of scrolling section
            Widgets.EndScrollView();

            // begin gamble button



            // must be set back to default
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /*
         * Method that handles receiving or taking items once the gambling session has concluded
         */
        private void concludeDeal()
        {
            if (TradeSession.deal is GamblingDeal)
            {
                foreach (var tradeable in tradeablesList)
                {
                    //if (tradeable.thingsTrader.Count > 0)
                    //{
                    //    tradeable.AdjustTo(1); // Remove 1 TEMP

                    //    tradeable.ResolveTrade(); // Adjust stock
                    //}
                    //else
                    //{
                    //    Log.Message(tradeable.Label);
                    //}

                    //if (tradeable.thingsTrader.Count > 1)
                    //{
                    //    Log.Message("Trader: " + tradeable.Label + tradeable.thingsTrader.Sum(p => p.stackCount));
                    //}
                    //if (tradeable.thingsColony.Count > 0)
                    //{
                    //    Log.Message("Colony: " + tradeable.Label + tradeable.thingsColony.Sum(p => p.stackCount));
                    //}
                    //if (!tradeable.TraderWillTrade)
                    //{
                    //    Log.Message(tradeable.Label);

                    //}
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
