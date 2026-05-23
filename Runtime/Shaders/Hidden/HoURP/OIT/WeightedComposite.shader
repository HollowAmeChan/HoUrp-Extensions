Shader "Hidden/HoURP/OIT/WeightedComposite"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Weighted OIT Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpOitCompositeSourceTexture);
            TEXTURE2D_X(_HoUrpOitAccumulationTexture);
            TEXTURE2D_X(_HoUrpOitRevealageTexture);

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 sourceColor = SAMPLE_TEXTURE2D_X(_HoUrpOitCompositeSourceTexture, sampler_LinearClamp, uv);
                float4 accumulation = SAMPLE_TEXTURE2D_X(_HoUrpOitAccumulationTexture, sampler_LinearClamp, uv);
                float revealage = SAMPLE_TEXTURE2D_X(_HoUrpOitRevealageTexture, sampler_LinearClamp, uv).r;

                float weight = max(accumulation.a, 1.0e-5);
                float3 transparentColor = accumulation.rgb / weight;
                float transparentAlpha = saturate(1.0 - revealage);
                sourceColor.rgb = lerp(sourceColor.rgb, transparentColor, transparentAlpha);
                return sourceColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
