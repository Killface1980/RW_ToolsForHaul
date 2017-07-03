// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompMountable.cs" company="">
// </copyright>
// <summary>
//   Defines the CompMountable type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ToolsForHaul.Components
{
    using System;
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Defs;
    using ToolsForHaul.Designators;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;
    using Verse.AI;
    using Verse.Sound;

    using Debug = System.Diagnostics.Debug;

    public class CompMountable : ThingComp
    {

        public float lastDrawAsAngle;

        private Building_Door lastPassedDoor;

        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;

        private int tickLastDoorCheck = Find.TickManager.TicksGame;

        public bool IsPrisonBreaking;

        public Vector3 InteractionOffset => this.parent.def.interactionCellOffset.ToVector3().RotatedBy(this.lastDrawAsAngle);


        public bool IsMounted = false;

        public Vector3 Position
        {
            get
            {
                Vector3 position;

                position = this.Driver.DrawPos - this.InteractionOffset * 1.3f;

                if (!this.cart.Rotation.IsHorizontal)
                {
                    if (this.cart.Rotation == Rot4.North)
                    {
                        position += this.Props.drawOffsetRotN;
                    }
                    else
                    {
                        position += this.Props.drawOffsetRotS;
                    }
                }

                // No Driver
                if (this.Driver == null)
                {
                    return this.cart.DrawPos;
                }

                // Out of bound or Preventing cart from stucking door
                if (!position.InBounds(this.parent.Map))
                {
                    return this.Driver.DrawPos;
                }

                if (!position.ToIntVec3().Walkable(this.parent.Map))
                {
                    return this.Driver.DrawPos;
                }

                return position;
            }
        }

        public Pawn Driver;

        private Vehicle_Cart cart;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo compCom in base.CompGetGizmosExtra())
            {
                yield return compCom;
            }

            if (this.parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }


        }

        public override void CompTick()
        {
            base.CompTick();

            if (!this.IsMounted)
            {
                return;
            }

            if (this.Driver.Dead || this.Driver.Downed || this.Driver.health.InPainShock
                || (this.parent.IsForbidden(Faction.OfPlayer) && this.Driver.Faction == Faction.OfPlayer))
            {
                if (!this.Driver.Position.InBounds(this.parent.Map))
                {
                    Log.Message("1");
                    this.DismountAt(this.Driver.Position);
                    return;
                }

                Log.Message("2");
                this.DismountAt(
                    this.Driver.Position - this.parent.def.interactionCellOffset.RotatedBy(this.Driver.Rotation));
                return;
            }

            if (!this.Driver.Spawned)
            {
                this.parent.DeSpawn();
                return;
            }
            if (Find.TickManager.TicksGame - this.tickCheck >= this.tickCooldown)
            {
                if (this.Driver.Faction == Faction.OfPlayer && this.Driver.CurJob != null)
                {
                    if (this.Driver.CurJob.def.playerInterruptible
                        && (this.Driver.CurJob.def == JobDefOf.GotoWander || this.Driver.CurJob.def == JobDefOf.Open
                            || this.Driver.CurJob.def == JobDefOf.ManTurret
                            || this.Driver.CurJob.def == JobDefOf.EnterCryptosleepCasket
                            || this.Driver.CurJob.def == JobDefOf.UseNeurotrainer
                            || this.Driver.CurJob.def == JobDefOf.UseArtifact
                            || this.Driver.CurJob.def == JobDefOf.DoBill || this.Driver.CurJob.def == JobDefOf.Research

                            // || this.Driver.CurJob.def == JobDefOf.OperateDeepDrill
                            || this.Driver.CurJob.def == JobDefOf.Repair
                            || this.Driver.CurJob.def == JobDefOf.FixBrokenDownBuilding
                            || this.Driver.CurJob.def == JobDefOf.UseCommsConsole
                            || this.Driver.CurJob.def == JobDefOf.BuryCorpse
                            || this.Driver.CurJob.def == JobDefOf.TradeWithPawn
                            || this.Driver.CurJob.def == JobDefOf.Lovin
                            || this.Driver.CurJob.def == JobDefOf.SocialFight
                            || this.Driver.CurJob.def == JobDefOf.Maintain
                            || this.Driver.CurJob.def == JobDefOf.MarryAdjacentPawn
                            || this.Driver.CurJob.def == JobDefOf.SpectateCeremony
                            || this.Driver.CurJob.def == JobDefOf.StandAndBeSociallyActive
                            || this.Driver.CurJob.def == JobDefOf.LayDown || this.Driver.CurJob.def == JobDefOf.Ingest
                            || this.Driver.CurJob.def == JobDefOf.SocialRelax
                            || this.Driver.CurJob.def == JobDefOf.Refuel

                            // || this.Driver.CurJob.def == JobDefOf.FillFermentingBarrel
                            // || this.Driver.CurJob.def == JobDefOf.TakeBeerOutOfFermentingBarrel
                            || this.Driver.CurJob.def == JobDefOf.TakeWoundedPrisonerToBed
                            || this.Driver.CurJob.def == JobDefOf.TakeToBedToOperate
                            || this.Driver.CurJob.def == JobDefOf.EscortPrisonerToBed
                            || this.Driver.CurJob.def == JobDefOf.CarryToCryptosleepCasket
                            || this.Driver.CurJob.def == JobDefOf.ReleasePrisoner
                            || this.Driver.CurJob.def == JobDefOf.PrisonerAttemptRecruit
                            || this.Driver.CurJob.def == JobDefOf.PrisonerFriendlyChat
                            || this.Driver.CurJob.def == JobDefOf.PrisonerExecution
                            || this.Driver.CurJob.def == JobDefOf.FeedPatient
                            || this.Driver.CurJob.def == JobDefOf.TendPatient
                            || this.Driver.CurJob.def == JobDefOf.VisitSickPawn
                            || this.Driver.CurJob.def == JobDefOf.Slaughter || this.Driver.CurJob.def == JobDefOf.Milk
                            || this.Driver.CurJob.def == JobDefOf.Shear || this.Driver.CurJob.def == JobDefOf.Train
                            || this.Driver.CurJob.def == JobDefOf.Mate || this.Driver.health.HasHediffsNeedingTend())
                        && this.Driver.Position.Roofed(this.Driver.Map))
                    {
                        this.cart.Position = this.Position.ToIntVec3();
                        this.cart.Rotation = this.Driver.Rotation;
                        if (!this.Driver.Position.InBounds(this.cart.Map))
                        {
                            Log.Message("3");
                            this.DismountAt(this.Driver.Position);
                            return;
                        }

                        Log.Message("4");
                        this.DismountAt(this.Driver.Position - this.InteractionOffset.ToIntVec3());
                        this.Driver.Position = this.Driver.Position.RandomAdjacentCell8Way();
                        return;
                    }
                }

                this.tickCheck = Find.TickManager.TicksGame;
                this.tickCooldown = Rand.RangeInclusive(60, 180);


                // bring vehicles home
                if (cart.VehicleComp != null && !cart.VehicleComp.MotorizedWithoutFuel())
                {
                    float hitPointsPercent = this.parent.HitPoints / this.parent.MaxHitPoints;

                    if (this.cart.Faction.IsPlayer)
                    {
                        if (!GenAI.EnemyIsNear(this.Driver, 120f))
                        {
                            if (!this.Driver.drafter.Drafted)
                            {
                                var flag = this.cart.HasGasTank() && this.cart.GasTankComp.tankLeaking;

                                if (hitPointsPercent < 0.65f
                                    //          || (this.Driver.CurJob != null && this.Driver.jobs.curDriver.asleep)
                                    || flag || !this.cart.RefuelableComp.HasFuel)
                                {
                                    Job jobNew = this.Driver.DismountAtParkingLot("CM");
                                    this.Driver.jobs.TryTakeOrderedJob(jobNew);
                                }
                            }
                        }
                    }
                }
            }

            if (Find.TickManager.TicksGame - this.tickLastDoorCheck >= 96
                && (this.Driver.Position.GetDoor(this.parent.Map) != null
                    || this.parent.Position.GetDoor(this.parent.Map) != null))
            {
                this.lastPassedDoor = this.Driver.Position.GetDoor(this.Driver.Map) != null
                                           ? this.Driver.Position.GetDoor(this.Driver.Map)
                                           : this.cart.Position.GetDoor(this.cart.Map);
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
            if (this.Driver.pather.Moving && !this.Position.ToIntVec3().CloseToEdge(this.cart.Map, 2))
            {
                // Not there yet
                if (this.Driver.Position != this.Driver.pather.Destination.Cell)
                {
                    this.lastDrawAsAngle = this.Driver.Rotation.AsAngle;
                    this.cart.Position = this.Position.ToIntVec3();

                    // Check rotation
                    if (this.Driver.Rotation != this.cart.Rotation)
                    {
                        if (this.Driver.Rotation.Opposite == this.cart.Rotation)
                        {
                            // Don't rotate if near tagert or short distance
                            if (!this.Driver.pather.Destination.Cell.InHorDistOf(this.cart.Position, 12f))
                            {
                                this.cart.Rotation = this.Driver.Rotation;
                            }
                        }
                        else
                        {
                            this.cart.Rotation = this.Driver.Rotation;
                        }
                    }
                }
            }
        }

        public CompProperties_Mountable Props => (CompProperties_Mountable)this.props;

        public void Dismount()
        {
            Vehicle_Cart cart = this.parent as Vehicle_Cart;

            if (this.Driver.AllComps.Contains(cart.DriverComp))
            {
                this.Driver.AllComps?.Remove(cart.DriverComp);
                cart.DriverComp.Vehicle = null;
                cart.DriverComp.parent = null;
            }

            this.Driver.RaceProps.makesFootprints = this.Driver.def.race.makesFootprints;

            // if (Find.Reservations.IsReserved(parent, Driver.Faction))
            this.parent.Map.reservationManager.ReleaseAllForTarget(this.parent);

            if (this.Driver.Faction != Faction.OfPlayer)
            {
                //    this.parent.SetForbidden(true);
                if (this.Driver.Dead)
                {
                    this.parent.SetFaction(Faction.OfInsects);
                }
            }
            this.Driver.Position = this.Driver.Position.RandomAdjacentCell8Way();

            this.Driver = null;
            this.IsMounted = false;

            cart.EndSustainerVehicleIfActive();

            // Find.ListerBuildings.Add(parent as Building);
        }

        public void DismountAt(IntVec3 dismountPos)
        {
            // if (Driver.Position.IsAdjacentTo8WayOrInside(dismountPos, Driver.Rotation, new IntVec2(1,1)))
            if (dismountPos != IntVec3.Invalid)
            {
                this.Dismount();
                this.cart.Position = dismountPos;
                return;
            }

            Log.Warning("Tried dismount at " + dismountPos);
        }

        public void MountOn(Pawn pawn)
        {
            if (this.Driver != null)
            {
                return;
            }

            Vehicle_Cart cart = this.parent as Vehicle_Cart;
            if (cart == null)
            {
                return;
            }

            // Check to make pawns not mount two vehicles at once
            if (pawn.IsDriver())
            {
                if (pawn.MountedVehicle() != null)
                {
                    pawn.MountedVehicle().MountableComp.Dismount();
                }
            }

            this.Driver = pawn;
            this.IsMounted = true;

            if (pawn.RaceProps.Humanlike)
            {
                pawn.RaceProps.makesFootprints = false;
            }

            cart.DriverComp = new CompDriver { Vehicle = this.parent };
            this.Driver?.AllComps?.Add(cart.DriverComp);
            cart.DriverComp.parent = this.Driver;

            // Set faction of vehicle to whoever mounts it
            if (cart.Faction != this.Driver.Faction)
            {
                cart.SetFaction(this.Driver.Faction);
            }

            cart.StartSustainerVehicleIfInactive();



            this.IsPrisonBreaking = PrisonBreakUtility.IsPrisonBreaking(pawn);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref this.Driver, "Driver");
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
                if (this.cart.CanExplode()
                    && this.cart.ExplosiveComp.wickStarted)
                {
                    if (Rand.Value >= 0.1f)
                    {
                        this.DismountAt(
                            (this.Driver.Position - this.InteractionOffset.ToIntVec3()).RandomAdjacentCell8Way());
                    }
                    else
                    {
                        this.Driver.TryAttachFire(0.1f);
                        this.cart.TryAttachFire(0.1f);
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
            cart = this.parent as Vehicle_Cart;

            base.PostSpawnSetup(respawningAfterLoad);

            if (this.Driver != null)
            {
                this.IsMounted = true;

                // if (this.Driver.RaceProps.Humanlike)
                if (this.Driver.RaceProps.Humanlike)
                {
                    this.Driver.RaceProps.makesFootprints = false;
                }
            }

            // cart.DriverComp = new CompDriver { Vehicle = this.parent };
            // Driver.AllComps?.Add(cart.DriverComp);
            // cart.DriverComp.parent = this.Driver;
        }
    }
}