using HoUrp.Extensions.Core;
using HoUrp.Extensions.Resources;
using UnityEngine.Rendering.RenderGraphModule;

namespace HoUrp.Extensions.RenderGraph
{
    public readonly struct HoUrpRenderGraphResource
    {
        public HoUrpRenderGraphResource(ResourceDefinition definition, TextureHandle texture)
        {
            Definition = definition;
            Texture = texture;
        }

        public ResourceDefinition Definition { get; }
        public HoUrpIdentifier Id => Definition.Id;
        public TextureHandle Texture { get; }
    }
}
