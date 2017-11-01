using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;

using UnityEngine;

using Verse;

namespace ppumkin.LEDTechnology.GlowFlooders
{
    public interface IGlowFlooder
    {
        List<GlowGridCache> ColorCellIndexCache { get; set; }

        void CalculateGlowFlood();
        void Clear();
    }

    /// <summary>
    /// All values in here must be provided and calcualted. This serves as a cache to prevent recalculating glowers again
    /// The values here will be dumped into the GlowGrid as fast as possible to conserve CPU usage on refreshing!
    /// </summary>
    public class GlowGridCache
    {
        public IntVec3 Position { get; set; }
        public int CellGridIndex { get; set; }
        public Color32 ColorAtCellIndex { get; set; }
        public bool IsBlocked { get; set; }
    }
}