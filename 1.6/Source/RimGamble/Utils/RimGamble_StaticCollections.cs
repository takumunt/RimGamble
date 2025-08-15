using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimGamble
{
    [StaticConstructorOnStartup]
    public static class RimGamble_StaticCollections
    {
        public static List<ThingDef> compGachaMachines = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def => def.comps.Any(comp => comp is CompProperties_GachaRefuelable)).ToList();
    }
}
