#ifndef HOURP_MATERIAL_OIT_INCLUDED
#define HOURP_MATERIAL_OIT_INCLUDED

#include "HoUrpMaterialSurface.hlsl"

struct HoUrpOitAccumulationData
{
    half3 weightedColor;
    half weightedAlpha;
    half revealage;
    half weight;
};

half HoUrpComputeOitWeight(HoUrpTransparentOutputData transparentData)
{
    half alpha = saturate(transparentData.alpha * transparentData.coverage);
    return max(0.01h, alpha * max(transparentData.depthWeight, 0.01h));
}

HoUrpOitAccumulationData HoUrpEncodeOitAccumulation(HoUrpTransparentOutputData transparentData)
{
    half alpha = saturate(transparentData.alpha * transparentData.coverage);
    half weight = HoUrpComputeOitWeight(transparentData);

    HoUrpOitAccumulationData data;
    data.weightedColor = transparentData.color * alpha * weight;
    data.weightedAlpha = alpha * weight;
    data.revealage = alpha;
    data.weight = weight;
    return data;
}

#endif
