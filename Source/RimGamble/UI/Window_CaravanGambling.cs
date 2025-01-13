using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGamble
{
    public class Window_CaravanGambling : Window
    {
        public Window_CaravanGambling()
        {
            this.forcePause = true;
            this.closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(UI.screenWidth * 0.6f, UI.screenHeight * 0.6f);

        public override void DoWindowContents(Rect inRect)
        {

        }
    }
}
