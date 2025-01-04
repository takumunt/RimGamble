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
    public class CompUseEffect_DispenseRandomItem : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy); // Does all the effects already in DoEffect, such as camera shake

            // lootcrate behavior
            if (usedBy.Map == Find.CurrentMap)
            {
                // decide what to spawn 
                // TODO: add random items; for now, just spawn a random amount of silver
                Thing silverStack = ThingMaker.MakeThing(ThingDefOf.Silver);
                silverStack.stackCount = UnityEngine.Random.Range(1, 400);


                // Spawn the item at the location of the user (pawn)
                GenSpawn.Spawn(silverStack, usedBy.Position, usedBy.Map);
            }
        }
    }
}
