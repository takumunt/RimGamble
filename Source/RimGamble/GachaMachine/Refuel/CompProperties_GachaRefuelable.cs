using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using RimGamble;

namespace RimGamble
{
    public class CompProperties_GachaRefuelable : CompProperties_Refuelable
    {

        public CompProperties_GachaRefuelable()
        {
            compClass = typeof(CompGachaRefuelable);
        }
    }
}
