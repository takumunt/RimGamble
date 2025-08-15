using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RimGamble.CaravanGambleAI;

namespace RimGamble
{
    /*
     * Set of classes that define various AI behaviors for caravan gambling
     * Upon starting of a wager, a random behavior is chosen
     */
    public abstract class CaravanGambleAI
    {
        // How long between checks for addition of items
        public float wagerInterval = 5f;

        // how biased item additions are: closer to 0 makes items more common at the beginning, closer to 1 makes them more common at the end
        public float wagerBias = 0.5f;

        public virtual List<StakeItem> addTraderWager(List<Tradeable> keys, int colonyWagerVal, int traderWagerVal, Dictionary<Tradeable, WagerItem> traderItemsWagered)
        {
            var randomSelectedItem = keys[UnityEngine.Random.Range(0, keys.Count)];

            int newWagerCt = UnityEngine.Random.Range(1, randomSelectedItem.CountHeldBy(Transactor.Trader));

            List<StakeItem> itemsToWager = new List<StakeItem>();
            itemsToWager.Add(new StakeItem(randomSelectedItem, newWagerCt));

            return itemsToWager;
        }

        /*
         * Randomly selects an item and quantity every 5 time units
         * 
         */
        public class RandomGambler : CaravanGambleAI
        {
            public RandomGambler()
            {
                wagerInterval = 5f;
                wagerBias = 0.5f;
            }
        }

        public class AggressiveGambler : CaravanGambleAI
        {
            public AggressiveGambler()
            {
                wagerInterval = 3f;
                wagerBias = 0.1f;
            }

            public override List<StakeItem> addTraderWager(List<Tradeable> keys, int colonyWagerVal, int traderWagerVal, Dictionary<Tradeable, WagerItem> traderItemsWagered)
            {
                // if at any point when we want to add more items, the colony's wager is larger, we will try and make our wager as large as possible.
                if (colonyWagerVal > traderWagerVal)
                {
                    List<StakeItem> itemsToWager = new List<StakeItem>();
                    for (int i = 0; i < 3; i++)
                    {
                        itemsToWager.AddRange(base.addTraderWager(keys, colonyWagerVal, traderWagerVal, traderItemsWagered));
                    }
                    return itemsToWager;
                }
                // otherwise just do a normal random wager
                return base.addTraderWager(keys, colonyWagerVal, traderWagerVal, traderItemsWagered);
            }
        }

        public class LastMinuteGambler : CaravanGambleAI
        {
            public LastMinuteGambler()
            {
                wagerInterval = 5f;
                wagerBias = 0.9f;
           }
        }

        public class CowardGambler : CaravanGambleAI
        {
            public CowardGambler()
            {
                wagerInterval = 5f;
                wagerBias = 0.5f;
            }

            public override List<StakeItem> addTraderWager(List<Tradeable> keys, int colonyWagerVal, int traderWagerVal, Dictionary<Tradeable, WagerItem> traderItemsWagered)
            {
                // if the colony appears to be in a good position to win, the ai folds and tries to revoke some bets
                if (colonyWagerVal > 1000 && colonyWagerVal > traderWagerVal)
                {
                    List<StakeItem> itemsToWager = new List<StakeItem>();

                    for (int i = 0; i < 3; i++)
                    {
                        Tradeable itemToRevoke = traderItemsWagered.Keys.ToList()[UnityEngine.Random.Range(0, traderItemsWagered.Count)];
                        itemsToWager.Add(new StakeItem(itemToRevoke, 0));
                    }
                    return itemsToWager;
                }

                return base.addTraderWager(keys, colonyWagerVal, traderWagerVal, traderItemsWagered);

            }
        }
    }   

    public static class AIPicker
    {
        private static List<Func<CaravanGambleAI>> aiTypes = new List<Func<CaravanGambleAI>>()
        {
            () => new RandomGambler(),
            () => new AggressiveGambler(),
            () => new LastMinuteGambler(),
            () => new CowardGambler(),
        };

        public static CaravanGambleAI PickRandomAI()
        {
            return aiTypes[UnityEngine.Random.Range(0, aiTypes.Count)]();
        }
    }
}
