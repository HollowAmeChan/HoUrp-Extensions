Shader "Hidden/HoURP/SemanticPost/AovReadProbe"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "AovReadProbe"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpSourceColorTexture);
            TEXTURE2D_X(_HoUrpAovMaskIdTexture);
            TEXTURE2D_X(_HoUrpAovNormalDepthTexture);
            TEXTURE2D_X(_HoUrpAovObjectCustom0_3Texture);
            TEXTURE2D_X(_HoUrpAovObjectCustom4_7Texture);

            half4 _HoUrpSemanticPostTintColor;
            int _HoUrpSemanticPostObjectCustomChannel;

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

            half SampleObjectCustom(float2 uv)
            {
                int channel = clamp(_HoUrpSemanticPostObjectCustomChannel, 0, 7);
                if (channel < 4)
                {
                    half4 values = SAMPLE_TEXTURE2D_X(_HoUrpAovObjectCustom0_3Texture, sampler_PointClamp, uv);
                    return PickChannel(values, channel);
                }

                half4 values = SAMPLE_TEXTURE2D_X(_HoUrpAovObjectCustom4_7Texture, sampler_PointClamp, uv);
                return PickChannel(values, channel - 4);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 color = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_LinearClamp, uv);
                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovMaskIdTexture, sampler_PointClamp, uv);
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half objectCustom = SampleObjectCustom(uv);

                half mask = saturate(max(maskId.r * 0.35h, objectCustom) * (0.75h + 0.25h * normalDepth.a));
                color.rgb = lerp(color.rgb, _HoUrpSemanticPostTintColor.rgb, mask * _HoUrpSemanticPostTintColor.a);
                return color;
            }
            ENDHLSL
        }
    }
}
