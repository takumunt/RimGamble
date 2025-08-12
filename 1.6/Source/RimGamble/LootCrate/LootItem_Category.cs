using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble
{
    public class LootItem_Category : LootItem
    {
        public ThingCategoryDef category;
        public List<ThingDef> exclude = new List<ThingDef>();
        public int rareModif = 2; // variable used to change how the random generation of quality is handled (where applicable) the given value is where quality will cluster around
        public float widthFactor = 0; // variable used to change how clustered the random generation of quality is handled
        public TechLevel maxTechLevelGenerate = TechLevel.Archotech; // max level of technology the item collection can be
    }
}
