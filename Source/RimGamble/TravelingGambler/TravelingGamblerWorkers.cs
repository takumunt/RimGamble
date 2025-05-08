
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;

namespace RimGamble
{
    public interface ITravelingGamblerWorker
    {
        void OnCreated();

        void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs);
    }

    public abstract class BaseTravelingGamblerWorker : ITravelingGamblerWorker
    {
        public Pawn_TravelingGamblerTracker Tracker { get; set; }

        public virtual bool CanOccurOnDeath => false;

        public Pawn Pawn => Tracker.Pawn;

        public virtual void OnCreated()
        {
        }

        public abstract void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs);
    }

    // Acceptance Workers

    public abstract class BaseTravelingGamblerAcceptanceWorker : BaseTravelingGamblerWorker
    {
        public virtual bool CanOccur()
        {
            return true;
        }
    }

    public class TravelingGambler_DepartAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            base.Tracker.DoLeave();
        }
    }

    public class TravelingGambler_HumanBombAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            base.Tracker.DoHumanBomb();
        }
    }

    public class TravelingGamblerWorker_SabotageAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            var pawn = base.Tracker.Pawn;
            var def = base.Tracker.acceptance;
            var map = pawn.Map;

            if (pawn == null || map == null || def == null)
            {
                Log.Warning("[RimGamble] DoResponse: Invalid pawn, map, or def.");
                return;
            }

            pawn.jobs?.StopAll();

            int count = Rand.RangeInclusive(def.sabotageMinTargets, def.sabotageMaxTargets);

            var powered = map.listerBuildings.allBuildingsColonist
                .Where(b => b.GetComp<CompPowerTrader>()?.PowerOn == true).ToList();
            var others = map.listerBuildings.allBuildingsColonist
                .Where(b => b.GetComp<CompPowerTrader>() == null).ToList();

            var targets = new List<Thing>();
            targets.AddRange(powered.InRandomOrder().Take(count));
            if (targets.Count < count)
                targets.AddRange(others.InRandomOrder().Take(count - targets.Count));

            base.Tracker.sabotageTargets = targets;

            foreach (var t in targets)
            {
                pawn.jobs.jobQueue.EnqueueLast(new Job(JobDefOf.Goto, t)
                {
                    expiryInterval = 1500,
                    playerForced = true
                });

                pawn.jobs.jobQueue.EnqueueLast(new Job(JobDefOf.Wait, 180)
                {
                    playerForced = true
                });
            }

            pawn.jobs.jobQueue.EnqueueLast(new Job(JobDefOf.Wait, 60)
            {
                playerForced = true
            });

            pawn.jobs.jobQueue.EnqueueLast(new Job(DefDatabase<JobDef>.GetNamed("RimGamble_LeaveAfterSabotage")));

            RimGambleManager.Instance.QueueDelayedSabotage(pawn);
        }
    }

    // Rejection Workers

    public class TravelingGamblerWorker_DoDepart : BaseTravelingGamblerWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            base.Tracker.DoLeave();
        }
    }

    public class TravelingGamblerWorker_DoAggressive : BaseTravelingGamblerWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            base.Tracker.DoAggressive();
        }
    }

    // Aggressive Workers
    public class TravelingGamblerWorker_AggressionBasic : BaseTravelingGamblerWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            base.Tracker.DoFight();
        }
    }

    public class TravelingGamblerWorker_Raid : BaseTravelingGamblerWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            Faction faction = Find.FactionManager.RandomEnemyFaction();
            base.Tracker.DoRaid(faction);
        }
    }

    public class TravelingGamblerWorker_DelayedRaid : BaseTravelingGamblerWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            Pawn pawn = base.Tracker.Pawn;
            Faction faction = Find.FactionManager.RandomEnemyFaction();
            
            base.Tracker.DoLeave();
            RimGambleManager.Instance.QueueDelayedRaid(pawn, faction);
        }
    }

    public class TravelingGamblerWorker_Theft : BaseTravelingGamblerWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            Map map = base.Tracker.Pawn.Map;

            int totalPlayerSilver = 0;

            List<SlotGroup> slotGroups = map.haulDestinationManager.AllGroupsListForReading;
            HashSet<Thing> countedThings = new HashSet<Thing>();

            foreach (SlotGroup slotGroup in slotGroups)
            {
                foreach (IntVec3 cell in slotGroup.CellsList)
                {
                    List<Thing> things = cell.GetThingList(map);
                    foreach (Thing thing in things)
                    {
                        if (thing.def == ThingDefOf.Silver && !countedThings.Contains(thing))
                        {
                            totalPlayerSilver += thing.stackCount;
                            countedThings.Add(thing);
                        }
                    }
                }
            }

            base.Tracker.DoLeave();
            int stolenSilver = base.Tracker.DoTheft(totalPlayerSilver);

            if (stolenSilver == 0)
            {
                base.Tracker.SetAlternativeLetter(true);
            }

            namedArgs.Add(stolenSilver);
        }
    }

}
