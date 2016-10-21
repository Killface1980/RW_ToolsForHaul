using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.Designators;
using ToolsForHaul.JobDefs;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Vehicle_Saddle : ThingWithComps, IThingContainerOwner
    {

        #region Variables
        // ==================================
        private const int maxNumBoarding = 1;

        //Graphic data
        private  Graphic_Multi graphic_Saddle;
        //Body and part location
        private Vector3 saddleLoc;

        //mount and storage data
        public CompMountable mountableComp;
        public ThingContainer storage;
        public ThingContainer GetContainer() { return storage; }

        public IntVec3 GetPosition()
        {
            return Position;
        }

        public int MaxItem { get { return Rider != null ? 3 : 2; } }

        public Pawn Rider
        {
            get
            {
                return storage.Any(x => x is Pawn) ? storage.First(x => x is Pawn) as Pawn : null;
            }
        }
        public virtual void BoardOn(Pawn pawn)
        {
            if (mountableComp.IsMounted
                && (storage.Count(x => x is Pawn) >= maxNumBoarding                                //No Space
                || (Faction != null && Faction != pawn.Faction)))                        //Not your vehicle
                return;

            if (pawn.Faction == Faction.OfPlayer && (pawn.needs.food.CurCategory == HungerCategory.Starving || pawn.needs.rest.CurCategory == RestCategory.Exhausted))
            {
                Messages.Message(pawn.LabelCap + "cannot board on " + LabelCap + ": " + pawn.LabelCap + "is starving or exhausted", MessageSound.RejectInput);
                return;
            }
            Job jobNew = new Job(HaulJobDefOf.StandBy, mountableComp.Driver.Position, 4800);
            mountableComp.Driver.jobs.StartJob(jobNew, JobCondition.Incompletable);

            storage.TryAdd(pawn);
            pawn.holder = GetContainer();
            pawn.holder.owner = this;
            pawn.jobs.StartJob(new Job(JobDefOf.WaitCombat));
        }

        public virtual void Unboard(Pawn pawn)
        {
            if (storage.Count(x => x is Pawn) <= 0)
                return;

            if (storage.Contains(pawn))
            {
                pawn.holder = null;
                pawn.jobs.StopAll();
                Thing dummy;
                storage.TryDrop(pawn, Position, ThingPlaceMode.Near, out dummy);
            }
        }
        public virtual void UnboardAll()
        {
            if (storage.Count(x => x is Pawn) <= 0)
                return;

            foreach (Thing thing in storage.Where(x => x is Pawn).ToList())
            {
                Pawn crew = (Pawn) thing;
                if (crew == null) continue;

                crew.holder = null;
                crew.jobs?.StopAll();
                Thing dummy;
                storage.TryDrop(crew, Position, ThingPlaceMode.Near, out dummy);
                //     storage.TryDropAll(Position, ThingPlaceMode.Near);
            }
        }
        #endregion

        #region Setup Work

        public Vehicle_Saddle()
        {
            storage = new ThingContainer(this);
        }


        public override void SpawnSetup()
        {
            base.SpawnSetup();
            mountableComp = GetComp<CompMountable>();

            LongEventHandler.ExecuteWhenFinished(UpdateGraphics);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.LookDeep(ref storage, "storage");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            UnboardAll();
            storage.TryDropAll(Position, ThingPlaceMode.Near);

            if (mode == DestroyMode.Deconstruct)
                mode = DestroyMode.Kill;
            base.Destroy(mode);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo baseGizmo in base.GetGizmos())
                yield return baseGizmo;
            if (mountableComp.IsMounted && storage.Count(x => x is Pawn) < maxNumBoarding)
            {
                Designator_Board designatorBoard = new Designator_Board();

                designatorBoard.vehicle = this;
                designatorBoard.defaultLabel = "CommandRideLabel".Translate();
                designatorBoard.defaultDesc = "CommandRideDesc".Translate();
                designatorBoard.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconBoard");
                designatorBoard.activateSound = SoundDef.Named("Click");

                yield return designatorBoard;
            }
            if (mountableComp.IsMounted && storage.Count(x => x is Pawn) >= maxNumBoarding)
            {
                Command_Action commandUnboardAll = new Command_Action();

                commandUnboardAll.defaultLabel = "CommandGetOffLabel".Translate();
                commandUnboardAll.defaultDesc = "CommandGetOffDesc".Translate();
                commandUnboardAll.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnboardAll");
                commandUnboardAll.activateSound = SoundDef.Named("Click");
                commandUnboardAll.action = () =>
                {
                    UnboardAll();
                };

                yield return commandUnboardAll;

                Designator_Move designator = new Designator_Move();

                designator.driver = mountableComp.Driver;
                designator.defaultLabel = "CommandMoveLabel".Translate();
                designator.defaultDesc = "CommandMoveDesc".Translate();
                designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/ReleaseAnimals");
                designator.activateSound = SoundDef.Named("Click");
                designator.hotKey = KeyBindingDefOf.Misc1;

                yield return designator;
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;

            if (mountableComp.IsMounted)
            {
                FloatMenuOption fmoBoard = new FloatMenuOption();

                fmoBoard.Label = "Ride".Translate(mountableComp.Driver.LabelCap);
                fmoBoard.priority = MenuOptionPriority.High;
                fmoBoard.action = () =>
                {
                    Job jobNew = new Job(HaulJobDefOf.Board, this);
                    myPawn.drafter.TakeOrderedJob(jobNew);
                };
                if (storage.Count(x => x is Pawn) >= 1)
                {
                    fmoBoard.Label = "AlreadyRide".Translate();
                    fmoBoard.Disabled = true;
                }

                yield return fmoBoard;
            }
        }

        /// <summary>
        /// Import the graphics
        /// </summary>
        private void UpdateGraphics()
        {
            graphic_Saddle = new Graphic_Multi();
            graphic_Saddle = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Saddle", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
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
            storage.ThingContainerTick();
            foreach (Thing thing in storage.Where(x => x is Pawn))
            {
                Pawn crew = (Pawn) thing;
                if (crew.Downed || crew.Dead)
                    Unboard(crew);
                crew.Position = Position;
            }
            if (mountableComp != null && mountableComp.IsMounted)
                return;
            UnboardAll();
        }

        #endregion

        #region Graphics / Inspections
        // ==================================

        /// <summary>
        /// 
        /// </summary>
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
            if (!Spawned)
            {
                base.DrawAt(drawLoc);
                return;
            }
            saddleLoc = drawLoc;
            saddleLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.01f;

            if (mountableComp.IsMounted)
            {
                graphic_Saddle.Draw(saddleLoc, Rotation, this);
                Vector3 crewLoc = drawLoc;
                crewLoc.y = Altitudes.AltitudeFor(AltitudeLayer.PawnUnused) + 0.1f;
                Vector3 crewsOffset = new Vector3(0.25f, 0.02f, -0.25f);
                if (Rotation == Rot4.North || Rotation == Rot4.South)
                    crewsOffset.x = 0f;
                else
                {
                    crewLoc.z += 1f;
                }
                if (storage == null) return;

                foreach (Thing thing in storage.Where(x => x is Pawn).ToList())
                {
                    Pawn pawn = (Pawn) thing;
                    if (pawn == null) continue;
                    pawn.Rotation = Rotation;
                    pawn.DrawAt(crewLoc + crewsOffset.RotatedBy(Rotation.AsAngle));

                    if (pawn.stances.curStance is Stance_Warmup && Find.Selector.IsSelected(this))
                    {
                        Stance_Warmup stanceWarmup = pawn.stances.curStance as Stance_Warmup;
                        float num2 = stanceWarmup.ticksLeft >= 300 ? (stanceWarmup.ticksLeft >= 450 ? 0.5f : 0.75f) : 1f;
                        GenDraw.DrawAimPie(stanceWarmup.stanceTracker.pawn, stanceWarmup.focusTarg, (int)(stanceWarmup.ticksLeft * (double)num2), 0.2f);
                    }
                }
            }
            else
                base.DrawAt(drawLoc);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.AppendLine("Rider".Translate());
            foreach (Pawn pawn in storage.Where(x => x is Pawn).ToList())
                stringBuilder.Append(pawn.LabelCap.Translate() + ", ");
            stringBuilder.Remove(stringBuilder.Length - 3, 1);
            return stringBuilder.ToString();
        }
        #endregion
    }
}