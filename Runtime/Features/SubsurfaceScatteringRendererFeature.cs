using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.Features
{
    [DisallowMultipleRendererFeature("HoURP Subsurface Scattering")]
    public sealed class SubsurfaceScatteringRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private bool enabledForGameView = true;

        [SerializeField]
        private bool enabledForSceneView = true;

        [SerializeField]
        [Range(0.0f, 2.0f)]
        private float strength = 0.65f;

        [SerializeField]
        [Range(0.25f, 8.0f)]
        private float radius = 2.0f;

        [SerializeField]
        [Range(0.0001f, 0.25f)]
        private float depthTolerance = 0.03f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float normalTolerance = 0.25f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float sourcePreserve = 0.35f;

        private HoUrpContractRegistry registry;
        private SubsurfaceScatteringPass sssPass;
        private Material material;

        public HoUrpContractRegistry Registry => registry;

        public override void Create()
        {
            registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Shader shader = Shader.Find(HoUrpShaderPropertyIds.SubsurfaceScatteringShaderName);
            material = shader != null
                ? CoreUtils.CreateEngineMaterial(shader)
                : null;

            sssPass = new SubsurfaceScatteringPass(registry)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || sssPass == null || material == null)
            {
                return;
            }

            sssPass.Setup(material, strength, radius, depthTolerance, normalTolerance, sourcePreserve);
            renderer.EnqueuePass(sssPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            sssPass = null;
            registry = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        private sealed class SubsurfaceScatteringPass : ScriptableRenderPass
        {
            private const int DiffusionPassIndex = 1;
            private const int CompositePassIndex = 2;
            private static readonly ProfilingSampler DiffusionSampler = new ProfilingSampler("HoURP SSS Diffusion");
            private static readonly ProfilingSampler CompositeSampler = new ProfilingSampler("HoURP SSS Composite");
            private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

            private readonly HoUrpContractRegistry registry;
            private Material material;
            private float strength;
            private float radius;
            private float depthTolerance;
            private float normalTolerance;
            private float sourcePreserve;

            public SubsurfaceScatteringPass(HoUrpContractRegistry registry)
            {
                this.registry = registry;
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(
                Material material,
                float strength,
                float radius,
                float depthTolerance,
                float normalTolerance,
                float sourcePreserve)
            {
                this.material = material;
                this.strength = Mathf.Max(0.0f, strength);
                this.radius = Mathf.Max(0.0f, radius);
                this.depthTolerance = Mathf.Max(0.0001f, depthTolerance);
                this.normalTolerance = Mathf.Clamp01(normalTolerance);
                this.sourcePreserve = Mathf.Clamp01(sourcePreserve);
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                {
                    return;
                }

                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();
                if (!resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovMaskId, out TextureHandle maskIdTexture)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovNormalDepth, out TextureHandle normalDepthTexture)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovSurfaceData, out TextureHandle surfaceDataTexture)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovSssSource, out TextureHandle aovSssSourceTexture))
                {
                    return;
                }

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                HoUrpSssResourceDeclaration.DeclareMinimalSssTextures(
                    renderGraph,
                    resources,
                    registry,
                    cameraData.cameraTargetDescriptor);

                TextureHandle sssSourceTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.SssSource);
                TextureHandle sssDiffusionTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.SssDiffusion);

                RecordSourcePass(
                    renderGraph,
                    aovSssSourceTexture,
                    sssSourceTexture);

                RecordDiffusionPass(
                    renderGraph,
                    sssSourceTexture,
                    sssDiffusionTexture);

                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                TextureHandle sourceColor = resourceData.activeColorTexture;
                TextureDesc colorCopyDesc = renderGraph.GetTextureDesc(sourceColor);
                colorCopyDesc.name = "_HoUrpSssColorCopy";
                colorCopyDesc.clearBuffer = false;
                TextureHandle colorCopy = renderGraph.CreateTexture(colorCopyDesc);
                renderGraph.AddBlitPass(sourceColor, colorCopy, Vector2.one, Vector2.zero, passName: "HoURP SSS Color Copy");

                RecordCompositePass(
                    renderGraph,
                    normalDepthTexture,
                    surfaceDataTexture,
                    sssDiffusionTexture,
                    colorCopy,
                    sourceColor);
            }

            private void RecordSourcePass(
                UnityRenderGraph renderGraph,
                TextureHandle aovSssSourceTexture,
                TextureHandle sssSourceTexture)
            {
                renderGraph.AddBlitPass(
                    aovSssSourceTexture,
                    sssSourceTexture,
                    Vector2.one,
                    Vector2.zero,
                    passName: "HoURP SSS Source Copy");
            }

            private void RecordDiffusionPass(
                UnityRenderGraph renderGraph,
                TextureHandle sssSourceTexture,
                TextureHandle sssDiffusionTexture)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssRadius, radius);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssDepthTolerance, depthTolerance);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssNormalTolerance, normalTolerance);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssSourcePreserve, sourcePreserve);

                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        sssSourceTexture,
                        sssDiffusionTexture,
                        material,
                        DiffusionPassIndex)
                    {
                        propertyBlock = propertyBlock,
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.SssSourceTexture
                    };

                renderGraph.AddBlitPass(blitParameters, passName: "HoURP SSS Diffusion");
            }

            private void RecordCompositePass(
                UnityRenderGraph renderGraph,
                TextureHandle normalDepthTexture,
                TextureHandle surfaceDataTexture,
                TextureHandle sssDiffusionTexture,
                TextureHandle colorCopy,
                TextureHandle destination)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "HoURP SSS Composite",
                    out PassData passData,
                    CompositeSampler))
                {
                    passData.normalDepthTexture = normalDepthTexture;
                    passData.surfaceDataTexture = surfaceDataTexture;
                    passData.sssDiffusionTexture = sssDiffusionTexture;
                    passData.sourceColorTexture = colorCopy;
                    passData.material = material;
                    passData.strength = strength;
                    passData.radius = radius;
                    passData.depthTolerance = depthTolerance;
                    passData.normalTolerance = normalTolerance;
                    passData.sourcePreserve = sourcePreserve;
                    passData.passIndex = CompositePassIndex;

                    builder.UseTexture(normalDepthTexture, AccessFlags.Read);
                    builder.UseTexture(surfaceDataTexture, AccessFlags.Read);
                    builder.UseTexture(sssDiffusionTexture, AccessFlags.Read);
                    builder.UseTexture(colorCopy, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        BindCommonProperties(data, context);
                        context.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            data.passIndex,
                            MeshTopology.Triangles,
                            3,
                            1,
                            PropertyBlock);
                    });
                }
            }

            private static void BindCommonProperties(PassData data, RasterGraphContext context)
            {
                PropertyBlock.Clear();
                SetGlobalTextureIfValid(context, HoUrpShaderPropertyIds.AovMaskIdTexture, data.maskIdTexture);
                SetGlobalTextureIfValid(context, HoUrpShaderPropertyIds.AovNormalDepthTexture, data.normalDepthTexture);
                SetGlobalTextureIfValid(context, HoUrpShaderPropertyIds.AovSurfaceDataTexture, data.surfaceDataTexture);
                SetGlobalTextureIfValid(context, HoUrpShaderPropertyIds.AovSssSourceTexture, data.aovSssSourceTexture);
                SetGlobalTextureIfValid(context, HoUrpShaderPropertyIds.SssSourceTexture, data.sssSourceTexture);
                SetGlobalTextureIfValid(context, HoUrpShaderPropertyIds.SssDiffusionTexture, data.sssDiffusionTexture);
                SetGlobalTextureIfValid(context, HoUrpShaderPropertyIds.SourceColorTexture, data.sourceColorTexture);
                PropertyBlock.SetFloat(HoUrpShaderPropertyIds.SssStrength, data.strength);
                PropertyBlock.SetFloat(HoUrpShaderPropertyIds.SssRadius, data.radius);
                PropertyBlock.SetFloat(HoUrpShaderPropertyIds.SssDepthTolerance, data.depthTolerance);
                PropertyBlock.SetFloat(HoUrpShaderPropertyIds.SssNormalTolerance, data.normalTolerance);
                PropertyBlock.SetFloat(HoUrpShaderPropertyIds.SssSourcePreserve, data.sourcePreserve);
            }

            private static void SetGlobalTextureIfValid(
                RasterGraphContext context,
                int propertyId,
                TextureHandle texture)
            {
                if (texture.IsValid())
                {
                    context.cmd.SetGlobalTexture(propertyId, texture);
                }
            }

            private sealed class PassData
            {
                public TextureHandle maskIdTexture;
                public TextureHandle normalDepthTexture;
                public TextureHandle surfaceDataTexture;
                public TextureHandle aovSssSourceTexture;
                public TextureHandle sssSourceTexture;
                public TextureHandle sssDiffusionTexture;
                public TextureHandle sourceColorTexture;
                public Material material;
                public float strength;
                public float radius;
                public float depthTolerance;
                public float normalTolerance;
                public float sourcePreserve;
                public int passIndex;
            }
        }
    }
}
