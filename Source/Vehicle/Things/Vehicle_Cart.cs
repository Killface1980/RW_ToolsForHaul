using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ppumkin.LEDTechnology.Managers;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using Random = UnityEngine.Random;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Vehicle_Cart : Building, IThingContainerOwner, IAttackTarget
    {
        #region Variables
        // ==================================
        public int DefaultMaxItem { get { return (int)this.GetStatValue(vehicleMaxItem); } }
        public int MaxItemPerBodySize { get { return (int)this.GetStatValue(vehicleMaxItem); } }
        public bool instantiated;

        public bool ThreatDisabled()
        {
            return !mountableComp.IsMounted;
        }

        // TODO make vehicles break down & get repaired like buildings
        public override bool ClaimableBy(Faction faction)
        {
            if (!mountableComp.IsMounted)
            {
                return true;
            }
            return false;
        }

        public float currentDriverSpeed;

        public float DesiredSpeed
        {
            get
            {
                return this.GetStatValue(vehicleSpeed);
            }
        }
        public bool IsCurrentlyMotorized()
        {
            return (refuelableComp != null && refuelableComp.HasFuel) || compVehicles.MotorizedWithoutFuel();
        }
        public float VehicleSpeed;
        public bool despawnAtEdge;

        public bool fueledByAI;
        private bool fuelSpilled;
        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;
        private static readonly StatDef vehicleSpeed;
        private float _puddleHitpointCounter;

        //Graphic data
        private Graphic_Single graphic_Wheel_Single;
        private Graphic_Multi graphic_FullStorage;

        //Body and part location
        private Vector3 wheelLoc;
        private Vector3 bodyLoc;
        private float wheelRotation;


        //mount and storage data
        public CompMountable mountableComp;
        public ThingContainer storage;
        public ThingContainer GetContainer() { return storage; }
        public IntVec3 GetPosition() { return Position; }

        //slotGroupParent Interface
        public ThingFilter allowances;

        public int MaxItem
        {
            get
            {
                return mountableComp.IsMounted && mountableComp.Driver.RaceProps.Animal ?
                    Mathf.CeilToInt(mountableComp.Driver.BodySize * MaxItemPerBodySize) : DefaultMaxItem;
            }
        }
        public int MaxStack { get { return MaxItem * 100; } }

        // ARB
        private static readonly StatDef vehicleMaxItem = DefDatabase<StatDef>.GetNamed("VehicleMaxItem");

        private CompRefuelable refuelableComp
        {
            get
            {
                return GetComp<CompRefuelable>();
            }
        }

        private CompExplosive explosiveComp
        {
            get
            {
                return GetComp<CompExplosive>();
            }
        }

        private CompBreakdownable breakdownableComp
        {
            get
            {
                return GetComp<CompBreakdownable>();
            }
        }

        private CompAxles compAxles
        {
            get
            {
                return GetComp<CompAxles>();
            }
        }

        private CompVehicles compVehicles
        {
            get
            {
                return GetComp<CompVehicles>();
            }
        }


        #endregion

        #region Setup Work


        public Vehicle_Cart()
        {
            // current spin degree
            wheelRotation = 0;
            //Inventory Initialize. It should be moved in constructor
            storage = new ThingContainer(this);
            allowances = new ThingFilter();
            allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
            allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
        }

        static Vehicle_Cart()
        {
            vehicleSpeed = DefDatabase<StatDef>.GetNamed("VehicleSpeed");
            vehicleMaxItem = DefDatabase<StatDef>.GetNamed("VehicleMaxItem");
        }

        private Sustainer sustainerAmbient;

        //    public static ListerVehicles listerVehicles = new ListerVehicles();

        HeadLights flooder;

        public override void SpawnSetup()
        {

            base.SpawnSetup();

            mountableComp = GetComp<CompMountable>();

            if (allowances == null)
            {
                allowances = new ThingFilter();
                allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
                allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
            }

            LongEventHandler.ExecuteWhenFinished(UpdateGraphics);
        }

        public override void DeSpawn()
        {
            base.DeSpawn();
            //    ListerVehiclesRepairable.Notify_VehicleDeSpawned(this);

        }

        public bool repairable = true;

        private ThingDef VehicleDef
        {
            get
            {
                return def;
            }
        }

        private void UpdateGraphics()
        {
            if (compAxles.HasAxles())
            {
                string text = "Things/Pawn/" + VehicleDef.defName + "/Wheel";
                graphic_Wheel_Single = new Graphic_Single();
                graphic_Wheel_Single = GraphicDatabase.Get<Graphic_Single>(text, def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Single;
            }
            if (compVehicles.ShowsStorage())
            {
                string text2 = string.Concat("Things/Pawn/", VehicleDef.defName, "/", VehicleDef.defName, "_FullStorage");
                graphic_FullStorage = new Graphic_Multi();
                graphic_FullStorage = GraphicDatabase.Get<Graphic_Multi>(text2, def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep(ref storage, "storage");
            Scribe_Deep.LookDeep(ref allowances, "allowances");
            Scribe_Values.LookValue(ref tankLeaking, "tankLeaking");
            Scribe_Values.LookValue(ref tankHitPos, "tankHitPos");
            Scribe_Values.LookValue(ref despawnAtEdge, "despawnAtEdge");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {

            foreach (Gizmo baseGizmo in base.GetGizmos())
                yield return baseGizmo;

            if (Faction != Faction.OfPlayer)
                yield break;

            if (GetComp<CompExplosive>() != null)
            {

                Command_Action command_Action = new Command_Action();
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate", true);
                command_Action.defaultDesc = "CommandDetonateDesc".Translate();
                command_Action.action = Command_Detonate;
                if (GetComp<CompExplosive>().wickStarted)
                {
                    command_Action.Disable(null);
                }
                command_Action.defaultLabel = "CommandDetonateLabel".Translate();
                yield return command_Action;
            }
            // yield return new Command_Action
            // {
            //     defaultLabel = "CommandBedSetOwnerLabel".Translate(),
            //     icon = ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", true),
            //     defaultDesc = "CommandGraveAssignColonistDesc".Translate(),
            //     action = delegate
            //     {
            //         Find.WindowStack.Add(new Dialog_AssignBuildingOwner(this));
            //     },
            //     hotKey = KeyBindingDefOf.Misc3
            // };
        }

        private void Command_Detonate()
        {
            GetComp<CompExplosive>().StartWick();
        }



        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            Action action_Dismount;
            Action action_Mount;
            Action action_MakeMount;
            Action action_Repair;
            Action action_Refuel;
            Action action_Deconstruct;

            // do nothing if not of colony
            if (Faction != Faction.OfPlayer)
                yield break;

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;

            action_Mount = () =>
            {
                Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Mount"));
                Find.Reservations.ReleaseAllForTarget(this);
                jobNew.targetA = this;
                myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
            };

            action_Dismount = () =>
            {
                mountableComp.Dismount();

                if (!myPawn.Position.InBounds())
                {
                    mountableComp.DismountAt(myPawn.Position);
                    return;
                }

                mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
                myPawn.Position = myPawn.Position.RandomAdjacentCell8Way();
                // mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
            };

            action_MakeMount = () =>
            {
                Pawn worker = null;
                Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("MakeMount"));
                Find.Reservations.ReleaseAllForTarget(this);
                jobNew.maxNumToCarry = 1;
                jobNew.targetA = this;
                jobNew.targetB = myPawn;
                foreach (Pawn colonyPawn in Find.MapPawns.FreeColonistsSpawned)
                    if (colonyPawn.CurJob.def != jobNew.def && (worker == null || (worker.Position - myPawn.Position).LengthHorizontal > (colonyPawn.Position - myPawn.Position).LengthHorizontal))
                        worker = colonyPawn;
                if (worker == null)
                {
                    Messages.Message("NoWorkForMakeMount".Translate(), MessageSound.RejectInput);
                }
                else worker.jobs.StartJob(jobNew, JobCondition.InterruptForced);
            };



            action_Deconstruct = () =>
            {
                Find.Reservations.ReleaseAllForTarget(this);
                Find.Reservations.Reserve(myPawn, this);
                Find.DesignationManager.AddDesignation(new Designation(this, DesignationDefOf.Deconstruct));
                Job job = new Job(JobDefOf.Deconstruct, this);
                myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
            };
            if (!mountableComp.IsMounted)
            {
                bool alreadyMounted = false;
                foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart())
                    if (cart.mountableComp.Driver == myPawn)
                        alreadyMounted = true;

                if (myPawn.Faction == Faction.OfPlayer && (myPawn.RaceProps.IsMechanoid || myPawn.RaceProps.Humanlike) && !alreadyMounted)
                {
                    yield return new FloatMenuOption("Mount".Translate(LabelShort), action_Mount);
                }

                yield return new FloatMenuOption("Deconstruct".Translate(LabelShort), action_Deconstruct);

            }
            else if (myPawn == mountableComp.Driver)
                yield return new FloatMenuOption("Dismount".Translate(LabelShort), action_Dismount);


        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.Deconstruct)
                mode = DestroyMode.Kill;
            else if (explosiveComp != null)
                storage.ClearAndDestroyContents();

            storage.TryDropAll(Position, ThingPlaceMode.Near);

            base.Destroy(mode);
        }
        private ThingDef fuelDefName = ThingDef.Named("Puddle_BioDiesel_Fuel");

        public bool tankLeaking;
        private int tankHitCount;

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);

            // TODO make vehicles break down & get repaired like buildings
            //      ListerVehiclesRepairable.Notify_VehicleTookDamage(this);

            if (dinfo.Def == DamageDefOf.Repair && tankLeaking)
            {
                tankLeaking = false;
                tankHitPos = 1f;
                //if (breakdownableComp.BrokenDown)
                //    breakdownableComp.Notify_Repaired();
                return;
            }

            if (dinfo.Def == DamageDefOf.Deterioration || dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Repair)
            {
                return;
            }
            if (HitPoints < compVehicles.FuelCatchesFireHitPointsPercent() * MaxHitPoints && refuelableComp != null && refuelableComp.HasFuel)
            {
                if (!fuelSpilled)
                {
                    refuelableComp.ConsumeFuel(1f);

                    FilthMaker.MakeFilth(Position, fuelDefName, LabelCap, 6);

                    if (Random.value >= 0.5f)
                    {
                        FireUtility.TryStartFireIn(Position, 0.1f);
                    }
                    fuelSpilled = true;
                    return;
                }
                if (Random.value >= 0.5f)
                {

                    FireUtility.TryStartFireIn(Position, 0.1f);
                }
            }
            if (Random.value <= 0.15f)
            {
                tankLeaking = true;
                tankHitCount += 1;
                tankHitPos = Math.Min(tankHitPos, Rand.Value);

                int splash = (int)(refuelableComp.FuelPercent - tankHitPos * 20);

                FilthMaker.MakeFilth(Position, fuelDefName, LabelCap, splash);
            }
        }




        #endregion

        #region Ticker
        // ==================================

        /// <summary>
        /// 
        /// </summary>
        public override void Tick()
        {
            if (!instantiated)
            {
                using (IEnumerator<Thing> enumerator = GetContainer().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        enumerator.Current.holder.owner = this;
                    }
                }
                currentDriverSpeed = VehicleSpeed;
                instantiated = true;
            }
            base.Tick();

            #region Headlights

            if (Find.GlowGrid.GameGlowAt(Position - Rotation.FacingCell) < 0.4f)
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
            else
            {
                if (mountableComp.Driver == null && flooder != null || flooder != null)
                {
                    CustomGlowFloodManager.DeRegisterGlower(flooder);
                    CustomGlowFloodManager.RefreshGlowFlooders();
                    flooder = null;
                }

            }


            #endregion


            if (!fueledByAI)
            {
                if (mountableComp.IsMounted && mountableComp.Driver.Faction != Faction.OfPlayer && refuelableComp != null && refuelableComp.FuelPercent < 0.550000011920929)
                    refuelableComp.Refuel(ThingMaker.MakeThing(refuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                else if (mountableComp.IsMounted && mountableComp.Driver.Faction != Faction.OfPlayer && refuelableComp != null && refuelableComp.FuelPercent >= 0.550000011920929)
                    fueledByAI = true;
            }
            //if (mountableComp.IsMounted && mountableComp.Driver.pather.Moving && !mountableComp.Driver.stances.FullBodyBusy)
            //{    // rotate rotor by parent's move speed value
            //    int degree = Mathf.FloorToInt(mountableComp.Driver.GetStatValue(StatDefOf.MoveSpeed) / (GenDate.SecondsToTicks(1) * wheelRadius));
            //    RotateWheelByDegree(degree);
            //}
            if (mountableComp.IsMounted && mountableComp.Driver.pather.Moving && !mountableComp.Driver.stances.FullBodyBusy && compAxles.HasAxles())
                wheelRotation += currentDriverSpeed / 3f;

            if (Find.TickManager.TicksGame - tickCheck >= tickCooldown)
            {
#if CR
                if (this.mountableComp.IsMounted && ((Pawn_PathFollower)this.mountableComp.Driver.pather).Moving && (!((Pawn_StanceTracker)this.mountableComp.Driver.stances).FullBodyBusy && this.compAxles.HasAxles()))
                    this.currentDriverSpeed = Utility.GetMoveSpeed(this.mountableComp.Driver);
#else
                if (mountableComp.IsMounted && mountableComp.Driver.pather.Moving && !mountableComp.Driver.stances.FullBodyBusy && compAxles.HasAxles())
                    currentDriverSpeed = ToolsForHaulUtility.GetMoveSpeed(mountableComp.Driver);
#endif
                if (refuelableComp != null && mountableComp.IsMounted && mountableComp.Driver.pather.Moving && !mountableComp.Driver.stances.FullBodyBusy)
                    refuelableComp.Notify_UsedThisTick();
                if (GetPosition().InNoBuildEdgeArea() && despawnAtEdge && Spawned && (mountableComp.Driver.Faction != Faction.OfPlayer || mountableComp.Driver.MentalState.def == MentalStateDefOf.PanicFlee))
                    DeSpawn();

                if (breakdownableComp != null && breakdownableComp.BrokenDown ||
                    refuelableComp != null && !refuelableComp.HasFuel)
                    VehicleSpeed = 0.75f;
                else VehicleSpeed = DesiredSpeed;
                tickCheck = Find.TickManager.TicksGame;
            }

            if (tankLeaking && Find.TickManager.TicksGame > tankSpillTick)
            {
                if (refuelableComp.FuelPercent > tankHitPos)
                {
                    refuelableComp.ConsumeFuel(0.35f);

                    FilthMaker.MakeFilth(Position, fuelDefName, LabelCap, 3);
                    tankSpillTick = Find.TickManager.TicksGame + 80;
                }
                // else
                // {
                //     if (!breakdownableComp.BrokenDown)
                //         breakdownableComp.DoBreakdown();
                // }
            }

        }

        private float tankHitPos = 1f;
        private int tankSpillTick = -5000;

        #endregion

        #region Graphics / Inspections
        // ==================================

        //private void UpdateGraphics()
        //{
        //}

        public override Graphic Graphic
        {
            get
            {
                if (compAxles.HasAxles() && graphic_Wheel_Single == null)
                {
                    UpdateGraphics();
                }
                if (graphic_FullStorage == null)
                {
                    return base.Graphic;
                }
                if (storage.Count <= 0)
                {
                    return base.Graphic;
                }
                return graphic_FullStorage;
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (!Spawned || !mountableComp.IsMounted || !instantiated)
                {
                    return base.DrawPos;
                }
                float num = mountableComp.Driver.Drawer.renderer.graphics.nakedGraphic.drawSize.x - 1f;
                num *= mountableComp.Driver.Rotation.AsInt % 2 == 1 ? 0.5f : 0.25f;
                Vector3 vector = new Vector3(0f, 0f, -num);
                return mountableComp.Position + vector.RotatedBy(mountableComp.Driver.Rotation.AsAngle);
            }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            if (!Spawned)
            {
                base.DrawAt(drawLoc);
                return;
            }

            wheelLoc = drawLoc;
            bodyLoc = drawLoc;
            if (compAxles.HasAxles() && Rotation.AsInt % 2 == 1)
            {
                wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.04f;
                bodyLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.03f;

                Vector2 drawSize = def.graphic.drawSize;
                int num = Rotation == Rot4.West ? -1 : 1;
                Vector3 s = new Vector3(1f * drawSize.x, 1f, 1f * drawSize.y);
                Quaternion asQuat = Rotation.AsQuat;
                float x = 1f * Mathf.Sin(num * (wheelRotation * 0.05f) % (2 * Mathf.PI));
                float z = 1f * Mathf.Cos(num * (wheelRotation * 0.05f) % (2 * Mathf.PI));

                asQuat.SetLookRotation(new Vector3(x, 0f, z), Vector3.up);
                List<Vector3> list;
                if (compAxles.GetAxleLocations(drawSize, num, out list))
                {
                    foreach (Vector3 current in list)
                    {
                        Matrix4x4 matrix = default(Matrix4x4);
                        matrix.SetTRS(wheelLoc + current, asQuat, s);
                        Graphics.DrawMesh(MeshPool.plane10, matrix, graphic_Wheel_Single.MatAt(Rotation), 0);
                    }
                }
            }
            base.DrawAt(bodyLoc);
        }
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.AppendLine("InStorage".Translate());
            foreach (Thing thing in storage)
                stringBuilder.Append(thing.LabelCap.Translate() + ", ");
            stringBuilder.Remove(stringBuilder.Length - 3, 1);
            return stringBuilder.ToString();
        }
        #endregion






        public IEnumerable<Pawn> AssigningCandidates
        {
            get
            {
                return Find.MapPawns.FreeColonists;
            }
        }



        public int MaxAssignedPawnsCount
        {
            get
            {
                return 2;
            }
        }
    }
}