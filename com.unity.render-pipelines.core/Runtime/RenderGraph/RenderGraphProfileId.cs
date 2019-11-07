using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    internal enum RenderGraphProfileId
    {
        RenderGraphClear,
        RenderGraphClearDebug,
    }

    internal static class HDCustomSamplerExtension
    {
        public static ProfilingSampler Get(this RenderGraphProfileId samplerId, string nameOverride = "")
        {
            return ProfileSamplerList<RenderGraphProfileId>.Get(samplerId, nameOverride);
        }
    }
}
