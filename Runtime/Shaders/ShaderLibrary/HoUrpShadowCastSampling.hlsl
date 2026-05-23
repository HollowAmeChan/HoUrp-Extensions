#ifndef HOURP_SHADOW_CAST_SAMPLING_INCLUDED
#define HOURP_SHADOW_CAST_SAMPLING_INCLUDED

#define HOURP_SHADOW_CAST_MAX_LIGHTS 8
#define HOURP_SHADOW_CAST_MAX_SLICES 28
#define HOURP_SHADOW_CAST_LIGHT_SPOT 1.0
#define HOURP_SHADOW_CAST_LIGHT_POINT 2.0
#define HOURP_SHADOW_CAST_MIN_ATTENUATION 0.15
#define HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_LIGHTS 4
#define HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_CASCADES 4
#define HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_SLICES 16

float _HoUrpShadowCastActive;
int _HoUrpShadowCastLightCount;
int _HoUrpShadowCastSliceCount;
float _HoUrpShadowReceiverStrength;
float4 _HoUrpShadowCastAtlasSize;
float4 _HoUrpShadowCastWorldToShadowRow0[HOURP_SHADOW_CAST_MAX_SLICES];
float4 _HoUrpShadowCastWorldToShadowRow1[HOURP_SHADOW_CAST_MAX_SLICES];
float4 _HoUrpShadowCastWorldToShadowRow2[HOURP_SHADOW_CAST_MAX_SLICES];
float4 _HoUrpShadowCastWorldToShadowRow3[HOURP_SHADOW_CAST_MAX_SLICES];
float4 _HoUrpShadowCastLightData0[HOURP_SHADOW_CAST_MAX_LIGHTS];
float4 _HoUrpShadowCastLightData1[HOURP_SHADOW_CAST_MAX_LIGHTS];
float4 _HoUrpShadowCastLightData2[HOURP_SHADOW_CAST_MAX_LIGHTS];
float4 _HoUrpShadowCastLightAttenuation[HOURP_SHADOW_CAST_MAX_LIGHTS];
float4 _HoUrpShadowCastLightColor[HOURP_SHADOW_CAST_MAX_LIGHTS];
float4 _HoUrpShadowCastSliceData[HOURP_SHADOW_CAST_MAX_SLICES];
float4 _HoUrpShadowCastPcssParams;
float4 _HoUrpShadowCastPcssParams2;

float4 _HoUrpShadowCastSecondDirectionalParams;
float4 _HoUrpShadowCastSecondDirectionalCameraPosition;
float4 _HoUrpShadowCastSecondDirectionalAtlasSize;
float4 _HoUrpShadowCastSecondDirectionalPcssParams;
float4 _HoUrpShadowCastSecondDirectionalWorldToShadowRow0[HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_SLICES];
float4 _HoUrpShadowCastSecondDirectionalWorldToShadowRow1[HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_SLICES];
float4 _HoUrpShadowCastSecondDirectionalWorldToShadowRow2[HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_SLICES];
float4 _HoUrpShadowCastSecondDirectionalWorldToShadowRow3[HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_SLICES];
float4 _HoUrpShadowCastSecondDirectionalLightData[HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_LIGHTS];
float4 _HoUrpShadowCastSecondDirectionalSliceData[HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_SLICES];

TEXTURE2D_FLOAT(_HoUrpShadowCastAtlas);
TEXTURE2D_FLOAT(_HoUrpShadowCastSecondDirectionalAtlas);

float4 HoUrpShadowCastTransform(int sliceIndex, float3 positionWS)
{
    float4 position = float4(positionWS, 1.0);
    return float4(
        dot(_HoUrpShadowCastWorldToShadowRow0[sliceIndex], position),
        dot(_HoUrpShadowCastWorldToShadowRow1[sliceIndex], position),
        dot(_HoUrpShadowCastWorldToShadowRow2[sliceIndex], position),
        dot(_HoUrpShadowCastWorldToShadowRow3[sliceIndex], position));
}

float4 HoUrpShadowCastTransformSecondDirectional(int sliceIndex, float3 positionWS)
{
    float4 position = float4(positionWS, 1.0);
    return float4(
        dot(_HoUrpShadowCastSecondDirectionalWorldToShadowRow0[sliceIndex], position),
        dot(_HoUrpShadowCastSecondDirectionalWorldToShadowRow1[sliceIndex], position),
        dot(_HoUrpShadowCastSecondDirectionalWorldToShadowRow2[sliceIndex], position),
        dot(_HoUrpShadowCastSecondDirectionalWorldToShadowRow3[sliceIndex], position));
}

