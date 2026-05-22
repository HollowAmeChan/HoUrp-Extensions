using HoUrp.Extensions.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.RenderGraph
{
    public static class HoUrpSssResourceDeclaration
    {
        public static void DeclareMinimalSssTextures(
            UnityRenderGraph renderGraph,
            HoUrpRenderGraphResources resources,
            HoUrpContractRegistry registry,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            DeclareRequiredTexture(
                renderGraph,
                resources,
                registry,
                HoUrpBuiltInNames.Resources.SssSource,
                cameraTextureDescriptor);

            DeclareRequiredTexture(
                renderGraph,
                resources,
                registry,
                HoUrpBuiltInNames.Resources.SssDiffusion,
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
                throw new System.InvalidOperationException($"Required SSS resource '{id}' is not registered.");
            }
        }
    }
}
