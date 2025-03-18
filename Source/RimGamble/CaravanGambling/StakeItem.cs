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
    public class StakeItem
    {
        public Tradeable item;
        public int wagerCt;
        public float marketVal;
        public StakeItem(Tradeable item, int wagerCt, float marketVal) {
            this.item = item;
            this.wagerCt = wagerCt;
            this.marketVal = marketVal;
        }
    }
}
