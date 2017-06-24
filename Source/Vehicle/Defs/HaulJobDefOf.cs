using RimWorld;
using Verse;

namespace ToolsForHaul.JobDefs
{
    [DefOf]
    public static class HaulJobDefOf
    {
        // public static readonly JobDef HuntWithVehicle = DefDatabase<JobDef>.GetNamed("HuntWithVehicle");
        public static readonly JobDef HaulWithAnimalCart = DefDatabase<JobDef>.GetNamed("HaulWithAnimalCart");

        public static readonly JobDef HaulWithCart = DefDatabase<JobDef>.GetNamed("HaulWithCart");

        public static readonly JobDef DismountInBase = DefDatabase<JobDef>.GetNamed("DismountInBase");

        public static readonly JobDef StandBy = DefDatabase<JobDef>.GetNamed("StandBy");

        public static readonly JobDef Mount = DefDatabase<JobDef>.GetNamed("Mount");

        // public static readonly JobDef Board = DefDatabase<JobDef>.GetNamed("Board");
        public static readonly JobDef MakeMount = DefDatabase<JobDef>.GetNamed("MakeMount");
    }
}
