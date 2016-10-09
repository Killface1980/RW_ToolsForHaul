using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Random = UnityEngine.Random;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Vehicle_Cart : ThingWithComps, IThingContainerOwner, IAttackTarget
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
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo baseGizmo in base.GetGizmos())
                yield return baseGizmo;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            Action action_Order;

            // do nothing if not of colony
            if (myPawn.Faction != Faction.OfPlayer)
                yield break;

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;

            action_Order = () =>
            {
                Find.Reservations.ReleaseAllForTarget(this);
                Find.Reservations.Reserve(myPawn, this);
                Find.DesignationManager.AddDesignation(new Designation(this, DesignationDefOf.Deconstruct));
                Job job = new Job(JobDefOf.Deconstruct, this);
                myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
            };

            yield return new FloatMenuOption("Deconstruct".Translate(LabelShort), action_Order); // right? Was this.LabelShort
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

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (dinfo.Def == DamageDefOf.Deterioration || dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Repair)
            {
                return;
            }
            if (HitPoints < compVehicles.FuelCatchesFireHitPointsPercent() * MaxHitPoints && refuelableComp != null && refuelableComp.HasFuel)
            {
                if (!fuelSpilled)
                {
                    refuelableComp.ConsumeFuel(1f);
                    Thing fuelPuddle = ThingMaker.MakeThing(ThingDef.Named("Puddle_Fuel"));
                    int hitPoints = 5;
                    GenSpawn.Spawn(fuelPuddle, Position);
                    fuelPuddle.HitPoints = hitPoints;
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
                wheelRotation +=  currentDriverSpeed / 3f;

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
            if (fuelSpilled)
                if (HitPoints > compVehicles.FuelCatchesFireHitPointsPercent()*(double) MaxHitPoints)
                    fuelSpilled = false;
                else if (refuelableComp != null && !refuelableComp.HasFuel)
                {
                    _puddleHitpointCounter = 0.0f;
                }
                else
                {
                    refuelableComp.ConsumeFuel(0.01f);
                    Thing thing1 = Position.GetThingList().FirstOrDefault(x => x.def == ThingDef.Named("Puddle_Fuel"));
                    if (thing1 != null)
                    {
                        if (_puddleHitpointCounter <= 1.0)
                        {
                            _puddleHitpointCounter = _puddleHitpointCounter + 0.01f;
                        }
                        else
                        {
                            thing1.HitPoints = Mathf.Min(thing1.MaxHitPoints, thing1.HitPoints + 5);
                            _puddleHitpointCounter = 0.0f;
                        }
                    }
                    else
                    {
                        Thing thing2 = ThingMaker.MakeThing(ThingDef.Named("Puddle_Fuel"));
                        int num1 = 5;
                        IntVec3 position = Position;
                        GenSpawn.Spawn(thing2, position);
                        int num2 = num1;
                        thing2.HitPoints = num2;
                        _puddleHitpointCounter = 0.0f;
                    }
                }
        }
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
            wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.04f;
            bodyLoc = drawLoc;
            bodyLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.03f;
            if (compAxles.HasAxles() && Rotation.AsInt % 2 == 1)
            {
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
    }
}