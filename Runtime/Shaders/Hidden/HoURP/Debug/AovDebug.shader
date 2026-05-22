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
                if (_HoUrpAovDebugMode == 1)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugIdColor(maskId.g);
                }

                if (_HoUrpAovDebugMode == 2)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugScalar(VisualizeLinearDepth01(normalDepth.a));
                }

                if (_HoUrpAovDebugMode == 3)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    if (!HasValidNormal(normalDepth))
                    {
                        return half4(0.0h, 0.0h, 0.0h, 0.0h);
                    }

                    return half4(normalDepth.rgb, 1.0h);
                }

                if (_HoUrpAovDebugMode >= 4 && _HoUrpAovDebugMode <= 7)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugScalar(PickChannel(objectCustom, _HoUrpAovDebugMode - 4));
                }

                if (_HoUrpAovDebugMode >= 8 && _HoUrpAovDebugMode <= 11)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugScalar(PickChannel(objectCustom, _HoUrpAovDebugMode - 8));
                }

                if (_HoUrpAovDebugMode >= 12 && _HoUrpAovDebugMode <= 15)
                {
                    half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    half value = PickChannel(surfaceData, _HoUrpAovDebugMode - 12);
                    if (_HoUrpAovDebugMode == 12 || _HoUrpAovDebugMode == 13)
                    {
                        return DebugIdColor(value);
                    }

                    if (_HoUrpAovDebugMode == 15)
                    {
                        value = saturate(value);
                    }

                    return DebugScalar(value);
                }

                if (_HoUrpAovDebugMode >= 16 && _HoUrpAovDebugMode <= 19)
                {
                    half4 materialCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugScalar(PickChannel(materialCustom, _HoUrpAovDebugMode - 16));
                }

                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                return DebugScalar(maskId.r);
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

            half VisualizeLinearDepth01(half linearDepth)
            {
                return saturate(sqrt(saturate(linearDepth)));
            }

            bool HasValidNormal(half4 normalDepth)
            {
                return dot(abs(normalDepth.rgb), half3(1.0h, 1.0h, 1.0h)) > 0.0h;
            }

            half4 FragTile(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                if (_HoUrpAovDebugMode == 1)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugIdColor(maskId.g);
                }

                if (_HoUrpAovDebugMode == 2)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugScalar(VisualizeLinearDepth01(normalDepth.a));
                }

                if (_HoUrpAovDebugMode == 3)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    if (!HasValidNormal(normalDepth))
                    {
                        return half4(0.0h, 0.0h, 0.0h, 0.0h);
                    }

                    return half4(normalDepth.rgb, 1.0h);
                }

                if (_HoUrpAovDebugMode >= 4 && _HoUrpAovDebugMode <= 7)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugScalar(PickChannel(objectCustom, _HoUrpAovDebugMode - 4));
                }

                if (_HoUrpAovDebugMode >= 8 && _HoUrpAovDebugMode <= 11)
                {
                    half4 objectCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugScalar(PickChannel(objectCustom, _HoUrpAovDebugMode - 8));
                }

                if (_HoUrpAovDebugMode >= 12 && _HoUrpAovDebugMode <= 15)
                {
                    half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    half value = PickChannel(surfaceData, _HoUrpAovDebugMode - 12);
                    if (_HoUrpAovDebugMode == 12 || _HoUrpAovDebugMode == 13)
                    {
                        return DebugIdColor(value);
                    }

                    if (_HoUrpAovDebugMode == 15)
                    {
                        value = saturate(value);
                    }

                    return DebugScalar(value);
                }

                if (_HoUrpAovDebugMode >= 16 && _HoUrpAovDebugMode <= 19)
                {
                    half4 materialCustom = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                    return DebugScalar(PickChannel(materialCustom, _HoUrpAovDebugMode - 16));
                }

                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovDebugSourceTexture, sampler_PointClamp, uv);
                return DebugScalar(maskId.r);
            }
            ENDHLSL
        }
    }
}
