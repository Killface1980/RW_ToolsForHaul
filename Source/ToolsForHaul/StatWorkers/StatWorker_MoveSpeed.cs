using System.Collections.Generic;
using System.Text;
#if CR
using Combat_Realism;
#endif
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

                if (thisPawn?.RaceProps.intelligence >= Intelligence.ToolUser)
                {
                    if (GameComponentToolsForHaul.CurrentDrivers.ContainsKey(thisPawn))
                    {
                        Vehicle_Cart vehicleCart = GameComponentToolsForHaul.CurrentDrivers[(Pawn)req.Thing] as Vehicle_Cart;
                        if (vehicleCart != null)
                        {
                            if (vehicleCart.MountableComp.IsMounted && vehicleCart.MountableComp.Driver == thisPawn)
                            {
                                stringBuilder.AppendLine();
                                stringBuilder.AppendLine("VehicleSpeed".Translate() + ": x" + vehicleCart.VehicleComp.VehicleSpeed);
                                return stringBuilder.ToString();
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

                    if (this.GetStatFactor(pawn) > 1.01f)
                    {
                        num = this.GetStatFactor(pawn);
                    }
                    else
                    {
                        num *= this.GetStatFactor(pawn);
                    }
            }

            return num;

        }

        private float GetStatFactor(Pawn thisPawn)
        {
            float result = 1f;

            if (GameComponentToolsForHaul.CurrentDrivers.ContainsKey(thisPawn))
            {
                Vehicle_Cart vehicleTank = GameComponentToolsForHaul.CurrentDrivers[thisPawn] as Vehicle_Cart;
                if (vehicleTank != null)
                {
                    if (vehicleTank.MountableComp.IsMounted && !vehicleTank.MountableComp.Driver.RaceProps.Animal && vehicleTank.MountableComp.Driver == thisPawn)
                    {
                        if (vehicleTank.VehicleComp.IsCurrentlyMotorized())
                        {
                            result = Mathf.Clamp(vehicleTank.VehicleComp.VehicleSpeed, 2f, 100f);
                        }
                        else
                        {
                            result = Mathf.Clamp(vehicleTank.VehicleComp.VehicleSpeed, 0.5f, 1f);
                        }

                        return result;
                    }
                }
            }

#if CR
            CompInventory compInventory = thisPawn.TryGetComp<CompInventory>();
            if (compInventory != null)
            {
                result = Mathf.Clamp(compInventory.moveSpeedFactor - compInventory.encumberPenalty, 0.1f, 1f);
                return result;
            }
#endif
            return result;
        }
    }
}
