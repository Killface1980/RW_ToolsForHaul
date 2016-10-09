using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace CommunityCoreLibrary
{

    public class ModHelperDef : Def
    {

        #region XML Data

        #region Engine Level Injectors

        // InjectionSubController
        public List<CompInjectionSet> ThingComps;


        #endregion


        [Unsaved]

        #region Instance Data

        // Used to flag xml defined (false) and auto-generated (true) for logging
        public bool dummy = false;

        // Used to link directly to the mod which this def controls
        public ModContentPack mod;

        #endregion


        #endregion

    }

}