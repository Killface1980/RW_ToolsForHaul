// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompMountable.cs" company="">
// </copyright>
// <summary>
//   Defines the CompMountable type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ToolsForHaul.Components.Vehicle
{
    using System;
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Components.Vehicles;
    using ToolsForHaul.Designators;
    using ToolsForHaul.JobDefs;
    using ToolsForHaul.Utilities;

    using UnityEngine;

    using Verse;
    using Verse.AI;
    using Verse.Sound;

    public class CompMountable : ThingComp
    {
        private Pawn driver;

        private CompDriver driverComp;

        public float lastDrawAsAngle = 0;

        private Building_Door lastPassedDoor;

        private Sustainer sustainerAmbient;

        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;

        private int tickLastDoorCheck = Find.TickManager.TicksGame;

        public bool IsPrisonBreaking;

        public Vector3 InteractionOffset => this.parent.def.interactionCellOffset.ToVector3().RotatedBy(this.lastDrawAsAngle);

        public bool IsMounted => this.Driver != null;

        public Vector3 Position
        {
            get
            {
                Vector3 position;

                position = this.Driver.DrawPos - this.InteractionOffset * 1.3f;

                // No Driver
                if (this.Driver == null) return this.parent.DrawPos;

                // Out of bound or Preventing cart from stucking door
                if (!position.InBounds(this.parent.Map)) return this.Driver.DrawPos;

                if (!position.ToIntVec3().Walkable(this.parent.Map)) return this.Driver.DrawPos;

                return position;
            }
        }

        public Sustainer SustainerAmbient
        {
            get
            {
                return this.sustainerAmbient;
            }

            set
            {
                this.sustainerAmbient = value;
            }
        }

        public Pawn Driver
        {
            get
            {
                return this.driver;
            }

            set
            {
                this.driver = value;
            }
        }

        public CompDriver DriverComp
        {
            get
            {
                return this.driverComp;
            }

            set
            {
                this.driverComp = value;
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (this.parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Action action_Mount = () =>
                {
                    Job jobNew = new Job(HaulJobDefOf.Mount);
                    selPawn.Map.reservationManager.ReleaseAllForTarget(this.parent);
                    selPawn.Map.reservationManager.Reserve(selPawn, this.parent);
                    jobNew.targetA = this.parent;
                    selPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_DismountInBase = () =>
                {
                    Job jobNew = ToolsForHaulUtility.DismountAtParkingLot(selPawn, this.parent as Vehicle_Cart);

                    selPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            if (!this.IsMounted)
            {
                if (!this.parent.IsForbidden(Faction.OfPlayer))
                {
                    if (selPawn.Faction == Faction.OfPlayer && (selPawn.RaceProps.IsMechanoid || selPawn.RaceProps.Humanlike)
                        && !ToolsForHaulUtility.IsDriverOfThisVehicle(selPawn, this.parent))
                    {
                        yield return new FloatMenuOption("MountOn".Translate(this.parent.LabelShort), action_Mount);
                        yield return new FloatMenuOption("DismountAtParkingLot".Translate(this.parent.LabelShort), action_DismountInBase);
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Command compCom in base.CompGetGizmosExtra())
            {
                yield return compCom;
            }

            if (this.parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Designator_Mount designator =
                new Designator_Mount
                {
                    vehicle = this.parent,
                    defaultLabel = Strings.TxtCommandMountLabel.Translate(),
                    defaultDesc = Strings.TxtCommandMountDesc.Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/IconMount"),
                    activateSound = SoundDef.Named("Click")
                };

            yield return designator;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (this.IsMounted)
            {
                if (!this.Driver.Spawned)
                {
                    this.parent.DeSpawn();
                    return;
                }

                if (this.Driver.Dead || this.Driver.Downed || this.Driver.health.InPainShock
                    || this.Driver.InMentalState
                    || (this.parent.IsForbidden(Faction.OfPlayer) && this.Driver.Faction == Faction.OfPlayer))
                {
                    if (!this.Driver.Position.InBounds(this.parent.Map))
                    {
                        this.DismountAt(this.Driver.Position);
                        return;
                    }

                    this.DismountAt(
                        this.Driver.Position - this.parent.def.interactionCellOffset.RotatedBy(this.Driver.Rotation));
                    this.Driver.Position = this.Driver.Position.RandomAdjacentCell8Way();
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
                                || this.Driver.CurJob.def == JobDefOf.DoBill
                                || this.Driver.CurJob.def == JobDefOf.Research
                                //   || this.Driver.CurJob.def == JobDefOf.OperateDeepDrill
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
                                || this.Driver.CurJob.def == JobDefOf.LayDown
                                || this.Driver.CurJob.def == JobDefOf.Ingest
                                || this.Driver.CurJob.def == JobDefOf.SocialRelax
                                || this.Driver.CurJob.def == JobDefOf.Refuel
                                //    || this.Driver.CurJob.def == JobDefOf.FillFermentingBarrel
                                //    || this.Driver.CurJob.def == JobDefOf.TakeBeerOutOfFermentingBarrel
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
                                || this.Driver.CurJob.def == JobDefOf.Slaughter
                                || this.Driver.CurJob.def == JobDefOf.Milk || this.Driver.CurJob.def == JobDefOf.Shear
                                || this.Driver.CurJob.def == JobDefOf.Train || this.Driver.CurJob.def == JobDefOf.Mate
                                || this.Driver.health.HasHediffsNeedingTend())
                            && this.Driver.Position.Roofed(this.Driver.Map))
                        {
                            this.parent.Position = this.Position.ToIntVec3();
                            this.parent.Rotation = this.Driver.Rotation;
                            if (!this.Driver.Position.InBounds(this.parent.Map))
                            {
                                this.DismountAt(this.Driver.Position);
                                return;
                            }

                            this.DismountAt(this.Driver.Position - this.InteractionOffset.ToIntVec3());
                            this.Driver.Position = this.Driver.Position.RandomAdjacentCell8Way();
                            return;
                        }
                    }

                    this.tickCheck = Find.TickManager.TicksGame;
                    this.tickCooldown = Rand.RangeInclusive(60, 180);

                    CompVehicle vehicleComp = this.parent.TryGetComp<CompVehicle>();
                    // bring vehicles home
                    if (vehicleComp != null && !vehicleComp.MotorizedWithoutFuel())
                    {
                        CompRefuelable refuelableComp = this.parent.TryGetComp<CompRefuelable>();
                        Job jobNew = ToolsForHaulUtility.DismountAtParkingLot(
                            this.Driver,
                           GameComponentToolsForHaul.CurrentDrivers[this.Driver]);
                        float hitPointsPercent = this.parent.HitPoints / this.parent.MaxHitPoints;

                        if (this.Driver.Faction == Faction.OfPlayer)
                        {
                            if (!GenAI.EnemyIsNear(this.Driver, 40f))
                            {
                                if (hitPointsPercent < 0.65f
                                    || (this.Driver.CurJob != null && this.Driver.jobs.curDriver.asleep)
                                    || ((this.parent as Vehicle_Cart) != null
                                        && (this.parent as Vehicle_Cart).VehicleComp.tankLeaking)
                                    || !refuelableComp.HasFuel)
                                {
                                    this.Driver.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                                }
                            }
                        }
                    }
                }

                if (Find.TickManager.TicksGame - this.tickLastDoorCheck >= 96
                    && (this.Driver.Position.GetEdifice(this.parent.Map) is Building_Door
                        || this.parent.Position.GetEdifice(this.parent.Map) is Building_Door))
                {
                    this.lastPassedDoor =
                        (this.Driver.Position.GetEdifice(this.parent.Map) is Building_Door
                             ? this.Driver.Position.GetEdifice(this.parent.Map)
                             : this.parent.Position.GetEdifice(this.parent.Map)) as Building_Door;
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

                Vector3 pos = this.parent.DrawPos;
                if (this.Driver.pather.Moving)
                {
                    if (this.Driver.Position != this.Driver.pather.Destination.Cell)
                    {
                        this.lastDrawAsAngle = this.Driver.Rotation.AsAngle;
                        this.parent.Position = this.Position.ToIntVec3();
                        this.parent.Rotation = this.Driver.Rotation;
                    }
                }
            }
        }

        public void Dismount()
        {
#if CR
            Building_Reloadable turret = (parent as Building_Reloadable);
            if (turret != null)
            {
                turret.dontReload = false;
            }
#endif
            //    if (this.Driver.RaceProps.Humanlike)
            {
                this.Driver.AllComps?.Remove(this.DriverComp);
                this.DriverComp.Vehicle = null;
                this.DriverComp.parent = null;
            }

            GameComponentToolsForHaul.CurrentDrivers.Remove(this.Driver);

            this.Driver.RaceProps.makesFootprints = true;

            // if (Find.Reservations.IsReserved(parent, Driver.Faction))
            this.parent.Map.reservationManager.ReleaseAllForTarget(this.parent);
            if (this.Driver.Faction != Faction.OfPlayer)
            {
                this.parent.SetForbidden(true);
                this.parent.SetFaction(null);
            }

            this.Driver = null;

            this.SustainerAmbient?.End();

            // Find.ListerBuildings.Add(parent as Building);
        }

        public void DismountAt(IntVec3 dismountPos)
        {
            // if (Driver.Position.IsAdjacentTo8WayOrInside(dismountPos, Driver.Rotation, new IntVec2(1,1)))
            if (dismountPos != IntVec3.Invalid)
            {
                this.Dismount();
                this.parent.Position = dismountPos;
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

            // Check to make pawns not mount two vehicles at once
            if (ToolsForHaulUtility.IsDriver(pawn))
            {
                if (ToolsForHaulUtility.GetCartByDriver(pawn) != null)
                {
                    ToolsForHaulUtility.GetCartByDriver(pawn).MountableComp.Dismount();
                }

            }

            this.Driver = pawn;


            if (this.Driver.RaceProps.Humanlike)
            {
                GameComponentToolsForHaul.CurrentDrivers.Add(pawn, this.parent as Vehicle_Cart);
                this.Driver.RaceProps.makesFootprints = false;
            }
            this.DriverComp = new CompDriver
            {
                Vehicle = this.parent
            };
            this.Driver?.AllComps?.Add(this.DriverComp);
            this.DriverComp.parent = this.Driver;

            Vehicle_Cart vehicleCart = this.parent as Vehicle_Cart;
            if (vehicleCart != null)
            {
                // Set faction of vehicle to whoever mounts it
                if (vehicleCart.Faction != this.driver.Faction)
                {
                    vehicleCart.SetFaction(this.driver.Faction, null);
                }

                if (vehicleCart.VehicleComp.IsCurrentlyMotorized())
                {
                    SoundInfo info = SoundInfo.InMap(this.parent);
                    this.SustainerAmbient = vehicleCart.VehicleComp.compProps.soundAmbient.TrySpawnSustainer(info);
                }

                this.IsPrisonBreaking = PrisonBreakUtility.IsPrisonBreaking(pawn);

                return;
            }
        }

        public override void PostDeSpawn(Map map)
        {
            if (this.IsMounted)
            {
                if (GameComponentToolsForHaul.CurrentDrivers.ContainsKey(this.Driver))
                {
                    GameComponentToolsForHaul.CurrentDrivers.Remove(this.Driver);
                }
            }

            this.SustainerAmbient?.End();

            base.PostDeSpawn(map);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref this.driver, "Driver");
            Scribe_References.Look(ref this.lastPassedDoor, "lastPassedDoor");
            Scribe_Values.Look(ref this.lastDrawAsAngle, "lastDrawAsAngle");
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (this.IsMounted)
            {
                if (this.parent.TryGetComp<CompExplosive>() != null
                    && this.parent.TryGetComp<CompExplosive>().wickStarted)
                {
                    if (Rand.Value >= 0.1f)
                    {
                        this.DismountAt(
                            (this.Driver.Position - this.InteractionOffset.ToIntVec3()).RandomAdjacentCell8Way());
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
            CompVehicle compVehicle = this.parent.TryGetComp<CompVehicle>();

            if (this.Driver == null)
            {
                return;
            }

            if (compVehicle.IsCurrentlyMotorized())
            {
                LongEventHandler.ExecuteWhenFinished(
                    delegate
                        {
                            SoundInfo info = SoundInfo.InMap(this.parent);
                            this.SustainerAmbient = compVehicle.compProps.soundAmbient.TrySpawnSustainer(info);
                        });
            }

            if (this.Driver.RaceProps.Humanlike)
            {
                if (!GameComponentToolsForHaul.CurrentDrivers.ContainsKey(this.Driver))
                {
                    GameComponentToolsForHaul.CurrentDrivers.Add(this.Driver, parent as Vehicle_Cart);
                }
                this.Driver.RaceProps.makesFootprints = false;
                this.DriverComp = new CompDriver { Vehicle = this.parent };
                this.Driver.AllComps?.Add(this.DriverComp);
                this.DriverComp.parent = this.Driver;
            }

        }
    }
}