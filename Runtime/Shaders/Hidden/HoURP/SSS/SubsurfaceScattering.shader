Shader "Hidden/HoURP/SSS/SubsurfaceScattering"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "HoURP SSS Source Prepare"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSource

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/SSS/HoUrpSssFilter.hlsl"

            TEXTURE2D_X(_HoUrpAovMaskIdTexture);
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);
            TEXTURE2D_X(_HoUrpAovDiffuseTexture);

            half4 FragSource(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovMaskIdTexture, sampler_PointClamp, uv);
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half4 diffuse = SAMPLE_TEXTURE2D_X(_HoUrpAovDiffuseTexture, sampler_PointClamp, uv);

                half receivesSss = (half)HoUrpSssReceivesSss(maskId);
                half weight = (half)HoUrpSssSourceParticipation(maskId, normalDepth, surfaceData, 1.0);
                return half4(diffuse.rgb * receivesSss, weight);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HoURP SSS Profile Diffusion"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDiffusion

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/SSS/HoUrpSssFilter.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterSampling.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl"

            TEXTURE2D_X(_HoUrpSssSourceTexture);
            TEXTURE2D_X(_HoUrpSourceColorTexture);
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);

            float _HoUrpSssRadius;
            float _HoUrpSssStrength;
            float _HoUrpSssDepthTolerance;
            float _HoUrpSssNormalTolerance;
            float _HoUrpSssSourcePreserve;
            float4 _HoUrpSssProfileIds[8];
            float4 _HoUrpSssProfileDiffusionParams[8];
            float4 _HoUrpSssProfileShapeParams[8];

            half4 ProfileDiffusionParams(half profileByte)
            {
                half4 fallback = half4((half)_HoUrpSssRadius, saturate((half)_HoUrpSssSourcePreserve), 1.0h, 0.43h);
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    if (_HoUrpSssProfileIds[i].y > 0.5 && abs(_HoUrpSssProfileIds[i].x - profileByte) < 0.5)
                    {
                        return (half4)_HoUrpSssProfileDiffusionParams[i];
                    }
                }

                return fallback;
            }

            half4 ProfileShapeParams(half profileByte)
            {
                half4 fallback = half4(1.0h, 0.32h, 0.0h, 1.0h);
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    if (_HoUrpSssProfileIds[i].y > 0.5 && abs(_HoUrpSssProfileIds[i].x - profileByte) < 0.5)
                    {
                        return (half4)_HoUrpSssProfileShapeParams[i];
                    }
                }

                return fallback;
            }

            half SurfaceThinness(half4 surfaceData)
            {
                half profile = (half)HoUrpSssProfileByte(surfaceData);
                half thicknessScale = max(ProfileShapeParams(profile).x, 0.0h);
                return (half)HoUrpSssSurfaceThinness(surfaceData, thicknessScale);
            }

            half3 ProfileDiffusionColor(half profileByte)
            {
                half4 profileDiffusion = ProfileDiffusionParams(profileByte);
                half4 profileShape = ProfileShapeParams(profileByte);
                return max(half3(profileDiffusion.z, profileDiffusion.w, profileShape.y), half3(0.08h, 0.08h, 0.08h));
            }

            half SampleGate(float2 uv, half4 centerNormalDepth, half4 centerSurfaceData)
            {
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half centerThickness = SurfaceThinness(centerSurfaceData);

                half normalTolerance = (half)_HoUrpSssNormalTolerance;
                half depthTolerance = (half)_HoUrpSssDepthTolerance;
                half normalGate = (half)HoFilterNormalGate(
                    HoUrpSssDecodeNormal(normalDepth.rgb),
                    HoUrpSssDecodeNormal(centerNormalDepth.rgb),
                    normalTolerance);
                half depthGate = (half)HoFilterDepthGate(normalDepth.a, centerNormalDepth.a, depthTolerance);
                half thicknessGate = saturate(min(centerThickness, SurfaceThinness(surfaceData)) * 4.0h);
                return normalGate * depthGate * thicknessGate * (half)HoFilterByteProfileGate(
                    HoUrpSssProfileByte(surfaceData),
                    HoUrpSssProfileByte(centerSurfaceData));
            }

            half4 AccumulateSample(float2 uv, half3 weight, inout half totalWeight)
            {
                half4 source = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_LinearClamp, uv);
                half scalarWeight = max(max(weight.r, weight.g), weight.b);
                half sampleWeight = source.a * scalarWeight;
                totalWeight += sampleWeight;
                return half4(source.rgb * (half3)weight * source.a, sampleWeight);
            }

            half4 FragDiffusion(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 centerSource = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_PointClamp, uv);
                if (centerSource.a <= 0.0001h)
                {
                    return half4(0.0h, 0.0h, 0.0h, 0.0h);
                }

                half4 centerNormalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 centerSurface = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half centerThickness = SurfaceThinness(centerSurface);
                half centerProfileByte = (half)HoUrpSssProfileByte(centerSurface);
                half4 profileDiffusion = ProfileDiffusionParams(centerProfileByte);
                half4 profileShape = ProfileShapeParams(centerProfileByte);
                half3 profileColor = ProfileDiffusionColor(centerProfileByte);
                half profileAlpha = saturate(profileShape.w);
                float profileRadius = max(profileDiffusion.x, 0.0h) * max(0.0, _HoUrpSssRadius);
                float2 texel = rcp(max(_ScreenParams.xy, float2(1.0, 1.0))) * profileRadius * max(centerThickness, 0.05h);

                half totalWeight = 0.0h;
                half4 accum = AccumulateSample(
                    uv,
                    half3(1.5h, 1.5h, 1.5h),
                    totalWeight);

                float phase = HoFilterInterleavedNoise(uv, _ScreenParams.xy) * (2.0 * HOURP_FILTER_PI);
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float radius01;
                    float rcpPdf;
                    HoFilterBurleySampleDiffusionProfile(((float)i + 0.5) / 8.0, radius01, rcpPdf);
                    float2 sampleUv = uv + HoFilterGoldenAngleOffset(i, radius01, phase) * texel;
                    half gate = SampleGate(sampleUv, centerNormalDepth, centerSurface);
                    half3 weight = (half3)HoFilterBurleyProfileWeight(radius01, rcpPdf, profileColor) * gate;
                    accum += AccumulateSample(sampleUv, weight, totalWeight);
                }

                half3 diffused = accum.rgb / max(totalWeight, 0.0001h);
                half preserve = saturate(profileDiffusion.y);
                diffused = lerp(diffused, centerSource.rgb, preserve);
                diffused = lerp(centerSource.rgb, diffused, profileAlpha);
                half compositeWeight = saturate(centerSource.a * profileAlpha * (half)_HoUrpSssStrength);
                return half4(diffused, compositeWeight);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HoURP SSS Composite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl"

            TEXTURE2D_X(_HoUrpSourceColorTexture);
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);
            TEXTURE2D_X(_HoUrpSssSourceTexture);
            TEXTURE2D_X(_HoUrpSssDiffusionTexture);

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 color = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_LinearClamp, uv);
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half4 source = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_PointClamp, uv);
                half4 diffusion = SAMPLE_TEXTURE2D_X(_HoUrpSssDiffusionTexture, sampler_LinearClamp, uv);

                half validNormal = dot(abs(normalDepth.rgb), half3(1.0h, 1.0h, 1.0h)) > 0.0h ? 1.0h : 0.0h;
                half hasThickness = surfaceData.b > 0.0h ? 1.0h : 0.0h;
                half compositeWeight = saturate(diffusion.a * validNormal * hasThickness);
                if (compositeWeight <= 0.0001h)
                {
                    return color;
                }

                half3 sssColor = max(diffusion.rgb, half3(0.0h, 0.0h, 0.0h));
                half sourceLuma = saturate((half)HoFilterLuma(source.rgb));
                half3 liftedCamera = color.rgb + sssColor * (0.08h + 0.22h * (1.0h - sourceLuma));
                half3 target = lerp(sssColor, liftedCamera, 0.35h);
                color.rgb = lerp(color.rgb, target, compositeWeight);
                return color;
            }
            ENDHLSL
        }
    }
}
