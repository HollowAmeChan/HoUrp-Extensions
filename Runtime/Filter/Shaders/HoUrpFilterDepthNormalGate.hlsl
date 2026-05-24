#ifndef HOURP_FILTER_DEPTH_NORMAL_GATE_INCLUDED
#define HOURP_FILTER_DEPTH_NORMAL_GATE_INCLUDED

float HoFilterDepthGate(float sampleDepth, float centerDepth, float tolerance)
{
    return saturate(1.0 - abs(sampleDepth - centerDepth) / max(tolerance, 1.0e-5));
}

float HoFilterNormalGate(float3 sampleNormal, float3 centerNormal, float tolerance)
{
    float normalizedTolerance = saturate(tolerance);
    return saturate((dot(sampleNormal, centerNormal) - normalizedTolerance) / max(1.0e-5, 1.0 - normalizedTolerance));
}

float HoFilterByteProfileGate(float sampleProfileByte, float centerProfileByte)
{
    return 1.0 - step(0.5, abs(sampleProfileByte - centerProfileByte));
}

float HoFilterMaskGate(float mask)
{
    return saturate(mask);
}

#endif
