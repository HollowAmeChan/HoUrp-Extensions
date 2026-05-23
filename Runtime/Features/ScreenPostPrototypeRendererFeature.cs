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
    [DisallowMultipleRendererFeature("HoURP ScreenPost Prototype")]
    public sealed class ScreenPostPrototypeRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private bool enabledForGameView = true;

        [SerializeField]
        private bool enabledForSceneView = true;

        [SerializeField]
        private Color tintColor = new Color(0.05f, 0.85f, 1.0f, 0.65f);

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float opacity = 0.5f;

        [SerializeField]
        [Min(0.0f)]
        private float depthFadeDistance = 100.0f;

        [SerializeField]
        private bool previewWhenAovMaskIsEmpty = true;

        private ScreenPostPrototypePass screenPostPass;
        private Material material;

        public override void Create()
        {
            Shader shader = Shader.Find(HoUrpShaderPropertyIds.ScreenPostPrototypeShaderName);
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
            screenPostPass = new ScreenPostPrototypePass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || screenPostPass == null || material == null)
            {
                return;
            }

            screenPostPass.Setup(material, tintColor, opacity, depthFadeDistance, previewWhenAovMaskIsEmpty);
            renderer.EnqueuePass(screenPostPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            screenPostPass = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        private sealed class ScreenPostPrototypePass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler MaskSampler = new ProfilingSampler("HoURP ScreenPost Prototype Mask");
            private static readonly ProfilingSampler CompositeSampler = new ProfilingSampler("HoURP ScreenPost Prototype Composite");
            private const int MaskPassIndex = 0;
            private const int CompositePassIndex = 1;

            private Material material;
            private Color tintColor;
            private float opacity;
            private float depthFadeDistance;
            private bool previewWhenAovMaskIsEmpty;

            public ScreenPostPrototypePass()
            {
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(
                Material material,
                Color tintColor,
                float opacity,
                float depthFadeDistance,
                bool previewWhenAovMaskIsEmpty)
            {
                this.material = material;
                this.tintColor = tintColor;
                this.opacity = Mathf.Clamp01(opacity);
                this.depthFadeDistance = Mathf.Max(0.0f, depthFadeDistance);
                this.previewWhenAovMaskIsEmpty = previewWhenAovMaskIsEmpty;
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
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovNormalDepth, out TextureHandle normalDepthTexture))
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                TextureHandle sourceColor = resourceData.activeColorTexture;
                TextureDesc colorCopyDesc = renderGraph.GetTextureDesc(sourceColor);
                colorCopyDesc.name = "ScreenPost.SourceCopy";
                colorCopyDesc.clearBuffer = false;
                TextureHandle colorCopy = renderGraph.CreateTexture(colorCopyDesc);
                renderGraph.AddBlitPass(sourceColor, colorCopy, Vector2.one, Vector2.zero, passName: "HoURP ScreenPost Source Copy");

                TextureDesc maskDesc = renderGraph.GetTextureDesc(sourceColor);
                maskDesc.name = "ScreenPost.RuleMask";
                maskDesc.clearBuffer = true;
                maskDesc.clearColor = Color.clear;
                TextureHandle ruleMask = renderGraph.CreateTexture(maskDesc);

                MaterialPropertyBlock propertyBlock = CreatePropertyBlock(tintColor, opacity, depthFadeDistance);

                RecordMaskPass(
                    renderGraph,
                    maskIdTexture,
                    normalDepthTexture,
                    ruleMask,
                    propertyBlock);

                RecordCompositePass(
                    renderGraph,
                    colorCopy,
                    ruleMask,
                    sourceColor,
                    propertyBlock);
            }

            private void RecordMaskPass(
                UnityRenderGraph renderGraph,
                TextureHandle maskIdTexture,
                TextureHandle normalDepthTexture,
                TextureHandle ruleMask,
                MaterialPropertyBlock propertyBlock)
            {
                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                    "HoURP ScreenPost Prototype Mask",
                    out MaskPassData passData,
                    MaskSampler))
                {
                    passData.maskIdTexture = maskIdTexture;
                    passData.normalDepthTexture = normalDepthTexture;
                    passData.material = material;
                    passData.propertyBlock = propertyBlock;

                    builder.UseTexture(maskIdTexture, AccessFlags.Read);
                    builder.UseTexture(normalDepthTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(ruleMask, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovMaskIdTexture, data.maskIdTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovNormalDepthTexture, data.normalDepthTexture);
                        context.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            MaskPassIndex,
                            MeshTopology.Triangles,
                            3,
                            1,
                            data.propertyBlock);
                    });
                }
            }

            private void RecordCompositePass(
                UnityRenderGraph renderGraph,
                TextureHandle source,
                TextureHandle ruleMask,
                TextureHandle destination,
                MaterialPropertyBlock propertyBlock)
            {
                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                    "HoURP ScreenPost Prototype Composite",
                    out CompositePassData passData,
                    CompositeSampler))
                {
                    passData.source = source;
                    passData.ruleMask = ruleMask;
                    passData.material = material;
                    passData.propertyBlock = propertyBlock;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(ruleMask, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.SourceColorTexture, data.source);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.ScreenPostMaskTexture, data.ruleMask);
                        context.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            CompositePassIndex,
                            MeshTopology.Triangles,
                            3,
                            1,
                            data.propertyBlock);
                    });
                }
            }

            private MaterialPropertyBlock CreatePropertyBlock(Color tintColor, float opacity, float depthFadeDistance)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(HoUrpShaderPropertyIds.ScreenPostTintColor, tintColor);
                propertyBlock.SetVector(
                    HoUrpShaderPropertyIds.ScreenPostParams,
                    new Vector4(opacity, depthFadeDistance, previewWhenAovMaskIsEmpty ? 1.0f : 0.0f, 0.0f));
                return propertyBlock;
            }

            private sealed class MaskPassData
            {
                public TextureHandle maskIdTexture;
                public TextureHandle normalDepthTexture;
                public Material material;
                public MaterialPropertyBlock propertyBlock;
            }

            private sealed class CompositePassData
            {
                public TextureHandle source;
                public TextureHandle ruleMask;
                public Material material;
                public MaterialPropertyBlock propertyBlock;
            }
        }
    }
}
