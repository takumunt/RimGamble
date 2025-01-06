﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble
{
    /* Class that defines a LootItem, which contains information about a given "result" of a loot crate opening.
     * Information includes: chance of the item, possible quantity of the item, name of the item
     */
    public class LootItem
    {
        public ThingDef item;
        public int itemQuantMin;
        public int itemQuantMax;
        public int itemWeight;

        // default constructor
        public LootItem() { }
    }
}
