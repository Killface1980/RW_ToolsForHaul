using UnityEngine;
using Verse;

namespace ToolsForHaul.Things
{
    [StaticConstructorOnStartup]
    public static class Textures
    {
        public static readonly Material ShadowMat = MaterialPool.MatFrom("Things/Special/DropPodShadow", ShaderDatabase.Transparent);
    }
}
