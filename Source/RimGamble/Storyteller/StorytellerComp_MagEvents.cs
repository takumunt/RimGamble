using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Noise;

namespace RimGamble
{
    public class StorytellerComp_MagEvents : StorytellerComp
    { 
        protected StorytellerCompProperties_MagEvents Props => (StorytellerCompProperties_MagEvents)props;
         
        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            // first check if we need to fire off any large events
            int numEventsToFire = RimGambleManager.Instance.CheckIfMagEventFire();
            if (numEventsToFire > 0)
            {
                bool flag = target.IncidentTargetTags().Contains(IncidentTargetTagDefOf.Map_RaidBeacon);
                List<IncidentCategoryDef> list = new List<IncidentCategoryDef>();

                for (int i = 0; i < numEventsToFire; i++)
                {
                    do
                    {
                        IncidentCategoryDef incidentCategoryDef = ChooseRandomCategory(target, list);
                        IncidentParms parmsBig = GenerateParms(incidentCategoryDef, target);
                        if (TrySelectRandomIncident(UsableIncidentsInCategory(incidentCategoryDef, parmsBig), out var foundDef, target))
                        {
                            if (!(Props.skipThreatBigIfRaidBeacon && flag) || foundDef.category != IncidentCategoryDefOf.ThreatBig)
                            {
                                yield return new FiringIncident(foundDef, this, parmsBig);
                            }

                            break;
                        }

                        list.Add(incidentCategoryDef);
                    }
                    while (list.Count < Props.categoryWeights.Count);
                }
            }

            // try and roll for the warning event if possible
            // dont roll if there is already a big event scheduled
            if (!Rand.MTBEventOccurs(Props.mtbDays, 60000f, 1000f))
            {
                yield break;
            }

            IncidentDef warningEvent = RimGamble_DefOf.RimGamble_WarningEvent;
            IncidentParms parms = GenerateParms(IncidentCategoryDefOf.Misc, target);

            yield return new FiringIncident(warningEvent, this, parms);

            RimGambleManager.Instance.SetWarningEvent((GenDate.TicksPerDay * Rand.Range(1, 3)));
        }

        private IncidentCategoryDef ChooseRandomCategory(IIncidentTarget target, List<IncidentCategoryDef> skipCategories)
        {
            if (!skipCategories.Contains(IncidentCategoryDefOf.ThreatBig))
            {
                int num = Find.TickManager.TicksGame - target.StoryState.LastThreatBigTick;
                if (target.StoryState.LastThreatBigTick >= 0 && (float)num > 60000f * Props.maxThreatBigIntervalDays)
                {
                    return IncidentCategoryDefOf.ThreatBig;
                }
            }

            return Props.categoryWeights.Where((IncidentCategoryEntry cw) => !skipCategories.Contains(cw.category)).RandomElementByWeight((IncidentCategoryEntry cw) => cw.weight).category;
        }

        // custom override of generateparms that is identical to the one found in randommain, except the points are multiplied
        public override IncidentParms GenerateParms(IncidentCategoryDef incCat, IIncidentTarget target)
        {
            IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(incCat, target);
            if (incidentParms.points >= 0f)
            {
                incidentParms.points *= Props.randomPointsFactorRange.RandomInRange;
                incidentParms.points *= Props.eventMagnitudeMult;
            }

            return incidentParms;
        }
    }
}
