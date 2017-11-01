namespace TFH_Tools
{
    using RimWorld;

    using Verse;

    internal static class _Pawn_ApparelTracker
    {
        internal static bool TryDrop(this Pawn_ApparelTracker _this, Apparel ap, out Apparel resultingAp, IntVec3 pos, bool forbid = true)
        {
           // drop all toolbelt & backpack stuff so that it won't disappear
            Apparel_Backpack backpack = ap as Apparel_Backpack;
            Apparel_ToolBelt toolbelt = ap as Apparel_ToolBelt;
            Thing dropThing = null;
            
            if (backpack?.slotsComp?.slots?.Count >= 1)
            {
                foreach (Thing slot in backpack.slotsComp.slots)
                {
                    GenThing.TryDropAndSetForbidden(slot, pos,ap.Map, ThingPlaceMode.Near, out dropThing, forbid);
                }
            }

            if (toolbelt?.slotsComp?.slots?.Count >= 1)
            {
                foreach (Thing slot in toolbelt.slotsComp.slots)
                {
                    GenThing.TryDropAndSetForbidden(slot, pos, ap.Map, ThingPlaceMode.Near, out dropThing, forbid);
                }

                for (int i = MapComponent_ToolsForHaul.CachedToolEntries.Count - 1;
                    i >= 0;
                    i--)
                {
                    var entry = MapComponent_ToolsForHaul.CachedToolEntries[i];
                    if (entry.pawn == _this.pawn)
                    {
                        MapComponent_ToolsForHaul.CachedToolEntries.RemoveAt(i);
                    }
                }
            }


            if (!_this.WornApparel.Contains(ap))
            {
                Log.Warning(_this.pawn.LabelCap + " tried to drop apparel he didn't have: " + ap.LabelCap);
                resultingAp = null;
                return false;
            }
            if (_this.pawn.MapHeld == null)
            {
                Log.Warning(_this.pawn.LabelCap + " tried to drop apparel but his MapHeld is null.");
                resultingAp = null;
                return false;
            }

            _this.Remove(ap);
            Thing thing = null;
            bool result = GenThing.TryDropAndSetForbidden(ap, pos, _this.pawn.MapHeld, ThingPlaceMode.Near, out thing, forbid);

            resultingAp = (thing as Apparel);

#if CR
            Combat_Realism.CR_Utility.TryUpdateInventory(_this.pawn);     // Apparel was dropped, update inventory
#endif
            return result;
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
