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
            #include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpObjectSemantic.hlsl"

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
                half4 diffuse : SV_Target6;
            };

            float _HoUrpMaterialClass;
            float _HoUrpMaterialSssProfile;
            float _HoUrpMaterialThickness;
            float _HoUrpMaterialCurvature;
            float4 _HoUrpMaterialCustom0_3;
            float4 _HoUrpSssSourceColor;

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
                HoUrpObjectSemanticData objectSemantic = HoUrpResolveObjectSemanticData();
                half maskWeight = objectSemantic.maskWeight;
                half semanticGate = maskWeight > 0.0h ? 1.0h : 0.0h;
                output.maskId = HoUrpEncodeObjectMaskId(objectSemantic);
                output.normalDepth = half4(encodedNormal, half(saturate(linear01Depth)));
                output.objectCustom0 = HoUrpEncodeObjectCustom0_3(objectSemantic);
                output.objectCustom1 = HoUrpEncodeObjectCustom4_7(objectSemantic);
                output.surfaceData = half4(
                    half(saturate(_HoUrpMaterialClass / 255.0)),
                    half(saturate(_HoUrpMaterialSssProfile / 255.0)),
                    half(saturate(_HoUrpMaterialThickness)),
                    half(saturate(_HoUrpMaterialCurvature * 0.5 + 0.5))) * semanticGate;
                output.materialCustom0 = half4(saturate(_HoUrpMaterialCustom0_3)) * semanticGate;
                output.diffuse = half4(max(half3(_HoUrpSssSourceColor.rgb), half3(0.0h, 0.0h, 0.0h)) * maskWeight, 1.0h);
                return output;
            }
            ENDHLSL
        }
    }
}
