using HoUrp.Extensions.Core;
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
        private Color colorTint = Color.white;

        [SerializeField]
        [Range(0.0f, 2.0f)]
        private float brightness = 1.0f;

        [SerializeField]
        [Range(0.0f, 2.0f)]
        private float contrast = 1.0f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float tintStrength = 0.15f;

        [SerializeField]
        private bool runSecondPass;

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

            imagePostPass.Setup(material, colorTint, brightness, contrast, tintStrength, runSecondPass);
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

        private sealed class ImagePostPrototypePass : ScriptableRenderPass
        {
            private const int ColorAdjustPassIndex = 0;
            private Material material;
            private Color colorTint;
            private float brightness;
            private float contrast;
            private float tintStrength;
            private bool runSecondPass;

            public ImagePostPrototypePass()
            {
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(
                Material material,
                Color colorTint,
                float brightness,
                float contrast,
                float tintStrength,
                bool runSecondPass)
            {
                this.material = material;
                this.colorTint = colorTint;
                this.brightness = Mathf.Max(0.0f, brightness);
                this.contrast = Mathf.Max(0.0f, contrast);
                this.tintStrength = Mathf.Clamp01(tintStrength);
                this.runSecondPass = runSecondPass;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
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
                RecordColorAdjustPass(renderGraph, current, alternate, "HoURP ImagePost Prototype Pass 0", brightness, contrast, tintStrength);
                Swap(ref current, ref alternate);

                if (runSecondPass)
                {
                    RecordColorAdjustPass(renderGraph, current, alternate, "HoURP ImagePost Prototype Pass 1", 1.0f, 1.0f, tintStrength * 0.5f);
                    Swap(ref current, ref alternate);
                }

                renderGraph.AddBlitPass(current, cameraColor, Vector2.one, Vector2.zero, passName: "HoURP ImagePost Copy Back");
            }

            private void RecordColorAdjustPass(
                UnityRenderGraph renderGraph,
                TextureHandle source,
                TextureHandle destination,
                string passName,
                float passBrightness,
                float passContrast,
                float passTintStrength)
            {
                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        source,
                        destination,
                        material,
                        ColorAdjustPassIndex)
                    {
                        propertyBlock = CreatePropertyBlock(passBrightness, passContrast, passTintStrength),
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.SourceColorTexture
                    };

                renderGraph.AddBlitPass(blitParameters, passName: passName);
            }

            private MaterialPropertyBlock CreatePropertyBlock(float passBrightness, float passContrast, float passTintStrength)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(HoUrpShaderPropertyIds.ImagePostColorTint, colorTint);
                propertyBlock.SetVector(
                    HoUrpShaderPropertyIds.ImagePostParams,
                    new Vector4(passBrightness, passContrast, passTintStrength, 0.0f));
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
