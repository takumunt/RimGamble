using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimGamble
{
    [StaticConstructorOnStartup]
    public class CompGachaRefuelable : CompRefuelable
    {
        public new CompProperties_GachaRefuelable Props => (CompProperties_GachaRefuelable)props;

        public override string CompInspectStringExtra()
        {
            if (Props.fuelIsMortarBarrel && Find.Storyteller.difficulty.classicMortars)
            {
                return string.Empty;
            }

            string text = Props.FuelLabel + ": " + Fuel.ToStringDecimalIfSmall() + " / " + Props.fuelCapacity.ToStringDecimalIfSmall();

            if (!HasFuel && !Props.outOfFuelMessage.NullOrEmpty())
            {
                string arg = ((parent.def.building != null && parent.def.building.IsTurret) ? ("CannotShoot".Translate() + ": " + Props.outOfFuelMessage).Resolve() : Props.outOfFuelMessage);
                text += $"\n{arg} ({GetFuelCountToFullyRefuel()}x {Props.fuelFilter.AnyAllowedDef.label})";
            }

            if (Props.targetFuelLevelConfigurable)
            {
                text += "\n" + "ConfiguredTargetFuelLevel".Translate(TargetFuelLevel.ToStringDecimalIfSmall());
            }

            return text;
        }

        public override void CompTick()
        {
            
        }
    }
}
