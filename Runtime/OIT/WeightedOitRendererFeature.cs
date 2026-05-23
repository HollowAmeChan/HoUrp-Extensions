using System.Collections.Generic;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.OIT
{
    [DisallowMultipleRendererFeature("HoURP Weighted OIT")]
    public sealed class WeightedOitRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private WeightedOitSettings settings = new WeightedOitSettings();

        private HoUrpContractRegistry registry;
        private WeightedOitPass accumulationPass;
        private WeightedOitPass compositePass;
        private Material compositeMaterial;

        public HoUrpContractRegistry Registry => registry;

        public WeightedOitSettings Settings => settings;

        public override void Create()
        {
            registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();
            accumulationPass = new WeightedOitPass(registry, WeightedOitPassMode.Accumulation)
            {
                renderPassEvent = settings != null
                    ? settings.accumulationPassEvent
                    : RenderPassEvent.BeforeRenderingTransparents
            };
            compositePass = new WeightedOitPass(registry, WeightedOitPassMode.Composite)
            {
                renderPassEvent = settings != null
                    ? settings.compositePassEvent
                    : RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings == null
                || !settings.ShouldRender(renderingData.cameraData.cameraType)
                || accumulationPass == null
                || compositePass == null)
            {
                return;
            }

            EnsureMaterial();
            if (compositeMaterial == null)
            {
                return;
            }

            accumulationPass.Setup(settings, compositeMaterial);
            compositePass.Setup(settings, compositeMaterial);
            renderer.EnqueuePass(accumulationPass);
            renderer.EnqueuePass(compositePass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(compositeMaterial);
            compositeMaterial = null;
            accumulationPass = null;
            compositePass = null;
            registry = null;
        }

        private void EnsureMaterial()
        {
            if (compositeMaterial != null)
            {
                return;
            }

            Shader shader = settings != null && settings.compositeShader != null
                ? settings.compositeShader
                : Shader.Find(WeightedOitShaderConstants.CompositeShaderName);

            if (shader != null)
            {
                compositeMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        private enum WeightedOitPassMode
        {
            Accumulation,
            Composite
        }

        private sealed class WeightedOitPass : ScriptableRenderPass
        {
            private const int CompositePassIndex = 0;
            private static readonly List<ShaderTagId> ShaderTagIds = new List<ShaderTagId>
            {
                WeightedOitShaderConstants.ShaderTagId
            };

            private static readonly ProfilingSampler ResetSampler = new ProfilingSampler("HoURP Weighted OIT Reset");
            private static readonly ProfilingSampler ClearSampler = new ProfilingSampler("HoURP Weighted OIT Clear");
            private static readonly ProfilingSampler AccumulationSampler = new ProfilingSampler("HoURP Weighted OIT Accumulation");
            private static readonly ProfilingSampler CompositeSampler = new ProfilingSampler("HoURP Weighted OIT Composite");

            private readonly HoUrpContractRegistry registry;
            private readonly WeightedOitPassMode mode;
            private WeightedOitSettings settings;
            private Material compositeMaterial;

            public WeightedOitPass(HoUrpContractRegistry registry, WeightedOitPassMode mode)
            {
                this.registry = registry;
                this.mode = mode;
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(WeightedOitSettings settings, Material compositeMaterial)
            {
                this.settings = settings;
                this.compositeMaterial = compositeMaterial;
                renderPassEvent = mode == WeightedOitPassMode.Accumulation
                    ? settings.accumulationPassEvent
                    : settings.compositePassEvent;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                if (settings == null)
                {
                    return;
                }

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();

                TextureHandle cameraColor = resourceData.activeColorTexture;
                if (!cameraColor.IsValid())
                {
                    RecordResetPass(renderGraph, "HoURP Weighted OIT Reset");
                    return;
                }

                if (mode == WeightedOitPassMode.Accumulation)
                {
                    RecordAccumulationChain(
                        renderGraph,
                        resources,
                        renderingData,
                        cameraData,
                        lightData,
                        resourceData,
                        cameraColor);
                    return;
                }

                RecordCompositeChain(renderGraph, resources, resourceData, cameraColor);
            }

            private void RecordAccumulationChain(
                UnityRenderGraph renderGraph,
                HoUrpRenderGraphResources resources,
                UniversalRenderingData renderingData,
                UniversalCameraData cameraData,
                UniversalLightData lightData,
                UniversalResourceData resourceData,
                TextureHandle cameraColor)
            {
                HoUrpOitResourceDeclaration.DeclareMinimalOitTextures(
                    renderGraph,
                    resources,
                    registry,
                    cameraData.cameraTargetDescriptor);

                TextureHandle opaqueColor = resources.GetTexture(HoUrpBuiltInNames.Resources.OitOpaqueColor);
                TextureHandle accumulation = resources.GetTexture(HoUrpBuiltInNames.Resources.OitAccumulation);
                TextureHandle revealage = resources.GetTexture(HoUrpBuiltInNames.Resources.OitRevealage);

                RecordResetPass(renderGraph, "HoURP Weighted OIT Reset");
                RecordCopyPass(renderGraph, cameraColor, opaqueColor, "HoURP Weighted OIT Opaque Copy");
                RecordClearPass(renderGraph, accumulation, revealage);

                RecordAccumulationPass(
                    renderGraph,
                    renderingData,
                    cameraData,
                    lightData,
                    resourceData,
                    opaqueColor,
                    accumulation,
                    revealage);
            }

            private void RecordCompositeChain(
                UnityRenderGraph renderGraph,
                HoUrpRenderGraphResources resources,
                UniversalResourceData resourceData,
                TextureHandle cameraColor)
            {
                if (resourceData.isActiveTargetBackBuffer
                    || compositeMaterial == null
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.OitAccumulation, out TextureHandle accumulation)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.OitRevealage, out TextureHandle revealage)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.OitCompositeSource, out TextureHandle compositeSource))
                {
                    RecordResetPass(renderGraph, "HoURP Weighted OIT Final Reset");
                    return;
                }

                RecordCopyPass(renderGraph, cameraColor, compositeSource, "HoURP Weighted OIT Composite Source Copy");
                RecordCompositePass(renderGraph, compositeSource, accumulation, revealage, cameraColor);
                RecordResetPass(renderGraph, "HoURP Weighted OIT Final Reset");
            }

            private static void RecordResetPass(UnityRenderGraph renderGraph, string passName)
            {
                using (var builder = renderGraph.AddRasterRenderPass<ResetPassData>(
                    passName,
                    out _,
                    ResetSampler))
                {
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (ResetPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalFloat(HoUrpShaderPropertyIds.OitActive, 0.0f);
                    });
                }
            }

            private static void RecordCopyPass(
                UnityRenderGraph renderGraph,
                TextureHandle source,
                TextureHandle destination,
                string passName)
            {
                if (!source.IsValid() || !destination.IsValid())
                {
                    return;
                }

                renderGraph.AddBlitPass(source, destination, Vector2.one, Vector2.zero, passName: passName);
            }

            private static void RecordClearPass(
                UnityRenderGraph renderGraph,
                TextureHandle accumulation,
                TextureHandle revealage)
            {
                if (!accumulation.IsValid() || !revealage.IsValid())
                {
                    return;
                }

                using (var builder = renderGraph.AddRasterRenderPass<ClearPassData>(
                    "HoURP Weighted OIT Clear",
                    out ClearPassData passData,
                    ClearSampler))
                {
                    passData.accumulation = accumulation;
                    passData.revealage = revealage;
                    builder.SetRenderAttachment(accumulation, 0, AccessFlags.Write);
                    builder.SetRenderAttachment(revealage, 1, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetGlobalTextureAfterPass(accumulation, HoUrpShaderPropertyIds.OitAccumulationTexture);
                    builder.SetGlobalTextureAfterPass(revealage, HoUrpShaderPropertyIds.OitRevealageTexture);
                    builder.SetRenderFunc(static (ClearPassData data, RasterGraphContext context) =>
                    {
                        _ = data.accumulation;
                        _ = data.revealage;
                    });
                }
            }

            private void RecordAccumulationPass(
                UnityRenderGraph renderGraph,
                UniversalRenderingData renderingData,
                UniversalCameraData cameraData,
                UniversalLightData lightData,
                UniversalResourceData resourceData,
                TextureHandle opaqueColor,
                TextureHandle accumulation,
                TextureHandle revealage)
            {
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    ShaderTagIds,
                    renderingData,
                    cameraData,
                    lightData,
                    SortingCriteria.CommonTransparent);

                FilteringSettings filteringSettings = new FilteringSettings(
                    settings.RenderQueueRange,
                    settings.layerMask);

                RendererListParams rendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings);

                using (var builder = renderGraph.AddRasterRenderPass<AccumulationPassData>(
                    "HoURP Weighted OIT Accumulation",
                    out AccumulationPassData passData,
                    AccumulationSampler))
                {
                    passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
                    passData.opaqueColor = opaqueColor;
                    passData.weight = Mathf.Max(0.0f, settings.weight);
                    passData.alphaClipThreshold = Mathf.Clamp01(settings.alphaClipThreshold);

                    if (!passData.rendererList.IsValid())
                    {
                        return;
                    }

                    builder.UseRendererList(passData.rendererList);
                    builder.UseTexture(opaqueColor, AccessFlags.Read);
                    builder.SetRenderAttachment(accumulation, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachment(revealage, 1, AccessFlags.ReadWrite);

                    if (resourceData.activeDepthTexture.IsValid())
                    {
                        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    }

                    builder.SetGlobalTextureAfterPass(accumulation, HoUrpShaderPropertyIds.OitAccumulationTexture);
                    builder.SetGlobalTextureAfterPass(revealage, HoUrpShaderPropertyIds.OitRevealageTexture);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (AccumulationPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalFloat(HoUrpShaderPropertyIds.OitActive, 1.0f);
                        context.cmd.SetGlobalFloat(HoUrpShaderPropertyIds.OitWeight, data.weight);
                        context.cmd.SetGlobalFloat(HoUrpShaderPropertyIds.OitAlphaClipThreshold, data.alphaClipThreshold);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.OitOpaqueColorTexture, data.opaqueColor);
                        context.cmd.DrawRendererList(data.rendererList);
                    });
                }
            }

            private void RecordCompositePass(
                UnityRenderGraph renderGraph,
                TextureHandle source,
                TextureHandle accumulation,
                TextureHandle revealage,
                TextureHandle destination)
            {
                RecordGlobalTextureBinding(
                    renderGraph,
                    accumulation,
                    HoUrpShaderPropertyIds.OitAccumulationTexture,
                    "HoURP Weighted OIT Bind Accumulation");
                RecordGlobalTextureBinding(
                    renderGraph,
                    revealage,
                    HoUrpShaderPropertyIds.OitRevealageTexture,
                    "HoURP Weighted OIT Bind Revealage");
                RecordGlobalTextureBinding(
                    renderGraph,
                    source,
                    HoUrpShaderPropertyIds.OitCompositeSourceTexture,
                    "HoURP Weighted OIT Bind Composite Source");

                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        source,
                        destination,
                        compositeMaterial,
                        CompositePassIndex)
                    {
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.OitCompositeSourceTexture
                    };

                renderGraph.AddBlitPass(blitParameters, passName: "HoURP Weighted OIT Composite");
            }

            private static void RecordGlobalTextureBinding(
                UnityRenderGraph renderGraph,
                TextureHandle texture,
                int propertyId,
                string passName)
            {
                if (!texture.IsValid())
                {
                    return;
                }

                using (var builder = renderGraph.AddRasterRenderPass<GlobalTexturePassData>(
                    passName,
                    out GlobalTexturePassData passData,
                    CompositeSampler))
                {
                    passData.texture = texture;
                    passData.propertyId = propertyId;
                    builder.UseTexture(texture, AccessFlags.Read);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetGlobalTextureAfterPass(texture, propertyId);
                    builder.SetRenderFunc(static (GlobalTexturePassData data, RasterGraphContext context) =>
                    {
                        _ = data.texture;
                        _ = data.propertyId;
                    });
                }
            }

            private sealed class ResetPassData
            {
            }

            private sealed class ClearPassData
            {
                public TextureHandle accumulation;
                public TextureHandle revealage;
            }

            private sealed class AccumulationPassData
            {
                public RendererListHandle rendererList;
                public TextureHandle opaqueColor;
                public float weight;
                public float alphaClipThreshold;
            }

            private sealed class GlobalTexturePassData
            {
                public TextureHandle texture;
                public int propertyId;
            }
        }
    }
}
