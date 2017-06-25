using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ToolsForHaul.JobDefs;
using ToolsForHaul.StatDefs;
using ToolsForHaul.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;
using Random = UnityEngine.Random;

namespace ToolsForHaul.Components
{
    using ppumkin.LEDTechnology.Managers;

    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.Components.Vehicles;

    public class CompVehicle : ThingComp
    {
        public CompProperties_Vehicle compProps => (CompProperties_Vehicle)this.props;

        public bool ShowsStorage()
        {
            return this.compProps.showsStorage;
        }

        public bool AnimalsCanDrive()
        {
            return this.compProps.animalsCanDrive;
        }

        public bool IsMedical()
        {
            return this.compProps.isMedical;
        }

        public bool MotorizedWithoutFuel()
        {
            return this.compProps.motorizedWithoutFuel;
        }

        public float FuelCatchesFireHitPointsPercent()
        {
            return this.compProps.fuelCatchesFireHitPointsPercent;
        }

        public bool LeaveTrail()
        {
            return this.compProps.leaveTrail;
        }

        public float currentDriverSpeed;

        public float DesiredSpeed => this.parent.GetStatValue(HaulStatDefOf.VehicleSpeed);

        public bool IsCurrentlyMotorized()
        {
            CompRefuelable refuelableComp = this.parent.TryGetComp<CompRefuelable>();
            return (refuelableComp != null && refuelableComp.HasFuel) || this.MotorizedWithoutFuel();
        }

        public float VehicleSpeed;
        public bool despawnAtEdge;

        public bool fueledByAI;
        public bool tankLeaking;
        public float _tankHitPos = 1f;
        private int tankHitCount;
        private int _tankSpillTick = -5000;
        public ThingDef fuelDefName = ThingDef.Named("FilthFuel");
        private Graphic_Shadow shadowGraphic;
        private int tickCheck = Find.TickManager.TicksGame;
        private readonly int tickCooldown = 60;

        private Vector3 _lastTireTrackPlacePos;
        private const float FootprintIntervalDist = 0.7f;
        private static readonly Vector3 TrackOffset = new Vector3(0f, 0f, -0.3f);
        private static readonly Vector3 DustOffset = new Vector3(-0.3f, 0f, -0.3f);
        private static readonly Vector3 FumesOffset = new Vector3(-0.3f, 0f, 0f);

      //  private HeadLights flooder;

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (!this.parent.Spawned)
                return;
            // todo add repair
            if ( this.tankLeaking)
            {
                this.tankLeaking = false;
                this._tankHitPos = 1f;

                // if (breakdownableComp.BrokenDown)
                // breakdownableComp.Notify_Repaired();
                return;
            }

            float hitpointsPercent = (float)this.parent.HitPoints / this.parent.MaxHitPoints;


            if (hitpointsPercent <= 0.35f)
            {
                if (this.IsCurrentlyMotorized())
                    MoteMakerTFH.ThrowMicroSparks(this.parent.DrawPos, this.parent.Map);
            }

            if (dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Bomb)
            {
                return;
            }

            bool makeHole = false;

            if (!this.MotorizedWithoutFuel())
            {
                CompRefuelable refuelableComp = this.parent.TryGetComp<CompRefuelable>();
                if (dinfo.Def == DamageDefOf.Deterioration && Rand.Value > 0.5f)
                {
                    if (hitpointsPercent < 0.35f)
                    {
                        this.tankLeaking = true;
                        this.tankHitCount += 1;
                        this._tankHitPos = Math.Min(this._tankHitPos, Rand.Value);

                        int splash = (int)(refuelableComp.FuelPercentOfMax - this._tankHitPos * 20);

                        FilthMaker.MakeFilth(this.parent.Position, this.parent.Map,this.fuelDefName, this.parent.LabelCap, splash);
                    }

                    if (hitpointsPercent < 0.05f && Rand.Value > 0.5f)
                    {
                        FireUtility.TryStartFireIn(this.parent.Position,this.parent.Map, 0.1f);
                    }

                    return;
                }

                if (refuelableComp != null && refuelableComp.HasFuel)
                {
                    if (hitpointsPercent < this.FuelCatchesFireHitPointsPercent() && Rand.Value > 0.5f)
                    {
                        if (!this.tankLeaking)
                        {
                            refuelableComp.ConsumeFuel(1f);
                            FilthMaker.MakeFilth(this.parent.Position, this.parent.Map, this.fuelDefName, this.parent.LabelCap, 6);
                            makeHole = true;
                        }

                        FireUtility.TryStartFireIn(this.parent.Position, this.parent.Map, 0.1f);
                    }
                }

                if (Random.value <= 0.1f || makeHole)
                {
                    this.tankLeaking = true;
                    this.tankHitCount += 1;
                    this._tankHitPos = Math.Min(this._tankHitPos, Rand.Value);

                    int splash = (int)(refuelableComp.FuelPercentOfMax - this._tankHitPos * 20);

                    FilthMaker.MakeFilth(this.parent.Position, this.parent.Map, this.fuelDefName, this.parent.LabelCap, splash);
                }

            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref this.tankLeaking, "tankLeaking");
            Scribe_Values.Look(ref this._tankHitPos, "tankHitPos");
            Scribe_Values.Look(ref this.despawnAtEdge, "despawnAtEdge");
        }

