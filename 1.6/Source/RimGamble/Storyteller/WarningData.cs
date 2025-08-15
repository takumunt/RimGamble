using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble
{
    public class WarningData : IExposable
    {
        public int warningEventTick;

        public WarningData() { }

        public WarningData(int warningEventTick, int delayTick)
        {
            this.warningEventTick = warningEventTick + delayTick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref warningEventTick, "warningEventTick");
        }

    }
}
