#ifndef HOURP_FILTER_BURLEY_DIFFUSION_INCLUDED
#define HOURP_FILTER_BURLEY_DIFFUSION_INCLUDED

#ifndef HOURP_FILTER_PI
#define HOURP_FILTER_PI 3.14159265359
#endif

// Disney Burley profile helpers, adapted from HDRP's SSS convolution path for
// fullscreen URP blit passes. The helper only evaluates profile weights; it
// does not sample AOV or SSS textures.
#define HOURP_FILTER_BURLEY_FILTER_RADIUS 16.5585
#define HOURP_FILTER_LOG2_E 1.44269504089

float3 HoFilterBurleyEvalDiffusionProfile(float radius, float3 shape)
{
    float3 safeShape = max(shape, float3(1.0e-3, 1.0e-3, 1.0e-3));
    float3 exp13 = exp2(((-HOURP_FILTER_LOG2_E / 3.0) * radius) * safeShape);
    float3 expSum = exp13 * (1.0 + exp13 * exp13);
    return (safeShape * (1.0 / (8.0 * HOURP_FILTER_PI))) * expSum;
}

void HoFilterBurleySampleDiffusionProfile(float u, out float radius01, out float rcpPdf)
{
    float safeU = 1.0 - saturate(u);
    safeU = max(safeU, 1.0e-4);

    float g = 1.0 + (4.0 * safeU) * (2.0 * safeU + sqrt(1.0 + (4.0 * safeU) * safeU));
    float n = exp2(log2(g) * (-1.0 / 3.0));
    float p = (g * n) * n;
    float c = 1.0 + p + n;
    float radius = (3.0 / HOURP_FILTER_LOG2_E) * log2(c / (4.0 * safeU));
    float rcpExp = ((c * c) * c) / max((4.0 * safeU) * ((c * c) + (4.0 * safeU) * (4.0 * safeU)), 1.0e-5);

    radius01 = saturate(radius / HOURP_FILTER_BURLEY_FILTER_RADIUS);
    rcpPdf = (8.0 * HOURP_FILTER_PI) * rcpExp;
}

float3 HoFilterBurleyProfileWeight(float radius01, float rcpPdf, float3 diffusionColor)
{
    float radius = max(radius01 * HOURP_FILTER_BURLEY_FILTER_RADIUS, 1.0e-4);
    float3 scatteringDistance = max(diffusionColor, float3(0.08, 0.08, 0.08));
    float3 shape = rcp(scatteringDistance);
    return HoFilterBurleyEvalDiffusionProfile(radius, shape) * rcpPdf;
}

#endif
