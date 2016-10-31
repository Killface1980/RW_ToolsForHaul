using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;

namespace ToolsForHaul
{
    public static class _ThingDef
    {

        internal static FieldInfo _IsBlueprint;
        internal static FieldInfo _IsFrame;
        internal static FieldInfo _category;

        internal static bool GetPawn(this ThingDef _this)
        {
            if (_IsBlueprint == null)
            {
                _IsBlueprint = typeof(ThingDef).GetField("IsBlueprint", BindingFlags.Instance | BindingFlags.Public);
                if (_IsBlueprint == null)
                {
                    Log.ErrorOnce("Unable to reflect Pawn_RelationsTracker.pawn!", 0x12348765);
                }
            }
            if ((bool)_IsBlueprint.GetValue(_this)) return true;
            return false;
        }

        internal static bool GetPawn2(this ThingDef _this)
        {
            if (_IsFrame == null)
            {
                _IsFrame = typeof(ThingDef).GetField("IsFrame", BindingFlags.Instance | BindingFlags.Public);
                if (_IsFrame == null)
                {
                    Log.ErrorOnce("Unable to reflect Pawn_RelationsTracker.pawn!", 0x12348765);
                }
            }
            return (bool)_IsFrame.GetValue(_this);
        }

        internal static ThingCategory GetPawn3(this ThingDef _this)
        {
            if (_category == null)
            {
                _category = typeof(ThingDef).GetField("category", BindingFlags.Instance | BindingFlags.Public);
                if (_category == null)
                {
                    Log.ErrorOnce("Unable to reflect Pawn_RelationsTracker.pawn!", 0x12348765);
                }
            }
            return (ThingCategory)_category.GetValue(_this);
        }

        // Verse.ThingDef
        [Detour(typeof(ThingDef), bindingFlags = (BindingFlags.Instance | BindingFlags.Public))]
        internal static bool CanHaveFaction(this ThingDef _this)
        {
            return true;

            bool IsBlueprint = _this.GetPawn();
            bool IsFrame = _this.GetPawn2();
            ThingCategory category = _this.GetPawn3();

            if (IsBlueprint || IsFrame)
            {
                return true;
            }
            switch (category)
            {
                case ThingCategory.Pawn:
                    return true;
                case ThingCategory.Building:
                    return true;
            }
            return false;
        }


    }
}
