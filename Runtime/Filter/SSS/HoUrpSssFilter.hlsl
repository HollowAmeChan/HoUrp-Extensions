#ifndef HOURP_SSS_FILTER_INCLUDED
#define HOURP_SSS_FILTER_INCLUDED

#include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl"
#include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl"

float3 HoUrpSssDecodeNormal(float3 encodedNormal)
{
    return HoFilterSafeNormalize(encodedNormal * 2.0 - 1.0, float3(0.0, 0.0, 1.0));
}

float HoUrpSssProfileByte(float4 surfaceData)
{
    return HoFilterByteValue(surfaceData.g);
}

float HoUrpSssSurfaceThinness(float4 surfaceData, float thicknessScale)
{
    return saturate(surfaceData.b * max(thicknessScale, 0.0));
}

float HoUrpSssGeometryValid(float4 normalDepth)
{
    return step(1.0e-5, dot(abs(normalDepth.rgb), float3(1.0, 1.0, 1.0)));
}

float HoUrpSssReceivesSss(float4 maskId)
{
    return HoFilterPickLowByteFlag(maskId.a, 4.0);
}

float HoUrpSssSourceParticipation(float4 maskId, float4 normalDepth, float4 surfaceData, float thicknessScale)
{
    float receivesSss = HoUrpSssReceivesSss(maskId);
    float geometryValid = HoUrpSssGeometryValid(normalDepth);
    float profileValid = step(0.5 / 255.0, surfaceData.g);
    float thicknessGate = saturate(HoUrpSssSurfaceThinness(surfaceData, thicknessScale) * 4.0);
    return saturate(maskId.r * receivesSss * geometryValid * profileValid * thicknessGate);
}

#endif
