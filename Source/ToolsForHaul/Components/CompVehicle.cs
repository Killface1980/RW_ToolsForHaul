namespace ToolsForHaul.Components
{
    using System;
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Defs;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;
    using Verse.AI;
    using Verse.Sound;

    using Random = UnityEngine.Random;

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

        public float DesiredSpeed => this.parent.GetStatValue(StatDefOf.MoveSpeed);

        public float VehicleSpeed;
        public bool despawnAtEdge;



        private Graphic_Shadow shadowGraphic;
        private int tickCheck = Find.TickManager.TicksGame;
        private readonly int tickCooldown = 60;

        private Vector3 _lastTireTrackPlacePos;
        private const float FootprintIntervalDist = 0.7f;
        private static readonly Vector3 TrackOffset = new Vector3(0f, 0f, -0.3f);
        private static readonly Vector3 DustOffset = new Vector3(-0.3f, 0f, -0.3f);
        private static readonly Vector3 FumesOffset = new Vector3(-0.3f, 0f, 0f);

        private Vehicle_Cart cart;

        // private HeadLights flooder;




        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (!this.parent.Spawned)
                return;

            float hitpointsPercent = (float)this.parent.HitPoints / this.parent.MaxHitPoints;

            if (hitpointsPercent <= 0.35f)
            {
                if (this.cart.IsCurrentlyMotorized())
                {
                    MoteMakerTFH.ThrowMicroSparks(this.parent.DrawPos, this.parent.Map);
                }
            }

            if (dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Bomb)
            {
                return;
            }


        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref this.despawnAtEdge, "despawnAtEdge");
        }

        public override void CompTick()
        {
            base.CompTick();
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
            if (this.cart.MountableComp.IsMounted)
            {
                Pawn_PathFollower pawnPathFollower = this.cart.MountableComp.Driver.pather;
                bool isMoving = pawnPathFollower != null && pawnPathFollower.Moving;

                if (isMoving)
                {
                    Vector3 pos = this.cart.DrawPos;
                    if (this.cart.Map.terrainGrid.TerrainAt(pos.ToIntVec3()).takeFootprints
                        || this.cart.Map.snowGrid.GetDepth(pos.ToIntVec3()) > 0.2f)
                    {
                        if (this.LeaveTrail())
                        {
                            Vector3 normalized = (pos - this._lastTireTrackPlacePos).normalized;
                            float rot = normalized.AngleFlat();
                            Vector3 loc = pos + TrackOffset;

                            if ((loc - this._lastTireTrackPlacePos).MagnitudeHorizontalSquared() > FootprintIntervalDist)
                            {
                                MoteMakerTFH.PlaceTireTrack(loc, this.parent.Map, rot, pos);
                                this._lastTireTrackPlacePos = pos;
                            }
                        }


                        if (this.cart.HasAxles())
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
                    if (isMoving)
                    {
                        Pawn_StanceTracker pawnStanceTracker = this.cart.MountableComp.Driver.stances;
                        if (pawnStanceTracker != null && !pawnStanceTracker.FullBodyBusy)
                        {
                            this.cart.RefuelableComp?.Notify_UsedThisTick();

                            if (this.cart.HasAxles())
                            {
                                this.currentDriverSpeed = TFH_Utility.GetMoveSpeed(this.cart.MountableComp.Driver);
                            }
                        }

                        if (this.cart.BreakdownableComp != null && this.cart.BreakdownableComp.BrokenDown
                            || this.cart.RefuelableComp != null && !this.cart.RefuelableComp.HasFuel)
                        {
                            this.VehicleSpeed = 0.75f;
                        }
                        else
                        {
                            this.VehicleSpeed = this.DesiredSpeed;
                        }

                        this.tickCheck = Find.TickManager.TicksGame;
                    }

             //     if (this.cart.Position.InNoBuildEdgeArea(this.parent.Map) && this.despawnAtEdge && this.parent.Spawned
             //         && this.cart.MountableComp.Driver.Faction != Faction.OfPlayer)
             //     {
             //         this.cart.DeSpawn();
             //     }
                }

                // Exhaustion fumes - basic
                // only fumes on vehicles with combustion and no animals driving
                if (!this.MotorizedWithoutFuel() && !this.AnimalsCanDrive())
                {
                    MoteMakerTFH.ThrowSmoke(this.parent.DrawPos + FumesOffset, this.parent.Map, 0.05f + this.currentDriverSpeed * 0.01f);
                }
            }


        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.cart = this.parent as Vehicle_Cart;

        }
    }
}
