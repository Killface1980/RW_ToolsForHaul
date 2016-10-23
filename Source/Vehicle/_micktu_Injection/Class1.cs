using System;
using Verse;
using RimWorld;
using BuildProductive.Injection;
using ToolsForHaul.Detoured;
using UnityEngine;

namespace BuildProductive
{
    [StaticConstructorOnStartup]
    class Bootstrapper
    {

        static Bootstrapper()
        {
            var injector = new HookInjector();
            Globals.Injector = injector;

            // Post load hook
            injector.Inject(typeof(Pawn_ApparelTracker), "TryDrop", typeof(_Pawn_ApparelTracker));

          
        }
    }
}