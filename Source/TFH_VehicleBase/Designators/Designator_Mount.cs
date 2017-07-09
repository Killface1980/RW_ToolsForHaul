namespace TFH_VehicleBase.Designators
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase.DefOfs_TFH;

    using Verse;
    using Verse.AI;

    public class Designator_Mount : Designator
    {
        public BasicVehicle vehicle;

        public Designator_Mount()
        {
            this.useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions => 2;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            Pawn pawn = loc.GetThingList(this.Map).Find(t => t is Pawn) as Pawn;
            if (pawn == null)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotPawn".Translate());
            if (pawn.Faction != Faction.OfPlayer)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotColonyFaction".Translate());
#if Saddle
            if (!pawn.RaceProps.Animal && vehicle is Vehicle_Saddle)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotHumanlikeOrMechanoid".Translate());
#endif
            if (pawn.RaceProps.Animal && !pawn.training.IsCompleted(TrainableDefOf.Obedience))
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotTrainedAnimal".Translate());
            if (pawn.RaceProps.Animal && !(pawn.RaceProps.baseBodySize >= 1.0))
                return new AcceptanceReport("CannotMount".Translate() + ": " + "TooSmallAnimal".Translate());
            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList(this.Map);
            foreach (Thing thing in thingList)
            {
                Pawn vehicle = thing as Pawn;

                if (vehicle == null)
                {
                    return;

                }

                if (vehicle.Faction == Faction.OfPlayer)
                {
                    if (vehicle.RaceProps.IsMechanoid || vehicle.RaceProps.Animal && !vehicle.IsDriver(out Vehicle_Cart drivenCart))
                    {
                        Job jobNew = new Job(VehicleJobDefOf.Mount);
                        this.Map.reservationManager.ReleaseAllForTarget(this.vehicle);
                        jobNew.targetA = this.vehicle;
                        vehicle.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                        break;
                    }


                }
            }

            Find.DesignatorManager.Deselect();
        }
    }
}