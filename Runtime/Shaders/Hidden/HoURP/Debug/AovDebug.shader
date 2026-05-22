Shader "Hidden/HoURP/Debug/AovDebug"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "AovDebug"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpAovDebugSourceTexture);

            int _HoUrpAovDebugMode;
            int _HoUrpAovDebugTileMode;
            float4 _HoUrpAovDebugTileRect;
            float4 _HoUrpAovDebugTileGrid;

            Varyings VertTile(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float2 quadUv[6] =
                {
                    float2(0.0, 0.0),
                    float2(0.0, 1.0),
                    float2(1.0, 1.0),
                    float2(0.0, 0.0),
                    float2(1.0, 1.0),
                    float2(1.0, 0.0)
                };

                float2 uv = quadUv[input.vertexID];
                float2 tileMin = _HoUrpAovDebugTileRect.xy;
                float2 tileSize = _HoUrpAovDebugTileRect.zw;
                float2 position = tileMin + uv * tileSize;

                output.positionCS = float4(position * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
                output.texcoord = uv;
                return output;
            }

            half4 DebugScalar(half value)
            {
                return half4(value, value, value, 1.0h);
            }

            half4 DebugIdColor(half encodedId)
            {
                uint id = (uint)round(saturate(encodedId) * 255.0h);
                if (id == 0u)
                {
                    return half4(0.0h, 0.0h, 0.0h, 1.0h);
                }

                uint hash = id * 747796405u + 2891336453u;
                hash = ((hash >> ((hash >> 28u) + 4u)) ^ hash) * 277803737u;
                hash = (hash >> 22u) ^ hash;
                half3 color = half3(
                    half((float)(hash & 255u) / 255.0),
                    half((float)((hash >> 8u) & 255u) / 255.0),
                    half((float)((hash >> 16u) & 255u) / 255.0));
                return half4(saturate(color * 0.75h + 0.25h), 1.0h);
            }

            half PickChannel(half4 values, int channel)
            {
                if (channel == 1)
                {
                    return values.g;
                }

                if (channel == 2)
                {
                    return values.b;
                }

                if (channel == 3)
                {
                    return values.a;
                }

                return values.r;
            }

            half PickFlagBit(half encodedFlags, int bitIndex)
            {
                uint flags = (uint)round(saturate(encodedFlags) * 255.0h);
                uint bit = 1u << (uint)clamp(bitIndex, 0, 7);
                return (flags & bit) != 0u ? 1.0h : 0.0h;
            }

            half VisualizeLinearDepth01(half linearDepth)
            {
                return saturate(sqrt(saturate(linearDepth)));
            }

            bool HasValidNormal(half4 normalDepth)
            {
                return dot(abs(normalDepth.rgb), half3(1.0h, 1.0h, 1.0h)) > 0.0h;
            }

            half4 ResolveDebugColor(float2 uv)
            {
                half4 resolvedColor = half4(0.0h, 0.0h, 0.0h, 1.0h);

                if (_HoUrpAovDebugMode == 1)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugIdColor(maskId.g);
                }
                else if (_HoUrpAovDebugMode == 2)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(VisualizeLinearDepth01(normalDepth.a));
                }
                else if (_HoUrpAovDebugMode == 3)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    if (!HasValidNormal(normalDepth))
                    {
                        resolvedColor = half4(0.0h, 0.0h, 0.0h, 0.0h);
                    }
                    else
                    {
                        resolvedColor = half4(normalDepth.rgb, 1.0h);
                    }
                }
                else if (_HoUrpAovDebugMode >= 4 && _HoUrpAovDebugMode <= 7)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickChannel(objectCustom, _HoUrpAovDebugMode - 4));
                }
                else if (_HoUrpAovDebugMode >= 8 && _HoUrpAovDebugMode <= 11)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickChannel(objectCustom, _HoUrpAovDebugMode - 8));
                }
                else if (_HoUrpAovDebugMode >= 12 && _HoUrpAovDebugMode <= 15)
                {
                    half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    half value = PickChannel(surfaceData, _HoUrpAovDebugMode - 12);
                    if (_HoUrpAovDebugMode == 12 || _HoUrpAovDebugMode == 13)
                    {
                        resolvedColor = DebugIdColor(value);
                    }
                    else
                    {
                        if (_HoUrpAovDebugMode == 15)
                        {
                            value = saturate(value);
                        }

                        resolvedColor = DebugScalar(value);
                    }
                }
                else if (_HoUrpAovDebugMode >= 16 && _HoUrpAovDebugMode <= 19)
                {
                    half4 materialCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickChannel(materialCustom, _HoUrpAovDebugMode - 16));
                }
                else if (_HoUrpAovDebugMode == 20)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = half4(sssSource.rgb, 1.0h);
                }
                else if (_HoUrpAovDebugMode == 21)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(sssSource.a);
                }
                else if (_HoUrpAovDebugMode == 22)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(sssSource.a);
                }
                else if (_HoUrpAovDebugMode == 23)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = half4(sssSource.rgb, 1.0h);
                }
                else if (_HoUrpAovDebugMode == 24)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = half4(sssDiffusion.rgb, 1.0h);
                }
                else if (_HoUrpAovDebugMode == 25)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(sssDiffusion.a);
                }
                else if (_HoUrpAovDebugMode == 26)
                {
                    half4 semanticPostMask = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(max(max(semanticPostMask.r, semanticPostMask.g), max(semanticPostMask.b, semanticPostMask.a)));
                }
                else if (_HoUrpAovDebugMode >= 27 && _HoUrpAovDebugMode <= 34)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickFlagBit(maskId.a, _HoUrpAovDebugMode - 27));
                }
                else
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(maskId.r);
                }

                return resolvedColor;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return ResolveDebugColor(input.texcoord);
            }

            half4 FragTile(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return ResolveDebugColor(input.texcoord);
            }
            ENDHLSL
        }

        Pass
        {
            Name "AovDebugTile"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertTile
            #pragma fragment FragTile

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpAovDebugSourceTexture);

            int _HoUrpAovDebugMode;
            int _HoUrpAovDebugTileMode;
            float4 _HoUrpAovDebugTileRect;
            float4 _HoUrpAovDebugTileGrid;

            Varyings VertTile(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float2 quadUv[6] =
                {
                    float2(0.0, 0.0),
                    float2(0.0, 1.0),
                    float2(1.0, 1.0),
                    float2(0.0, 0.0),
                    float2(1.0, 1.0),
                    float2(1.0, 0.0)
                };

                float2 uv = quadUv[input.vertexID];
                float2 tileMin = _HoUrpAovDebugTileRect.xy;
                float2 tileSize = _HoUrpAovDebugTileRect.zw;
                float2 position = tileMin + uv * tileSize;

                output.positionCS = float4(position * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
                output.texcoord = uv;
                return output;
            }

            half4 DebugScalar(half value)
            {
                return half4(value, value, value, 1.0h);
            }

            half4 DebugIdColor(half encodedId)
            {
                uint id = (uint)round(saturate(encodedId) * 255.0h);
                if (id == 0u)
                {
                    return half4(0.0h, 0.0h, 0.0h, 1.0h);
                }

                uint hash = id * 747796405u + 2891336453u;
                hash = ((hash >> ((hash >> 28u) + 4u)) ^ hash) * 277803737u;
                hash = (hash >> 22u) ^ hash;
                half3 color = half3(
                    half((float)(hash & 255u) / 255.0),
                    half((float)((hash >> 8u) & 255u) / 255.0),
                    half((float)((hash >> 16u) & 255u) / 255.0));
                return half4(saturate(color * 0.75h + 0.25h), 1.0h);
            }

            half PickChannel(half4 values, int channel)
            {
                if (channel == 1)
                {
                    return values.g;
                }

                if (channel == 2)
                {
                    return values.b;
                }

                if (channel == 3)
                {
                    return values.a;
                }

                return values.r;
            }

            half PickFlagBit(half encodedFlags, int bitIndex)
            {
                uint flags = (uint)round(saturate(encodedFlags) * 255.0h);
                uint bit = 1u << (uint)clamp(bitIndex, 0, 7);
                return (flags & bit) != 0u ? 1.0h : 0.0h;
            }

            half VisualizeLinearDepth01(half linearDepth)
            {
                return saturate(sqrt(saturate(linearDepth)));
            }

            bool HasValidNormal(half4 normalDepth)
            {
                return dot(abs(normalDepth.rgb), half3(1.0h, 1.0h, 1.0h)) > 0.0h;
            }

            uint PickLabelChar(int index, uint c0, uint c1, uint c2, uint c3, uint c4, uint c5, uint c6, uint c7, uint c8, uint c9)
            {
                if (index == 0) return c0;
                if (index == 1) return c1;
                if (index == 2) return c2;
                if (index == 3) return c3;
                if (index == 4) return c4;
                if (index == 5) return c5;
                if (index == 6) return c6;
                if (index == 7) return c7;
                if (index == 8) return c8;
                if (index == 9) return c9;
                return 32u;
            }

            uint LabelChar(int mode, int index)
            {
                if (mode == 0) return PickLabelChar(index, 65u, 79u, 86u, 32u, 77u, 65u, 83u, 75u, 32u, 32u);
                if (mode == 1) return PickLabelChar(index, 79u, 66u, 74u, 69u, 67u, 84u, 32u, 73u, 68u, 32u);
                if (mode == 2) return PickLabelChar(index, 68u, 69u, 80u, 84u, 72u, 32u, 32u, 32u, 32u, 32u);
                if (mode == 3) return PickLabelChar(index, 78u, 79u, 82u, 77u, 65u, 76u, 32u, 32u, 32u, 32u);
                if (mode == 4) return PickLabelChar(index, 83u, 85u, 66u, 74u, 69u, 67u, 84u, 32u, 32u, 32u);
                if (mode == 5) return PickLabelChar(index, 70u, 65u, 67u, 69u, 32u, 32u, 32u, 32u, 32u, 32u);
                if (mode == 6) return PickLabelChar(index, 72u, 65u, 73u, 82u, 32u, 32u, 32u, 32u, 32u, 32u);
                if (mode == 7) return PickLabelChar(index, 69u, 89u, 69u, 32u, 32u, 32u, 32u, 32u, 32u, 32u);
                if (mode == 8) return PickLabelChar(index, 65u, 67u, 67u, 69u, 83u, 83u, 32u, 32u, 32u, 32u);
                if (mode == 9) return PickLabelChar(index, 67u, 76u, 79u, 84u, 72u, 32u, 32u, 32u, 32u, 32u);
                if (mode == 10) return PickLabelChar(index, 80u, 82u, 79u, 80u, 32u, 32u, 32u, 32u, 32u, 32u);
                if (mode == 11) return PickLabelChar(index, 82u, 69u, 83u, 69u, 82u, 86u, 69u, 68u, 32u, 32u);
                if (mode == 12) return PickLabelChar(index, 77u, 65u, 84u, 32u, 67u, 76u, 65u, 83u, 83u, 32u);
                if (mode == 13) return PickLabelChar(index, 83u, 83u, 83u, 32u, 80u, 82u, 79u, 70u, 32u, 32u);
                if (mode == 14) return PickLabelChar(index, 84u, 72u, 73u, 67u, 75u, 32u, 32u, 32u, 32u, 32u);
                if (mode == 15) return PickLabelChar(index, 67u, 85u, 82u, 86u, 69u, 32u, 32u, 32u, 32u, 32u);
                if (mode >= 16 && mode <= 19) return PickLabelChar(index, 77u, 65u, 84u, 32u, 67u, uint(48 + mode - 16), 32u, 32u, 32u, 32u);
                if (mode == 20) return PickLabelChar(index, 83u, 83u, 83u, 32u, 83u, 82u, 67u, 32u, 32u, 32u);
                if (mode == 21) return PickLabelChar(index, 83u, 83u, 83u, 32u, 87u, 71u, 84u, 32u, 32u, 32u);
                if (mode == 22) return PickLabelChar(index, 83u, 83u, 83u, 32u, 77u, 65u, 83u, 75u, 32u, 32u);
                if (mode == 23) return PickLabelChar(index, 83u, 83u, 83u, 32u, 83u, 82u, 67u, 50u, 32u, 32u);
                if (mode == 24) return PickLabelChar(index, 83u, 83u, 83u, 32u, 68u, 73u, 70u, 70u, 32u, 32u);
                if (mode == 25) return PickLabelChar(index, 83u, 83u, 83u, 32u, 67u, 77u, 80u, 32u, 87u, 32u);
                if (mode == 26) return PickLabelChar(index, 80u, 79u, 83u, 84u, 32u, 77u, 65u, 83u, 75u, 32u);
                if (mode == 27) return PickLabelChar(index, 80u, 79u, 83u, 84u, 32u, 82u, 88u, 32u, 32u, 32u);
                if (mode >= 28 && mode <= 34) return PickLabelChar(index, 70u, 76u, 65u, 71u, 32u, uint(48 + mode - 27), 32u, 32u, 32u, 32u);
                return 32u;
            }

            uint GlyphRow(uint c, uint row)
            {
                if (c == 32u) return 0u;
                if (c == 48u) { if (row == 0u) return 14u; if (row == 1u) return 17u; if (row == 2u) return 19u; if (row == 3u) return 21u; if (row == 4u) return 25u; if (row == 5u) return 17u; return 14u; }
                if (c == 49u) { if (row == 0u) return 4u; if (row == 1u) return 12u; if (row == 6u) return 14u; return 4u; }
                if (c == 50u) { if (row == 0u) return 14u; if (row == 1u) return 17u; if (row == 2u) return 1u; if (row == 3u) return 2u; if (row == 4u) return 4u; if (row == 5u) return 8u; return 31u; }
                if (c == 51u) { if (row == 0u) return 30u; if (row == 3u) return 14u; if (row == 6u) return 30u; return 1u; }
                if (c == 52u) { if (row == 0u) return 2u; if (row == 1u) return 6u; if (row == 2u) return 10u; if (row == 3u) return 18u; if (row == 4u) return 31u; return 2u; }
                if (c == 53u) { if (row == 0u) return 31u; if (row == 1u || row == 2u) return 16u; if (row == 3u) return 30u; if (row == 4u || row == 5u) return 1u; return 30u; }
                if (c == 54u) { if (row == 0u) return 14u; if (row == 1u || row == 2u) return 16u; if (row == 3u) return 30u; if (row == 4u || row == 5u) return 17u; return 14u; }
                if (c == 55u) { if (row == 0u) return 31u; if (row == 1u) return 1u; if (row == 2u) return 2u; if (row == 3u) return 4u; return 8u; }
                if (c == 56u) { if (row == 0u || row == 3u || row == 6u) return 14u; return 17u; }
                if (c == 57u) { if (row == 0u) return 14u; if (row == 1u || row == 2u) return 17u; if (row == 3u) return 15u; if (row == 4u || row == 5u) return 1u; return 14u; }
                if (c == 65u) { if (row == 0u) return 14u; if (row == 3u) return 31u; return 17u; }
                if (c == 66u) { if (row == 0u || row == 3u || row == 6u) return 30u; return 17u; }
                if (c == 67u) { if (row == 0u || row == 6u) return 15u; return 16u; }
                if (c == 68u) { if (row == 0u || row == 6u) return 30u; return 17u; }
                if (c == 69u) { if (row == 0u || row == 6u) return 31u; if (row == 3u) return 30u; return 16u; }
                if (c == 70u) { if (row == 0u) return 31u; if (row == 3u) return 30u; return 16u; }
                if (c == 71u) { if (row == 0u || row == 6u) return 15u; if (row == 3u) return 23u; if (row >= 4u) return 17u; return 16u; }
                if (c == 72u) { if (row == 3u) return 31u; return 17u; }
                if (c == 73u) { if (row == 0u || row == 6u) return 31u; return 4u; }
                if (c == 74u) { if (row == 0u) return 7u; if (row == 5u) return 18u; if (row == 6u) return 12u; return 2u; }
                if (c == 75u) { if (row == 0u || row == 6u) return 17u; if (row == 1u || row == 5u) return 18u; if (row == 2u || row == 4u) return 20u; return 24u; }
                if (c == 76u) { if (row == 6u) return 31u; return 16u; }
                if (c == 77u) { if (row == 1u) return 27u; if (row == 2u || row == 3u) return 21u; return 17u; }
                if (c == 78u) { if (row == 1u) return 25u; if (row == 2u) return 21u; if (row == 3u) return 19u; return 17u; }
                if (c == 79u) { if (row == 0u || row == 6u) return 14u; return 17u; }
                if (c == 80u) { if (row == 0u || row == 3u) return 30u; if (row == 1u || row == 2u) return 17u; return 16u; }
                if (c == 82u) { if (row == 0u || row == 3u) return 30u; if (row == 1u || row == 2u) return 17u; if (row == 4u) return 20u; if (row == 5u) return 18u; return 17u; }
                if (c == 83u) { if (row == 0u) return 15u; if (row == 1u || row == 2u) return 16u; if (row == 3u) return 14u; if (row == 4u || row == 5u) return 1u; return 30u; }
                if (c == 84u) { if (row == 0u) return 31u; return 4u; }
                if (c == 85u) { if (row == 6u) return 14u; return 17u; }
                if (c == 86u) { if (row <= 4u) return 17u; if (row == 5u) return 10u; return 4u; }
                if (c == 87u) { if (row == 6u) return 10u; if (row >= 3u) return 21u; return 17u; }
                if (c == 88u) { if (row == 0u || row == 6u) return 17u; if (row == 1u || row == 5u) return 10u; if (row == 2u || row == 4u) return 4u; return 4u; }
                if (c == 89u) { if (row <= 2u) return 17u; if (row == 3u) return 10u; return 4u; }
                return 0u;
            }

            half DrawLabel(float2 uv, int mode)
            {
                float density = max(_HoUrpAovDebugTileGrid.x, _HoUrpAovDebugTileGrid.y);
                float labelScale = saturate((density - 2.0) / 4.0);
                float cellHeight = lerp(0.085, 0.14, labelScale);
                float cellWidth = cellHeight * 0.42;
                float2 textOrigin = float2(0.02, 0.98);
                float xCell = (uv.x - textOrigin.x) / cellWidth;
                float yCell = (textOrigin.y - uv.y) / cellHeight;
                if (xCell < 0.0 || yCell < 0.0 || yCell >= 1.0)
                {
                    return 0.0h;
                }

                int charIndex = (int)floor(xCell);
                int col = (int)floor(frac(xCell) * 6.0);
                int row = (int)floor(yCell * 8.0);
                if (charIndex < 0 || charIndex >= 10 || col < 0 || col >= 5 || row < 0 || row >= 7)
                {
                    return 0.0h;
                }

                uint rowBits = GlyphRow(LabelChar(mode, charIndex), (uint)row);
                return half((rowBits >> (uint)(4 - col)) & 1u);
            }

            half4 ApplyTileOverlay(half4 color, float2 uv)
            {
                half border = half(step(uv.x, 0.015) + step(uv.y, 0.015) + step(0.985, uv.x) + step(0.985, uv.y));
                if (border > 0.0h)
                {
                    return half4(1.0h, 0.0h, 0.0h, 1.0h);
                }

                float density = max(_HoUrpAovDebugTileGrid.x, _HoUrpAovDebugTileGrid.y);
                float labelScale = saturate((density - 2.0) / 4.0);
                float labelHeight = lerp(0.11, 0.18, labelScale);
                half labelBackground = half(step(uv.y, 0.985) * step(1.0 - labelHeight, uv.y) * step(uv.x, 0.52));
                half label = DrawLabel(uv, _HoUrpAovDebugMode);
                color.rgb = lerp(color.rgb, color.rgb * 0.2h, labelBackground);
                color.rgb = lerp(color.rgb, half3(1.0h, 1.0h, 1.0h), label);
                color.a = 1.0h;
                return color;
            }

            half4 FragTile(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                if (_HoUrpAovDebugMode == 1)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugIdColor(maskId.g), uv);
                }

                if (_HoUrpAovDebugMode == 2)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(VisualizeLinearDepth01(normalDepth.a)), uv);
                }

                if (_HoUrpAovDebugMode == 3)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    if (!HasValidNormal(normalDepth))
                    {
                        return ApplyTileOverlay(half4(0.0h, 0.0h, 0.0h, 0.0h), uv);
                    }

                    return ApplyTileOverlay(half4(normalDepth.rgb, 1.0h), uv);
                }

                if (_HoUrpAovDebugMode >= 4 && _HoUrpAovDebugMode <= 7)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickChannel(objectCustom, _HoUrpAovDebugMode - 4)), uv);
                }

                if (_HoUrpAovDebugMode >= 8 && _HoUrpAovDebugMode <= 11)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickChannel(objectCustom, _HoUrpAovDebugMode - 8)), uv);
                }

                if (_HoUrpAovDebugMode >= 12 && _HoUrpAovDebugMode <= 15)
                {
                    half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    half value = PickChannel(surfaceData, _HoUrpAovDebugMode - 12);
                    if (_HoUrpAovDebugMode == 12 || _HoUrpAovDebugMode == 13)
                    {
                        return ApplyTileOverlay(DebugIdColor(value), uv);
                    }

                    if (_HoUrpAovDebugMode == 15)
                    {
                        value = saturate(value);
                    }

                    return ApplyTileOverlay(DebugScalar(value), uv);
                }

                if (_HoUrpAovDebugMode >= 16 && _HoUrpAovDebugMode <= 19)
                {
                    half4 materialCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickChannel(materialCustom, _HoUrpAovDebugMode - 16)), uv);
                }

                if (_HoUrpAovDebugMode == 20)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(half4(sssSource.rgb, 1.0h), uv);
                }

                if (_HoUrpAovDebugMode == 21)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(sssSource.a), uv);
                }

                if (_HoUrpAovDebugMode == 22)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(sssSource.a), uv);
                }

                if (_HoUrpAovDebugMode == 23)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(half4(sssSource.rgb, 1.0h), uv);
                }

                if (_HoUrpAovDebugMode == 24)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(half4(sssDiffusion.rgb, 1.0h), uv);
                }

                if (_HoUrpAovDebugMode == 25)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(sssDiffusion.a), uv);
                }

                if (_HoUrpAovDebugMode == 26)
                {
                    half4 semanticPostMask = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(max(max(semanticPostMask.r, semanticPostMask.g), max(semanticPostMask.b, semanticPostMask.a))), uv);
                }

                if (_HoUrpAovDebugMode >= 27 && _HoUrpAovDebugMode <= 34)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickFlagBit(maskId.a, _HoUrpAovDebugMode - 27)), uv);
                }

                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                return ApplyTileOverlay(DebugScalar(maskId.r), uv);
            }
            ENDHLSL
        }
    }
}
