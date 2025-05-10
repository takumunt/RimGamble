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
        public static void DoLeave(Pawn pawn, ref bool hasLeft, LocomotionUrgency speed = LocomotionUrgency.Sprint)
        {
            if (pawn == null || hasLeft) return;

            // Remove any existing Lord
            Lord oldLord = pawn.GetLord();
            if (oldLord != null)
            {
                oldLord.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
            }

            // Clear any job the pawn is doing
            pawn.jobs?.StopAll();

            // Set faction to null (optional depending on your mod)
            if (pawn.Faction != null && pawn.Faction.IsPlayer)
            {
                pawn.SetFaction(null);
            }

            // Send pawn to map edge
            IntVec3 exitCell = CellFinder.RandomEdgeCell(pawn.Map);
            var lordJob = new LordJob_ExitMapNear(exitCell, speed);
            LordMaker.MakeNewLord(pawn.Faction, lordJob, pawn.Map).AddPawn(pawn);

            hasLeft = true;
        }

        public static void DoFight(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || !pawn.Spawned) return;

            pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
            pawn.jobs?.StopAll();
            pawn.SetFaction(FactionUtility.DefaultFactionFrom(FactionDefOf.Pirate));

            if (pawn.InMentalState && pawn.mindState.mentalStateHandler.CurState != null)
            {
                pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
            }

            LordMaker.MakeNewLord(pawn.Faction,
                new LordJob_AssaultColony(pawn.Faction),
                pawn.Map,
                new List<Pawn> { pawn });
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

        public static int DoTheft(Pawn pawn, int totalPlayerSilver)
        {
            if (pawn == null || pawn.Map == null || pawn.inventory == null) return -1;
            if (totalPlayerSilver <= 0) return 0;

            int maxStealable = (int)(totalPlayerSilver * 0.75f);
            int stolenSilver = Rand.Range(1, Math.Max(1, maxStealable));
            if (stolenSilver <= 0) return 0;

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

            return stolenSilver;
        }

        public static void DoHumanBomb(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                Log.Warning("HumanBombUtility: Invalid pawn or pawn not spawned.");
                return;
            }

            Map map = pawn.Map;
            IntVec3 center = pawn.Position;
            int numberOfStrikes = Rand.RangeInclusive(3, 5);

            for (int i = 0; i < numberOfStrikes; i++)
            {
                IntVec3 strikePos = center + GenRadial.RadialPattern[i];
                if (strikePos.InBounds(map))
                {
                    GenExplosion.DoExplosion(
                        strikePos,
                        map,
                        2.75f,
                        DamageDefOf.Bomb,
                        pawn,
                        -1,
                        -1f,
                        null,
                        null,
                        null,
                        null,
                        null,
                        1f,
                        1,
                        null,
                        false,
                        null,
                        1
                    );
                }
            }
        }

        public static void DoStatusEffect(Pawn pawn, TravelingGamblerAcceptanceDef acceptance, string thoughtDefName)
        {
            if (pawn == null || pawn.Map == null || acceptance == null) return;

            RimGambleManager.Instance?.ApplyThoughtToColony(thoughtDefName);
        }
    
    }
}
