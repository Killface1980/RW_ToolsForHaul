using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ToolsForHaul
{
    public static class ListerVehiclesRepairable
    {
        private static Dictionary<Faction, List<Thing>> repairables = new Dictionary<Faction, List<Thing>>();

        public static void Reinit()
        {
            repairables.Clear();
        }

        public static List<Thing> RepairableVehicles(Faction fac)
        {
            return ListFor(fac);
        }

        public static void Notify_VehicleSpawned(ThingWithComps b)
        {
            if (b.Faction == null)
            {
                return;
            }
            UpdateVehicle(b);
        }

        public static void Notify_VehicleDeSpawned(ThingWithComps b)
        {
            if (b.Faction == null)
            {
                return;
            }
            List<Thing> list = ListFor(b.Faction);
            if (list.Contains(b))
            {
                list.Remove(b);
            }
        }

        public static void Notify_VehicleTookDamage(ThingWithComps b)
        {
            if (b.Faction == null)
            {
                return;
            }
            UpdateVehicle(b);
        }

        internal static void Notify_BuildingFactionChanged(Building b)
        {
            Notify_VehicleDeSpawned(b);
            Notify_VehicleSpawned(b);
        }

        private static void UpdateVehicle(ThingWithComps b)
        {
            if (b.Faction == null || !b.def.building.repairable)
            {
                return;
            }
            List<Thing> list = ListFor(b.Faction);
            if (b.HitPoints < b.MaxHitPoints)
            {
                if (!list.Contains(b))
                {
                    list.Add(b);
                }
            }
            else if (list.Contains(b))
            {
                list.Remove(b);
            }
        }

        private static List<Thing> ListFor(Faction fac)
        {
            List<Thing> list;
            if (!repairables.TryGetValue(fac, out list))
            {
                list = new List<Thing>();
                repairables.Add(fac, list);
            }
            return list;
        }

        internal static string DebugString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Faction current in Find.FactionManager.AllFactions)
            {
                List<Thing> list = ListFor(current);
                if (!list.NullOrEmpty<Thing>())
                {
                    stringBuilder.AppendLine(string.Concat(new object[]
                    {
                        "=======",
                        current.Name,
                        " (",
                        current.def,
                        ")"
                    }));
                    foreach (Thing current2 in list)
                    {
                        stringBuilder.AppendLine(current2.ThingID);
                    }
                }
            }
            return stringBuilder.ToString();
        }
    }
}
