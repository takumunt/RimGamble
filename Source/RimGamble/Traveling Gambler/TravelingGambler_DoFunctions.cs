using System.Linq;
using Verse.AI.Group;
using Verse.AI;
using Verse;
using RimWorld;
using System.Net;
using System.Collections.Generic;
using System;

namespace RimGamble
{
    public static class TravelingGambler_DoFunctions
    {
        public static void DoLeave(Pawn pawn, ref bool hasLeft, LocomotionUrgency speed = LocomotionUrgency.Jog)
        {
            if (pawn == null || hasLeft) return;

            if (pawn.Faction != null && pawn.Faction.IsPlayer)
            {
                pawn.SetFaction(null);
            }

            LordMaker.MakeNewLord(pawn.Faction, new LordJob_ExitMapBest(speed), pawn.Map).AddPawn(pawn);
            hasLeft = true;
        }

        public static void DoFight(Pawn pawn)
        {
            if (pawn == null || pawn.guest == null || pawn.Map == null) return;

            pawn.guest.Recruitable = false;
            pawn.GetLord()?.RemovePawn(pawn);
            pawn.SetFaction(null);
            pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);

            if (pawn.Map.mapPawns.FreeColonists.Any())
            {
                pawn.mindState.enemyTarget = pawn.Map.mapPawns.FreeColonists.RandomElement();
            }

            pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
        }

        public static void DoRaid(Pawn pawn, Faction faction)
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null || faction == null || pawn == null) return;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.faction = faction;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(map);

            if (parms.points < faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat))
            {
                parms.points = faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat);
            }

            if (!IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
            {
                Log.Warning("Raid incident execution failed!");
            }

            DoFight(pawn);
        }

        public static void DoTheft(Pawn pawn, int totalPlayerSilver)
        {
            if (pawn == null || pawn.Map == null || pawn.inventory == null) return;
            if (totalPlayerSilver <= 0) return;

            int stolenSilver = Rand.Range(1, totalPlayerSilver);
            if (stolenSilver <= 0) return;

            // Remove silver from colony storage
            int remaining = stolenSilver;
            foreach (SlotGroup group in pawn.Map.haulDestinationManager.AllGroupsListForReading)
            {
                foreach (IntVec3 cell in group.CellsList)
                {
                    if (remaining <= 0)
                        break;

                    List<Thing> things = cell.GetThingList(pawn.Map);
                    foreach (Thing thing in things)
                    {
                        if (thing.def == ThingDefOf.Silver && remaining > 0)
                        {
                            int taken = Math.Min(thing.stackCount, remaining);
                            thing.SplitOff(taken).Destroy(DestroyMode.Vanish);
                            remaining -= taken;
                        }
                    }
                }
            }

            // Add stolen silver to the gambler's inventory
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = stolenSilver;
            if (!pawn.inventory.innerContainer.TryAdd(silver))
            {
                silver.Destroy(); // fallback: don't spawn on ground
            }
        }

    }
}
