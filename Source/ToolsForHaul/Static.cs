
namespace ToolsForHaul
{
    using UnityEngine;

    using Verse;

    [StaticConstructorOnStartup]
    public static class Static
    {
        #region ThingDefs



        #endregion

        #region Zone_ParkingLot

        public static string ParkingLotDesc = "TFH_DescriptionParkingLot".Translate();

        public static Texture2D TexParkingLot = ContentFinder<Texture2D>.Get("UI/Designations/ZoneCreate_ParkingLot");

        #endregion

        #region Vehicles


        public static Texture2D IconBoard = ContentFinder<Texture2D>.Get("UI/Commands/IconBoard");

        public static Texture2D IconMount = ContentFinder<Texture2D>.Get("UI/Commands/IconMount");

        public static Texture2D IconUnmount = ContentFinder<Texture2D>.Get("UI/Commands/IconUnmount");


        #endregion

        #region Sounds

        public static SoundDef ClickSound = SoundDef.Named("Click");

        #endregion

        #region Strings

        public static readonly string BurningLowerTrans = "BurningLower".Translate();

        public static readonly string NoAvailableCart = "NoAvailableCart".Translate();

        public static readonly string NoEmptyPlaceForCart = "NoEmptyPlaceForCart".Translate();

        public static readonly string NoEmptyPlaceLowerTrans = "NoEmptyPlaceLower".Translate();

        public static readonly string TooLittleHaulable = "TooLittleHaulable".Translate();

        public const string TxtCommandDismountDesc = "CommandDismountDesc";

        public const string TxtCommandDismountLabel = "CommandDismountLabel";

        public const string TxtCommandMountDesc = "CommandMountDesc";

        public const string TxtCommandMountLabel = "CommandMountLabel";

        public static readonly string TxtDismount = "Dismount".Translate();

        public const string TxtMountOn = "MountOn";

        #endregion

        #region Colours

        public static Color ParkingLotColour = new Color32(23, 44, 150, 100);
        
        #endregion
    }
    
}
