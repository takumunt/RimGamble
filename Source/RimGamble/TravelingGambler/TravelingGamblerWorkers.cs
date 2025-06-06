﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using RimWorld.QuestGen;
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

    public class TravelingGamblerWorker_MoodAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            var def = base.Tracker.acceptance;

            base.Tracker.DoMoodEffect(def.moodEffect);
        }
    }

    public class TravelingGamblerWorker_TheftAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            Pawn pawn = base.Tracker.Pawn;
            Map map = pawn.Map;

            int totalPlayerSilver = 0;
            List<SlotGroup> slotGroups = map.haulDestinationManager.AllGroupsListForReading;
            HashSet<Thing> countedThings = new HashSet<Thing>();

            foreach (SlotGroup slotGroup in slotGroups)
            {
                foreach (IntVec3 cell in slotGroup.CellsList)
                {
                    foreach (Thing thing in cell.GetThingList(map))
                    {
                        if (thing.def == ThingDefOf.Silver && !countedThings.Contains(thing))
                        {
                            totalPlayerSilver += thing.stackCount;
                            countedThings.Add(thing);
                        }
                    }
                }
            }

            if (countedThings.Count == 0 || totalPlayerSilver <= 0)
            {
                base.Tracker.SetAlternativeLetter(true);
                base.Tracker.DoLeave();
                namedArgs.Add(pawn.Named("PAWN"));
                return;
            }

            Thing targetSilver = countedThings.FirstOrDefault();
            if (targetSilver != null)
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("RimGamble_StealSilver"), targetSilver);
                job.count = totalPlayerSilver; // Pass actual silver total
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job);

                namedArgs.Add(pawn.Named("PAWN"));
            }
            else
            {
                base.Tracker.SetAlternativeLetter(true);
                base.Tracker.DoLeave();
                namedArgs.Add(pawn.Named("PAWN"));
            }
        }
    }

    public class TravelingGambler_TradeCaravanAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            Pawn pawn = base.Tracker.Pawn;
            var def = base.Tracker.acceptance;
            Map map = pawn?.Map;

            if (pawn == null || map == null || def == null)
            {
                Log.Warning("[RimGamble] DoResponse: Invalid pawn, map, or acceptance def.");
                return;
            }

            pawn.jobs?.StopAll();

            pawn.jobs.jobQueue.EnqueueLast(new Job(JobDefOf.Wait, 60) { playerForced = true });
            pawn.jobs.jobQueue.EnqueueLast(new Job(DefDatabase<JobDef>.GetNamed("RimGamble_SpawnTradeCaravan")));

            // Pick a random valid trading faction
            Faction faction = Find.FactionManager.AllFactions
                .Where(f => !f.IsPlayer && f.def.caravanTraderKinds != null && f.def.caravanTraderKinds.Any())
                .RandomElementWithFallback();

            if (faction == null)
            {
                Log.Warning("[RimGamble] Could not find any faction with caravanTraderKinds.");
                return;
            }

            TraderKindDef traderKind = faction.def.caravanTraderKinds
                .Where(k => k != null)
                .RandomElementWithFallback();

            if (traderKind == null)
            {
                Log.Warning($"[RimGamble] Faction '{faction.Name}' has no usable TraderKindDefs.");
                return;
            }

            // Queue the caravan
            RimGambleManager.Instance.QueueDelayedTradeCaravan(pawn, faction, traderKind, 6000);
        }
    }

    public class TravelingGamblerWorker_DropPodAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            base.Tracker.DoLeave();
            base.Tracker.DoSpawnDropPod();
        }
    }

    public class TravelingGamblerWorker_TeachSkillAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            base.Tracker.DoLeave();
            var (learner, skill) = base.Tracker.DoTeachSkill();

            if (learner != null && skill != null)
            {
                namedArgs.Add(learner.Named("LEARNER"));
                namedArgs.Add(skill.skillLabel.CapitalizeFirst().Named("SKILL"));
            }
        }
    }

    public class TravelingGamblerWorker_GiveInspirationAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            base.Tracker.DoLeave();
            var (learner, inspiration) = base.Tracker.DoGiveInspiration();

            if (learner != null && inspiration != null)
            {
                namedArgs.Add(learner.Named("LEARNER"));
                namedArgs.Add(inspiration.Named("INSPIRATION"));
            }
        }
    }

    public class TravelingGamblerWorker_AskJoinAcceptance : BaseTravelingGamblerAcceptanceWorker
    {
        public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
        {
            var tracker = Tracker;
            if (tracker == null || tracker.Pawn == null)
            {
                Log.Warning("[Gambler] JoinAcceptance failed: missing tracker or pawn.");
                return;
            }

            Pawn pawn = tracker.Pawn;

            var slate = new Slate();
            slate.Set("pawn", pawn);

            try
            {
                QuestUtility.GenerateQuestAndMakeAvailable(
                    DefDatabase<QuestScriptDef>.GetNamed("GamblerJoinDecisionQuest"),
                    slate
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[Gambler] Failed to start GamblerJoinDecisionQuest: {ex}");
            }
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
            Pawn pawn = base.Tracker.Pawn;
            Map map = pawn.Map;

            int totalPlayerSilver = 0;
            List<SlotGroup> slotGroups = map.haulDestinationManager.AllGroupsListForReading;
            HashSet<Thing> countedThings = new HashSet<Thing>();

            foreach (SlotGroup slotGroup in slotGroups)
            {
                foreach (IntVec3 cell in slotGroup.CellsList)
                {
                    foreach (Thing thing in cell.GetThingList(map))
                    {
                        if (thing.def == ThingDefOf.Silver && !countedThings.Contains(thing))
                        {
                            totalPlayerSilver += thing.stackCount;
                            countedThings.Add(thing);
                        }
                    }
                }
            }

            if (countedThings.Count == 0 || totalPlayerSilver <= 0)
            {
                base.Tracker.SetAlternativeLetter(true);
                base.Tracker.DoLeave();
                namedArgs.Add(pawn.Named("PAWN"));
                return;
            }

            Thing targetSilver = countedThings.FirstOrDefault();
            if (targetSilver != null)
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("RimGamble_StealSilver"), targetSilver);
                job.count = totalPlayerSilver; // Pass actual silver total
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job);

                namedArgs.Add(pawn.Named("PAWN"));
            }
            else
            {
                base.Tracker.SetAlternativeLetter(true);
                base.Tracker.DoLeave();
                namedArgs.Add(pawn.Named("PAWN"));
            }
        }
    }

}
