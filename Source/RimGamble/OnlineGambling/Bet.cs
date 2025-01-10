using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimGamble
{
    public class Bet : IExposable
    {
        public string siteName;
        public string betLabel;
        public int stake;
        public float odds;
        public int endTimeInTicks;

        // used for the GUI
        public string stakeBuffer;
        public int stakeBufferInt;

        public Bet(GambleSiteDef site)
        {
            this.siteName = site.siteLabel;
            this.betLabel = site.betLabel;
            this.stake = 0;
            this.odds = UnityEngine.Random.value + 0.01f; // add a little to make sure the odds are never exactly zero
            this.endTimeInTicks = Find.TickManager.TicksGame + (GenDate.TicksPerDay * Rand.Range(1, 3));
        }

        /*
         * Based on the odds and stake of the bet, figure out the payout
         */
        public int completeBet()
        {
            if (stake == 0) // if the player is not participating
            {
                return -1;
            }
            // if the bet is successful
            if (UnityEngine.Random.value < odds)
            {
                return (int)(stake / odds); // maybe change this later to be logarithmic
            }
            // otherwise get nothing
            return 0;
        }

        public void ExposeData()
        { 
            Scribe_Values.Look(ref siteName, "siteName");
            Scribe_Values.Look(ref betLabel, "betLabel");
            Scribe_Values.Look(ref stake, "stake");
            Scribe_Values.Look(ref odds, "odds");
            Scribe_Values.Look(ref endTimeInTicks, "endTimeInTicks");
        }

        public override string ToString()
        {
            return this.siteName + " " + this.betLabel + " " + this.stake + " " + this.odds + " " + this.endTimeInTicks;  
        }
    }
}
