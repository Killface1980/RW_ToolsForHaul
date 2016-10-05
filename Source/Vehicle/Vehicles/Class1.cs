// Decompiled with JetBrains decompiler
// Type: ToolsForHaul.Vehicle_Saddle
// Assembly: ToolsForHaul, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: F435184E-8554-41E8-8A69-42684763FD64
// Assembly location: C:\Program Files (x86)\Steam\SteamApps\common\RimWorld\Mods\ARBRealisticResearch\Assemblies\ToolsForHaul.dll

using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Vehicle_Saddle : ThingWithComps, IThingContainerOwner
    {
        private const int maxNumBoarding = 1;
        private Graphic_Multi graphic_Saddle;
        private Vector3 saddleLoc;
        public CompMountable mountableComp;
        public ThingContainer storage;

        public int MaxItem
        {
            get
            {
                return Rider == null ? 2 : 3;
            }
        }

        public Pawn Rider
        {
            get
            {
                return (this.storage.Where(x => x is Pawn).Count() > 0) ? this.storage.Where(x => x is Pawn).First() as Pawn : null;
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (!mountableComp.IsMounted || !Spawned)
                    return ((Thing)this).DrawPos;
                return mountableComp.Position;
            }
        }

        public Vehicle_Saddle()
        {
            storage = new ThingContainer(this);
        }

        public ThingContainer GetContainer()
        {
            return storage;
        }

        public IntVec3 GetPosition()
        {
            return Position;
        }

        public virtual void BoardOn(Pawn pawn)
        {
            if (mountableComp.IsMounted)
            {
                ThingContainer thingContainer = storage;

                if (thingContainer.Count((x => x is Pawn)) >= 1 || Faction != null && Faction != pawn.Faction)
                    return;
            }
            if (pawn.Faction == Faction.OfPlayer && (pawn.needs.food.CurCategory == HungerCategory.Starving || pawn.needs.rest.CurCategory == RestCategory.Exhausted))
            {
                Messages.Message(pawn.LabelCap + "cannot board on " + this.LabelCap + ": " + pawn.LabelCap + "is starving or exhausted", (MessageSound)2);
            }
            else
            {
                mountableComp.Driver.jobs.StartJob(new Job(DefDatabase<JobDef>.GetNamed("Standby", true), mountableComp.Driver.Position, 4800, false), (JobCondition)3, null, false, true, null);
                storage.TryAdd(pawn);
                pawn.holder = GetContainer();
                pawn.holder.owner = this;
                pawn.jobs.StartJob(new Job(JobDefOf.WaitCombat), 0, null, false, true, null);
            }
        }

        public virtual void Unboard(Pawn pawn)
        {
            ThingContainer thingContainer = storage;

            if (thingContainer.Count<Thing>(x => x is Pawn) <= 0 || !storage.Contains(pawn))
                return;
            pawn.holder = null;
            pawn.jobs.StopAll(false);
            Thing thing;
            storage.TryDrop((Thing)pawn, this.Position, (ThingPlaceMode)1, out thing);
        }

        public virtual void UnboardAll()
        {
            ThingContainer thingContainer = storage;

            if (thingContainer.Count<Thing>(x => x is Pawn) <= 0)
                return;
            using (List<Thing>.Enumerator enumerator = storage.Where<Thing>(x => x is Pawn).ToList<Thing>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Pawn pawn = (Pawn)enumerator.Current;
                    pawn.holder = null;
                    pawn.jobs.StopAll(false);
                    Thing thing;
                    storage.TryDrop((Thing)pawn, this.Position, (ThingPlaceMode)1, out thing);
                }
            }
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            mountableComp = GetComp<CompMountable>();
            UpdateGraphics();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // ISSUE: explicit reference operation
            // ISSUE: cast to a reference type
            Scribe_Deep.LookDeep<ThingContainer>(ref storage, "storage", new object[0]);
        }

        public override void Destroy(DestroyMode mode = 0)
        {
            UnboardAll();
            storage.TryDropAll(this.Position, (ThingPlaceMode)1);
            if (mode == DestroyMode.Deconstruct)
                mode = DestroyMode.Kill;
            base.Destroy(mode);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var baseGizmo in base.GetGizmos())
                yield return baseGizmo;
            if (mountableComp.IsMounted && this.storage.Count(x => x is Pawn) < maxNumBoarding)
            {
                Designator_Board designatorBoard = new Designator_Board();

                designatorBoard.vehicle = this;
                designatorBoard.defaultLabel = "CommandRideLabel".Translate();
                designatorBoard.defaultDesc = "CommandRideDesc".Translate();
                designatorBoard.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconBoard");
                designatorBoard.activateSound = SoundDef.Named("Click");

                yield return designatorBoard;
            }
            if (mountableComp.IsMounted && this.storage.Count(x => x is Pawn) >= maxNumBoarding)
            {
                Command_Action commandUnboardAll = new Command_Action();

                commandUnboardAll.defaultLabel = "CommandGetOffLabel".Translate();
                commandUnboardAll.defaultDesc = "CommandGetOffDesc".Translate();
                commandUnboardAll.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnboardAll");
                commandUnboardAll.activateSound = SoundDef.Named("Click");
                commandUnboardAll.action = () => { this.UnboardAll(); };

                yield return commandUnboardAll;

                Designator_Move designator = new Designator_Move();

                designator.driver = this.mountableComp.Driver;
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
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Board"), this);
                    myPawn.drafter.TakeOrderedJob(jobNew);
                };
                if (this.storage.Count(x => x is Pawn) >= 1)
                {
                    fmoBoard.Label = "AlreadyRide".Translate();
                    fmoBoard.Disabled = true;
                }

                yield return fmoBoard;
            }
        }

        private void UpdateGraphics()
        {
            graphic_Saddle = new Graphic_Multi();
            graphic_Saddle = GraphicDatabase.Get<Graphic_Multi>("Things/Pawn/Vehicle/Saddle", def.graphic.Shader, def.graphic.drawSize, def.graphic.color, def.graphic.colorTwo) as Graphic_Multi;
        }

        public override void Tick()
        {
            base.Tick();
            storage.ThingContainerTick();
            using (IEnumerator<Thing> enumerator = storage.Where<Thing>(x => x is Pawn).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Pawn pawn = (Pawn)enumerator.Current;
                    if (pawn.Downed || pawn.Dead)
                        Unboard(pawn);
                    pawn.Position=(this.Position);
                }
            }
            if (mountableComp.IsMounted)
                return;
            UnboardAll();
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            if (!this.Spawned)
            {
                this.DrawAt(drawLoc);
            }
            else
            {
                saddleLoc = drawLoc;
                saddleLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.01f;

                if (mountableComp.IsMounted)
                {
                    graphic_Saddle.Draw(saddleLoc, ((Thing)this).Rotation, this);
                    Vector3 crewLoc = drawLoc;
                    crewLoc.y = Altitudes.AltitudeFor((AltitudeLayer.Pawn));
                    Vector3 crewsOffset = new Vector3(0.25f, 0.02f, -0.25f);
                    if (this.Rotation == Rot4.North || this.Rotation == Rot4.South)
                        crewsOffset.x = 0f;
                    using (List<Thing>.Enumerator enumerator = storage.Where<Thing>(x => x is Pawn).ToList<Thing>().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Pawn pawn1 = (Pawn)enumerator.Current;
                            ((Thing)pawn1).Rotation =(((Thing)this).Rotation);
                            Pawn pawn2 = pawn1;
                            Vector3 vector3_3 = crewLoc;
                            Vector3 vector3_4 = crewsOffset;
                            Rot4 rotation = this.Rotation;
                            // ISSUE: explicit reference operation
                            double num1 = @rotation.AsAngle;
                            Vector3 vector3_5 = vector3_4.RotatedBy((float)num1);
                            Vector3 vector3_6 = vector3_3 + vector3_5;
                            pawn2.DrawAt(vector3_6);
                            if (pawn1.stances.curStance is Stance_Warmup && Find.Selector.IsSelected(this))
                            {
                                Stance_Warmup stanceWarmup = pawn1.stances.curStance as Stance_Warmup;
                                float num2 = stanceWarmup.ticksLeft >= 300 ? (stanceWarmup.ticksLeft >= 450 ? 0.5f : 0.75f) : 1f;
                                GenDraw.DrawAimPie(stanceWarmup.stanceTracker.pawn, stanceWarmup.focusTarg, (int)(stanceWarmup.ticksLeft * (double)num2), 0.2f);
                            }
                        }
                    }
                }
                else
                    this.DrawAt(drawLoc);
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.AppendLine(Translator.Translate("Rider"));
            using (List<Thing>.Enumerator enumerator = storage.Where<Thing>(x => x is Pawn).ToList<Thing>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Pawn pawn = (Pawn)enumerator.Current;
                    stringBuilder.Append(Translator.Translate(pawn.LabelCap) + ", ");
                }
            }
            stringBuilder.Remove(stringBuilder.Length - 3, 1);
            return stringBuilder.ToString();
        }

    }
}
