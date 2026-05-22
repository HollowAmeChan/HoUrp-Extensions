using UnityEngine;

namespace HoUrp.Extensions.Core
{
    public static class HoUrpShaderPropertyIds
    {
        public const string AovOutputFallbackShaderName = "Hidden/HoURP/AOV/AovOutputFallback";
        public const string AovDebugShaderName = "Hidden/HoURP/Debug/AovDebug";
        public const string SemanticPostAovReadProbeShaderName = "Hidden/HoURP/SemanticPost/AovReadProbe";

        public static readonly int AovMaskIdTexture = Shader.PropertyToID("_HoUrpAovMaskIdTexture");
        public static readonly int AovNormalDepthTexture = Shader.PropertyToID("_HoUrpAovNormalDepthTexture");
        public static readonly int AovObjectCustom0_3Texture = Shader.PropertyToID("_HoUrpAovObjectCustom0_3Texture");
        public static readonly int AovObjectCustom4_7Texture = Shader.PropertyToID("_HoUrpAovObjectCustom4_7Texture");
        public static readonly int AovDebugSourceTexture = Shader.PropertyToID("_HoUrpAovDebugSourceTexture");
        public static readonly int AovDebugTileMode = Shader.PropertyToID("_HoUrpAovDebugTileMode");
        public static readonly int AovDebugTileRect = Shader.PropertyToID("_HoUrpAovDebugTileRect");
        public static readonly int SourceColorTexture = Shader.PropertyToID("_HoUrpSourceColorTexture");
        public static readonly int AovDebugMode = Shader.PropertyToID("_HoUrpAovDebugMode");
        public static readonly int SemanticPostTintColor = Shader.PropertyToID("_HoUrpSemanticPostTintColor");
        public static readonly int AovMaskWeight = Shader.PropertyToID("_HoUrpAovMaskWeight");
        public static readonly int ObjectCustomMask = Shader.PropertyToID("_HoUrpObjectCustomMask");
        public static readonly int SemanticPostObjectCustomChannel = Shader.PropertyToID("_HoUrpSemanticPostObjectCustomChannel");
    }
}
