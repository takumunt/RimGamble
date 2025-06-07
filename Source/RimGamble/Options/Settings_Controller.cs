using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using UnityEngine;
using Verse;

namespace RimGamble.Options
{
     class RimGamble_Mod : Mod
    {
        public RimGamble_Mod(ModContentPack content) : base(content)
        {
            GetSettings<RimGamble_Settings>(); 
        }

        public override string SettingsCategory() => "RimGamble";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            RimGamble_Settings.DoWindowContents(inRect);
        }
    }
}
