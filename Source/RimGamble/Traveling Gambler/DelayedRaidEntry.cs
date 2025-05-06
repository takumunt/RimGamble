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
}
