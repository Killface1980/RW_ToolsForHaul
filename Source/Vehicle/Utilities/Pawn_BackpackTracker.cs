using RimWorld;
using System;
using Verse;

namespace ToolsForHaul
{
    public class Pawn_BackpackTracker : IExposable, IThingContainerOwner
    {
        public Pawn pawn;

        public ThingContainer backpack;

        public bool Spawned
        {
            get
            {
                return pawn.Spawned;
            }
        }

        public Pawn_BackpackTracker(Pawn pawn)
        {
            this.pawn = pawn;
            backpack = new ThingContainer(this, false);
        }

        public void ExposeData()
        {
            Scribe_Deep.LookDeep<ThingContainer>(ref backpack, "backpack", new object[]
            {
                this
            });
        }

        public void InventoryTrackerTick()
        {
            backpack.ThingContainerTick();
        }

        public void DropAllNearPawn(IntVec3 pos, bool forbid = false)
        {
            while (backpack.Count > 0)
            {
                Thing thing;
                backpack.TryDrop(backpack[0], pos, ThingPlaceMode.Near, out thing, delegate (Thing t, int unused)
                {
                    if (forbid)
                    {
                        t.SetForbiddenIfOutsideHomeArea();
                    }
                    if (t.def.IsPleasureDrug)
                    {
                        LessonAutoActivator.TeachOpportunity(ConceptDefOf.DrugBurning, OpportunityType.Important);
                    }
                });
            }
        }

        public void DestroyAll(DestroyMode mode = DestroyMode.Vanish)
        {
            backpack.ClearAndDestroyContents(mode);
        }

        public bool Contains(Thing item)
        {
            return backpack.Contains(item);
        }

        public ThingContainer GetContainer()
        {
            return backpack;
        }

        public IntVec3 GetPosition()
        {
            return pawn.Position;
        }
    }
}
