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

        public abstract StakeItem UpdateTraderWager(List<Tradeable> keys);

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

            public override StakeItem UpdateTraderWager(List<Tradeable> keys)
            {
                var randomSelectedItem = keys[UnityEngine.Random.Range(0, keys.Count)];

                int newWagerCt = UnityEngine.Random.Range(1, randomSelectedItem.CountHeldBy(Transactor.Trader));
                float itemMarketVal = randomSelectedItem.BaseMarketValue;

                return new StakeItem(randomSelectedItem, newWagerCt, itemMarketVal);
            }
        }

        public class AggressiveGambler : CaravanGambleAI
        {
            public AggressiveGambler()
            {
                wagerInterval = 3f;
                wagerBias = 0.1f;
            }

            public override StakeItem UpdateTraderWager(List<Tradeable> keys)
            {
                var randomSelectedItem = keys[UnityEngine.Random.Range(0, keys.Count)];

                int newWagerCt = UnityEngine.Random.Range(1, randomSelectedItem.CountHeldBy(Transactor.Trader));
                float itemMarketVal = randomSelectedItem.BaseMarketValue;

                return new StakeItem(randomSelectedItem, newWagerCt, itemMarketVal);
            }
        }

        public class LastMinuteGambler : CaravanGambleAI
        {
            public LastMinuteGambler()
            {
                wagerInterval = 5f;
                wagerBias = 0.9f;
            }

            public override StakeItem UpdateTraderWager(List<Tradeable> keys)
            {
                var randomSelectedItem = keys[UnityEngine.Random.Range(0, keys.Count)];

                int newWagerCt = UnityEngine.Random.Range(1, randomSelectedItem.CountHeldBy(Transactor.Trader));
                float itemMarketVal = randomSelectedItem.BaseMarketValue;

                return new StakeItem(randomSelectedItem, newWagerCt, itemMarketVal);
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
        };

        public static CaravanGambleAI PickRandomAI()
        {
            return aiTypes[UnityEngine.Random.Range(0, aiTypes.Count)]();
        }
    }
}
