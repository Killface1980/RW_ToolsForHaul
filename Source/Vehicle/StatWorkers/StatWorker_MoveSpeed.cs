using System.Collections.Generic;
using System.Text;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.Utilities;
using UnityEngine;
using Verse;

namespace ToolsForHaul.StatWorkers
{
    internal class StatWorker_MoveSpeed : StatWorker
    {
        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetExplanation(req, numberSense));
            if (req.HasThing)
            {
                Pawn thisPawn = req.Thing as Pawn;

                if (thisPawn != null)
                {
                    if (thisPawn.RaceProps.intelligence >= Intelligence.ToolUser)
                    {
                        if (MapComponent_ToolsForHaul.currentVehicle.ContainsKey(thisPawn))
                        {
                            Vehicle_Cart vehicle_Cart = MapComponent_ToolsForHaul.currentVehicle[thisPawn] as Vehicle_Cart;
                            if (vehicle_Cart != null)
                                if (vehicle_Cart.mountableComp.IsMounted && vehicle_Cart.mountableComp.Driver == thisPawn)
                                {
                                    stringBuilder.AppendLine();
                                    stringBuilder.AppendLine("VehicleSpeed".Translate() + ": x" + vehicle_Cart.VehicleSpeed);
                                    return stringBuilder.ToString();
                                }

                            Vehicle_Turret vehicle_Turret = MapComponent_ToolsForHaul.currentVehicle[req.Thing as Pawn] as Vehicle_Turret;
                            if (vehicle_Turret != null)
                                if (vehicle_Turret.mountableComp.IsMounted && vehicle_Turret.mountableComp.Driver == thisPawn)
                                {
                                    stringBuilder.AppendLine();
                                    stringBuilder.AppendLine("VehicleSpeed".Translate() + ": x" + vehicle_Turret.VehicleSpeed);
                                    return stringBuilder.ToString();
                                }
                        }

                        CompSlotsBackpack compSlotsBackpack = ToolsForHaulUtility.TryGetBackpack(thisPawn).TryGetComp<CompSlotsBackpack>();
                        if (compSlotsBackpack != null)
                        {

                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine("CR_CarriedWeight".Translate() + ": x" + compSlotsBackpack.moveSpeedFactor.ToStringPercent());
                            if (compSlotsBackpack.encumberPenalty > 0f)
                            {
                                stringBuilder.AppendLine("CR_Encumbered".Translate() + ": -" + compSlotsBackpack.encumberPenalty.ToStringPercent());
                                stringBuilder.AppendLine("CR_FinalModifier".Translate() + ": x" + GetStatFactor(thisPawn).ToStringPercent());
                            }
                        }
                    }
                }
            }

            return stringBuilder.ToString();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {

            float num = base.GetValueUnfinalized(req, applyPostProcess);

            if (req.HasThing && req.Thing is Pawn)
            {
                Pawn pawn = req.Thing as Pawn;

                if (pawn.RaceProps.intelligence >= Intelligence.ToolUser)

                    if (GetStatFactor(pawn) > 1.01f)
                    {
                        num = GetStatFactor(pawn);
                    }
                    else
                    {
                        num *= GetStatFactor(pawn);
                    }
            }
            return num;

        }

        private float GetStatFactor(Pawn thisPawn)
        {
            float result = 1f;

            if (MapComponent_ToolsForHaul.currentVehicle.ContainsKey(thisPawn))
            {
                Vehicle_Cart vehicleCart = MapComponent_ToolsForHaul.currentVehicle[thisPawn] as Vehicle_Cart;
                if (vehicleCart != null)
                {
                    if (vehicleCart.mountableComp.IsMounted && !vehicleCart.mountableComp.Driver.RaceProps.Animal && vehicleCart.mountableComp.Driver == thisPawn)
                    {
                        if (vehicleCart.IsCurrentlyMotorized())
                        {
                            result = Mathf.Clamp(vehicleCart.VehicleSpeed, 2f, 100f);
                        }
                        else
                        {
                            result = Mathf.Clamp(vehicleCart.VehicleSpeed, 0.5f, 1f);
                        }
                        return result;
                    }
                }

                Vehicle_Turret vehicleTank = MapComponent_ToolsForHaul.currentVehicle[thisPawn] as Vehicle_Turret;
                if (vehicleTank != null)
                {
                    if (vehicleTank.mountableComp.IsMounted && !vehicleTank.mountableComp.Driver.RaceProps.Animal && vehicleTank.mountableComp.Driver == thisPawn)
                    {
                        if (vehicleTank.IsCurrentlyMotorized())
                        {
                            result = Mathf.Clamp(vehicleTank.VehicleSpeed, 2f, 100f);
                        }
                        else
                        {
                            result = Mathf.Clamp(vehicleTank.VehicleSpeed, 0.5f, 1f);
                        }
                        return result;
                    }
                }
            }

            Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(thisPawn);
            if (backpack != null)
            {
                result = Mathf.Clamp(backpack.slotsComp.moveSpeedFactor - backpack.slotsComp.encumberPenalty, 0.1f, 1f);
            }

            return result;
        }
    }
}
