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

                // Use spawn frequency setting to modify base chance
                // Lower frequency = lower chance, but make sure very low frequencies still have reasonable chances
                float baseChance = base.BaseChanceThisGame;
                
                if (RimGamble_Settings.gamblerSpawnFrequency <= 1)
                {
                    // For daily or more frequent spawns, significantly increase chance
                    return baseChance * 20f;
                }
                else if (RimGamble_Settings.gamblerSpawnFrequency <= 5)
                {
                    // For very frequent spawns (2-5 days), increase chance a lot
                    return baseChance * 10f;
                }
                else if (RimGamble_Settings.gamblerSpawnFrequency <= 15)
                {
                    // For frequent spawns (6-15 days), increase chance moderately
                    return baseChance * 5f;
                }
                else
                {
                    // For normal/slow spawns, use frequency modifier
                    float frequencyModifier = 60f / Mathf.Max(1f, RimGamble_Settings.gamblerSpawnFrequency);
                    return baseChance * frequencyModifier;
                }
            }
        }
    }
}
