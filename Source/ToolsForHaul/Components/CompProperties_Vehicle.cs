// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompProperties_Vehicle.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the CompProperties_Vehicle type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ToolsForHaul.Components.Vehicle
{
    using Verse;

    public class CompProperties_Vehicle : CompProperties
    {
        public bool animalsCanDrive;

        public float fuelCatchesFireHitPointsPercent;

        public bool isMedical;

        public bool leaveTrail = true;

        public bool motorizedWithoutFuel;

        public bool showsStorage;

        public SoundDef soundAmbient;

        public ShadowData specialShadowData;

        public CompProperties_Vehicle()
        {
            this.compClass = typeof(CompVehicle);
        }
    }
}