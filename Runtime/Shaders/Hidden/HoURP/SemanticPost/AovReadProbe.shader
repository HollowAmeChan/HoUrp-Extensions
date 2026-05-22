Shader "Hidden/HoURP/SemanticPost/AovReadProbe"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SemanticPostMask"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertSemanticPost
            #pragma fragment FragMask

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpAovMaskIdTexture);
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovObjectCustom0_3Texture);
            TEXTURE2D_X(_HoUrpAovObjectCustom4_7Texture);
            TEXTURE2D_X(_HoUrpAovSurfaceDataTexture);
            TEXTURE2D_X(_HoUrpAovMaterialCustom0_3Texture);
            TEXTURE2D_X(_HoUrpAovSssSourceTexture);
            TEXTURE2D_X(_HoUrpSssSourceTexture);
            TEXTURE2D_X(_HoUrpSssDiffusionTexture);

            float4 _HoUrpSemanticPostLayerParams[4];
            float4 _HoUrpSemanticPostRuleParams[16];
            float4 _HoUrpSemanticPostRuleValues[16];

            Varyings VertSemanticPost(uint vertexID : SV_VertexID)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
                return output;
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

            half SampleObjectCustom(float2 uv, int channel)
            {
                channel = clamp(channel, 0, 7);
                if (channel < 4)
                {
                    half4 values = SAMPLE_TEXTURE2D_X(_HoUrpAovObjectCustom0_3Texture, sampler_PointClamp, uv);
                    return PickChannel(values, channel);
                }

                half4 values = SAMPLE_TEXTURE2D_X(_HoUrpAovObjectCustom4_7Texture, sampler_PointClamp, uv);
                return PickChannel(values, channel - 4);
            }

            half SampleMaterialCustom(float2 uv, int channel)
            {
                half4 values = SAMPLE_TEXTURE2D_X(_HoUrpAovMaterialCustom0_3Texture, sampler_PointClamp, uv);
                return PickChannel(values, clamp(channel, 0, 3));
            }

            bool AllowsSemanticPost(float2 uv)
            {
                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovMaskIdTexture, sampler_PointClamp, uv);
                uint flags = (uint)round(maskId.a * 255.0h);
                return maskId.r > 0.0h && (flags & 2u) != 0u;
            }

            half SampleSource(float2 uv, int source)
            {
                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovMaskIdTexture, sampler_PointClamp, uv);
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half4 surfaceData = SAMPLE_TEXTURE2D_X(_HoUrpAovSurfaceDataTexture, sampler_PointClamp, uv);
                half groupId = half((uint)round(maskId.b * 255.0h) & 7u) / 255.0h;

                if (source == 1) return maskId.r;
                if (source == 2) return maskId.g;
                if (source == 3) return groupId;
                if (source == 4) return maskId.a;
                if (source >= 10 && source <= 17) return SampleObjectCustom(uv, source - 10);
                if (source == 20) return surfaceData.r;
                if (source == 21) return surfaceData.g;
                if (source == 22) return surfaceData.b;
                if (source == 23) return surfaceData.a;
                if (source >= 30 && source <= 33) return SampleMaterialCustom(uv, source - 30);
                if (source == 40)
                {
                    half4 sssSource = SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture, sampler_PointClamp, uv);
                    return sssSource.a;
                }
                if (source == 41)
                {
                    half4 sssDiffusion = SAMPLE_TEXTURE2D_X(_HoUrpSssDiffusionTexture, sampler_PointClamp, uv);
                    return sssDiffusion.a;
                }
                if (source == 50) return normalDepth.a;
                if (source == 51)
                {
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

            half EvaluateLayerMask(float2 uv, int layer)
            {
                if (_HoUrpSemanticPostLayerParams[layer].x <= 0.0)
                {
                    return 0.0h;
                }

                if (!AllowsSemanticPost(uv))
                {
                    return 0.0h;
                }

                half result = 0.0h;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    int ruleIndex = layer * 4 + i;
                    float4 ruleParams = _HoUrpSemanticPostRuleParams[ruleIndex];
                    int source = (int)round(ruleParams.x);
                    if (source < 0)
                    {
                        continue;
                    }

                    int op = (int)round(ruleParams.y);
                    int combine = (int)round(ruleParams.z);
                    half sourceValue = SampleSource(uv, source);
                    half ruleMask = EvaluateOperator(sourceValue, _HoUrpSemanticPostRuleValues[ruleIndex], op);
                    result = CombineMask(result, ruleMask, combine);
                }

                return saturate(result);
            }

            half4 FragMask(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                return half4(
                    EvaluateLayerMask(uv, 0),
                    EvaluateLayerMask(uv, 1),
                    EvaluateLayerMask(uv, 2),
                    EvaluateLayerMask(uv, 3));
            }
            ENDHLSL
        }

        Pass
        {
            Name "SemanticTintComposite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertSemanticPost
            #pragma fragment FragComposite

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpSourceColorTexture);
            TEXTURE2D_X(_HoUrpSemanticPostMaskTexture);

            float4 _HoUrpSemanticPostLayerParams[4];
            float4 _HoUrpSemanticPostLayerColors[4];

            Varyings VertSemanticPost(uint vertexID : SV_VertexID)
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
                half4 masks = SAMPLE_TEXTURE2D_X(_HoUrpSemanticPostMaskTexture, sampler_PointClamp, uv);

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float4 layerParams = _HoUrpSemanticPostLayerParams[i];
                    if (layerParams.x <= 0.0 || round(layerParams.y) != 1.0)
                    {
                        continue;
                    }

                    half mask = PickChannel(masks, i);
                    half opacity = saturate(mask * (half)layerParams.w);
                    half4 layerColor = (half4)_HoUrpSemanticPostLayerColors[i];
                    color.rgb = saturate(BlendLayer(color.rgb, layerColor.rgb, opacity * layerColor.a, (int)round(layerParams.z)));
                }

                return color;
            }
            ENDHLSL
        }
    }
}
