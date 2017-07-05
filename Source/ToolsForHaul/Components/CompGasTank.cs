using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolsForHaul.Components
{
    using RimWorld;

    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;

    public class CompGasTank : ThingComp
    {
        private Vehicle_Cart cart;
        private int _tankSpillTick = -5000;
        public bool fueledByAI;
        public bool tankLeaking;
        public float _tankHitPos = 1f;
        private int tankHitCount;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.cart = this.parent as Vehicle_Cart;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!this.cart.Spawned)
                return;

            if (this.cart.MountableComp.IsMounted)
            {
                if (this.cart.RefuelableComp != null)
                {
                    if (!this.cart.MountableComp.Driver.Faction.IsPlayer)
                    {
                        if (!this.fueledByAI)
                        {
                            if (this.cart.RefuelableComp.FuelPercentOfMax < 0.550000011920929)
                            {
                                this.cart.RefuelableComp.Refuel(
                                    ThingMaker.MakeThing(
                                        this.cart.RefuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                            }
                            else
                            {
                                this.fueledByAI = true;
                            }
                        }
                    }
                }
            }

            if (this.tankLeaking)
            {
                if (Find.TickManager.TicksGame > this._tankSpillTick)
                {
                    if (this.cart.RefuelableComp.FuelPercentOfMax > this._tankHitPos)
                    {
                        this.cart.RefuelableComp.ConsumeFuel(0.15f);

                        FilthMaker.MakeFilth(this.parent.Position, this.parent.Map, VehicleDefOf.ChemFuelFilth, this.parent.LabelCap);
                        this._tankSpillTick = Find.TickManager.TicksGame + 15;
                    }
                }
            }

        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);

            if (!this.cart.Spawned)
                return;

            bool makeHole = false;
            float hitpointsPercent = this.cart.health.capacities.GetLevel(PawnCapacityDefOf.BloodPumping);
                // (float)this.parent.HitPoints / this.parent.MaxHitPoints;

            if (!this.cart.HasGasTank())
            {
                if (dinfo.Def == DamageDefOf.Deterioration && Rand.Value > 0.5f)
                {
                    if (hitpointsPercent < 0.35f)
                    {
                        this.tankLeaking = true;
                        this.tankHitCount += 1;
                        this._tankHitPos = Math.Min(this._tankHitPos, Rand.Value);

                        int splash = (int)(this.cart.RefuelableComp.FuelPercentOfMax - this._tankHitPos * 20);

                        FilthMaker.MakeFilth(this.parent.Position, this.parent.Map, VehicleDefOf.ChemFuelFilth, this.parent.LabelCap, splash);
                    }

                    if (hitpointsPercent < 0.05f && Rand.Value > 0.5f)
                    {
                        FireUtility.TryStartFireIn(this.parent.Position, this.parent.Map, 0.1f);
                    }

                    return;
                }

                if (this.cart.RefuelableComp.HasFuel)
                {
                    if (hitpointsPercent < this.cart.VehicleComp.FuelCatchesFireHitPointsPercent() && Rand.Value > 0.5f)
                    {
                        if (!this.tankLeaking)
                        {
                            this.cart.RefuelableComp.ConsumeFuel(1f);
                            FilthMaker.MakeFilth(this.parent.Position, this.parent.Map, VehicleDefOf.ChemFuelFilth, this.parent.LabelCap, 6);
                            makeHole = true;
                        }

                      //  FireUtility.TryStartFireIn(this.parent.Position, this.parent.Map, 0.1f);
                    }
                }

                if (Random.value <= 0.1f || makeHole)
                {
                    this.tankLeaking = true;
                    this.tankHitCount += 1;
                    this._tankHitPos = Math.Min(this._tankHitPos, Rand.Value);

                    if (this.cart.RefuelableComp != null)
                    {
                        int splash = (int)(this.cart.RefuelableComp.FuelPercentOfMax - this._tankHitPos * 20);

                        FilthMaker.MakeFilth(this.parent.Position, this.parent.Map, VehicleDefOf.ChemFuelFilth, this.parent.LabelCap, splash);
                    }
                }

            }

        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.tankLeaking, "tankLeaking");
            Scribe_Values.Look(ref this._tankHitPos, "tankHitPos");
        }
    }
}
