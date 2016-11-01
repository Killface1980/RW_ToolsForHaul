using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.Detoured;
using UnityEngine;
using Verse;
using Verse.AI;
using Object = UnityEngine.Object;

namespace ToolsForHaul
{
    [SpecialInjectorSequencer(InjectionSequence.MainLoad, InjectionTiming.SpecialInjectors)]
    class DetourInjector : SpecialInjector
    {
        public override bool Inject()
        {

            // Pawn_ApparelTracker

            MethodInfo tryDrop3Source = typeof(Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            MethodInfo tryDrop3Dest = typeof(_Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Pawn_ApparelTracker), typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            if (!Detours.TryDetourFromTo(tryDrop3Source, tryDrop3Dest))
                Log.Message("Failed detour Pawn_ApparelTracker TryDrop");
            else
            {
                Log.Message("TFH detoured Pawn_ApparelTracker TryDrop");
                return false;
            }


            GameObject initializer = new GameObject("TFHMapComponentInjector");
            initializer.AddComponent<MapComponentInjector>();
            Object.DontDestroyOnLoad(initializer);

            return true;
        }
    }
}