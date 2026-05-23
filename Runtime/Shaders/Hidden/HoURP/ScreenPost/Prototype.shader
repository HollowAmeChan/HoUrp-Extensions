Shader "Hidden/HoURP/ScreenPost/Prototype"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ScreenPostMask"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragMask

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpAovMaskIdTexture);
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovObjectCustom0_3Texture);
            TEXTURE2D_X(_HoUrpAovObjectCustom4_7Texture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);
            float4 _HoUrpScreenPostParams;
            float4 _HoUrpScreenPostRuleParams[4];
            float4 _HoUrpScreenPostRuleValues[4];

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            half PickChannel(half4 values, int channel)
            {
                if (channel == 1) return values.g;
                if (channel == 2) return values.b;
                if (channel == 3) return values.a;
                return values.r;
            }

            half SampleObjectCustom(float2 uv, int channel)
            {
                channel = clamp(channel, 0, 7);
                if (channel < 4)
                {
                    return PickChannel(SAMPLE_TEXTURE2D_X(_HoUrpAovObjectCustom0_3Texture, sampler_PointClamp, uv), channel);
                }

                return PickChannel(SAMPLE_TEXTURE2D_X(_HoUrpAovObjectCustom4_7Texture, sampler_PointClamp, uv), channel - 4);
            }

            half SampleSource(float2 uv, int source)
            {
                if (source >= 1 && source <= 4)
                {
                    half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovMaskIdTexture, sampler_PointClamp, uv);
                    if (source == 1) return maskId.r;
                    if (source == 2) return maskId.g;
                    if (source == 3) return half((uint)round(maskId.b * 255.0h) & 7u) / 255.0h;
                    return maskId.a;
                }

                if (source >= 10 && source <= 17) return SampleObjectCustom(uv, source - 10);

                if (source == 20 || source == 22 || source == 23)
                {
                    half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                    if (source == 20) return surfaceData.r;
                    if (source == 22) return surfaceData.b;
                    return surfaceData.a;
                }

                if (source == 50 || source == 51)
                {
                    half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                    if (source == 50) return normalDepth.a;
                    half3 n = normalize(normalDepth.rgb * 2.0h - 1.0h);
                    return saturate(n.z * 0.5h + 0.5h);
                }

                return 1.0h;
            }

            half EvaluateOperator(half sourceValue, float4 values, int op)
            {
                if (op == 0) return 1.0h;
                if (op == 1) return sourceValue > values.x ? 1.0h : 0.0h;
                if (op == 2) return sourceValue < values.x ? 1.0h : 0.0h;
                if (op == 3) return (sourceValue >= values.x && sourceValue <= values.y) ? 1.0h : 0.0h;
                if (op == 4) return abs(round(sourceValue * 255.0h) - values.x) < 0.5h ? 1.0h : 0.0h;
                if (op == 5)
                {
                    uint sourceBits = (uint)round(sourceValue * 255.0h);
                    uint mask = (uint)round(values.x);
                    return (sourceBits & mask) != 0u ? 1.0h : 0.0h;
                }
                if (op == 6)
                {
                    uint sourceBits = (uint)round(sourceValue * 255.0h);
                    uint mask = (uint)round(values.x);
                    return (sourceBits & mask) == mask ? 1.0h : 0.0h;
                }

                return 0.0h;
            }

            half CombineMask(half currentMask, half ruleMask, int combine)
            {
                if (combine == 1) return max(currentMask, ruleMask);
                if (combine == 2) return currentMask * ruleMask;
                if (combine == 3) return saturate(currentMask - ruleMask);
                if (combine == 4) return currentMask * ruleMask;
                return ruleMask;
            }

            half4 FragMask(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half result = saturate((half)_HoUrpScreenPostParams.y);

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 ruleParams = _HoUrpScreenPostRuleParams[i];
                    int source = (int)round(ruleParams.x);
                    if (source < 0)
                    {
                        continue;
                    }

                    half sourceValue = SampleSource(uv, source);
                    half ruleMask = EvaluateOperator(sourceValue, _HoUrpScreenPostRuleValues[i], (int)round(ruleParams.y));
                    result = CombineMask(result, saturate(ruleMask * (half)ruleParams.w), (int)round(ruleParams.z));
                }

                if (_HoUrpScreenPostParams.z > 0.5)
                {
                    result = 1.0h - result;
                }

                return half4(saturate(result), 0.0h, 0.0h, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ScreenPostComposite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpSourceColorTexture);
            TEXTURE2D_X(_HoUrpScreenPostMaskTexture);
            float4 _HoUrpScreenPostTintColor;
            float4 _HoUrpScreenPostParams;
            float4 _HoUrpScreenPostLayerParams;

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            half3 BlendLayer(half3 baseColor, half3 layerColor, half opacity, int blend)
            {
                if (blend == 1) return baseColor + layerColor * opacity;
                if (blend == 2) return lerp(baseColor, baseColor * layerColor, opacity);
                if (blend == 3) return lerp(baseColor, 1.0h - (1.0h - baseColor) * (1.0h - layerColor), opacity);
                return lerp(baseColor, layerColor, opacity);
            }

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_LinearClamp, uv);
                half mask = SAMPLE_TEXTURE2D_X(_HoUrpScreenPostMaskTexture, sampler_PointClamp, uv).r;
                half opacity = saturate(mask * (half)_HoUrpScreenPostParams.x * (half)_HoUrpScreenPostTintColor.a);
                color.rgb = saturate(BlendLayer(color.rgb, (half3)_HoUrpScreenPostTintColor.rgb, opacity, (int)round(_HoUrpScreenPostLayerParams.x)));
                return color;
            }
            ENDHLSL
        }
    }
}
