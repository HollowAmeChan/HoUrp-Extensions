using HoUrp.Extensions.Core;
using UnityEngine.Rendering;

namespace HoUrp.Extensions.ShadowCast
{
    public static class HoShadowCastShaderConstants
    {
        public const int MaxDirectionalLights = 4;
        public const int MaxSpotLights = 4;
        public const int MaxPointLights = 4;
        public const int MaxLights = MaxSpotLights + MaxPointLights;
        public const int MaxShadowSlices = MaxSpotLights + MaxPointLights * 6;
        public const int MaxSecondDirectionalCascades = 4;
        public const int MaxSecondDirectionalSlices = MaxDirectionalLights * MaxSecondDirectionalCascades;

        public const string AtlasTextureName = "_HoUrpShadowCastAtlas";
        public const string SecondDirectionalAtlasTextureName = "_HoUrpShadowCastSecondDirectionalAtlas";
        public const string CastingPunctualKeywordName = "_HOURP_SHADOW_CASTING_PUNCTUAL";

        public static readonly ShaderTagId ShadowCasterShaderTagId = new ShaderTagId("ShadowCaster");
        public static readonly GlobalKeyword CastingPunctualLightShadowKeyword = GlobalKeyword.Create(CastingPunctualKeywordName);

        public static readonly int AtlasTextureId = HoUrpShaderPropertyIds.ShadowCastAtlas;
        public static readonly int SecondDirectionalAtlasTextureId = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalAtlas;
        public static readonly int ActiveId = HoUrpShaderPropertyIds.ShadowCastActive;
        public static readonly int LightCountId = HoUrpShaderPropertyIds.ShadowCastLightCount;
        public static readonly int SliceCountId = HoUrpShaderPropertyIds.ShadowCastSliceCount;
        public static readonly int AtlasSizeId = HoUrpShaderPropertyIds.ShadowCastAtlasSize;
        public static readonly int WorldToShadowRow0Id = HoUrpShaderPropertyIds.ShadowCastWorldToShadowRow0;
        public static readonly int WorldToShadowRow1Id = HoUrpShaderPropertyIds.ShadowCastWorldToShadowRow1;
        public static readonly int WorldToShadowRow2Id = HoUrpShaderPropertyIds.ShadowCastWorldToShadowRow2;
        public static readonly int WorldToShadowRow3Id = HoUrpShaderPropertyIds.ShadowCastWorldToShadowRow3;
        public static readonly int LightData0Id = HoUrpShaderPropertyIds.ShadowCastLightData0;
        public static readonly int LightData1Id = HoUrpShaderPropertyIds.ShadowCastLightData1;
        public static readonly int LightData2Id = HoUrpShaderPropertyIds.ShadowCastLightData2;
        public static readonly int LightAttenuationId = HoUrpShaderPropertyIds.ShadowCastLightAttenuation;
        public static readonly int LightColorId = HoUrpShaderPropertyIds.ShadowCastLightColor;
        public static readonly int SliceDataId = HoUrpShaderPropertyIds.ShadowCastSliceData;
        public static readonly int PcssParamsId = HoUrpShaderPropertyIds.ShadowCastPcssParams;
        public static readonly int PcssParams2Id = HoUrpShaderPropertyIds.ShadowCastPcssParams2;
        public static readonly int SecondDirectionalParamsId = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalParams;
        public static readonly int SecondDirectionalCameraPositionId = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalCameraPosition;
        public static readonly int SecondDirectionalAtlasSizeId = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalAtlasSize;
        public static readonly int SecondDirectionalPcssParamsId = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalPcssParams;
        public static readonly int SecondDirectionalWorldToShadowRow0Id = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalWorldToShadowRow0;
        public static readonly int SecondDirectionalWorldToShadowRow1Id = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalWorldToShadowRow1;
        public static readonly int SecondDirectionalWorldToShadowRow2Id = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalWorldToShadowRow2;
        public static readonly int SecondDirectionalWorldToShadowRow3Id = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalWorldToShadowRow3;
        public static readonly int SecondDirectionalLightDataId = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalLightData;
        public static readonly int SecondDirectionalSliceDataId = HoUrpShaderPropertyIds.ShadowCastSecondDirectionalSliceData;
        public static readonly int DebugModeId = HoUrpShaderPropertyIds.ShadowCastDebugMode;
        public static readonly int ReceiverStrengthId = HoUrpShaderPropertyIds.ShadowCastReceiverStrength;

        public static readonly int ShadowBiasId = UnityEngine.Shader.PropertyToID("_ShadowBias");
        public static readonly int LightDirectionId = UnityEngine.Shader.PropertyToID("_LightDirection");
        public static readonly int LightPositionId = UnityEngine.Shader.PropertyToID("_LightPosition");
        public static readonly int WorldSpaceCameraPosId = UnityEngine.Shader.PropertyToID("_WorldSpaceCameraPos");
        public static readonly int WorldToCameraMatrixId = UnityEngine.Shader.PropertyToID("unity_WorldToCamera");
        public static readonly int CameraToWorldMatrixId = UnityEngine.Shader.PropertyToID("unity_CameraToWorld");
    }
}
