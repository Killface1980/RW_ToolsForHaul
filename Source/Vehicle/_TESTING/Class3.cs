using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public  class ListerVehicles
    {
        public List<ThingWithComps> allVehiclesColonist = new List<ThingWithComps>();

        public HashSet<ThingWithComps> allVehiclesColonistCombatTargets = new HashSet<ThingWithComps>();

        public HashSet<ThingWithComps> allBuildingsColonistElecFire = new HashSet<ThingWithComps>();

        public void Add(ThingWithComps vehicle)
        {
            if (vehicle.Faction == Faction.OfPlayer)
            {
                allVehiclesColonist.Add(vehicle);
                if (vehicle is IAttackTarget)
                {
                    allVehiclesColonistCombatTargets.Add(vehicle);
                }
            }
            CompProperties_Power compProperties = vehicle.def.GetCompProperties<CompProperties_Power>();
            if (compProperties != null && compProperties.startElectricalFires)
            {
                allBuildingsColonistElecFire.Add(vehicle);
            }
        }

        public void Remove(ThingWithComps b)
        {
            if (b.Faction == Faction.OfPlayer)
            {
                allVehiclesColonist.Remove(b);
                if (b is IAttackTarget)
                {
                    allVehiclesColonistCombatTargets.Remove(b);
                }
            }
            CompProperties_Power compProperties = b.def.GetCompProperties<CompProperties_Power>();
            if (compProperties != null && compProperties.startElectricalFires)
            {
                allBuildingsColonistElecFire.Remove(b);
            }
        }

        public bool ColonistsHaveBuilding(ThingDef def)
        {
            for (int i = 0; i < allVehiclesColonist.Count; i++)
            {
                if (allVehiclesColonist[i].def == def)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ColonistsHaveBuilding(Func<Thing, bool> predicate)
        {
            for (int i = 0; i < allVehiclesColonist.Count; i++)
            {
                if (predicate(allVehiclesColonist[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ColonistsHaveResearchBench()
        {
            for (int i = 0; i < allVehiclesColonist.Count; i++)
            {
                if (allVehiclesColonist[i] is Building_ResearchBench)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ColonistsHaveBuildingWithPowerOn(ThingDef def)
        {
            for (int i = 0; i < allVehiclesColonist.Count; i++)
            {
                if (allVehiclesColonist[i].def == def)
                {
                    CompPowerTrader compPowerTrader = allVehiclesColonist[i].TryGetComp<CompPowerTrader>();
                    if (compPowerTrader == null || compPowerTrader.PowerOn)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [DebuggerHidden]
        public IEnumerable<ThingWithComps> AllBuildingsColonistOfDef(ThingDef def)
        {
            for (int i = 0; i < allVehiclesColonist.Count; i++)
            {
                if (allVehiclesColonist[i].def == def)
                {
                    yield return allVehiclesColonist[i];
                }
            }
            yield break;
        }

        [DebuggerHidden]
        public IEnumerable<T> AllBuildingsColonistOfClass<T>() where T : Building
        {
            for (int i = 0; i < allVehiclesColonist.Count; i++)
            {
                T t = allVehiclesColonist[i] as T;
                if (t != null)
                {
                    yield return t;
                }
            }
            yield break;
        }
    }
}
