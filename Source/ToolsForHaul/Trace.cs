#define LOGGING

namespace ToolsForHaul
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using RimWorld;

    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using Verse;
    using Verse.AI;

    public static class Trace
    {
        public static Stopwatch stopWatch = new Stopwatch();

        public static StringBuilder stringBuilder = new StringBuilder();

        [Conditional("LOGGING")]
        public static void AppendLine(string str)
        {
            stringBuilder.AppendLine(
                (stopWatch.IsRunning ? stopWatch.ElapsedMilliseconds + "ms: " : string.Empty) + str);
        }

        [Conditional("LOGGING")]
        public static void DebugWriteHaulingPawn(Pawn pawn)
        {
            List<Thing> availableVehicles = TFH_Utility.AvailableVehicles(pawn);

            foreach (var thing in availableVehicles)
            {
                var cart = (Vehicle_Cart)thing;
                string driver = cart.MountableComp.IsMounted ? cart.MountableComp.Driver.LabelCap : "No Driver";
                string state = string.Empty;
                if (cart.IsForbidden(pawn.Faction))
                {
                    state = string.Concat(state, "Forbidden ");
                }

                if (pawn.CanReserveAndReach(cart, PathEndMode.Touch, Danger.Some))
                {
                    state = string.Concat(state, "CanReserveAndReach ");
                }

                if (TFH_Utility.IsVehicleAvailable(pawn, cart))
                {
                    state = string.Concat(state, "AvailableCart ");
                }

                if (TFH_Utility.AvailableAnimalCart(cart))
                {
                    state = string.Concat(state, "AvailableAnimalCart ");
                }

                // Pawn reserver = cart.Map.reservationManager.FirstReserverWhoseReservationsRespects(cart, Faction.OfPlayer);
                // if (reserver != null)
                // state = string.Concat(state, reserver.LabelCap, " Job: ", reserver.CurJob.def.defName);
                AppendLine(cart.LabelCap + "- " + driver + ": " + state);
            }

            LogMessage();
        }

        [Conditional("LOGGING")]
        public static void LogMessage()
        {
            Log.Message(stringBuilder.ToString());
            stringBuilder.Remove(0, stringBuilder.Length);
            stopWatch.Reset();
        }

        [Conditional("LOGGING")]
        public static void StopWatchStart()
        {
            stopWatch.Start();
        }

        [Conditional("LOGGING")]
        public static void StopWatchStop()
        {
            stopWatch.Stop();
        }
    }
}