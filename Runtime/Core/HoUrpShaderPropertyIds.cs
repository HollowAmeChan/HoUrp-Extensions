using UnityEngine;

namespace HoUrp.Extensions.Core
{
    public static class HoUrpShaderPropertyIds
    {
        public const string AovOutputFallbackShaderName = "Hidden/HoURP/AOV/AovOutputFallback";
        public const string AovDebugShaderName = "Hidden/HoURP/Debug/AovDebug";
        public const string SemanticPostAovReadProbeShaderName = "Hidden/HoURP/SemanticPost/AovReadProbe";
        public const string SubsurfaceScatteringShaderName = "Hidden/HoURP/SSS/SubsurfaceScattering";

        public static readonly int AovMaskIdTexture = Shader.PropertyToID("_HoUrpAovMaskIdTexture");
        public static readonly int AovNormalDepthTexture = Shader.PropertyToID("_HoUrpAovNormalDepthTexture");
        public static readonly int AovObjectCustom0_3Texture = Shader.PropertyToID("_HoUrpAovObjectCustom0_3Texture");
        public static readonly int AovObjectCustom4_7Texture = Shader.PropertyToID("_HoUrpAovObjectCustom4_7Texture");
        public static readonly int AovSurfaceDataTexture = Shader.PropertyToID("_HoUrpAovSurfaceDataTexture");
        public static readonly int AovMaterialCustom0_3Texture = Shader.PropertyToID("_HoUrpAovMaterialCustom0_3Texture");
        public static readonly int AovSssSourceTexture = Shader.PropertyToID("_HoUrpAovSssSourceTexture");
        public static readonly int SssSourceTexture = Shader.PropertyToID("_HoUrpSssSourceTexture");
        public static readonly int SssDiffusionTexture = Shader.PropertyToID("_HoUrpSssDiffusionTexture");
        public static readonly int AovDebugSourceTexture = Shader.PropertyToID("_HoUrpAovDebugSourceTexture");
        public static readonly int AovDebugTileMode = Shader.PropertyToID("_HoUrpAovDebugTileMode");
        public static readonly int AovDebugTileRect = Shader.PropertyToID("_HoUrpAovDebugTileRect");
        public static readonly int AovDebugTileGrid = Shader.PropertyToID("_HoUrpAovDebugTileGrid");
        public static readonly int SourceColorTexture = Shader.PropertyToID("_HoUrpSourceColorTexture");
        public static readonly int AovDebugMode = Shader.PropertyToID("_HoUrpAovDebugMode");
        public static readonly int SemanticPostTintColor = Shader.PropertyToID("_HoUrpSemanticPostTintColor");
        public static readonly int AovMaskWeight = Shader.PropertyToID("_HoUrpAovMaskWeight");
        public static readonly int ObjectCustomMask = Shader.PropertyToID("_HoUrpObjectCustomMask");
        public static readonly int MaterialClass = Shader.PropertyToID("_HoUrpMaterialClass");
        public static readonly int MaterialSssProfile = Shader.PropertyToID("_HoUrpMaterialSssProfile");
        public static readonly int MaterialThickness = Shader.PropertyToID("_HoUrpMaterialThickness");
        public static readonly int MaterialCurvature = Shader.PropertyToID("_HoUrpMaterialCurvature");
        public static readonly int MaterialUtility = Shader.PropertyToID("_HoUrpMaterialUtility");
        public static readonly int MaterialCustom0_3 = Shader.PropertyToID("_HoUrpMaterialCustom0_3");
        public static readonly int SssSourceColor = Shader.PropertyToID("_HoUrpSssSourceColor");
        public static readonly int SssWeight = Shader.PropertyToID("_HoUrpSssWeight");
        public static readonly int SemanticPostObjectCustomChannel = Shader.PropertyToID("_HoUrpSemanticPostObjectCustomChannel");
        public static readonly int SemanticPostMaterialCustomChannel = Shader.PropertyToID("_HoUrpSemanticPostMaterialCustomChannel");
        public static readonly int SssStrength = Shader.PropertyToID("_HoUrpSssStrength");
        public static readonly int SssRadius = Shader.PropertyToID("_HoUrpSssRadius");
        public static readonly int SssDepthTolerance = Shader.PropertyToID("_HoUrpSssDepthTolerance");
        public static readonly int SssNormalTolerance = Shader.PropertyToID("_HoUrpSssNormalTolerance");
        public static readonly int SssSourcePreserve = Shader.PropertyToID("_HoUrpSssSourcePreserve");
        public static readonly int SssProfileIds = Shader.PropertyToID("_HoUrpSssProfileIds");
        public static readonly int SssProfileDiffusionParams = Shader.PropertyToID("_HoUrpSssProfileDiffusionParams");
        public static readonly int SssProfileShapeParams = Shader.PropertyToID("_HoUrpSssProfileShapeParams");
    }
}
