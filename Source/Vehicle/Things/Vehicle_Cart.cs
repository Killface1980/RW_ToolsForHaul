using RimWorld.Planet;

namespace ToolsForHaul
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

#if Headlights
using ppumkin.LEDTechnology.Managers;
#endif
    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.JobDefs;
    using ToolsForHaul.StatDefs;
    using ToolsForHaul.Utilities;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    [StaticConstructorOnStartup]
    public class Vehicle_Cart :  Building, IThingContainerOwner, IAttackTarget
    {
        #region Variables

        // ==================================
        private bool instantiated;

        private bool fueledByAI;

        private int tickCheck = Find.TickManager.TicksGame;

        private readonly int tickCooldown = 60;

        public CompMountable MountableComp => this.GetComp<CompMountable>();

        private CompRefuelable RefuelableComp => this.GetComp<CompRefuelable>();

        private CompExplosive ExplosiveComp => this.GetComp<CompExplosive>();

        private CompBreakdownable BreakdownableComp => this.GetComp<CompBreakdownable>();

        private CompAxles AxlesComp => this.GetComp<CompAxles>();

        public CompVehicle VehicleComp => this.GetComp<CompVehicle>();

        public int MaxItem => this.MountableComp.IsMounted && this.MountableComp.Driver.RaceProps.Animal
                                  ? Mathf.CeilToInt(this.MountableComp.Driver.BodySize * this.DefaultMaxItem)
                                  : this.DefaultMaxItem;

        public int MaxStack => this.MaxItem * 100;

        // mount and storage data
        private ThingContainer storage;

        private int DefaultMaxItem => (int)this.GetStatValue(HaulStatDefOf.VehicleMaxItem);

        public float DesiredSpeed => this.GetStatValue(HaulStatDefOf.VehicleSpeed);

        public int GetMaxStackCount => this.MaxItem * 100;

        public bool ThreatDisabled()
        {
            if (this.GetComp<CompExplosive>().wickStarted) return true;
            return !this.MountableComp.IsMounted;
        }

        // TODO make vehicles break down & get repaired like buildings
        public override bool ClaimableBy(Faction faction)
        {
            if (!this.MountableComp.IsMounted)
            {
                return true;
            }

            return false;
        }

        public bool IsCurrentlyMotorized()
        {
            return (this.RefuelableComp != null && this.RefuelableComp.HasFuel)
                   || this.VehicleComp.MotorizedWithoutFuel();
        }

        public Map GetMap()
        {
            return base.MapHeld;
        }

        public ThingContainer GetInnerContainer()
        {
            return this.Storage;
        }

        public IntVec3 GetPosition()
        {
            return this.Position;
        }

        // slotGroupParent Interface
        public ThingFilter allowances;

        #endregion

        #region Setup Work

        public Vehicle_Cart()
        {
            // Inventory Initialize. It should be moved in constructor
            this.Storage = new ThingContainer(this);
            this.allowances = new ThingFilter();
            this.allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
            this.allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
        }

        static Vehicle_Cart()
        {
        }

        // public static ListerVehicles listerVehicles = new ListerVehicles();
#if Headlights
        HeadLights flooder;
#endif

        public override void SpawnSetup(Map map)
        {
            base.SpawnSetup(map);

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
            if (ToolsForHaulUtility.Cart.Contains(this))
            {
                ToolsForHaulUtility.Cart.Remove(this);
            }

            if (this.MountableComp.SustainerAmbient != null)
            {
                this.MountableComp.SustainerAmbient.End();
            }

            if (this.MountableComp.IsMounted)
            {
                if (MapComponent_ToolsForHaul.currentVehicle.ContainsKey(this.MountableComp.Driver))
                {
                    MapComponent_ToolsForHaul.currentVehicle.Remove(this.MountableComp.Driver);
                }
            }

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
            Scribe_Values.LookValue(ref this.VehicleComp.tankLeaking, "tankLeaking");
            Scribe_Values.LookValue(ref this._tankHitPos, "tankHitPos");
            Scribe_Values.LookValue(ref this.MountableComp.lastDrawAsAngle, "lastDrawAsAngle");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo baseGizmo in base.GetGizmos()) yield return baseGizmo;

            if (this.Faction != Faction.OfPlayer) yield break;

            if (this.ExplosiveComp != null)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate");
                command_Action.defaultDesc = "CommandDetonateDesc".Translate();
                command_Action.action = Command_Detonate;
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
            Action action_Dismount;
            Action action_Mount;
            Action action_DismountInBase;
            Action action_MakeMount;
            Action action_Repair;
            Action action_Refuel;
            Action action_Deconstruct;

            // do nothing if not of colony
            if (this.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
            {
                yield return fmo;
            }

            action_Mount = () =>
                {
                    Job jobNew = new Job(HaulJobDefOf.Mount);
                    myPawn.Map.reservationManager.ReleaseAllForTarget(this);
                    jobNew.targetA = this;
                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            action_DismountInBase = () =>
                {
                    Job jobNew = ToolsForHaulUtility.DismountInBase(
                        this.MountableComp.Driver,
                        MapComponent_ToolsForHaul.currentVehicle[this.MountableComp.Driver]);

                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            action_Dismount = () =>
                {
                    if (!myPawn.Position.InBounds(Map))
                    {
                        this.MountableComp.DismountAt(myPawn.Position);
                        return;
                    }

                    this.MountableComp.DismountAt(
                        myPawn.Position - this.def.interactionCellOffset.RotatedBy(myPawn.Rotation));
                    myPawn.Position = myPawn.Position.RandomAdjacentCell8Way();

                    // mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
                };

            action_MakeMount = () =>
                {
                    Pawn worker = null;
                    Job jobNew = new Job(HaulJobDefOf.MakeMount);
                    myPawn.Map.reservationManager.ReleaseAllForTarget(this);
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

            action_Deconstruct = () =>
                {
                    myPawn.Map.reservationManager.ReleaseAllForTarget(this);
                    myPawn.Map.reservationManager.Reserve(myPawn, this);
                    myPawn.Map.designationManager.AddDesignation(new Designation(this, DesignationDefOf.Deconstruct));
                    Job job = new Job(JobDefOf.Deconstruct, this);
                    myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                };
            bool alreadyMounted = false;
            if (!this.MountableComp.IsMounted)
            {
                foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart)
                {
                    if (cart.MountableComp.Driver == myPawn)
                    {
                        alreadyMounted = true;
                    }
                }

                foreach (Vehicle_Turret cart in ToolsForHaulUtility.CartTurret)
                {
                    if (cart.mountableComp.Driver == myPawn)
                    {
                        alreadyMounted = true;
                    }
                }

                if (myPawn.Faction == Faction.OfPlayer)
                {
                    if (myPawn.RaceProps.IsMechanoid || myPawn.RaceProps.Humanlike)
                    {
                        if (!alreadyMounted)
                        {
                            if (!this.IsForbidden(myPawn.Faction))
                            {
                                yield return new FloatMenuOption("Mount".Translate(this.LabelShort), action_Mount);
                            }
                        }
                    }
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
            if (mode == DestroyMode.Deconstruct) mode = DestroyMode.Kill;
            else if (this.ExplosiveComp != null && this.ExplosiveComp.wickStarted)
            {
                this.Storage.ClearAndDestroyContents();
            }

            this.Storage.TryDropAll(this.Position, this.Map, ThingPlaceMode.Near);

            base.Destroy(mode);
        }

        public ThingDef fuelDefName = ThingDef.Named("FilthFuel");

        #endregion

        #region Ticker

        public override void Tick()
        {
            if (!this.instantiated)
            {
                foreach (Thing thing in this.GetInnerContainer())
                {
                    thing.holdingContainer.owner = this;
                }

                this.VehicleComp.currentDriverSpeed = this.VehicleComp.VehicleSpeed;
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

            if (this.MountableComp.IsMounted)
            {
                if (this.RefuelableComp != null)
                {
                    if (this.MountableComp.Driver.Faction != Faction.OfPlayer)
                        if (!this.FueledByAI)
                        {
                            if (this.RefuelableComp.FuelPercentOfMax < 0.550000011920929)
                                this.RefuelableComp.Refuel(
                                    ThingMaker.MakeThing(
                                        this.RefuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                            else this.FueledByAI = true;
                        }
                }

                if (this.MountableComp.Driver.pather.Moving)
                {
                    // || mountableComp.Driver.drafter.pawn.pather.Moving)
                    if (!this.MountableComp.Driver.stances.FullBodyBusy && this.AxlesComp.HasAxles())
                    {
                        this.AxlesComp.wheelRotation += this.VehicleComp.currentDriverSpeed / 3f;
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
                            || this.RefuelableComp != null && !this.RefuelableComp.HasFuel)
                        {
                            this.VehicleComp.VehicleSpeed = 0.75f;
                        }
                        else
                        {
                            this.VehicleComp.VehicleSpeed = this.DesiredSpeed;
                        }
                        this.tickCheck = Find.TickManager.TicksGame;
                    }

                    if (this.Position.InNoBuildEdgeArea(this.Map) && this.VehicleComp.despawnAtEdge && this.Spawned
                        && (this.MountableComp.Driver.Faction != Faction.OfPlayer
                            || this.MountableComp.Driver.MentalState.def == MentalStateDefOf.PanicFlee))
                    {
                        this.DeSpawn();
                    }
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

                        FilthMaker.MakeFilth(this.Position, this.Map, this.fuelDefName, this.LabelCap);
                        this._tankSpillTick = Find.TickManager.TicksGame + 15;
                    }
                }
            }
        }

        private float _tankHitPos = 1f;

        private int damagetick = -5000;

        private int _tankSpillTick = -5000;

        #endregion

        #region Graphics / Inspections

        // ==================================

        // private void UpdateGraphics()
        // {
        // }
        public override Vector3 DrawPos
        {
            get
            {
                if (!this.Spawned || !this.MountableComp.IsMounted || !this.instantiated || this.VehicleComp == null)
                {
                    return base.DrawPos;
                }

                float num = this.MountableComp.Driver.BodySize - 1f;

                // float num = mountableComp.Driver.Drawer.renderer.graphics.nakedGraphic.drawSize.x - 1f;
                num *= this.MountableComp.Driver.Rotation.AsInt % 2 == 1 ? 0.5f : 0.25f;
                Vector3 vector = new Vector3(0f, 0f, -num);
                return this.MountableComp.Position + vector.RotatedBy(this.MountableComp.Driver.Rotation.AsAngle);
            }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            if (!this.Spawned)
            {
                base.DrawAt(drawLoc);
                return;
            }

            if (this.VehicleComp.ShowsStorage())
            {
                if (this.Storage.Any()
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
                    else if (this.Storage.Count > 0)
                    {
                        foreach (Thing mountThing in this.Storage)
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
            string text = this.Storage.ContentsString;
            stringBuilder.AppendLine(string.Concat(new object[] { "InStorage".Translate(), ": ", text }));
            return stringBuilder.ToString();
        }

        #endregion

        public IEnumerable<Pawn> AssigningCandidates => PawnsFinder.AllMaps_FreeColonists;

        public int MaxAssignedPawnsCount => 2;

        public ThingContainer Storage
        {
            get
            {
                return this.storage;
            }

            set
            {
                this.storage = value;
            }
        }

        public bool FueledByAI
        {
            get
            {
                return this.fueledByAI;
            }

            set
            {
                this.fueledByAI = value;
            }
        }
    }
}