        public override void CompTick()
        {

#if Headlights
            {
                if ( this.parent.Map.glo Find.GlowGrid.GameGlowAt(Position - Rotation.FacingCell - Rotation.FacingCell) < 0.4f)
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


            CompMountable mountableComp = this.parent.TryGetComp<CompMountable>();
            CompRefuelable refuelableComp = this.parent.TryGetComp<CompRefuelable>();
            CompAxles axlesComp = this.parent.TryGetComp<CompAxles>();
            CompBreakdownable breakdownableComp = this.parent.TryGetComp<CompBreakdownable>();

            if (mountableComp.IsMounted)
            {
                if (refuelableComp != null)
                {
                    if (mountableComp.Driver.Faction != Faction.OfPlayer)
                        if (!this.fueledByAI)
                        {
                            if (refuelableComp.FuelPercentOfMax < 0.550000011920929)
                                refuelableComp.Refuel(
                                    ThingMaker.MakeThing(
                                        refuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                            else this.fueledByAI = true;
                        }
                }

                if (mountableComp.Driver.pather.Moving)
                {
                    Vector3 pos = this.parent.DrawPos;
                    if (this.parent.Map.terrainGrid.TerrainAt(pos.ToIntVec3()).takeFootprints
                        || this.parent.Map.snowGrid.GetDepth(pos.ToIntVec3()) > 0.2f)
                    {
                        if (this.LeaveTrail())
                        {
                            Vector3 normalized = (pos - this._lastTireTrackPlacePos).normalized;
                            float rot = normalized.AngleFlat();
                            Vector3 loc = pos + TrackOffset;

                            if ((loc - this._lastTireTrackPlacePos).MagnitudeHorizontalSquared() > FootprintIntervalDist)
                            {
                                MoteMakerTFH.PlaceTireTrack(loc,this.parent.Map, rot, pos);
                                this._lastTireTrackPlacePos = pos;
                            }
                        }

                        if (axlesComp.HasAxles())
                        {
                            MoteMakerTFH.ThrowDustPuff(
                                pos + DustOffset,
                                this.parent.Map,
                                0.15f + Mathf.InverseLerp(0, 50, this.currentDriverSpeed) * 0.6f);
                        }
                        else
                        {
                            MoteMakerTFH.ThrowDustPuff(
                                pos + DustOffset,
                                this.parent.Map,
                                0.15f + Mathf.InverseLerp(0, 50, this.VehicleSpeed) * 0.6f);
                        }
                    }
                }

                if (Find.TickManager.TicksGame - this.tickCheck >= this.tickCooldown)
                {
                    if (mountableComp.Driver.pather.Moving)
                    {
                        if (!mountableComp.Driver.stances.FullBodyBusy)
                        {
                            if (refuelableComp != null)
                            {
                                refuelableComp.Notify_UsedThisTick();
                            }

                            if (axlesComp.HasAxles())
                            {
                                this.currentDriverSpeed = ToolsForHaulUtility.GetMoveSpeed(mountableComp.Driver);
                            }
                        }

                        if (breakdownableComp != null && breakdownableComp.BrokenDown
                            || refuelableComp != null && !refuelableComp.HasFuel)
                        {
                            this.VehicleSpeed = 0.75f;
                        }
                        else
                        {
                            this.VehicleSpeed = this.DesiredSpeed;
                        }
                        this.tickCheck = Find.TickManager.TicksGame;
                    }

                    if (this.parent.Position.InNoBuildEdgeArea(this.parent.Map) && this.despawnAtEdge && this.parent.Spawned
                        && (mountableComp.Driver.Faction != Faction.OfPlayer
                            || mountableComp.Driver.MentalState.def == MentalStateDefOf.PanicFlee))
                    {
                        this.parent.DeSpawn();
                    }
                }

                // Exhaustion fumes - basic
                // only fumes on vehicles with combustion and no animals driving
                if (!this.MotorizedWithoutFuel() && !this.AnimalsCanDrive())
                {
                    MoteMakerTFH.ThrowSmoke(this.parent.DrawPos + FumesOffset, this.parent.Map, 0.05f + this.currentDriverSpeed * 0.01f);
                }
            }

            if (this.tankLeaking)
            {
                if (Find.TickManager.TicksGame > this._tankSpillTick)
                {
                    if (refuelableComp.FuelPercentOfMax > this._tankHitPos)
                    {
                        refuelableComp.ConsumeFuel(0.15f);

                        FilthMaker.MakeFilth(this.parent.Position, this.parent.Map,this.fuelDefName, this.parent.LabelCap);
                        this._tankSpillTick = Find.TickManager.TicksGame + 15;
                    }
                }
            }

            base.CompTick();
        }



        public override void PostDraw()
        {
            if (this.compProps.specialShadowData != null)
            {
                if (this.shadowGraphic == null)
                {
                    this.shadowGraphic = new Graphic_Shadow(this.compProps.specialShadowData);
                }

                this.shadowGraphic.Draw(this.parent.DrawPos, Rot4.North, this.parent);
            }

            base.PostDraw();
        }

    }
}
