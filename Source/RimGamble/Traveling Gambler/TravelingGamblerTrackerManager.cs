using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimGamble
{
    public static class TravelingGamblerTrackerManager
    {
        private static Dictionary<Pawn, Pawn_TravelingGamblerTracker> trackers = new Dictionary<Pawn, Pawn_TravelingGamblerTracker>();

        public static void AddTracker(Pawn pawn)
        {
            if (!trackers.ContainsKey(pawn))
            {
                trackers[pawn] = new Pawn_TravelingGamblerTracker(pawn);
            }
        }

        public static bool HasTracker(Pawn pawn)
        {
            return trackers.ContainsKey(pawn);
        }

        public static Pawn_TravelingGamblerTracker GetTracker(Pawn pawn)
        {
            if (trackers.TryGetValue(pawn, out var tracker))
                return tracker;

            return null;
        }
    }

    public static class TravelingGamblerDefLoader
    {
        private static HashSet<PawnKindDef> travelingGamblerDefs = new HashSet<PawnKindDef>();

        public static void LoadDefs()
        {
            travelingGamblerDefs.Clear();

            foreach (var def in DefDatabase<TravelingGamblerFormKindDef>.AllDefs)
            {
                travelingGamblerDefs.Add(def);
            }
        }

        public static bool IsTravelingGambler(PawnKindDef kindDef)
        {
            return kindDef != null && travelingGamblerDefs.Contains(kindDef);
        }
    }

    public class TravelingGamblerMod : Mod
    {
        public TravelingGamblerMod(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(TravelingGamblerDefLoader.LoadDefs, null, false, null);
        }
    }


}
