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
    public class CompVehicle : ThingComp
    {
        public CompProperties_Vehicle compProps
        {
            get
            {
                return (CompProperties_Vehicle)props;
            }
        }

        public bool ShowsStorage()
        {
            return compProps.showsStorage;
        }

        public bool AnimalsCanDrive()
        {
            return compProps.animalsCanDrive;
        }

        public bool IsMedical()
        {
            return compProps.isMedical;
        }

        public bool MotorizedWithoutFuel()
        {
            return compProps.motorizedWithoutFuel;
        }

        public float FuelCatchesFireHitPointsPercent()
        {
            return compProps.fuelCatchesFireHitPointsPercent;
        }

        public bool LeaveTrail()
        {
            return compProps.leaveTrail;
        }

        public float currentDriverSpeed;

        public float DesiredSpeed
        {
            get { return parent.GetStatValue(HaulStatDefOf.VehicleSpeed); }
        }

        public bool IsCurrentlyMotorized()
        {
            CompRefuelable refuelableComp = parent.TryGetComp<CompRefuelable>();
            return (refuelableComp != null && refuelableComp.HasFuel) || MotorizedWithoutFuel();
        }

        public float VehicleSpeed;
        public bool despawnAtEdge;

        public bool fueledByAI;
        public bool tankLeaking;
        public float _tankHitPos = 1f;
        private int tankHitCount;
        private int _tankSpillTick = -5000;
        public ThingDef fuelDefName = ThingDef.Named("Puddle_BioDiesel_Fuel");
        private Graphic_Shadow shadowGraphic;
        private int tickCheck = Find.TickManager.TicksGame;
        private readonly int tickCooldown = 60;

        private Vector3 _lastFootprintPlacePos;
        private const float FootprintIntervalDist = 0.7f;
        private static readonly Vector3 TrailOffset = new Vector3(0f, 0f, -0.3f);
        private static readonly Vector3 DustOffset = new Vector3(-0.3f, 0f, -0.3f);
        private static readonly Vector3 FumesOffset = new Vector3(-0.3f, 0f, 0f);


        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (!parent.Spawned)
                return;

            if (dinfo.Def == DamageDefOf.Repair && tankLeaking)
            {
                tankLeaking = false;
                _tankHitPos = 1f;
                //if (breakdownableComp.BrokenDown)
                //    breakdownableComp.Notify_Repaired();
                return;
            }


            float hitpointsPercent = (float)parent.HitPoints / parent.MaxHitPoints;


            if (hitpointsPercent <= 0.35f)
            {
                if (IsCurrentlyMotorized())
                    MoteMaker.ThrowMicroSparks(parent.DrawPos);
            }

            if (dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Repair || dinfo.Def == DamageDefOf.Bomb)
            {
                return;
            }

            bool makeHole = false;

            if (!MotorizedWithoutFuel())
            {
                CompRefuelable refuelableComp = parent.TryGetComp<CompRefuelable>();
                if (dinfo.Def == DamageDefOf.Deterioration && Rand.Value > 0.5f)
                {
                    if (hitpointsPercent < 0.35f)
                    {
                        tankLeaking = true;
                        tankHitCount += 1;
                        _tankHitPos = Math.Min(_tankHitPos, Rand.Value);

                        int splash = (int)(refuelableComp.FuelPercent - _tankHitPos * 20);

                        FilthMaker.MakeFilth(parent.Position, fuelDefName, parent.LabelCap, splash);
                    }
                    if (hitpointsPercent < 0.05f && Rand.Value > 0.5f)
                    {
                        FireUtility.TryStartFireIn(parent.Position, 0.1f);
                    }
                    return;
                }

                if (refuelableComp != null && refuelableComp.HasFuel)
                {
                    if (hitpointsPercent < FuelCatchesFireHitPointsPercent() && Rand.Value > 0.5f)
                    {
                        if (!tankLeaking)
                        {
                            refuelableComp.ConsumeFuel(1f);
                            FilthMaker.MakeFilth(parent.Position, fuelDefName, parent.LabelCap, 6);
                            makeHole = true;
                        }
                        FireUtility.TryStartFireIn(parent.Position, 0.1f);
                    }
                }

                if (Random.value <= 0.1f || makeHole)
                {
                    tankLeaking = true;
                    tankHitCount += 1;
                    _tankHitPos = Math.Min(_tankHitPos, Rand.Value);

                    int splash = (int)(refuelableComp.FuelPercent - _tankHitPos * 20);

                    FilthMaker.MakeFilth(parent.Position, fuelDefName, parent.LabelCap, splash);
                }

            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.LookValue(ref tankLeaking, "tankLeaking");
            Scribe_Values.LookValue(ref _tankHitPos, "tankHitPos");
            Scribe_Values.LookValue(ref despawnAtEdge, "despawnAtEdge");
        }

        public override void CompTick()
        {
            #region Headlights
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
            #endregion

            CompMountable mountableComp = parent.TryGetComp<CompMountable>();
            CompRefuelable refuelableComp = parent.TryGetComp<CompRefuelable>();
            CompAxles axlesComp = parent.TryGetComp<CompAxles>();
            CompBreakdownable breakdownableComp = parent.TryGetComp<CompBreakdownable>();

            if (mountableComp.IsMounted)
            {

                if (refuelableComp != null)
                {
                    if (mountableComp.Driver.Faction != Faction.OfPlayer)
                        if (!fueledByAI)
                        {
                            if (refuelableComp.FuelPercent < 0.550000011920929)
                                refuelableComp.Refuel(
                                    ThingMaker.MakeThing(refuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                            else
                                fueledByAI = true;
                        }
                }

                if (mountableComp.Driver.pather.Moving)
                {
                    Vector3 pos = parent.DrawPos;
                    if (Find.TerrainGrid.TerrainAt(pos.ToIntVec3()).takeFootprints || Find.SnowGrid.GetDepth(pos.ToIntVec3()) > 0.2f)
                    {
                        if (LeaveTrail())
                        {

                            Vector3 normalized = (pos - _lastFootprintPlacePos).normalized;
                            float rot = normalized.AngleFlat();
                            Vector3 loc = pos + TrailOffset;

                            if ((loc - _lastFootprintPlacePos).MagnitudeHorizontalSquared() > FootprintIntervalDist)
                                if (loc.ShouldSpawnMotesAt() && !MoteCounter.SaturatedLowPriority)
                                {
                                    MoteThrown moteThrown =
                                        (MoteThrown)ThingMaker.MakeThing(ThingDef.Named("Mote_Trail_ATV"));
                                    moteThrown.exactRotation = rot;
                                    moteThrown.exactPosition = loc;
                                    GenSpawn.Spawn(moteThrown, loc.ToIntVec3());
                                    _lastFootprintPlacePos = pos;
                                }
                        }
                        if (axlesComp.HasAxles())
                        {
                            MoteMaker.ThrowDustPuff(pos + DustOffset,
                                0.15f + Mathf.InverseLerp(0, 50, currentDriverSpeed)*0.6f);
                        }
                        else
                        {
                            MoteMaker.ThrowDustPuff(pos + DustOffset, 0.15f + Mathf.InverseLerp(0, 50, VehicleSpeed) * 0.6f);
                        }
                    }

                }

                if (Find.TickManager.TicksGame - tickCheck >= tickCooldown)
                {
                    if (mountableComp.Driver.pather.Moving)
                    {
                        if (!mountableComp.Driver.stances.FullBodyBusy)
                        {
                            if (refuelableComp != null)
                                refuelableComp.Notify_UsedThisTick();

                            if (axlesComp.HasAxles())
                                currentDriverSpeed = ToolsForHaulUtility.GetMoveSpeed(mountableComp.Driver);
                        }
                        if (breakdownableComp != null && breakdownableComp.BrokenDown ||
                            refuelableComp != null && !refuelableComp.HasFuel)
                            VehicleSpeed = 0.75f;
                        else VehicleSpeed = DesiredSpeed;
                        tickCheck = Find.TickManager.TicksGame;
                    }

                    if (parent.Position.InNoBuildEdgeArea() && despawnAtEdge && parent.Spawned && (mountableComp.Driver.Faction != Faction.OfPlayer || mountableComp.Driver.MentalState.def == MentalStateDefOf.PanicFlee))
                        parent.DeSpawn();
                }
                //Exhaustion fumes - basic
                // only fumes on vehicles with combustion and no animals driving
                if (!MotorizedWithoutFuel() && !AnimalsCanDrive())
                    MoteMaker.ThrowSmoke(parent.DrawPos + FumesOffset, 0.05f + currentDriverSpeed * 0.01f);


            }

            if (tankLeaking)
            {
                if (Find.TickManager.TicksGame > _tankSpillTick)
                {
                    if (refuelableComp.FuelPercent > _tankHitPos)
                    {
                        refuelableComp.ConsumeFuel(0.15f);

                        FilthMaker.MakeFilth(parent.Position, fuelDefName, parent.LabelCap);
                        _tankSpillTick = Find.TickManager.TicksGame + 15;
                    }
                }
            }
            base.CompTick();
        }

        public override void PostDraw()
        {
            if (compProps.specialShadowData != null)
            {
                if (shadowGraphic == null)
                {
                    shadowGraphic = new Graphic_Shadow(compProps.specialShadowData);
                }
                shadowGraphic.Draw(parent.DrawPos, Rot4.North, parent);
            }

            base.PostDraw();
        }

    }
}
