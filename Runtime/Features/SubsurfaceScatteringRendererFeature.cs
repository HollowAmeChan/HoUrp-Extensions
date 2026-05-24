using HoUrp.Extensions.Core;
using HoUrp.Extensions.Filter;
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
    [DisallowMultipleRendererFeature("HoURP Subsurface Scattering")]
    public sealed class SubsurfaceScatteringRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        [InspectorName("启用")]
        private bool enabled = true;

        [SerializeField]
        [InspectorName("游戏视图")]
        private bool enabledForGameView = true;

        [SerializeField]
        [InspectorName("场景视图")]
        private bool enabledForSceneView = true;

        [SerializeField]
        [InspectorName("强度")]
        [Range(0.0f, 2.0f)]
        private float strength = 1.0f;

        [SerializeField]
        [InspectorName("半径")]
        [Range(0.0f, 24.0f)]
        private float radius = 8.0f;

        [SerializeField]
        [InspectorName("采样数")]
        [Range(1, 24)]
        private int sampleCount = 16;

        [SerializeField]
        [InspectorName("深度容差")]
        [Range(0.0001f, 2.0f)]
        private float depthTolerance = 0.08f;

        [SerializeField]
        [InspectorName("法线容差")]
        [Range(0.0f, 1.0f)]
        private float normalTolerance = 0.25f;

        [SerializeField]
        [InspectorName("保留源色")]
        [Range(0.0f, 1.0f)]
        private float sourcePreserve = 0.1f;

        [SerializeField]
        [InspectorName("调试模式")]
        private SssDebugMode debugMode = SssDebugMode.Off;

        [SerializeField]
        [InspectorName("材质配置")]
        private SssProfileSettings[] profiles = SssProfileSettings.CreateDefaults();

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
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || sssPass == null || material == null)
            {
                return;
            }

            EnsureProfiles();
            sssPass.Setup(material, strength, radius, sampleCount, depthTolerance, normalTolerance, sourcePreserve, debugMode, profiles);
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
            return enabled
                && ((enabledForGameView && cameraType == CameraType.Game)
                    || (enabledForSceneView && cameraType == CameraType.SceneView));
        }

        private void OnValidate()
        {
            EnsureProfiles();
        }

        private void EnsureProfiles()
        {
            if (profiles == null || profiles.Length != SssProfileSettings.MaxProfileCount)
            {
                SssProfileSettings[] defaults = SssProfileSettings.CreateDefaults();
                if (profiles != null)
                {
                    int copyCount = Mathf.Min(profiles.Length, defaults.Length);
                    for (int i = 0; i < copyCount; i++)
                    {
                        if (profiles[i] != null)
                        {
                            defaults[i] = profiles[i];
                        }
                    }
                }

                profiles = defaults;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] == null)
                {
                    profiles[i] = new SssProfileSettings
                    {
                        enabled = i == 0,
                        profileId = i + 1
                    };
                }
            }
        }

        [Serializable]
        public sealed class SssProfileSettings
        {
            public const int MaxProfileCount = 8;

            [SerializeField]
            [InspectorName("启用")]
            public bool enabled = true;

            [SerializeField]
            [InspectorName("配置 ID")]
            [Range(0, 255)]
            public int profileId = 1;

            [SerializeField]
            [InspectorName("扩散颜色")]
            public Color diffusionColor = new Color(1.0f, 0.43f, 0.32f, 1.0f);

            [SerializeField]
            [InspectorName("扩散半径")]
            [Range(0.0f, 24.0f)]
            public float diffusionRadius = 8.0f;

            [SerializeField]
            [InspectorName("保留源色")]
            [Range(0.0f, 1.0f)]
            public float sourcePreserve = 0.1f;

            [SerializeField]
            [InspectorName("厚度倍率")]
            [Range(0.0f, 4.0f)]
            public float thicknessScale = 1.0f;

            public static SssProfileSettings[] CreateDefaults()
            {
                var result = new SssProfileSettings[MaxProfileCount];
                result[0] = new SssProfileSettings
                {
                    enabled = true,
                    profileId = 1,
                    diffusionColor = new Color(1.0f, 0.43f, 0.32f, 1.0f),
                    diffusionRadius = 8.0f,
                    sourcePreserve = 0.1f,
                    thicknessScale = 1.0f
                };

                for (int i = 1; i < MaxProfileCount; i++)
                {
                    result[i] = new SssProfileSettings
                    {
                        enabled = false,
                        profileId = i + 1,
                        diffusionColor = new Color(1.0f, 0.43f, 0.32f, 1.0f),
                        diffusionRadius = 8.0f,
                        sourcePreserve = 0.1f,
                        thicknessScale = 1.0f
                    };
                }

                return result;
            }
        }

        private enum SssDebugMode
        {
            [InspectorName("关闭")]
            Off = 0,
            [InspectorName("参与遮罩")]
            Mask = 1,
            [InspectorName("源颜色")]
            Source = 2,
            [InspectorName("扩散结果")]
            Diffusion = 3,
            [InspectorName("合成权重")]
            CompositeWeight = 4,
            [InspectorName("配置 ID")]
            ProfileId = 5,
            [InspectorName("厚度")]
            Thickness = 6,
            [InspectorName("配置半径")]
            ProfileRadius = 7
        }

        private sealed class SubsurfaceScatteringPass : ScriptableRenderPass
        {
            private static readonly Vector4[] ProfileIds = new Vector4[SssProfileSettings.MaxProfileCount];
            private static readonly Vector4[] ProfileDiffusionParams = new Vector4[SssProfileSettings.MaxProfileCount];
            private static readonly Vector4[] ProfileShapeParams = new Vector4[SssProfileSettings.MaxProfileCount];
            private static readonly ProfilingSampler SetGlobalTextureSampler = new ProfilingSampler("HoURP SSS Set Global Texture");

            private readonly HoUrpContractRegistry registry;
            private Material material;
            private float strength;
            private float radius;
            private int sampleCount;
            private float depthTolerance;
            private float normalTolerance;
            private float sourcePreserve;
            private int debugMode;
            private SssProfileSettings[] profiles;

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
                int sampleCount,
                float depthTolerance,
                float normalTolerance,
                float sourcePreserve,
                SssDebugMode debugMode,
                SssProfileSettings[] profiles)
            {
                this.material = material;
                this.strength = Mathf.Max(0.0f, strength);
                this.radius = Mathf.Max(0.0f, radius);
                this.sampleCount = Mathf.Clamp(sampleCount, 1, 24);
                this.depthTolerance = Mathf.Max(0.0001f, depthTolerance);
                this.normalTolerance = Mathf.Clamp01(normalTolerance);
                this.sourcePreserve = Mathf.Clamp01(sourcePreserve);
                this.debugMode = Mathf.Max(0, (int)debugMode);
                this.profiles = profiles;
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
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovDiffuse, out TextureHandle diffuseTexture))
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
                TextureHandle sourceColor = resourceData.activeColorTexture;

                RecordSourcePass(
                    renderGraph,
                    maskIdTexture,
                    normalDepthTexture,
                    surfaceDataTexture,
                    diffuseTexture,
                    sssSourceTexture);

                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                TextureDesc colorCopyDesc = renderGraph.GetTextureDesc(sourceColor);
                colorCopyDesc.name = "_HoUrpSssColorCopy";
                colorCopyDesc.clearBuffer = false;
                TextureHandle colorCopy = renderGraph.CreateTexture(colorCopyDesc);
                renderGraph.AddBlitPass(sourceColor, colorCopy, Vector2.one, Vector2.zero, passName: "HoURP SSS Color Copy");

                RecordDiffusionPass(
                    renderGraph,
                    normalDepthTexture,
                    surfaceDataTexture,
                    colorCopy,
                    sssSourceTexture,
                    sssDiffusionTexture);

                RecordCompositePass(
                    renderGraph,
                    normalDepthTexture,
                    surfaceDataTexture,
                    sssSourceTexture,
                    sssDiffusionTexture,
                    colorCopy,
                    sourceColor);
            }

            private void RecordSourcePass(
                UnityRenderGraph renderGraph,
                TextureHandle maskIdTexture,
                TextureHandle normalDepthTexture,
                TextureHandle surfaceDataTexture,
                TextureHandle diffuseTexture,
                TextureHandle sssSourceTexture)
            {
                RecordGlobalTextureBinding(renderGraph, maskIdTexture, HoUrpShaderPropertyIds.AovMaskIdTexture, "HoURP SSS Bind AOV MaskId");
                RecordGlobalTextureBinding(renderGraph, normalDepthTexture, HoUrpShaderPropertyIds.AovNormalDepthTexture, "HoURP SSS Bind AOV NormalDepth");
                RecordGlobalTextureBinding(renderGraph, surfaceDataTexture, HoUrpShaderPropertyIds.AovSurfaceDataTexture, "HoURP SSS Bind AOV SurfaceData");

                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        diffuseTexture,
                        sssSourceTexture,
                        material,
                        HoUrpFilterIds.SssSourcePreparePass)
                    {
                        propertyBlock = CreatePropertyBlock(),
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.AovDiffuseTexture
                    };

                renderGraph.AddBlitPass(blitParameters, passName: "HoURP SSS Source Prepare");
            }

            private void RecordDiffusionPass(
                UnityRenderGraph renderGraph,
                TextureHandle normalDepthTexture,
                TextureHandle surfaceDataTexture,
                TextureHandle sourceColorTexture,
                TextureHandle sssSourceTexture,
                TextureHandle sssDiffusionTexture)
            {
                RecordGlobalTextureBinding(renderGraph, normalDepthTexture, HoUrpShaderPropertyIds.AovNormalDepthTexture, "HoURP SSS Bind AOV NormalDepth");
                RecordGlobalTextureBinding(renderGraph, surfaceDataTexture, HoUrpShaderPropertyIds.AovSurfaceDataTexture, "HoURP SSS Bind AOV SurfaceData");
                RecordGlobalTextureBinding(renderGraph, sourceColorTexture, HoUrpShaderPropertyIds.SourceColorTexture, "HoURP SSS Bind Source Color");

                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        sssSourceTexture,
                        sssDiffusionTexture,
                        material,
                        HoUrpFilterIds.SssDiffusionPass)
                    {
                        propertyBlock = CreatePropertyBlock(),
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.SssSourceTexture
                    };

                renderGraph.AddBlitPass(blitParameters, passName: "HoURP SSS Profile Diffusion");
            }

            private void RecordCompositePass(
                UnityRenderGraph renderGraph,
                TextureHandle normalDepthTexture,
                TextureHandle surfaceDataTexture,
                TextureHandle sssSourceTexture,
                TextureHandle sssDiffusionTexture,
                TextureHandle colorCopy,
                TextureHandle destination)
            {
                RecordGlobalTextureBinding(renderGraph, normalDepthTexture, HoUrpShaderPropertyIds.AovNormalDepthTexture, "HoURP SSS Bind AOV NormalDepth");
                RecordGlobalTextureBinding(renderGraph, surfaceDataTexture, HoUrpShaderPropertyIds.AovSurfaceDataTexture, "HoURP SSS Bind AOV SurfaceData");
                RecordGlobalTextureBinding(renderGraph, sssSourceTexture, HoUrpShaderPropertyIds.SssSourceTexture, "HoURP SSS Bind Source");
                RecordGlobalTextureBinding(renderGraph, sssDiffusionTexture, HoUrpShaderPropertyIds.SssDiffusionTexture, "HoURP SSS Bind Diffusion");

                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        colorCopy,
                        destination,
                        material,
                        HoUrpFilterIds.SssCompositePass)
                    {
                        propertyBlock = CreatePropertyBlock(),
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.SourceColorTexture
                    };

                renderGraph.AddBlitPass(blitParameters, passName: "HoURP SSS Composite");
            }

            private MaterialPropertyBlock CreatePropertyBlock()
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetVector(HoUrpShaderPropertyIds.SssParams, new Vector4(strength, radius, sampleCount, 1.0f));
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssDebugMode, debugMode);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssDepthTolerance, depthTolerance);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssNormalTolerance, normalTolerance);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssSourcePreserve, sourcePreserve);
                BuildProfileShaderData(profiles, radius, sourcePreserve);
                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.SssProfileIds, ProfileIds);
                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.SssProfileDiffusionParams, ProfileDiffusionParams);
                propertyBlock.SetVectorArray(HoUrpShaderPropertyIds.SssProfileShapeParams, ProfileShapeParams);
                return propertyBlock;
            }

            private static void BuildProfileShaderData(
                SssProfileSettings[] profiles,
                float fallbackRadius,
                float fallbackSourcePreserve)
            {
                for (int i = 0; i < SssProfileSettings.MaxProfileCount; i++)
                {
                    SssProfileSettings profile = profiles != null && i < profiles.Length ? profiles[i] : null;
                    bool enabled = profile != null && profile.enabled;
                    int profileId = profile != null ? profile.profileId : i + 1;
                    Color diffusionColor = profile != null ? profile.diffusionColor : new Color(1.0f, 0.43f, 0.32f, 1.0f);

                    ProfileIds[i] = new Vector4(
                        enabled ? Mathf.Clamp(profileId, 0, 255) : -1.0f,
                        enabled ? 1.0f : 0.0f,
                        0.0f,
                        0.0f);

                    ProfileDiffusionParams[i] = new Vector4(
                        ClampRadius(enabled ? profile.diffusionRadius : fallbackRadius, 24.0f),
                        enabled ? Mathf.Clamp01(profile.sourcePreserve) : Mathf.Clamp01(fallbackSourcePreserve),
                        diffusionColor.r,
                        diffusionColor.g);

                    ProfileShapeParams[i] = new Vector4(
                        enabled ? Mathf.Max(0.0f, profile.thicknessScale) : 1.0f,
                        diffusionColor.b,
                        0.0f,
                        diffusionColor.a);
                }
            }

            private static float ClampRadius(float radius, float maxRadius)
            {
                return Mathf.Clamp(Mathf.Max(0.0f, radius), 0.0f, Mathf.Max(0.0001f, maxRadius));
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

            private sealed class GlobalTexturePassData
            {
                public TextureHandle texture;
                public int propertyId;
            }
        }
    }
}
