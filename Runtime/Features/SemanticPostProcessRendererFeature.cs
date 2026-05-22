using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
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

        [SerializeField]
        private Color tintColor = new Color(0.0f, 0.65f, 1.0f, 0.35f);

        [SerializeField]
        [Range(0, 7)]
        private int objectCustomChannel;

        [SerializeField]
        [Range(0, 3)]
        private int materialCustomChannel;

        [SerializeField]
        private SemanticPostLayer[] layers = SemanticPostLayer.CreateDefaults();

        private HoUrpContractRegistry registry;
        private SemanticPostProcessPass semanticPostPass;
        private Material semanticPostMaterial;

        public override void Create()
        {
            registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Shader shader = Shader.Find(HoUrpShaderPropertyIds.SemanticPostShaderName);
            semanticPostMaterial = shader != null
                ? CoreUtils.CreateEngineMaterial(shader)
                : null;

            semanticPostPass = new SemanticPostProcessPass(registry)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || semanticPostPass == null || semanticPostMaterial == null)
            {
                return;
            }

            EnsureLayers();
            semanticPostPass.Setup(semanticPostMaterial, tintColor, objectCustomChannel, materialCustomChannel, layers);
            renderer.EnqueuePass(semanticPostPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(semanticPostMaterial);
            semanticPostMaterial = null;
            semanticPostPass = null;
            registry = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        private void OnValidate()
        {
            EnsureLayers();
        }

        private void EnsureLayers()
        {
            if (layers == null || layers.Length != SemanticPostLayer.MaxLayerCount)
            {
                SemanticPostLayer[] defaults = SemanticPostLayer.CreateDefaults();
                if (layers != null)
                {
                    int copyCount = Mathf.Min(layers.Length, defaults.Length);
                    for (int i = 0; i < copyCount; i++)
                    {
                        if (layers[i] != null)
                        {
                            defaults[i] = layers[i];
                        }
                    }
                }

                layers = defaults;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    layers[i] = SemanticPostLayer.CreateDisabled();
                }

                layers[i].EnsureRules();
            }
        }

        public enum SemanticPostEffect
        {
            None = 0,
            SemanticTint = 1
        }

        public enum SemanticPostBlendMode
        {
            Alpha = 0,
            Add = 1,
            Multiply = 2,
            Screen = 3
        }

        public enum SemanticPostRuleSource
        {
            Always = 0,
            MaskWeight = 1,
            ObjectId = 2,
            GroupId = 3,
            Flags = 4,
            ObjectCustom0 = 10,
            ObjectCustom1 = 11,
            ObjectCustom2 = 12,
            ObjectCustom3 = 13,
            ObjectCustom4 = 14,
            ObjectCustom5 = 15,
            ObjectCustom6 = 16,
            ObjectCustom7 = 17,
            MaterialClass = 20,
            SssProfile = 21,
            Thickness = 22,
            Curvature = 23,
            MaterialCustom0 = 30,
            MaterialCustom1 = 31,
            MaterialCustom2 = 32,
            MaterialCustom3 = 33,
            SssWeight = 40,
            SssCompositeWeight = 41,
            LinearDepth = 50,
            WorldNormalFacing = 51
        }

        public enum SemanticPostRuleOperator
        {
            Always = 0,
            Greater = 1,
            Less = 2,
            Range = 3,
            EqualByte = 4,
            FlagsAny = 5,
            FlagsAll = 6
        }

        public enum SemanticPostRuleCombine
        {
            Replace = 0,
            Or = 1,
            And = 2,
            Subtract = 3,
            Multiply = 4
        }

        [Serializable]
        public sealed class SemanticPostRule
        {
            [SerializeField]
            public bool enabled = true;

            [SerializeField]
            public SemanticPostRuleSource source = SemanticPostRuleSource.Always;

            [SerializeField]
            public SemanticPostRuleOperator op = SemanticPostRuleOperator.Always;

            [SerializeField]
            public SemanticPostRuleCombine combine = SemanticPostRuleCombine.Replace;

            [SerializeField]
            public Vector2 range = new Vector2(0.5f, 1.0f);

            [SerializeField]
            [Range(0, 255)]
            public int byteValue;
        }

        [Serializable]
        public sealed class SemanticPostLayer
        {
            public const int MaxLayerCount = 4;
            public const int MaxRuleCount = 4;

            [SerializeField]
            public bool enabled;

            [SerializeField]
            public SemanticPostEffect effect = SemanticPostEffect.SemanticTint;

            [SerializeField]
            public SemanticPostBlendMode blendMode = SemanticPostBlendMode.Alpha;

            [SerializeField]
            [Range(0.0f, 1.0f)]
            public float opacity = 0.35f;

            [SerializeField]
            public Color color = new Color(0.0f, 0.65f, 1.0f, 1.0f);

            [SerializeField]
            public SemanticPostRule[] rules = CreateDefaultRules();

            public static SemanticPostLayer[] CreateDefaults()
            {
                SemanticPostLayer[] result = new SemanticPostLayer[MaxLayerCount];
                result[0] = new SemanticPostLayer
                {
                    enabled = true,
                    effect = SemanticPostEffect.SemanticTint,
                    blendMode = SemanticPostBlendMode.Alpha,
                    opacity = 0.35f,
                    color = new Color(0.0f, 0.65f, 1.0f, 1.0f),
                    rules = CreateDefaultRules()
                };

                for (int i = 1; i < result.Length; i++)
                {
                    result[i] = CreateDisabled();
                }

                return result;
            }

            public static SemanticPostLayer CreateDisabled()
            {
                return new SemanticPostLayer
                {
                    enabled = false,
                    effect = SemanticPostEffect.SemanticTint,
                    blendMode = SemanticPostBlendMode.Alpha,
                    opacity = 0.0f,
                    color = Color.white,
                    rules = CreateDefaultRules()
                };
            }

            public void EnsureRules()
            {
                if (rules == null || rules.Length != MaxRuleCount)
                {
                    SemanticPostRule[] defaults = CreateDefaultRules();
                    if (rules != null)
                    {
                        int copyCount = Mathf.Min(rules.Length, defaults.Length);
                        for (int i = 0; i < copyCount; i++)
                        {
                            if (rules[i] != null)
                            {
                                defaults[i] = rules[i];
                            }
                        }
                    }

                    rules = defaults;
                }

                for (int i = 0; i < rules.Length; i++)
                {
                    if (rules[i] == null)
                    {
                        rules[i] = new SemanticPostRule { enabled = false };
                    }
                }
            }

            private static SemanticPostRule[] CreateDefaultRules()
            {
                return new[]
                {
                    new SemanticPostRule
                    {
                        enabled = true,
                        source = SemanticPostRuleSource.ObjectCustom0,
                        op = SemanticPostRuleOperator.Greater,
                        combine = SemanticPostRuleCombine.Replace,
                        range = new Vector2(0.01f, 1.0f)
                    },
                    new SemanticPostRule { enabled = false },
                    new SemanticPostRule { enabled = false },
                    new SemanticPostRule { enabled = false }
                };
            }
        }

        private sealed class SemanticPostProcessPass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler MaskSampler = new ProfilingSampler("HoURP Semantic Post Mask");
            private static readonly ProfilingSampler CompositeSampler = new ProfilingSampler("HoURP Semantic Post Composite");
            private static readonly ProfilingSampler SetGlobalTextureSampler = new ProfilingSampler("HoURP Semantic Post Set Global Texture");
            private const int MaskPassIndex = 0;
            private const int CompositePassIndex = 1;

            private readonly HoUrpContractRegistry registry;
            private Material material;
            private Color tintColor;
            private int objectCustomChannel;
            private int materialCustomChannel;
            private SemanticPostLayer[] layers;

            public SemanticPostProcessPass(HoUrpContractRegistry registry)
            {
                this.registry = registry;
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(
                Material material,
                Color tintColor,
                int objectCustomChannel,
                int materialCustomChannel,
                SemanticPostLayer[] layers)
            {
                this.material = material;
                this.tintColor = tintColor;
                this.objectCustomChannel = Mathf.Clamp(objectCustomChannel, 0, 7);
                this.materialCustomChannel = Mathf.Clamp(materialCustomChannel, 0, 3);
                this.layers = layers;
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
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovObjectCustom0_3, out TextureHandle objectCustom0Texture)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovObjectCustom4_7, out TextureHandle objectCustom1Texture)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovSurfaceData, out TextureHandle surfaceDataTexture)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3, out TextureHandle materialCustomTexture)
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovSssSource, out TextureHandle sssSourceTexture))
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                if (!HoUrpRenderGraphResourceDeclaration.TryDeclareTexture(
                    renderGraph,
                    resources,
                    registry,
                    HoUrpBuiltInNames.Resources.SemanticPostMask,
                    cameraData.cameraTargetDescriptor,
                    out HoUrpRenderGraphResource maskResource))
                {
                    return;
                }

                resources.TryGetTexture(HoUrpBuiltInNames.Resources.SssSource, out TextureHandle preparedSssSourceTexture);
                resources.TryGetTexture(HoUrpBuiltInNames.Resources.SssDiffusion, out TextureHandle sssDiffusionTexture);
                if (!preparedSssSourceTexture.IsValid())
                {
                    preparedSssSourceTexture = sssSourceTexture;
                }

                if (!sssDiffusionTexture.IsValid())
                {
                    sssDiffusionTexture = sssSourceTexture;
                }

                TextureHandle sourceColor = resourceData.activeColorTexture;
                TextureDesc colorCopyDesc = renderGraph.GetTextureDesc(sourceColor);
                colorCopyDesc.name = "_HoUrpSemanticPostColorCopy";
                colorCopyDesc.clearBuffer = false;
                TextureHandle colorCopy = renderGraph.CreateTexture(colorCopyDesc);
                renderGraph.AddBlitPass(sourceColor, colorCopy, Vector2.one, Vector2.zero, passName: "HoURP Semantic Post Color Copy");

                MaterialPropertyBlock propertyBlock = CreatePropertyBlock(
                    layers,
                    tintColor,
                    objectCustomChannel,
                    materialCustomChannel);

                RecordMaskPass(
                    renderGraph,
                    maskIdTexture,
                    normalDepthTexture,
                    objectCustom0Texture,
                    objectCustom1Texture,
                    surfaceDataTexture,
                    materialCustomTexture,
                    sssSourceTexture,
                    preparedSssSourceTexture,
                    sssDiffusionTexture,
                    maskResource.Texture,
                    propertyBlock);

                RecordCompositePass(
                    renderGraph,
                    colorCopy,
                    maskResource.Texture,
                    sourceColor,
                    propertyBlock);
            }

            private void RecordMaskPass(
                UnityRenderGraph renderGraph,
                TextureHandle maskIdTexture,
                TextureHandle normalDepthTexture,
                TextureHandle objectCustom0Texture,
                TextureHandle objectCustom1Texture,
                TextureHandle surfaceDataTexture,
                TextureHandle materialCustomTexture,
                TextureHandle sssSourceTexture,
                TextureHandle preparedSssSourceTexture,
                TextureHandle sssDiffusionTexture,
                TextureHandle semanticPostMaskTexture,
                MaterialPropertyBlock propertyBlock)
            {
                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                    "HoURP Semantic Post Mask",
                    out MaskPassData passData,
                    MaskSampler))
                {
                    passData.maskIdTexture = maskIdTexture;
                    passData.normalDepthTexture = normalDepthTexture;
                    passData.objectCustom0Texture = objectCustom0Texture;
                    passData.objectCustom1Texture = objectCustom1Texture;
                    passData.surfaceDataTexture = surfaceDataTexture;
                    passData.materialCustomTexture = materialCustomTexture;
                    passData.sssSourceTexture = sssSourceTexture;
                    passData.preparedSssSourceTexture = preparedSssSourceTexture;
                    passData.sssDiffusionTexture = sssDiffusionTexture;
                    passData.material = material;
                    passData.propertyBlock = propertyBlock;

                    builder.UseTexture(maskIdTexture, AccessFlags.Read);
                    builder.UseTexture(normalDepthTexture, AccessFlags.Read);
                    builder.UseTexture(objectCustom0Texture, AccessFlags.Read);
                    builder.UseTexture(objectCustom1Texture, AccessFlags.Read);
                    builder.UseTexture(surfaceDataTexture, AccessFlags.Read);
                    builder.UseTexture(materialCustomTexture, AccessFlags.Read);
                    builder.UseTexture(sssSourceTexture, AccessFlags.Read);
                    builder.UseTexture(preparedSssSourceTexture, AccessFlags.Read);
                    builder.UseTexture(sssDiffusionTexture, AccessFlags.Read);

                    builder.SetRenderAttachment(semanticPostMaskTexture, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovMaskIdTexture, data.maskIdTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovNormalDepthTexture, data.normalDepthTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovObjectCustom0_3Texture, data.objectCustom0Texture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovObjectCustom4_7Texture, data.objectCustom1Texture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovSurfaceDataTexture, data.surfaceDataTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovMaterialCustom0_3Texture, data.materialCustomTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovSssSourceTexture, data.sssSourceTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.SssSourceTexture, data.preparedSssSourceTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.SssDiffusionTexture, data.sssDiffusionTexture);

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
                TextureHandle colorCopy,
                TextureHandle semanticPostMaskTexture,
                TextureHandle destination,
                MaterialPropertyBlock propertyBlock)
            {
                RecordGlobalTextureBinding(
                    renderGraph,
                    semanticPostMaskTexture,
                    HoUrpShaderPropertyIds.SemanticPostMaskTexture,
                    "HoURP Semantic Post Bind Mask");

                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        colorCopy,
                        destination,
                        material,
                        CompositePassIndex)
                    {
                        propertyBlock = propertyBlock,
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.SourceColorTexture
                    };

                renderGraph.AddBlitPass(blitParameters, passName: "HoURP Semantic Post Composite");
            }

            private static MaterialPropertyBlock CreatePropertyBlock(
                SemanticPostLayer[] layers,
                Color fallbackTint,
                int fallbackObjectCustomChannel,
                int fallbackMaterialCustomChannel)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                Vector4[] layerParams = new Vector4[SemanticPostLayer.MaxLayerCount];
                Vector4[] layerColors = new Vector4[SemanticPostLayer.MaxLayerCount];
                Vector4[] ruleParams = new Vector4[SemanticPostLayer.MaxLayerCount * SemanticPostLayer.MaxRuleCount];
                Vector4[] ruleValues = new Vector4[SemanticPostLayer.MaxLayerCount * SemanticPostLayer.MaxRuleCount];
                SemanticPostLayer[] effectiveLayers = HasEnabledLayer(layers)
                    ? layers
                    : SemanticPostLayer.CreateDefaults();

                BuildShaderData(
                    effectiveLayers,
                    fallbackTint,
                    fallbackObjectCustomChannel,
                    fallbackMaterialCustomChannel,
                    layerParams,
                    layerColors,
                    ruleParams,
                    ruleValues);

                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.SemanticPostLayerParams, layerParams);
                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.SemanticPostLayerColors, layerColors);
                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.SemanticPostRuleParams, ruleParams);
                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.SemanticPostRuleValues, ruleValues);
                return propertyBlock;
            }

            private static bool HasEnabledLayer(SemanticPostLayer[] layers)
            {
                if (layers == null)
                {
                    return false;
                }

                for (int i = 0; i < layers.Length; i++)
                {
                    if (layers[i] != null && layers[i].enabled)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static void BuildShaderData(
                SemanticPostLayer[] layers,
                Color fallbackTint,
                int fallbackObjectCustomChannel,
                int fallbackMaterialCustomChannel,
                Vector4[] layerParams,
                Vector4[] layerColors,
                Vector4[] ruleParams,
                Vector4[] ruleValues)
            {
                for (int layerIndex = 0; layerIndex < SemanticPostLayer.MaxLayerCount; layerIndex++)
                {
                    SemanticPostLayer layer = layers != null && layerIndex < layers.Length ? layers[layerIndex] : null;
                    bool enabled = layer != null && layer.enabled;
                    SemanticPostEffect effect = layer != null ? layer.effect : SemanticPostEffect.None;
                    SemanticPostBlendMode blend = layer != null ? layer.blendMode : SemanticPostBlendMode.Alpha;
                    float opacity = layer != null ? Mathf.Clamp01(layer.opacity) : 0.0f;
                    Color color = layer != null ? layer.color : fallbackTint;

                    layerParams[layerIndex] = new Vector4(
                        enabled ? 1.0f : 0.0f,
                        (float)effect,
                        (float)blend,
                        opacity);
                    layerColors[layerIndex] = color;

                    for (int ruleIndex = 0; ruleIndex < SemanticPostLayer.MaxRuleCount; ruleIndex++)
                    {
                        int packedRuleIndex = layerIndex * SemanticPostLayer.MaxRuleCount + ruleIndex;
                        SemanticPostRule rule = layer != null && layer.rules != null && ruleIndex < layer.rules.Length
                            ? layer.rules[ruleIndex]
                            : null;

                        if (rule == null || !rule.enabled)
                        {
                            ruleParams[packedRuleIndex] = new Vector4(-1.0f, 0.0f, 0.0f, 0.0f);
                            ruleValues[packedRuleIndex] = Vector4.zero;
                            continue;
                        }

                        ruleParams[packedRuleIndex] = new Vector4(
                            (float)rule.source,
                            (float)rule.op,
                            (float)rule.combine,
                            0.0f);
                        ruleValues[packedRuleIndex] = new Vector4(
                            rule.op == SemanticPostRuleOperator.EqualByte
                                || rule.op == SemanticPostRuleOperator.FlagsAny
                                || rule.op == SemanticPostRuleOperator.FlagsAll
                                ? Mathf.Clamp(rule.byteValue, 0, 255)
                                : rule.range.x,
                            rule.range.y,
                            fallbackObjectCustomChannel,
                            fallbackMaterialCustomChannel);
                    }
                }
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
                    SetGlobalTextureSampler))
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

            private sealed class MaskPassData
            {
                public TextureHandle maskIdTexture;
                public TextureHandle normalDepthTexture;
                public TextureHandle objectCustom0Texture;
                public TextureHandle objectCustom1Texture;
                public TextureHandle surfaceDataTexture;
                public TextureHandle materialCustomTexture;
                public TextureHandle sssSourceTexture;
                public TextureHandle preparedSssSourceTexture;
                public TextureHandle sssDiffusionTexture;
                public Material material;
                public MaterialPropertyBlock propertyBlock;
            }

            private sealed class GlobalTexturePassData
            {
                public TextureHandle texture;
                public int propertyId;
            }
        }
    }
}
