Shader "Hidden/HoURP/Filter/Blur"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "HoURP Filter Copy"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCopy

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoFilterSourceTex);

            half4 FragCopy(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X(_HoFilterSourceTex, sampler_LinearClamp, input.texcoord);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HoURP Filter Separable Blur"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSeparableBlur

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterSampling.hlsl"

            TEXTURE2D_X(_HoFilterSourceTex);
            float4 _HoFilterParams0;
            float4 _HoFilterDirection;

            half4 SampleSource(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_HoFilterSourceTex, sampler_LinearClamp, uv);
            }

            half4 FragSeparableBlur(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 texelSize = HoFilterGetTexelSize(_ScreenParams.xy);
                float radius = max(_HoFilterParams0.x, 0.0);
                int sampleCount = clamp((int)round(_HoFilterParams0.y), 1, 13);

                half4 accum = SampleSource(uv);
                half weightSum = 1.0h;

                [loop]
                for (int i = 1; i <= 6; i++)
                {
                    if (i * 2 + 1 > sampleCount)
                    {
                        break;
                    }

                    float sampleRadius = radius * ((float)i / 6.0);
                    float2 offset = HoFilterAxisOffset(texelSize, _HoFilterDirection.xy, sampleRadius, 1);
                    half weight = (half)exp(-sampleRadius * sampleRadius * 0.5);
                    accum += SampleSource(uv + offset) * weight;
                    accum += SampleSource(uv - offset) * weight;
                    weightSum += weight * 2.0h;
                }

                return accum / max(weightSum, 0.0001h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HoURP Filter Depth Normal Aware Blur"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDepthNormalAwareBlur

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterSampling.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl"

            TEXTURE2D_X(_HoFilterSourceTex);
            TEXTURE2D_X(_HoFilterGuideDepthTex);
            TEXTURE2D_X(_HoFilterGuideNormalTex);
            float4 _HoFilterParams0;
            float4 _HoFilterDirection;
            float _HoFilterDebugMode;

            float3 DecodeGuideNormal(float3 encodedNormal)
            {
                return HoFilterSafeNormalize(encodedNormal * 2.0 - 1.0, float3(0.0, 0.0, 1.0));
            }

            half4 FragDepthNormalAwareBlur(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 texelSize = HoFilterGetTexelSize(_ScreenParams.xy);
                float radius = max(_HoFilterParams0.x, 0.0);
                int sampleCount = clamp((int)round(_HoFilterParams0.y), 1, 9);
                float depthTolerance = max(_HoFilterParams0.z, 1.0e-5);
                float normalTolerance = saturate(_HoFilterParams0.w);

                float centerDepth = SAMPLE_TEXTURE2D_X(_HoFilterGuideDepthTex, sampler_PointClamp, uv).r;
                float3 centerNormal = DecodeGuideNormal(SAMPLE_TEXTURE2D_X(_HoFilterGuideNormalTex, sampler_PointClamp, uv).rgb);
                half4 accum = SAMPLE_TEXTURE2D_X(_HoFilterSourceTex, sampler_LinearClamp, uv);
                half weightSum = 1.0h;
                half gateSum = 1.0h;

                [loop]
                for (int i = 1; i <= 4; i++)
                {
                    if (i * 2 + 1 > sampleCount)
                    {
                        break;
                    }

                    float sampleRadius = radius * ((float)i / 4.0);
                    float2 offset = HoFilterAxisOffset(texelSize, _HoFilterDirection.xy, sampleRadius, 1);

                    [unroll]
                    for (int side = 0; side < 2; side++)
                    {
                        float2 sampleUv = uv + offset * (side == 0 ? 1.0 : -1.0);
                        float sampleDepth = SAMPLE_TEXTURE2D_X(_HoFilterGuideDepthTex, sampler_PointClamp, sampleUv).r;
                        float3 sampleNormal = DecodeGuideNormal(SAMPLE_TEXTURE2D_X(_HoFilterGuideNormalTex, sampler_PointClamp, sampleUv).rgb);
                        half gate = (half)(HoFilterDepthGate(sampleDepth, centerDepth, depthTolerance) * HoFilterNormalGate(sampleNormal, centerNormal, normalTolerance));
                        half weight = (half)exp(-sampleRadius * sampleRadius * 0.5) * gate;
                        accum += SAMPLE_TEXTURE2D_X(_HoFilterSourceTex, sampler_LinearClamp, sampleUv) * weight;
                        weightSum += weight;
                        gateSum += gate;
                    }
                }

                if (_HoFilterDebugMode > 0.5)
                {
                    return HoFilterEncodeDebugWeight(gateSum / max((half)sampleCount, 1.0h));
                }

                return accum / max(weightSum, 0.0001h);
            }
            ENDHLSL
        }
    }
}
