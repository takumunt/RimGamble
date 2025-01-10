using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimGamble
{
    public class WorkGiver_RefuelGachaMachine : WorkGiver_Scanner
    {
        public virtual JobDef JobStandard => RimGamble_DefOf.RimGamble_RefuelGachaMachines;

        public virtual bool CanRefuelThing(Thing t)
        {
            return !(t is Building_Turret);
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var list = new List<Thing>();

            foreach (var gachaDef in RimGamble_StaticCollections.compGachaMachines)
                list.AddRange(pawn.Map.listerThings.ThingsOfDef(gachaDef));

            return list;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (CanRefuelThing(t))
            {
                return GachaRefuelWorkGiverUtility.CanRefuel(pawn, t, forced);
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return GachaRefuelWorkGiverUtility.RefuelJob(pawn, t, forced, JobStandard);
        }
    }
}
