using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using static RimWorld.PsychicRitualRoleDef;


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
                    QualityCategory? qual = null;
                    if (gift is LootItem_SingleDef singleDefGift)
                    {
                        giftItem = ThingMaker.MakeThing(singleDefGift.item);

                    }
                    else if (gift is LootItem_Category categoryGift)
                    {
                        ThingDef stuff = null;
                        if (!categoryGift.category.DescendantThingDefs.Where((ThingDef t) => (int)t.techLevel <= (int)categoryGift.maxTechLevelGenerate && (categoryGift.exclude == null || !categoryGift.exclude.Contains(t))).TryRandomElement(out var giftItemPreMake))
                        {
                            Log.Error("Could not generate a valid item for the loot crate.");
                        }

                        // set the material of the item (if applicable)
                        if (giftItemPreMake.MadeFromStuff)
                        {
                            stuff = GenStuff.RandomStuffByCommonalityFor(giftItemPreMake);
                        }

                        giftItem = ThingMaker.MakeThing(giftItemPreMake, stuff);

                        // set the quality of the item (if applicable)
                        if (giftItem.TryGetComp<CompQuality>() != null)
                        {
                            qual = generateQual(categoryGift.rareModif, categoryGift.widthFactor);
                        }
                    }
                    // spawn the item
                    giftItem.stackCount = UnityEngine.Random.Range(gift.itemQuantMin, gift.itemQuantMax);
                    spawnItems(giftItem, usedBy.Position, usedBy.Map, qual);
                }
            }
        }

        /* given a location, the map, and a LootItem, attempts to spawn that item(s) on the given position
         * If there are too many items to fit on one tile, the rest will overflow onto adjacent open tiles
         */
        private void spawnItems(Thing giftItem, IntVec3 pos, Map map, QualityCategory? qual)
        {
            // split the stack into separate chunks if it is too large
            int lim = giftItem.def.stackLimit; // stack limit of the item

            while (giftItem.stackCount > 0)
            {
                int amountToSpawn = Math.Min(giftItem.stackCount, lim);

                Thing itemToSpawn = ThingMaker.MakeThing(giftItem.def, giftItem.Stuff);
                itemToSpawn.stackCount = amountToSpawn;

                if (qual != null)
                {
                    CompQuality compQuality = itemToSpawn.TryGetComp<CompQuality>();
                    compQuality.SetQuality((QualityCategory) qual, ArtGenerationContext.Colony);
                }

                GenSpawn.Spawn(itemToSpawn, pos, map);

                giftItem.stackCount -= amountToSpawn;

            }

        }

        // uses a gaussian distribution to generate a random quality given a quality to cluster values around
        public QualityCategory generateQual(int rareModif, float widthFactor)
        {
            var values = Enum.GetValues(typeof(QualityCategory)).Cast<byte>();
            byte min = values.Min();
            byte max = values.Max();

            float num = 2; // the rarity is normal by default

            // generate a float that tends to cluster around the given rareModif
            num = Rand.Gaussian((float)(int)rareModif + 0.5f, widthFactor);

            // ensure that any given value falls within an acceptable range
            if (num < (float)(int)min)
            {
                num = (int)min;
            }
            if (num > (float)(int)max)
            {
                num = (int)max;
            }

            return (QualityCategory)(int)num;
        }
    }
}
