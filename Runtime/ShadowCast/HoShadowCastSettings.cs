using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoUrp.Extensions.ShadowCast
{
    [Serializable]
    public sealed class HoShadowCastSettings
    {
        [Tooltip("Skip ShadowCast without removing the renderer feature from the renderer data.")]
        public bool enabled = true;

        [Tooltip("Game view cameras that can run ShadowCast.")]
        public bool enabledForGameView = true;

        [Tooltip("Scene view cameras that can run ShadowCast.")]
        public bool enabledForSceneView = true;

        [Tooltip("Objects on these layers can write to ShadowCast atlases.")]
        public LayerMask casterLayerMask = -1;

        [Tooltip("Scene lights on these layers can be collected from the camera visible light list.")]
        public LayerMask lightLayerMask = -1;

        [Tooltip("Collect camera-visible scene lights at runtime. This is the default path because renderer assets cannot reference scene Light objects.")]
        public bool collectVisibleSceneLights = true;

        [Tooltip("Spot lights rendered into the punctual atlas.")]
        public Light[] spotLights = new Light[HoShadowCastShaderConstants.MaxSpotLights];

        [Tooltip("Point lights rendered into the punctual atlas. Each point light uses six slices.")]
        public Light[] pointLights = new Light[HoShadowCastShaderConstants.MaxPointLights];

        [Tooltip("Extra directional lights rendered into the second-directional atlas.")]
        public Light[] secondDirectionalLights = new Light[HoShadowCastShaderConstants.MaxDirectionalLights];

        [Range(0.0f, 1.0f)]
        public float receiverStrength = 1.0f;

        [Range(0.0f, 1.0f)]
        public float punctualShadowStrength = 0.5f;

        [Range(0.1f, 4.0f)]
        public float punctualShadowFadeSpeed = 1.0f;

        [Range(0.0f, 1.0f)]
        public float secondDirectionalShadowStrength = 0.3f;

        [Min(256)]
        public int atlasSize = 4096;

        [Min(64)]
        public int spotResolution = 512;

        [Min(64)]
        public int pointFaceResolution = 512;

        [Min(256)]
        public int secondDirectionalAtlasSize = 4096;

        [Min(64)]
        public int secondDirectionalCascadeResolution = 1024;

        [Range(1, HoShadowCastShaderConstants.MaxSecondDirectionalCascades)]
        public int secondDirectionalCascadeCount = 4;

        [Min(0.01f)]
        public float secondDirectionalMaxDistance = 80.0f;

        [Min(0.01f)]
        public float secondDirectionalShadowDepth = 80.0f;

        public Vector3 secondDirectionalCascadeSplits = new Vector3(0.08f, 0.22f, 0.5f);

        public bool pcssEnabled = true;

        public HoShadowCastPcssQuality pcssQuality = HoShadowCastPcssQuality.High;

        [Range(0.0f, 4.0f)]
        public float punctualPcssSoftness = 0.6f;

        [Range(0.0f, 4.0f)]
        public float secondDirectionalPcssSoftness = 4.0f;

        [Range(0.25f, 8.0f)]
        public float pcssBlockerSearchRadius = 2.8f;

        [Range(1.0f, 32.0f)]
        public float pcssMaxPenumbraRadius = 7.4f;

        [Range(0.0f, 0.01f)]
        public float pcssDepthBias = 0.0f;

        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPrePasses;

        public RenderPassEvent debugPassEvent = RenderPassEvent.AfterRenderingTransparents;

        public HoShadowCastDebugMode debugMode = HoShadowCastDebugMode.Off;

        public Shader debugShader;

        public bool ShouldRender(CameraType cameraType)
        {
            if (!enabled)
            {
                return false;
            }

            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }

        public void Validate()
        {
            EnsureArraySize(ref spotLights, HoShadowCastShaderConstants.MaxSpotLights);
            EnsureArraySize(ref pointLights, HoShadowCastShaderConstants.MaxPointLights);
            EnsureArraySize(ref secondDirectionalLights, HoShadowCastShaderConstants.MaxDirectionalLights);
            if (lightLayerMask.value == 0)
            {
                lightLayerMask = -1;
            }

            receiverStrength = Mathf.Clamp01(receiverStrength);
            punctualShadowStrength = Mathf.Clamp01(punctualShadowStrength);
            punctualShadowFadeSpeed = Mathf.Clamp(punctualShadowFadeSpeed, 0.1f, 4.0f);
            secondDirectionalShadowStrength = Mathf.Clamp01(secondDirectionalShadowStrength);
            atlasSize = Mathf.Max(256, atlasSize);
            spotResolution = Mathf.Max(64, spotResolution);
            pointFaceResolution = Mathf.Max(64, pointFaceResolution);
            secondDirectionalAtlasSize = Mathf.Max(256, secondDirectionalAtlasSize);
            secondDirectionalCascadeResolution = Mathf.Max(64, secondDirectionalCascadeResolution);
            secondDirectionalCascadeCount = Mathf.Clamp(secondDirectionalCascadeCount, 1, HoShadowCastShaderConstants.MaxSecondDirectionalCascades);
            secondDirectionalMaxDistance = Mathf.Max(0.01f, secondDirectionalMaxDistance);
            secondDirectionalShadowDepth = Mathf.Max(0.01f, secondDirectionalShadowDepth);
            secondDirectionalCascadeSplits = ClampCascadeSplits(secondDirectionalCascadeSplits);
            punctualPcssSoftness = Mathf.Clamp(punctualPcssSoftness, 0.0f, 4.0f);
            secondDirectionalPcssSoftness = Mathf.Clamp(secondDirectionalPcssSoftness, 0.0f, 4.0f);
            pcssBlockerSearchRadius = Mathf.Clamp(pcssBlockerSearchRadius, 0.25f, 8.0f);
            pcssMaxPenumbraRadius = Mathf.Clamp(pcssMaxPenumbraRadius, 1.0f, 32.0f);
            pcssDepthBias = Mathf.Clamp(pcssDepthBias, 0.0f, 0.01f);
        }

        private static void EnsureArraySize(ref Light[] lights, int size)
        {
            if (lights == null)
            {
                lights = new Light[size];
                return;
            }

            if (lights.Length == size)
            {
                return;
            }

            Array.Resize(ref lights, size);
        }

        private static Vector3 ClampCascadeSplits(Vector3 splits)
        {
            float x = Mathf.Clamp(splits.x, 0.001f, 0.997f);
            float y = Mathf.Clamp(splits.y, x + 0.001f, 0.998f);
            float z = Mathf.Clamp(splits.z, y + 0.001f, 0.999f);
            return new Vector3(x, y, z);
        }
    }
}
