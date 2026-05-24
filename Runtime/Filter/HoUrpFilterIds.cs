using UnityEngine;

namespace HoUrp.Extensions.Filter
{
    internal static class HoUrpFilterIds
    {
        public const string BlurShaderName = "Hidden/HoURP/Filter/Blur";
        public const string SssDiffusionShaderName = "Hidden/HoURP/SSS/SubsurfaceScattering";

        public const int CopyPass = 0;
        public const int SeparableBlurPass = 1;
        public const int DepthNormalAwareBlurPass = 2;

        public const int SssSourcePreparePass = 0;
        public const int SssDiffusionPass = 1;
        public const int SssCompositePass = 2;
        public const int SssDebugPass = 3;

        public static readonly int SourceTex = Shader.PropertyToID("_HoFilterSourceTex");
        public static readonly int GuideDepthTex = Shader.PropertyToID("_HoFilterGuideDepthTex");
        public static readonly int GuideNormalTex = Shader.PropertyToID("_HoFilterGuideNormalTex");
        public static readonly int Params0 = Shader.PropertyToID("_HoFilterParams0");
        public static readonly int Direction = Shader.PropertyToID("_HoFilterDirection");
        public static readonly int DebugMode = Shader.PropertyToID("_HoFilterDebugMode");
    }
}
