using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimGamble
{
    public class Alert_TravelingGamblerTimeout : Alert
    {
        private const int WarningTicks = 30000; // 12 in-game hours

        private readonly List<Pawn> gamblers = new List<Pawn>();
        private readonly StringBuilder sb = new StringBuilder();

        private List<Pawn> Gamblers
        {
            get
            {
                gamblers.Clear();
                foreach (Map map in Find.Maps)
                {
                    foreach (Pawn pawn in map.mapPawns.AllHumanlikeSpawned)
                    {
                        if (TravelingGamblerTrackerManager.HasTracker(pawn))
                        {
                            var tracker = TravelingGamblerTrackerManager.GetTracker(pawn);
                            if (tracker != null && tracker.IsOnEntryLord &&
                                GenTicks.TicksAbs >= tracker.timeoutAt - WarningTicks)
                            {
                                gamblers.Add(pawn);
                            }
                        }
                    }
                }
                return gamblers;
            }
        }

        public Alert_TravelingGamblerTimeout()
        {
            defaultPriority = AlertPriority.High;
        }

        public override string GetLabel()
        {
            if (gamblers.NullOrEmpty())
                return string.Empty;

            return "TravelingGamblerTimeout".Translate();
        }

        public override TaggedString GetExplanation()
        {
            if (gamblers.NullOrEmpty())
                return string.Empty;

            sb.Length = 0;
            foreach (Pawn pawn in gamblers)
            {
                var tracker = TravelingGamblerTrackerManager.GetTracker(pawn);
                int timeRemaining = Mathf.Max(tracker.timeoutAt - GenTicks.TicksAbs, 0);
                sb.AppendLineTagged($"  - {pawn.LabelShortCap.Colorize(ColoredText.NameColor)}, {tracker.form.label.Colorize(Color.gray)}: {timeRemaining.ToStringTicksToPeriodVerbose()}");
            }

            return "TravelingGamblerTimeoutDesc".Translate() + ":\n\n" + sb.ToString();
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(Gamblers);
        }
    }
}
