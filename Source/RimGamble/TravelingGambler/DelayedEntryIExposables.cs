using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble
{
    public class DelayedRaidEntry : IExposable
    {
        public Pawn pawn;
        public Faction faction;
        public int triggerAfterTick;

        public string raidLetterLabel;
        public string raidLetterDesc;
        public LetterDef raidLetterDef;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref triggerAfterTick, "triggerAfterTick");
            Scribe_Values.Look(ref raidLetterLabel, "raidLetterLabel");
            Scribe_Values.Look(ref raidLetterDesc, "raidLetterDesc");
            Scribe_Defs.Look(ref raidLetterDef, "raidLetterDef");
        }
    }

    public class DelayedSabotageEntry : IExposable
    {
        public List<Thing> targets;
        public int triggerTick;
        public string label;
        public string desc;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref targets, "targets", LookMode.Reference);
            Scribe_Values.Look(ref triggerTick, "triggerTick");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref desc, "desc");
        }
    }

    public class DelayedTradeCaravanEntry : IExposable
    {
        public Pawn pawn;
        public Faction faction;
        public Map map;
        public TraderKindDef traderKind;
        public int triggerAfterTick;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref map, "map");
            Scribe_Defs.Look(ref traderKind, "traderKind");
            Scribe_Values.Look(ref triggerAfterTick, "triggerAfterTick");
        }
    }

}
