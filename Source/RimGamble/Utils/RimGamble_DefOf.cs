using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble
{
    [DefOf]
    public static class RimGamble_DefOf
    {
        // JobDef
        public static JobDef RimGamble_StartOnlineGambling;
        public static JobDef RimGamble_StartCaravanGambling;
        public static JobDef RimGamble_PlayGachaMachine;
        public static JobDef RimGamble_PlaySlotMachine;
        public static JobDef RimGamble_RefuelGachaMachines;
        public static JobDef RimGamble_TalkWarningPawn;

        // IncidentDef
        public static IncidentDef RimGamble_WarningEvent;
    }
}
