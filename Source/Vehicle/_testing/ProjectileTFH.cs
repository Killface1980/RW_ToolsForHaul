using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ToolsForHaul
{
   public abstract class ProjectileTFH : Projectile
    {
        private static readonly List<Thing> cellThingsFiltered = new List<Thing>();
        
        // Verse.Projectile
        [Detour(typeof(Projectile), bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic)]
        private void ImpactSomething()
        {

            if (def.projectile.flyOverhead)
            {
                RoofDef roofDef = Find.RoofGrid.RoofAt(Position);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        def.projectile.soundHitThickRoof.PlayOneShot(Position);
                        Destroy();
                        return;
                    }

                    if (Position.GetEdifice() == null || Position.GetEdifice().def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(Position);
                    }
                }
            }

            if (assignedTarget != null)
            {
                Pawn pawn = assignedTarget as Pawn;
                if (pawn != null && pawn.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f)
                {
                    Impact(null);
                    return;
                }

                Impact(assignedTarget);
                return;
            }
            else
            {
                cellThingsFiltered.Clear();
                List<Thing> thingList = Position.GetThingList();
                for (int i = 0; i < thingList.Count; i++)
                {
                    Pawn pawn2 = thingList[i] as Pawn;
                    if (pawn2 != null)
                    {
                        cellThingsFiltered.Add(pawn2);
                    }

                    Vehicle_Cart cart = thingList[i] as Vehicle_Cart;
                    if (cart != null)
                    {
                        cellThingsFiltered.Add(cart);
                    }
                }

                if (cellThingsFiltered.Count > 0)
                {
                    Impact(cellThingsFiltered.RandomElement());
                    return;
                }

                cellThingsFiltered.Clear();
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing thing = thingList[j];
                    if (thing.def.fillPercent > 0f || thing.def.passability != Traversability.Standable)
                    {
                        cellThingsFiltered.Add(thing);
                    }
                }

                if (cellThingsFiltered.Count > 0)
                {
                    Impact(cellThingsFiltered.RandomElement());
                    return;
                }

                Impact(null);
                return;
            }
        }

    }
}
