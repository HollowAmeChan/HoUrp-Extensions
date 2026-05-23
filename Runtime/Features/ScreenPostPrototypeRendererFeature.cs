using HoUrp.Extensions.Core;
using HoUrp.Extensions.PostProcess;
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
        private ScreenPostLayerSettings[] layers = { ScreenPostLayerSettings.CreateDefault() };

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

            EnsureLayers();
            screenPostPass.Setup(material, layers);
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

        private void OnValidate()
        {
            EnsureLayers();
        }

        private void EnsureLayers()
        {
            if (layers == null || layers.Length == 0)
            {
                layers = new[] { ScreenPostLayerSettings.CreateDefault() };
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    layers[i] = ScreenPostLayerSettings.CreateDisabled("ScreenPost Layer " + i);
                }

                layers[i].Ensure();
            }
        }

        private sealed class ScreenPostPrototypePass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler MaskSampler = new ProfilingSampler("HoURP ScreenPost Rule Mask");
            private static readonly ProfilingSampler CompositeSampler = new ProfilingSampler("HoURP ScreenPost Composite");
            private const int MaskPassIndex = 0;
            private const int CompositePassIndex = 1;

            private Material material;
            private ScreenPostLayerSettings[] layers;

            public ScreenPostPrototypePass()
            {
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material, ScreenPostLayerSettings[] layers)
            {
                this.material = material;
                this.layers = layers;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null || layers == null || layers.Length == 0)
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();
                TextureHandle cameraColor = resourceData.activeColorTexture;

                for (int i = 0; i < layers.Length; i++)
                {
                    ScreenPostLayerSettings layer = layers[i];
                    if (layer == null)
                    {
                        continue;
                    }

                    layer.Ensure();
                    if (!layer.enabled || layer.opacity <= 0.0001f)
                    {
                        continue;
                    }

                    if (!TryGetRuleTextures(resources, layer.ruleSet, out RuleTextureSet ruleTextures))
                    {
                        continue;
                    }

                    TextureDesc colorCopyDesc = renderGraph.GetTextureDesc(cameraColor);
                    colorCopyDesc.name = "ScreenPost.SourceCopy." + i;
                    colorCopyDesc.clearBuffer = false;
                    TextureHandle colorCopy = renderGraph.CreateTexture(colorCopyDesc);
                    renderGraph.AddBlitPass(cameraColor, colorCopy, Vector2.one, Vector2.zero, passName: "HoURP ScreenPost Source Copy " + i);

                    TextureDesc maskDesc = renderGraph.GetTextureDesc(cameraColor);
                    maskDesc.name = "ScreenPost.RuleMask." + i;
                    maskDesc.clearBuffer = true;
                    maskDesc.clearColor = Color.clear;
                    TextureHandle ruleMask = renderGraph.CreateTexture(maskDesc);

                    MaterialPropertyBlock propertyBlock = CreatePropertyBlock(layer);

                    RecordMaskPass(renderGraph, ruleTextures, ruleMask, propertyBlock, i);
                    RecordCompositePass(renderGraph, colorCopy, ruleMask, cameraColor, propertyBlock, i);
                }
            }

            private bool TryGetRuleTextures(HoUrpRenderGraphResources resources, ScreenPostRuleSet ruleSet, out RuleTextureSet textures)
            {
                textures = default;
                if (ruleSet == null || !ruleSet.enabled)
                {
                    return false;
                }

                ruleSet.EnsureRules();

                bool needsMaskId = false;
                bool needsObjectCustom0 = false;
                bool needsObjectCustom1 = false;
                bool needsSurfaceData = false;
                bool needsNormalDepth = false;

                for (int i = 0; i < ruleSet.rules.Length; i++)
                {
                    ScreenPostRule rule = ruleSet.rules[i];
                    if (rule == null || !rule.enabled)
                    {
                        continue;
                    }

                    switch (rule.source)
                    {
                        case ScreenPostRuleSource.MaskWeight:
                        case ScreenPostRuleSource.ObjectId:
                        case ScreenPostRuleSource.GroupId:
                        case ScreenPostRuleSource.Flags:
                            needsMaskId = true;
                            break;
                        case ScreenPostRuleSource.ObjectCustom0:
                        case ScreenPostRuleSource.ObjectCustom1:
                        case ScreenPostRuleSource.ObjectCustom2:
                        case ScreenPostRuleSource.ObjectCustom3:
                            needsObjectCustom0 = true;
                            break;
                        case ScreenPostRuleSource.ObjectCustom4:
                        case ScreenPostRuleSource.ObjectCustom5:
                        case ScreenPostRuleSource.ObjectCustom6:
                        case ScreenPostRuleSource.ObjectCustom7:
                            needsObjectCustom1 = true;
                            break;
                        case ScreenPostRuleSource.MaterialClass:
                        case ScreenPostRuleSource.Thickness:
                        case ScreenPostRuleSource.Curvature:
                            needsSurfaceData = true;
                            break;
                        case ScreenPostRuleSource.LinearDepth:
                        case ScreenPostRuleSource.WorldNormalFacing:
                            needsNormalDepth = true;
                            break;
                    }
                }

                if (needsMaskId && !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovMaskId, out textures.maskIdTexture))
                {
                    return false;
                }

                if (needsObjectCustom0 && !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovObjectCustom0_3, out textures.objectCustom0Texture))
                {
                    return false;
                }

                if (needsObjectCustom1 && !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovObjectCustom4_7, out textures.objectCustom1Texture))
                {
                    return false;
                }

                if (needsSurfaceData && !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovSurfaceData, out textures.surfaceDataTexture))
                {
                    return false;
                }

                if (needsNormalDepth && !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovNormalDepth, out textures.normalDepthTexture))
                {
                    return false;
                }

                return true;
            }

            private void RecordMaskPass(
                UnityRenderGraph renderGraph,
                RuleTextureSet textures,
                TextureHandle ruleMask,
                MaterialPropertyBlock propertyBlock,
                int layerIndex)
            {
                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                    "HoURP ScreenPost Rule Mask " + layerIndex,
                    out MaskPassData passData,
                    MaskSampler))
                {
                    passData.textures = textures;
                    passData.material = material;
                    passData.propertyBlock = propertyBlock;

                    if (textures.maskIdTexture.IsValid())
                    {
                        builder.UseTexture(textures.maskIdTexture, AccessFlags.Read);
                    }

                    if (textures.normalDepthTexture.IsValid())
                    {
                        builder.UseTexture(textures.normalDepthTexture, AccessFlags.Read);
                    }

                    if (textures.objectCustom0Texture.IsValid())
                    {
                        builder.UseTexture(textures.objectCustom0Texture, AccessFlags.Read);
                    }

                    if (textures.objectCustom1Texture.IsValid())
                    {
                        builder.UseTexture(textures.objectCustom1Texture, AccessFlags.Read);
                    }

                    if (textures.surfaceDataTexture.IsValid())
                    {
                        builder.UseTexture(textures.surfaceDataTexture, AccessFlags.Read);
                    }

                    builder.SetRenderAttachment(ruleMask, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                    {
                        if (data.textures.maskIdTexture.IsValid())
                        {
                            context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovMaskIdTexture, data.textures.maskIdTexture);
                        }

                        if (data.textures.normalDepthTexture.IsValid())
                        {
                            context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovNormalDepthTexture, data.textures.normalDepthTexture);
                        }

                        if (data.textures.objectCustom0Texture.IsValid())
                        {
                            context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovObjectCustom0_3Texture, data.textures.objectCustom0Texture);
                        }

                        if (data.textures.objectCustom1Texture.IsValid())
                        {
                            context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovObjectCustom4_7Texture, data.textures.objectCustom1Texture);
                        }

                        if (data.textures.surfaceDataTexture.IsValid())
                        {
                            context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovSurfaceDataTexture, data.textures.surfaceDataTexture);
                        }

                        context.cmd.DrawProcedural(Matrix4x4.identity, data.material, MaskPassIndex, MeshTopology.Triangles, 3, 1, data.propertyBlock);
                    });
                }
            }

            private void RecordCompositePass(
                UnityRenderGraph renderGraph,
                TextureHandle source,
                TextureHandle ruleMask,
                TextureHandle destination,
                MaterialPropertyBlock propertyBlock,
                int layerIndex)
            {
                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                    "HoURP ScreenPost Composite " + layerIndex,
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
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.material, CompositePassIndex, MeshTopology.Triangles, 3, 1, data.propertyBlock);
                    });
                }
            }

            private static MaterialPropertyBlock CreatePropertyBlock(ScreenPostLayerSettings layer)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(HoUrpShaderPropertyIds.ScreenPostTintColor, layer.color);
                propertyBlock.SetVector(
                    HoUrpShaderPropertyIds.ScreenPostParams,
                    new Vector4(layer.opacity, layer.ruleSet.defaultValue, layer.ruleSet.invert ? 1.0f : 0.0f, 0.0f));
                propertyBlock.SetVector(HoUrpShaderPropertyIds.ScreenPostLayerParams, new Vector4((float)layer.blendMode, 0.0f, 0.0f, 0.0f));

                Vector4[] ruleParams = new Vector4[ScreenPostRuleSet.MaxRuleCount];
                Vector4[] ruleValues = new Vector4[ScreenPostRuleSet.MaxRuleCount];
                for (int i = 0; i < ruleParams.Length; i++)
                {
                    ScreenPostRule rule = layer.ruleSet.rules != null && i < layer.ruleSet.rules.Length ? layer.ruleSet.rules[i] : null;
                    if (rule == null || !rule.enabled || !layer.ruleSet.enabled)
                    {
                        ruleParams[i] = new Vector4(-1.0f, 0.0f, 0.0f, 0.0f);
                        continue;
                    }

                    ruleParams[i] = new Vector4((float)rule.source, (float)rule.op, (float)rule.combine, Mathf.Clamp01(rule.weight));
                    ruleValues[i] = new Vector4(
                        rule.op == ScreenPostRuleOperator.EqualByte || rule.op == ScreenPostRuleOperator.FlagsAny || rule.op == ScreenPostRuleOperator.FlagsAll
                            ? Mathf.Clamp(rule.byteValue, 0, 255)
                            : rule.range.x,
                        rule.range.y,
                        0.0f,
                        0.0f);
                }

                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.ScreenPostRuleParams, ruleParams);
                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.ScreenPostRuleValues, ruleValues);
                return propertyBlock;
            }

            private struct RuleTextureSet
            {
                public TextureHandle maskIdTexture;
                public TextureHandle normalDepthTexture;
                public TextureHandle objectCustom0Texture;
                public TextureHandle objectCustom1Texture;
                public TextureHandle surfaceDataTexture;
            }

            private sealed class MaskPassData
            {
                public RuleTextureSet textures;
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
