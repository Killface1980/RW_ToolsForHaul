namespace TFH_VehicleBase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using RimWorld;

    using TFH_VehicleBase.Components;
    using TFH_VehicleBase.Designators;

    using UnityEngine;

    using Verse;
    using Verse.AI;
    using Verse.Sound;

    public class Vehicle_Cart : Pawn, IThingHolder, ILoadReferenceable
    {
        #region Variables

        public override Color DrawColor
        {
            get
            {
                CompColorable comp = this.GetComp<CompColorable>();
                if (comp != null && comp.Active)
                {
                    return comp.Color;
                }

                return base.DrawColor;
            }

            set
            {
                this.SetColor(value, true);
            }
        }

        // ==================================
        public int DefaultMaxItem => (int)this.GetStatValue(HaulStatDefOf.VehicleMaxItem);

        public int MaxItemPerBodySize => (int)this.GetStatValue(HaulStatDefOf.VehicleMaxItem);

        public bool instantiated;

        public bool ThreatDisabled()
        {
            return !this.MountableComp.IsMounted;

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

        public virtual bool ClaimableBy(Faction claimee)
        {
            // No vehicles if enemy near
            if (base.Faction != null)
            {
                if (claimee != base.Faction)
                {
                    if (base.Faction.HostileTo(claimee))
                    {
                        foreach (IAttackTarget attackTarget in this.Map.attackTargetsCache.TargetsHostileToFaction(claimee))
                        {
                            if (attackTarget.Thing.Position.InHorDistOf(this.Position, 20f))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;

            // CompPowerTrader comp = this.GetComp<CompPowerTrader>();
            // if (comp == null || !comp.PowerOn)
            // {
            // CompMannable comp2 = this.GetComp<CompMannable>();
            // if (comp2 == null || !comp2.MannedNow)
            // {
            // return true;
            // }
            // }
        }

        public float DesiredSpeed => this.GetStatValue(StatDefOf.MoveSpeed);

        public bool IsCurrentlyMotorized()
        {
            return (this.RefuelableComp != null && this.RefuelableComp.HasFuel)
                   || this.VehicleComp.MotorizedWithoutFuel();
        }

        public bool CanExplode()
        {
            if (!this.ExplosiveTickers.NullOrEmpty())
            {
                for (int index = this.ExplosiveTickers.Count - 1; index >= 0; index--)
                {
                    HediffCompExplosive_TFH ticker = this.ExplosiveTickers[index];
                    if (ticker.CompShouldRemove)
                    {
                        this.ExplosiveTickers.Remove(ticker);
                    }
                }
            }
            return !this.ExplosiveTickers.NullOrEmpty();
        }

        public bool IsAboutToBlowUp()
        {
            if (this.CanExplode())
            {
                // TODO place hitpoints & maxhitpoints with calls to vehocle part health
                bool maximumDamage = this.HitPoints / this.MaxHitPoints < 0.4f;
                if ((this.IsBurning() && maximumDamage) || this.wickStarted)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAxles()
        {
            if (this.AxlesComp?.Props.axles.Count > 0)
            {
                return true;
            }

            return false;
        }

        public bool HasGasTank()
        {
            return this.GasTankComp != null;
        }

        public bool fueledByAI;

        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;

        // mount and storage data
        private ThingOwner<ThingWithComps> storage;

        public ThingOwner GetContainer()
        {
            return this.storage;
        }

        public IntVec3 GetPosition()
        {
            return this.Position;
        }

        // slotGroupParent Interface
        public ThingFilter allowances;

        public CompMountable MountableComp;

        public CompRefuelable RefuelableComp;

        public CompBreakdownable BreakdownableComp;

        public CompAxles AxlesComp;

        public CompVehicle VehicleComp;

        public CompGasTank GasTankComp;

        private Sustainer sustainerAmbient;

        public Vector3 DriverOffset = new Vector3();

        public CompDriver DriverComp { get; set; }

        public int MaxItem => this.MountableComp.IsMounted && this.MountableComp.Driver.RaceProps.Animal
                                  ? Mathf.CeilToInt(this.MountableComp.Driver.BodySize * this.DefaultMaxItem)
                                  : this.DefaultMaxItem;

        public int MaxStack => this.MaxItem * 100;

        #endregion

        #region Setup Work

        // public static ListerVehicles listerVehicles = new ListerVehicles();
#if Headlights
        HeadLights flooder;
#endif

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // Reload textures to get colored versions
            LongEventHandler.ExecuteWhenFinished(
                delegate
                    {
                        PawnKindLifeStage curKindLifeStage = this.ageTracker.CurKindLifeStage;
                        this.Drawer.renderer.graphics.nakedGraphic =
                            curKindLifeStage.bodyGraphicData.Graphic.GetColoredVersion(
                                ShaderDatabase.Cutout,
                                this.DrawColor,
                                this.DrawColorTwo);
                    });

            // To allow the vehicle to be drafted -> Still not possible to draft, because 1. not humanlike and 2. the GetGizmos in Pawn_Drafter is internal! 
            this.drafter = new Pawn_DraftController(this); // Maybe not needed because not usable?

            if (this.equipment == null)
            {
                this.equipment = new Pawn_EquipmentTracker(this);
            }

            this.MountableComp = this.TryGetComp<CompMountable>();

            this.RefuelableComp = this.TryGetComp<CompRefuelable>();

            // if (this.health.hediffSet.HasHediff(HediffDef.Named("Bomb")))
            // {
            // }
            this.BreakdownableComp = this.TryGetComp<CompBreakdownable>();

            this.AxlesComp = this.TryGetComp<CompAxles>();

            this.VehicleComp = this.TryGetComp<CompVehicle>();

            this.GasTankComp = this.TryGetComp<CompGasTank>();

            // Get the vehicles away from buildings
            // map.designationManager.Notify_BuildingDespawned(this);
            this.storage = new ThingOwner<ThingWithComps>(this, false);

            if (this.allowances == null)
            {
                this.allowances = new ThingFilter();
                this.allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
                this.allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
            }

            if (this.MountableComp.IsMounted)
            {
                this.AddDriverComp();
                this.StartSustainerVehicleIfInactive();
            }

            // Spotlight
            // this.updateOffsetInTicks = Rand.RangeInclusive(0, updatePeriodInTicks);
            // spotlightMatrix.SetTRS(base.DrawPos + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightScale);
        }

        public void AddDriverComp()
        {
            this.DriverComp = new CompDriver
            {
                Cart = this,
                Pawn = this.MountableComp.Driver,
                parent = this.MountableComp.Driver
            };
            this.MountableComp.Driver.AllComps.Add(this.DriverComp);
        }

        public void StartSustainerVehicleIfInactive()
        {
            CompVehicle vehicleComp = this.VehicleComp;
            if (vehicleComp != null && (!vehicleComp.compProps.soundAmbient.NullOrUndefined()
                                        && this.sustainerAmbient == null))
            {
                SoundInfo info = SoundInfo.InMap(this, MaintenanceType.None);
                this.sustainerAmbient = this.VehicleComp.compProps.soundAmbient.TrySpawnSustainer(info);
            }
        }

        public void EndSustainerVehicleIfActive()
        {
            if (this.sustainerAmbient != null)
            {
                this.sustainerAmbient.End();
                this.sustainerAmbient = null;
            }
        }

        public override void DeSpawn()
        {
            base.DeSpawn();

            this.EndSustainerVehicleIfActive();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref this.storage, "storage", this);
            Scribe_Deep.Look(ref this.allowances, "allowances");

            Scribe_Deep.Look(ref this.ExplosiveTickers, "explosiveTickers");
            Scribe_Values.Look<bool>(ref this.wickStarted, "wickStarted", false, false);

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
            // Getting the gizmos manually - no drafting, see SpawnSetup?

            Designator_ClaimVehicle des = new Designator_ClaimVehicle();

            if (this.Faction != Faction.OfPlayer)
            {
                if (this.ClaimableBy(Faction.OfPlayer))
                {
                    Command_Action command_Claim = new Command_Action();
                    command_Claim.defaultLabel = des.LabelCapReverseDesignating(this);
                    command_Claim.icon = des.IconReverseDesignating(this);
                    command_Claim.defaultDesc = des.DescReverseDesignating(this);
                    command_Claim.action = delegate
                        {
                            des.DesignateThing(this);
                            des.Finalize(true);
                        };
                    command_Claim.hotKey = des.hotKey;
                    command_Claim.groupKey = des.groupKey;

                    yield return command_Claim;
                }
                yield break;
            }
            CompForbiddable forbid = this.GetComp<CompForbiddable>();

            foreach (Gizmo gizmo in forbid.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (this.RefuelableComp != null)
            {
                foreach (Gizmo gizmo in this.RefuelableComp.CompGetGizmosExtra())
                {
                    yield return gizmo;
                }
            }

            if (!this.MountableComp.IsMounted)
            {
                Designator_Mount designator =
                    new Designator_Mount
                    {
                        vehicle = this,
                        defaultLabel = Static.TxtCommandMountLabel.Translate(),
                        defaultDesc = Static.TxtCommandMountDesc.Translate(),
                        icon = Static.IconMount,
                        activateSound = Static.ClickSound
                    };
                yield return designator;
            }
            else
            {
                if (this.MountableComp.Driver != null)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = Static.TxtCommandDismountLabel.Translate(),
                        defaultDesc = Static.TxtCommandDismountDesc.Translate(),
                        icon = Static.IconUnmount,
                        activateSound = Static.ClickSound,
                        action = delegate
                            {
                                TFH_BaseUtility.DismountGizmoFloatMenu(
                                    this.MountableComp.Driver);
                            }
                    };
                }
            }

            // Get explosive Gizmos
            if (this.CanExplode())
            {
                foreach (HediffCompExplosive_TFH ticker in this.ExplosiveTickers)
                {
                    foreach (Gizmo c in ticker.ActionExplode())
                    {
                        yield return c;
                    }
                }
            }

            if (this.equipment.Primary != null)
            {
                if (this.equipment.Primary.def.IsRangedWeapon)
                {
                    Command_Toggle draft = new Command_Toggle
                    {
                        hotKey = KeyBindingDefOf.CommandColonistDraft,
                        isActive = () => this.Drafted,
                        toggleAction = delegate
                            {
                                this.drafter.Drafted = !this.Drafted;
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(
                                    ConceptDefOf.Drafting,
                                    KnowledgeAmount.SpecificInteraction);
                            },
                        defaultDesc = "CommandToggleDraftDesc".Translate(),
                        icon = TexCommand.Draft,
                        turnOnSound = SoundDefOf.DraftOn,
                        turnOffSound = SoundDefOf.DraftOff
                    };
                    if (!this.Drafted)
                    {
                        draft.defaultLabel = "CommandDraftLabel".Translate();
                    }

                    if (this.Downed)
                    {
                        draft.Disable("IsIncapped".Translate(new object[] { this.NameStringShort }));
                    }

                    if (!this.Drafted)
                    {
                        draft.tutorTag = "Draft";
                    }
                    else
                    {
                        draft.tutorTag = "Undraft";
                    }

                    yield return draft;

                    if (!this.drafter.Drafted)
                    {
                        yield break;
                    }

                    Command_VerbTarget gizmos =
                        new Command_VerbTarget
                        {
                            defaultLabel = "CommandSetForceAttackTarget".Translate(),
                            defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                            verb = this.equipment.PrimaryEq.PrimaryVerb,
                            hotKey = KeyBindingDefOf.Misc4
                        };

                    yield return gizmos;

                    // if (this.CurJob.targetA.IsValid)
                    // {
                    // Command_Action stop = new Command_Action
                    // {
                    // defaultLabel = "CommandStopForceAttack".Translate(),
                    // defaultDesc = "CommandStopForceAttackDesc".Translate(),
                    // icon = ContentFinder<Texture2D>.Get(
                    // "UI/Commands/Halt",
                    // true),
                    // action = delegate
                    // {
                    // this.ResetForcedTarget();
                    // SoundDefOf.TickLow.PlayOneShotOnCamera(null);
                    // }
                    // };
                    // if (!this.CurJob.targetA.IsValid)
                    // {
                    // stop.Disable("CommandStopAttackFailNotForceAttacking".Translate());
                    // }
                    // stop.hotKey = KeyBindingDefOf.Misc5;
                    // yield return stop;
                    // }
                    yield return new Command_Toggle
                    {
                        hotKey = KeyBindingDefOf.Misc6,
                        isActive = () => this.drafter.FireAtWill,
                        toggleAction =
                                             delegate
                                                 {
                                                     this.drafter.FireAtWill = !this.drafter.FireAtWill;
                                                 },
                        icon = TexCommand.FireAtWill,
                        defaultLabel = "CommandFireAtWillLabel".Translate(),
                        defaultDesc = "CommandFireAtWillDesc".Translate(),
                        tutorTag = "FireAtWillToggle"
                    };
                }
            }

            // Spotlight
            // int groupKeyBase = 700000102;
            // Command_Action lightModeButton = new Command_Action();
            // switch (this.lightMode)
            // {
            // case (LightMode.Conic):
            // lightModeButton.defaultLabel = "Ligth mode: conic.";
            // lightModeButton.defaultDesc = "In this mode, the spotlight turret patrols in a conic area in front of it. Automatically lock on hostiles.";
            // break;
            // case (LightMode.Automatic):
            // lightModeButton.defaultLabel = "Ligth mode: automatic.";
            // lightModeButton.defaultDesc = "In this mode, the spotlight turret randomly lights the surroundings. Automatically lock on hostiles.";
            // break;
            // case (LightMode.Fixed):
            // lightModeButton.defaultLabel = "Ligth mode: fixed.";
            // lightModeButton.defaultDesc = "In this mode, the spotlight turret only light a fixed area. Does NOT automatically lock on hostiles.";
            // break;
            // }
            // lightModeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_SwitchMode");
            // lightModeButton.activateSound = Static.ClickSound;
            // lightModeButton.action = new Action(SwitchLigthMode);
            // lightModeButton.groupKey = groupKeyBase + 1;
            // yield return lightModeButton;
            // if ((this.lightMode == LightMode.Conic)
            // || (this.lightMode == LightMode.Fixed))
            // {
            // Command_Action decreaseRangeButton = new Command_Action();
            // decreaseRangeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_DecreaseRange");
            // decreaseRangeButton.defaultLabel = "Range: " + this.spotLightRangeBaseOffset;
            // decreaseRangeButton.defaultDesc = "Decrease range.";
            // decreaseRangeButton.activateSound = Static.ClickSound;
            // decreaseRangeButton.action = new Action(DecreaseSpotlightRange);
            // decreaseRangeButton.groupKey = groupKeyBase + 2;
            // yield return decreaseRangeButton;
            // Command_Action increaseRangeButton = new Command_Action();
            // increaseRangeButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_IncreaseRange");
            // increaseRangeButton.defaultLabel = "";
            // increaseRangeButton.defaultDesc = "Increase range.";
            // increaseRangeButton.activateSound = Static.ClickSound;
            // increaseRangeButton.action = new Action(IncreaseSpotlightRange);
            // increaseRangeButton.groupKey = groupKeyBase + 3;
            // yield return increaseRangeButton;
            // float rotation = Mathf.Repeat(this.Rotation.AsAngle + this.spotLightRotationBaseOffset, 360f);
            // Command_Action turnLeftButton = new Command_Action();
            // turnLeftButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_TurnLeft");
            // turnLeftButton.defaultLabel = "Rotation: " + rotation + "°";
            // turnLeftButton.defaultDesc = "Turn left.";
            // turnLeftButton.activateSound = Static.ClickSound;
            // turnLeftButton.action = new Action(AddSpotlightBaseRotationLeftOffset);
            // turnLeftButton.groupKey = groupKeyBase + 4;
            // yield return turnLeftButton;
            // Command_Action turnRightButton = new Command_Action();
            // turnRightButton.icon = ContentFinder<Texture2D>.Get("UI/Commands/CommandButton_TurnRight");
            // turnRightButton.defaultLabel = "";
            // turnRightButton.defaultDesc = "Turn right.";
            // turnRightButton.activateSound = Static.ClickSound;
            // turnRightButton.action = new Action(AddSpotlightBaseRotationRightOffset);
            // turnRightButton.groupKey = groupKeyBase + 5;
            // yield return turnRightButton;
            // }

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

        private void ResetForcedTarget()
        {
            this.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            // do nothing if not of colony
            if (this.Faction != Faction.OfPlayer)
            {
                FloatMenuOption failer = new FloatMenuOption(
                    "NotPlayerFaction".Translate(this.LabelCap),
                    null,
                    MenuOptionPriority.Default,
                    null,
                    null,
                    0f,
                    null,
                    null);
                yield return failer;
                yield break;
            }

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
            {
                yield return fmo;
            }

            Map map = myPawn.Map;

            Action action_MakeMount = () =>
                {
                    Pawn worker = null;
                    Job jobNew = new Job(VehicleJobDefOf.MakeMount);
                    map.reservationManager.ReleaseAllForTarget(this);
                    jobNew.count = 1;
                    jobNew.targetA = this;
                    jobNew.targetB = myPawn;
                    foreach (Pawn colonyPawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
                    {
                        if (colonyPawn.CurJob.def != jobNew.def
                            && (worker == null || (worker.Position - myPawn.Position).LengthHorizontal
                                > (colonyPawn.Position - myPawn.Position).LengthHorizontal))
                        {
                            worker = colonyPawn;
                        }
                    }

                    if (worker == null)
                    {
                        Messages.Message("NoWorkForMakeMount".Translate(), MessageSound.RejectInput);
                    }
                    else
                    {
                        worker.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    }
                };

            Action action_Deconstruct = () =>
                {
                    map.reservationManager.ReleaseAllForTarget(this);
                    map.reservationManager.Reserve(myPawn, this);
                    map.designationManager.AddDesignation(new Designation(this, DesignationDefOf.Deconstruct));
                    Job job = new Job(JobDefOf.Deconstruct, this);
                    myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                };

            Action action_Mount = () =>
                {
                    Job jobNew = new Job(VehicleJobDefOf.Mount);
                    myPawn.Map.reservationManager.ReleaseAllForTarget(this);
                    myPawn.Map.reservationManager.Reserve(myPawn, this);
                    jobNew.targetA = this;
                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_DismountInBase = () =>
                {
                    Job jobNew = myPawn.DismountAtParkingLot("VC GFMO");

                    myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            if (!this.MountableComp.IsMounted)
            {
                if (!this.IsForbidden(Faction.OfPlayer))
                {
                    if (myPawn.RaceProps.Humanlike && !TFH_BaseUtility.IsDriverOfThisVehicle(myPawn, this))
                    {
                        yield return new FloatMenuOption("MountOn".Translate(this.LabelShort), action_Mount);
                    }

                    bool flag = this.Map.HasFreeCellsInParkingLot();

                    if (flag)
                    {
                        yield return new FloatMenuOption(
                            "DismountAtParkingLot".Translate(this.LabelShort),
                            action_DismountInBase);
                    }
                    else
                    {
                        FloatMenuOption failer = new FloatMenuOption(
                            "NoFreeParkingSpace".Translate(this.LabelShort),
                            null,
                            MenuOptionPriority.Default,
                            null,
                            null,
                            0f,
                            null,
                            null);
                        yield return failer;
                    }
                }

                yield return new FloatMenuOption("Deconstruct".Translate(this.LabelShort), action_Deconstruct);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // PowerOffLight();
            if (mode == DestroyMode.Deconstruct)
            {
                mode = DestroyMode.KillFinalize;
            }
            else if (this.wickStarted)
            {
                this.storage?.ClearAndDestroyContents();
            }

            this.storage?.TryDropAll(this.Position, this.Map, ThingPlaceMode.Near);

            base.Destroy(mode);
        }

        #endregion

        #region Ticker

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (!this.ExplosiveTickers.NullOrEmpty())
            {
                foreach (HediffCompExplosive_TFH ticker in this.ExplosiveTickers)
                {
                    ticker.PostPostApplyDamage(dinfo);
                }
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (!this.Spawned || this.Dead)
            {
                return;

            }

            if (!this.instantiated)
            {
                this.VehicleComp.currentDriverSpeed = this.VehicleComp.VehicleSpeed;
                this.instantiated = true;
            }

            // this.innerContainer.ThingOwnerTick();

            // Lights
            // Check if turret is powered.
            // if (!this.MountableComp.IsMounted)
            // {
            // PowerOffLight();
            // ResetLight();
            // }
            // // Check locked target is still valid.
            // if (this.CurrentTarget != null)
            // {
            // //   // Check target is still valid: not killed or downed and in sight.
            // //   if (this.CurrentTarget.Thing.DestroyedOrNull()
            // //       || (IsPawnValidTarget(this.target) == false))
            // //   {
            // //       // Target is no more valid.
            // //       this.target = null;
            // //   }
            // // Target is valid.
            // this.spotLightRotationTarget = Mathf.Round((this.CurrentTarget.Thing.Position - this.Position).AngleFlat);
            // ComputeRotationDirection();
            // this.spotLightRangeTarget = (this.CurrentTarget.Thing.Position - this.Position).ToVector3().magnitude;
            // }
            // else
            // {
            // // Reset idle tick counter.
            // this.idlePauseTicks = idlePauseDurationInTicks;
            // // fixed rotation
            // IdleTurnTick();
            // }
            // // Update the spotlight rotation and range.
            // SpotlightMotionTick();
            if (this.MountableComp.IsMounted)
            {
                if (this.RefuelableComp != null)
                {
                    if (this.MountableComp.Driver.Faction != Faction.OfPlayer)
                    {
                        if (!this.fueledByAI)
                        {
                            if (this.RefuelableComp.FuelPercentOfMax < 0.550000011920929)
                            {
                                this.RefuelableComp.Refuel(
                                    ThingMaker.MakeThing(
                                        this.RefuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                            }
                            else
                            {
                                this.fueledByAI = true;
                            }
                        }
                    }
                }
            }

            // if (Find.TickManager.TicksGame >= damagetick)
            // {
            // TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1, null, null, null));
            // damagetick = Find.TickManager.TicksGame + 3600;
            // }
        }

        #endregion

        #region Graphics / Inspections

        public override Vector3 DrawPos
        {
            get
            {
                if (!this.Spawned || !this.instantiated)
                {
                    return base.DrawPos;
                }

                if (!this.MountableComp.IsMounted)
                {
                    return base.DrawPos;
                }

                float num = this.MountableComp.Driver.Drawer.renderer.graphics.nakedGraphic.drawSize.x - 1f;
                num *= this.MountableComp.Driver.Rotation.AsInt % 2 == 1 ? 0.5f : 0.25f;
                Vector3 vector = new Vector3(0f, 0f, -num);

                // vector += DriverOffset;
                return this.MountableComp.Position + vector.RotatedBy(this.MountableComp.Driver.Rotation.AsAngle);
            }
        }

        public bool wickStarted;

        private bool fireAtWillInt = true;

        public List<HediffCompExplosive_TFH> ExplosiveTickers;

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc);

            if (!this.Spawned)
            {
                return;
            }

            if (this.VehicleComp.ShowsStorage())
            {
                if (!this.storage.InnerListForReading.NullOrEmpty() || (this.MountableComp.IsMounted
                                                  && this.MountableComp.Driver.RaceProps.packAnimal
                                                  && this.MountableComp.Driver.RaceProps.Animal))
                {
                    Vector3 mountThingLoc = drawLoc;
                    if (this.Rotation.AsInt % 2 == 1)
                    {
                        mountThingLoc.y = Altitudes.AltitudeFor(AltitudeLayer.LayingPawn); // horizontal
                        mountThingLoc.z += 0.1f;
                    }
                    else
                    {
                        mountThingLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.07f; // vertical
                    }

                    Vector3 mountThingOffset =
                        (-0.3f * this.def.interactionCellOffset.ToVector3()).RotatedBy(this.Rotation.AsAngle);
                    if (this.MountableComp.IsMounted)
                    {
                        mountThingOffset =
                            (-0.3f * this.def.interactionCellOffset.ToVector3()).RotatedBy(this.Rotation.AsAngle);
                    }

                    if (this.MountableComp.Driver.RaceProps.packAnimal && this.MountableComp.Driver.RaceProps.Animal)
                    {
                        if (this.MountableComp.IsMounted
                            && this.MountableComp.Driver.inventory.innerContainer.Count > 0)
                        {
                            foreach (Thing mountThing in this.MountableComp.Driver.inventory.innerContainer)
                            {
                                mountThing.Rotation = this.Rotation;
                                mountThing.DrawAt(mountThingLoc + mountThingOffset);
                            }
                        }
                    }
                    else if (!this.storage.InnerListForReading.NullOrEmpty())
                    {
                        foreach (Thing mountThing in this.storage.InnerListForReading)
                        {
                            mountThing.Rotation = this.Rotation;
                            mountThing.DrawAt(mountThingLoc + mountThingOffset);
                        }
                    }
                }
            }

            if (this.CanExplode())
            {
                if (this.wickStarted)
                {
                    this.Map.overlayDrawer.DrawOverlay(this, OverlayTypes.BurningWick);
                }
            }

            // // Lights
            // spotlightMatrix.SetTRS(drawLoc + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightScale);
            // if (this.MountableComp.IsMounted)
            // {
            // Graphics.DrawMesh(MeshPool.plane10, spotlightMatrix, spotlightOnTexture, 0);
            // spotlightLightEffectMatrix.SetTRS(drawLoc + Altitudes.AltIncVect, this.spotLightRotation.ToQuat(), spotlightLightEffectScale);
            // Graphics.DrawMesh(MeshPool.plane10, spotlightLightEffectMatrix, spotlightLightEffectTexture, 0);
            // }
            // else
            // {
            // Graphics.DrawMesh(MeshPool.plane10, spotlightMatrix, spotlightOffTexture, 0);
            // }
            // if (Find.Selector.IsSelected(this)
            // && (this.CurrentTarget != null))
            // {
            // Vector3 lineOrigin = this.TrueCenter();
            // Vector3 lineTarget = this.CurrentTarget.Thing.Position.ToVector3Shifted();
            // lineTarget.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            // lineOrigin.y = lineTarget.y;
            // GenDraw.DrawLineBetween(lineOrigin, lineTarget, targetLineTexture);
            // }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());

            if (this.HasGasTank())
            {
                if (this.GasTankComp.tankLeaking)
                {
                    stringBuilder.Append(" - " + "TankIsLeaking".Translate());
                    stringBuilder.Append(" - ");
                }
            }

            string currentDriverString;
            if (this.MountableComp.IsMounted)
            {
                currentDriverString = "Driver".Translate() + ": " + this.MountableComp.Driver.LabelCap;
            }
            else
            {
                currentDriverString = "NoDriver".Translate();
            }

            stringBuilder.AppendLine(currentDriverString);

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
            return this.storage;
        }

        public bool InParkingLot => this.Position.GetZone(this.Map) is Zone_ParkingLot;
    }
}