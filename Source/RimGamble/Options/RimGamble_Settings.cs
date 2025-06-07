using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Verse;

namespace RimGamble.Options
{
    public class RimGamble_Settings : ModSettings
    {

        public const int bigEventMtbBase = 3;

        public static int bigEventMtb = bigEventMtbBase;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref bigEventMtb, "bigEventMtb", bigEventMtbBase, true);
        }

        public static void DoWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();

            ls.Begin(inRect);
            ls.Gap(12f);
            ls.Label("bigEventMtb".Translate() + ": " +  (int) (bigEventMtb) +  "days");
            bigEventMtb = (int) ls.Slider(bigEventMtb, 1, 10);

            ls.End();
        }
    }
}
