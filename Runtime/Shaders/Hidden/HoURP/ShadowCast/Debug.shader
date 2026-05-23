Shader "Hidden/HoURP/ShadowCast/Debug"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ShadowCast Debug"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X_FLOAT(_BlitTexture);
            float4 _BlitScaleBias;
            float _HoUrpShadowCastDebugMode;
            int _HoUrpShadowCastSliceCount;
            float4 _HoUrpShadowCastAtlasSize;
            float4 _HoUrpShadowCastSliceData[28];
            float4 _HoUrpShadowCastSecondDirectionalParams;
            float4 _HoUrpShadowCastSecondDirectionalAtlasSize;
            float4 _HoUrpShadowCastSecondDirectionalLightData[4];
            float4 _HoUrpShadowCastSecondDirectionalSliceData[16];

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
                return output;
            }

            float HoUrpShadowCastRectLine(float2 uv, float4 rect, float lineUv)
            {
                float2 rectMin = rect.xy;
                float2 rectMax = rect.xy + rect.zz;
                if (any(uv < rectMin) || any(uv > rectMax))
                {
                    return 0.0;
                }

                float2 distanceToEdge = min(uv - rectMin, rectMax - uv);
                return 1.0 - step(lineUv, min(distanceToEdge.x, distanceToEdge.y));
            }

            float HoUrpShadowCastSecondDirectionalBlockLine(float2 uv, int firstSlice, int sliceCount, float lineUv)
            {
                if (sliceCount <= 0)
                {
                    return 0.0;
                }

                float2 blockMin = float2(1.0, 1.0);
                float2 blockMax = float2(0.0, 0.0);
                [unroll]
                for (int sliceOffset = 0; sliceOffset < 4; sliceOffset++)
                {
                    if (sliceOffset >= sliceCount)
                    {
                        break;
                    }

                    int sliceIndex = firstSlice + sliceOffset;
                    if (sliceIndex < 0 || sliceIndex >= 16)
                    {
                        continue;
                    }

                    float4 slice = _HoUrpShadowCastSecondDirectionalSliceData[sliceIndex];
                    if (slice.z <= 0.0)
                    {
                        continue;
                    }

                    blockMin = min(blockMin, slice.xy);
                    blockMax = max(blockMax, slice.xy + slice.zz);
                }

                if (any(blockMax <= blockMin))
                {
                    return 0.0;
                }

                return HoUrpShadowCastRectLine(uv, float4(blockMin, max(blockMax - blockMin, 0.0)), lineUv);
            }

            half3 HoUrpShadowCastApplySliceOverlay(float2 uv, half3 color)
            {
                float atlasTexel = max(_HoUrpShadowCastAtlasSize.z, _HoUrpShadowCastAtlasSize.w);
                float lineUv = max(atlasTexel * 2.0, 0.001);
                int sliceCount = min(_HoUrpShadowCastSliceCount, 28);
                float sliceLine = 0.0;
                [loop]
                for (int i = 0; i < 28; i++)
                {
                    if (i >= sliceCount)
                    {
                        break;
                    }

                    sliceLine = max(sliceLine, HoUrpShadowCastRectLine(uv, _HoUrpShadowCastSliceData[i], lineUv));
                }

                return lerp(color, half3(0.0h, 0.95h, 1.0h), saturate(sliceLine * 0.85));
            }

            half3 HoUrpShadowCastApplySecondDirectionalOverlay(float2 uv, half3 color)
            {
                float atlasTexel = max(_HoUrpShadowCastSecondDirectionalAtlasSize.z, _HoUrpShadowCastSecondDirectionalAtlasSize.w);
                float cascadeLineUv = max(atlasTexel * 2.0, 0.001);
                float blockLineUv = max(atlasTexel * 4.0, 0.0015);
                int sliceCount = min((int)round(_HoUrpShadowCastSecondDirectionalParams.w), 16);
                int lightCount = min((int)round(_HoUrpShadowCastSecondDirectionalParams.y), 4);
                float cascadeLine = 0.0;
                float blockLine = 0.0;

                [loop]
                for (int i = 0; i < 16; i++)
                {
                    if (i >= sliceCount)
                    {
                        break;
                    }

                    cascadeLine = max(cascadeLine, HoUrpShadowCastRectLine(uv, _HoUrpShadowCastSecondDirectionalSliceData[i], cascadeLineUv));
                }

                [loop]
                for (int lightIndex = 0; lightIndex < 4; lightIndex++)
                {
                    if (lightIndex >= lightCount)
                    {
                        break;
                    }

                    int firstSlice = (int)round(_HoUrpShadowCastSecondDirectionalLightData[lightIndex].x);
                    int perLightSliceCount = min((int)round(_HoUrpShadowCastSecondDirectionalLightData[lightIndex].y), 4);
                    blockLine = max(blockLine, HoUrpShadowCastSecondDirectionalBlockLine(uv, firstSlice, perLightSliceCount, blockLineUv));
                }

                color = lerp(color, half3(1.0h, 0.52h, 0.12h), saturate(cascadeLine * 0.7));
                return lerp(color, half3(1.0h, 0.95h, 0.05h), saturate(blockLine));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float depth = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, input.uv, 0).r;
#if UNITY_REVERSED_Z
                float visibleDepth = 1.0 - depth;
#else
                float visibleDepth = depth;
#endif
                half3 color = half3(visibleDepth, visibleDepth, visibleDepth);
                color = _HoUrpShadowCastDebugMode > 1.5
                    ? HoUrpShadowCastApplySecondDirectionalOverlay(input.uv, color)
                    : HoUrpShadowCastApplySliceOverlay(input.uv, color);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
