﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;

namespace RimGamble
{
    /* 
     * all other loot crate classes will inherit from this
     */
    public class CompUseEffectLootCrate_Base : CompUseEffect
    {

        public ModExtension_LootCrate Props => parent.def.GetModExtension<ModExtension_LootCrate>();



        /* Override of the DoEffect method from CompUseEffect, runs the original method then calls another 
         * method to open the box */
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy); // Does all the effects already in DoEffect, such as camera shake

            OpenCrate(usedBy);
        }

        protected void OpenCrate(Pawn usedBy)
        {
            // lootcrate opening behavior
            if (usedBy.Map == Find.CurrentMap)
            {
                // default LootItem (in case something goes wrong)
                LootItem gift = null;

                // sum the weights of all the possible rewards then randomly select
                int totalWeight = 0;
                List<int> cumulWeight = new List<int>();
                foreach (LootItem lootItem in Props.lootItems)
                {
                    totalWeight += lootItem.itemWeight;
                    cumulWeight.Add(totalWeight);
                }

                // select a random number in the range 
                int randNum = UnityEngine.Random.Range(0, totalWeight);

                // use it to select a random gift based on weight
                for (int i = 0; i < cumulWeight.Count; i++)
                {
                    if (randNum < cumulWeight[i])
                    {
                        gift = Props.lootItems[i];
                        break;
                    }
                }

                // present the gift with a random amount based on the LootItem's fields
                if (gift != null)
                {

                    Thing giftItem = null;
                    if (gift is LootItem_SingleDef singleDefGift)
                    {
                        giftItem = ThingMaker.MakeThing(singleDefGift.item);

                    }
                    else if (gift is LootItem_Category categoryGift)
                    {
                        giftItem = ThingMaker.MakeThing(categoryGift.category.childThingDefs.RandomElement());

                    }
                    // spawn the item
                    giftItem.stackCount = UnityEngine.Random.Range(gift.itemQuantMin, gift.itemQuantMax);
                    GenSpawn.Spawn(giftItem, usedBy.Position, usedBy.Map);
                    //spawnItems(giftitem, usedBy.Position, usedBy.Map);
                }
            }
        }

        /* given a location, the map, and a LootItem, attempts to spawn that item(s) on the given position
         * If there are too many items to fit on one tile, the rest will overflow onto adjacent open tiles
         * If there are no free tiles, the leftover items are destroyed
         */
        private void spawnItems(LootItem giftItem, IntVec3 pos, Map map)
        {

        }
    }
}
