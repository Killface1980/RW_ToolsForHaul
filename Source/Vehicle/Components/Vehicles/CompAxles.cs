using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ToolsForHaul.Components
{
    public class CompAxles : ThingComp
    {
        public CompProperties_Axle Props
        {
            get
            {
                return (CompProperties_Axle)props;
            }
        }

        public float wheelRotation;
        public float wheel_shake;
        public double tick_time = 0;
        private bool breakSoundPlayed;

        //Graphic data
        public Graphic_Single graphic_Wheel_Single;


        public bool HasAxles()
        {
            return Props.axles.Count > 0;
        }

        public bool GetAxleLocations(Vector2 drawSize, int flip, out List<Vector3> axleVecs)
        {
            axleVecs = new List<Vector3>();
            if (Props.axles.Count <= 0)
            {
                return false;
            }
            foreach (Vector2 current in Props.axles)
            {
                Vector3 item = new Vector3(current.x / 192f * drawSize.x * flip, 0f, current.y / 192f * drawSize.y);
                axleVecs.Add(item);
            }
            return true;
        }

        public override void CompTick()
        {
            var mountableComp = parent.TryGetComp<CompMountable>();
            var vehicleComp = parent.TryGetComp<CompVehicle>();

            if (mountableComp.IsMounted)
            {
                if (mountableComp.Driver.pather.Moving) // || mountableComp.Driver.drafter.pawn.pather.Moving)
                {
                    if (!mountableComp.Driver.stances.FullBodyBusy && HasAxles())
                    {
                        wheelRotation += vehicleComp.currentDriverSpeed / 3f;
                        tick_time += 0.01f * vehicleComp.currentDriverSpeed / 5f;
                        wheel_shake = (float)((Math.Sin(tick_time) + Math.Abs(Math.Sin(tick_time))) / 40.0);
                    }

                    if (mountableComp.Driver.Position.AdjacentTo8WayOrInside(mountableComp.Driver.pather.Destination.Cell))
                    {
                        // Make the breaks sound once and throw some dust if Driver comes to his destination
                        if (parent.TryGetComp<CompAxles>().HasAxles())
                            if (!breakSoundPlayed)
                            {
                                SoundDef.Named("VehicleATV_Ambience_Break").PlayOneShot(mountableComp.Driver.Position);
                                MoteMaker.ThrowDustPuff(parent.DrawPos, 0.8f);
                                breakSoundPlayed = true;
                            }
                    }
                    else
                    {
                        breakSoundPlayed = false;
                    }

                }
            }
            base.CompTick();
        }

        public override void PostSpawnSetup()
        {
            LongEventHandler.ExecuteWhenFinished(UpdateGraphics);

            base.PostSpawnSetup();
        }

        private void UpdateGraphics()
        {
            if (HasAxles())
            {
                string text = "Things/Pawn/" + parent.def.defName + "/Wheel";
                graphic_Wheel_Single = new Graphic_Single();
                graphic_Wheel_Single =
                    GraphicDatabase.Get<Graphic_Single>(text, parent.def.graphic.Shader, parent.def.graphic.drawSize,
                        parent.def.graphic.color, parent.def.graphic.colorTwo) as Graphic_Single;
            }
        }

    }
}
