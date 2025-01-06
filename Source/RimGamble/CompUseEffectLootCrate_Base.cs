using System;
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

        public CompProperties_LootCrate Props => (CompProperties_LootCrate)props;

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
                // decide what to spawn 
                Thing silverStack = ThingMaker.MakeThing(ThingDefOf.Silver);
                silverStack.stackCount = UnityEngine.Random.Range(1, 400);


                // Spawn the item at the location of the user (pawn)
                GenSpawn.Spawn(silverStack, usedBy.Position, usedBy.Map);
            }
        }
    }
}
