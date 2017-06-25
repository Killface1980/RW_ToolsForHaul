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
        public static void AppendLine(string str)
        {
            stringBuilder.AppendLine((stopWatch.IsRunning ? stopWatch.ElapsedMilliseconds + "ms: " : string.Empty) + str);
        }

        [Conditional("DEBUG")]
        public static void LogMessage()
        {
            Log.Message(stringBuilder.ToString());
            stringBuilder.Remove(0, stringBuilder.Length);
            stopWatch.Reset();
        }

        [Conditional("DEBUG")]public static void StopWatchStart() { stopWatch.Start(); }
        [Conditional("DEBUG")]public static void StopWatchStop() { stopWatch.Stop(); }
        [Conditional("DEBUG")]
        public static void DebugWriteHaulingPawn(Pawn pawn)
        {


            foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart)
            {
                string driver = cart.MountableComp.IsMounted ? cart.MountableComp.Driver.LabelCap : "No Driver";
                string state = string.Empty;
                if (cart.IsForbidden(pawn.Faction))
                    state = string.Concat(state, "Forbidden ");
                if (pawn.CanReserveAndReach(cart, PathEndMode.Touch, Danger.Some))
                    state = string.Concat(state, "CanReserveAndReach ");
                if (ToolsForHaulUtility.AvailableVehicle(pawn, cart))
                    state = string.Concat(state, "AvailableCart ");
                if (ToolsForHaulUtility.AvailableAnimalCart(cart))
                    state = string.Concat(state, "AvailableAnimalCart ");
              //Pawn reserver = cart.Map.reservationManager.FirstReserverWhoseReservationsRespects(cart, Faction.OfPlayer);
              //if (reserver != null)
              //    state = string.Concat(state, reserver.LabelCap, " Job: ", reserver.CurJob.def.defName);
                AppendLine(cart.LabelCap + "- " + driver + ": " + state);

            }

            LogMessage();
        }
    }
}
