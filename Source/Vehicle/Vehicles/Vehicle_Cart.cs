using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Vehicle_Cart : ThingWithComps, IThingContainerOwner
    {
        #region Variables
        // ==================================
        private const int MaxItemPerBodySize = 4;
        private const int DefaultMaxItem = 4;

        //Graphic data
        private static Graphic_Multi graphic_Handle;
        private static Graphic_Multi graphic_Wheel;
        private static Graphic_Multi graphic_FullStorage;
        //Body and part location
        private Vector3 handleLoc;
        private Vector3 wheelLoc;
        private Vector3 bodyLoc;
        private int rotor;


        //mount and storage data
        public CompMountable mountableComp;
        public ThingContainer storage = null;
        public ThingContainer GetContainer() { return storage; }
        public IntVec3 GetPosition() { return Position; }

        //slotGroupParent Interface
        public ThingFilter allowances;

        public int MaxItem
        {
            get
            {
                return (mountableComp.IsMounted && mountableComp.Driver.RaceProps.Animal) ?
                    Mathf.CeilToInt(mountableComp.Driver.BodySize * MaxItemPerBodySize) : DefaultMaxItem;
            }
        }
        public int MaxStack { get { return MaxItem * 100; } }

        #endregion

        #region Setup Work


        public Vehicle_Cart() : base()
        {
            // current spin degree
            rotor = 0;
            //Inventory Initialize. It should be moved in constructor
            storage = new ThingContainer(this);
            allowances = new ThingFilter();
            allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
            allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
        }

        static Vehicle_Cart()
        {
            ThingDef def = ThingDef.Named("VehicleCart");
            graphic_Wheel = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cart_Wheel", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;

            graphic_Handle = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cart_Handle", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
            graphic_FullStorage = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Cart_FullStorage", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
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

            //UpdateGraphics();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep<ThingContainer>(ref storage, "storage");
            Scribe_Deep.LookDeep<ThingFilter>(ref allowances, "allowances");
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
            storage.TryDropAll(Position, ThingPlaceMode.Near);

            if (mode == DestroyMode.Deconstruct)
                mode = DestroyMode.Kill;
            base.Destroy(mode);
        }
        // half of the quater of the cell
        public const float wheelRadius = 0.0256f;





        public void RotateWheelByDegree(int degree)
        {
            // nullify rotor counter if wheel made whole spin
            if (rotor % 360 == 0)
            {
                rotor = 0;
            }

            if (Rotation == Rot4.East)
            {
                rotor += degree;
            }
            else
            {
                rotor -= degree;
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
            base.Tick();
            if (mountableComp.IsMounted && mountableComp.Driver.pather.Moving && !mountableComp.Driver.stances.FullBodyBusy)
            {
                // rotate rotor by parent's move speed value
                int degree = Mathf.FloorToInt(mountableComp.Driver.GetStatValue(StatDefOf.MoveSpeed) / (GenDate.SecondsToTicks(1) * wheelRadius));
                RotateWheelByDegree(degree);
            }
            if (GetPosition().InNoBuildEdgeArea()  && Spawned && (mountableComp.Driver.Faction != Faction.OfPlayer || mountableComp.Driver.MentalState.def == MentalStateDefOf.WanderPsychotic))
            {
                DeSpawn();
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
                // if (graphic_FullStorage == null)
                //     UpdateGraphics();
                return (storage.Count > 0) ? graphic_FullStorage : base.Graphic;
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (!mountableComp.IsMounted || !Spawned)
                    return base.DrawPos;
                return mountableComp.Position;
            }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            Vector2 drawSize = def.graphic.drawSize;

            //Body and part location
            handleLoc = drawLoc;
            handleLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.05f;
            wheelLoc = drawLoc;
            wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.2f;
            bodyLoc = drawLoc;
            bodyLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.15f;

            if (!Spawned || mountableComp.Driver == null)
            {
                var standingBodyLoc = new Vector3(bodyLoc.x, bodyLoc.y, bodyLoc.z + 0.1f);
                var standingWheelLoc = new Vector3(wheelLoc.x + -20f / 96f * drawSize.x * -1, wheelLoc.y, wheelLoc.z + 0.1f + -24f / 192f * drawSize.y);
                //  var standingHandleLoc = new Vector3(handleLoc.x, handleLoc.y, handleLoc.z + 0.1f);

                Graphic.Draw(standingBodyLoc, Rot4.West, this);
                graphic_Wheel.Draw(standingWheelLoc, Rot4.West, this);
                //   graphic_Handle.Draw(standingHandleLoc, Rot4.West, this);

                return;
            }

            //Vertical
            if (Rotation.AsInt % 2 == 0)
            {
                wheelLoc.z += 0.025f * Mathf.Sin((rotor * 0.10f) % (2 * Mathf.PI));
                wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.1f;
            }

            base.DrawAt(bodyLoc);
            //Wheels
            //Horizontal
            if (Rotation.AsInt % 2 == 1)
            {
                int flip = (Rotation == Rot4.West) ? -1 : 1;
                handleLoc.z += 0.1f;
                handleLoc.x += 1.2f * flip;
                wheelLoc.z += 0.1f;
                bodyLoc.z += 0.1f;
                Vector3 scale = new Vector3(1f * drawSize.x, 1f, 1f * drawSize.y);
                Matrix4x4 matrix = new Matrix4x4();
                Vector3 offset = new Vector3(-20f / 96f * drawSize.x * flip, 0, -24f / 192f * drawSize.y);
                Quaternion quat = Rotation.AsQuat;
                float x = 1f * Mathf.Sin((rotor * 0.015f) % (2 * Mathf.PI));
                float y = 1f * Mathf.Cos((rotor * 0.015f) % (2 * Mathf.PI));
                quat.SetLookRotation(new Vector3(x, 0, y), Vector3.up);
                matrix.SetTRS(wheelLoc + offset, quat, scale);
                Graphics.DrawMesh(MeshPool.plane10, matrix, graphic_Wheel.MatAt(Rotation), 0);
            }
            else
                graphic_Wheel.Draw(wheelLoc, Rotation, this);
            if (mountableComp.Driver.RaceProps.Animal)
            {
                graphic_Handle.Draw(handleLoc, Rotation, this);
            }
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