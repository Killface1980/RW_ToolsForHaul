using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFH_VehicleBase.Components
{
    using RimWorld;

    using UnityEngine;

    using Verse;

    class CompRefuelableVehicle:CompRefuelable
    {
        // RimWorld.CompRefuelable
        public override void PostDraw()
        {
            base.PostDraw();
            if (!this.HasFuel && this.Props.drawOutOfFuelOverlay)
            {
                this.parent.Map.overlayDrawer.DrawOverlay(this.parent,  OverlayTypes.OutOfFuel);
            }

            if (this.Props.drawFuelGaugeInMap)
            {
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = this.parent.DrawPos + Vector3.up * 0.1f;
                r.size = FuelBarSize;
                r.fillPercent = this.FuelPercentOfMax;
                r.filledMat = FuelBarFilledMat;
                r.unfilledMat = FuelBarUnfilledMat;
                r.margin = 0.15f;
                Rot4 rotation = this.parent.Rotation;
                rotation.Rotate(RotationDirection.Clockwise);
                r.rotation = rotation;
                GenDraw.DrawFillableBar(r);
            }
        }

    }
}