float HoUrpShadowCastCompareDepth(float rawDepth, float receiverDepth, float bias)
{
    if (rawDepth >= 0.99999)
    {
        return 1.0;
    }

#if UNITY_REVERSED_Z
    return rawDepth <= receiverDepth + bias ? 1.0 : 0.0;
#else
    return rawDepth >= receiverDepth - bias ? 1.0 : 0.0;
#endif
}

float HoUrpShadowCastSampleRaw(float2 atlasUv, bool secondDirectional)
{
    return secondDirectional
        ? SAMPLE_TEXTURE2D_LOD(_HoUrpShadowCastSecondDirectionalAtlas, sampler_PointClamp, atlasUv, 0).r
        : SAMPLE_TEXTURE2D_LOD(_HoUrpShadowCastAtlas, sampler_PointClamp, atlasUv, 0).r;
}

float HoUrpShadowCastSampleAtlas(float3 sliceCoord, float4 sliceData, float depthBias, bool secondDirectional)
{
    if (any(sliceCoord.xy < 0.0) || any(sliceCoord.xy > 1.0) || sliceCoord.z <= 0.0 || sliceCoord.z >= 1.0)
    {
        return 1.0;
    }

    float2 atlasUv = sliceCoord.xy;
    float rawDepth = HoUrpShadowCastSampleRaw(atlasUv, secondDirectional);
    return HoUrpShadowCastCompareDepth(rawDepth, sliceCoord.z, depthBias);
}

float HoUrpShadowCastSampleSlice(int sliceIndex, float3 positionWS)
{
    if (sliceIndex < 0 || sliceIndex >= _HoUrpShadowCastSliceCount || sliceIndex >= HOURP_SHADOW_CAST_MAX_SLICES)
    {
        return 1.0;
    }

    float4 shadowCoord = HoUrpShadowCastTransform(sliceIndex, positionWS);
    if (abs(shadowCoord.w) <= 0.00001)
    {
        return 1.0;
    }

    shadowCoord.xyz /= shadowCoord.w;
    return HoUrpShadowCastSampleAtlas(shadowCoord.xyz, _HoUrpShadowCastSliceData[sliceIndex], _HoUrpShadowCastPcssParams2.x, false);
}

int HoUrpShadowCastPointFaceIndex(float3 lightToReceiver)
{
    float3 absDirection = abs(lightToReceiver);
    if (absDirection.x >= absDirection.y && absDirection.x >= absDirection.z)
    {
        return lightToReceiver.x >= 0.0 ? 0 : 1;
    }

    if (absDirection.y >= absDirection.x && absDirection.y >= absDirection.z)
    {
        return lightToReceiver.y >= 0.0 ? 2 : 3;
    }

    return lightToReceiver.z >= 0.0 ? 4 : 5;
}

float HoUrpShadowCastLightInfluence(int lightIndex, float3 positionWS)
{
    float4 lightData0 = _HoUrpShadowCastLightData0[lightIndex];
    float lightType = lightData0.x;
    float4 lightData1 = _HoUrpShadowCastLightData1[lightIndex];
    float3 lightToReceiver = positionWS - lightData1.xyz;
    float distanceSqr = dot(lightToReceiver, lightToReceiver);
    float4 lightAttenuation = _HoUrpShadowCastLightAttenuation[lightIndex];
    float rangeFactor = saturate(distanceSqr * lightAttenuation.x);
    if (rangeFactor >= 1.0)
    {
        return 0.0;
    }

    float rangeFade = saturate(1.0 - rangeFactor * rangeFactor);
    rangeFade *= rangeFade;
    rangeFade = pow(rangeFade, max(lightAttenuation.y, 0.001));

    if (lightType == HOURP_SHADOW_CAST_LIGHT_POINT)
    {
        return rangeFade;
    }

    float3 spotDirectionWS = normalize(_HoUrpShadowCastLightData2[lightIndex].xyz);
    float receiverCosAngle = dot(lightToReceiver * rsqrt(max(distanceSqr, 0.000001)), spotDirectionWS);
    float2 spotAttenuation = lightAttenuation.zw;
    float spotFade = saturate(receiverCosAngle * spotAttenuation.x + spotAttenuation.y);
    return rangeFade * spotFade * spotFade;
}

