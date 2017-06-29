using System;
using System.Linq;
using RimWorld;
using ToolsForHaul.Utilities;
using UnityEngine;
using Verse;
using static ToolsForHaul.GameComponentToolsForHaul;

namespace ToolsForHaul
{
    using System.Collections.Generic;

    using ToolsForHaul.Components.Vehicle;

    using Verse.AI;

    [StaticConstructorOnStartup]
    public static class RightVehicle
    {
        static RightVehicle()
        {
            PreLoad();
        }

        private static void PreLoad()
        {
        }


        /// <summary>
        /// Selects the appropriate vehicle by worktype
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="worktype"></param>
        /// <returns></returns>
        public static Thing GetRightVehicle(
            Pawn pawn, List<Thing> availableVehicles,
            WorkTypeDef worktype = null,
            Thing t = null)
        {
            Thing cart = null;

            if (worktype.Equals(WorkTypeDefOf.Hunting))
            {
                IOrderedEnumerable<Thing> orderedEnumerable =
                    availableVehicles.OrderBy(x => pawn.Position.DistanceToSquared(x.Position));
                foreach (Thing thing in orderedEnumerable)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null) continue;
                    if (!ToolsForHaulUtility.IsVehicleAvailable(pawn, vehicleCart)) continue;
                    if (!vehicleCart.VehicleComp.IsCurrentlyMotorized()) continue;
                    if (vehicleCart.VehicleComp.tankLeaking) continue;
                    if (vehicleCart.ExplosiveComp.wickStarted) continue;
                    if (!vehicleCart.IsCurrentlyMotorized()) continue;
                    
                    cart = vehicleCart;
                    break;
                }
            }

            if (worktype == DefDatabase<WorkTypeDef>.GetNamed("Hauling"))
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                    availableVehicles.OrderByDescending(x => (x as Vehicle_Cart).MaxItem).ThenBy(x => pawn.Position.DistanceToSquared(x.Position));

                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                        continue;
                    if (!ToolsForHaulUtility.IsVehicleAvailable(pawn, vehicleCart)) continue;
                    if (vehicleCart.VehicleComp.tankLeaking) continue;
                    if (vehicleCart.ExplosiveComp.wickStarted) continue;
                    if (!vehicleCart.IsCurrentlyMotorized()) continue;
                    cart = vehicleCart;
                    break;
                }
            }

            if (worktype.Equals(WorkTypeDefOf.Construction))
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                    availableVehicles.OrderBy(x => pawn.Position.DistanceToSquared(x.Position)).ThenByDescending(x => (x as Vehicle_Cart).VehicleComp.VehicleSpeed);
                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                        continue;
                    if (!ToolsForHaulUtility.IsVehicleAvailable(pawn, vehicleCart)) continue;
                    if (!vehicleCart.VehicleComp.IsCurrentlyMotorized()) continue;
                    if (vehicleCart.VehicleComp.tankLeaking) continue;
                    if (vehicleCart.ExplosiveComp.wickStarted) continue;
                    if (!vehicleCart.IsCurrentlyMotorized()) continue;
                    cart = vehicleCart;
                    break;
                }
            }

            return cart;
        }
    }
}
