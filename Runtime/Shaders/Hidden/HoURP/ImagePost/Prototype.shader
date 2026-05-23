Shader "Hidden/HoURP/ImagePost/Prototype"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ImagePostColorAdjust"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HoUrpSourceColorTexture);
            float4 _HoUrpImagePostColorTint;
            float4 _HoUrpImagePostParams;

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_LinearClamp, uv);
                half brightness = (half)_HoUrpImagePostParams.x;
                half contrast = (half)_HoUrpImagePostParams.y;
                half tintStrength = saturate((half)_HoUrpImagePostParams.z);
                color.rgb = (color.rgb - 0.5h) * max(0.0h, contrast) + 0.5h;
                color.rgb *= brightness;
                color.rgb = lerp(color.rgb, (half3)_HoUrpImagePostColorTint.rgb, tintStrength * (half)_HoUrpImagePostColorTint.a);
                return color;
            }
            ENDHLSL
        }
    }
}
