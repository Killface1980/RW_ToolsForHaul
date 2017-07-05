using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolsForHaul.Components
{
    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;

   public class CompFrontTex : ThingComp
    {
        private Vehicle_Cart cart;

        public Graphic graphic_VehicleFront;

        private Vector2 drawSize;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            this.cart = this.parent as Vehicle_Cart;



     // if (this.Props != null)
     // {
     // this.cart.DriverOffset = this.Props.driverOffset;
     // }
            string text = "Things/Vehicles/" + this.cart.def.defName + "/Front";
            {
                LongEventHandler.ExecuteWhenFinished(
                    delegate
                        {
                            PawnKindLifeStage curKindLifeStage = ((Pawn)this.parent).ageTracker.CurKindLifeStage;
                            var graphic = curKindLifeStage.bodyGraphicData.Graphic;

                            this.drawSize = graphic.drawSize;

                            this.graphic_VehicleFront =
                                GraphicDatabase.Get<Graphic_Multi>(
                                    text,
                                    graphic.Shader,
                                    graphic.drawSize,
                                    graphic.color,
                                    graphic.colorTwo);
                        });
            }
        }

        public override void PostDraw()
        {

            base.PostDraw();

            Vector3 vector3 = new Vector3(1f * drawSize.x, 1f, 1f * drawSize.y);
            var pos = this.cart.DrawPos;
            pos.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.05f;

            // Rot4 rotation = this.Rotation;
            // rotation.Rotate(RotationDirection.Clockwise);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos, new Quaternion(), vector3);
            bool flip = this.cart.Rotation == Rot4.West;

            Graphics.DrawMesh(flip ? MeshPool.plane10Flip :
                                  MeshPool.plane10,
                matrix,
                this.graphic_VehicleFront.MatAt(this.cart.Rotation),
                0);
        }

    }
}
