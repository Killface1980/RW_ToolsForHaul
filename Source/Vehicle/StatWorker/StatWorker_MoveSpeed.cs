using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ToolsForHaul
{
    internal class StatWorker_MoveSpeed : StatWorker
    {
        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetExplanation(req, numberSense));
            if (req.HasThing)
            {

                foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart())
                {
                    if (vehicle_Cart == null)
                        continue;

                    if (vehicle_Cart.mountableComp.IsMounted && !vehicle_Cart.mountableComp.Driver.RaceProps.Animal && vehicle_Cart.mountableComp.Driver.ThingID == req.Thing.ThingID)
                    {
                        if (vehicle_Cart.IsCurrentlyMotorized())
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine("VehicleSpeed".Translate() + ": x" + vehicle_Cart.VehicleSpeed);
                        }

                    }
                }

                CompSlotsBackpack compInventory = ToolsForHaulUtility.TryGetBackpack(req.Thing as Pawn).TryGetComp<CompSlotsBackpack>();
                if (compInventory != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("CR_CarriedWeight".Translate() + ": x" + compInventory.moveSpeedFactor.ToStringPercent());
                    if (compInventory.encumberPenalty > 0f)
                    {
                        stringBuilder.AppendLine("CR_Encumbered".Translate() + ": -" + compInventory.encumberPenalty.ToStringPercent());
                        stringBuilder.AppendLine("CR_FinalModifier".Translate() + ": x" + GetStatFactor(req.Thing).ToStringPercent());
                    }
                }



            }
            return stringBuilder.ToString();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            float num = base.GetValueUnfinalized(req, applyPostProcess);
            if (req.HasThing)
            {
                if (GetStatFactor(req.Thing) > 1.01f)
                {
                    num = GetStatFactor(req.Thing);
                }
                else
                {
                    num *= GetStatFactor(req.Thing);
                }
            }
            return num;
        }

        private float GetStatFactor(Thing thing)
        {
            float result = 1f;

            foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart())
            {
                if (vehicle_Cart == null)
                    continue;

                if (vehicle_Cart.mountableComp.IsMounted && !vehicle_Cart.mountableComp.Driver.RaceProps.Animal && vehicle_Cart.mountableComp.Driver.ThingID == thing.ThingID)
                {
                    if (vehicle_Cart.IsCurrentlyMotorized())
                    {
                        result = Mathf.Clamp(vehicle_Cart.VehicleSpeed, 2f, 100f);
                    }
                    else
                    {
                        result = Mathf.Clamp(vehicle_Cart.VehicleSpeed, 0.5f, 1f);
                    }
                    return result;
                }

            }

       //   Apparel_Backpack apparelBackpack = ToolsForHaulUtility.TryGetBackpack(thing as Pawn);
       //   if (apparelBackpack != null)
       //   {
       //       CompSlotsBackpack compInventory = apparelBackpack.slotsComp;
       //       if (compInventory != null)
       //       {
       //           result = Mathf.Clamp(compInventory.moveSpeedFactor - compInventory.encumberPenalty, 0.1f, 1f);
       //       }
       //   }


            return result;
        }
    }
}
