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

    using CompVehicle = ToolsForHaul.Components.CompVehicle;

    [StaticConstructorOnStartup]
    public class Vehicle_Cart : Building_Turret, IThingHolder, IAttackTargetSearcher, IAttackTarget
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

            return !this.MountableComp.IsMounted;
            return false;
        }

        // TODO make vehicles break down & get repaired like buildings
        public override bool ClaimableBy(Faction faction)
        {
            if (!this.MountableComp.IsMounted)
            {
                return true;
            }

            // CompPowerTrader comp = this.GetComp<CompPowerTrader>();
            // if (comp == null || !comp.PowerOn)
            // {
            //     CompMannable comp2 = this.GetComp<CompMannable>();
            //     if (comp2 == null || !comp2.MannedNow)
            //     {
            //         return true;
            //     }
            // }

            return false;
        }

        public float DesiredSpeed => this.GetStatValue(HaulStatDefOf.VehicleSpeed);

        public bool IsCurrentlyMotorized()
        {
            return (this.RefuelableComp != null && this.RefuelableComp.HasFuel)
                   || this.VehicleComp.MotorizedWithoutFuel();
        }

        public bool fueledByAI;

        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;


        // mount and storage data
        public ThingOwner<Thing> innerContainer;

        public ThingOwner GetContainer()
        {
            return this.innerContainer;
        }

        public IntVec3 GetPosition()
        {
            return this.Position;
        }

        // slotGroupParent Interface
        public ThingFilter allowances;

        public CompMountable MountableComp => this.GetComp<CompMountable>();

        public CompRefuelable RefuelableComp => this.GetComp<CompRefuelable>();

        public CompExplosive ExplosiveComp => this.GetComp<CompExplosive>();

        public CompBreakdownable BreakdownableComp => this.GetComp<CompBreakdownable>();

        public CompAxles AxlesComp => this.GetComp<CompAxles>();

        public CompVehicle VehicleComp => this.GetComp<CompVehicle>();

        public int MaxItem => this.MountableComp.IsMounted && this.MountableComp.Driver.RaceProps.Animal
                                  ? Mathf.CeilToInt(this.MountableComp.Driver.BodySize * this.DefaultMaxItem)
                                  : this.DefaultMaxItem;

        public int MaxStack => this.MaxItem * 100;

        #endregion

        #region Setup Work

        public Vehicle_Cart()
        {
            this.stunner = new StunHandler(this);
        }

        static Vehicle_Cart()
        {
        }

        Thing IAttackTargetSearcher.Thing
        {
            get
            {
                return this;
            }
        }
        // public static ListerVehicles listerVehicles = new ListerVehicles();
#if Headlights
        HeadLights flooder;
