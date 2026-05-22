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

        private SemanticPostProcessReadPass readPass;
        private Material readProbeMaterial;

        public override void Create()
        {
            Shader readProbeShader = Shader.Find(HoUrpShaderPropertyIds.SemanticPostAovReadProbeShaderName);
            readProbeMaterial = readProbeShader != null
                ? CoreUtils.CreateEngineMaterial(readProbeShader)
                : null;

            readPass = new SemanticPostProcessReadPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || readPass == null || readProbeMaterial == null)
            {
                return;
            }

            readPass.Setup(readProbeMaterial, tintColor, objectCustomChannel, materialCustomChannel);
            renderer.EnqueuePass(readPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(readProbeMaterial);
            readProbeMaterial = null;
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
            private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();
            private Material material;
            private Color tintColor;
            private int objectCustomChannel;
            private int materialCustomChannel;

            public SemanticPostProcessReadPass()
            {
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material, Color tintColor, int objectCustomChannel, int materialCustomChannel)
            {
                this.material = material;
                this.tintColor = tintColor;
                this.objectCustomChannel = Mathf.Clamp(objectCustomChannel, 0, 7);
                this.materialCustomChannel = Mathf.Clamp(materialCustomChannel, 0, 3);
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
                    || !resources.TryGetTexture(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3, out TextureHandle materialCustomTexture))
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
                colorCopyDesc.name = "_HoUrpSemanticPostColorCopy";
                colorCopyDesc.clearBuffer = false;
                TextureHandle colorCopy = renderGraph.CreateTexture(colorCopyDesc);
                renderGraph.AddBlitPass(sourceColor, colorCopy, Vector2.one, Vector2.zero, passName: "HoURP Semantic Post Color Copy");

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "HoURP Semantic Post AOV Read",
                    out PassData passData,
                    ProfilingSampler))
                {
                    passData.maskIdTexture = maskIdTexture;
                    passData.normalDepthTexture = normalDepthTexture;
                    passData.objectCustom0Texture = objectCustom0Texture;
                    passData.objectCustom1Texture = objectCustom1Texture;
                    passData.surfaceDataTexture = surfaceDataTexture;
                    passData.materialCustomTexture = materialCustomTexture;
                    passData.sourceColorTexture = colorCopy;
                    passData.material = material;
                    passData.tintColor = tintColor;
                    passData.objectCustomChannel = objectCustomChannel;
                    passData.materialCustomChannel = materialCustomChannel;

                    builder.UseTexture(colorCopy, AccessFlags.Read);
                    builder.UseTexture(maskIdTexture, AccessFlags.Read);
                    builder.UseTexture(normalDepthTexture, AccessFlags.Read);
                    builder.UseTexture(objectCustom0Texture, AccessFlags.Read);
                    builder.UseTexture(objectCustom1Texture, AccessFlags.Read);
                    builder.UseTexture(surfaceDataTexture, AccessFlags.Read);
                    builder.UseTexture(materialCustomTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(sourceColor, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        PropertyBlock.Clear();
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.SourceColorTexture, data.sourceColorTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovMaskIdTexture, data.maskIdTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovNormalDepthTexture, data.normalDepthTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovObjectCustom0_3Texture, data.objectCustom0Texture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovObjectCustom4_7Texture, data.objectCustom1Texture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovSurfaceDataTexture, data.surfaceDataTexture);
                        context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovMaterialCustom0_3Texture, data.materialCustomTexture);
                        PropertyBlock.SetColor(HoUrpShaderPropertyIds.SemanticPostTintColor, data.tintColor);
                        PropertyBlock.SetInt(HoUrpShaderPropertyIds.SemanticPostObjectCustomChannel, data.objectCustomChannel);
                        PropertyBlock.SetInt(HoUrpShaderPropertyIds.SemanticPostMaterialCustomChannel, data.materialCustomChannel);
                        context.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            0,
                            MeshTopology.Triangles,
                            3,
                            1,
                            PropertyBlock);
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
                public TextureHandle sourceColorTexture;
                public Material material;
                public Color tintColor;
                public int objectCustomChannel;
                public int materialCustomChannel;
            }
        }
    }
}
