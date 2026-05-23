using HoUrp.Extensions.Core;
using UnityEngine.Rendering;

namespace HoUrp.Extensions.OIT
{
    public static class WeightedOitShaderConstants
    {
        public const string ShaderPassName = "HoUrpOitAccumulation";
        public const string CompositeShaderName = HoUrpShaderPropertyIds.OitWeightedCompositeShaderName;

        public static readonly ShaderTagId ShaderTagId = new ShaderTagId(ShaderPassName);
    }
}
