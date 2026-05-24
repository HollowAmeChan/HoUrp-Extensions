#ifndef HOURP_FILTER_COMMON_INCLUDED
#define HOURP_FILTER_COMMON_INCLUDED

float3 HoFilterSafeNormalize(float3 value, float3 fallback)
{
    float lengthSq = dot(value, value);
    return lengthSq > 1.0e-8 ? value * rsqrt(lengthSq) : fallback;
}

float2 HoFilterGetTexelSize(float2 textureSize)
{
    return rcp(max(textureSize, float2(1.0, 1.0)));
}

float HoFilterLuma(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float HoFilterSaturateWeight(float weight)
{
    return saturate(max(weight, 0.0));
}

float4 HoFilterEncodeDebugWeight(float weight)
{
    float value = HoFilterSaturateWeight(weight);
    return float4(value, value, value, 1.0);
}

float HoFilterPickLowByteFlag(float packedFlags, float bitValue)
{
    float flags = round(saturate(packedFlags) * 255.0);
    return step(0.5, fmod(floor(flags / max(bitValue, 1.0)), 2.0));
}

float HoFilterByteValue(float encoded)
{
    return round(saturate(encoded) * 255.0);
}

#endif
