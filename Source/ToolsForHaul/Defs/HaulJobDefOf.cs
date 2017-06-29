namespace ToolsForHaul.Defs
{
    using RimWorld;

    using Verse;

    [DefOf]
    public static class HaulJobDefOf
    {
        // public static readonly JobDef HuntWithVehicle = DefDatabase<JobDef>.GetNamed("HuntWithVehicle");
        public static readonly JobDef HaulWithAnimalCart = DefDatabase<JobDef>.GetNamed("HaulWithAnimalCart");

        public static readonly JobDef HaulWithCart = DefDatabase<JobDef>.GetNamed("HaulWithCart");

        public static readonly JobDef DismountAtParkingLot = DefDatabase<JobDef>.GetNamed("DismountAtParkingLot");

        public static readonly JobDef StandBy = DefDatabase<JobDef>.GetNamed("StandBy");

        public static readonly JobDef Mount = DefDatabase<JobDef>.GetNamed("Mount");

        // public static readonly JobDef Board = DefDatabase<JobDef>.GetNamed("Board");
        public static readonly JobDef MakeMount = DefDatabase<JobDef>.GetNamed("MakeMount");
    }
}
