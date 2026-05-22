using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.Features
{
    [DisallowMultipleRendererFeature("HoURP AOV Output")]
    public sealed class AovOutputRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private bool enabledForGameView = true;

        [SerializeField]
        private bool enabledForSceneView = true;

        private AovOutputDeclarationPass declarationPass;
        private HoUrpContractRegistry registry;

        public HoUrpContractRegistry Registry => registry;

        public override void Create()
        {
            registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();
            declarationPass = new AovOutputDeclarationPass(registry)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || declarationPass == null)
            {
                return;
            }

            renderer.EnqueuePass(declarationPass);
        }

        protected override void Dispose(bool disposing)
        {
            declarationPass = null;
            registry = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        private sealed class AovOutputDeclarationPass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("HoURP AOV Resource Declaration");
            private readonly HoUrpContractRegistry registry;

            public AovOutputDeclarationPass(HoUrpContractRegistry registry)
            {
                this.registry = registry;
                ConfigureInput(ScriptableRenderPassInput.None);
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();

                HoUrpAovResourceDeclaration.DeclareMinimalAovTextures(
                    renderGraph,
                    resources,
                    registry,
                    cameraData.cameraTargetDescriptor);

                TextureHandle maskIdTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovMaskId);
                TextureHandle normalDepthTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovNormalDepth);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "HoURP AOV Resource Declaration",
                    out PassData passData,
                    ProfilingSampler))
                {
                    passData.maskIdTexture = maskIdTexture;
                    passData.normalDepthTexture = normalDepthTexture;
                    builder.SetRenderAttachment(maskIdTexture, 0, AccessFlags.WriteAll);
                    builder.SetRenderAttachment(normalDepthTexture, 1, AccessFlags.WriteAll);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context)
                    {
                        _ = data.maskIdTexture;
                        _ = data.normalDepthTexture;
                        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1.0f, 0);
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
