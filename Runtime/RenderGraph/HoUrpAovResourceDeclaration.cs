using HoUrp.Extensions.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.RenderGraph
{
    public static class HoUrpAovResourceDeclaration
    {
        public static void DeclareMinimalAovTextures(
            UnityRenderGraph renderGraph,
            HoUrpRenderGraphResources resources,
            HoUrpContractRegistry registry,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            DeclareRequiredTexture(
                renderGraph,
                resources,
                registry,
                HoUrpBuiltInNames.Resources.AovMaskId,
                cameraTextureDescriptor);

            DeclareRequiredTexture(
                renderGraph,
                resources,
                registry,
                HoUrpBuiltInNames.Resources.AovNormalDepth,
                cameraTextureDescriptor);
        }

        private static void DeclareRequiredTexture(
            UnityRenderGraph renderGraph,
            HoUrpRenderGraphResources resources,
            HoUrpContractRegistry registry,
            HoUrpIdentifier id,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (!HoUrpRenderGraphResourceDeclaration.TryDeclareTexture(
                renderGraph,
                resources,
                registry,
                id,
                cameraTextureDescriptor,
                out _))
            {
                throw new System.InvalidOperationException($"Required AOV resource '{id}' is not registered.");
            }
        }
    }
}
