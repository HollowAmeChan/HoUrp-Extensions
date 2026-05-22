using System.Collections.Generic;
using HoUrp.Extensions.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace HoUrp.Extensions.RenderGraph
{
    public sealed class HoUrpRenderGraphResources : ContextItem
    {
        private readonly Dictionary<HoUrpIdentifier, TextureHandle> textures =
            new Dictionary<HoUrpIdentifier, TextureHandle>();

        public int Count => textures.Count;

        public void SetTexture(HoUrpIdentifier id, TextureHandle texture)
        {
            textures[id] = texture;
        }

        public bool TryGetTexture(HoUrpIdentifier id, out TextureHandle texture)
        {
            return textures.TryGetValue(id, out texture) && texture.IsValid();
        }

        public TextureHandle GetTexture(HoUrpIdentifier id)
        {
            if (!TryGetTexture(id, out TextureHandle texture))
            {
                throw new KeyNotFoundException($"RenderGraph texture '{id}' is not declared.");
            }

            return texture;
        }

        public override void Reset()
        {
            textures.Clear();
        }
    }
}
