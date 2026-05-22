using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.Debugging
{
    [DisallowMultipleRendererFeature("HoURP AOV Debug")]
    public sealed class AovDebugRendererFeature : ScriptableRendererFeature
    {
        private enum AovDebugView
        {
            None,
            Mask,
            ObjectId,
            LinearDepth,
            WorldNormal
        }

        [SerializeField]
        private bool enabledForGameView = true;

        [SerializeField]
        private bool enabledForSceneView = true;

        [SerializeField]
        private AovDebugView selectedView = AovDebugView.None;

        private HoUrpContractRegistry registry;
        private AovDebugPass debugPass;
        private Material debugMaterial;

        public override void Create()
        {
            registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Shader debugShader = Shader.Find(HoUrpShaderPropertyIds.AovDebugShaderName);
            debugMaterial = debugShader != null
                ? CoreUtils.CreateEngineMaterial(debugShader)
                : null;

            debugPass = new AovDebugPass(registry)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData)
                || selectedView == AovDebugView.None
                || debugPass == null
                || debugMaterial == null)
            {
                return;
            }

            debugPass.Setup(debugMaterial, ResolveDebugViewId(selectedView), ResolveShaderMode(selectedView));
            renderer.EnqueuePass(debugPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(debugMaterial);
            debugMaterial = null;
            debugPass = null;
            registry = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        private static HoUrpIdentifier ResolveDebugViewId(AovDebugView view)
        {
            switch (view)
            {
                case AovDebugView.Mask:
                    return HoUrpBuiltInNames.DebugViews.AovMask;
                case AovDebugView.ObjectId:
                    return HoUrpBuiltInNames.DebugViews.AovObjectId;
                case AovDebugView.LinearDepth:
                    return HoUrpBuiltInNames.DebugViews.AovLinearDepth;
                case AovDebugView.WorldNormal:
                    return HoUrpBuiltInNames.DebugViews.AovWorldNormal;
                default:
                    return HoUrpBuiltInNames.DebugViews.AovMask;
            }
        }

        private static int ResolveShaderMode(AovDebugView view)
        {
            switch (view)
            {
                case AovDebugView.ObjectId:
                    return 1;
                case AovDebugView.LinearDepth:
                    return 2;
                case AovDebugView.WorldNormal:
                    return 3;
                default:
                    return 0;
            }
        }

        private sealed class AovDebugPass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("HoURP AOV Debug");
            private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();
            private readonly HoUrpContractRegistry registry;
            private Material material;
            private HoUrpIdentifier debugViewId;
            private int shaderMode;

            public AovDebugPass(HoUrpContractRegistry registry)
            {
                this.registry = registry;
                ConfigureInput(ScriptableRenderPassInput.None);
            }

            public void Setup(Material material, HoUrpIdentifier debugViewId, int shaderMode)
            {
                this.material = material;
                this.debugViewId = debugViewId;
                this.shaderMode = shaderMode;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null || !registry.DebugViews.TryGet(debugViewId, out DebugViewDefinition debugView))
                {
                    return;
                }

                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();
                if (!resources.TryGetTexture(debugView.SourceResource, out TextureHandle sourceTexture))
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle destination = resourceData.activeColorTexture;

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                    "HoURP AOV Debug",
                    out PassData passData,
                    ProfilingSampler))
                {
                    passData.sourceTexture = sourceTexture;
                    passData.sourceResource = debugView.SourceResource;
                    passData.material = material;
                    passData.shaderMode = shaderMode;

                    builder.UseTexture(sourceTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData data, RasterGraphContext context)
                    {
                        PropertyBlock.Clear();
                        if (data.sourceResource == HoUrpBuiltInNames.Resources.AovMaskId)
                        {
                            PropertyBlock.SetTexture(HoUrpShaderPropertyIds.AovMaskIdTexture, data.sourceTexture);
                        }
                        else
                        {
                            PropertyBlock.SetTexture(HoUrpShaderPropertyIds.AovNormalDepthTexture, data.sourceTexture);
                        }

                        PropertyBlock.SetInt(HoUrpShaderPropertyIds.AovDebugMode, data.shaderMode);
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
                public TextureHandle sourceTexture;
                public HoUrpIdentifier sourceResource;
                public Material material;
                public int shaderMode;
            }
        }
    }
}
