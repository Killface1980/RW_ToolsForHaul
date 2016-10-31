#define DEBUG

using System;
using System.Diagnostics;
using System.Text;
using RimWorld;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public static class Trace
    {
        public static StringBuilder stringBuilder = new StringBuilder();
        public static Stopwatch stopWatch = new Stopwatch();

        [Conditional("DEBUG")]
        public static void AppendLine(String str)
        {
            stringBuilder.AppendLine((stopWatch.IsRunning ? stopWatch.ElapsedMilliseconds + "ms: " : "") + str);
        }
        [Conditional("DEBUG")]
        public static void LogMessage()
        {
            Log.Message(stringBuilder.ToString());
            stringBuilder.Remove(0, stringBuilder.Length);
            stopWatch.Reset();
        }
        [Conditional("DEBUG")]public static void stopWatchStart() { stopWatch.Start(); }
        [Conditional("DEBUG")]public static void stopWatchStop() { stopWatch.Stop(); }
        [Conditional("DEBUG")]
        public static void DebugWriteHaulingPawn(Pawn pawn)
        {
            AppendLine(pawn.LabelCap + " Report: Cart " + ToolsForHaulUtility.Cart.Count + " Job: " + (pawn.CurJob != null ? pawn.CurJob.def.defName : "No Job")
                + " Backpack: " + (ToolsForHaulUtility.TryGetBackpack(pawn) != null ? "True" : "False")
                + " lastGivenWorkType: " + pawn.mindState.lastGivenWorkType);
            foreach (Pawn other in Find.MapPawns.FreeColonistsSpawned)
            {
                //Vanilla haul or Haul with backpack
                if (other.CurJob != null && (other.CurJob.def == JobDefOf.HaulToCell || other.CurJob.def == HaulJobDefOf.HaulWithBackpack))
                    AppendLine(other.LabelCap + " Job: " + other.CurJob.def.defName
                        + " Backpack: " + (ToolsForHaulUtility.TryGetBackpack(other) != null ? "True" : "False")
                        + " lastGivenWorkType: " + other.mindState.lastGivenWorkType);
            }
            foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart)
            {
                string driver = cart.mountableComp.IsMounted ? cart.mountableComp.Driver.LabelCap : "No Driver";
                string state = "";
                if (cart.IsForbidden(pawn.Faction))
                    state = string.Concat(state, "Forbidden ");
                if (pawn.CanReserveAndReach(cart, PathEndMode.Touch, Danger.Some))
                    state = string.Concat(state, "CanReserveAndReach ");
                if (ToolsForHaulUtility.AvailableVehicle(pawn, cart))
                    state = string.Concat(state, "AvailableCart ");
                if (ToolsForHaulUtility.AvailableAnimalCart(cart))
                    state = string.Concat(state, "AvailableAnimalCart ");
                Pawn reserver = Find.Reservations.FirstReserverOf(cart, Faction.OfPlayer);
                if (reserver != null)
                    state = string.Concat(state, reserver.LabelCap, " Job: ", reserver.CurJob.def.defName);
                AppendLine(cart.LabelCap + "- " + driver + ": " + state);

            }
            LogMessage();
        }
    }
}
