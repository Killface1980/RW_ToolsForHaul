using System.Text;
using RimWorld;
using Verse;

namespace ToolsForHaul
{
    internal class StatWorker_MoveSpeed : StatWorker
    {
        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetExplanation(req, numberSense));
#if CR
            if (req.HasThing)
            {
            CompInventory compInventory = ThingCompUtility.TryGetComp<CompInventory>(req.Thing);
                if (compInventory != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(Translator.Translate("CR_CarriedWeight") + ": x" + GenText.ToStringPercent(compInventory.moveSpeedFactor));
                    if (compInventory.encumberPenalty > 0f)
                    {
                        stringBuilder.AppendLine(Translator.Translate("CR_Encumbered") + ": -" + GenText.ToStringPercent(compInventory.encumberPenalty));
                        stringBuilder.AppendLine(Translator.Translate("CR_FinalModifier") + ": x" + GenText.ToStringPercent(this.GetStatFactor(req.Thing)));
                    }
                }
            }
#endif

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
#if CR
            CompInventory compInventory = ThingCompUtility.TryGetComp<CompInventory>(thing);

            if (compInventory != null)
            {
                using (List<Thing>.Enumerator enumerator = ToolsForHaulUtility.Cart().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Vehicle_Cart vehicle_Cart = (Vehicle_Cart)enumerator.Current;
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
                }
                result = Mathf.Clamp(compInventory.moveSpeedFactor - compInventory.encumberPenalty, 0.1f, 1f);
                return result;
        }
#endif
            return result;
        }
    }
}
