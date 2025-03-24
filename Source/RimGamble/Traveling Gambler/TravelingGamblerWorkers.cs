
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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
            Log.Message("Raiding " + faction.Name);
            base.Tracker.DoRaid(faction);
        }
    }

}
