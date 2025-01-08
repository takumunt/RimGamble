using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimGamble
{
    public class LootItem_Category : LootItem
    {
        public ThingCategoryDef category;
        public List<ThingDef> exclude = new List<ThingDef>();

    }
}
