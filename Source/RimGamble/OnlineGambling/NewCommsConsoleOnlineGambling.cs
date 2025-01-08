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
     * Class for handling the usage of a comms console for the purposes of online gambling
     */
    public class NewCommsConsoleOnlineGambling : Building_CommsConsole
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            // call the original function and yield all the original options
            foreach (FloatMenuOption origOption in base.GetFloatMenuOptions(myPawn))
            {
                yield return origOption;
            }

            // custom gambling menu option
            yield return new FloatMenuOption("ConnectNetworkGamble".Translate(), () => OpenOnlineGamblingMenu(myPawn), MenuOptionPriority.Default);
        }

        private void OpenOnlineGamblingMenu(Pawn myPawn)
        {
            if (myPawn.IsColonistPlayerControlled)
            {
                Find.WindowStack.Add(new Window_OnlineGamblingMenu());
            }
        }

        /*
         * UI METHODS
         * 
         */
        private class Window_OnlineGamblingMenu : Window
        {
            public Window_OnlineGamblingMenu()
            {
                this.closeOnClickedOutside = true;
                this.absorbInputAroundWindow = true;
                this.forcePause = true;
                this.draggable = true;
                this.preventCameraMotion = true;
            }

            public override void DoWindowContents(Rect inRect)
            {
                
            }
        }
    }
}
