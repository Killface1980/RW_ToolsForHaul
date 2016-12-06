using RimWorld;
using Verse;

namespace ToolsForHaul.Detoured
{
    internal static class _Pawn_ApparelTracker
    {
        internal static bool TryDrop(this Pawn_ApparelTracker _this, Apparel ap, out Apparel resultingAp, IntVec3 pos, bool forbid = true)
        {
           // drop all toolbelt & backpack stuff so that it won't disappear
            Apparel_Backpack backpack = ap as Apparel_Backpack;

            Thing dropThing = null;
            
            if (backpack?.slotsComp?.slots?.Count >= 1)
            {
                foreach (Thing slot in backpack.slotsComp.slots)
                {
                    GenThing.TryDropAndSetForbidden(slot, pos, ThingPlaceMode.Near, out dropThing, forbid);
                }
            }

            if (!_this.WornApparel.Contains(ap))
            {
                Log.Warning(_this.pawn.LabelCap + " tried to drop apparel he didn't have: " + ap.LabelCap);
                resultingAp = null;
                return false;
            }

            _this.WornApparel.Remove(ap);
            ap.wearer = null;
            Thing thing = null;
            bool flag = GenThing.TryDropAndSetForbidden(ap, pos, ThingPlaceMode.Near, out thing, forbid);
            resultingAp = thing as Apparel;
            _this.ApparelChanged();
            if (flag && _this.pawn.outfits != null)
            {
                _this.pawn.outfits.forcedHandler.SetForced(ap, false);
            }

#if CR
            Combat_Realism.CR_Utility.TryUpdateInventory(_this.pawn);     // Apparel was dropped, update inventory
#endif
            return flag;
        }

        private static void ApparelChanged(this Pawn_ApparelTracker _this)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                _this.pawn.Drawer.renderer.graphics.ResolveApparelGraphics();
                PortraitsCache.SetDirty(_this.pawn);
            });
        }
    }
}
