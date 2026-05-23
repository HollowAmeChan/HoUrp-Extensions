using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoUrp.Extensions.OIT
{
    [Serializable]
    public sealed class WeightedOitSettings
    {
        [Tooltip("Skip the OIT passes without removing the renderer feature from the renderer data.")]
        public bool enabled = true;

        [Tooltip("Cameras that can run the OIT pass chain.")]
        public bool enabledForGameView = true;

        [Tooltip("Scene view cameras that can run the OIT pass chain.")]
        public bool enabledForSceneView = true;

        [Tooltip("Layers that can write to the OIT accumulation buffers.")]
        public LayerMask layerMask = -1;

        [Tooltip("Lowest render queue included in the OIT accumulation pass.")]
        public int minRenderQueue = (int)RenderQueue.AlphaTest + 1;

        [Tooltip("Highest render queue included in the OIT accumulation pass.")]
        public int maxRenderQueue = (int)RenderQueue.Overlay - 1;

        [Tooltip("When transparent OIT objects are drawn into accumulation buffers.")]
        public RenderPassEvent accumulationPassEvent = RenderPassEvent.BeforeRenderingTransparents;

        [Tooltip("When OIT is composited back to camera color.")]
        public RenderPassEvent compositePassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Tooltip("Composite shader. If empty, the feature uses Hidden/HoURP/OIT/WeightedComposite.")]
        public Shader compositeShader;

        [Tooltip("Global strength multiplier for weighted transparency accumulation.")]
        [Min(0.0f)]
        public float weight = 1.0f;

        [Tooltip("Reject very low alpha fragments before they enter the OIT buffers.")]
        [Range(0.0f, 1.0f)]
        public float alphaClipThreshold = 0.003921569f;

        public RenderQueueRange RenderQueueRange
        {
            get
            {
                int lower = Mathf.Min(minRenderQueue, maxRenderQueue);
                int upper = Mathf.Max(minRenderQueue, maxRenderQueue);
                return new RenderQueueRange
                {
                    lowerBound = lower,
                    upperBound = upper
                };
            }
        }

        public bool ShouldRender(CameraType cameraType)
        {
            if (!enabled)
            {
                return false;
            }

            return (enabledForGameView && cameraType == CameraType.Game)
                || (enabledForSceneView && cameraType == CameraType.SceneView);
        }
    }
}
