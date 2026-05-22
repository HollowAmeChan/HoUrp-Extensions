#ifndef HOURP_OBJECT_SEMANTIC_INCLUDED
#define HOURP_OBJECT_SEMANTIC_INCLUDED

// Object semantics are renderer-owned ABI. Materials may consume the resolved values,
// but material assets should not expose these fields as editable material properties.
float _HoUrpAovMaskWeight;
float _HoUrpObjectId;
float _HoUrpObjectGroupId;
float _HoUrpObjectFlags;
float _HoUrpObjectCustomMask;

struct HoUrpObjectSemanticData
{
    half maskWeight;
    float objectCustomMask;
    float objectId;
    float objectGroupAndHighFlags;
    float objectFlags;
};

half HoUrpHasObjectSemanticMaskBit(float mask, float bitValue)
{
    return half(step(0.5, fmod(floor(mask / bitValue), 2.0)));
}

bool HoUrpHasRendererSemanticV1(uint packedValue)
{
    return (packedValue & 0x80000000u) != 0u && (((packedValue >> 29u) & 3u) == 1u);
}

HoUrpObjectSemanticData HoUrpResolveObjectSemanticData()
{
    HoUrpObjectSemanticData data;
    data.maskWeight = half(saturate(_HoUrpAovMaskWeight));
    data.objectCustomMask = round(clamp(_HoUrpObjectCustomMask, 0.0, 255.0));
    data.objectId = _HoUrpObjectId;

    float objectGroupId = floor(clamp(_HoUrpObjectGroupId, 0.0, 7.0));
    float objectFeatureFlags = floor(clamp(_HoUrpObjectFlags, 0.0, 8191.0));
    data.objectFlags = fmod(objectFeatureFlags, 256.0);
    float objectFeatureFlagsHigh = floor(objectFeatureFlags / 256.0);
    data.objectGroupAndHighFlags = objectGroupId + objectFeatureFlagsHigh * 8.0;

    // RSUV is the preferred object-semantic source; MPB uniforms remain as fallback
    // for renderers or authoring modes that cannot write renderer user values.
    uint rendererStaticSemantic = unity_RendererUserValue;
    if (HoUrpHasRendererSemanticV1(rendererStaticSemantic))
    {
        uint featureFlags = ((rendererStaticSemantic >> 8u) & 8191u) & ~1u;
        data.objectCustomMask = float(rendererStaticSemantic & 255u);
        data.objectFlags = float(featureFlags & 255u);
        objectFeatureFlagsHigh = float((featureFlags >> 8u) & 31u);
        data.objectId = float((rendererStaticSemantic >> 21u) & 15u);
        objectGroupId = float((rendererStaticSemantic >> 25u) & 7u);
        data.objectGroupAndHighFlags = objectGroupId + objectFeatureFlagsHigh * 8.0;
    }

    return data;
}

half4 HoUrpEncodeObjectMaskId(HoUrpObjectSemanticData data)
{
    return half4(
        data.maskWeight,
        half(saturate(data.objectId / 255.0)),
        half(saturate(data.objectGroupAndHighFlags / 255.0)),
        half(saturate(data.objectFlags / 255.0)));
}

half4 HoUrpEncodeObjectCustom0_3(HoUrpObjectSemanticData data)
{
    return half4(
        HoUrpHasObjectSemanticMaskBit(data.objectCustomMask, 1.0),
        HoUrpHasObjectSemanticMaskBit(data.objectCustomMask, 2.0),
        HoUrpHasObjectSemanticMaskBit(data.objectCustomMask, 4.0),
        HoUrpHasObjectSemanticMaskBit(data.objectCustomMask, 8.0)) * data.maskWeight;
}

half4 HoUrpEncodeObjectCustom4_7(HoUrpObjectSemanticData data)
{
    return half4(
        HoUrpHasObjectSemanticMaskBit(data.objectCustomMask, 16.0),
        HoUrpHasObjectSemanticMaskBit(data.objectCustomMask, 32.0),
        HoUrpHasObjectSemanticMaskBit(data.objectCustomMask, 64.0),
        HoUrpHasObjectSemanticMaskBit(data.objectCustomMask, 128.0)) * data.maskWeight;
}

#endif
