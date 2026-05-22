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
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);
            TEXTURE2D_X(_HoUrpAovSssSourceTexture);

            half4 FragSource(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovMaskIdTexture, sampler_PointClamp, uv);
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovSssSourceTexture, sampler_PointClamp, uv);

                half validNormal = dot(abs(normalDepth.rgb), half3(1.0h, 1.0h, 1.0h)) > 0.0h ? 1.0h : 0.0h;
                half thicknessGate = saturate(surfaceData.b * 4.0h);
                half weight = saturate(sssSource.a * maskId.r * validNormal * thicknessGate);
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

            half3 DecodeNormal(half3 encodedNormal)
            {
                return normalize(encodedNormal * 2.0h - 1.0h);
            }

            half ProfileByte(half4 surfaceData)
            {
                return round(saturate(surfaceData.g) * 255.0h);
            }

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
                half profile = ProfileByte(surfaceData);
                half thicknessScale = max(ProfileShapeParams(profile).x, 0.0h);
                return saturate(surfaceData.b * thicknessScale);
            }

            half ProfileGate(half4 sampleSurfaceData, half4 centerSurfaceData)
            {
                half sampleProfile = ProfileByte(sampleSurfaceData);
                half centerProfile = ProfileByte(centerSurfaceData);
                return 1.0h - step(0.5h, abs(sampleProfile - centerProfile));
            }

            half3 ApplySssTint(half3 sceneColor, half4 source, half profileByte)
            {
                half4 profileDiffusion = ProfileDiffusionParams(profileByte);
                half4 profileShape = ProfileShapeParams(profileByte);
                half3 tint = max(source.rgb * half3(profileDiffusion.z, profileDiffusion.w, profileShape.y), half3(0.0h, 0.0h, 0.0h));
                half tintAlpha = saturate(profileShape.w);
                half3 softTint = sceneColor * (0.55h + tint * 0.9h);
                half luma = dot(sceneColor, half3(0.2126h, 0.7152h, 0.0722h));
                softTint += tint * (0.08h + 0.12h * (1.0h - saturate(luma))) * source.a * tintAlpha;
                return max(softTint, 0.0h);
            }

            half SampleGate(float2 uv, half4 centerNormalDepth, half4 centerSurfaceData)
            {
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half centerThickness = SurfaceThinness(centerSurfaceData);

                half normalTolerance = (half)_HoUrpSssNormalTolerance;
                half depthTolerance = (half)_HoUrpSssDepthTolerance;
                half normalGate = saturate((dot(DecodeNormal(centerNormalDepth.rgb), DecodeNormal(normalDepth.rgb)) - normalTolerance) / max(0.0001h, 1.0h - normalTolerance));
                half depthGate = saturate(1.0h - abs(normalDepth.a - centerNormalDepth.a) / max(0.0001h, depthTolerance));
                half thicknessGate = saturate(min(centerThickness, SurfaceThinness(surfaceData)) * 4.0h);
                return normalGate * depthGate * thicknessGate * ProfileGate(surfaceData, centerSurfaceData);
            }

            half4 AccumulateSample(float2 uv, half weight, half centerProfileByte, inout half totalWeight)
            {
                half4 source = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_LinearClamp, uv);
                half3 sceneColor = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_LinearClamp, uv).rgb;
                half sampleWeight = source.a * weight;
                totalWeight += weight;
                return half4(ApplySssTint(sceneColor, source, centerProfileByte) * sampleWeight, sampleWeight);
            }

            half4 FragDiffusion(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 centerSource = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_PointClamp, uv);
                half3 centerSceneColor = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_PointClamp, uv).rgb;
                half4 centerNormalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 centerSurface = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half centerThickness = SurfaceThinness(centerSurface);
                half centerProfileByte = ProfileByte(centerSurface);
                half4 profileDiffusion = ProfileDiffusionParams(centerProfileByte);
                float profileRadius = max(profileDiffusion.x, 0.0h) * max(0.0, _HoUrpSssRadius) / max(0.0001, 2.0);
                float2 texel = rcp(max(_ScreenParams.xy, float2(1.0, 1.0))) * profileRadius * max(centerThickness, 0.0h);

                half totalWeight = 0.0h;
                half4 accum = AccumulateSample(uv, 2.0h, centerProfileByte, totalWeight);

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
                    half gate = SampleGate(sampleUv, centerNormalDepth, centerSurface);
                    accum += AccumulateSample(sampleUv, gate, centerProfileByte, totalWeight);
                }

                half alpha = saturate(accum.a / max(totalWeight, 0.0001h));
                half3 diffused = accum.rgb / max(accum.a, 0.0001h);
                half3 centerTinted = ApplySssTint(centerSceneColor, centerSource, centerProfileByte);
                half preserve = saturate(profileDiffusion.y);
                diffused = lerp(diffused, centerTinted, preserve);
                half compositeWeight = saturate(alpha * (half)_HoUrpSssStrength);
                return half4(diffused, compositeWeight);
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
                half hasThickness = surfaceData.b > 0.0h ? 1.0h : 0.0h;
                half compositeWeight = saturate(diffusion.a * validNormal * hasThickness);
                if (compositeWeight <= 0.0001h)
                {
                    return color;
                }

                color.rgb = lerp(color.rgb, diffusion.rgb, compositeWeight);
                return color;
            }
            ENDHLSL
        }
    }
}
