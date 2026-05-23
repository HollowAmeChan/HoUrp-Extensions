using System.Collections.Generic;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityRenderGraph = UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace HoUrp.Extensions.Debugging
{
    [DisallowMultipleRendererFeature("HoURP AOV Debug")]
    public sealed class AovDebugRendererFeature : ScriptableRendererFeature
    {
        private enum AovDebugView
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
            SssSource = 22,
            SssWeight = 23,
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
                || debugPass == null
                || debugMaterial == null)
            {
                return;
            }

            if (selectedView == AovDebugView.None)
            {
                return;
            }

            if (selectedView == AovDebugView.AllRegistered)
            {
                debugPass.SetupAll(debugMaterial);
            }
            else
            {
                debugPass.Setup(debugMaterial, ResolveDebugViewId(selectedView), ResolveShaderMode(selectedView));
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

        private static HoUrpIdentifier ResolveDebugViewId(AovDebugView view)
        {
            switch (view)
            {
                case AovDebugView.Mask:
                    return HoUrpBuiltInNames.DebugViews.AovMask;
                case AovDebugView.ObjectId:
                    return HoUrpBuiltInNames.DebugViews.AovObjectId;
                case AovDebugView.Flag0Reserved:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag0;
                case AovDebugView.PostReceiver:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag1;
                case AovDebugView.Flag2:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag2;
                case AovDebugView.Flag3:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag3;
                case AovDebugView.Flag4:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag4;
                case AovDebugView.Flag5:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag5;
                case AovDebugView.Flag6:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag6;
                case AovDebugView.Flag7:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag7;
                case AovDebugView.Flag8:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag8;
                case AovDebugView.Flag9:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag9;
                case AovDebugView.Flag10:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag10;
                case AovDebugView.Flag11:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag11;
                case AovDebugView.Flag12:
                    return HoUrpBuiltInNames.DebugViews.AovObjectFlag12;
                case AovDebugView.LinearDepth:
                    return HoUrpBuiltInNames.DebugViews.AovLinearDepth;
                case AovDebugView.WorldNormal:
                    return HoUrpBuiltInNames.DebugViews.AovWorldNormal;
                case AovDebugView.Subject:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom0;
                case AovDebugView.Face:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom1;
                case AovDebugView.Hair:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom2;
                case AovDebugView.Eye:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom3;
                case AovDebugView.Accessory:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom4;
                case AovDebugView.Cloth:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom5;
                case AovDebugView.Prop:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom6;
                case AovDebugView.Reserved:
                    return HoUrpBuiltInNames.DebugViews.AovObjectCustom7;
                case AovDebugView.MaterialClass:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialClass;
                case AovDebugView.SssProfile:
                    return HoUrpBuiltInNames.DebugViews.AovSssProfile;
                case AovDebugView.Thickness:
                    return HoUrpBuiltInNames.DebugViews.AovThickness;
                case AovDebugView.Curvature:
                    return HoUrpBuiltInNames.DebugViews.AovCurvature;
                case AovDebugView.MaterialCustom0:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom0;
                case AovDebugView.MaterialCustom1:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom1;
                case AovDebugView.MaterialCustom2:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom2;
                case AovDebugView.MaterialCustom3:
                    return HoUrpBuiltInNames.DebugViews.AovMaterialCustom3;
                case AovDebugView.SssSource:
                    return HoUrpBuiltInNames.DebugViews.AovSssSource;
                case AovDebugView.SssWeight:
                    return HoUrpBuiltInNames.DebugViews.AovSssWeight;
                case AovDebugView.SssMask:
                    return HoUrpBuiltInNames.DebugViews.SssMask;
                case AovDebugView.SssPreparedSource:
                    return HoUrpBuiltInNames.DebugViews.SssSource;
                case AovDebugView.SssDiffusion:
                    return HoUrpBuiltInNames.DebugViews.SssDiffusion;
                case AovDebugView.SssCompositeWeight:
                    return HoUrpBuiltInNames.DebugViews.SssCompositeWeight;
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
                case AovDebugView.Flag0Reserved:
                    return 27;
                case AovDebugView.PostReceiver:
                    return 28;
                case AovDebugView.Flag2:
                    return 29;
                case AovDebugView.Flag3:
                    return 30;
                case AovDebugView.Flag4:
                    return 31;
                case AovDebugView.Flag5:
                    return 32;
                case AovDebugView.Flag6:
                    return 33;
                case AovDebugView.Flag7:
                    return 34;
                case AovDebugView.Flag8:
                    return 35;
                case AovDebugView.Flag9:
                    return 36;
                case AovDebugView.Flag10:
                    return 37;
                case AovDebugView.Flag11:
                    return 38;
                case AovDebugView.Flag12:
                    return 39;
                case AovDebugView.LinearDepth:
                    return 2;
                case AovDebugView.WorldNormal:
                    return 3;
                case AovDebugView.Subject:
                    return 4;
                case AovDebugView.Face:
                    return 5;
                case AovDebugView.Hair:
                    return 6;
                case AovDebugView.Eye:
                    return 7;
                case AovDebugView.Accessory:
                    return 8;
                case AovDebugView.Cloth:
                    return 9;
                case AovDebugView.Prop:
                    return 10;
                case AovDebugView.Reserved:
                    return 11;
                case AovDebugView.MaterialClass:
                    return 12;
                case AovDebugView.SssProfile:
                    return 13;
                case AovDebugView.Thickness:
                    return 14;
                case AovDebugView.Curvature:
                    return 15;
                case AovDebugView.MaterialCustom0:
                    return 16;
                case AovDebugView.MaterialCustom1:
                    return 17;
                case AovDebugView.MaterialCustom2:
                    return 18;
                case AovDebugView.MaterialCustom3:
                    return 19;
                case AovDebugView.SssSource:
                    return 20;
                case AovDebugView.SssWeight:
                    return 21;
                case AovDebugView.SssMask:
                    return 22;
                case AovDebugView.SssPreparedSource:
                    return 23;
                case AovDebugView.SssDiffusion:
                    return 24;
                case AovDebugView.SssCompositeWeight:
                    return 25;
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
            private bool showAllRegistered;

            public AovDebugPass(HoUrpContractRegistry registry)
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
                propertyBlock.SetInt(HoUrpShaderPropertyIds.AovDebugMode, shaderMode);
                propertyBlock.SetInt(HoUrpShaderPropertyIds.AovDebugTileMode, 0);

                RenderGraphUtils.BlitMaterialParameters blitParameters =
                    new RenderGraphUtils.BlitMaterialParameters(sourceTexture, destination, material, 0)
                    {
                        propertyBlock = propertyBlock,
                        sourceTexturePropertyID = HoUrpShaderPropertyIds.AovDebugSourceTexture
                    };
                renderGraph.AddBlitPass(blitParameters, passName: "HoURP AOV Debug");
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

                    tiles.Add(new DebugTile(sourceTexture, ResolveShaderMode(debugView)));
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
                        CalculateTileRect(
                            column,
                            row,
                            columns,
                            rows,
                            sourceAspect,
                            targetAspect));
                }

                using (var builder = renderGraph.AddRasterRenderPass<AllPassData>(
                    "HoURP AOV Debug All",
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
                            PropertyBlock.SetInt(HoUrpShaderPropertyIds.AovDebugTileMode, 1);
                            PropertyBlock.SetInt(HoUrpShaderPropertyIds.AovDebugMode, tile.shaderMode);
                            PropertyBlock.SetVector(HoUrpShaderPropertyIds.AovDebugTileRect, tile.tileRect);
                            PropertyBlock.SetVector(HoUrpShaderPropertyIds.AovDebugTileGrid, data.tileGrid);
                            context.cmd.SetGlobalTexture(HoUrpShaderPropertyIds.AovDebugSourceTexture, tile.sourceTexture);
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
                columns = Mathf.CeilToInt(Mathf.Sqrt(tileCount));
                rows = Mathf.CeilToInt(tileCount / (float)columns);

                float desiredNormalizedAspect = Mathf.Max(0.0001f, sourceAspect / Mathf.Max(0.0001f, targetAspect));
                float bestScore = float.MaxValue;

                for (int candidateColumns = 1; candidateColumns <= tileCount; candidateColumns++)
                {
                    int candidateRows = Mathf.CeilToInt(tileCount / (float)candidateColumns);
                    float cellAspect = (1.0f / candidateColumns) / (1.0f / candidateRows);
                    float aspectFit = cellAspect > desiredNormalizedAspect
                        ? desiredNormalizedAspect / cellAspect
                        : cellAspect / desiredNormalizedAspect;
                    int emptyCells = candidateColumns * candidateRows - tileCount;
                    float emptyPenalty = emptyCells / (float)tileCount;
                    float score = (1.0f - aspectFit) + emptyPenalty * 0.8f;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        columns = candidateColumns;
                        rows = candidateRows;
                    }
                }
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

                float innerX = cellX;
                float innerY = cellY;
                float innerWidth = cellWidth;
                float innerHeight = cellHeight;

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

            private static int ResolveShaderMode(DebugViewDefinition debugView)
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

                if (id == HoUrpBuiltInNames.DebugViews.AovSssSource)
                {
                    return 20;
                }

                if (id == HoUrpBuiltInNames.DebugViews.AovSssWeight)
                {
                    return 21;
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

                return 0;
            }

            private readonly struct DebugTile
            {
                public DebugTile(TextureHandle sourceTexture, int shaderMode)
                    : this(sourceTexture, shaderMode, Vector4.zero)
                {
                }

                public DebugTile(TextureHandle sourceTexture, int shaderMode, Vector4 tileRect)
                {
                    this.sourceTexture = sourceTexture;
                    this.shaderMode = shaderMode;
                    this.tileRect = tileRect;
                }

                public readonly TextureHandle sourceTexture;
                public readonly int shaderMode;
                public readonly Vector4 tileRect;
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
