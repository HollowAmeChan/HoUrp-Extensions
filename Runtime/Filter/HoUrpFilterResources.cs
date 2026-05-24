using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace HoUrp.Extensions.Filter
{
    internal static class HoUrpFilterResources
    {
        public static TextureDesc CreateWorkTextureDesc(TextureDesc sourceDesc, string name)
        {
            sourceDesc.name = name;
            sourceDesc.clearBuffer = false;
            sourceDesc.format = sourceDesc.format == GraphicsFormat.None
                ? GraphicsFormat.R16G16B16A16_SFloat
                : sourceDesc.format;
            return sourceDesc;
        }
    }
}
