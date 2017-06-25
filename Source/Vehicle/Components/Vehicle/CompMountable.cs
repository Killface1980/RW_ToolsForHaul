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

#if CR
using Combat_Realism;
#endif

    public class CompMountable : ThingComp
    {
        private const string TxtCommandDismountDesc = "CommandDismountDesc";

        private const string TxtCommandDismountLabel = "CommandDismountLabel";

        private const string TxtCommandMountDesc = "CommandMountDesc";

        private const string TxtCommandMountLabel = "CommandMountLabel";

        private const string TxtDismount = "Dismount";

        private const string TxtMountOn = "MountOn";

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

        public IEnumerable<FloatMenuOption> CompGetFloatMenuOptionsForExtra(Pawn myPawn)
        {
            // order to drive
            Action action_Order;
            string verb;
            if (this.parent.Faction == Faction.OfPlayer)
            {
                if (!this.IsMounted)
                {
                    if (!this.parent.IsForbidden(Faction.OfPlayer))
                    {
                        action_Order = () =>
                            {
                                this.parent.Map.reservationManager.ReleaseAllForTarget(this.parent);
                                this.parent.Map.reservationManager.Reserve(myPawn, this.parent);
                                Job jobNew = new Job(HaulJobDefOf.Mount, this.parent);
                                myPawn.jobs.TryTakeOrderedJob(jobNew);
                            };
                        verb = TxtMountOn;
                        yield return new FloatMenuOption(verb.Translate(this.parent.LabelShort), action_Order);
                    }
                    else if (this.IsMounted && myPawn == this.Driver)
                    {
                        // && !myPawn.health.hediffSet.HasHediff(HediffDef.Named("HediffWheelChair")))
                        action_Order = () => { this.Dismount(); };
                        verb = TxtDismount;
                        yield return new FloatMenuOption(verb.Translate(this.parent.LabelShort), action_Order);
                    }
                }
                else if (this.IsMounted && myPawn == this.Driver)
                {
                    // && !myPawn.health.hediffSet.HasHediff(HediffDef.Named("HediffWheelChair")))
                    action_Order = () => { this.Dismount(); };
                    verb = TxtDismount;
                    yield return new FloatMenuOption(verb.Translate(this.parent.LabelShort), action_Order);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Command compCom in base.CompGetGizmosExtra()) yield return compCom;

            if (this.parent.Faction != Faction.OfPlayer) yield break;

            Command_Action com = new Command_Action();

            if (this.IsMounted)
            {
                com.defaultLabel = TxtCommandDismountLabel.Translate();
                com.defaultDesc = TxtCommandDismountDesc.Translate();
                com.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnmount");
                com.activateSound = SoundDef.Named("Click");
                com.action = Dismount;

                yield return com;
            }
            else
            {
                Designator_Mount designator = new Designator_Mount();

                designator.vehicle = this.parent;
                designator.defaultLabel = TxtCommandMountLabel.Translate();
                designator.defaultDesc = TxtCommandMountDesc.Translate();
                designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconMount");
                designator.activateSound = SoundDef.Named("Click");

                yield return designator;
            }
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
                    || this.Driver.MentalStateDef == MentalStateDefOf.WanderPsychotic
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

                CompVehicle vehicleComp = this.parent.TryGetComp<CompVehicle>();

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
                            //    || this.Driver.health.NeedsMedicalRest || this.Driver.health.PrefersMedicalRest
                            )
                            && this.Driver.Position.Roofed(Driver.Map))
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

                    // bring vehicles home
                    if (!vehicleComp.MotorizedWithoutFuel())
                    {
                        CompRefuelable refuelableComp = this.parent.TryGetComp<CompRefuelable>();
                        Job jobNew = ToolsForHaulUtility.DismountInBase(
                            this.Driver,
                           GameComponentToolsForHaul.CurrentVehicle[this.Driver]);
                        float hitPointsPercent = this.parent.HitPoints / this.parent.MaxHitPoints;

                        if (this.Driver.Faction == Faction.OfPlayer)
                        {
                            if (!GenAI.EnemyIsNear(this.Driver, 40f))
                            {
                                if (hitPointsPercent < 0.65f
                                    || (this.Driver.CurJob != null && this.Driver.jobs.curDriver.asleep)
                                    || ((this.parent as Vehicle_Cart) != null
                                        && (this.parent as Vehicle_Cart).VehicleComp.tankLeaking)
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

        /// <summary>
        /// The dismount.
        /// </summary>
        public void Dismount()
        {
#if CR
            Building_Reloadable turret = (parent as Building_Reloadable);
            if (turret != null)
            {
                turret.dontReload = false;
            }
#endif
            if (this.Driver.RaceProps.Humanlike)
            {
                this.Driver.AllComps?.Remove(this.DriverComp);
                this.DriverComp.Vehicle = null;
                this.DriverComp.parent = null;
            }

            GameComponentToolsForHaul.CurrentVehicle.Remove(this.Driver);

            this.Driver.RaceProps.makesFootprints = true;

            // if (Find.Reservations.IsReserved(parent, Driver.Faction))
            this.parent.Map.reservationManager.ReleaseAllForTarget(this.parent);
            if (this.Driver.Faction != Faction.OfPlayer)
            {
                this.parent.SetForbidden(true);
                this.parent.SetFaction(null);
            }

            this.Driver = null;

            if (this.SustainerAmbient != null)
            {
                this.SustainerAmbient.End();
            }

            // Find.ListerBuildings.Add(parent as Building);
        }

        /// <summary>
        /// The dismount at.
        /// </summary>
        /// <param name="dismountPos">
        /// The dismount pos.
        /// </param>
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
#if CR
            Building_Reloadable turret = (parent as Building_Reloadable);
            if (turret != null)
            {
                turret.dontReload = true;
            }
#endif

            // Check to make pawns not mount two vehicles at once
            if (ToolsForHaulUtility.IsDriver(pawn))
            {
                if (ToolsForHaulUtility.GetCartByDriver(pawn) != null)
                {
                    ToolsForHaulUtility.GetCartByDriver(pawn).TryGetComp<CompMountable>().Dismount();
                }

                if (ToolsForHaulUtility.GetTurretByDriver(pawn) != null)
                {
                    ToolsForHaulUtility.GetTurretByDriver(pawn).MountableComp.Dismount();
                }
            }

            this.Driver = pawn;

            GameComponentToolsForHaul.CurrentVehicle.Add(pawn, this.parent);

            if (this.Driver.RaceProps.Humanlike)
            {
                this.Driver.RaceProps.makesFootprints = false;
                this.DriverComp = new CompDriver { Vehicle = this.parent as Building };
                this.Driver?.AllComps?.Add(this.DriverComp);
                this.DriverComp.parent = this.Driver;
            }

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
            if (ToolsForHaulUtility.Cart.Contains(this.parent))
            {
                ToolsForHaulUtility.Cart.Remove(this.parent);
            }

            if (ToolsForHaulUtility.Cart.Contains(this.parent))
            {
                ToolsForHaulUtility.Cart.Remove(this.parent);
            }

            if (this.IsMounted)
            {
                if (GameComponentToolsForHaul.CurrentVehicle.ContainsKey(this.Driver))
                {
                    GameComponentToolsForHaul.CurrentVehicle.Remove(this.Driver);
                }
            }

            if (this.SustainerAmbient != null)
            {
                this.SustainerAmbient.End();
            }

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

            if (this.Driver == null) return;

            if (compVehicle.IsCurrentlyMotorized())
            {
                LongEventHandler.ExecuteWhenFinished(
                    delegate
                        {
                            SoundInfo info = SoundInfo.InMap(this.parent);
                            this.SustainerAmbient = compVehicle.compProps.soundAmbient.TrySpawnSustainer(info);
                        });
            }
            if (!GameComponentToolsForHaul.CurrentVehicle.ContainsKey(this.Driver))
            {
                GameComponentToolsForHaul.CurrentVehicle.Add(this.Driver, parent);
            }

            if (this.Driver.RaceProps.Humanlike)
            {
                this.Driver.RaceProps.makesFootprints = false;
                this.DriverComp = new CompDriver { Vehicle = this.parent };
                this.Driver.AllComps?.Add(this.DriverComp);
                this.DriverComp.parent = this.Driver;
            }

        }
    }
}