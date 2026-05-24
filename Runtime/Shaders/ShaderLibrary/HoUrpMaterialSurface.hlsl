#ifndef HOURP_MATERIAL_SURFACE_INCLUDED
#define HOURP_MATERIAL_SURFACE_INCLUDED

struct HoUrpSurfaceData
{
    half3 baseColor;
    half alpha;
    half3 normalWS;
    half3 normalTS;
    half roughness;
    half metallic;
    half3 emission;
    half occlusion;
};

struct HoUrpMaterialSemanticData
{
    half materialClass;
    half sssProfile;
    half thickness;
    half curvature;
    half4 materialCustom0_3;
    half3 sssSourceColor;
};

struct HoUrpTransparentOutputData
{
    half3 color;
    half alpha;
    half coverage;
    half depthWeight;
    half supportsOit;
    half participatesOit;
};

HoUrpSurfaceData HoUrpCreateSurfaceData(
    half3 baseColor,
    half alpha,
    half3 normalWS)
{
    HoUrpSurfaceData data;
    data.baseColor = baseColor;
    data.alpha = saturate(alpha);
    data.normalWS = normalize(normalWS);
    data.normalTS = half3(0.0h, 0.0h, 1.0h);
    data.roughness = 0.5h;
    data.metallic = 0.0h;
    data.emission = half3(0.0h, 0.0h, 0.0h);
    data.occlusion = 1.0h;
    return data;
}

HoUrpMaterialSemanticData HoUrpCreateMaterialSemanticData(
    half materialClass,
    half sssProfile,
    half thickness,
    half curvature,
    half4 materialCustom0_3,
    half3 sssSourceColor)
{
    HoUrpMaterialSemanticData data;
    data.materialClass = clamp(materialClass, 0.0h, 255.0h);
    data.sssProfile = clamp(sssProfile, 0.0h, 255.0h);
    data.thickness = saturate(thickness);
    data.curvature = clamp(curvature, -1.0h, 1.0h);
    data.materialCustom0_3 = saturate(materialCustom0_3);
    data.sssSourceColor = max(sssSourceColor, 0.0h);
    return data;
}

HoUrpTransparentOutputData HoUrpCreateTransparentOutputData(
    HoUrpSurfaceData surface,
    half supportsOit,
    half participatesOit)
{
    HoUrpTransparentOutputData data;
    data.color = surface.baseColor;
    data.alpha = saturate(surface.alpha);
    data.coverage = data.alpha;
    data.depthWeight = 1.0h;
    data.supportsOit = saturate(supportsOit);
    data.participatesOit = saturate(participatesOit);
    return data;
}

#endif
