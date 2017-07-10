namespace TFH_VehicleBase
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using RimWorld;

    using TFH_VehicleBase.DefOfs_TFH;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class Vehicle_Animal : BasicVehicle
    {


        #region Variables

        // ==================================

        public bool instantiated;


        public float DesiredSpeed => this.GetStatValue(StatDefOf.MoveSpeed);

        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;



        // slotGroupParent Interface



        public Vector3 DriverOffset = new Vector3();


        #endregion

        #region Setup Work

        // public static ListerVehicles listerVehicles = new ListerVehicles();
#if Headlights
        HeadLights flooder;
#endif



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

            Action action_Mount = () =>
                {
                    Job jobNew = new Job(VehicleJobDefOf.Mount);
                    myPawn.Map.reservationManager.ReleaseAllForTarget(this);
                    myPawn.Map.reservationManager.Reserve(myPawn, this);
                    jobNew.targetA = this;
                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);

                    // Job job = new Job(VehicleJobDefOf.StandBy, myPawn.Position);
                    // if (this.RaceProps.Animal) { this.jobs.StartJob(job, JobCondition.InterruptForced);}
                };

            Action action_DismountInBase = () =>
                {
                    Job jobNew = myPawn.DismountAtParkingLot("VC GFMO");

                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            if (!this.MountableComp.IsMounted)
            {
                {
                    if (myPawn.RaceProps.Humanlike && !myPawn.IsDriver())
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

                float num = this.MountableComp.Rider.Drawer.renderer.graphics.nakedGraphic.drawSize.x - 1f;
                num *= this.MountableComp.Rider.Rotation.AsInt % 2 == 1 ? 0.5f : 0.25f;
                Vector3 vector = new Vector3(0f, 0f, -num);

                // vector += DriverOffset;
                this.Position = this.MountableComp.drawPosition.ToIntVec3();
                return this.MountableComp.drawPosition + vector.RotatedBy(this.MountableComp.Rider.Rotation.AsAngle);
            }
        }


        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc);

            if (!this.Spawned)
            {
                return;
            }

        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());

            string currentDriverString;
            if (this.MountableComp.IsMounted)
            {
                currentDriverString = "Rider".Translate() + ": " + this.MountableComp.Rider.LabelCap;
            }
            else
            {
                currentDriverString = "NoRider".Translate();
            }

            stringBuilder.Append(currentDriverString);

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


        public bool InParkingLot => this.Position.GetZone(this.Map) is Zone_ParkingLot;
    }
}