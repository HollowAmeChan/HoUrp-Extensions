using HoUrp.Extensions.Core;
using HoUrp.Extensions.PostProcess;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.Features
{
    [DisallowMultipleRendererFeature("HoURP ImagePost Prototype")]
    public sealed class ImagePostPrototypeRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private bool enabledForGameView = true;

        [SerializeField]
        private bool enabledForSceneView = true;

        [SerializeField]
        private ImagePostFilterSettings[] filters = { ImagePostFilterSettings.CreateDefault() };

        private ImagePostPrototypePass imagePostPass;
        private Material material;

        public override void Create()
        {
            Shader shader = Shader.Find(HoUrpShaderPropertyIds.ImagePostPrototypeShaderName);
            material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
            imagePostPass = new ImagePostPrototypePass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData) || imagePostPass == null || material == null)
            {
                return;
            }

            EnsureFilters();
            imagePostPass.Setup(material, filters);
            renderer.EnqueuePass(imagePostPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            imagePostPass = null;
        }

        private bool ShouldRender(in RenderingData renderingData)
        {
            CameraType cameraType = renderingData.cameraData.cameraType;
            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        private void OnValidate()
        {
            EnsureFilters();
        }

        private void EnsureFilters()
        {
            if (filters == null || filters.Length == 0)
            {
                filters = new[] { ImagePostFilterSettings.CreateDefault() };
            }

            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i] == null)
                {
                    filters[i] = ImagePostFilterSettings.CreateDefault("Color Adjust " + i);
                }

                filters[i].Ensure();
            }
        }

        private sealed class ImagePostPrototypePass : ScriptableRenderPass
        {
            private const int ColorAdjustPassIndex = 0;
            private Material material;
            private ImagePostFilterSettings[] filters;

            public ImagePostPrototypePass()
            {
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material, ImagePostFilterSettings[] filters)
            {
                this.material = material;
                this.filters = filters;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null || filters == null || filters.Length == 0)
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                if (!HasEnabledFilter(filters))
                {
                    return;
                }

                TextureHandle cameraColor = resourceData.activeColorTexture;
                TextureDesc workADesc = renderGraph.GetTextureDesc(cameraColor);
                workADesc.name = HoUrpBuiltInNames.PostFrameResources.ImageWorkA.Value;
                workADesc.clearBuffer = false;
                TextureHandle workA = renderGraph.CreateTexture(workADesc);

                TextureDesc workBDesc = renderGraph.GetTextureDesc(cameraColor);
                workBDesc.name = HoUrpBuiltInNames.PostFrameResources.ImageWorkB.Value;
                workBDesc.clearBuffer = false;
                TextureHandle workB = renderGraph.CreateTexture(workBDesc);

                renderGraph.AddBlitPass(cameraColor, workA, Vector2.one, Vector2.zero, passName: "HoURP ImagePost Source Copy");

                TextureHandle current = workA;
                TextureHandle alternate = workB;

                for (int i = 0; i < filters.Length; i++)
                {
                    ImagePostFilterSettings filter = filters[i];
                    if (filter == null)
                    {
                        continue;
                    }

                    filter.Ensure();
                    if (!filter.enabled)
                    {
                        continue;
                    }

                    RecordColorAdjustPass(renderGraph, current, alternate, filter, "HoURP ImagePost Filter " + i);
                    Swap(ref current, ref alternate);
                }

                renderGraph.AddBlitPass(current, cameraColor, Vector2.one, Vector2.zero, passName: "HoURP ImagePost Copy Back");
            }

            private static bool HasEnabledFilter(ImagePostFilterSettings[] filters)
            {
                for (int i = 0; i < filters.Length; i++)
                {
                    if (filters[i] != null && filters[i].enabled)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void RecordColorAdjustPass(
                UnityRenderGraph renderGraph,
                TextureHandle source,
                TextureHandle destination,
                ImagePostFilterSettings filter,
                string passName)
            {
                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(source, destination, material, ColorAdjustPassIndex)
                    {
                        propertyBlock = CreatePropertyBlock(filter),
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.SourceColorTexture
                    };

                renderGraph.AddBlitPass(blitParameters, passName: passName);
            }

            private static MaterialPropertyBlock CreatePropertyBlock(ImagePostFilterSettings filter)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(HoUrpShaderPropertyIds.ImagePostColorTint, filter.colorTint);
                propertyBlock.SetVector(
                    HoUrpShaderPropertyIds.ImagePostParams,
                    new Vector4(filter.brightness, filter.contrast, filter.tintStrength, 0.0f));
                return propertyBlock;
            }

            private static void Swap(ref TextureHandle current, ref TextureHandle alternate)
            {
                TextureHandle temp = current;
                current = alternate;
                alternate = temp;
            }
        }
    }
}
