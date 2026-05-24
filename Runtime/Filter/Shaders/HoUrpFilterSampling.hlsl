#ifndef HOURP_FILTER_SAMPLING_INCLUDED
#define HOURP_FILTER_SAMPLING_INCLUDED

#define HOURP_FILTER_PI 3.14159265359

float HoFilterInterleavedNoise(float2 uv, float2 screenSize)
{
    float2 pixel = floor(uv * max(screenSize, float2(1.0, 1.0)));
    return frac(52.9829189 * frac(0.06711056 * pixel.x + 0.00583715 * pixel.y));
}

float2 HoFilterGoldenAngleOffset(int sampleIndex, float radius, float phase)
{
    const float goldenAngle = 2.39996323;
    float angle = phase + (float)sampleIndex * goldenAngle;
    return float2(cos(angle), sin(angle)) * radius;
}

float2 HoFilterAxisOffset(float2 texelSize, float2 direction, float radius, int index)
{
    float2 axis = length(direction) > 1.0e-5 ? normalize(direction) : float2(1.0, 0.0);
    return axis * texelSize * radius * (float)index;
}

#endif
