Shader "Hidden/HoURP/SSS/SubsurfaceScattering"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SssSource"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSource

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpAovMaskIdTexture);
            TEXTURE2D_X(_HoUrpAovSssSourceTexture);

            float _HoUrpSssStrength;

            half4 FragSource(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovMaskIdTexture, sampler_PointClamp, uv);
                half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovSssSourceTexture, sampler_PointClamp, uv);

                half weight = saturate(sssSource.a * maskId.r * (half)_HoUrpSssStrength);
                return half4(sssSource.rgb, weight);
            }
            ENDHLSL
        }

        Pass
        {
            Name "SssDiffusion"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDiffusion

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpSssSourceTexture);

            float _HoUrpSssRadius;
            float _HoUrpSssSourcePreserve;

            half4 AccumulateSample(float2 uv, half weight, inout half totalWeight)
            {
                half4 source = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_LinearClamp, uv);
                totalWeight += weight;
                return half4(source.rgb * source.a * weight, source.a * weight);
            }

            half4 FragDiffusion(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 centerSource = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_PointClamp, uv);
                float2 texel = rcp(max(_ScreenParams.xy, float2(1.0, 1.0))) * max(0.0, _HoUrpSssRadius);

                half totalWeight = 0.0h;
                half4 accum = AccumulateSample(uv, 2.0h, totalWeight);

                float2 offsets[8] =
                {
                    float2(1.0, 0.0),
                    float2(-1.0, 0.0),
                    float2(0.0, 1.0),
                    float2(0.0, -1.0),
                    float2(0.7071, 0.7071),
                    float2(-0.7071, 0.7071),
                    float2(0.7071, -0.7071),
                    float2(-0.7071, -0.7071)
                };

                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float2 sampleUv = uv + offsets[i] * texel;
                    accum += AccumulateSample(sampleUv, 1.0h, totalWeight);
                }

                half invWeight = rcp(max(totalWeight, 0.0001h));
                half alpha = saturate(accum.a * invWeight);
                half3 diffused = accum.rgb / max(accum.a, 0.0001h);
                diffused = lerp(diffused, centerSource.rgb, saturate(_HoUrpSssSourcePreserve));
                return half4(diffused, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "SssComposite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpSourceColorTexture);
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);
            TEXTURE2D_X(_HoUrpSssDiffusionTexture);

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 color = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_LinearClamp, uv);
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half4 diffusion = SAMPLE_TEXTURE2D_X(_HoUrpSssDiffusionTexture, sampler_LinearClamp, uv);

                half validNormal = dot(abs(normalDepth.rgb), half3(1.0h, 1.0h, 1.0h)) > 0.0h ? 1.0h : 0.0h;
                half compositeWeight = saturate(diffusion.a * surfaceData.b * validNormal);
                color.rgb = lerp(color.rgb, diffusion.rgb, compositeWeight);
                return color;
            }
            ENDHLSL
        }
    }
}
