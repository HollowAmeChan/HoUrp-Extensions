using System.Collections.Generic;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.ShadowCast
{
    [DisallowMultipleRendererFeature("HoURP ShadowCast")]
    public sealed class HoShadowCastRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private HoShadowCastSettings settings = new HoShadowCastSettings();

        private HoUrpContractRegistry registry;
        private HoShadowCastPass shadowPass;
        private HoShadowCastDebugPass debugPass;
        private Material debugMaterial;
        private HoShadowCastRuntimeReport runtimeReport = new HoShadowCastRuntimeReport();

        public HoShadowCastSettings Settings => settings;
        public HoShadowCastRuntimeReport RuntimeReport => runtimeReport;

        public override void Create()
        {
            if (runtimeReport == null)
            {
                runtimeReport = new HoShadowCastRuntimeReport();
            }

            registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();
            shadowPass = new HoShadowCastPass(registry)
            {
                renderPassEvent = settings != null ? settings.passEvent : RenderPassEvent.BeforeRenderingPrePasses
            };
            debugPass = new HoShadowCastDebugPass
            {
                renderPassEvent = settings != null ? settings.debugPassEvent : RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (shadowPass == null)
            {
                return;
            }

            if (settings == null)
            {
                settings = new HoShadowCastSettings();
            }
            settings.Validate();
            shadowPass.Setup(settings, runtimeReport);
            renderer.EnqueuePass(shadowPass);

            if (settings.debugMode == HoShadowCastDebugMode.Off || debugPass == null)
            {
                return;
            }

            EnsureDebugMaterial();
            if (debugMaterial == null)
            {
                return;
            }

            debugPass.Setup(settings, debugMaterial);
            renderer.EnqueuePass(debugPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(debugMaterial);
            debugMaterial = null;
            shadowPass = null;
            debugPass = null;
            registry = null;
        }

        private void EnsureDebugMaterial()
        {
            if (debugMaterial != null)
            {
                return;
            }

            Shader shader = settings != null && settings.debugShader != null
                ? settings.debugShader
                : Shader.Find(HoUrpShaderPropertyIds.ShadowCastDebugShaderName);

            if (shader != null)
            {
                debugMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        private sealed class HoShadowCastPass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("HoURP ShadowCast");
            private static readonly Vector4[] WorldToShadowRow0 = new Vector4[HoShadowCastShaderConstants.MaxShadowSlices];
            private static readonly Vector4[] WorldToShadowRow1 = new Vector4[HoShadowCastShaderConstants.MaxShadowSlices];
            private static readonly Vector4[] WorldToShadowRow2 = new Vector4[HoShadowCastShaderConstants.MaxShadowSlices];
            private static readonly Vector4[] WorldToShadowRow3 = new Vector4[HoShadowCastShaderConstants.MaxShadowSlices];
            private static readonly Vector4[] LightData0 = new Vector4[HoShadowCastShaderConstants.MaxLights];
            private static readonly Vector4[] LightData1 = new Vector4[HoShadowCastShaderConstants.MaxLights];
            private static readonly Vector4[] LightData2 = new Vector4[HoShadowCastShaderConstants.MaxLights];
            private static readonly Vector4[] LightAttenuation = new Vector4[HoShadowCastShaderConstants.MaxLights];
            private static readonly Vector4[] LightColor = new Vector4[HoShadowCastShaderConstants.MaxLights];
            private static readonly Vector4[] SliceData = new Vector4[HoShadowCastShaderConstants.MaxShadowSlices];
            private static readonly Vector4[] SecondDirectionalWorldToShadowRow0 = new Vector4[HoShadowCastShaderConstants.MaxSecondDirectionalSlices];
            private static readonly Vector4[] SecondDirectionalWorldToShadowRow1 = new Vector4[HoShadowCastShaderConstants.MaxSecondDirectionalSlices];
            private static readonly Vector4[] SecondDirectionalWorldToShadowRow2 = new Vector4[HoShadowCastShaderConstants.MaxSecondDirectionalSlices];
            private static readonly Vector4[] SecondDirectionalWorldToShadowRow3 = new Vector4[HoShadowCastShaderConstants.MaxSecondDirectionalSlices];
            private static readonly Vector4[] SecondDirectionalLightData = new Vector4[HoShadowCastShaderConstants.MaxDirectionalLights];
            private static readonly Vector4[] SecondDirectionalSliceData = new Vector4[HoShadowCastShaderConstants.MaxSecondDirectionalSlices];

            private readonly HoUrpContractRegistry registry;
            private HoShadowCastSettings settings;
            private HoShadowCastRuntimeReport runtimeReport;

            public HoShadowCastPass(HoUrpContractRegistry registry)
            {
                this.registry = registry;
                ConfigureInput(ScriptableRenderPassInput.None);
            }

            public void Setup(HoShadowCastSettings settings, HoShadowCastRuntimeReport runtimeReport)
            {
                this.settings = settings;
                this.runtimeReport = runtimeReport;
                renderPassEvent = settings.passEvent;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();
                int visibleLightCount = lightData.visibleLights.IsCreated ? lightData.visibleLights.Length : 0;

                if (settings == null || !settings.ShouldRender(cameraData.cameraType))
                {
                    runtimeReport?.Reset(cameraData.camera, visibleLightCount, "Skipped by camera type or disabled settings.");
                    RecordResetPass(renderGraph, "HoURP ShadowCast Reset");
                    return;
                }

                HoShadowCastFrame frame = new HoShadowCastFrame();
                HoShadowCastSecondDirectionalFrame secondDirectionalFrame = new HoShadowCastSecondDirectionalFrame();
                runtimeReport?.Reset(cameraData.camera, visibleLightCount, "Collected, no atlases rendered yet.");
                bool hasFrame = BuildFrameData(
                    settings,
                    ref renderingData.cullResults,
                    lightData.visibleLights,
                    lightData.mainLightIndex,
                    cameraData.camera,
                    frame);
                bool hasSecondDirectionalFrame = BuildSecondDirectionalFrameData(
                    settings,
                    lightData.visibleLights,
                    lightData.mainLightIndex,
                    cameraData.camera,
                    secondDirectionalFrame);
                runtimeReport?.CopyFromFrames(frame.report, secondDirectionalFrame.report);

                if (!hasFrame && !hasSecondDirectionalFrame)
                {
                    runtimeReport?.MarkNotRendered("No eligible ShadowCast lights after collection.");
                    RecordResetPass(renderGraph, "HoURP ShadowCast Reset");
                    return;
                }

                if (hasFrame)
                {
                    TextureHandle atlas = HoUrpShadowCastResourceDeclaration.DeclareAtlas(
                        renderGraph,
                        resources,
                        registry,
                        frame.atlasSize,
                        false);
                    RecordFramePass(renderGraph, renderingData, cameraData, atlas, frame);
                }

                if (hasSecondDirectionalFrame)
                {
                    TextureHandle atlas = HoUrpShadowCastResourceDeclaration.DeclareAtlas(
                        renderGraph,
                        resources,
                        registry,
                        secondDirectionalFrame.atlasSize,
                        true);
                    RecordSecondDirectionalPass(renderGraph, renderingData, cameraData, atlas, secondDirectionalFrame);
                }

                RecordPublishPass(renderGraph, frame, hasFrame, secondDirectionalFrame, hasSecondDirectionalFrame);
                runtimeReport?.MarkRendered("ShadowCast globals published.");
            }

            private static void RecordResetPass(UnityRenderGraph renderGraph, string passName)
            {
                using (var builder = renderGraph.AddRasterRenderPass<ResetPassData>(
                    passName,
                    out _,
                    ProfilingSampler))
                {
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (ResetPassData data, RasterGraphContext context) =>
                    {
                        _ = data;
                        ResetGlobals(context.cmd);
                    });
                }
            }

            private static void RecordFramePass(
                UnityRenderGraph renderGraph,
                UniversalRenderingData renderingData,
                UniversalCameraData cameraData,
                TextureHandle atlas,
                HoShadowCastFrame frame)
            {
                using (var builder = renderGraph.AddRasterRenderPass<FramePassData>(
                    "HoURP ShadowCast Punctual Atlas",
                    out FramePassData passData,
                    ProfilingSampler))
                {
                    passData.atlas = atlas;
                    passData.frame = frame;
                    passData.rendererLists = new RendererListHandle[frame.sliceCount];
                    for (int i = 0; i < frame.sliceCount; i++)
                    {
                        DrawingSettings drawingSettings = CreateShadowCasterDrawingSettings(cameraData.camera);
                        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, frame.casterLayerMask);
                        RendererListParams rendererListParams = new RendererListParams(
                            renderingData.cullResults,
                            drawingSettings,
                            filteringSettings);
                        passData.rendererLists[i] = renderGraph.CreateRendererList(rendererListParams);
                        builder.UseRendererList(passData.rendererLists[i]);
                    }

                    builder.SetRenderAttachmentDepth(atlas, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(atlas, HoShadowCastShaderConstants.AtlasTextureId);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (FramePassData data, RasterGraphContext context) =>
                    {
                        RasterCommandBuffer cmd = context.cmd;
                        cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1.0f, 0);
                        ApplyGlobalData(cmd, data.frame);

                        for (int i = 0; i < data.frame.sliceCount; i++)
                        {
                            ShadowSliceInfo slice = data.frame.slices[i];
                            SetShadowCasterGlobals(cmd, data.frame.cameraPosition, slice);
                            RenderShadowSlice(cmd, ref slice.shadowSliceData, data.rendererLists[i], slice.projectionMatrix, slice.viewMatrix);
                        }

                        cmd.SetKeyword(HoShadowCastShaderConstants.CastingPunctualLightShadowKeyword, false);
                        RestoreCameraGlobals(cmd, data.frame);
                        ApplyGlobalData(cmd, data.frame);
                    });
                }
            }

            private static void RecordSecondDirectionalPass(
                UnityRenderGraph renderGraph,
                UniversalRenderingData renderingData,
                UniversalCameraData cameraData,
                TextureHandle atlas,
                HoShadowCastSecondDirectionalFrame frame)
            {
                using (var builder = renderGraph.AddRasterRenderPass<SecondDirectionalPassData>(
                    "HoURP ShadowCast Second Directional Atlas",
                    out SecondDirectionalPassData passData,
                    ProfilingSampler))
                {
                    passData.atlas = atlas;
                    passData.frame = frame;
                    passData.rendererLists = new RendererListHandle[frame.sliceCount];
                    for (int i = 0; i < frame.sliceCount; i++)
                    {
                        DrawingSettings drawingSettings = CreateShadowCasterDrawingSettings(cameraData.camera);
                        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, frame.casterLayerMask);
                        RendererListParams rendererListParams = new RendererListParams(
                            renderingData.cullResults,
                            drawingSettings,
                            filteringSettings);
                        passData.rendererLists[i] = renderGraph.CreateRendererList(rendererListParams);
                        builder.UseRendererList(passData.rendererLists[i]);
                    }

                    builder.SetRenderAttachmentDepth(atlas, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(atlas, HoShadowCastShaderConstants.SecondDirectionalAtlasTextureId);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (SecondDirectionalPassData data, RasterGraphContext context) =>
                    {
                        RasterCommandBuffer cmd = context.cmd;
                        cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1.0f, 0);
                        ApplySecondDirectionalGlobalData(cmd, data.frame);

                        for (int i = 0; i < data.frame.sliceCount; i++)
                        {
                            ShadowSliceInfo slice = data.frame.slices[i];
                            SetShadowCasterGlobals(cmd, data.frame.cameraPosition, slice);
                            RenderShadowSlice(cmd, ref slice.shadowSliceData, data.rendererLists[i], slice.projectionMatrix, slice.viewMatrix);
                        }

                        cmd.SetKeyword(HoShadowCastShaderConstants.CastingPunctualLightShadowKeyword, false);
                        RestoreCameraGlobals(cmd, data.frame.cameraPosition, data.frame.cameraViewMatrix, data.frame.cameraProjectionMatrix);
                        ApplySecondDirectionalGlobalData(cmd, data.frame);
                    });
                }
            }

            private static void RecordPublishPass(
                UnityRenderGraph renderGraph,
                HoShadowCastFrame frame,
                bool hasFrame,
                HoShadowCastSecondDirectionalFrame secondDirectionalFrame,
                bool hasSecondDirectionalFrame)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PublishPassData>(
                    "HoURP ShadowCast Publish Globals",
                    out PublishPassData passData,
                    ProfilingSampler))
                {
                    passData.frame = frame;
                    passData.hasFrame = hasFrame;
                    passData.secondDirectionalFrame = secondDirectionalFrame;
                    passData.hasSecondDirectionalFrame = hasSecondDirectionalFrame;
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (PublishPassData data, RasterGraphContext context) =>
                    {
                        if (data.hasFrame)
                        {
                            ApplyGlobalData(context.cmd, data.frame);
                        }
                        else
                        {
                            SetGlobalEmpty(context.cmd);
                        }

                        if (data.hasSecondDirectionalFrame)
                        {
                            ApplySecondDirectionalGlobalData(context.cmd, data.secondDirectionalFrame);
                        }
                        else
                        {
                            SetSecondDirectionalGlobalEmpty(context.cmd);
                        }

                        context.cmd.SetGlobalFloat(HoShadowCastShaderConstants.ActiveId, data.hasFrame || data.hasSecondDirectionalFrame ? 1.0f : 0.0f);
                    });
                }
            }

            private bool BuildFrameData(
                HoShadowCastSettings settings,
                ref CullingResults cullResults,
                NativeArray<VisibleLight> visibleLights,
                int mainLightIndex,
                Camera camera,
                HoShadowCastFrame target)
            {
                target.Clear();
                target.atlasSize = Mathf.Max(1, settings.atlasSize);
                target.cameraPosition = camera.transform.position;
                target.cameraViewMatrix = camera.worldToCameraMatrix;
                target.cameraProjectionMatrix = camera.projectionMatrix;
                target.pcssParams = CreatePcssParams(settings, settings.punctualPcssSoftness);
                target.pcssParams2 = CreatePcssParams2(settings);
                target.receiverStrength = settings.receiverStrength;
                target.casterLayerMask = settings.casterLayerMask.value;

                int requestedSliceCount = CountRequestedSlices(settings, visibleLights, mainLightIndex);
                target.report.requestedSlices = requestedSliceCount;
                int maxSliceResolution = GetMaxResolutionForSliceCount(target.atlasSize, requestedSliceCount);
                HoShadowCastAtlasPacker packer = new HoShadowCastAtlasPacker(target.atlasSize);
                if (settings.collectVisibleSceneLights)
                {
                    AddVisibleLightArray(visibleLights, settings, ref cullResults, mainLightIndex, maxSliceResolution, ref packer, target);
                }

                AddLightArray(settings.spotLights, LightType.Spot, settings, ref cullResults, visibleLights, mainLightIndex, maxSliceResolution, ref packer, target);
                AddLightArray(settings.pointLights, LightType.Point, settings, ref cullResults, visibleLights, mainLightIndex, maxSliceResolution, ref packer, target);

                target.FillUnused();
                return target.lightCount > 0 && target.sliceCount > 0;
            }

            private static bool BuildSecondDirectionalFrameData(
                HoShadowCastSettings settings,
                NativeArray<VisibleLight> visibleLights,
                int mainLightIndex,
                Camera camera,
                HoShadowCastSecondDirectionalFrame target)
            {
                target.Clear();
                target.cameraPosition = camera.transform.position;
                target.cameraViewMatrix = camera.worldToCameraMatrix;
                target.cameraProjectionMatrix = camera.projectionMatrix;
                target.pcssParams = CreatePcssParams(settings, settings.secondDirectionalPcssSoftness);
                target.pcssParams2 = CreatePcssParams2(settings);
                target.receiverStrength = settings.receiverStrength;
                target.casterLayerMask = settings.casterLayerMask.value;

                int cascadeCount = Mathf.Clamp(settings.secondDirectionalCascadeCount, 1, HoShadowCastShaderConstants.MaxSecondDirectionalCascades);
                int requestedSliceCount = CountRequestedSecondDirectionalSlices(settings, visibleLights, mainLightIndex, cascadeCount);
                target.report.requestedSlices = requestedSliceCount;
                if (requestedSliceCount <= 0)
                {
                    return false;
                }

                int atlasSize = Mathf.Max(1, settings.secondDirectionalAtlasSize);
                int resolution = Mathf.Clamp(settings.secondDirectionalCascadeResolution, 64, atlasSize);
                HoShadowCastAtlasPacker packer = new HoShadowCastAtlasPacker(atlasSize);
                float nearDistance = Mathf.Max(0.001f, camera.nearClipPlane);
                float farDistance = Mathf.Min(Mathf.Max(nearDistance + 0.01f, settings.secondDirectionalMaxDistance), Mathf.Max(nearDistance + 0.01f, camera.farClipPlane));

                target.atlasSize = atlasSize;
                target.cascadeCountPerLight = cascadeCount;

                if (settings.collectVisibleSceneLights)
                {
                    AddVisibleSecondDirectionalLights(settings, visibleLights, mainLightIndex, camera, atlasSize, resolution, nearDistance, farDistance, ref packer, target);
                }

                Light[] lights = settings.secondDirectionalLights;
                for (int lightSlot = 0; lights != null && lightSlot < lights.Length; lightSlot++)
                {
                    Light light = lights[lightSlot];
                    if (light == null)
                    {
                        continue;
                    }

                    if (!IsLightCollectable(light, settings, LightType.Directional))
                    {
                        target.report.skippedNotCollectableCount++;
                        continue;
                    }

                    if (target.Contains(light))
                    {
                        target.report.skippedDuplicateCount++;
                        continue;
                    }

                    int visibleLightIndex = FindVisibleLightIndex(visibleLights, light, LightType.Directional);
                    if (visibleLightIndex >= 0 && visibleLightIndex == mainLightIndex)
                    {
                        target.report.skippedMainDirectionalCount++;
                        continue;
                    }

                    AddSecondDirectionalLight(light, settings, camera, atlasSize, resolution, nearDistance, farDistance, ref packer, target, "Explicit");
                }

                target.FillUnused();
                return target.lightCount > 0 && target.sliceCount > 0;
            }

            private static void AddVisibleLightArray(
                NativeArray<VisibleLight> visibleLights,
                HoShadowCastSettings settings,
                ref CullingResults cullResults,
                int mainLightIndex,
                int maxSliceResolution,
                ref HoShadowCastAtlasPacker packer,
                HoShadowCastFrame target)
            {
                if (!visibleLights.IsCreated)
                {
                    return;
                }

                for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; visibleLightIndex++)
                {
                    if (visibleLightIndex == mainLightIndex)
                    {
                        continue;
                    }

                    VisibleLight visibleLight = visibleLights[visibleLightIndex];
                    LightType lightType = visibleLight.lightType;
                    if (lightType != LightType.Spot && lightType != LightType.Point)
                    {
                        continue;
                    }

                    Light light = visibleLight.light;
                    if (!IsLightCollectable(light, settings, lightType))
                    {
                        target.report.skippedNotCollectableCount++;
                        continue;
                    }

                    if (target.Contains(light))
                    {
                        target.report.skippedDuplicateCount++;
                        continue;
                    }

                    AddLightByVisibleIndex(light, lightType, visibleLightIndex, settings, ref cullResults, maxSliceResolution, ref packer, target, "Visible");
                }
            }

            private static void AddVisibleSecondDirectionalLights(
                HoShadowCastSettings settings,
                NativeArray<VisibleLight> visibleLights,
                int mainLightIndex,
                Camera camera,
                int atlasSize,
                int resolution,
                float nearDistance,
                float farDistance,
                ref HoShadowCastAtlasPacker packer,
                HoShadowCastSecondDirectionalFrame target)
            {
                if (!visibleLights.IsCreated)
                {
                    return;
                }

                for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; visibleLightIndex++)
                {
                    if (visibleLightIndex == mainLightIndex)
                    {
                        continue;
                    }

                    VisibleLight visibleLight = visibleLights[visibleLightIndex];
                    if (visibleLight.lightType != LightType.Directional)
                    {
                        continue;
                    }

                    if (!IsLightCollectable(visibleLight.light, settings, LightType.Directional))
                    {
                        target.report.skippedNotCollectableCount++;
                        continue;
                    }

                    if (target.Contains(visibleLight.light))
                    {
                        target.report.skippedDuplicateCount++;
                        continue;
                    }

                    AddSecondDirectionalLight(visibleLight.light, settings, camera, atlasSize, resolution, nearDistance, farDistance, ref packer, target, "Visible");
                }
            }

            private static void AddSecondDirectionalLight(
                Light light,
                HoShadowCastSettings settings,
                Camera camera,
                int atlasSize,
                int resolution,
                float nearDistance,
                float farDistance,
                ref HoShadowCastAtlasPacker packer,
                HoShadowCastSecondDirectionalFrame target,
                string source)
            {
                int cascadeCount = target.cascadeCountPerLight;
                if (target.lightCount >= HoShadowCastShaderConstants.MaxDirectionalLights
                    || target.sliceCount + cascadeCount > HoShadowCastShaderConstants.MaxSecondDirectionalSlices
                    || target.Contains(light))
                {
                    if (target.Contains(light))
                    {
                        target.report.skippedDuplicateCount++;
                    }
                    else
                    {
                        target.report.skippedCapacityCount++;
                    }

                    return;
                }

                int firstSlice = target.sliceCount;
                HoShadowCastAtlasPacker packerBeforeLight = packer;
                GetSecondDirectionalCascadeBlock(cascadeCount, out int cascadeColumns, out int cascadeRows);
                bool allocatedBlock = packer.TryAllocate(
                    resolution * cascadeColumns,
                    resolution * cascadeRows,
                    out int blockOffsetX,
                    out int blockOffsetY);
                if (!allocatedBlock)
                {
                    target.report.skippedCapacityCount++;
                    return;
                }

                float lightShadowStrength = light.shadows == LightShadows.None ? 1.0f : light.shadowStrength;
                float shadowStrength = Mathf.Clamp01(settings.secondDirectionalShadowStrength * lightShadowStrength);
                float previousDistance = nearDistance;
                bool completed = true;

                for (int cascadeIndex = 0; cascadeIndex < cascadeCount; cascadeIndex++)
                {
                    float splitRatio = GetSecondDirectionalCascadeSplit(settings.secondDirectionalCascadeSplits, cascadeCount, cascadeIndex);
                    float cascadeFarDistance = cascadeIndex == cascadeCount - 1
                        ? farDistance
                        : Mathf.Lerp(nearDistance, farDistance, splitRatio);
                    cascadeFarDistance = Mathf.Max(previousDistance + 0.01f, cascadeFarDistance);

                    int offsetX = blockOffsetX + (cascadeIndex % cascadeColumns) * resolution;
                    int offsetY = blockOffsetY + (cascadeIndex / cascadeColumns) * resolution;
                    if (!TryBuildSecondDirectionalCascadeSlice(
                            light,
                            camera,
                            previousDistance,
                            cascadeFarDistance,
                            settings,
                            atlasSize,
                            resolution,
                            offsetX,
                            offsetY,
                            out ShadowSliceInfo slice))
                    {
                        completed = false;
                        target.report.skippedMatrixCount++;
                        break;
                    }

                    target.slices[target.sliceCount] = slice;
                    target.worldToShadow[target.sliceCount] = slice.worldToShadow;
                    target.sliceData[target.sliceCount] = slice.sliceData;
                    target.sliceCount++;
                    previousDistance = cascadeFarDistance;
                }

                if (!completed)
                {
                    target.sliceCount = firstSlice;
                    packer = packerBeforeLight;
                    return;
                }

                int lightIndex = target.lightCount++;
                target.sourceLights[lightIndex] = light;
                target.lightData[lightIndex] = new Vector4(firstSlice, cascadeCount, shadowStrength, 0.0f);
                target.report.AddAccepted(light, firstSlice, cascadeCount, source);
            }

            private static void AddLightArray(
                Light[] lights,
                LightType requiredType,
                HoShadowCastSettings settings,
                ref CullingResults cullResults,
                NativeArray<VisibleLight> visibleLights,
                int mainLightIndex,
                int maxSliceResolution,
                ref HoShadowCastAtlasPacker packer,
                HoShadowCastFrame target)
            {
                if (lights == null)
                {
                    return;
                }

                for (int i = 0; i < lights.Length; i++)
                {
                    Light light = lights[i];
                    if (light == null)
                    {
                        continue;
                    }

                    AddLight(light, requiredType, settings, ref cullResults, visibleLights, mainLightIndex, maxSliceResolution, ref packer, target, "Explicit");
                }
            }

            private static void AddLight(
                Light light,
                LightType requiredType,
                HoShadowCastSettings settings,
                ref CullingResults cullResults,
                NativeArray<VisibleLight> visibleLights,
                int mainLightIndex,
                int maxSliceResolution,
                ref HoShadowCastAtlasPacker packer,
                HoShadowCastFrame target,
                string source)
            {
                if (!IsLightCollectable(light, settings, requiredType))
                {
                    target.report.skippedNotCollectableCount++;
                    return;
                }

                if (target.Contains(light))
                {
                    target.report.skippedDuplicateCount++;
                    return;
                }

                if (target.lightCount >= HoShadowCastShaderConstants.MaxLights)
                {
                    target.report.skippedCapacityCount++;
                    return;
                }

                int visibleLightIndex = FindVisibleLightIndex(visibleLights, light, requiredType);
                if (visibleLightIndex >= 0 && visibleLightIndex == mainLightIndex)
                {
                    target.report.skippedMainDirectionalCount++;
                    return;
                }

                AddLightByVisibleIndex(light, requiredType, visibleLightIndex, settings, ref cullResults, maxSliceResolution, ref packer, target, source);
            }

            private static void AddLightByVisibleIndex(
                Light light,
                LightType requiredType,
                int visibleLightIndex,
                HoShadowCastSettings settings,
                ref CullingResults cullResults,
                int maxSliceResolution,
                ref HoShadowCastAtlasPacker packer,
                HoShadowCastFrame target,
                string source)
            {
                if (target.lightCount >= HoShadowCastShaderConstants.MaxLights || target.Contains(light))
                {
                    if (target.Contains(light))
                    {
                        target.report.skippedDuplicateCount++;
                    }
                    else
                    {
                        target.report.skippedCapacityCount++;
                    }

                    return;
                }

                int firstSlice = target.sliceCount;
                int requestedSlices = requiredType == LightType.Point ? 6 : 1;
                if (firstSlice + requestedSlices > HoShadowCastShaderConstants.MaxShadowSlices)
                {
                    target.report.skippedCapacityCount++;
                    return;
                }

                int resolution = GetResolution(settings, requiredType, maxSliceResolution);
                int writtenSlices = 0;
                bool completed = true;

                for (int face = 0; face < requestedSlices; face++)
                {
                    if (!packer.TryAllocate(resolution, out int offsetX, out int offsetY)
                        || !TryBuildSlice(
                            light,
                            ref cullResults,
                            visibleLightIndex,
                            requiredType,
                            face,
                            settings,
                            target.atlasSize,
                            resolution,
                            offsetX,
                            offsetY,
                            out ShadowSliceInfo slice))
                    {
                        completed = false;
                        target.report.skippedMatrixCount++;
                        break;
                    }

                    target.slices[target.sliceCount++] = slice;
                    writtenSlices++;
                }

                if (!completed || writtenSlices != requestedSlices)
                {
                    target.sliceCount = firstSlice;
                    return;
                }

                int lightIndex = target.lightCount++;
                target.sourceLights[lightIndex] = light;
                Vector3 position = light.transform.position;
                Vector3 direction = light.transform.forward;
                Color finalColor = light.color * light.intensity;
                float lightShadowStrength = light.shadows == LightShadows.None ? 1.0f : light.shadowStrength;
                target.lightData0[lightIndex] = new Vector4(GetLightTypeId(requiredType), firstSlice, writtenSlices, Mathf.Clamp01(settings.punctualShadowStrength * lightShadowStrength));
                target.lightData1[lightIndex] = new Vector4(position.x, position.y, position.z, light.range);
                target.lightData2[lightIndex] = new Vector4(direction.x, direction.y, direction.z, Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad));
                target.lightAttenuation[lightIndex] = ComputeLightAttenuation(light, requiredType, settings.punctualShadowFadeSpeed);
                target.lightColor[lightIndex] = new Vector4(finalColor.r, finalColor.g, finalColor.b, 1.0f);
                target.report.AddAccepted(light, requiredType, visibleLightIndex, firstSlice, writtenSlices, source);
            }

            private static bool IsLightCollectable(Light light, HoShadowCastSettings settings, LightType requiredType)
            {
                if (light == null || light.type != requiredType || !light.isActiveAndEnabled)
                {
                    return false;
                }

                return (settings.lightLayerMask.value & (1 << light.gameObject.layer)) != 0;
            }

            private static bool TryBuildSlice(
                Light light,
                ref CullingResults cullResults,
                int visibleLightIndex,
                LightType lightType,
                int face,
                HoShadowCastSettings settings,
                int atlasSize,
                int resolution,
                int offsetX,
                int offsetY,
                out ShadowSliceInfo slice)
            {
                slice = new ShadowSliceInfo
                {
                    visibleLightIndex = visibleLightIndex,
                    lightType = lightType,
                    faceIndex = face
                };

                if (!TryBuildLightMatrices(light, ref cullResults, visibleLightIndex, lightType, face, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData))
                {
                    return false;
                }

                Matrix4x4 shadowMatrix = GetShadowTransform(projectionMatrix, viewMatrix);
                ShadowSliceData shadowSliceData = new ShadowSliceData
                {
                    viewMatrix = viewMatrix,
                    projectionMatrix = projectionMatrix,
                    shadowTransform = shadowMatrix,
                    splitData = splitData,
                    offsetX = offsetX,
                    offsetY = offsetY,
                    resolution = resolution
                };
                ShadowUtils.ApplySliceTransform(ref shadowSliceData, atlasSize, atlasSize);

                slice.shadowSliceData = shadowSliceData;
                slice.viewMatrix = viewMatrix;
                slice.projectionMatrix = projectionMatrix;
                slice.splitData = splitData;
                slice.shadowBias = ComputeShadowBias(light, lightType, projectionMatrix, resolution);
                slice.lightDirection = -light.transform.forward;
                slice.lightPosition = light.transform.position;
                slice.worldToShadow = shadowSliceData.shadowTransform;
                slice.sliceData = new Vector4((float)offsetX / atlasSize, (float)offsetY / atlasSize, (float)resolution / atlasSize, face);
                return true;
            }

            private static bool TryBuildLightMatrices(
                Light light,
                ref CullingResults cullResults,
                int visibleLightIndex,
                LightType lightType,
                int face,
                out Matrix4x4 viewMatrix,
                out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData)
            {
                viewMatrix = Matrix4x4.identity;
                projectionMatrix = Matrix4x4.identity;
                splitData = default;
                if (light == null)
                {
                    return false;
                }

                Transform lightTransform = light.transform;
                float nearPlane = Mathf.Max(0.001f, light.shadowNearPlane);
                if (lightType == LightType.Spot)
                {
                    if (visibleLightIndex >= 0 && cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(visibleLightIndex, out viewMatrix, out projectionMatrix, out splitData))
                    {
                        return true;
                    }

                    BuildManualSpotMatrix(lightTransform, light, nearPlane, out viewMatrix, out projectionMatrix);
                    return true;
                }

                if (lightType == LightType.Point)
                {
                    if (visibleLightIndex >= 0 && cullResults.ComputePointShadowMatricesAndCullingPrimitives(visibleLightIndex, (CubemapFace)face, 4.0f, out viewMatrix, out projectionMatrix, out splitData))
                    {
                        viewMatrix.m10 = -viewMatrix.m10;
                        viewMatrix.m11 = -viewMatrix.m11;
                        viewMatrix.m12 = -viewMatrix.m12;
                        viewMatrix.m13 = -viewMatrix.m13;
                        return true;
                    }

                    BuildManualPointMatrix(lightTransform, light, face, nearPlane, out viewMatrix, out projectionMatrix);
                    return true;
                }

                return false;
            }

            private static bool TryBuildSecondDirectionalCascadeSlice(
                Light light,
                Camera camera,
                float cascadeNearDistance,
                float cascadeFarDistance,
                HoShadowCastSettings settings,
                int atlasSize,
                int resolution,
                int offsetX,
                int offsetY,
                out ShadowSliceInfo slice)
            {
                slice = new ShadowSliceInfo
                {
                    visibleLightIndex = -1,
                    lightType = LightType.Directional
                };

                if (light == null || camera == null)
                {
                    return false;
                }

                Vector3[] corners = new Vector3[8];
                FillCameraFrustumCorners(camera, cascadeNearDistance, cascadeFarDistance, corners);

                Vector3 center = Vector3.zero;
                for (int i = 0; i < corners.Length; i++)
                {
                    center += corners[i];
                }

                center /= corners.Length;
                Vector3 lightForward = light.transform.forward;
                float minLightDistance = float.PositiveInfinity;
                float maxLightDistance = float.NegativeInfinity;
                for (int i = 0; i < corners.Length; i++)
                {
                    float lightDistance = Vector3.Dot(corners[i] - center, lightForward);
                    minLightDistance = Mathf.Min(minLightDistance, lightDistance);
                    maxLightDistance = Mathf.Max(maxLightDistance, lightDistance);
                }

                float cascadeDepth = maxLightDistance - minLightDistance;
                float depth = Mathf.Max(Mathf.Max(0.01f, settings.secondDirectionalShadowDepth), cascadeDepth + 1.0f);
                Matrix4x4 fitViewMatrix = CreateViewMatrix(center - lightForward * (depth * 0.5f), lightForward, light.transform.up);
                float size = 0.01f;
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 cornerView = fitViewMatrix.MultiplyPoint(corners[i]);
                    size = Mathf.Max(size, Mathf.Max(Mathf.Abs(cornerView.x), Mathf.Abs(cornerView.y)));
                }

                size *= 1.05f;
                center = SnapDirectionalCascadeCenter(center, lightForward, light.transform.up, size, resolution);
                float nearPlane = Mathf.Max(0.001f, light.shadowNearPlane);
                Matrix4x4 viewMatrix = CreateViewMatrix(center - lightForward * (depth * 0.5f), lightForward, light.transform.up);
                Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-size, size, -size, size, nearPlane, depth);
                Matrix4x4 shadowMatrix = GetShadowTransform(projectionMatrix, viewMatrix);

                ShadowSliceData shadowSliceData = new ShadowSliceData
                {
                    viewMatrix = viewMatrix,
                    projectionMatrix = projectionMatrix,
                    shadowTransform = shadowMatrix,
                    splitData = default,
                    offsetX = offsetX,
                    offsetY = offsetY,
                    resolution = resolution
                };
                ShadowUtils.ApplySliceTransform(ref shadowSliceData, atlasSize, atlasSize);

                slice.shadowSliceData = shadowSliceData;
                slice.viewMatrix = viewMatrix;
                slice.projectionMatrix = projectionMatrix;
                slice.splitData = default;
                slice.shadowBias = ComputeShadowBias(light, LightType.Directional, projectionMatrix, resolution);
                slice.lightDirection = -light.transform.forward;
                slice.lightPosition = light.transform.position;
                slice.worldToShadow = shadowSliceData.shadowTransform;
                slice.sliceData = new Vector4((float)offsetX / atlasSize, (float)offsetY / atlasSize, (float)resolution / atlasSize, cascadeFarDistance * cascadeFarDistance);
                return true;
            }

            private static void BuildManualSpotMatrix(Transform lightTransform, Light light, float nearPlane, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix)
            {
                float range = Mathf.Max(nearPlane + 0.01f, light.range);
                float fov = Mathf.Clamp(light.spotAngle, 0.1f, 179.0f);
                viewMatrix = CreateViewMatrix(lightTransform.position, lightTransform.forward, lightTransform.up);
                projectionMatrix = Matrix4x4.Perspective(fov, 1.0f, nearPlane, range);
            }

            private static void BuildManualPointMatrix(Transform lightTransform, Light light, int face, float nearPlane, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix)
            {
                float range = Mathf.Max(nearPlane + 0.01f, light.range);
                GetPointLightFaceVectors(face, out Vector3 direction, out Vector3 up);
                viewMatrix = CreateViewMatrix(lightTransform.position, direction, up);
                viewMatrix.m10 = -viewMatrix.m10;
                viewMatrix.m11 = -viewMatrix.m11;
                viewMatrix.m12 = -viewMatrix.m12;
                viewMatrix.m13 = -viewMatrix.m13;
                projectionMatrix = Matrix4x4.Perspective(94.0f, 1.0f, nearPlane, range);
            }

            private static Matrix4x4 CreateViewMatrix(Vector3 position, Vector3 forward, Vector3 up)
            {
                Quaternion rotation = Quaternion.LookRotation(-forward, up);
                return Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
            }

            private static void GetPointLightFaceVectors(int face, out Vector3 direction, out Vector3 up)
            {
                switch ((CubemapFace)face)
                {
                    case CubemapFace.PositiveX:
                        direction = Vector3.right;
                        up = Vector3.down;
                        break;
                    case CubemapFace.NegativeX:
                        direction = Vector3.left;
                        up = Vector3.down;
                        break;
                    case CubemapFace.PositiveY:
                        direction = Vector3.up;
                        up = Vector3.forward;
                        break;
                    case CubemapFace.NegativeY:
                        direction = Vector3.down;
                        up = Vector3.back;
                        break;
                    case CubemapFace.PositiveZ:
                        direction = Vector3.forward;
                        up = Vector3.down;
                        break;
                    case CubemapFace.NegativeZ:
                        direction = Vector3.back;
                        up = Vector3.down;
                        break;
                    default:
                        direction = Vector3.forward;
                        up = Vector3.down;
                        break;
                }
            }

            private static void FillCameraFrustumCorners(Camera camera, float nearDistance, float farDistance, Vector3[] corners)
            {
                Vector3[] tempCorners = new Vector3[4];
                camera.CalculateFrustumCorners(new Rect(0.0f, 0.0f, 1.0f, 1.0f), nearDistance, Camera.MonoOrStereoscopicEye.Mono, tempCorners);
                for (int i = 0; i < 4; i++)
                {
                    corners[i] = camera.transform.position + camera.transform.TransformVector(tempCorners[i]);
                }

                camera.CalculateFrustumCorners(new Rect(0.0f, 0.0f, 1.0f, 1.0f), farDistance, Camera.MonoOrStereoscopicEye.Mono, tempCorners);
                for (int i = 0; i < 4; i++)
                {
                    corners[i + 4] = camera.transform.position + camera.transform.TransformVector(tempCorners[i]);
                }
            }

            private static Vector3 SnapDirectionalCascadeCenter(Vector3 center, Vector3 lightForward, Vector3 lightUp, float size, int resolution)
            {
                if (resolution <= 0 || size <= 0.0f)
                {
                    return center;
                }

                Matrix4x4 lightViewAtOrigin = CreateViewMatrix(Vector3.zero, lightForward, lightUp);
                Vector3 centerLightSpace = lightViewAtOrigin.MultiplyPoint(center);
                float texelSize = (size * 2.0f) / resolution;
                centerLightSpace.x = Mathf.Round(centerLightSpace.x / texelSize) * texelSize;
                centerLightSpace.y = Mathf.Round(centerLightSpace.y / texelSize) * texelSize;
                return lightViewAtOrigin.inverse.MultiplyPoint(centerLightSpace);
            }

            private static Matrix4x4 GetShadowTransform(Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
            {
                if (SystemInfo.usesReversedZBuffer)
                {
                    projectionMatrix.m20 = -projectionMatrix.m20;
                    projectionMatrix.m21 = -projectionMatrix.m21;
                    projectionMatrix.m22 = -projectionMatrix.m22;
                    projectionMatrix.m23 = -projectionMatrix.m23;
                }

                Matrix4x4 textureScaleAndBias = Matrix4x4.identity;
                textureScaleAndBias.m00 = 0.5f;
                textureScaleAndBias.m11 = 0.5f;
                textureScaleAndBias.m22 = 0.5f;
                textureScaleAndBias.m03 = 0.5f;
                textureScaleAndBias.m13 = 0.5f;
                textureScaleAndBias.m23 = 0.5f;
                return textureScaleAndBias * projectionMatrix * viewMatrix;
            }

            private static Vector4 ComputeShadowBias(Light light, LightType lightType, Matrix4x4 lightProjectionMatrix, int resolution)
            {
                if (light == null)
                {
                    return Vector4.zero;
                }

                float frustumSize;
                if (lightType == LightType.Directional)
                {
                    frustumSize = Mathf.Abs(2.0f / lightProjectionMatrix.m00);
                }
                else if (lightType == LightType.Spot)
                {
                    frustumSize = Mathf.Tan(light.spotAngle * 0.5f * Mathf.Deg2Rad) * light.range;
                }
                else if (lightType == LightType.Point)
                {
                    frustumSize = Mathf.Tan(94.0f * 0.5f * Mathf.Deg2Rad) * light.range;
                }
                else
                {
                    frustumSize = 0.0f;
                }

                float texelSize = resolution > 0 ? frustumSize / resolution : 0.0f;
                float depthBias = -light.shadowBias * texelSize;
                float normalBias = lightType == LightType.Point ? 0.0f : -light.shadowNormalBias * texelSize;
                return new Vector4(depthBias, normalBias, (float)lightType, 0.0f);
            }

            private static int FindVisibleLightIndex(NativeArray<VisibleLight> visibleLights, Light light, LightType requiredType)
            {
                if (!visibleLights.IsCreated)
                {
                    return -1;
                }

                for (int i = 0; i < visibleLights.Length; i++)
                {
                    VisibleLight visibleLight = visibleLights[i];
                    if (visibleLight.light == light && visibleLight.lightType == requiredType)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private static int CountRequestedSlices(HoShadowCastSettings settings, NativeArray<VisibleLight> visibleLights, int mainLightIndex)
            {
                int count = 0;
                if (settings.collectVisibleSceneLights && visibleLights.IsCreated)
                {
                    for (int i = 0; i < visibleLights.Length; i++)
                    {
                        if (i == mainLightIndex)
                        {
                            continue;
                        }

                        VisibleLight visibleLight = visibleLights[i];
                        if (visibleLight.lightType == LightType.Spot && IsLightCollectable(visibleLight.light, settings, LightType.Spot))
                        {
                            count++;
                        }
                        else if (visibleLight.lightType == LightType.Point && IsLightCollectable(visibleLight.light, settings, LightType.Point))
                        {
                            count += 6;
                        }
                    }
                }

                count += CountRequestedSlices(settings.spotLights, LightType.Spot, visibleLights, mainLightIndex, settings);
                count += CountRequestedSlices(settings.pointLights, LightType.Point, visibleLights, mainLightIndex, settings);
                return Mathf.Min(count, HoShadowCastShaderConstants.MaxShadowSlices);
            }

            private static int CountRequestedSlices(Light[] lights, LightType requiredType, NativeArray<VisibleLight> visibleLights, int mainLightIndex, HoShadowCastSettings settings)
            {
                if (lights == null)
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < lights.Length; i++)
                {
                    Light light = lights[i];
                    if (!IsLightCollectable(light, settings, requiredType))
                    {
                        continue;
                    }

                    int visibleLightIndex = FindVisibleLightIndex(visibleLights, light, requiredType);
                    if (visibleLightIndex >= 0 && visibleLightIndex == mainLightIndex)
                    {
                        continue;
                    }

                    count += requiredType == LightType.Point ? 6 : 1;
                }

                return count;
            }

            private static int CountRequestedSecondDirectionalSlices(HoShadowCastSettings settings, NativeArray<VisibleLight> visibleLights, int mainLightIndex, int cascadeCount)
            {
                int count = 0;
                if (settings.collectVisibleSceneLights && visibleLights.IsCreated)
                {
                    for (int i = 0; i < visibleLights.Length; i++)
                    {
                        if (i == mainLightIndex)
                        {
                            continue;
                        }

                        VisibleLight visibleLight = visibleLights[i];
                        if (visibleLight.lightType == LightType.Directional && IsLightCollectable(visibleLight.light, settings, LightType.Directional))
                        {
                            count += cascadeCount;
                        }
                    }
                }

                Light[] lights = settings.secondDirectionalLights;
                for (int i = 0; i < lights.Length; i++)
                {
                    Light light = lights[i];
                    if (light == null)
                    {
                        continue;
                    }

                    if (!IsLightCollectable(light, settings, LightType.Directional))
                    {
                        continue;
                    }

                    int visibleLightIndex = FindVisibleLightIndex(visibleLights, light, LightType.Directional);
                    if (visibleLightIndex >= 0 && visibleLightIndex == mainLightIndex)
                    {
                        continue;
                    }

                    count += cascadeCount;
                }

                return Mathf.Min(count, HoShadowCastShaderConstants.MaxSecondDirectionalSlices);
            }

            private static int GetMaxResolutionForSliceCount(int atlasSize, int requestedSliceCount)
            {
                atlasSize = Mathf.Max(1, atlasSize);
                if (requestedSliceCount <= 1)
                {
                    return atlasSize;
                }

                int gridSize = Mathf.CeilToInt(Mathf.Sqrt(requestedSliceCount));
                return Mathf.Max(64, atlasSize / Mathf.Max(1, gridSize));
            }

            private static int GetResolution(HoShadowCastSettings settings, LightType type, int maxSliceResolution)
            {
                int atlasSize = Mathf.Max(1, settings.atlasSize);
                int resolution = type == LightType.Point ? settings.pointFaceResolution : settings.spotResolution;
                return Mathf.Clamp(resolution, 64, Mathf.Min(atlasSize, maxSliceResolution));
            }

            private static void GetSecondDirectionalCascadeBlock(int cascadeCount, out int columns, out int rows)
            {
                if (cascadeCount <= 1)
                {
                    columns = 1;
                    rows = 1;
                    return;
                }

                if (cascadeCount == 2)
                {
                    columns = 2;
                    rows = 1;
                    return;
                }

                columns = 2;
                rows = 2;
            }

            private static float GetLightTypeId(LightType type)
            {
                if (type == LightType.Spot)
                {
                    return 1.0f;
                }

                return type == LightType.Point ? 2.0f : 0.0f;
            }

            private static float GetSecondDirectionalCascadeSplit(Vector3 splits, int cascadeCount, int cascadeIndex)
            {
                float splitX = Mathf.Clamp(splits.x, 0.001f, 0.997f);
                float splitY = Mathf.Clamp(splits.y, splitX + 0.001f, 0.998f);
                float splitZ = Mathf.Clamp(splits.z, splitY + 0.001f, 0.999f);
                if (cascadeCount <= 1)
                {
                    return 1.0f;
                }

                if (cascadeCount == 2)
                {
                    return cascadeIndex == 0 ? splitX : 1.0f;
                }

                if (cascadeCount == 3)
                {
                    return cascadeIndex == 0 ? splitX : cascadeIndex == 1 ? splitY : 1.0f;
                }

                return cascadeIndex == 0 ? splitX : cascadeIndex == 1 ? splitY : cascadeIndex == 2 ? splitZ : 1.0f;
            }

            private static Vector4 ComputeLightAttenuation(Light light, LightType lightType, float fadeSpeed)
            {
                if (light == null || lightType == LightType.Directional)
                {
                    return Vector4.zero;
                }

                float range = Mathf.Max(0.0001f, light.range);
                float oneOverRangeSqr = 1.0f / (range * range);
                fadeSpeed = fadeSpeed <= 0.0f ? 1.0f : Mathf.Clamp(fadeSpeed, 0.1f, 4.0f);
                float spotScale = 0.0f;
                float spotOffset = 0.0f;

                if (lightType == LightType.Spot)
                {
                    float spotAngle = Mathf.Max(2.6f, light.spotAngle);
                    float innerSpotAngle = Mathf.Clamp(light.innerSpotAngle, 0.0f, spotAngle);
                    float cosOuterAngle = Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad);
                    float cosInnerAngle = Mathf.Cos(innerSpotAngle * 0.5f * Mathf.Deg2Rad);
                    float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                    spotScale = 1.0f / smoothAngleRange;
                    spotOffset = -cosOuterAngle * spotScale;
                }

                return new Vector4(oneOverRangeSqr, fadeSpeed, spotScale, spotOffset);
            }

            private static Vector4 CreatePcssParams(HoShadowCastSettings settings, float softness)
            {
                if (settings == null || !settings.pcssEnabled || softness <= 0.0f)
                {
                    return Vector4.zero;
                }

                return new Vector4(
                    1.0f,
                    Mathf.Clamp(softness, 0.0f, 4.0f),
                    Mathf.Clamp(settings.pcssBlockerSearchRadius, 0.25f, 8.0f),
                    Mathf.Clamp(settings.pcssMaxPenumbraRadius, 1.0f, 32.0f));
            }

            private static Vector4 CreatePcssParams2(HoShadowCastSettings settings)
            {
                if (settings == null || !settings.pcssEnabled)
                {
                    return Vector4.zero;
                }

                GetPcssSampleCounts(settings.pcssQuality, out int blockerSamples, out int filterSamples);
                return new Vector4(
                    Mathf.Clamp(settings.pcssDepthBias, 0.0f, 0.01f),
                    blockerSamples,
                    filterSamples,
                    0.0f);
            }

            private static void GetPcssSampleCounts(HoShadowCastPcssQuality quality, out int blockerSamples, out int filterSamples)
            {
                switch (quality)
                {
                    case HoShadowCastPcssQuality.Low:
                        blockerSamples = 8;
                        filterSamples = 16;
                        break;
                    case HoShadowCastPcssQuality.High:
                        blockerSamples = 24;
                        filterSamples = 48;
                        break;
                    case HoShadowCastPcssQuality.Ultra:
                        blockerSamples = 32;
                        filterSamples = 64;
                        break;
                    default:
                        blockerSamples = 16;
                        filterSamples = 32;
                        break;
                }
            }

            private static DrawingSettings CreateShadowCasterDrawingSettings(Camera camera)
            {
                SortingSettings sortingSettings = new SortingSettings(camera)
                {
                    criteria = SortingCriteria.None
                };

                return new DrawingSettings(HoShadowCastShaderConstants.ShadowCasterShaderTagId, sortingSettings)
                {
                    perObjectData = PerObjectData.None,
                    enableDynamicBatching = false,
                    enableInstancing = true
                };
            }

            private static void SetShadowCasterGlobals(RasterCommandBuffer cmd, Vector3 cameraPosition, ShadowSliceInfo slice)
            {
                cmd.SetGlobalVector(HoShadowCastShaderConstants.WorldSpaceCameraPosId, cameraPosition);
                SetWorldToCameraMatrices(cmd, slice.viewMatrix);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.ShadowBiasId, slice.shadowBias);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.LightDirectionId, new Vector4(slice.lightDirection.x, slice.lightDirection.y, slice.lightDirection.z, 0.0f));
                cmd.SetGlobalVector(HoShadowCastShaderConstants.LightPositionId, new Vector4(slice.lightPosition.x, slice.lightPosition.y, slice.lightPosition.z, 1.0f));
                cmd.SetKeyword(HoShadowCastShaderConstants.CastingPunctualLightShadowKeyword, slice.lightType != LightType.Directional);
            }

            private static void SetWorldToCameraMatrices(RasterCommandBuffer cmd, Matrix4x4 viewMatrix)
            {
                Matrix4x4 worldToCameraMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)) * viewMatrix;
                cmd.SetGlobalMatrix(HoShadowCastShaderConstants.WorldToCameraMatrixId, worldToCameraMatrix);
                cmd.SetGlobalMatrix(HoShadowCastShaderConstants.CameraToWorldMatrixId, worldToCameraMatrix.inverse);
            }

            private static void RestoreCameraGlobals(RasterCommandBuffer cmd, HoShadowCastFrame frame)
            {
                RestoreCameraGlobals(cmd, frame.cameraPosition, frame.cameraViewMatrix, frame.cameraProjectionMatrix);
            }

            private static void RestoreCameraGlobals(RasterCommandBuffer cmd, Vector3 cameraPosition, Matrix4x4 cameraViewMatrix, Matrix4x4 cameraProjectionMatrix)
            {
                cmd.SetGlobalVector(HoShadowCastShaderConstants.WorldSpaceCameraPosId, cameraPosition);
                SetWorldToCameraMatrices(cmd, cameraViewMatrix);
                cmd.SetViewProjectionMatrices(cameraViewMatrix, cameraProjectionMatrix);
            }

            private static void RenderShadowSlice(
                RasterCommandBuffer cmd,
                ref ShadowSliceData shadowSliceData,
                RendererListHandle rendererList,
                Matrix4x4 projectionMatrix,
                Matrix4x4 viewMatrix)
            {
                cmd.SetGlobalDepthBias(1.0f, 2.5f);
                cmd.SetViewport(new Rect(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));
                cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                cmd.DrawRendererList(rendererList);
                cmd.DisableScissorRect();
                cmd.SetGlobalDepthBias(0.0f, 0.0f);
            }

            private static void ApplyGlobalData(RasterCommandBuffer cmd, HoShadowCastFrame frame)
            {
                CopyFrameArrays(frame);
                cmd.SetGlobalFloat(HoShadowCastShaderConstants.ActiveId, frame.lightCount > 0 ? 1.0f : 0.0f);
                cmd.SetGlobalInt(HoShadowCastShaderConstants.LightCountId, frame.lightCount);
                cmd.SetGlobalInt(HoShadowCastShaderConstants.SliceCountId, frame.sliceCount);
                cmd.SetGlobalFloat(HoShadowCastShaderConstants.ReceiverStrengthId, frame.receiverStrength);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.AtlasSizeId, new Vector4(frame.atlasSize, frame.atlasSize, 1.0f / frame.atlasSize, 1.0f / frame.atlasSize));
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.WorldToShadowRow0Id, WorldToShadowRow0);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.WorldToShadowRow1Id, WorldToShadowRow1);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.WorldToShadowRow2Id, WorldToShadowRow2);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.WorldToShadowRow3Id, WorldToShadowRow3);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.LightData0Id, LightData0);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.LightData1Id, LightData1);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.LightData2Id, LightData2);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.LightAttenuationId, LightAttenuation);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.LightColorId, LightColor);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.SliceDataId, SliceData);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.PcssParamsId, frame.pcssParams);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.PcssParams2Id, frame.pcssParams2);
            }

            private static void ApplySecondDirectionalGlobalData(RasterCommandBuffer cmd, HoShadowCastSecondDirectionalFrame frame)
            {
                CopySecondDirectionalFrameArrays(frame);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.SecondDirectionalParamsId, new Vector4(frame.lightCount > 0 ? 1.0f : 0.0f, frame.lightCount, frame.cascadeCountPerLight, frame.sliceCount));
                cmd.SetGlobalVector(HoShadowCastShaderConstants.SecondDirectionalCameraPositionId, new Vector4(frame.cameraPosition.x, frame.cameraPosition.y, frame.cameraPosition.z, 1.0f));
                cmd.SetGlobalVector(HoShadowCastShaderConstants.SecondDirectionalAtlasSizeId, new Vector4(frame.atlasSize, frame.atlasSize, 1.0f / Mathf.Max(1, frame.atlasSize), 1.0f / Mathf.Max(1, frame.atlasSize)));
                cmd.SetGlobalFloat(HoShadowCastShaderConstants.ReceiverStrengthId, frame.receiverStrength);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.SecondDirectionalPcssParamsId, new Vector4(frame.pcssParams.x, frame.pcssParams.y, frame.pcssParams.z, frame.pcssParams2.x));
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.SecondDirectionalWorldToShadowRow0Id, SecondDirectionalWorldToShadowRow0);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.SecondDirectionalWorldToShadowRow1Id, SecondDirectionalWorldToShadowRow1);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.SecondDirectionalWorldToShadowRow2Id, SecondDirectionalWorldToShadowRow2);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.SecondDirectionalWorldToShadowRow3Id, SecondDirectionalWorldToShadowRow3);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.SecondDirectionalLightDataId, SecondDirectionalLightData);
                cmd.SetGlobalVectorArray(HoShadowCastShaderConstants.SecondDirectionalSliceDataId, SecondDirectionalSliceData);
            }

            private static void SetGlobalEmpty(RasterCommandBuffer cmd)
            {
                cmd.SetGlobalFloat(HoShadowCastShaderConstants.ActiveId, 0.0f);
                cmd.SetGlobalInt(HoShadowCastShaderConstants.LightCountId, 0);
                cmd.SetGlobalInt(HoShadowCastShaderConstants.SliceCountId, 0);
                cmd.SetGlobalFloat(HoShadowCastShaderConstants.ReceiverStrengthId, 0.0f);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.AtlasSizeId, Vector4.zero);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.PcssParamsId, Vector4.zero);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.PcssParams2Id, Vector4.zero);
            }

            private static void SetSecondDirectionalGlobalEmpty(RasterCommandBuffer cmd)
            {
                cmd.SetGlobalVector(HoShadowCastShaderConstants.SecondDirectionalParamsId, Vector4.zero);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.SecondDirectionalCameraPositionId, Vector4.zero);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.SecondDirectionalAtlasSizeId, Vector4.zero);
                cmd.SetGlobalVector(HoShadowCastShaderConstants.SecondDirectionalPcssParamsId, Vector4.zero);
            }

            private static void ResetGlobals(RasterCommandBuffer cmd)
            {
                SetGlobalEmpty(cmd);
                SetSecondDirectionalGlobalEmpty(cmd);
            }

            private static void CopyFrameArrays(HoShadowCastFrame frame)
            {
                for (int i = 0; i < HoShadowCastShaderConstants.MaxShadowSlices; i++)
                {
                    WorldToShadowRow0[i] = frame.worldToShadow[i].GetRow(0);
                    WorldToShadowRow1[i] = frame.worldToShadow[i].GetRow(1);
                    WorldToShadowRow2[i] = frame.worldToShadow[i].GetRow(2);
                    WorldToShadowRow3[i] = frame.worldToShadow[i].GetRow(3);
                    SliceData[i] = frame.sliceData[i];
                }

                for (int i = 0; i < HoShadowCastShaderConstants.MaxLights; i++)
                {
                    LightData0[i] = frame.lightData0[i];
                    LightData1[i] = frame.lightData1[i];
                    LightData2[i] = frame.lightData2[i];
                    LightAttenuation[i] = frame.lightAttenuation[i];
                    LightColor[i] = frame.lightColor[i];
                }
            }

            private static void CopySecondDirectionalFrameArrays(HoShadowCastSecondDirectionalFrame frame)
            {
                for (int i = 0; i < HoShadowCastShaderConstants.MaxSecondDirectionalSlices; i++)
                {
                    SecondDirectionalWorldToShadowRow0[i] = frame.worldToShadow[i].GetRow(0);
                    SecondDirectionalWorldToShadowRow1[i] = frame.worldToShadow[i].GetRow(1);
                    SecondDirectionalWorldToShadowRow2[i] = frame.worldToShadow[i].GetRow(2);
                    SecondDirectionalWorldToShadowRow3[i] = frame.worldToShadow[i].GetRow(3);
                    SecondDirectionalSliceData[i] = frame.sliceData[i];
                }

                for (int i = 0; i < HoShadowCastShaderConstants.MaxDirectionalLights; i++)
                {
                    SecondDirectionalLightData[i] = frame.lightData[i];
                }
            }

            private sealed class ResetPassData
            {
            }

            private sealed class FramePassData
            {
                public TextureHandle atlas;
                public HoShadowCastFrame frame;
                public RendererListHandle[] rendererLists;
            }

            private sealed class SecondDirectionalPassData
            {
                public TextureHandle atlas;
                public HoShadowCastSecondDirectionalFrame frame;
                public RendererListHandle[] rendererLists;
            }

            private sealed class PublishPassData
            {
                public HoShadowCastFrame frame;
                public bool hasFrame;
                public HoShadowCastSecondDirectionalFrame secondDirectionalFrame;
                public bool hasSecondDirectionalFrame;
            }
        }

        private sealed class HoShadowCastDebugPass : ScriptableRenderPass
        {
            private const int DebugPassIndex = 0;
            private HoShadowCastSettings settings;
            private Material debugMaterial;

            public void Setup(HoShadowCastSettings settings, Material debugMaterial)
            {
                this.settings = settings;
                this.debugMaterial = debugMaterial;
                renderPassEvent = settings.debugPassEvent;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                if (settings == null || debugMaterial == null || settings.debugMode == HoShadowCastDebugMode.Off)
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();
                HoUrpIdentifier sourceId = settings.debugMode == HoShadowCastDebugMode.SecondDirectionalAtlas
                    ? HoUrpBuiltInNames.Resources.ShadowCastSecondDirectionalAtlas
                    : HoUrpBuiltInNames.Resources.ShadowCastAtlas;

                if (resourceData.isActiveTargetBackBuffer
                    || !resources.TryGetTexture(sourceId, out TextureHandle source)
                    || !resourceData.activeColorTexture.IsValid())
                {
                    return;
                }

                debugMaterial.SetFloat(HoShadowCastShaderConstants.DebugModeId, (float)settings.debugMode);
                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(
                        source,
                        resourceData.activeColorTexture,
                        debugMaterial,
                        DebugPassIndex);

                renderGraph.AddBlitPass(blitParameters, passName: "HoURP ShadowCast Debug");
            }
        }

        private sealed class HoShadowCastFrame
        {
            public int atlasSize;
            public int lightCount;
            public int sliceCount;
            public float receiverStrength;
            public int casterLayerMask;
            public Vector3 cameraPosition;
            public Matrix4x4 cameraViewMatrix;
            public Matrix4x4 cameraProjectionMatrix;
            public Vector4 pcssParams;
            public Vector4 pcssParams2;
            public readonly Light[] sourceLights = new Light[HoShadowCastShaderConstants.MaxLights];
            public readonly ShadowSliceInfo[] slices = new ShadowSliceInfo[HoShadowCastShaderConstants.MaxShadowSlices];
            public readonly Matrix4x4[] worldToShadow = new Matrix4x4[HoShadowCastShaderConstants.MaxShadowSlices];
            public readonly Vector4[] lightData0 = new Vector4[HoShadowCastShaderConstants.MaxLights];
            public readonly Vector4[] lightData1 = new Vector4[HoShadowCastShaderConstants.MaxLights];
            public readonly Vector4[] lightData2 = new Vector4[HoShadowCastShaderConstants.MaxLights];
            public readonly Vector4[] lightAttenuation = new Vector4[HoShadowCastShaderConstants.MaxLights];
            public readonly Vector4[] lightColor = new Vector4[HoShadowCastShaderConstants.MaxLights];
            public readonly Vector4[] sliceData = new Vector4[HoShadowCastShaderConstants.MaxShadowSlices];
            public readonly HoShadowCastFrameReport report = new HoShadowCastFrameReport();

            public void Clear()
            {
                atlasSize = 1;
                lightCount = 0;
                sliceCount = 0;
                receiverStrength = 0.0f;
                casterLayerMask = -1;
                cameraPosition = Vector3.zero;
                cameraViewMatrix = Matrix4x4.identity;
                cameraProjectionMatrix = Matrix4x4.identity;
                pcssParams = Vector4.zero;
                pcssParams2 = Vector4.zero;
                report.Clear();

                for (int i = 0; i < sourceLights.Length; i++)
                {
                    sourceLights[i] = null;
                    lightData0[i] = Vector4.zero;
                    lightData1[i] = Vector4.zero;
                    lightData2[i] = Vector4.zero;
                    lightAttenuation[i] = Vector4.zero;
                    lightColor[i] = Vector4.zero;
                }

                for (int i = 0; i < slices.Length; i++)
                {
                    slices[i] = default;
                    worldToShadow[i] = Matrix4x4.identity;
                    sliceData[i] = Vector4.zero;
                }
            }

            public bool Contains(Light light)
            {
                for (int i = 0; i < lightCount; i++)
                {
                    if (sourceLights[i] == light)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void FillUnused()
            {
                for (int i = 0; i < sliceCount; i++)
                {
                    worldToShadow[i] = slices[i].worldToShadow;
                    sliceData[i] = slices[i].sliceData;
                }

                for (int i = sliceCount; i < worldToShadow.Length; i++)
                {
                    worldToShadow[i] = Matrix4x4.identity;
                    sliceData[i] = Vector4.zero;
                }
            }
        }

        private sealed class HoShadowCastSecondDirectionalFrame
        {
            public int atlasSize;
            public int lightCount;
            public int cascadeCountPerLight;
            public int sliceCount;
            public float receiverStrength;
            public int casterLayerMask;
            public Vector3 cameraPosition;
            public Matrix4x4 cameraViewMatrix;
            public Matrix4x4 cameraProjectionMatrix;
            public Vector4 pcssParams;
            public Vector4 pcssParams2;
            public readonly Light[] sourceLights = new Light[HoShadowCastShaderConstants.MaxDirectionalLights];
            public readonly Vector4[] lightData = new Vector4[HoShadowCastShaderConstants.MaxDirectionalLights];
            public readonly ShadowSliceInfo[] slices = new ShadowSliceInfo[HoShadowCastShaderConstants.MaxSecondDirectionalSlices];
            public readonly Matrix4x4[] worldToShadow = new Matrix4x4[HoShadowCastShaderConstants.MaxSecondDirectionalSlices];
            public readonly Vector4[] sliceData = new Vector4[HoShadowCastShaderConstants.MaxSecondDirectionalSlices];
            public readonly HoShadowCastSecondDirectionalFrameReport report = new HoShadowCastSecondDirectionalFrameReport();

            public void Clear()
            {
                atlasSize = 1;
                lightCount = 0;
                cascadeCountPerLight = 0;
                sliceCount = 0;
                receiverStrength = 0.0f;
                casterLayerMask = -1;
                cameraPosition = Vector3.zero;
                cameraViewMatrix = Matrix4x4.identity;
                cameraProjectionMatrix = Matrix4x4.identity;
                pcssParams = Vector4.zero;
                pcssParams2 = Vector4.zero;
                report.Clear();

                for (int i = 0; i < sourceLights.Length; i++)
                {
                    sourceLights[i] = null;
                    lightData[i] = Vector4.zero;
                }

                for (int i = 0; i < slices.Length; i++)
                {
                    slices[i] = default;
                    worldToShadow[i] = Matrix4x4.identity;
                    sliceData[i] = Vector4.zero;
                }
            }

            public bool Contains(Light light)
            {
                for (int i = 0; i < lightCount; i++)
                {
                    if (sourceLights[i] == light)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void FillUnused()
            {
                for (int i = sliceCount; i < worldToShadow.Length; i++)
                {
                    worldToShadow[i] = Matrix4x4.identity;
                    sliceData[i] = Vector4.zero;
                }
            }
        }

        private struct ShadowSliceInfo
        {
            public int visibleLightIndex;
            public LightType lightType;
            public int faceIndex;
            public ShadowSliceData shadowSliceData;
            public Matrix4x4 viewMatrix;
            public Matrix4x4 projectionMatrix;
            public ShadowSplitData splitData;
            public Vector4 shadowBias;
            public Vector3 lightDirection;
            public Vector3 lightPosition;
            public Matrix4x4 worldToShadow;
            public Vector4 sliceData;
        }
    }
}
