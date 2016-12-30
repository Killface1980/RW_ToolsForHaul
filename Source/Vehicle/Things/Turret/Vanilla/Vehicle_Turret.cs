#if !CR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if Headlights
using ppumkin.LEDTechnology.Managers;
#endif
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.JobDefs;
using ToolsForHaul.StatDefs;
using ToolsForHaul.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ToolsForHaul
{
    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.Components.Vehicles;

    [StaticConstructorOnStartup]
    public class Vehicle_Turret : Building_Turret, IAttackTarget
    {
        #region Tank

        protected StunHandler stunner;

        protected LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;

        public override LocalTargetInfo CurrentTarget { get; }

        public override Verb AttackVerb { get; }

        #endregion

        #region Variables

        // ==================================
        public int DefaultMaxItem => (int)this.GetStatValue(HaulStatDefOf.VehicleMaxItem);

        public int MaxItemPerBodySize => (int)this.GetStatValue(HaulStatDefOf.VehicleMaxItem);

        public bool instantiated;

        // RimWorld.Building_TurretGun
        public override void OrderAttack(LocalTargetInfo targ)
        {
        }

        public bool ThreatDisabled()
        {
            CompPowerTrader comp = this.GetComp<CompPowerTrader>();
            if (comp == null || !comp.PowerOn)
            {
                CompMannable comp2 = this.GetComp<CompMannable>();
                if (comp2 == null || !comp2.MannedNow)
                {
                    return true;
                }
            }

            return !this.mountableComp.IsMounted;
            return false;
        }

        // TODO make vehicles break down & get repaired like buildings
        public override bool ClaimableBy(Faction faction)
        {
            if (!this.mountableComp.IsMounted)
            {
                return true;
            }

            CompPowerTrader comp = this.GetComp<CompPowerTrader>();
            if (comp == null || !comp.PowerOn)
            {
                CompMannable comp2 = this.GetComp<CompMannable>();
                if (comp2 == null || !comp2.MannedNow)
                {
                    return true;
                }
            }

            return false;
        }

        public float DesiredSpeed => this.GetStatValue(HaulStatDefOf.VehicleSpeed);

        public bool IsCurrentlyMotorized()
        {
            return (this.refuelableComp != null && this.refuelableComp.HasFuel)
                   || this.vehicleComp.MotorizedWithoutFuel();
        }

        public bool fueledByAI;

        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;


        // mount and storage data
        public ThingContainer storage;

        public ThingContainer GetContainer()
        {
            return this.storage;
        }

        public IntVec3 GetPosition()
        {
            return this.Position;
        }

        // slotGroupParent Interface
        public ThingFilter allowances;

        public CompMountable mountableComp => this.GetComp<CompMountable>();

        public CompRefuelable refuelableComp => this.GetComp<CompRefuelable>();

        public CompExplosive explosiveComp => this.GetComp<CompExplosive>();

        public CompBreakdownable breakdownableComp => this.GetComp<CompBreakdownable>();

        public CompAxles axlesComp => this.GetComp<CompAxles>();

        public CompVehicle vehicleComp => this.GetComp<CompVehicle>();

        #endregion

        #region Setup Work

        public Vehicle_Turret()
        {
            this.stunner = new StunHandler(this);
        }

        static Vehicle_Turret()
        {
        }

        // public static ListerVehicles listerVehicles = new ListerVehicles();
#if Headlights
        HeadLights flooder;
#endif

        public override void  SpawnSetup(Map map)
        {
            base.SpawnSetup(map);

            ToolsForHaulUtility.CartTurret.Add(this);

            if (this.mountableComp.Driver != null && this.IsCurrentlyMotorized())
                LongEventHandler.ExecuteWhenFinished(
                    delegate
                        {
                            SoundInfo info = SoundInfo.InMap(this);
                            this.mountableComp.SustainerAmbient =
                                this.vehicleComp.compProps.soundAmbient.TrySpawnSustainer(info);
                        });

            if (this.mountableComp.Driver != null)
            {
                this.mountableComp.Driver.RaceProps.makesFootprints = false;

                if (this.mountableComp.Driver.RaceProps.Humanlike)
                {
                    this.mountableComp.DriverComp = new CompDriver { Vehicle = this };
                    this.mountableComp.Driver.AllComps?.Add(this.mountableComp.DriverComp);
                    this.mountableComp.DriverComp.parent = this.mountableComp.Driver;
                }
            }

            if (this.allowances == null)
            {
                this.allowances = new ThingFilter();
                this.allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
                this.allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
            }
        }

        public override void DeSpawn()
        {
            // bool hasChair = false;
            // foreach (Pawn pawn in Find.MapPawns.AllPawnsSpawned)
            // {
            // if (pawn.health.hediffSet.HasHediff(HediffDef.Named("HediffWheelChair")) &&
            // !ToolsForHaulUtility.IsDriver(pawn) && base.Position.AdjacentTo8WayOrInside(pawn.Position))
            // {
            // mountableComp.MountOn(pawn);
            // hasChair = true;
            // break;
            // }
            // }
            // if (!hasChair)
            // {
            if (ToolsForHaulUtility.CartTurret.Contains(this)) ToolsForHaulUtility.CartTurret.Remove(this);

            if (this.mountableComp.IsMounted) if (MapComponent_ToolsForHaul.currentVehicle.ContainsKey(this.mountableComp.Driver)) MapComponent_ToolsForHaul.currentVehicle.Remove(this.mountableComp.Driver);

            if (this.mountableComp.SustainerAmbient != null) this.mountableComp.SustainerAmbient.End();
            base.DeSpawn();

            // not working
            // if (explosiveComp != null && explosiveComp.wickStarted)
            // if (tankLeaking)
            // {
            // FireUtility.TryStartFireIn(Position, 0.3f);
            // if (Rand.Value > 0.8f)
            // {
            // FireUtility.TryStartFireIn(Position.RandomAdjacentCell8Way(), 0.1f);
            // }
            // if (Rand.Value > 0.7f)
            // {
            // FireUtility.TryStartFireIn(Position.RandomAdjacentCell8Way(), 0.1f);
            // }
            // if (Rand.Value > 0.6f)
            // {
            // FireUtility.TryStartFireIn(Position.RandomAdjacentCell8Way(), 0.1f);
            // }
            // if (Rand.Value > 0.5f)
            // {
            // FireUtility.TryStartFireIn(Position.RandomAdjacentCell8Way(), 0.1f);
            // }
            // }
            // }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep(ref this.storage, "storage");
            Scribe_Deep.LookDeep(ref this.allowances, "allowances");
            Scribe_Values.LookValue(ref this.vehicleComp.tankLeaking, "tankLeaking");
            Scribe_Values.LookValue(ref this._tankHitPos, "tankHitPos");

            Scribe_Deep.LookDeep(ref this.stunner, "stunner", new object[] { this });
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo baseGizmo in base.GetGizmos()) yield return baseGizmo;

            if (this.Faction != Faction.OfPlayer) yield break;

            if (this.explosiveComp != null)
            {
                Command_Action command_Action = new Command_Action
                                                    {
                                                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate"),
                                                        defaultDesc = "CommandDetonateDesc".Translate(),
                                                        action = Command_Detonate
                                                    };
                if (this.explosiveComp.wickStarted)
                {
                    command_Action.Disable();
                }

                command_Action.defaultLabel = "CommandDetonateLabel".Translate();
                yield return command_Action;
            }

            // yield return new Command_Action
            // {
            // defaultLabel = "CommandBedSetOwnerLabel".Translate(),
            // icon = ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", true),
            // defaultDesc = "CommandGraveAssignColonistDesc".Translate(),
            // action = delegate
            // {
            // Find.WindowStack.Add(new Dialog_AssignBuildingOwner(this));
            // },
            // hotKey = KeyBindingDefOf.Misc3
            // };
        }

        private void Command_Detonate()
        {
            this.explosiveComp.StartWick();
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            // do nothing if not of colony
            if (this.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
            {
                yield return fmo;
            }

            Map map = myPawn.Map;

            Action action_Mount = () =>
                {
                    Job jobNew = new Job(HaulJobDefOf.Mount);
                    map.reservationManager.ReleaseAllForTarget(this);
                    jobNew.targetA = this;
                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_DismountInBase = () =>
                {
                    Job jobNew = ToolsForHaulUtility.DismountInBase(this.mountableComp.Driver, this);

                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_Dismount = () =>
                {
                    if (!myPawn.Position.InBounds(map))
                    {
                        this.mountableComp.DismountAt(myPawn.Position);
                        return;
                    }

                    this.mountableComp.DismountAt(
                        myPawn.Position - this.def.interactionCellOffset.RotatedBy(myPawn.Rotation));
                    myPawn.Position = myPawn.Position.RandomAdjacentCell8Way();

                    // mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
                };

            Action action_MakeMount = () =>
                {
                    Pawn worker = null;
                    Job jobNew = new Job(HaulJobDefOf.MakeMount);
                    map.reservationManager.ReleaseAllForTarget(this);
                    jobNew.count = 1;
                    jobNew.targetA = this;
                    jobNew.targetB = myPawn;
                    foreach (Pawn colonyPawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
                        if (colonyPawn.CurJob.def != jobNew.def
                            && (worker == null
                                || (worker.Position - myPawn.Position).LengthHorizontal
                                > (colonyPawn.Position - myPawn.Position).LengthHorizontal)) worker = colonyPawn;
                    if (worker == null)
                    {
                        Messages.Message("NoWorkForMakeMount".Translate(), MessageSound.RejectInput);
                    }
                    else worker.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_Deconstruct = () =>
                {
                    map.reservationManager.ReleaseAllForTarget(this);
                    map.reservationManager.Reserve(myPawn, this);
                    map.designationManager.AddDesignation(new Designation(this, DesignationDefOf.Deconstruct));
                    Job job = new Job(JobDefOf.Deconstruct, this);
                    myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                };
            bool alreadyMounted = false;
            if (!this.mountableComp.IsMounted)
            {
                foreach (Vehicle_Turret cart in ToolsForHaulUtility.CartTurret) if (cart.mountableComp.Driver == myPawn) alreadyMounted = true;

                if (myPawn.Faction == Faction.OfPlayer && (myPawn.RaceProps.IsMechanoid || myPawn.RaceProps.Humanlike)
                    && !alreadyMounted && !this.IsForbidden(myPawn.Faction))
                {
                    yield return new FloatMenuOption("Mount".Translate(this.LabelShort), action_Mount);
                }

                yield return new FloatMenuOption("Deconstruct".Translate(this.LabelShort), action_Deconstruct);
            }
            else if (myPawn == this.mountableComp.Driver)
            {
                yield return new FloatMenuOption("Dismount".Translate(this.LabelShort), action_Dismount);
            }

            yield return new FloatMenuOption("DismountInBase".Translate(this.LabelShort), action_DismountInBase);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.Deconstruct) mode = DestroyMode.Kill;
            else if (this.explosiveComp != null && this.explosiveComp.wickStarted)
            {
                this.storage.ClearAndDestroyContents();
            }

            this.storage.TryDropAll(this.Position, Map, ThingPlaceMode.Near);

            base.Destroy(mode);
        }

        private ThingDef fuelDefName = ThingDef.Named("FilthFuel");

        /// <summary>
        /// PreApplyDamage from Building_Turret - not sure what the stunner does
        /// </summary>
        /// <param name="dinfo"></param>
        /// <param name="absorbed"></param>
        public override void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(dinfo, out absorbed);
            if (absorbed)
            {
                return;
            }

            this.stunner.Notify_DamageApplied(dinfo, true);
            absorbed = false;
        }

        #endregion

        #region Ticker

        public override void Tick()
        {
            if (!this.instantiated)
            {
                this.vehicleComp.currentDriverSpeed = this.vehicleComp.VehicleSpeed;
                this.instantiated = true;
            }

            base.Tick();

#if Headlights
            {
                if (Find.GlowGrid.GameGlowAt(Position - Rotation.FacingCell - Rotation.FacingCell) < 0.4f)
                {
                    // TODO Add headlights to xml & move the flooder initialization to mountableComp
                    if (mountableComp.Driver != null && !compVehicles.AnimalsCanDrive() && flooder == null)
                    {
                        flooder = new HeadLights(Position, Rotation, this);
                        CustomGlowFloodManager.RegisterFlooder(flooder);
                        CustomGlowFloodManager.RefreshGlowFlooders();
                    }
                    if (mountableComp.Driver == null && flooder != null)
                    {
                        flooder.Clear();
                        CustomGlowFloodManager.DeRegisterGlower(flooder);
                        CustomGlowFloodManager.RefreshGlowFlooders();
                        flooder = null;
                    }
                    

// TODO optimized performance, lights only at night and when driver is mounted => light switch gizmo?
                    if (flooder != null)
                    {
                        flooder.Position = Position + Rotation.FacingCell + Rotation.FacingCell;
                        flooder.Orientation = Rotation;
                        flooder.Clear();
                        flooder.CalculateGlowFlood();
                    }
                }
                if (mountableComp.Driver == null && flooder != null || flooder != null)
                {
                    CustomGlowFloodManager.DeRegisterGlower(flooder);
                    CustomGlowFloodManager.RefreshGlowFlooders();
                    flooder = null;
                }
            }
#endif

            if (this.mountableComp.IsMounted)
            {
                if (this.refuelableComp != null)
                {
                    if (this.mountableComp.Driver.Faction != Faction.OfPlayer)
                        if (!this.fueledByAI)
                        {
                            if (this.refuelableComp.FuelPercentOfMax < 0.550000011920929)
                                this.refuelableComp.Refuel(
                                    ThingMaker.MakeThing(
                                        this.refuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                            else this.fueledByAI = true;
                        }
                }

                if (this.mountableComp.Driver.pather.Moving)
                {
                    // || mountableComp.Driver.drafter.pawn.pather.Moving)
                    if (!this.mountableComp.Driver.stances.FullBodyBusy && this.axlesComp.HasAxles())
                    {
                        this.axlesComp.wheelRotation += this.vehicleComp.currentDriverSpeed / 3f;
                        this.axlesComp.tick_time += 0.01f * this.vehicleComp.currentDriverSpeed / 5f;
                    }
                }

                if (Find.TickManager.TicksGame - this.tickCheck >= this.tickCooldown)
                {
                    if (this.mountableComp.Driver.pather.Moving)
                    {
                        if (!this.mountableComp.Driver.stances.FullBodyBusy)
                        {
                            if (this.refuelableComp != null) this.refuelableComp.Notify_UsedThisTick();
                            this.damagetick -= 1;

                            if (this.axlesComp.HasAxles())
                                this.vehicleComp.currentDriverSpeed =
                                    ToolsForHaulUtility.GetMoveSpeed(this.mountableComp.Driver);
                        }

                        if (this.breakdownableComp != null && this.breakdownableComp.BrokenDown
                            || this.refuelableComp != null && !this.refuelableComp.HasFuel) this.vehicleComp.VehicleSpeed = 0.75f;
                        else this.vehicleComp.VehicleSpeed = this.DesiredSpeed;
                        this.tickCheck = Find.TickManager.TicksGame;
                    }

                    if (this.Position.InNoBuildEdgeArea(Map) && this.vehicleComp.despawnAtEdge && this.Spawned
                        && (this.mountableComp.Driver.Faction != Faction.OfPlayer
                            || this.mountableComp.Driver.MentalState.def == MentalStateDefOf.PanicFlee)) this.DeSpawn();
                }
            }

            // if (Find.TickManager.TicksGame >= damagetick)
            // {
            // TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1, null, null, null));
            // damagetick = Find.TickManager.TicksGame + 3600;
            // }
            if (this.vehicleComp.tankLeaking)
            {
                if (Find.TickManager.TicksGame > this._tankSpillTick)
                {
                    if (this.refuelableComp.FuelPercentOfMax > this._tankHitPos)
                    {
                        this.refuelableComp.ConsumeFuel(0.15f);

                        FilthMaker.MakeFilth(this.Position, Map,this.fuelDefName, this.LabelCap);
                        this._tankSpillTick = Find.TickManager.TicksGame + 15;
                    }
                }
            }
        }

        private static readonly Vector3 TrailOffset = new Vector3(0f, 0f, -0.3f);

        private static readonly Vector3 FumesOffset = new Vector3(-0.3f, 0f, 0f);

        private static readonly Vector3 DustOffset = new Vector3(-0.3f, 0f, -0.3f);

        private float _tankHitPos = 1f;

        int damagetick = -5000;

        private int _tankSpillTick = -5000;

        #endregion

        #region Graphics / Inspections

        public override Vector3 DrawPos
        {
            get
            {
                if (!this.Spawned || !this.mountableComp.IsMounted || !this.instantiated)
                {
                    return base.DrawPos;
                }

                float num = this.mountableComp.Driver.Drawer.renderer.graphics.nakedGraphic.drawSize.x - 1f;
                num *= this.mountableComp.Driver.Rotation.AsInt % 2 == 1 ? 0.5f : 0.25f;
                Vector3 vector = new Vector3(0f, 0f, -num);
                return this.mountableComp.Position + vector.RotatedBy(this.mountableComp.Driver.Rotation.AsAngle);
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());

            string currentDriverString;
            if (this.mountableComp.Driver != null) currentDriverString = this.mountableComp.Driver.LabelCap;
            else currentDriverString = "NoDriver".Translate();

            stringBuilder.AppendLine("Driver".Translate() + ": " + currentDriverString);
            if (this.vehicleComp.tankLeaking) stringBuilder.AppendLine("TankLeaking".Translate());

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
    }
}
#endif