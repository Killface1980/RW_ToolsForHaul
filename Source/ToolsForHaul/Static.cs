using System.Reflection;

using UnityEngine;
using Verse;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public static class Static
    {
        #region Zone_ParkingLot

        public static string ParkingLotDesc = "TFH_DescriptionParkingLot".Translate();

        public static Texture2D TexParkingLot = ContentFinder<Texture2D>.Get("UI/Designations/ZoneCreate_ParkingLot", true);

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
