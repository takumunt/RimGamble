using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble
{
    /* Custom properties class that will hold our list of "results" of opening a crate
     */
    public class CompProperties_LootCrate : CompProperties_UseEffect
    {
        /* List of possible "results" of the loot crate opening
        */
        public List<LootItem> lootItems = new List<LootItem>();

        public CompProperties_LootCrate()
        {
            compClass = typeof(CompUseEffectLootCrate_Base);
        }
    }
}
