using System;
using UnityEngine;

namespace HoUrp.Extensions.ShadowCast
{
    [Serializable]
    public sealed class HoShadowCastRuntimeReport
    {
        public string cameraName = string.Empty;
        public CameraType cameraType;
        public bool rendered;
        public string status = "Not rendered yet.";
        public int visibleLightCount;
        public int requestedPunctualSlices;
        public int requestedSecondDirectionalSlices;
        public int punctualLightCount;
        public int punctualSliceCount;
        public int secondDirectionalLightCount;
        public int secondDirectionalSliceCount;
        public int skippedNotCollectableCount;
        public int skippedMainDirectionalCount;
        public int skippedDuplicateCount;
        public int skippedCapacityCount;
        public int skippedMatrixCount;
        public readonly HoShadowCastRuntimeLight[] punctualLights = new HoShadowCastRuntimeLight[HoShadowCastShaderConstants.MaxLights];
        public readonly HoShadowCastRuntimeLight[] secondDirectionalLights = new HoShadowCastRuntimeLight[HoShadowCastShaderConstants.MaxDirectionalLights];

        public void Reset(Camera camera, int visibleLights, string newStatus)
        {
            cameraName = camera != null ? camera.name : string.Empty;
            cameraType = camera != null ? camera.cameraType : CameraType.Game;
            rendered = false;
            status = newStatus;
            visibleLightCount = visibleLights;
            requestedPunctualSlices = 0;
            requestedSecondDirectionalSlices = 0;
            punctualLightCount = 0;
            punctualSliceCount = 0;
            secondDirectionalLightCount = 0;
            secondDirectionalSliceCount = 0;
            skippedNotCollectableCount = 0;
            skippedMainDirectionalCount = 0;
            skippedDuplicateCount = 0;
            skippedCapacityCount = 0;
            skippedMatrixCount = 0;
            ClearLights(punctualLights);
            ClearLights(secondDirectionalLights);
        }

        public void MarkRendered(string newStatus)
        {
            rendered = true;
            status = newStatus;
        }

        public void MarkNotRendered(string newStatus)
        {
            rendered = false;
            status = newStatus;
        }

        public void CopyFromFrames(HoShadowCastFrameReport punctualFrame, HoShadowCastSecondDirectionalFrameReport secondDirectionalFrame)
        {
            if (punctualFrame != null)
            {
                requestedPunctualSlices = punctualFrame.requestedSlices;
                punctualLightCount = punctualFrame.lightCount;
                punctualSliceCount = punctualFrame.sliceCount;
                skippedNotCollectableCount += punctualFrame.skippedNotCollectableCount;
                skippedMainDirectionalCount += punctualFrame.skippedMainDirectionalCount;
                skippedDuplicateCount += punctualFrame.skippedDuplicateCount;
                skippedCapacityCount += punctualFrame.skippedCapacityCount;
                skippedMatrixCount += punctualFrame.skippedMatrixCount;
                CopyLights(punctualFrame.lights, punctualFrame.lightCount, punctualLights);
            }

            if (secondDirectionalFrame != null)
            {
                requestedSecondDirectionalSlices = secondDirectionalFrame.requestedSlices;
                secondDirectionalLightCount = secondDirectionalFrame.lightCount;
                secondDirectionalSliceCount = secondDirectionalFrame.sliceCount;
                skippedNotCollectableCount += secondDirectionalFrame.skippedNotCollectableCount;
                skippedMainDirectionalCount += secondDirectionalFrame.skippedMainDirectionalCount;
                skippedDuplicateCount += secondDirectionalFrame.skippedDuplicateCount;
                skippedCapacityCount += secondDirectionalFrame.skippedCapacityCount;
                skippedMatrixCount += secondDirectionalFrame.skippedMatrixCount;
                CopyLights(secondDirectionalFrame.lights, secondDirectionalFrame.lightCount, secondDirectionalLights);
            }
        }

        private static void CopyLights(HoShadowCastRuntimeLight[] source, int count, HoShadowCastRuntimeLight[] destination)
        {
            ClearLights(destination);
            int copyCount = Mathf.Min(count, destination.Length);
            for (int i = 0; i < copyCount; i++)
            {
                destination[i] = source[i];
            }
        }

        private static void ClearLights(HoShadowCastRuntimeLight[] lights)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i] = default;
            }
        }
    }

    public sealed class HoShadowCastFrameReport
    {
        public int requestedSlices;
        public int lightCount;
        public int sliceCount;
        public int skippedNotCollectableCount;
        public int skippedMainDirectionalCount;
        public int skippedDuplicateCount;
        public int skippedCapacityCount;
        public int skippedMatrixCount;
        public readonly HoShadowCastRuntimeLight[] lights = new HoShadowCastRuntimeLight[HoShadowCastShaderConstants.MaxLights];

        public void Clear()
        {
            requestedSlices = 0;
            lightCount = 0;
            sliceCount = 0;
            skippedNotCollectableCount = 0;
            skippedMainDirectionalCount = 0;
            skippedDuplicateCount = 0;
            skippedCapacityCount = 0;
            skippedMatrixCount = 0;
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i] = default;
            }
        }

        public void AddAccepted(Light light, LightType lightType, int visibleLightIndex, int firstSlice, int sliceCount, string source)
        {
            if (lightCount >= lights.Length)
            {
                return;
            }

            lights[lightCount++] = HoShadowCastRuntimeLight.Create(light, lightType, visibleLightIndex, firstSlice, sliceCount, source);
            this.sliceCount += sliceCount;
        }
    }

    public sealed class HoShadowCastSecondDirectionalFrameReport
    {
        public int requestedSlices;
        public int lightCount;
        public int sliceCount;
        public int skippedNotCollectableCount;
        public int skippedMainDirectionalCount;
        public int skippedDuplicateCount;
        public int skippedCapacityCount;
        public int skippedMatrixCount;
        public readonly HoShadowCastRuntimeLight[] lights = new HoShadowCastRuntimeLight[HoShadowCastShaderConstants.MaxDirectionalLights];

        public void Clear()
        {
            requestedSlices = 0;
            lightCount = 0;
            sliceCount = 0;
            skippedNotCollectableCount = 0;
            skippedMainDirectionalCount = 0;
            skippedDuplicateCount = 0;
            skippedCapacityCount = 0;
            skippedMatrixCount = 0;
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i] = default;
            }
        }

        public void AddAccepted(Light light, int firstSlice, int sliceCount, string source)
        {
            if (lightCount >= lights.Length)
            {
                return;
            }

            lights[lightCount++] = HoShadowCastRuntimeLight.Create(light, LightType.Directional, -1, firstSlice, sliceCount, source);
            this.sliceCount += sliceCount;
        }
    }

    [Serializable]
    public struct HoShadowCastRuntimeLight
    {
        public Light light;
        public string name;
        public LightType lightType;
        public int visibleLightIndex;
        public int firstSlice;
        public int sliceCount;
        public int layer;
        public string source;

        public static HoShadowCastRuntimeLight Create(Light light, LightType lightType, int visibleLightIndex, int firstSlice, int sliceCount, string source)
        {
            return new HoShadowCastRuntimeLight
            {
                light = light,
                name = light != null ? light.name : "<missing>",
                lightType = lightType,
                visibleLightIndex = visibleLightIndex,
                firstSlice = firstSlice,
                sliceCount = sliceCount,
                layer = light != null ? light.gameObject.layer : -1,
                source = source
            };
        }
    }
}
