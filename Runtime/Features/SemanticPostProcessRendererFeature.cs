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

            readPass.Setup(readProbeMaterial, tintColor);
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

            public SemanticPostProcessReadPass()
            {
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material, Color tintColor)
            {
                this.material = material;
                this.tintColor = tintColor;
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
                    passData.sourceColorTexture = colorCopy;
                    passData.material = material;
                    passData.tintColor = tintColor;

                    builder.UseTexture(colorCopy, AccessFlags.Read);
                    builder.UseTexture(maskIdTexture, AccessFlags.Read);
                    builder.UseTexture(normalDepthTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(sourceColor, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData data, RasterGraphContext context)
                    {
                        PropertyBlock.Clear();
                        PropertyBlock.SetTexture(HoUrpShaderPropertyIds.SourceColorTexture, data.sourceColorTexture);
                        PropertyBlock.SetTexture(HoUrpShaderPropertyIds.AovMaskIdTexture, data.maskIdTexture);
                        PropertyBlock.SetTexture(HoUrpShaderPropertyIds.AovNormalDepthTexture, data.normalDepthTexture);
                        PropertyBlock.SetColor(HoUrpShaderPropertyIds.SemanticPostTintColor, data.tintColor);
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
                public TextureHandle sourceColorTexture;
                public Material material;
                public Color tintColor;
            }
        }
    }
}
