#ifndef HOURP_FILTER_BURLEY_DIFFUSION_INCLUDED
#define HOURP_FILTER_BURLEY_DIFFUSION_INCLUDED

#ifndef HOURP_FILTER_PI
#define HOURP_FILTER_PI 3.14159265359
#endif

// Derived from the old HoSSS Burley-like realtime approximation.
// This helper only evaluates profile weights; it does not sample AOV or SSS textures.
float3 HoFilterBurleyEvalDiffusionProfile(float radius, float3 shape)
{
    float3 safeShape = max(shape, float3(1.0e-3, 1.0e-3, 1.0e-3));
    float3 expA = exp(-radius / safeShape);
    float3 expB = exp(-radius / max(safeShape * 3.0, float3(1.0e-3, 1.0e-3, 1.0e-3)));
    return (expA + expB) / max(8.0 * HOURP_FILTER_PI * safeShape * radius, float3(1.0e-3, 1.0e-3, 1.0e-3));
}

void HoFilterBurleySampleDiffusionProfile(float u, out float radius01, out float rcpPdf)
{
    float safeU = saturate(u);
    radius01 = sqrt(safeU);
    rcpPdf = max(2.0 * radius01, 1.0e-3);
}

float3 HoFilterBurleyProfileWeight(float radius01, float rcpPdf, float3 diffusionColor)
{
    float radius = max(radius01, 1.0e-3);
    float3 shape = max(diffusionColor, float3(0.08, 0.08, 0.08));
    return HoFilterBurleyEvalDiffusionProfile(radius, shape) * rcpPdf;
}

#endif
