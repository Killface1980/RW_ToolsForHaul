// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompMountable.cs" company="">
// </copyright>
// <summary>
//   Defines the CompMountable type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace TFH_VehicleBase.Components
{
    using JetBrains.Annotations;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class CompMountable : ThingComp
    {
        #region Public Fields

        public bool IsMounted => isMounted;

        private bool isMounted = false;
        public bool IsPrisonBreaking;

        #endregion Public Fields

        #region Private Fields

        private float lastDrawAsAngle;

        private Building_Door lastPassedDoor;
        private IntVec3 lastPos = IntVec3.Invalid;

        public Vehicle_Cart ParentCart
        {
            get
            {
                if (this.parentCart == null)
                {
                    this.parentCart = parent as Vehicle_Cart;
                }

                return this.parentCart;
            }
        }

        private Vehicle_Cart parentCart;
        private Vector3 position;
        private Pawn rider;
        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;

        private int tickLastDoorCheck = Find.TickManager.TicksGame;

        #endregion Private Fields

        #region Public Properties

        public Vector3 drawPosition
        {
            get
            {
                this.position = this.Rider.DrawPos - this.InteractionOffset * 1.3f;

                if (!this.ParentCart.Rotation.IsHorizontal)
                {
                    if (this.ParentCart.Rotation == Rot4.North)
                    {
                        this.position += this.Props.drawOffsetRotN;
                    }
                    else
                    {
                        this.position += this.Props.drawOffsetRotS;
                    }
                }



                // Out of bound or Preventing cart from stucking door
                if (!this.position.InBounds(this.parent.Map))
                {
                    return this.Rider.DrawPos;
                }

                if (!this.position.ToIntVec3().Walkable(this.ParentCart.Map))
                {
                    return this.Rider.DrawPos;
                }

                return this.position;
            }
        }

        public CompProperties_Mountable Props => (CompProperties_Mountable)this.props;

        [CanBeNull]
        public Pawn Rider => this.rider;

        #endregion Public Properties

        #region Private Properties

        private Vector3 InteractionOffset => this.parent.def.interactionCellOffset.ToVector3()
            .RotatedBy(this.lastDrawAsAngle);

        #endregion Private Properties

        #region Public Methods

        public override void CompTick()
        {
            base.CompTick();

            if (this.ParentCart == null || !this.ParentCart.Spawned || !this.IsMounted)
            {
                return;
            }

            if (this.rider.Dead || this.rider.Downed || this.rider.health.InPainShock
                || (this.ParentCart.RaceProps.IsMechanoid && this.ParentCart.IsForbidden(Faction.OfPlayer)
                    && this.rider.Faction == Faction.OfPlayer))
            {
                if (!this.rider.Position.InBounds(this.ParentCart.Map))
                {
                    Log.Message("1");
                    this.DismountAt(this.Rider.Position);
                    return;
                }

                Log.Message("2");
                this.DismountAt(
                    this.rider.Position - this.parent.def.interactionCellOffset.RotatedBy(this.rider.Rotation));
                return;
            }

            if (!this.rider.Spawned)
            {
                this.ParentCart.DeSpawn();
                return;
            }

            if (Find.TickManager.TicksGame - this.tickCheck >= this.tickCooldown)
            {
                if (this.rider.Faction == Faction.OfPlayer && this.rider.CurJob != null)
                {
                    Job curJob = this.rider.CurJob;
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
                            || curJob.def == JobDefOf.Mate || this.Rider.health.HasHediffsNeedingTend())
                        && this.Rider.Position.Roofed(this.Rider.Map))
                    {
                        this.ParentCart.Position = this.drawPosition.ToIntVec3();
                        this.ParentCart.Rotation = this.rider.Rotation;
                        if (!this.rider.Position.InBounds(this.ParentCart.Map))
                        {
                            Log.Message("3");
                            this.DismountAt(this.Rider.Position);
                            return;
                        }

                        Log.Message("4");
                        this.DismountAt(this.rider.Position - this.InteractionOffset.ToIntVec3());

                        // this.rider.Position = this.rider.Position.RandomAdjacentCell8Way();
                        return;
                    }
                }

                this.tickCheck = Find.TickManager.TicksGame;
                this.tickCooldown = Rand.RangeInclusive(60, 180);
            }

            if (Find.TickManager.TicksGame - this.tickLastDoorCheck >= 96
                && (this.rider.Position.GetDoor(this.ParentCart.Map) != null
                    || this.ParentCart.Position.GetDoor(this.ParentCart.Map) != null))
            {
                this.lastPassedDoor = this.Rider.Position.GetDoor(this.Rider.Map) != null
                                          ? this.Rider.Position.GetDoor(this.Rider.Map)
                                          : this.ParentCart.Position.GetDoor(this.ParentCart.Map);
                this.lastPassedDoor?.StartManualOpenBy(this.Rider);
                this.tickLastDoorCheck = Find.TickManager.TicksGame;
            }
            else if (Find.TickManager.TicksGame - this.tickLastDoorCheck >= 96)
            {
                if (this.lastPassedDoor != null)
                {
                    this.lastPassedDoor.StartManualCloseBy(this.Rider);
                    this.lastPassedDoor = null;
                }
            }

            // Keep the heading of the vehicle
            if (this.ParentCart.IsMoving && !this.drawPosition.ToIntVec3().CloseToEdge(this.ParentCart.Map, 2))
            {
                // Not there yet
                if (this.Rider.Position != this.Rider.pather.Destination.Cell)
                {
                    {
                        // if (this.Rider.Position.DistanceTo(this.lastPos) > 6f)
                        // Check rotation
                        Rot4 riderRotation = this.Rider.Rotation;
                        if (this.ParentCart.Rotation != riderRotation)
                        {
                            {
                                // if (riderRotation.Opposite != this.ParentCart.Rotation)
                                this.ParentCart.Rotation = riderRotation;

                                // Rot4.FromAngleFlat(
                                // (this.Rider.Position - this.Rider.pather.Destination.Cell).AngleFlat).Opposite;
                                this.lastDrawAsAngle = this.ParentCart.Rotation.AsAngle;
                            }
                            {
                                // if (this.ParentCart.Position.DistanceTo(this.Rider.pather.Destination.Cell) < 12f)
                                // this.ParentCart.Rotation = Rot4.FromIntVec3(this.Rider.Position - this.Rider.pather.nextCell);
                            }
                        }

                        this.lastPos = this.drawPosition.ToIntVec3();
                    }
                }

                this.ParentCart.Position = this.drawPosition.ToIntVec3();
            }
        }

        public void Dismount()
        {
            TFH_BaseUtility.DriverDict.Remove(this.Rider);

            this.RemoveDriverComp();

            this.Rider.RaceProps.makesFootprints = this.Rider.def.race.makesFootprints;

            // if (Find.Reservations.IsReserved(parent, Driver.Faction))
            this.parent.Map.reservationManager.ReleaseAllForTarget(this.parent);

            if (this.Rider.Faction != Faction.OfPlayer && this.ParentCart.RaceProps.IsMechanoid)
            {
                // this.parent.SetForbidden(true);
                if (this.Rider.Dead)
                {
                    this.parent.SetFaction(null);
                }
            }

            this.Rider.Position = this.ParentCart.InteractionCell;

            this.rider = null;
            this.isMounted = false;

            this.ParentCart.EndSustainerVehicleIfActive();

            // Find.ListerBuildings.Add(parent as Building);
        }

        public void DismountAt(IntVec3 dismountPos)
        {
            // if (Driver.Position.IsAdjacentTo8WayOrInside(dismountPos, Driver.Rotation, new IntVec2(1,1)))
            if (dismountPos != IntVec3.Invalid)
            {
                this.Dismount();
                this.ParentCart.Position = dismountPos;
                return;
            }

            Log.Warning("Tried dismount at " + dismountPos);
        }

        public void MountOn(Pawn pawn)
        {

            if (this.rider != null)
            {
                return;
            }

            if (this.ParentCart == null)
            {
                return;
            }

            TFH_BaseUtility.DriverDict.Add(pawn, this.ParentCart);

            // Check to make pawns not mount two vehicles at once
            if (pawn.IsDriver(out Vehicle_Cart drivenCart))
            {
                drivenCart.MountableComp.Dismount();
            }

            this.rider = pawn;
            this.isMounted = true;

            if (pawn.RaceProps.Humanlike && this.ParentCart.IsCurrentlyMotorized())
            {
                pawn.RaceProps.makesFootprints = false;
            }

            this.ParentCart.AddDriverComp();

            if (this.ParentCart.RaceProps.IsMechanoid)
            {
                // Set faction of vehicle to whoever mounts it
                if (this.ParentCart.Faction != this.Rider.Faction)
                {
                    this.ParentCart.SetFaction(this.Rider.Faction);
                }
            }

            this.ParentCart.StartSustainerVehicleIfInactive();

            this.IsPrisonBreaking = PrisonBreakUtility.IsPrisonBreaking(pawn);
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            this.RemoveDriverComp();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref this.rider, "Driver");
            Scribe_References.Look(ref this.lastPassedDoor, "lastPassedDoor");
            Scribe_Values.Look(ref this.lastDrawAsAngle, "lastDrawAsAngle");
            Scribe_Values.Look(ref this.isMounted, "IsMounted");
            Scribe_Values.Look(ref this.IsPrisonBreaking, "IsPrisonBreaking");
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (this.IsMounted)
            {
                if (this.ParentCart.CanExplode() && this.ParentCart.wickStarted)
                {
                    if (Rand.Value >= 0.1f)
                    {
                        this.DismountAt(
                            (this.rider.Position - this.InteractionOffset.ToIntVec3()).RandomAdjacentCell8Way());
                    }
                    else
                    {
                        if (this.ParentCart.Spawned)
                        {
                            // this.driver.TryAttachFire(0.1f);
                            this.ParentCart.TryAttachFire(0.1f);
                        }
                    }
                }

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

            if (this.rider != null)
            {
                this.isMounted = true;

                // if (this.Driver.RaceProps.Humanlike)
                if (this.rider.RaceProps.Humanlike)
                {
                    this.rider.RaceProps.makesFootprints = false;
                }
            }



            // cart.DriverComp = new CompDriver { Vehicle = this.parent };
            // Driver.AllComps?.Add(cart.DriverComp);
            // cart.DriverComp.parent = this.Driver;
        }
        
        #endregion Public Methods

        #region Private Methods

        private void RemoveDriverComp()
        {
            if (this.rider == null)
            {
                return;

            }

            if (this.rider.AllComps.Contains(this.ParentCart.DriverComp))
            {
                this.rider.AllComps?.Remove(this.ParentCart.DriverComp);
                this.ParentCart.DriverComp.Vehicle = null;
                this.ParentCart.DriverComp.Pawn = null;
                this.ParentCart.DriverComp.parent = null;
            }
        }

        #endregion Private Methods
    }
}