using HoUrp.Extensions.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.RenderGraph
{
    public static class HoUrpOitResourceDeclaration
    {
        public static void DeclareMinimalOitTextures(
            UnityRenderGraph renderGraph,
            HoUrpRenderGraphResources resources,
            HoUrpContractRegistry registry,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor noMsaaDescriptor = cameraTextureDescriptor;
            noMsaaDescriptor.msaaSamples = 1;
            noMsaaDescriptor.bindMS = false;

            DeclareRequiredTexture(
                renderGraph,
                resources,
                registry,
                HoUrpBuiltInNames.Resources.OitOpaqueColor,
                noMsaaDescriptor);

            DeclareRequiredTexture(
                renderGraph,
                resources,
                registry,
                HoUrpBuiltInNames.Resources.OitAccumulation,
                cameraTextureDescriptor);

            DeclareRequiredTexture(
                renderGraph,
                resources,
                registry,
                HoUrpBuiltInNames.Resources.OitRevealage,
                cameraTextureDescriptor);

            DeclareRequiredTexture(
                renderGraph,
                resources,
                registry,
                HoUrpBuiltInNames.Resources.OitCompositeSource,
                noMsaaDescriptor);
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
                throw new System.InvalidOperationException($"Required OIT resource '{id}' is not registered.");
            }
        }
    }
}
