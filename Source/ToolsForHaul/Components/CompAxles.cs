namespace ToolsForHaul.Components
{
    using System;
    using System.Collections.Generic;

    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;
    using Verse.Sound;

    public class CompAxles : ThingComp
    {
        // Graphic data
        public Graphic_Single graphic_Wheel_Single;

        public double tick_time;

        public float wheel_shake;

        public float wheelRotation;

        private Vector3 bodyLoc;

        private bool breakSoundPlayed;

        // Body and part location
        private Vector3 wheelLoc;

        private Vehicle_Cart cart;

        public CompProperties_Axle Props => (CompProperties_Axle)this.props;

        public override void CompTick()
        {
            base.CompTick();

            if (cart == null || cart.MountableComp.Driver == null)
            {
                return;
            }

            if (cart.MountableComp.Driver.pather.Moving)
            {
                // || mountableComp.Driver.drafter.pawn.pather.Moving)
                if (!cart.MountableComp.Driver.stances.FullBodyBusy && this.HasAxles())
                {
                    this.wheelRotation += cart.VehicleComp.currentDriverSpeed / 3f;
                    this.tick_time += 0.01f * cart.VehicleComp.currentDriverSpeed / 5f;
                    this.wheel_shake = (float)((Math.Sin(this.tick_time) + Math.Abs(Math.Sin(this.tick_time))) / 40.0);
                }

                if (cart.MountableComp.Driver.Position.AdjacentTo8WayOrInside(cart.MountableComp.Driver.pather.Destination.Cell))
                {
                    // Make the breaks sound once and throw some dust if Driver comes to his destination
                    if (this.HasAxles())
                    {
                        if (!this.breakSoundPlayed)
                        {
                            SoundDef.Named("VehicleATV_Ambience_Break")
                                .PlayOneShot(new TargetInfo(cart.MountableComp.Driver.Position, this.parent.Map));
                            MoteMakerTFH.ThrowDustPuff(this.parent.DrawPos, this.parent.Map, 0.8f);
                            this.breakSoundPlayed = true;
                        }
                    }
                }
                else
                {
                    this.breakSoundPlayed = false;
                }
            }
        }

        private bool GetAxleLocations(Vector2 drawSize, int flip, out List<Vector3> axleVecs)
        {
            axleVecs = new List<Vector3>();
            if (this.Props.axles.Count <= 0)
            {
                return false;
            }

            foreach (Vector2 current in this.Props.axles)
            {
                Vector3 item = new Vector3(current.x / 192f * drawSize.x * flip, 0f, current.y / 192f * drawSize.y);
                axleVecs.Add(item);
            }

            return true;
        }

        public bool HasAxles()
        {
            return this.Props.axles.Count > 0;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            this.wheelLoc = this.parent.DrawPos;
            this.bodyLoc = this.parent.DrawPos;

            // Vertical
            if (this.parent.Rotation.AsInt % 2 == 0)
            {
                this.wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Item) + 0.02f;
            }

            // horizontal
            if (this.HasAxles() && this.parent.Rotation.AsInt % 2 == 1)
            {
                this.wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.04f;
                this.bodyLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.03f;

                Vector2 drawSize = this.parent.def.graphic.drawSize;
                int num = this.parent.Rotation == Rot4.West ? -1 : 1;
                Vector3 vector3 = new Vector3(1f * drawSize.x, 1f, 1f * drawSize.y);
                Quaternion asQuat = this.parent.Rotation.AsQuat;
                float x = 1f * Mathf.Sin(num * (this.wheelRotation * 0.05f) % (2 * Mathf.PI));
                float z = 1f * Mathf.Cos(num * (this.wheelRotation * 0.05f) % (2 * Mathf.PI));

                asQuat.SetLookRotation(new Vector3(x, 0f, z), Vector3.up);

                this.wheel_shake = (float)((Math.Sin(this.tick_time) + Math.Abs(Math.Sin(this.tick_time))) / 40.0);

                this.wheelLoc.z = this.wheelLoc.z + this.wheel_shake;

                List<Vector3> list;
                if (this.GetAxleLocations(drawSize, num, out list))
                {
                    foreach (Vector3 current in list)
                    {
                        Matrix4x4 matrix = default(Matrix4x4);
                        matrix.SetTRS(this.wheelLoc + current, asQuat, vector3);
                        Graphics.DrawMesh(
                            MeshPool.plane10,
                            matrix,
                            graphic_Wheel_Single.MatAt(this.parent.Rotation),
                            0);
                    }
                }
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            if (this.HasAxles())
            {
                string text = "Things/Vehicles/" + this.parent.def.defName + "/Wheel";
                LongEventHandler.ExecuteWhenFinished(
                    delegate
                        {
                            graphic_Wheel_Single =
                                GraphicDatabase.Get<Graphic_Single>(
                                    text,
                                    this.parent.def.graphic.Shader,
                                    this.parent.def.graphic.drawSize,
                                    this.parent.def.graphic.color,
                                    this.parent.def.graphic.colorTwo) as Graphic_Single;
                        });

            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            cart = this.parent as Vehicle_Cart;
        }
    }
}