#endif

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
            ToolsForHaulUtility.Cart.Add(this);

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
            if (ToolsForHaulUtility.Cart.Contains(this)) ToolsForHaulUtility.Cart.Remove(this);

            if (this.MountableComp.IsMounted) if (GameComponentToolsForHaul.CurrentVehicle.ContainsKey(this.MountableComp.Driver)) GameComponentToolsForHaul.CurrentVehicle.Remove(this.MountableComp.Driver);

            if (this.MountableComp.SustainerAmbient != null) this.MountableComp.SustainerAmbient.End();
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
            Scribe_Deep.Look<ThingOwner<Thing>>(ref this.innerContainer, "innerContainer", new object[] { this });
            Scribe_Deep.Look(ref this.allowances, "allowances");
            Scribe_Values.Look(ref this.VehicleComp.tankLeaking, "tankLeaking");
            Scribe_Values.Look(ref this._tankHitPos, "tankHitPos");

            Scribe_Deep.Look(ref this.stunner, "stunner", new object[] { this });
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (this.Faction != Faction.OfPlayer) yield break;

            foreach (Gizmo baseGizmo in base.GetGizmos()) yield return baseGizmo;

            if (this.ExplosiveComp != null)
            {
                Command_Action command_Action = new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate"),
                    defaultDesc = "CommandDetonateDesc".Translate(),
                    action = Command_Detonate
                };
                if (this.ExplosiveComp.wickStarted)
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
            this.ExplosiveComp.StartWick();
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
                    Job jobNew = ToolsForHaulUtility.DismountInBase(this.MountableComp.Driver, this);

                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_Dismount = () =>
                {
                    if (!myPawn.Position.InBounds(map))
                    {
                        this.MountableComp.DismountAt(myPawn.Position);
                        return;
                    }

                    this.MountableComp.DismountAt(
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
            if (!this.MountableComp.IsMounted)
            {
                foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart) if (cart.MountableComp.Driver == myPawn) alreadyMounted = true;

                if (myPawn.Faction == Faction.OfPlayer && (myPawn.RaceProps.IsMechanoid || myPawn.RaceProps.Humanlike)
                    && !alreadyMounted && !this.IsForbidden(myPawn.Faction))
                {
                    yield return new FloatMenuOption("Mount".Translate(this.LabelShort), action_Mount);
                }

                yield return new FloatMenuOption("Deconstruct".Translate(this.LabelShort), action_Deconstruct);
            }
            else if (myPawn == this.MountableComp.Driver)
            {
                yield return new FloatMenuOption("Dismount".Translate(this.LabelShort), action_Dismount);
            }

            yield return new FloatMenuOption("DismountInBase".Translate(this.LabelShort), action_DismountInBase);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.Deconstruct) mode = DestroyMode.KillFinalize;
            //  else if (this.explosiveComp != null && this.explosiveComp.wickStarted)
            //  {
            //      this.storage.ClearAndDestroyContents();
            //  }
            //
            //  this.storage.TryDropAll(this.Position, Map, ThingPlaceMode.Near);

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
                this.VehicleComp.currentDriverSpeed = this.VehicleComp.VehicleSpeed;
                this.instantiated = true;
            }

            base.Tick();

