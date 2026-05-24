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
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);

            float4 _HoUrpSssParams; // x strength, y global radius, z sample budget, w reserved
            float _HoUrpSssDepthTolerance;
            float _HoUrpSssNormalTolerance;
            float _HoUrpSssSourcePreserve;
            float4 _HoUrpSssProfileIds[8];
            float4 _HoUrpSssProfileDiffusionParams[8];
            float4 _HoUrpSssProfileShapeParams[8];

            half4 ProfileDiffusionParams(half profileByte)
            {
                half4 fallback = half4((half)_HoUrpSssParams.y, saturate((half)_HoUrpSssSourcePreserve), 1.0h, 0.43h);
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
                half profileByte = (half)HoUrpSssProfileByte(surfaceData);
                half thicknessScale = max(ProfileShapeParams(profileByte).x, 0.0h);
                return (half)HoUrpSssSurfaceThinness(surfaceData, thicknessScale);
            }

            half SampleGate(float2 uv, half4 centerNormalDepth, half4 centerSurfaceData)
            {
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half4 source = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_LinearClamp, uv);

                half normalGate = (half)HoFilterNormalGate(
                    HoUrpSssDecodeNormal(normalDepth.rgb),
                    HoUrpSssDecodeNormal(centerNormalDepth.rgb),
                    (half)_HoUrpSssNormalTolerance);
                half depthGate = (half)HoFilterDepthGate(
                    normalDepth.a,
                    centerNormalDepth.a,
                    max((half)_HoUrpSssDepthTolerance, 0.0001h));
                half profileGate = (half)HoFilterByteProfileGate(
                    HoUrpSssProfileByte(surfaceData),
                    HoUrpSssProfileByte(centerSurfaceData));

                return source.a * normalGate * depthGate * profileGate;
            }

            half3 ProfileDiffusionColor(half profileByte)
            {
                half4 profileDiffusion = ProfileDiffusionParams(profileByte);
                half4 profileShape = ProfileShapeParams(profileByte);
                return max(half3(profileDiffusion.z, profileDiffusion.w, profileShape.y), half3(0.08h, 0.08h, 0.08h));
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
                half profileByte = (half)HoUrpSssProfileByte(centerSurface);
                half4 profileDiffusion = ProfileDiffusionParams(profileByte);
                half4 profileShape = ProfileShapeParams(profileByte);
                half profileAlpha = saturate(profileShape.w);
                half preserve = saturate(profileDiffusion.y);
                half thickness = max(SurfaceThinness(centerSurface), 0.05h);
                half3 diffusionColor = ProfileDiffusionColor(profileByte);

                float globalRadiusScale = max(_HoUrpSssParams.y, 0.0) / 8.0;
                float radiusPx = max((float)profileDiffusion.x * globalRadiusScale * (float)thickness, 0.0);
                if (radiusPx <= 0.0001)
                {
                    return half4(centerSource.rgb, centerSource.a * profileAlpha);
                }

                int sampleCount = clamp((int)round(_HoUrpSssParams.z), 1, 24);
                float2 texelRadius = rcp(max(_ScreenParams.xy, float2(1.0, 1.0))) * radiusPx;
                float phase = HoFilterInterleavedNoise(uv, _ScreenParams.xy) * (2.0 * HOURP_FILTER_PI);

                half3 centerWeight = (half3)HoFilterBurleyProfileWeight(0.0, 1.0, diffusionColor) * 0.35h;
                half3 irradianceSum = centerSource.rgb * centerWeight * centerSource.a;
                half3 weightSum = centerWeight * centerSource.a;
                half alphaSum = centerSource.a;
                half alphaWeightSum = 1.0h;

                [loop]
                for (int i = 0; i < 24; i++)
                {
                    if (i >= sampleCount)
                    {
                        break;
                    }

                    float radius01;
                    float rcpPdf;
                    HoFilterBurleySampleDiffusionProfile(((float)i + 0.5) / (float)sampleCount, radius01, rcpPdf);
                    float2 sampleUv = uv + HoFilterGoldenAngleOffset(i, radius01, phase) * texelRadius;
                    half gate = SampleGate(sampleUv, centerNormalDepth, centerSurface);
                    half4 sampleSource = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_LinearClamp, sampleUv);
                    half3 weight = (half3)HoFilterBurleyProfileWeight(radius01, rcpPdf, diffusionColor) * gate;

                    irradianceSum += sampleSource.rgb * weight;
                    weightSum += weight;
                    alphaSum += sampleSource.a * gate;
                    alphaWeightSum += gate;
                }

                half3 diffused = irradianceSum / max(weightSum, half3(0.0001h, 0.0001h, 0.0001h));
                diffused = lerp(diffused, centerSource.rgb, preserve);
                diffused = lerp(centerSource.rgb, diffused, profileAlpha);
                half diffusedMask = saturate(alphaSum / max(alphaWeightSum, 0.0001h));
                half compositeWeight = saturate(centerSource.a * diffusedMask * profileAlpha * (half)_HoUrpSssParams.x);
                return half4(max(diffused, half3(0.0h, 0.0h, 0.0h)), compositeWeight);
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
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/SSS/HoUrpSssFilter.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl"

            TEXTURE2D_X(_HoUrpSourceColorTexture);
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);
            TEXTURE2D_X(_HoUrpSssSourceTexture);
            TEXTURE2D_X(_HoUrpSssDiffusionTexture);

            float _HoUrpSssDebugMode;
            float4 _HoUrpSssParams;
            float _HoUrpSssSourcePreserve;
            float4 _HoUrpSssProfileIds[8];
            float4 _HoUrpSssProfileDiffusionParams[8];
            float4 _HoUrpSssProfileShapeParams[8];

            half4 ProfileDiffusionParamsComposite(half profileByte)
            {
                half4 fallback = half4((half)_HoUrpSssParams.y, saturate((half)_HoUrpSssSourcePreserve), 1.0h, 0.43h);
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

            half4 ProfileShapeParamsComposite(half profileByte)
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

            half SurfaceThinnessComposite(half4 surfaceData)
            {
                half profileByte = (half)HoUrpSssProfileByte(surfaceData);
                half thicknessScale = max(ProfileShapeParamsComposite(profileByte).x, 0.0h);
                return (half)HoUrpSssSurfaceThinness(surfaceData, thicknessScale);
            }

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 color = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_LinearClamp, uv);
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half4 source = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_PointClamp, uv);
                half4 diffusion = SAMPLE_TEXTURE2D_X(_HoUrpSssDiffusionTexture, sampler_LinearClamp, uv);

                half profileByte = (half)HoUrpSssProfileByte(surfaceData);
                half4 profileDiffusion = ProfileDiffusionParamsComposite(profileByte);
                half validNormal = dot(abs(normalDepth.rgb), half3(1.0h, 1.0h, 1.0h)) > 0.0h ? 1.0h : 0.0h;
                half thickness = SurfaceThinnessComposite(surfaceData);
                half hasThickness = thickness > 0.0h ? 1.0h : 0.0h;
                half centerMask = saturate(source.a * validNormal * hasThickness);
                half compositeWeight = saturate(diffusion.a * centerMask);

                int debugMode = (int)round(_HoUrpSssDebugMode);
                if (debugMode == 1)
                {
                    return HoFilterEncodeDebugWeight(centerMask);
                }

                if (debugMode == 2)
                {
                    return half4(source.rgb, 1.0h);
                }

                if (debugMode == 3)
                {
                    return half4(diffusion.rgb, 1.0h);
                }

                if (debugMode == 4)
                {
                    return HoFilterEncodeDebugWeight(compositeWeight);
                }

                if (debugMode == 5)
                {
                    return half4((profileByte / 255.0h).xxx, 1.0h);
                }

                if (debugMode == 6)
                {
                    return HoFilterEncodeDebugWeight(thickness);
                }

                if (debugMode == 7)
                {
                    return HoFilterEncodeDebugWeight(saturate(profileDiffusion.x / 32.0h));
                }

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
