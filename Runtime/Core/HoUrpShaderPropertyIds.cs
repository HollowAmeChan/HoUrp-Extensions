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
        public static readonly int SourceColorTexture = Shader.PropertyToID("_HoUrpSourceColorTexture");
        public static readonly int AovDebugMode = Shader.PropertyToID("_HoUrpAovDebugMode");
        public static readonly int SemanticPostTintColor = Shader.PropertyToID("_HoUrpSemanticPostTintColor");
    }
}
