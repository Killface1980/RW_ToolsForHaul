#if CR
using Combat_Realism;
#endif

namespace TFH_VehicleBase.StatWorkers
{
    using System.Text;

    using RimWorld;

    using UnityEngine;

    using Verse;

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
                    if (thisPawn.IsDriver(out Vehicle_Cart vehicleCart))
                    {
                        if (vehicleCart.MountableComp.IsMounted && vehicleCart.MountableComp.Driver == thisPawn)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine(
                                "VehicleSpeed".Translate() + ": x" + vehicleCart.VehicleComp.VehicleSpeed);
                            return stringBuilder.ToString();
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
                Pawn pawn = (Pawn)req.Thing;

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

            if (thisPawn.IsDriver(out Vehicle_Cart drivenCart))
            {
                if (!drivenCart.MountableComp.Driver.RaceProps.Animal && drivenCart.MountableComp.Driver == thisPawn)
                {
                    if (drivenCart.IsCurrentlyMotorized())
                    {
                        result = Mathf.Clamp(drivenCart.VehicleComp.VehicleSpeed, 2f, 100f);
                    }
                    else
                    {
                        result = Mathf.Clamp(drivenCart.VehicleComp.VehicleSpeed, 0.5f, 1f);
                    }

                    return result;
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
