using System.Collections.Generic;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.Debugging
{
    [DisallowMultipleRendererFeature("HoURP Render Cache Debug")]
    [MovedFrom(true, "HoUrp.Extensions.Debugging", null, "AovDebugRendererFeature")]
    public sealed class RenderCacheDebugRendererFeature : ScriptableRendererFeature
    {
        public const string NoneViewId = "";
        public const string AllRegisteredViewId = "__AllRegistered";

        private enum RenderCacheDebugView
        {
            None = 0,
            AllRegistered = 21,
            Mask = 1,
            ObjectId = 2,
            LinearDepth = 3,
            WorldNormal = 4,
            Subject = 5,
            Face = 6,
            Hair = 7,
            Eye = 8,
            Accessory = 9,
            Cloth = 10,
            Prop = 11,
            Reserved = 12,
            MaterialClass = 13,
            SssProfile = 14,
            Thickness = 15,
            Curvature = 16,
            MaterialCustom0 = 17,
            MaterialCustom1 = 18,
            MaterialCustom2 = 19,
            MaterialCustom3 = 20,
            Diffuse = 22,
            SssMask = 24,
            SssPreparedSource = 25,
            SssDiffusion = 26,
            SssCompositeWeight = 27,
            PostReceiver = 29,
            Flag0Reserved = 30,
            Flag2 = 31,
            Flag3 = 32,
            Flag4 = 33,
            Flag5 = 34,
            Flag6 = 35,
            Flag7 = 36,
            Flag8 = 37,
            Flag9 = 38,
            Flag10 = 39,
            Flag11 = 40,
            Flag12 = 41
        }

        [SerializeField]
        private bool enabledForGameView = true;

        [SerializeField]
        private bool enabledForSceneView = true;

        [SerializeField]
        [HideInInspector]
        private RenderCacheDebugView selectedView = RenderCacheDebugView.None;

        [SerializeField]
        private string selectedDebugViewId = NoneViewId;

        private HoUrpContractRegistry registry;
        private RenderCacheDebugPass debugPass;
        private Material debugMaterial;

        public override void Create()
        {
            registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Shader debugShader = Shader.Find(HoUrpShaderPropertyIds.RenderCacheDebugShaderName);
            debugMaterial = debugShader != null
                ? CoreUtils.CreateEngineMaterial(debugShader)
                : null;

            debugPass = new RenderCacheDebugPass(registry)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!ShouldRender(in renderingData)
                || debugPass == null
                || debugMaterial == null)
            {
                return;
            }

            string currentDebugViewId = ResolveCurrentDebugViewId();
            if (string.IsNullOrEmpty(currentDebugViewId))
            {
                return;
            }

            if (currentDebugViewId == AllRegisteredViewId)
            {
                debugPass.SetupAll(debugMaterial);
            }
            else
            {
                HoUrpIdentifier debugViewId = HoUrpIdentifier.From(currentDebugViewId);
                if (!registry.DebugViews.TryGet(debugViewId, out DebugViewDefinition debugView))
                {
                    return;
                }

                debugPass.Setup(debugMaterial, debugViewId, RenderCacheDebugPass.ResolveShaderMode(debugView));
            }

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

        private string ResolveCurrentDebugViewId()
        {
            if (!string.IsNullOrEmpty(selectedDebugViewId))
            {
                return selectedDebugViewId;
            }

            if (selectedView == RenderCacheDebugView.None)
            {
                return NoneViewId;
            }

            if (selectedView == RenderCacheDebugView.AllRegistered)
            {
                return AllRegisteredViewId;
            }

            return ResolveDebugViewId(selectedView).ToString();
        }

        private static HoUrpIdentifier ResolveDebugViewId(RenderCacheDebugView view)
        {
            switch (view)
            {
                case RenderCacheDebugView.Mask:
                    return HoUrpBuiltInNames.DebugViews.AovMask;
                case RenderCacheDebugView.ObjectId:
                    return HoUrpBuiltInNames.DebugViews.AovObjectId;
                case RenderCacheDebugView.Flag0Reserved:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag0;
                case RenderCacheDebugView.PostReceiver:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag1;
                case RenderCacheDebugView.Flag2:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag2;
                case RenderCacheDebugView.Flag3:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag3;
                case RenderCacheDebugView.Flag4:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag4;
                case RenderCacheDebugView.Flag5:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag5;
                case RenderCacheDebugView.Flag6:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag6;
                case RenderCacheDebugView.Flag7:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag7;
                case RenderCacheDebugView.Flag8:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag8;
                case RenderCacheDebugView.Flag9:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag9;
                case RenderCacheDebugView.Flag10:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag10;
                case RenderCacheDebugView.Flag11:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag11;
                case RenderCacheDebugView.Flag12:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag12;
                case RenderCacheDebugView.LinearDepth:
                    return HoUrpBuiltInNames.DebugViews.AovLinearDepth;
                case RenderCacheDebugView.WorldNormal:
                    return HoUrpBuiltInNames.DebugViews.AovWorldNormal;
                case RenderCacheDebugView.Subject:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom0;
                case RenderCacheDebugView.Face:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom1;
                case RenderCacheDebugView.Hair:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom2;
                case RenderCacheDebugView.Eye:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom3;
                case RenderCacheDebugView.Accessory:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom4;
                case RenderCacheDebugView.Cloth:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom5;
                case RenderCacheDebugView.Prop:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom6;
                case RenderCacheDebugView.Reserved:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom7;
                case RenderCacheDebugView.MaterialClass:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialClass;
                case RenderCacheDebugView.SssProfile:
                    return HoUrpBuiltInNames.DebugViews.AovSssProfile;
                case RenderCacheDebugView.Thickness:
                    return HoUrpBuiltInNames.DebugViews.AovThickness;
                case RenderCacheDebugView.Curvature:
                    return HoUrpBuiltInNames.DebugViews.AovCurvature;
                case RenderCacheDebugView.MaterialCustom0:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom0;
                case RenderCacheDebugView.MaterialCustom1:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom1;
                case RenderCacheDebugView.MaterialCustom2:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom2;
                case RenderCacheDebugView.MaterialCustom3:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom3;
                case RenderCacheDebugView.Diffuse:
                    return HoUrpBuiltInNames.DebugViews.AovDiffuse;
                case RenderCacheDebugView.SssMask:
                    return HoUrpBuiltInNames.DebugViews.SssMask;
                case RenderCacheDebugView.SssPreparedSource:
                    return HoUrpBuiltInNames.DebugViews.SssSource;
                case RenderCacheDebugView.SssDiffusion:
                    return HoUrpBuiltInNames.DebugViews.SssDiffusion;
                case RenderCacheDebugView.SssCompositeWeight:
                    return HoUrpBuiltInNames.DebugViews.SssCompositeWeight;
                default:
                    return HoUrpBuiltInNames.DebugViews.AovMask;
            }
        }

        private sealed class RenderCacheDebugPass : ScriptableRenderPass
        {
            private static readonly ProfilingSampler ProfilingSampler = new ProfilingSampler("HoURP Render Cache Debug");
            private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();
            private readonly HoUrpContractRegistry registry;
            private Material material;
            private HoUrpIdentifier debugViewId;
            private int shaderMode;
            private bool showAllRegistered;

            public RenderCacheDebugPass(HoUrpContractRegistry registry)
            {
                this.registry = registry;
                ConfigureInput(ScriptableRenderPassInput.None);
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material, HoUrpIdentifier debugViewId, int shaderMode)
            {
                this.material = material;
                this.debugViewId = debugViewId;
                this.shaderMode = shaderMode;
                showAllRegistered = false;
                requiresIntermediateTexture = true;
            }

            public void SetupAll(Material material)
            {
                this.material = material;
                debugViewId = HoUrpBuiltInNames.DebugViews.AovMask;
                shaderMode = 0;
                showAllRegistered = true;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                {
                    return;
                }

                if (showAllRegistered)
                {
                    RecordAllDebugViews(renderGraph, frameData);
                    return;
                }

                if (!registry.DebugViews.TryGet(debugViewId, out DebugViewDefinition debugView))
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
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetInt(HoUrpShaderPropertyIds.RenderCacheDebugMode, shaderMode);
                propertyBlock.SetInt(HoUrpShaderPropertyIds.RenderCacheDebugTileMode, 0);

                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(sourceTexture, destination, material, 0)
                    {
                        propertyBlock = propertyBlock,
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.RenderCacheDebugSourceTexture
                    };
                renderGraph.AddBlitPass(blitParameters, passName: "HoURP Render Cache Debug");
            }

            private void RecordAllDebugViews(UnityRenderGraph renderGraph, ContextContainer frameData)
            {
                HoUrpRenderGraphResources resources = frameData.GetOrCreate<HoUrpRenderGraphResources>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle destination = resourceData.activeColorTexture;

                List<DebugTile> tiles = new List<DebugTile>(registry.DebugViews.Count);
                HashSet<string> uniqueViews = new HashSet<string>();
                foreach (DebugViewDefinition debugView in registry.DebugViews.Definitions)
                {
                    if (!resources.TryGetTexture(debugView.SourceResource, out TextureHandle sourceTexture))
                    {
                        continue;
                    }

                    string viewKey = debugView.SourceSemantic == HoUrpBuiltInNames.Semantics.ObjectFlags
                        ? debugView.Id.ToString()
                        : debugView.SourceResource.ToString() + "|" + debugView.SourceSemantic.ToString();
                    if (!uniqueViews.Add(viewKey))
                    {
                        continue;
                    }

                    tiles.Add(new DebugTile(
                        sourceTexture,
                        ResolveShaderMode(debugView),
                        EncodeLabel(ResolvePreviewLabel(debugView))));
                }

                if (tiles.Count == 0)
                {
                    return;
                }

                int targetWidth = Mathf.Max(1, cameraData.cameraTargetDescriptor.width);
                int targetHeight = Mathf.Max(1, cameraData.cameraTargetDescriptor.height);
                float targetAspect = targetWidth / (float)targetHeight;
                float sourceAspect = targetAspect;
                CalculateTileGrid(tiles.Count, sourceAspect, targetAspect, out int columns, out int rows);

                for (int i = 0; i < tiles.Count; i++)
                {
                    DebugTile tile = tiles[i];
                    int column = i % columns;
                    int row = i / columns;

                    tiles[i] = new DebugTile(
                        tile.sourceTexture,
                        tile.shaderMode,
                        new LabelData(tile.label0, tile.label1, tile.label2, tile.label3),
                        CalculateTileRect(
                            column,
                            row,
                            columns,
                            rows,
                            sourceAspect,
                            targetAspect));
                }

                using (var builder = renderGraph.AddRasterRenderPass<AllPassData>(
                    "HoURP Render Cache Debug All",
                    out AllPassData passData,
                    ProfilingSampler))
                {
                    passData.material = material;
                    passData.tiles = tiles;
                    passData.tileGrid = new Vector4(columns, rows, tiles.Count, 0.0f);

                    for (int i = 0; i < tiles.Count; i++)
                    {
                        builder.UseTexture(tiles[i].sourceTexture, AccessFlags.Read);
                    }

                    builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (AllPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 1.0f, 0);

                        for (int i = 0; i < data.tiles.Count; i++)
                        {
                            DebugTile tile = data.tiles[i];
                            PropertyBlock.Clear();
                            PropertyBlock.SetInt(HoUrpShaderPropertyIds.RenderCacheDebugTileMode, 1);
                            PropertyBlock.SetInt(HoUrpShaderPropertyIds.RenderCacheDebugMode, tile.shaderMode);
                            PropertyBlock.SetVector(HoUrpShaderPropertyIds.RenderCacheDebugTileRect, tile.tileRect);
                            PropertyBlock.SetVector(HoUrpShaderPropertyIds.RenderCacheDebugTileGrid, data.tileGrid);
                            PropertyBlock.SetVector(HoUrpShaderPropertyIds.RenderCacheDebugTileLabel0, tile.label0);
                            PropertyBlock.SetVector(HoUrpShaderPropertyIds.RenderCacheDebugTileLabel1, tile.label1);
                            PropertyBlock.SetVector(HoUrpShaderPropertyIds.RenderCacheDebugTileLabel2, tile.label2);
                            PropertyBlock.SetVector(HoUrpShaderPropertyIds.RenderCacheDebugTileLabel3, tile.label3);
                            context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.RenderCacheDebugSourceTexture, tile.sourceTexture);
                            context.cmd.DrawProcedural(
                                Matrix4x4.identity,
                                data.material,
                                1,
                                MeshTopology.Triangles,
                                6,
                                1,
                                PropertyBlock);
                        }
                    });
                }
            }

            private static void CalculateTileGrid(
                int tileCount,
                float sourceAspect,
                float targetAspect,
                out int columns,
                out int rows)
            {
                int preferredColumns = targetAspect >= 1.45f ? 5 : 4;
                columns = Mathf.Clamp(preferredColumns, 1, Mathf.Max(1, tileCount));
                rows = Mathf.CeilToInt(tileCount / (float)columns);
            }

            private static Vector4 CalculateTileRect(
                int column,
                int row,
                int columns,
                int rows,
                float sourceAspect,
                float targetAspect)
            {
                float cellWidth = 1.0f / columns;
                float cellHeight = 1.0f / rows;
                float cellX = column * cellWidth;
                float cellY = row * cellHeight;
                float gutter = Mathf.Min(cellWidth, cellHeight) * 0.035f;

                float innerX = cellX + gutter;
                float innerY = cellY + gutter;
                float innerWidth = Mathf.Max(0.0001f, cellWidth - gutter * 2.0f);
                float innerHeight = Mathf.Max(0.0001f, cellHeight - gutter * 2.0f);

                float desiredNormalizedAspect = Mathf.Max(0.0001f, sourceAspect / Mathf.Max(0.0001f, targetAspect));
                float innerAspect = innerWidth / innerHeight;
                float fittedWidth = innerWidth;
                float fittedHeight = innerHeight;

                if (innerAspect > desiredNormalizedAspect)
                {
                    fittedWidth = innerHeight * desiredNormalizedAspect;
                }
                else
                {
                    fittedHeight = innerWidth / desiredNormalizedAspect;
                }

                float fittedX = innerX + (innerWidth - fittedWidth) * 0.5f;
                float fittedY = innerY + (innerHeight - fittedHeight) * 0.5f;
                return new Vector4(fittedX, fittedY, fittedWidth, fittedHeight);
            }

            internal static int ResolveShaderMode(DebugViewDefinition debugView)
            {
                HoUrpIdentifier id = debugView.Id;
                if (id == HoUrpBuiltInNames.DebugViews.AovObjectId)
                {
                    return 1;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag0)
                {
                    return 27;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag1)
                {
                    return 28;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag2)
                {
                    return 29;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag3)
                {
                    return 30;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag4)
                {
                    return 31;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag5)
                {
                    return 32;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag6)
                {
                    return 33;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag7)
                {
                    return 34;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag8)
                {
                    return 35;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag9)
                {
                    return 36;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag10)
                {
                    return 37;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag11)
                {
                    return 38;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag12)
                {
                    return 39;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovLinearDepth)
                {
                    return 2;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovWorldNormal)
                {
                    return 3;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom0)
                {
                    return 4;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom1)
                {
                    return 5;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom2)
                {
                    return 6;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom3)
                {
                    return 7;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom4)
                {
                    return 8;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom5)
                {
                    return 9;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom6)
                {
                    return 10;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom7)
                {
                    return 11;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialClass)
                {
                    return 12;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovSssProfile)
                {
                    return 13;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovThickness)
                {
                    return 14;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovCurvature)
                {
                    return 15;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialCustom0)
                {
                    return 16;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialCustom1)
                {
                    return 17;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialCustom2)
                {
                    return 18;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialCustom3)
                {
                    return 19;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovDiffuse)
                {
                    return 20;
                }

                if (id == HoUrpBuiltInNames.DebugViews.SssMask)
                {
                    return 22;
                }

                if (id == HoUrpBuiltInNames.DebugViews.SssSource)
                {
                    return 23;
                }

                if (id == HoUrpBuiltInNames.DebugViews.SssDiffusion)
                {
                    return 24;
                }

                if (id == HoUrpBuiltInNames.DebugViews.SssCompositeWeight)
                {
                    return 25;
                }

                if (id == HoUrpBuiltInNames.DebugViews.OitAccumulation)
                {
                    return 40;
                }

                if (id == HoUrpBuiltInNames.DebugViews.OitRevealage)
                {
                    return 41;
                }

                if (id == HoUrpBuiltInNames.DebugViews.ShadowCastAtlas)
                {
                    return 42;
                }

                if (id == HoUrpBuiltInNames.DebugViews.ShadowCastSecondDirectionalAtlas)
                {
                    return 43;
                }

                return 0;
            }

            private static string ResolvePreviewLabel(DebugViewDefinition debugView)
            {
                return string.IsNullOrEmpty(debugView.PreviewLabel)
                    ? ResolveFallbackLabel(debugView)
                    : debugView.PreviewLabel;
            }

            private static string ResolveFallbackLabel(DebugViewDefinition debugView)
            {
                HoUrpIdentifier id = debugView.Id;
                if (id == HoUrpBuiltInNames.DebugViews.AovMask)
                {
                    return nameof(RenderCacheDebugView.Mask);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectId)
                {
                    return nameof(RenderCacheDebugView.ObjectId);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag0)
                {
                    return nameof(RenderCacheDebugView.Flag0Reserved);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag1)
                {
                    return nameof(RenderCacheDebugView.PostReceiver);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag2)
                {
                    return nameof(RenderCacheDebugView.Flag2);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag3)
                {
                    return nameof(RenderCacheDebugView.Flag3);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag4)
                {
                    return nameof(RenderCacheDebugView.Flag4);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag5)
                {
                    return nameof(RenderCacheDebugView.Flag5);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag6)
                {
                    return nameof(RenderCacheDebugView.Flag6);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag7)
                {
                    return nameof(RenderCacheDebugView.Flag7);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag8)
                {
                    return nameof(RenderCacheDebugView.Flag8);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag9)
                {
                    return nameof(RenderCacheDebugView.Flag9);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag10)
                {
                    return nameof(RenderCacheDebugView.Flag10);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag11)
                {
                    return nameof(RenderCacheDebugView.Flag11);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectFlag12)
                {
                    return nameof(RenderCacheDebugView.Flag12);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovLinearDepth)
                {
                    return nameof(RenderCacheDebugView.LinearDepth);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovWorldNormal)
                {
                    return nameof(RenderCacheDebugView.WorldNormal);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom0)
                {
                    return nameof(RenderCacheDebugView.Subject);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom1)
                {
                    return nameof(RenderCacheDebugView.Face);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom2)
                {
                    return nameof(RenderCacheDebugView.Hair);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom3)
                {
                    return nameof(RenderCacheDebugView.Eye);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom4)
                {
                    return nameof(RenderCacheDebugView.Accessory);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom5)
                {
                    return nameof(RenderCacheDebugView.Cloth);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom6)
                {
                    return nameof(RenderCacheDebugView.Prop);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovObjectCustom7)
                {
                    return nameof(RenderCacheDebugView.Reserved);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialClass)
                {
                    return nameof(RenderCacheDebugView.MaterialClass);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovSssProfile)
                {
                    return nameof(RenderCacheDebugView.SssProfile);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovThickness)
                {
                    return nameof(RenderCacheDebugView.Thickness);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovCurvature)
                {
                    return nameof(RenderCacheDebugView.Curvature);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialCustom0)
                {
                    return nameof(RenderCacheDebugView.MaterialCustom0);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialCustom1)
                {
                    return nameof(RenderCacheDebugView.MaterialCustom1);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialCustom2)
                {
                    return nameof(RenderCacheDebugView.MaterialCustom2);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovMaterialCustom3)
                {
                    return nameof(RenderCacheDebugView.MaterialCustom3);
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovDiffuse)
                {
                    return nameof(RenderCacheDebugView.Diffuse);
                }

                if (id == HoUrpBuiltInNames.DebugViews.SssMask)
                {
                    return nameof(RenderCacheDebugView.SssMask);
                }

                if (id == HoUrpBuiltInNames.DebugViews.SssSource)
                {
                    return nameof(RenderCacheDebugView.SssPreparedSource);
                }

                if (id == HoUrpBuiltInNames.DebugViews.SssDiffusion)
                {
                    return nameof(RenderCacheDebugView.SssDiffusion);
                }

                if (id == HoUrpBuiltInNames.DebugViews.SssCompositeWeight)
                {
                    return nameof(RenderCacheDebugView.SssCompositeWeight);
                }

                string text = id.ToString();
                int separator = text.LastIndexOf('.');
                return separator >= 0 && separator + 1 < text.Length
                    ? text.Substring(separator + 1)
                    : text;
            }

            private static LabelData EncodeLabel(string label)
            {
                string text = NormalizeLabel(label);
                return new LabelData(
                    PackLabelChunk(text, 0),
                    PackLabelChunk(text, 4),
                    PackLabelChunk(text, 8),
                    PackLabelChunk(text, 12));
            }

            private static string NormalizeLabel(string label)
            {
                if (string.IsNullOrEmpty(label))
                {
                    return string.Empty;
                }

                List<char> chars = new List<char>(16);
                char previous = '\0';
                for (int i = 0; i < label.Length && chars.Count < 16; i++)
                {
                    char c = label[i];
                    if (char.IsUpper(c) && i > 0 && previous != ' ' && !char.IsUpper(previous))
                    {
                        chars.Add(' ');
                        if (chars.Count >= 16)
                        {
                            break;
                        }
                    }

                    chars.Add(NormalizeLabelChar(c));
                    previous = c;
                }

                return new string(chars.ToArray());
            }

            private static char NormalizeLabelChar(char c)
            {
                if (c >= 'a' && c <= 'z')
                {
                    return (char)(c - 32);
                }

                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    return c;
                }

                return ' ';
            }

            private static Vector4 PackLabelChunk(string label, int start)
            {
                return new Vector4(
                    LabelCharCode(label, start),
                    LabelCharCode(label, start + 1),
                    LabelCharCode(label, start + 2),
                    LabelCharCode(label, start + 3));
            }

            private static float LabelCharCode(string label, int index)
            {
                return index >= 0 && index < label.Length ? label[index] : ' ';
            }

            private readonly struct DebugTile
            {
                public DebugTile(TextureHandle sourceTexture, int shaderMode, LabelData label)
                    : this(sourceTexture, shaderMode, label, Vector4.zero)
                {
                }

                public DebugTile(TextureHandle sourceTexture, int shaderMode, LabelData label, Vector4 tileRect)
                {
                    this.sourceTexture = sourceTexture;
                    this.shaderMode = shaderMode;
                    label0 = label.label0;
                    label1 = label.label1;
                    label2 = label.label2;
                    label3 = label.label3;
                    this.tileRect = tileRect;
                }

                public readonly TextureHandle sourceTexture;
                public readonly int shaderMode;
                public readonly Vector4 label0;
                public readonly Vector4 label1;
                public readonly Vector4 label2;
                public readonly Vector4 label3;
                public readonly Vector4 tileRect;
            }

            private readonly struct LabelData
            {
                public LabelData(Vector4 label0, Vector4 label1, Vector4 label2, Vector4 label3)
                {
                    this.label0 = label0;
                    this.label1 = label1;
                    this.label2 = label2;
                    this.label3 = label3;
                }

                public readonly Vector4 label0;
                public readonly Vector4 label1;
                public readonly Vector4 label2;
                public readonly Vector4 label3;
            }

            private sealed class AllPassData
            {
                public Material material;
                public List<DebugTile> tiles;
                public Vector4 tileGrid;
            }
        }
    }
}
