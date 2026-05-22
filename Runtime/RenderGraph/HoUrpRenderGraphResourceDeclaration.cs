using HoUrp.Extensions.Core;
using HoUrp.Extensions.Resources;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.RenderGraph
{
    public static class HoUrpRenderGraphResourceDeclaration
    {
        public static HoUrpRenderGraphResource DeclareTexture(
            UnityRenderGraph renderGraph,
            HoUrpRenderGraphResources resources,
            ResourceDefinition definition,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (renderGraph == null)
            {
                throw new System.ArgumentNullException(nameof(renderGraph));
            }

            if (resources == null)
            {
                throw new System.ArgumentNullException(nameof(resources));
            }

            TextureHandle texture = CreateTexture(renderGraph, definition, cameraTextureDescriptor);
            resources.SetTexture(definition.Id, texture);
            return new HoUrpRenderGraphResource(definition, texture);
        }

        public static bool TryDeclareTexture(
            UnityRenderGraph renderGraph,
            HoUrpRenderGraphResources resources,
            HoUrpContractRegistry registry,
            HoUrpIdentifier id,
            RenderTextureDescriptor cameraTextureDescriptor,
            out HoUrpRenderGraphResource resource)
        {
            if (registry == null)
            {
                throw new System.ArgumentNullException(nameof(registry));
            }

            if (!registry.Resources.TryGet(id, out ResourceDefinition definition))
            {
                resource = default;
                return false;
            }

            resource = DeclareTexture(renderGraph, resources, definition, cameraTextureDescriptor);
            return true;
        }

        private static TextureHandle CreateTexture(
            UnityRenderGraph renderGraph,
            ResourceDefinition definition,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            switch (definition.Kind)
            {
                case ResourceKind.Texture2D:
                    return renderGraph.CreateTexture(
                        HoUrpRenderGraphTextureDescFactory.CreateColorDesc(definition, cameraTextureDescriptor));
                case ResourceKind.DepthTexture:
                    return UniversalRenderer.CreateRenderGraphTexture(
                        renderGraph,
                        HoUrpRenderGraphTextureDescFactory.CreateDepthDesc(definition, cameraTextureDescriptor),
                        definition.Id.Value,
                        true,
                        FilterMode.Point,
                        TextureWrapMode.Clamp);
                default:
                    throw new System.NotSupportedException(
                        $"Resource kind '{definition.Kind}' cannot be declared as a RenderGraph texture.");
            }
        }
    }
}
