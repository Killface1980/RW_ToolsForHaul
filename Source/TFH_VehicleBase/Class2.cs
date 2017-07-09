using System;
using System.Collections.Generic;

namespace Verse
{
    public interface IGizmoOwner
    {
        VerbTracker VerbTracker
        {
            get;
        }

        List<VerbProperties> VerbProperties
        {
            get;
        }
    }
}
