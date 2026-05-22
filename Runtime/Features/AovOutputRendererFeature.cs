using System.Collections.Generic;
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

        [SerializeField]
        private LayerMask layerMask = -1;

        private AovOutputPass aovOutputPass;
        private HoUrpContractRegistry registry;

        public HoUrpContractRegistry Registry => registry;

        public override void Create()
        {
            registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();
            aovOutputPass = new AovOutputPass(registry)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || aovOutputPass == null)
            {
                return;
            }

            aovOutputPass.Setup(layerMask);
            if (!aovOutputPass.HasFallbackMaterial)
            {
                return;
            }

            renderer.EnqueuePass(aovOutputPass);
        }

        protected override void Dispose(bool disposing)
        {
            aovOutputPass?.Dispose();
            aovOutputPass = null;
            registry = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        private sealed class AovOutputPass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("HoURP AOV Output");
            private static readonly List<ShaderTagId> ShaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalGBuffer"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward")
            };

            private static readonly List<ShaderTagId> ExplicitAovShaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("HoUrpAovOutput")
            };

            // Generated/material-owned AOV passes must run as their own shader pass.
            // The fallback path below intentionally keeps using overrideMaterial for legacy/authoring-only materials.
            private static readonly RenderQueueRange ExplicitAovRenderQueueRange = RenderQueueRange.all;

            private readonly HoUrpContractRegistry registry;
            private readonly Material fallbackMaterial;
            private LayerMask layerMask;

            public AovOutputPass(HoUrpContractRegistry registry)
            {
                this.registry = registry;
                ConfigureInput(ScriptableRenderPassInput.None);

                Shader fallbackShader = Shader.Find(HoUrpShaderPropertyIds.AovOutputFallbackShaderName);
                if (fallbackShader != null)
                {
                    fallbackMaterial = CoreUtils.CreateEngineMaterial(fallbackShader);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.AovMaskWeight, 1.0f);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.ObjectId, 1.0f);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.ObjectGroupId, 0.0f);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.ObjectFlags, 0.0f);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.ObjectCustomMask, 0.0f);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.MaterialClass, 0.0f);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.MaterialSssProfile, 0.0f);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.MaterialThickness, 0.0f);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.MaterialCurvature, 0.0f);
                    fallbackMaterial.SetVector(HoUrpShaderPropertyIds.MaterialCustom0_3, Vector4.zero);
                    fallbackMaterial.SetColor(HoUrpShaderPropertyIds.SssSourceColor, Color.black);
                    fallbackMaterial.SetFloat(HoUrpShaderPropertyIds.SssWeight, 0.0f);
                }
            }

            public bool HasFallbackMaterial => fallbackMaterial != null;

            public void Setup(LayerMask layerMask)
            {
                this.layerMask = layerMask;
            }

            public void Dispose()
            {
                CoreUtils.Destroy(fallbackMaterial);
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();

                HoUrpAovResourceDeclaration.DeclareMinimalAovTextures(
                    renderGraph,
                    resources,
                    registry,
                    cameraData.cameraTargetDescriptor);

                TextureHandle maskIdTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovMaskId);
                TextureHandle normalDepthTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovNormalDepth);
                TextureHandle objectCustom0Texture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovObjectCustom0_3);
                TextureHandle objectCustom1Texture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovObjectCustom4_7);
                TextureHandle surfaceDataTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovSurfaceData);
                TextureHandle materialCustomTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3);
                TextureHandle sssSourceTexture = resources.GetTexture(HoUrpBuiltInNames.Resources.AovSssSource);

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    ShaderTagIds,
                    renderingData,
                    cameraData,
                    lightData,
                    cameraData.defaultOpaqueSortFlags);
                drawingSettings.overrideMaterial = fallbackMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;

                RendererListParams rendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings);

                FilteringSettings explicitAovFilteringSettings = new FilteringSettings(ExplicitAovRenderQueueRange, layerMask);
                DrawingSettings explicitAovDrawingSettings = RenderingUtils.CreateDrawingSettings(
                    ExplicitAovShaderTagIds,
                    renderingData,
                    cameraData,
                    lightData,
                    SortingCriteria.CommonTransparent);

                RendererListParams explicitAovRendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    explicitAovDrawingSettings,
                    explicitAovFilteringSettings);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "HoURP AOV Output",
                    out PassData passData,
                    ProfilingSampler))
                {
                    passData.maskIdTexture = maskIdTexture;
                    passData.normalDepthTexture = normalDepthTexture;
                    passData.objectCustom0Texture = objectCustom0Texture;
                    passData.objectCustom1Texture = objectCustom1Texture;
                    passData.surfaceDataTexture = surfaceDataTexture;
                    passData.materialCustomTexture = materialCustomTexture;
                    passData.sssSourceTexture = sssSourceTexture;
                    passData.fallbackRendererList = renderGraph.CreateRendererList(rendererListParams);
                    passData.explicitAovRendererList = renderGraph.CreateRendererList(explicitAovRendererListParams);

                    if (!passData.fallbackRendererList.IsValid() && !passData.explicitAovRendererList.IsValid())
                    {
                        return;
                    }

                    if (passData.fallbackRendererList.IsValid())
                    {
                        builder.UseRendererList(passData.fallbackRendererList);
                    }

                    if (passData.explicitAovRendererList.IsValid())
                    {
                        builder.UseRendererList(passData.explicitAovRendererList);
                    }
                    builder.SetRenderAttachment(maskIdTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachment(normalDepthTexture, 1, AccessFlags.ReadWrite);
                    builder.SetRenderAttachment(objectCustom0Texture, 2, AccessFlags.ReadWrite);
                    builder.SetRenderAttachment(objectCustom1Texture, 3, AccessFlags.ReadWrite);
                    builder.SetRenderAttachment(surfaceDataTexture, 4, AccessFlags.ReadWrite);
                    builder.SetRenderAttachment(materialCustomTexture, 5, AccessFlags.ReadWrite);
                    builder.SetRenderAttachment(sssSourceTexture, 6, AccessFlags.ReadWrite);

                    if (resourceData.activeDepthTexture.IsValid())
                    {
                        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    }

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        _ = data.maskIdTexture;
                        _ = data.normalDepthTexture;
                        _ = data.objectCustom0Texture;
                        _ = data.objectCustom1Texture;
                        _ = data.surfaceDataTexture;
                        _ = data.materialCustomTexture;
                        _ = data.sssSourceTexture;
                        if (data.fallbackRendererList.IsValid())
                        {
                            context.cmd.DrawRendererList(data.fallbackRendererList);
                        }

                        if (data.explicitAovRendererList.IsValid())
                        {
                            context.cmd.DrawRendererList(data.explicitAovRendererList);
                        }
                    });
                }
            }

            private sealed class PassData
            {
                public TextureHandle maskIdTexture;
                public TextureHandle normalDepthTexture;
                public TextureHandle objectCustom0Texture;
                public TextureHandle objectCustom1Texture;
                public TextureHandle surfaceDataTexture;
                public TextureHandle materialCustomTexture;
                public TextureHandle sssSourceTexture;
                public RendererListHandle fallbackRendererList;
                public RendererListHandle explicitAovRendererList;
            }
        }
    }
}
