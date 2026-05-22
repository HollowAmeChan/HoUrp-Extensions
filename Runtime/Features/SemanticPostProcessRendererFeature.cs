using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.Features
{
    [DisallowMultipleRendererFeature("HoURP Semantic Post Process")]
    public sealed class SemanticPostProcessRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private bool enabledForGameView = true;

        [SerializeField]
        private bool enabledForSceneView = true;

        private SemanticPostProcessReadPass readPass;

        public override void Create()
        {
            readPass = new SemanticPostProcessReadPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || readPass == null)
            {
                return;
            }

            renderer.EnqueuePass(readPass);
        }

        protected override void Dispose(bool disposing)
        {
            readPass = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        private sealed class SemanticPostProcessReadPass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("HoURP Semantic Post AOV Read");

            public SemanticPostProcessReadPass()
            {
                ConfigureInput(ScriptableRenderPassInput.None);
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();
                if (!resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovMaskId, out TextureHandle maskIdTexture)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovNormalDepth, out TextureHandle normalDepthTexture))
                {
                    return;
                }

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "HoURP Semantic Post AOV Read",
                    out PassData passData,
                    ProfilingSampler))
                {
                    passData.maskIdTexture = maskIdTexture;
                    passData.normalDepthTexture = normalDepthTexture;
                    builder.UseTexture(maskIdTexture, AccessFlags.Read);
                    builder.UseTexture(normalDepthTexture, AccessFlags.Read);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context)
                    {
                        _ = data.maskIdTexture;
                        _ = data.normalDepthTexture;
                    });
                }
            }

            private sealed class PassData
            {
                public TextureHandle maskIdTexture;
                public TextureHandle normalDepthTexture;
            }
        }
    }
}
