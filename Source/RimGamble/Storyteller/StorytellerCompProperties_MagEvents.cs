using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble
{
    public class StorytellerCompProperties_MagEvents : StorytellerCompProperties
    {
        public float mtbDays;
        public float eventMagnitudeMult;
        public FloatRange randomPointsFactorRange = new FloatRange(0.5f, 1.5f);
        public List<IncidentCategoryEntry> categoryWeights = new List<IncidentCategoryEntry>();
        public bool skipThreatBigIfRaidBeacon;
        public float maxThreatBigIntervalDays = 60f;

        public StorytellerCompProperties_MagEvents()
        {
            compClass = typeof(StorytellerComp_MagEvents);
        }
    }
}