#if Headlights
            {
                if (Find.GlowGrid.GameGlowAt(Position - Rotation.FacingCell - Rotation.FacingCell) < 0.4f)
                {
                    // TODO Add headlights to xml & move the flooder initialization to mountableComp
                    if (MountableComp.Driver != null && !VehicleComp.AnimalsCanDrive() && flooder == null)
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

            if (this.MountableComp.IsMounted)
            {
                if (this.RefuelableComp != null)
                {
                    if (this.MountableComp.Driver.Faction != Faction.OfPlayer)
                        if (!this.fueledByAI)
                        {
                            if (this.RefuelableComp.FuelPercentOfMax < 0.550000011920929)
                                this.RefuelableComp.Refuel(
                                    ThingMaker.MakeThing(
                                        this.RefuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                            else this.fueledByAI = true;
                        }
                }

                if (this.MountableComp.Driver.pather.Moving)
                {
                    // || mountableComp.Driver.drafter.pawn.pather.Moving)
                    if (!this.MountableComp.Driver.stances.FullBodyBusy && this.AxlesComp.HasAxles())
                    {
                        this.AxlesComp.wheelRotation += this.VehicleComp.currentDriverSpeed / 10f; // 3f
                        this.AxlesComp.tick_time += 0.01f * this.VehicleComp.currentDriverSpeed / 5f;
                    }
                }

                if (Find.TickManager.TicksGame - this.tickCheck >= this.tickCooldown)
                {
                    if (this.MountableComp.Driver.pather.Moving)
                    {
                        if (!this.MountableComp.Driver.stances.FullBodyBusy)
                        {
                            if (this.RefuelableComp != null) this.RefuelableComp.Notify_UsedThisTick();
                            this.damagetick -= 1;

                            if (this.AxlesComp.HasAxles())
                                this.VehicleComp.currentDriverSpeed =
                                    ToolsForHaulUtility.GetMoveSpeed(this.MountableComp.Driver);
                        }

                        if (this.BreakdownableComp != null && this.BreakdownableComp.BrokenDown
                            || this.RefuelableComp != null && !this.RefuelableComp.HasFuel) this.VehicleComp.VehicleSpeed = 0.75f;
                        else this.VehicleComp.VehicleSpeed = this.DesiredSpeed;
                        this.tickCheck = Find.TickManager.TicksGame;
                    }

                    if (this.Position.InNoBuildEdgeArea(Map) && this.VehicleComp.despawnAtEdge && this.Spawned
                        && (this.MountableComp.Driver.Faction != Faction.OfPlayer
                            || this.MountableComp.Driver.MentalState.def == MentalStateDefOf.PanicFlee)) this.DeSpawn();
                }
            }

            // if (Find.TickManager.TicksGame >= damagetick)
            // {
            // TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1, null, null, null));
            // damagetick = Find.TickManager.TicksGame + 3600;
            // }
            if (this.VehicleComp.tankLeaking)
            {
                if (Find.TickManager.TicksGame > this._tankSpillTick)
                {
                    if (this.RefuelableComp.FuelPercentOfMax > this._tankHitPos)
                    {
                        this.RefuelableComp.ConsumeFuel(0.15f);

                        FilthMaker.MakeFilth(this.Position, Map, this.fuelDefName, this.LabelCap);
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
                if (!this.Spawned || !this.MountableComp.IsMounted || !this.instantiated)
                {
                    return base.DrawPos;
                }

                float num = this.MountableComp.Driver.Drawer.renderer.graphics.nakedGraphic.drawSize.x - 1f;
                num *= this.MountableComp.Driver.Rotation.AsInt % 2 == 1 ? 0.5f : 0.25f;
                Vector3 vector = new Vector3(0f, 0f, -num);
                return this.MountableComp.Position + vector.RotatedBy(this.MountableComp.Driver.Rotation.AsAngle);
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip)
        {
            if (!this.Spawned)
            {
                base.DrawAt(drawLoc);
                return;
            }

            if (this.VehicleComp.ShowsStorage())
            {
                if (this.innerContainer.Any()
                    || (this.MountableComp.IsMounted && this.MountableComp.Driver.RaceProps.packAnimal
                        && this.MountableComp.Driver.RaceProps.Animal))
                {
                    Vector3 mountThingLoc = drawLoc;
                    if (this.Rotation.AsInt % 2 == 1)
                    {
                        mountThingLoc.y = Altitudes.AltitudeFor(AltitudeLayer.LayingPawn); // horizontal
                        mountThingLoc.z += 0.1f;
                    }
                    else mountThingLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.07f; // vertical

                    Vector3 mountThingOffset =
                        (-0.3f * this.def.interactionCellOffset.ToVector3()).RotatedBy(this.Rotation.AsAngle);
                    if (this.MountableComp.IsMounted)
                        mountThingOffset =
                            (-0.3f * this.def.interactionCellOffset.ToVector3()).RotatedBy(this.Rotation.AsAngle);

                    if (this.MountableComp.Driver.RaceProps.packAnimal && this.MountableComp.Driver.RaceProps.Animal)
                    {
                        if (this.MountableComp.IsMounted && this.MountableComp.Driver.inventory.innerContainer.Count > 0)
                            foreach (Thing mountThing in this.MountableComp.Driver.inventory.innerContainer)
                            {
                                mountThing.Rotation = this.Rotation;
                                mountThing.DrawAt(mountThingLoc + mountThingOffset);
                            }
                    }
                    else if (this.innerContainer.Count > 0)
                    {
                        foreach (Thing mountThing in this.innerContainer)
                        {
                            mountThing.Rotation = this.Rotation;
                            mountThing.DrawAt(mountThingLoc + mountThingOffset);
                        }
                    }
                }
            }

            base.DrawAt(drawLoc);
        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());

            string currentDriverString;
            if (this.MountableComp.Driver != null) currentDriverString = this.MountableComp.Driver.LabelCap;
            else currentDriverString = "NoDriver".Translate();

            stringBuilder.AppendLine("Driver".Translate() + ": " + currentDriverString);
            if (this.VehicleComp.tankLeaking) stringBuilder.AppendLine("TankLeaking".Translate());

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

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }
    }
}
#endif