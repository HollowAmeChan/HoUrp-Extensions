using HoUrp.Extensions.Core;
using HoUrp.Extensions.ShadowCast;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.RenderGraph
{
    public static class HoUrpShadowCastResourceDeclaration
    {
        public static TextureHandle DeclareAtlas(
            UnityRenderGraph renderGraph,
            HoUrpRenderGraphResources resources,
            HoUrpContractRegistry registry,
            int atlasSize,
            bool secondDirectional)
        {
            HoUrpIdentifier id = secondDirectional
                ? HoUrpBuiltInNames.Resources.ShadowCastSecondDirectionalAtlas
                : HoUrpBuiltInNames.Resources.ShadowCastAtlas;

            if (!registry.Resources.TryGet(id, out _))
            {
                throw new System.InvalidOperationException($"Required ShadowCast resource '{id}' is not registered.");
            }

            RenderTextureDescriptor descriptor = CreateAtlasDescriptor(atlasSize);
            TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                descriptor,
                id.Value,
                true,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp);

            resources.SetTexture(id, texture);
            return texture;
        }

        public static RenderTextureDescriptor CreateAtlasDescriptor(int atlasSize)
        {
            int size = Mathf.Max(1, atlasSize);
            return new RenderTextureDescriptor(size, size)
            {
                graphicsFormat = GraphicsFormat.None,
                depthStencilFormat = GraphicsFormat.D32_SFloat,
                dimension = TextureDimension.Tex2D,
                volumeDepth = 1,
                msaaSamples = 1,
                bindMS = false,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = false
            };
        }
    }
}
