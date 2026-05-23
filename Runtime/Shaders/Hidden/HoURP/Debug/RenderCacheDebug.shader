Shader "Hidden/HoURP/Debug/RenderCacheDebug"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "RenderCacheDebug"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture);

            int _HoUrpRenderCacheDebugMode;
            int _HoUrpRenderCacheDebugTileMode;
            float4 _HoUrpRenderCacheDebugTileRect;
            float4 _HoUrpRenderCacheDebugTileGrid;
            float4 _HoUrpRenderCacheDebugTileLabel0;
            float4 _HoUrpRenderCacheDebugTileLabel1;
            float4 _HoUrpRenderCacheDebugTileLabel2;
            float4 _HoUrpRenderCacheDebugTileLabel3;

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
                float2 tileMin = _HoUrpRenderCacheDebugTileRect.xy;
                float2 tileSize = _HoUrpRenderCacheDebugTileRect.zw;
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
                uint bit = 1u << (uint)clamp(bitIndex, 0, 31);
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

                if (_HoUrpRenderCacheDebugMode == 1)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugIdColor(maskId.g);
                }
                else if (_HoUrpRenderCacheDebugMode == 2)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(VisualizeLinearDepth01(normalDepth.a));
                }
                else if (_HoUrpRenderCacheDebugMode == 3)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    if (!HasValidNormal(normalDepth))
                    {
                        resolvedColor = half4(0.0h, 0.0h, 0.0h, 0.0h);
                    }
                    else
                    {
                        resolvedColor = half4(normalDepth.rgb, 1.0h);
                    }
                }
                else if (_HoUrpRenderCacheDebugMode >= 4 && _HoUrpRenderCacheDebugMode <= 7)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickChannel(objectCustom, _HoUrpRenderCacheDebugMode - 4));
                }
                else if (_HoUrpRenderCacheDebugMode >= 8 && _HoUrpRenderCacheDebugMode <= 11)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickChannel(objectCustom, _HoUrpRenderCacheDebugMode - 8));
                }
                else if (_HoUrpRenderCacheDebugMode >= 12 && _HoUrpRenderCacheDebugMode <= 15)
                {
                    half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    half value = PickChannel(surfaceData, _HoUrpRenderCacheDebugMode - 12);
                    if (_HoUrpRenderCacheDebugMode == 12 || _HoUrpRenderCacheDebugMode == 13)
                    {
                        resolvedColor = DebugIdColor(value);
                    }
                    else
                    {
                        if (_HoUrpRenderCacheDebugMode == 15)
                        {
                            value = saturate(value);
                        }

                        resolvedColor = DebugScalar(value);
                    }
                }
                else if (_HoUrpRenderCacheDebugMode >= 16 && _HoUrpRenderCacheDebugMode <= 19)
                {
                    half4 materialCustom = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickChannel(materialCustom, _HoUrpRenderCacheDebugMode - 16));
                }
                else if (_HoUrpRenderCacheDebugMode == 20)
                {
                    half4 diffuse = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = half4(diffuse.rgb, 1.0h);
                }
                else if (_HoUrpRenderCacheDebugMode == 21)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(sssSource.a);
                }
                else if (_HoUrpRenderCacheDebugMode == 22)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(sssSource.a);
                }
                else if (_HoUrpRenderCacheDebugMode == 23)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = half4(sssSource.rgb, 1.0h);
                }
                else if (_HoUrpRenderCacheDebugMode == 24)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = half4(sssDiffusion.rgb, 1.0h);
                }
                else if (_HoUrpRenderCacheDebugMode == 25)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(sssDiffusion.a);
                }
                else if (_HoUrpRenderCacheDebugMode == 26)
                {
                    half4 semanticPostMask = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(max(max(semanticPostMask.r, semanticPostMask.g), max(semanticPostMask.b, semanticPostMask.a)));
                }
                else if (_HoUrpRenderCacheDebugMode >= 27 && _HoUrpRenderCacheDebugMode <= 34)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickFlagBit(maskId.a, _HoUrpRenderCacheDebugMode - 27));
                }
                else if (_HoUrpRenderCacheDebugMode >= 35 && _HoUrpRenderCacheDebugMode <= 39)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(PickFlagBit(maskId.b, _HoUrpRenderCacheDebugMode - 32));
                }
                else if (_HoUrpRenderCacheDebugMode == 40)
                {
                    half4 accumulation = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = half4(saturate(accumulation.rgb), 1.0h);
                }
                else if (_HoUrpRenderCacheDebugMode == 41)
                {
                    half4 revealage = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    resolvedColor = DebugScalar(revealage.r);
                }
                else if (_HoUrpRenderCacheDebugMode == 42 || _HoUrpRenderCacheDebugMode == 43)
                {
                    half4 shadowDepth = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
#if UNITY_REVERSED_Z
                    half visibleDepth = 1.0h - shadowDepth.r;
#else
                    half visibleDepth = shadowDepth.r;
#endif
                    resolvedColor = DebugScalar(visibleDepth);
                }
                else
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
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
            Name "RenderCacheDebugTile"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertTile
            #pragma fragment FragTile

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture);

            int _HoUrpRenderCacheDebugMode;
            int _HoUrpRenderCacheDebugTileMode;
            float4 _HoUrpRenderCacheDebugTileRect;
            float4 _HoUrpRenderCacheDebugTileGrid;
            float4 _HoUrpRenderCacheDebugTileLabel0;
            float4 _HoUrpRenderCacheDebugTileLabel1;
            float4 _HoUrpRenderCacheDebugTileLabel2;
            float4 _HoUrpRenderCacheDebugTileLabel3;

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
                float2 tileMin = _HoUrpRenderCacheDebugTileRect.xy;
                float2 tileSize = _HoUrpRenderCacheDebugTileRect.zw;
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
                uint bit = 1u << (uint)clamp(bitIndex, 0, 31);
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

            uint PickVectorChar(float4 chars, int index)
            {
                if (index == 0) return (uint)round(chars.x);
                if (index == 1) return (uint)round(chars.y);
                if (index == 2) return (uint)round(chars.z);
                return (uint)round(chars.w);
            }

            uint PickPackedLabelChar(int index, float4 c0, float4 c1, float4 c2, float4 c3)
            {
                if (index < 4) return PickVectorChar(c0, index);
                if (index < 8) return PickVectorChar(c1, index - 4);
                if (index < 12) return PickVectorChar(c2, index - 8);
                return PickVectorChar(c3, index - 12);
            }

            uint LabelChar(int index)
            {
                return PickPackedLabelChar(
                    index,
                    _HoUrpRenderCacheDebugTileLabel0,
                    _HoUrpRenderCacheDebugTileLabel1,
                    _HoUrpRenderCacheDebugTileLabel2,
                    _HoUrpRenderCacheDebugTileLabel3);
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

            half DrawLabel(float2 uv)
            {
                float density = max(_HoUrpRenderCacheDebugTileGrid.x, _HoUrpRenderCacheDebugTileGrid.y);
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
                if (charIndex < 0 || charIndex >= 16 || col < 0 || col >= 5 || row < 0 || row >= 7)
                {
                    return 0.0h;
                }

                uint rowBits = GlyphRow(LabelChar(charIndex), (uint)row);
                return half((rowBits >> (uint)(4 - col)) & 1u);
            }

            half4 ApplyTileOverlay(half4 color, float2 uv)
            {
                half outerBorder = half(step(uv.x, 0.018) + step(uv.y, 0.018) + step(0.982, uv.x) + step(0.982, uv.y));
                if (outerBorder > 0.0h)
                {
                    return half4(0.02h, 0.02h, 0.02h, 1.0h);
                }

                half innerBorder = half(step(uv.x, 0.026) + step(uv.y, 0.026) + step(0.974, uv.x) + step(0.974, uv.y));
                if (innerBorder > 0.0h)
                {
                    return half4(0.92h, 0.92h, 0.86h, 1.0h);
                }

                float density = max(_HoUrpRenderCacheDebugTileGrid.x, _HoUrpRenderCacheDebugTileGrid.y);
                float labelScale = saturate((density - 2.0) / 4.0);
                float labelHeight = lerp(0.11, 0.18, labelScale);
                half labelBackground = half(step(uv.y, 0.985) * step(1.0 - labelHeight, uv.y) * step(uv.x, 0.52));
                half label = DrawLabel(uv);
                color.rgb = lerp(color.rgb, color.rgb * 0.2h, labelBackground);
                color.rgb = lerp(color.rgb, half3(1.0h, 1.0h, 1.0h), label);
                color.a = 1.0h;
                return color;
            }

            half4 FragTile(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                if (_HoUrpRenderCacheDebugMode == 1)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugIdColor(maskId.g), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 2)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(VisualizeLinearDepth01(normalDepth.a)), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 3)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    if (!HasValidNormal(normalDepth))
                    {
                        return ApplyTileOverlay(half4(0.0h, 0.0h, 0.0h, 0.0h), uv);
                    }

                    return ApplyTileOverlay(half4(normalDepth.rgb, 1.0h), uv);
                }

                if (_HoUrpRenderCacheDebugMode >= 4 && _HoUrpRenderCacheDebugMode <= 7)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickChannel(objectCustom, _HoUrpRenderCacheDebugMode - 4)), uv);
                }

                if (_HoUrpRenderCacheDebugMode >= 8 && _HoUrpRenderCacheDebugMode <= 11)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickChannel(objectCustom, _HoUrpRenderCacheDebugMode - 8)), uv);
                }

                if (_HoUrpRenderCacheDebugMode >= 12 && _HoUrpRenderCacheDebugMode <= 15)
                {
                    half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    half value = PickChannel(surfaceData, _HoUrpRenderCacheDebugMode - 12);
                    if (_HoUrpRenderCacheDebugMode == 12 || _HoUrpRenderCacheDebugMode == 13)
                    {
                        return ApplyTileOverlay(DebugIdColor(value), uv);
                    }

                    if (_HoUrpRenderCacheDebugMode == 15)
                    {
                        value = saturate(value);
                    }

                    return ApplyTileOverlay(DebugScalar(value), uv);
                }

                if (_HoUrpRenderCacheDebugMode >= 16 && _HoUrpRenderCacheDebugMode <= 19)
                {
                    half4 materialCustom = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickChannel(materialCustom, _HoUrpRenderCacheDebugMode - 16)), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 20)
                {
                    half4 diffuse = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(half4(diffuse.rgb, 1.0h), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 21)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(sssSource.a), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 22)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(sssSource.a), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 23)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(half4(sssSource.rgb, 1.0h), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 24)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(half4(sssDiffusion.rgb, 1.0h), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 25)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(sssDiffusion.a), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 26)
                {
                    half4 semanticPostMask = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(max(max(semanticPostMask.r, semanticPostMask.g), max(semanticPostMask.b, semanticPostMask.a))), uv);
                }

                if (_HoUrpRenderCacheDebugMode >= 27 && _HoUrpRenderCacheDebugMode <= 34)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickFlagBit(maskId.a, _HoUrpRenderCacheDebugMode - 27)), uv);
                }

                if (_HoUrpRenderCacheDebugMode >= 35 && _HoUrpRenderCacheDebugMode <= 39)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(PickFlagBit(maskId.b, _HoUrpRenderCacheDebugMode - 32)), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 40)
                {
                    half4 accumulation = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(half4(saturate(accumulation.rgb), 1.0h), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 41)
                {
                    half4 revealage = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                    return ApplyTileOverlay(DebugScalar(revealage.r), uv);
                }

                if (_HoUrpRenderCacheDebugMode == 42 || _HoUrpRenderCacheDebugMode == 43)
                {
                    half4 shadowDepth = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
#if UNITY_REVERSED_Z
                    half visibleDepth = 1.0h - shadowDepth.r;
#else
                    half visibleDepth = shadowDepth.r;
#endif
                    return ApplyTileOverlay(DebugScalar(visibleDepth), uv);
                }

                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpRenderCacheDebugSourceTexture, sampler_PointClamp, uv);
                return ApplyTileOverlay(DebugScalar(maskId.r), uv);
            }
            ENDHLSL
        }
    }
}
