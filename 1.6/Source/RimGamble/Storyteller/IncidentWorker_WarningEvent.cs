using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimGamble
{
    public class IncidentWorker_WarningEvent : IncidentWorker
    {
        private const int PawnStayDurationMax = 120000;

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            // we need the map to spawn the pawn
            Map map = (Map) parms.target;
            if (map == null)
            {
                return false;  // Fail if we don't have a valid map
            }

            // create the pawn we use to tell the colony
            Pawn warningPawn = CreateWarningPawn();
            if (warningPawn == null)
            {
                return false;
            }

            // spawn the pawn
            if (!RCellFinder.TryFindRandomPawnEntryCell(out var result, map, CellFinder.EdgeRoadChance_Friendly, allowFogged: false, (IntVec3 cell) => map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.PassDoors))))
            {
                return false;
            }

            GenSpawn.Spawn(warningPawn, result, map);
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(warningPawn, out var result2))
            {
                return false;
            }
            RCellFinder.TryFindRandomSpotJustOutsideColony(warningPawn, out var res);
            LordMaker.MakeNewLord(warningPawn.Faction, new LordJob_VisitColony(parms.faction, res), map).AddPawn(warningPawn);

            // create a letter event to tell the colony about the pawn
            Find.LetterStack.ReceiveLetter("Mysterious Figure Approaches", "A mysterious figure approaches bringing news.", LetterDefOf.NeutralEvent, warningPawn, null, null);

            return true;
        }


        // creates the pawn that we use to tell the colony about the incoming event
        // this pawn can be interacted with using right click (bringing up a float menu) which will then give the colony the relevant message
        // upon hearing the message, the pawn moves away
        // alternatively, the pawn may depart on its own if no one interacts with it for a period of time
        private Pawn CreateWarningPawn()
        {
            // get a list of all factions that could send a nonhostile pawn to the player
            List<Faction> validFactions = Find.FactionManager.AllFactions.Where(f => !f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction && f != Faction.OfPlayer).ToList();
            // randomly choose one of the factions from the list, or if no faction exists, use null for a pawn with no faction alignment
            Faction faction = validFactions.Any() ? validFactions.RandomElement() : null;

            Pawn warningPawn = PawnGenerator.GeneratePawn(PawnKindDef.Named("RimGamble_WarningPawn"), faction);
            warningPawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + PawnStayDurationMax;


            return warningPawn;
        }
    }
}
