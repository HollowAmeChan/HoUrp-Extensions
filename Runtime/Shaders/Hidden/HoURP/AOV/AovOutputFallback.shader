Shader "Hidden/HoURP/AOV/AovOutputFallback"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "HoUrpAovOutput"
            Tags { "LightMode" = "HoUrpAovOutput" }

            Cull Back
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 depthZW : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct AovOutput
            {
                half4 maskId : SV_Target0;
                half4 normalDepth : SV_Target1;
                half4 objectCustom0 : SV_Target2;
                half4 objectCustom1 : SV_Target3;
                half4 surfaceData : SV_Target4;
                half4 materialCustom0 : SV_Target5;
                half4 sssSource : SV_Target6;
            };

            float _HoUrpAovMaskWeight;
            float _HoUrpObjectId;
            float _HoUrpObjectGroupId;
            float _HoUrpObjectFlags;
            float _HoUrpObjectCustomMask;
            float _HoUrpMaterialClass;
            float _HoUrpMaterialSssProfile;
            float _HoUrpMaterialThickness;
            float _HoUrpMaterialCurvature;
            float4 _HoUrpMaterialCustom0_3;
            float4 _HoUrpSssSourceColor;
            float _HoUrpSssWeight;

            half HasMaskBit(float mask, float bitValue)
            {
                return half(step(0.5, fmod(floor(mask / bitValue), 2.0)));
            }

            bool HasRendererSemanticV1(uint packedValue)
            {
                return (packedValue & 0x80000000u) != 0u && (((packedValue >> 29u) & 3u) == 1u);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.depthZW = positionInputs.positionCS.zw;
                return output;
            }

            AovOutput Frag(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.normalWS);
                float rawDepth = input.depthZW.x / max(input.depthZW.y, 1.0e-6);
                float linear01Depth = Linear01Depth(rawDepth, _ZBufferParams);

                AovOutput output;
                half3 encodedNormal = half3(normalWS * 0.5 + 0.5);
                uint rendererStaticSemantic = unity_RendererUserValue;
                bool hasRendererStaticSemanticV1 = HasRendererSemanticV1(rendererStaticSemantic);
                float objectCustomMask = round(clamp(_HoUrpObjectCustomMask, 0.0, 255.0));
                float objectId = _HoUrpObjectId;
                float objectGroupId = floor(clamp(_HoUrpObjectGroupId, 0.0, 7.0));
                float objectFeatureFlags = floor(clamp(_HoUrpObjectFlags, 0.0, 8191.0));
                float objectFlags = fmod(objectFeatureFlags, 256.0);
                float objectFeatureFlagsHigh = floor(objectFeatureFlags / 256.0);
                float objectGroupAndHighFlags = objectGroupId + objectFeatureFlagsHigh * 8.0;
                if (hasRendererStaticSemanticV1)
                {
                    uint featureFlags = ((rendererStaticSemantic >> 8u) & 8191u) & ~1u;
                    objectCustomMask = float(rendererStaticSemantic & 255u);
                    objectFlags = float(featureFlags & 255u);
                    objectFeatureFlagsHigh = float((featureFlags >> 8u) & 31u);
                    objectId = float((rendererStaticSemantic >> 21u) & 15u);
                    objectGroupId = float((rendererStaticSemantic >> 25u) & 7u);
                    objectGroupAndHighFlags = objectGroupId + objectFeatureFlagsHigh * 8.0;
                }

                half maskWeight = half(saturate(_HoUrpAovMaskWeight));
                output.maskId = half4(
                    maskWeight,
                    half(saturate(objectId / 255.0)),
                    half(saturate(objectGroupAndHighFlags / 255.0)),
                    half(saturate(objectFlags / 255.0)));
                output.normalDepth = half4(encodedNormal, half(saturate(linear01Depth)));
                output.objectCustom0 = half4(
                    HasMaskBit(objectCustomMask, 1.0),
                    HasMaskBit(objectCustomMask, 2.0),
                    HasMaskBit(objectCustomMask, 4.0),
                    HasMaskBit(objectCustomMask, 8.0)) * maskWeight;
                output.objectCustom1 = half4(
                    HasMaskBit(objectCustomMask, 16.0),
                    HasMaskBit(objectCustomMask, 32.0),
                    HasMaskBit(objectCustomMask, 64.0),
                    HasMaskBit(objectCustomMask, 128.0)) * maskWeight;
                output.surfaceData = half4(
                    half(saturate(_HoUrpMaterialClass / 255.0)),
                    half(saturate(_HoUrpMaterialSssProfile / 255.0)),
                    half(saturate(_HoUrpMaterialThickness)),
                    half(saturate(_HoUrpMaterialCurvature * 0.5 + 0.5))) * maskWeight;
                output.materialCustom0 = half4(saturate(_HoUrpMaterialCustom0_3)) * maskWeight;
                half sssWeight = half(saturate(_HoUrpSssWeight)) * maskWeight;
                output.sssSource = half4(half3(max(_HoUrpSssSourceColor.rgb, 0.0)) * maskWeight, sssWeight);
                return output;
            }
            ENDHLSL
        }
    }
}
