namespace TFH_VehicleBase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using RimWorld;

    using TFH_VehicleBase.Components;
    using TFH_VehicleBase.DefOfs_TFH;
    using TFH_VehicleBase.Designators;

    using UnityEngine;

    using Verse;
    using Verse.AI;
    using Verse.Sound;

    public class Vehicle_Animal : BasicVehicle
    {
        #region Variables

        public override Color DrawColor
        {
            get
            {
                CompColorable comp = this.GetComp<CompColorable>();
                if (comp != null && comp.Active)
                {
                    return comp.Color;
                }

                return base.DrawColor;
            }

            set
            {
                this.SetColor(value, true);
            }
        }

        // ==================================

        public int MaxItemPerBodySize => (int)this.GetStatValue(HaulStatDefOf.VehicleMaxItem);

        public bool instantiated;


        public float DesiredSpeed => this.GetStatValue(StatDefOf.MoveSpeed);



        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;

        // mount and storage data



        public IntVec3 GetPosition()
        {
            return this.Position;
        }

        // slotGroupParent Interface

        public CompVehicle VehicleComp;


        public Vector3 DriverOffset = new Vector3();

        public CompDriver DriverComp { get; set; }

        #endregion

        #region Setup Work

        // public static ListerVehicles listerVehicles = new ListerVehicles();
#if Headlights
        HeadLights flooder;
#endif



        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }

        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            // do nothing if not of colony
            if (this.Faction != Faction.OfPlayer)
            {
                FloatMenuOption failer = new FloatMenuOption(
                    "NotPlayerFaction".Translate(this.LabelCap),
                    null,
                    MenuOptionPriority.Default,
                    null,
                    null,
                    0f,
                    null,
                    null);
                yield return failer;
                yield break;
            }

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
            {
                yield return fmo;
            }

            Map map = myPawn.Map;

            Action action_MakeMount = () =>
                {
                    Pawn worker = null;
                    Job jobNew = new Job(VehicleJobDefOf.MakeMount);
                    map.reservationManager.ReleaseAllForTarget(this);
                    jobNew.count = 1;
                    jobNew.targetA = this;
                    jobNew.targetB = myPawn;
                    foreach (Pawn colonyPawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
                    {
                        if (colonyPawn.CurJob.def != jobNew.def
                            && (worker == null || (worker.Position - myPawn.Position).LengthHorizontal
                                > (colonyPawn.Position - myPawn.Position).LengthHorizontal))
                        {
                            worker = colonyPawn;
                        }
                    }

                    if (worker == null)
                    {
                        Messages.Message("NoWorkForMakeMount".Translate(), MessageSound.RejectInput);
                    }
                    else
                    {
                        worker.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    }
                };



            Action action_Mount = () =>
                {
                    Job jobNew = new Job(VehicleJobDefOf.Mount);
                    myPawn.Map.reservationManager.ReleaseAllForTarget(this);
                    myPawn.Map.reservationManager.Reserve(myPawn, this);
                    jobNew.targetA = this;
                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_DismountInBase = () =>
                {
                    Job jobNew = myPawn.DismountAtParkingLot("VC GFMO");

                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            if (!this.MountableComp.IsMounted)
            {
                if (!this.IsForbidden(Faction.OfPlayer))
                {
                    if (myPawn.RaceProps.Humanlike && !myPawn.IsDriver(out BasicVehicle drivenCart, this))
                    {
                        yield return new FloatMenuOption("MountOn".Translate(this.LabelShort), action_Mount);
                    }

                    bool flag = this.Map.HasFreeCellsInParkingLot();

                    if (flag)
                    {
                        yield return new FloatMenuOption(
                            "DismountAtParkingLot".Translate(this.LabelShort),
                            action_DismountInBase);
                    }
                    else
                    {
                        FloatMenuOption failer = new FloatMenuOption(
                            "NoFreeParkingSpace".Translate(this.LabelShort),
                            null,
                            MenuOptionPriority.Default,
                            null,
                            null,
                            0f,
                            null,
                            null);
                        yield return failer;
                    }
                }

            }
        }



        #endregion

        #region Ticker



        public override void Tick()
        {
            base.Tick();

            if (!this.Spawned || this.Dead)
            {
                return;

            }

            if (!this.instantiated)
            {
                this.VehicleComp.currentDriverSpeed = this.VehicleComp.VehicleSpeed;
                this.instantiated = true;
            }

        }

        #endregion

        #region Graphics / Inspections

        public override Vector3 DrawPos
        {
            get
            {
                if (!this.Spawned || !this.instantiated)
                {
                    return base.DrawPos;
                }

                if (!this.MountableComp.IsMounted)
                {
                    return base.DrawPos;
                }

                float num = this.MountableComp.Driver.Drawer.renderer.graphics.nakedGraphic.drawSize.x - 1f;
                num *= this.MountableComp.Driver.Rotation.AsInt % 2 == 1 ? 0.5f : 0.25f;
                Vector3 vector = new Vector3(0f, 0f, -num);

                // vector += DriverOffset;
                return this.MountableComp.Position + vector.RotatedBy(this.MountableComp.Driver.Rotation.AsAngle);
            }
        }


        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc);

            if (!this.Spawned)
            {
                return;
            }



            // // Lights
            // spotlightMatrix.SetTRS(drawLoc + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightScale);
            // if (this.MountableComp.IsMounted)
            // {
            // Graphics.DrawMesh(MeshPool.plane10, spotlightMatrix, spotlightOnTexture, 0);
            // spotlightLightEffectMatrix.SetTRS(drawLoc + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightLightEffectScale);
            // Graphics.DrawMesh(MeshPool.plane10, spotlightLightEffectMatrix, spotlightLightEffectTexture, 0);
            // }
            // else
            // {
            // Graphics.DrawMesh(MeshPool.plane10, spotlightMatrix, spotlightOffTexture, 0);
            // }
            // if (Find.Selector.IsSelected(this)
            // && (this.CurrentTarget != null))
            // {
            // Vector3 lineOrigin = this.TrueCenter();
            // Vector3 lineTarget = this.CurrentTarget.Thing.Position.ToVector3Shifted();
            // lineTarget.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            // lineOrigin.y = lineTarget.y;
            // GenDraw.DrawLineBetween(lineOrigin, lineTarget, targetLineTexture);
            // }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());

            string currentDriverString;
            if (this.MountableComp.IsMounted)
            {
                currentDriverString = "Rider".Translate() + ": " + this.MountableComp.Driver.LabelCap;
            }
            else
            {
                currentDriverString = "NoRider".Translate();
            }

            stringBuilder.AppendLine(currentDriverString);

            // string text = storage.ContentsString;
            // stringBuilder.AppendLine(string.Concat(new object[]
            // {
            // "InStorage".Translate(),
            // ": ",
            // text
            // }));
            return stringBuilder.ToString();
        }

        #endregion

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }


        public bool InParkingLot => this.Position.GetZone(this.Map) is Zone_ParkingLot;
    }
}