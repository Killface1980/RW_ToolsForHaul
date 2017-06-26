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
    using SpotlightTurret;

    using ToolsForHaul.Components.Vehicle;

    using CompVehicle = ToolsForHaul.Components.CompVehicle;

    [StaticConstructorOnStartup]
    public class Vehicle_Cart : Building_Turret, IThingHolder, IAttackTargetSearcher, IAttackTarget
    {
        public enum LightMode
        {
            Conic,
            Automatic,
            Fixed
        }
        public const int updatePeriodInTicks = 30;
        public int updateOffsetInTicks = 0;
        public Thing light = null;

        // Light mode, spotlight range and rotation.
        public LightMode lightMode = LightMode.Conic;
        public const float spotlightMinRange = 5f;
        public const int spotLightRangeRateInTicksIdle = 16;          // Rate at which range is modified by 1 when idle.
        public const int spotLightRangeRateInTicksIdleTargetting = 8; // Rate at which range is modified by 1 when targetting.
        public float spotLightRangeBaseOffset = 15f;
        public float spotLightRange = 15f;
        public float spotLightRangeTarget = 15f;

        public const int spotLightRotationRateInTicksIdle = 4;       // Rate at which rotation is changed by 1° when idle.
        public const int spotLightRotationRateInTicksTargetting = 1; // Rate at which rotation is changed by 1° when targetting.
        public float spotLightRotationBaseOffset = 0f;
        public float spotLightRotation = 0f;
        public float spotLightRotationTarget = 0f;
        public bool spotLightRotationTurnRight = true;

        public const int idlePauseDurationInTicks = 3 * GenTicks.TicksPerRealSecond;
        public int idlePauseTicks = 1;

        // Textures.
        public static Material spotlightOnTexture = MaterialPool.MatFrom("Things/Building/SpotlightTurret_SpotlightOn");
        public Matrix4x4 spotlightMatrix = default(Matrix4x4);
        public Vector3 spotlightScale = new Vector3(5f, 1f, 5f);
        public static Material spotlightOffTexture = MaterialPool.MatFrom("Things/Building/SpotlightTurret_SpotlightOff");
        public Matrix4x4 spotlightLightEffectMatrix = default(Matrix4x4);
        public Vector3 spotlightLightEffectScale = new Vector3(5f, 1f, 5f);
        public static Material spotlightLightEffectTexture = MaterialPool.MatFrom("Things/Building/SpotlightTurret_LightEffect", ShaderDatabase.Transparent);
        public static Material targetLineTexture = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 1f, 1f));

        /// <summary>
        /// Power off the light.
        /// </summary>
        public void PowerOffLight()
        {
            if (this.light.DestroyedOrNull() == false)
            {
                this.light.Destroy();
            }
            this.light = null;
        }

        /// <summary>
        /// Light an area at given position.
        /// </summary>
        public void LightAreaAt(IntVec3 position)
        {
            // Remove old light if target has moved.
            if ((this.light.DestroyedOrNull() == false)
                && (position != this.light.Position))
            {
                PowerOffLight();
            }
            // Spawn a new light.
            if (this.light.DestroyedOrNull())
            {
                this.light = GenSpawn.Spawn(Util_SpotlightTurret.spotlightLightDef, position, this.Map);
            }
        }

        /// <summary>
        /// Start a new idle motion when turret is paused for a moment.
        /// </summary>
        public void IdleTurnTick()
        {
            if (this.idlePauseTicks > 0)
            {
                this.idlePauseTicks--;
                if (this.idlePauseTicks == 0)
                {
                    // Start a new idle motion.
                    StartNewIdleMotion();
                }
            }
        }

        /// <summary>
        /// Compute spotlight target rotation, target range and rotation direction.
        /// </summary>
        public void StartNewIdleMotion()
        {
            switch (this.lightMode)
            {
                case LightMode.Automatic:
                    this.spotLightRotationTarget = (float)Rand.Range(0, 360);
                    this.spotLightRangeTarget = Rand.Range(spotlightMinRange, this.def.specialDisplayRadius);
                    break;
                case LightMode.Conic:
                    if (this.spotLightRotation == Mathf.Repeat(this.Rotation.AsAngle + this.spotLightRotationBaseOffset - 45f, 360f))
                    {
                        // Spotlight is targeting the left. Now, target the right.
                        this.spotLightRotationTarget = Mathf.Repeat(this.Rotation.AsAngle + this.spotLightRotationBaseOffset + 45f, 360f);
                    }
                    else
                    {
                        // Spotlight is targeting the right. Now, target the left.
                        this.spotLightRotationTarget = Mathf.Repeat(this.Rotation.AsAngle + this.spotLightRotationBaseOffset - 45f, 360f);
                    }
                    this.spotLightRangeTarget = this.spotLightRangeBaseOffset;
                    break;
                case LightMode.Fixed:
                    // Fixed range and rotation.
                    this.spotLightRotationTarget = Mathf.Repeat(this.Rotation.AsAngle + this.spotLightRotationBaseOffset, 360f);
                    this.spotLightRangeTarget = this.spotLightRangeBaseOffset;
                    break;
            }
            // Compute rotation direction.
            ComputeRotationDirection();
        }

        /// <summary>
        /// Compute the optimal rotation direction.
        /// </summary>
        public void ComputeRotationDirection()
        {
            if (this.spotLightRotationTarget >= this.spotLightRotation)
            {
                float dif = this.spotLightRotationTarget - this.spotLightRotation;
                if (dif <= 180f)
                {
                    this.spotLightRotationTurnRight = true;
                }
                else
                {
                    this.spotLightRotationTurnRight = false;
                }
            }
            else
            {
                float dif = this.spotLightRotation - this.spotLightRotationTarget;
                if (dif <= 180f)
                {
                    this.spotLightRotationTurnRight = false;
                }
                else
                {
                    this.spotLightRotationTurnRight = true;
                }
            }
        }

        /// <summary>
        /// Update spotlight to face the target.
        /// </summary>
        public void SpotlightMotionTick()
        {
            // Update spotlight rotation.
            if (this.spotLightRotation != this.spotLightRotationTarget)
            {
                float rotationRate = spotLightRotationRateInTicksIdle;
                if (this.CurrentTarget != null)
                {
                    rotationRate = spotLightRotationRateInTicksTargetting;
                }
                else
                {
                         float deltaAngle = ComputeAbsoluteAngleDelta(this.spotLightRotation, this.spotLightRotationTarget);
                         if (deltaAngle < 20f)
                         {
                             rotationRate *= 2;
                         }
                }
                if ((Find.TickManager.TicksGame % rotationRate) == 0)
                {
                    if (this.spotLightRotationTurnRight)
                    {
                        this.spotLightRotation = Mathf.Repeat(this.spotLightRotation + 1f, 360f);
                    }
                    else
                    {
                        this.spotLightRotation = Mathf.Repeat(this.spotLightRotation - 1f, 360f);
                    }
                }
            }

            // Update spotlight range.
            if (this.spotLightRange != this.spotLightRangeTarget)
            {
                float rangeRate = spotLightRangeRateInTicksIdle;
                if (this.CurrentTarget != null)
                {
                    rangeRate = spotLightRangeRateInTicksIdleTargetting;
                }
                if ((Find.TickManager.TicksGame % rangeRate) == 0)
                {
                    if (Mathf.Abs(this.spotLightRangeTarget - this.spotLightRange) < 1f)
                    {
                        this.spotLightRange = this.spotLightRangeTarget;
                    }
                    else if (this.spotLightRange < this.spotLightRangeTarget)
                    {
                        this.spotLightRange++;
                    }
                    else
                    {
                        this.spotLightRange--;
                    }
                }
            }

            if ((this.CurrentTarget == null)
                && (this.idlePauseTicks == 0)
                && (this.spotLightRotation == this.spotLightRotationTarget)
                && (this.spotLightRange == this.spotLightRangeTarget))
            {
                // Motion is finished, start pause.
                this.idlePauseTicks = idlePauseDurationInTicks;
            }

            // Light the area in front of the spotlight.
            Vector3 lightVector3 = new Vector3(0, 0, this.spotLightRange).RotatedBy(this.spotLightRotation);
            IntVec3 lightIntVec3 = new IntVec3(Mathf.RoundToInt(lightVector3.x), 0, Mathf.RoundToInt(lightVector3.z));
            IntVec3 spotlightTarget = this.Position + lightIntVec3;
            IntVec3 farthestPosition = GetFarthestPositionInSight(spotlightTarget);
            LightAreaAt(farthestPosition);
        }

        /// <summary>
        /// Compute the absolute delta angle between two angles.
        /// </summary>
        public float ComputeAbsoluteAngleDelta(float angle1, float angle2)
        {
            float absoluteDeltaAngle = Mathf.Abs(angle2 - angle1) % 360f;
            if (absoluteDeltaAngle > 180f)
            {
                absoluteDeltaAngle -= 180f;
            }
            return absoluteDeltaAngle;
        }

        /// <summary>
        /// Get the farthest position from the turret in direction of spotlightTarget.
        /// </summary>
        public IntVec3 GetFarthestPositionInSight(IntVec3 spotlightTarget)
        {
            IntVec3 farthestPosition = this.Position;

            Mathf.Clamp(spotlightTarget.x, 0, this.Map.Size.x);
            Mathf.Clamp(spotlightTarget.z, 0, this.Map.Size.z);

            IEnumerable<IntVec3> lineOfSightPoints = GenSight.PointsOnLineOfSight(this.Position, spotlightTarget);
            foreach (IntVec3 point in lineOfSightPoints)
            {
                if (point.CanBeSeenOverFast(this.Map) == false)
                {
                    // Return last non-blocked position.
                    return farthestPosition;
                }
                farthestPosition = point; // Store last valid point in sight.
            }
            if (spotlightTarget.CanBeSeenOverFast(this.Map))
            {
                // Nothing is blocking.
                return spotlightTarget;
            }
            else
            {
                // Target position is blocked. Return last non-blocked position.
                return farthestPosition;
            }
        }


        /// <summary>
        /// Reset the light and immediately start an idle turn.
        /// </summary>
        public void ResetLight()
        {
            this.spotLightRotationTarget = this.spotLightRotation;
            this.spotLightRangeTarget = this.spotLightRange;
            this.idlePauseTicks = 1;
        }
        /// <summary>
        /// Switch light mode.
        /// </summary>
        public void SwitchLigthMode()
        {
            ResetLight();
            switch (this.lightMode)
            {
                case LightMode.Automatic:
                    this.lightMode = LightMode.Conic;
                    break;
                case LightMode.Conic:
                    this.lightMode = LightMode.Fixed;
                    break;
                case LightMode.Fixed:
                    this.lightMode = LightMode.Automatic;
                    break;
            }
        }

        /// <summary>
        /// Add an offset to the spotlight base rotation.
        /// </summary>
        public void AddSpotlightBaseRotationLeftOffset()
        {
            this.spotLightRotationBaseOffset = Mathf.Repeat(this.spotLightRotationBaseOffset - 10f, 360f);
            ResetLight();
        }

        /// <summary>
        /// Add an offset to the spotlight base rotation.
        /// </summary>
        public void AddSpotlightBaseRotationRightOffset()
        {
            this.spotLightRotationBaseOffset = Mathf.Repeat(this.spotLightRotationBaseOffset + 10f, 360f);
            ResetLight();
        }

        /// <summary>
        /// Decrease the spotlight range.
        /// </summary>
        public void DecreaseSpotlightRange()
        {
            if (this.spotLightRangeBaseOffset > spotlightMinRange)
            {
                this.spotLightRangeBaseOffset -= 1f;
            }
            ResetLight();
        }

        /// <summary>
        /// Increase the spotlight range.
        /// </summary>
        public void IncreaseSpotlightRange()
        {
            if (this.spotLightRangeBaseOffset < Mathf.Round(this.def.specialDisplayRadius))
            {
                this.spotLightRangeBaseOffset += 1f;
            }
            ResetLight();
        }


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

            this.updateOffsetInTicks = Rand.RangeInclusive(0, updatePeriodInTicks);

            spotlightMatrix.SetTRS(base.DrawPos + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightScale);
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

            Scribe_References.Look<Thing>(ref this.light, "light");
            Scribe_Values.Look<LightMode>(ref this.lightMode, "lightMode");
            Scribe_Values.Look<float>(ref this.spotLightRotationBaseOffset, "spotLightRotationBaseOffset");
            Scribe_Values.Look<float>(ref this.spotLightRotation, "spotLightRotation");
            Scribe_Values.Look<float>(ref this.spotLightRotationTarget, "spotLightRotationTarget");
            Scribe_Values.Look<bool>(ref this.spotLightRotationTurnRight, "spotLightRotationTurnRight");
            Scribe_Values.Look<float>(ref this.spotLightRangeBaseOffset, "spotLightRangeBaseOffset");
            Scribe_Values.Look<float>(ref this.spotLightRange, "spotLightRange");
            Scribe_Values.Look<float>(ref this.spotLightRangeTarget, "spotLightRangeTarget");
            Scribe_Values.Look<int>(ref this.idlePauseTicks, "idlePauseTicks");
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

            IList<Gizmo> buttonList = new List<Gizmo>();
            int groupKeyBase = 700000102;

            Command_Action lightModeButton = new Command_Action();
            switch (this.lightMode)
            {
                case (LightMode.Conic):
                    lightModeButton.defaultLabel = "Ligth mode: conic.";
                    lightModeButton.defaultDesc = "In this mode, the spotlight turret patrols in a conic area in front of it. Automatically lock on hostiles.";
                    break;
                case (LightMode.Automatic):
                    lightModeButton.defaultLabel = "Ligth mode: automatic.";
                    lightModeButton.defaultDesc = "In this mode, the spotlight turret randomly lights the surroundings. Automatically lock on hostiles.";
                    break;
                case (LightMode.Fixed):
                    lightModeButton.defaultLabel = "Ligth mode: fixed.";
                    lightModeButton.defaultDesc = "In this mode, the spotlight turret only light a fixed area. Does NOT automatically lock on hostiles.";
                    break;
            }
            lightModeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_SwitchMode");
            lightModeButton.activateSound = SoundDef.Named("Click");
            lightModeButton.action = new Action(SwitchLigthMode);
            lightModeButton.groupKey = groupKeyBase + 1;
            yield return lightModeButton;

            if ((this.lightMode == LightMode.Conic)
                || (this.lightMode == LightMode.Fixed))
            {
                Command_Action decreaseRangeButton = new Command_Action();
                decreaseRangeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_DecreaseRange");
                decreaseRangeButton.defaultLabel = "Range: " + this.spotLightRangeBaseOffset;
                decreaseRangeButton.defaultDesc = "Decrease range.";
                decreaseRangeButton.activateSound = SoundDef.Named("Click");
                decreaseRangeButton.action = new Action(DecreaseSpotlightRange);
                decreaseRangeButton.groupKey = groupKeyBase + 2;
                yield return decreaseRangeButton;

                Command_Action increaseRangeButton = new Command_Action();
                increaseRangeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_IncreaseRange");
                increaseRangeButton.defaultLabel = "";
                increaseRangeButton.defaultDesc = "Increase range.";
                increaseRangeButton.activateSound = SoundDef.Named("Click");
                increaseRangeButton.action = new Action(IncreaseSpotlightRange);
                increaseRangeButton.groupKey = groupKeyBase + 3;
                yield return increaseRangeButton;

                float rotation = Mathf.Repeat(this.Rotation.AsAngle + this.spotLightRotationBaseOffset, 360f);
                Command_Action turnLeftButton = new Command_Action();
                turnLeftButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_TurnLeft");
                turnLeftButton.defaultLabel = "Rotation: " + rotation + "°";
                turnLeftButton.defaultDesc = "Turn left.";
                turnLeftButton.activateSound = SoundDef.Named("Click");
                turnLeftButton.action = new Action(AddSpotlightBaseRotationLeftOffset);
                turnLeftButton.groupKey = groupKeyBase + 4;
                yield return turnLeftButton;

                Command_Action turnRightButton = new Command_Action();
                turnRightButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_TurnRight");
                turnRightButton.defaultLabel = "";
                turnRightButton.defaultDesc = "Turn right.";
                turnRightButton.activateSound = SoundDef.Named("Click");
                turnRightButton.action = new Action(AddSpotlightBaseRotationRightOffset);
                turnRightButton.groupKey = groupKeyBase + 5;
                yield return turnRightButton;
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
            PowerOffLight();

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
            // Lights
            // Check if turret is powered.
            if (!this.MountableComp.IsMounted)
            {
                PowerOffLight();
                ResetLight();
            }

            // Check locked target is still valid.
            if (this.CurrentTarget != null)
            {
                //   // Check target is still valid: not killed or downed and in sight.
                //   if (this.CurrentTarget.Thing.DestroyedOrNull()
                //       || (IsPawnValidTarget(this.target) == false))
                //   {
                //       // Target is no more valid.
                //       this.target = null;
                //   }
                // Target is valid.
                this.spotLightRotationTarget = Mathf.Round((this.CurrentTarget.Thing.Position - this.Position).AngleFlat);
                ComputeRotationDirection();
                this.spotLightRangeTarget = (this.CurrentTarget.Thing.Position - this.Position).ToVector3().magnitude;
            }
            else
            {
                // Reset idle tick counter.
                this.idlePauseTicks = idlePauseDurationInTicks;

                // fixed rotation
                IdleTurnTick();

            }
            // Update the spotlight rotation and range.
            SpotlightMotionTick();


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

            // Lights
            spotlightMatrix.SetTRS(drawLoc + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightScale);
            if (this.MountableComp.IsMounted)
            {
                Graphics.DrawMesh(MeshPool.plane10, spotlightMatrix, spotlightOnTexture, 0);
                spotlightLightEffectMatrix.SetTRS(drawLoc + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightLightEffectScale);
                Graphics.DrawMesh(MeshPool.plane10, spotlightLightEffectMatrix, spotlightLightEffectTexture, 0);
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, spotlightMatrix, spotlightOffTexture, 0);
            }

            if (Find.Selector.IsSelected(this)
                && (this.CurrentTarget != null))
            {
                Vector3 lineOrigin = this.TrueCenter();
                Vector3 lineTarget = this.CurrentTarget.Thing.Position.ToVector3Shifted();
                lineTarget.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
                lineOrigin.y = lineTarget.y;
                GenDraw.DrawLineBetween(lineOrigin, lineTarget, targetLineTexture);
            }
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