float HoUrpSampleShadowCastPunctual(float3 positionWS)
{
    if (_HoUrpShadowCastActive < 0.5)
    {
        return 1.0;
    }

    int lightCount = min(_HoUrpShadowCastLightCount, HOURP_SHADOW_CAST_MAX_LIGHTS);
    float attenuation = 1.0;
    for (int lightIndex = 0; lightIndex < lightCount; lightIndex++)
    {
        float4 lightData0 = _HoUrpShadowCastLightData0[lightIndex];
        int firstSlice = (int)round(lightData0.y);
        int sliceCount = min((int)round(lightData0.z), 6);
        float influence = HoUrpShadowCastLightInfluence(lightIndex, positionWS);
        float shadowStrength = saturate(lightData0.w * influence);
        if (shadowStrength <= 0.0)
        {
            continue;
        }

        float lightShadow = 1.0;
        if (lightData0.x == HOURP_SHADOW_CAST_LIGHT_POINT && sliceCount >= 6)
        {
            float3 lightToReceiver = positionWS - _HoUrpShadowCastLightData1[lightIndex].xyz;
            lightShadow = HoUrpShadowCastSampleSlice(firstSlice + HoUrpShadowCastPointFaceIndex(lightToReceiver), positionWS);
        }
        else
        {
            for (int sliceOffset = 0; sliceOffset < sliceCount; sliceOffset++)
            {
                lightShadow = min(lightShadow, HoUrpShadowCastSampleSlice(firstSlice + sliceOffset, positionWS));
            }
        }

        attenuation *= lerp(1.0, max(lightShadow, HOURP_SHADOW_CAST_MIN_ATTENUATION), shadowStrength);
    }

    return attenuation;
}

float HoUrpShadowCastSampleSecondDirectionalSlice(int sliceIndex, float3 positionWS)
{
    int sliceCount = min((int)round(_HoUrpShadowCastSecondDirectionalParams.w), HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_SLICES);
    if (sliceIndex < 0 || sliceIndex >= sliceCount)
    {
        return 1.0;
    }

    float4 shadowCoord = HoUrpShadowCastTransformSecondDirectional(sliceIndex, positionWS);
    if (abs(shadowCoord.w) <= 0.00001)
    {
        return 1.0;
    }

    shadowCoord.xyz /= shadowCoord.w;
    return HoUrpShadowCastSampleAtlas(shadowCoord.xyz, _HoUrpShadowCastSecondDirectionalSliceData[sliceIndex], _HoUrpShadowCastSecondDirectionalPcssParams.w, true);
}

float HoUrpSampleShadowCastSecondDirectional(float3 positionWS)
{
    if (_HoUrpShadowCastSecondDirectionalParams.x < 0.5)
    {
        return 1.0;
    }

    int lightCount = min((int)round(_HoUrpShadowCastSecondDirectionalParams.y), HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_LIGHTS);
    int cascadeCount = min((int)round(_HoUrpShadowCastSecondDirectionalParams.z), HOURP_SHADOW_CAST_MAX_SECOND_DIRECTIONAL_CASCADES);
    float attenuation = 1.0;
    float3 cameraToReceiver = positionWS - _HoUrpShadowCastSecondDirectionalCameraPosition.xyz;
    float distanceSqr = dot(cameraToReceiver, cameraToReceiver);

    for (int lightIndex = 0; lightIndex < lightCount; lightIndex++)
    {
        float4 lightData = _HoUrpShadowCastSecondDirectionalLightData[lightIndex];
        int firstSlice = (int)round(lightData.x);
        float shadowStrength = saturate(lightData.z);
        int cascadeIndex = cascadeCount - 1;
        for (int i = 0; i < cascadeCount; i++)
        {
            if (distanceSqr <= _HoUrpShadowCastSecondDirectionalSliceData[firstSlice + i].w)
            {
                cascadeIndex = i;
                break;
            }
        }

        float lightShadow = HoUrpShadowCastSampleSecondDirectionalSlice(firstSlice + cascadeIndex, positionWS);
        attenuation *= lerp(1.0, max(lightShadow, HOURP_SHADOW_CAST_MIN_ATTENUATION), shadowStrength);
    }

    return attenuation;
}

half HoUrpSampleShadowCastAttenuation(float3 positionWS, half3 normalWS)
{
    if (_HoUrpShadowCastActive < 0.5 && _HoUrpShadowCastSecondDirectionalParams.x < 0.5)
    {
        return 1.0h;
    }

    float attenuation = HoUrpSampleShadowCastPunctual(positionWS) * HoUrpSampleShadowCastSecondDirectional(positionWS);
    return half(lerp(1.0, attenuation, saturate(_HoUrpShadowReceiverStrength)));
}

#endif
