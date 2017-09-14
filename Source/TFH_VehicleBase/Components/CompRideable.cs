// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompMountable.cs" company="">
// </copyright>
// <summary>
//   Defines the CompMountable type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace TFH_VehicleBase.Components
{
    using RimWorld;

    using TFH_VehicleBase.DefOfs_TFH;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class CompRideable : ThingComp
    {
        public bool IsMounted = false;

        public bool IsPrisonBreaking;

        private float lastDrawAsAngle;

        private Vehicle_Animal rideableAnimal;

        private Pawn driver;

        private Building_Door lastPassedDoor;

        private Vector3 position;

        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;

        private int tickLastDoorCheck = Find.TickManager.TicksGame;

        public Pawn Driver => this.driver;

        private Vector3 InteractionOffset => this.parent.def.interactionCellOffset.ToVector3()
            .RotatedBy(this.lastDrawAsAngle);

        public Vector3 Position
        {
            get
            {
                this.position = this.Driver.DrawPos - this.InteractionOffset * 1.3f;

                if (!this.rideableAnimal.Rotation.IsHorizontal)
                {
                    if (this.rideableAnimal.Rotation == Rot4.North)
                    {
                        this.position += this.Props.drawOffsetRotN;
                    }
                    else
                    {
                        this.position += this.Props.drawOffsetRotS;
                    }
                }

                // No Driver
                if (this.Driver == null)
                {
                    return this.rideableAnimal.DrawPos;
                }

                // Out of bound or Preventing cart from stucking door
                if (!this.position.InBounds(this.parent.Map))
                {
                    return this.Driver.DrawPos;
                }

                if (!this.position.ToIntVec3().Walkable(this.rideableAnimal.Map))
                {
                    return this.Driver.DrawPos;
                }

                return this.position;
            }
        }

        public CompProperties_Rideable Props => (CompProperties_Rideable)this.props;

        public override void CompTick()
        {
            base.CompTick();

            if (this.rideableAnimal == null || !this.rideableAnimal.Spawned || !this.IsMounted)
            {
                return;
            }

            if (this.driver.Dead || this.driver.Downed || this.driver.health.InPainShock
                || (this.parent.IsForbidden(Faction.OfPlayer) && this.driver.Faction == Faction.OfPlayer))
            {
                if (!this.driver.Position.InBounds(this.rideableAnimal.Map))
                {
                    Log.Message("1");
                    this.DismountAt(this.Driver.Position);
                    return;
                }

                Log.Message("2");
                this.DismountAt(
                    this.driver.Position - this.parent.def.interactionCellOffset.RotatedBy(this.driver.Rotation));
                return;
            }

            if (!this.driver.Spawned)
            {
                this.rideableAnimal.DeSpawn();
                return;
            }

            if (Find.TickManager.TicksGame - this.tickCheck >= this.tickCooldown)
            {
                if (this.driver.Faction == Faction.OfPlayer && this.driver.CurJob != null)
                {
                    Job curJob = this.driver.CurJob;
                    if (curJob.def.playerInterruptible
                        && (curJob.def == JobDefOf.GotoWander || curJob.def == JobDefOf.Open
                            || curJob.def == JobDefOf.ManTurret
                            || curJob.def == JobDefOf.EnterCryptosleepCasket
                            || curJob.def == JobDefOf.UseNeurotrainer
                            || curJob.def == JobDefOf.UseArtifact
                            || curJob.def == JobDefOf.DoBill || curJob.def == JobDefOf.Research

                            // || this.Driver.CurJob.def == JobDefOf.OperateDeepDrill
                            || curJob.def == JobDefOf.Repair
                            || curJob.def == JobDefOf.FixBrokenDownBuilding
                            || curJob.def == JobDefOf.UseCommsConsole
                            || curJob.def == JobDefOf.BuryCorpse
                            || curJob.def == JobDefOf.TradeWithPawn
                            || curJob.def == JobDefOf.Lovin
                            || curJob.def == JobDefOf.SocialFight
                            || curJob.def == JobDefOf.Maintain
                            || curJob.def == JobDefOf.MarryAdjacentPawn
                            || curJob.def == JobDefOf.SpectateCeremony
                            || curJob.def == JobDefOf.StandAndBeSociallyActive
                            || curJob.def == JobDefOf.LayDown || curJob.def == JobDefOf.Ingest
                            || curJob.def == JobDefOf.SocialRelax
                            || curJob.def == JobDefOf.Refuel

                            // || this.Driver.CurJob.def == JobDefOf.FillFermentingBarrel
                            // || this.Driver.CurJob.def == JobDefOf.TakeBeerOutOfFermentingBarrel
                            || curJob.def == JobDefOf.TakeWoundedPrisonerToBed
                            || curJob.def == JobDefOf.TakeToBedToOperate
                            || curJob.def == JobDefOf.EscortPrisonerToBed
                            || curJob.def == JobDefOf.CarryToCryptosleepCasket
                            || curJob.def == JobDefOf.ReleasePrisoner
                            || curJob.def == JobDefOf.PrisonerAttemptRecruit
                            || curJob.def == JobDefOf.PrisonerFriendlyChat
                            || curJob.def == JobDefOf.PrisonerExecution
                            || curJob.def == JobDefOf.FeedPatient
                            || curJob.def == JobDefOf.TendPatient
                            || curJob.def == JobDefOf.VisitSickPawn
                            || curJob.def == JobDefOf.Slaughter || curJob.def == JobDefOf.Milk
                            || curJob.def == JobDefOf.Shear || curJob.def == JobDefOf.Train
                            || curJob.def == JobDefOf.Mate || this.Driver.health.HasHediffsNeedingTend())
                        && this.Driver.Position.Roofed(this.Driver.Map))
                    {
                        this.rideableAnimal.Position = this.Position.ToIntVec3();
                        this.rideableAnimal.Rotation = this.driver.Rotation;
                        if (!this.driver.Position.InBounds(this.rideableAnimal.Map))
                        {
                            Log.Message("3");
                            this.DismountAt(this.Driver.Position);
                            return;
                        }

                        Log.Message("4");
                        this.DismountAt(this.driver.Position - this.InteractionOffset.ToIntVec3());
                        this.driver.Position = this.driver.Position.RandomAdjacentCell8Way();
                        return;
                    }
                }

                this.tickCheck = Find.TickManager.TicksGame;
                this.tickCooldown = Rand.RangeInclusive(60, 180);

                // // bring vehicles home
                // if (this.cart.VehicleComp != null && !this.cart.VehicleComp.MotorizedWithoutFuel())
                // {
                // float hitPointsPercent = this.cart.health.summaryHealth.SummaryHealthPercent;
                // if (this.cart.Faction == Faction.OfPlayer)
                // {
                // if (!GenAI.EnemyIsNear(this.Driver, 120f))
                // {
                // if (!this.Driver.drafter.Drafted)
                // {
                // var flag = this.cart.HasGasTank() && this.cart.GasTankComp.tankLeaking;
                // if (hitPointsPercent < 0.65f
                // // || (this.Driver.CurJob != null && this.Driver.jobs.curDriver.asleep)
                // || flag || !this.cart.RefuelableComp.HasFuel)
                // {
                // Job jobNew = this.Driver.DismountAtParkingLot("CM");
                // this.Driver.jobs.TryTakeOrderedJob(jobNew);
                // }
                // }
                // }
                // }
                // }
            }

            if (Find.TickManager.TicksGame - this.tickLastDoorCheck >= 96
                && (this.driver.Position.GetDoor(this.rideableAnimal.Map) != null
                    || this.rideableAnimal.Position.GetDoor(this.rideableAnimal.Map) != null))
            {
                this.lastPassedDoor = this.Driver.Position.GetDoor(this.Driver.Map) != null
                                          ? this.Driver.Position.GetDoor(this.Driver.Map)
                                          : this.rideableAnimal.Position.GetDoor(this.rideableAnimal.Map);
                this.lastPassedDoor?.StartManualOpenBy(this.Driver);
                this.tickLastDoorCheck = Find.TickManager.TicksGame;
            }
            else if (Find.TickManager.TicksGame - this.tickLastDoorCheck >= 96)
            {
                if (this.lastPassedDoor != null)
                {
                    this.lastPassedDoor.StartManualCloseBy(this.Driver);
                    this.lastPassedDoor = null;
                }
            }

            // Keep the heading of the vehicle
            if (this.Driver.pather.Moving && !this.Position.ToIntVec3().CloseToEdge(this.rideableAnimal.Map, 2))
            {
                // Not there yet
                if (this.Driver.Position != this.Driver.pather.Destination.Cell)
                {
                    // Check rotation
                    if (this.Driver.Rotation != this.rideableAnimal.Rotation)
                    {
                        if (this.Driver.pather.Destination.Cell.InHorDistOf(this.rideableAnimal.Position, 12f))
                        {
                            // Don't rotate if near tagert or short distance
                            if (this.Driver.Rotation.Opposite == this.rideableAnimal.Rotation)
                            {
                                this.lastDrawAsAngle = this.Driver.Rotation.Opposite.AsAngle;
                                this.rideableAnimal.Position = this.Position.ToIntVec3();
                                return;
                            }
                        }

                        this.rideableAnimal.Rotation = this.Driver.Rotation;
                        this.lastDrawAsAngle = this.Driver.Rotation.AsAngle;

                    }

                    this.rideableAnimal.Position = this.Position.ToIntVec3();
                }
            }
        }

        public void Dismount()
        {

            this.RemoveDriverComp();

            this.Driver.RaceProps.makesFootprints = this.Driver.def.race.makesFootprints;

            // if (Find.Reservations.IsReserved(parent, Driver.Faction))
            this.parent.Map.reservationManager.ReleaseAllForTarget(this.parent);

            if (this.Driver.Faction != Faction.OfPlayer)
            {
                // this.parent.SetForbidden(true);
                if (this.Driver.Dead)
                {
                    this.parent.SetFaction(Faction.OfInsects);
                }
            }

            this.Driver.Position = this.Driver.Position.RandomAdjacentCell8Way();

            this.driver = null;
            this.IsMounted = false;


            // Find.ListerBuildings.Add(parent as Building);
        }

        private void RemoveDriverComp()
        {
            if (this.driver == null)
            {
                return;

            }

            if (this.driver.AllComps.Contains(this.rideableAnimal.DriverComp))
            {
                this.driver.AllComps?.Remove(this.rideableAnimal.DriverComp);
                this.rideableAnimal.DriverComp.Vehicle = null;
                this.rideableAnimal.DriverComp.Pawn = null;
                this.rideableAnimal.DriverComp.parent = null;
            }
        }

        public void DismountAt(IntVec3 dismountPos)
        {
            // if (Driver.Position.IsAdjacentTo8WayOrInside(dismountPos, Driver.Rotation, new IntVec2(1,1)))
            if (dismountPos != IntVec3.Invalid)
            {
                this.Dismount();
                this.rideableAnimal.Position = dismountPos;
                return;
            }

            Log.Warning("Tried dismount at " + dismountPos);
        }

        public void MountOn(Pawn pawn)
        {
            if (this.driver != null)
            {
                return;
            }

            if (this.rideableAnimal == null)
            {
                return;
            }

            // Check to make pawns not mount two vehicles at once
            if (pawn.IsDriver(out Vehicle_Cart drivenCart))
            {
                drivenCart.MountableComp.Dismount();
            }

            this.driver = pawn;
            this.IsMounted = true;

            this.IsPrisonBreaking = PrisonBreakUtility.IsPrisonBreaking(pawn);

            Job job = new Job(VehicleJobDefOf.StandBy);

            this.rideableAnimal.jobs.StartJob(job, JobCondition.Incompletable);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref this.driver, "Driver");
            Scribe_References.Look(ref this.lastPassedDoor, "lastPassedDoor");
            Scribe_Values.Look(ref this.lastDrawAsAngle, "lastDrawAsAngle");
            Scribe_Values.Look(ref this.IsMounted, "IsMounted");
            Scribe_Values.Look(ref this.IsPrisonBreaking, "IsPrisonBreaking");
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (this.IsMounted)
            {

                // if (tankLeaking)
                // {
                // if (hitpointsPercent < 0.2f * Rand.Range(0.5f, 1f))
                // {
                // if (!mountableComp.Driver.Position.InBounds())
                // {
                // mountableComp.DismountAt(mountableComp.Driver.Position);
                // FireUtility.TryStartFireIn(Position, 0.1f);
                // return;
                // }
                // mountableComp.DismountAt(mountableComp.Driver.Position - mountableComp.InteractionOffset.ToIntVec3());
                // mountableComp.Driver.Position = mountableComp.Driver.Position.RandomAdjacentCell8Way();
                // FireUtility.TryStartFireIn(Position, 0.1f);
                // return;
                // }
                // }
            }
        }

        /// <summary>
        /// The post spawn setup.
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            this.rideableAnimal = this.parent as Vehicle_Animal;

            if (this.driver != null)
            {
                this.IsMounted = true;

                // if (this.Driver.RaceProps.Humanlike)
                if (this.driver.RaceProps.Humanlike)
                {
                    this.driver.RaceProps.makesFootprints = false;
                }
            }



            // cart.DriverComp = new CompDriver { Vehicle = this.parent };
            // Driver.AllComps?.Add(cart.DriverComp);
            // cart.DriverComp.parent = this.Driver;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            this.RemoveDriverComp();
        }
    }
}