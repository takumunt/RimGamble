using Verse;

namespace RimGamble
{
    public class PawnTravelingGambler : ThingComp
    {
        private Pawn_TravelingGamblerTracker travelinggambler;

        public Pawn_TravelingGamblerTracker TravelingGambler
        {
            get => travelinggambler;
            set => travelinggambler = value;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref travelinggambler, "travelinggambler", 0);
        }
    }

    public class CompProperties_PawnTravelingGambler : CompProperties
    {
        public CompProperties_PawnTravelingGambler()
        {
            this.compClass = typeof(PawnTravelingGambler);
        }
    }
}
