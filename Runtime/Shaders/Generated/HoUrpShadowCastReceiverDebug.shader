Shader "HoURP/Generated/HoUrpShadowCastReceiverDebug"
{
    Properties
    {
        _HoUrpBaseColor("Base Color", Color) = (1, 1, 1, 1)
        _HoUrpShadowReceiverDebugMode("Debug Mode", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half4 _HoUrpBaseColor;
            float _HoUrpShadowReceiverDebugMode;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                return output;
            }

            half3 RampAttenuation(float value)
            {
                value = saturate(value);
                return lerp(half3(1.0h, 0.05h, 0.0h), half3(0.0h, 0.85h, 1.0h), half(value));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half punctual = half(HoUrpSampleShadowCastPunctual(input.positionWS));
                half secondDirectional = half(HoUrpSampleShadowCastSecondDirectional(input.positionWS));
                half combined = HoUrpSampleShadowCastAttenuation(input.positionWS, normalize(input.normalWS));
                half mode = half(_HoUrpShadowReceiverDebugMode);

                half3 color;
                if (mode < 0.5h)
                {
                    color = RampAttenuation(combined);
                }
                else if (mode < 1.5h)
                {
                    color = RampAttenuation(punctual);
                }
                else if (mode < 2.5h)
                {
                    color = RampAttenuation(secondDirectional);
                }
                else
                {
                    color = half3(1.0h - punctual, 1.0h - secondDirectional, 1.0h - combined);
                }

                return half4(color * _HoUrpBaseColor.rgb, _HoUrpBaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VertShadow
            #pragma fragment FragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertShadow(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 FragShadow(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
