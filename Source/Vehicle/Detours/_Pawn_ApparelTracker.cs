using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ToolsForHaul.Detoured
{
    internal static class _Pawn_ApparelTracker
    {
        internal static bool TryDrop(this Pawn_ApparelTracker _this, Apparel ap, out Apparel resultingAp, IntVec3 pos, bool forbid = true)
        {
            if (!_this.WornApparel.Contains(ap))
            {
                Log.Warning(_this.pawn.LabelCap + " tried to drop apparel he didn't have: " + ap.LabelCap);
                resultingAp = null;
                return false;
            }

            Apparel_Toolbelt toolbelt = ap as Apparel_Toolbelt;
            Apparel_Backpack backpack = ap as Apparel_Backpack;

            Thing thing = null;

            if (toolbelt != null)
            {
                if (toolbelt?.slotsComp?.slots?.Count >= 1)
                {
                    foreach (Thing slot in toolbelt.slotsComp.slots)
                    {
                        GenThing.TryDropAndSetForbidden(slot, pos, ThingPlaceMode.Near, out thing, forbid);
                    }
                }
            }
            if (backpack?.slotsComp?.slots?.Count >= 1)
            {
                foreach (Thing slot in backpack.slotsComp.slots)
                {
                    GenThing.TryDropAndSetForbidden(slot, pos, ThingPlaceMode.Near, out thing, forbid);
                }
            }

            _this.WornApparel.Remove(ap);
            ap.wearer = null;
            bool flag = GenThing.TryDropAndSetForbidden(ap, pos, ThingPlaceMode.Near, out thing, forbid);
            resultingAp = (thing as Apparel);
            _this.pawn.Drawer.renderer.graphics.ResolveApparelGraphics();
            if (flag && _this.pawn.outfits != null)
            {
                _this.pawn.outfits.forcedHandler.SetForced(ap, false);
            }
            return flag;
        }
    }
}
