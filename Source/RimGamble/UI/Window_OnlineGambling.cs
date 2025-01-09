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
    /*
     * Custom window class for the online gambling window
     */
    public class Window_OnlineGambling : Window
    {
        public Window_OnlineGambling()
        {
            this.forcePause = true;
            this.closeOnClickedOutside = true;
        }
        public override void DoWindowContents(Rect inRect)
        {

        }
    }
}
