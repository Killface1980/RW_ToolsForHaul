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

    [StaticConstructorOnStartup]
    public class Vehicle_Cart : Building_Turret, IThingHolder, IAttackTargetSearcher, IAttackTarget
    {

        #region Tank

        protected StunHandler stunner;

        protected LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;

        public override LocalTargetInfo CurrentTarget
        {
            get;

        }

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

            if (this.allowances == null)
            {
                this.allowances = new ThingFilter();
                this.allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
                this.allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
            }

            // Spotlight
            // this.updateOffsetInTicks = Rand.RangeInclusive(0, updatePeriodInTicks);
            // 
            // spotlightMatrix.SetTRS(base.DrawPos + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightScale);
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

            if (this.MountableComp.IsMounted) if (GameComponentToolsForHaul.CurrentDrivers.ContainsKey(this.MountableComp.Driver)) GameComponentToolsForHaul.CurrentDrivers.Remove(this.MountableComp.Driver);

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

            // Scribe_References.Look<Thing>(ref this.light, "light");
            // Scribe_Values.Look<LightMode>(ref this.lightMode, "lightMode");
            // Scribe_Values.Look<float>(ref this.spotLightRotationBaseOffset, "spotLightRotationBaseOffset");
            // Scribe_Values.Look<float>(ref this.spotLightRotation, "spotLightRotation");
            // Scribe_Values.Look<float>(ref this.spotLightRotationTarget, "spotLightRotationTarget");
            // Scribe_Values.Look<bool>(ref this.spotLightRotationTurnRight, "spotLightRotationTurnRight");
            // Scribe_Values.Look<float>(ref this.spotLightRangeBaseOffset, "spotLightRangeBaseOffset");
            // Scribe_Values.Look<float>(ref this.spotLightRange, "spotLightRange");
            // Scribe_Values.Look<float>(ref this.spotLightRangeTarget, "spotLightRangeTarget");
            // Scribe_Values.Look<int>(ref this.idlePauseTicks, "idlePauseTicks");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (this.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            foreach (Gizmo baseGizmo in base.GetGizmos())
            {
                yield return baseGizmo;
            }

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


            // Spotlight
            //  int groupKeyBase = 700000102;
            //
            //  Command_Action lightModeButton = new Command_Action();
            //  switch (this.lightMode)
            //  {
            //      case (LightMode.Conic):
            //          lightModeButton.defaultLabel = "Ligth mode: conic.";
            //          lightModeButton.defaultDesc = "In this mode, the spotlight turret patrols in a conic area in front of it. Automatically lock on hostiles.";
            //          break;
            //      case (LightMode.Automatic):
            //          lightModeButton.defaultLabel = "Ligth mode: automatic.";
            //          lightModeButton.defaultDesc = "In this mode, the spotlight turret randomly lights the surroundings. Automatically lock on hostiles.";
            //          break;
            //      case (LightMode.Fixed):
            //          lightModeButton.defaultLabel = "Ligth mode: fixed.";
            //          lightModeButton.defaultDesc = "In this mode, the spotlight turret only light a fixed area. Does NOT automatically lock on hostiles.";
            //          break;
            //  }
            //  lightModeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_SwitchMode");
            //  lightModeButton.activateSound = Static.ClickSound;
            //  lightModeButton.action = new Action(SwitchLigthMode);
            //  lightModeButton.groupKey = groupKeyBase + 1;
            //  yield return lightModeButton;
            //
            //  if ((this.lightMode == LightMode.Conic)
            //      || (this.lightMode == LightMode.Fixed))
            //  {
            //      Command_Action decreaseRangeButton = new Command_Action();
            //      decreaseRangeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_DecreaseRange");
            //      decreaseRangeButton.defaultLabel = "Range: " + this.spotLightRangeBaseOffset;
            //      decreaseRangeButton.defaultDesc = "Decrease range.";
            //      decreaseRangeButton.activateSound = Static.ClickSound;
            //      decreaseRangeButton.action = new Action(DecreaseSpotlightRange);
            //      decreaseRangeButton.groupKey = groupKeyBase + 2;
            //      yield return decreaseRangeButton;
            //
            //      Command_Action increaseRangeButton = new Command_Action();
            //      increaseRangeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_IncreaseRange");
            //      increaseRangeButton.defaultLabel = "";
            //      increaseRangeButton.defaultDesc = "Increase range.";
            //      increaseRangeButton.activateSound = Static.ClickSound;
            //      increaseRangeButton.action = new Action(IncreaseSpotlightRange);
            //      increaseRangeButton.groupKey = groupKeyBase + 3;
            //      yield return increaseRangeButton;
            //
            //      float rotation = Mathf.Repeat(this.Rotation.AsAngle + this.spotLightRotationBaseOffset, 360f);
            //      Command_Action turnLeftButton = new Command_Action();
            //      turnLeftButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_TurnLeft");
            //      turnLeftButton.defaultLabel = "Rotation: " + rotation + "°";
            //      turnLeftButton.defaultDesc = "Turn left.";
            //      turnLeftButton.activateSound = Static.ClickSound;
            //      turnLeftButton.action = new Action(AddSpotlightBaseRotationLeftOffset);
            //      turnLeftButton.groupKey = groupKeyBase + 4;
            //      yield return turnLeftButton;
            //
            //      Command_Action turnRightButton = new Command_Action();
            //      turnRightButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_TurnRight");
            //      turnRightButton.defaultLabel = "";
            //      turnRightButton.defaultDesc = "Turn right.";
            //      turnRightButton.activateSound = Static.ClickSound;
            //      turnRightButton.action = new Action(AddSpotlightBaseRotationRightOffset);
            //      turnRightButton.groupKey = groupKeyBase + 5;
            //      yield return turnRightButton;
            //  }




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
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
            {
                yield return fmo;
            }

            // do nothing if not of colony
            if (this.Faction != Faction.OfPlayer)
            {
                yield break;
            }


            Map map = myPawn.Map;


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

            if (!this.MountableComp.IsMounted)
            {
                yield return new FloatMenuOption("Deconstruct".Translate(this.LabelShort), action_Deconstruct);
            }

        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // PowerOffLight();

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
            base.Tick();

            if (!this.instantiated)
            {
                this.VehicleComp.currentDriverSpeed = this.VehicleComp.VehicleSpeed;
                this.instantiated = true;
            }
            //     this.innerContainer.ThingOwnerTick();

            // Lights
            // Check if turret is powered.
            //  if (!this.MountableComp.IsMounted)
            //  {
            //      PowerOffLight();
            //      ResetLight();
            //  }
            //
            //  // Check locked target is still valid.
            //  if (this.CurrentTarget != null)
            //  {
            //      //   // Check target is still valid: not killed or downed and in sight.
            //      //   if (this.CurrentTarget.Thing.DestroyedOrNull()
            //      //       || (IsPawnValidTarget(this.target) == false))
            //      //   {
            //      //       // Target is no more valid.
            //      //       this.target = null;
            //      //   }
            //      // Target is valid.
            //      this.spotLightRotationTarget = Mathf.Round((this.CurrentTarget.Thing.Position - this.Position).AngleFlat);
            //      ComputeRotationDirection();
            //      this.spotLightRangeTarget = (this.CurrentTarget.Thing.Position - this.Position).ToVector3().magnitude;
            //  }
            //  else
            //  {
            //      // Reset idle tick counter.
            //      this.idlePauseTicks = idlePauseDurationInTicks;
            //
            //      // fixed rotation
            //      IdleTurnTick();
            //
            //  }
            //  // Update the spotlight rotation and range.
            //  SpotlightMotionTick();


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
            base.DrawAt(drawLoc);
            if (!this.Spawned)
            {
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

            // // Lights
            // spotlightMatrix.SetTRS(drawLoc + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightScale);
            // if (this.MountableComp.IsMounted)
            // {
            //     Graphics.DrawMesh(MeshPool.plane10, spotlightMatrix, spotlightOnTexture, 0);
            //     spotlightLightEffectMatrix.SetTRS(drawLoc + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightLightEffectScale);
            //     Graphics.DrawMesh(MeshPool.plane10, spotlightLightEffectMatrix, spotlightLightEffectTexture, 0);
            // }
            // else
            // {
            //     Graphics.DrawMesh(MeshPool.plane10, spotlightMatrix, spotlightOffTexture, 0);
            // }
            //
            // if (Find.Selector.IsSelected(this)
            //     && (this.CurrentTarget != null))
            // {
            //     Vector3 lineOrigin = this.TrueCenter();
            //     Vector3 lineTarget = this.CurrentTarget.Thing.Position.ToVector3Shifted();
            //     lineTarget.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            //     lineOrigin.y = lineTarget.y;
            //     GenDraw.DrawLineBetween(lineOrigin, lineTarget, targetLineTexture);
            // }
        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());

            string currentDriverString;
            if (this.MountableComp.Driver != null)
            {
                currentDriverString = this.MountableComp.Driver.LabelCap;
            }
            else
            {
                currentDriverString = "NoDriver".Translate();
            }

            stringBuilder.AppendLine("Driver".Translate() + ": " + currentDriverString);
            if (this.VehicleComp.tankLeaking)
            {
                stringBuilder.AppendLine("TankLeaking".Translate());
            }

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