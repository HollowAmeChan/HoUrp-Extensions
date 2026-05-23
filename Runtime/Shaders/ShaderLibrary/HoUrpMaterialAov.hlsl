#ifndef HOURP_MATERIAL_AOV_INCLUDED
#define HOURP_MATERIAL_AOV_INCLUDED

#include "HoUrpMaterialSurface.hlsl"

struct HoUrpAovOutputData
{
    half4 surfaceData;
    half4 materialCustom0_3;
    half4 diffuse;
};

HoUrpAovOutputData HoUrpEncodeMaterialAov(HoUrpMaterialSemanticData semantic, half maskWeight)
{
    half coverage = saturate(maskWeight);
    // Byte-like material semantics must remain decodable even when mask coverage is fractional.
    // Mask only gates participation.
    half semanticGate = coverage > 0.0h ? 1.0h : 0.0h;

    HoUrpAovOutputData data;
    data.surfaceData = half4(
        saturate(semantic.materialClass / 255.0h),
        saturate(semantic.sssProfile / 255.0h),
        saturate(semantic.thickness),
        saturate(semantic.curvature * 0.5h + 0.5h)) * semanticGate;
    data.materialCustom0_3 = saturate(semantic.materialCustom0_3) * semanticGate;
    data.diffuse = half4(max(semantic.sssSourceColor, 0.0h) * coverage, 0.0h);
    return data;
}

#endif
