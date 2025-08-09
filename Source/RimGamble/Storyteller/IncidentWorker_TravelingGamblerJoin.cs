using RimWorld;
using RimWorld.QuestGen;
using Verse;
using UnityEngine;
using RimGamble.Options;

namespace RimGamble
{
    public class IncidentWorker_TravelingGamblerJoin : IncidentWorker_GiveQuest
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // First check if traveling gamblers are enabled in settings
            if (!RimGamble_Settings.enableTravelingGambler)
            {
                return false;
            }

            // Check minimum wealth requirement if set
            if (RimGamble_Settings.gamblerMinWealth > 0)
            {
                Map map = (Map)parms.target;
                if (map != null)
                {
                    float currentWealth = map.wealthWatcher.WealthTotal;
                    if (currentWealth < RimGamble_Settings.gamblerMinWealth)
                    {
                        return false;
                    }
                }
            }

            // Call the base implementation to check other requirements
            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            // Double-check settings before executing (safety check)
            if (!RimGamble_Settings.enableTravelingGambler)
            {
                return false;
            }

            // Execute the quest normally if settings allow
            return base.TryExecuteWorker(parms);
        }

        public override float BaseChanceThisGame
        {
            get
            {
                // If disabled, return 0 chance
                if (!RimGamble_Settings.enableTravelingGambler)
                {
                    return 0f;
                }

                // Use the base chance from the storyteller system
                // Frequency is now controlled by the XML patch mtbDays value
                return base.BaseChanceThisGame;
            }
        }
    }
}
