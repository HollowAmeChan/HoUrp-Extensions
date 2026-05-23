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
            float4 _HoUrpScreenPostParams;

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            half4 FragMask(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 maskId = SAMPLE_TEXTURE2D_X(_HoUrpAovMaskIdTexture, sampler_PointClamp, uv);
                half4 normalDepth = SAMPLE_TEXTURE2D_X(_HoUrpAovNormalDepthTexture, sampler_PointClamp, uv);
                half coverage = maskId.r;
                half isPreviewFallback = 0.0h;
                if (_HoUrpScreenPostParams.z > 0.5 && coverage <= 0.0001h)
                {
                    coverage = 0.5h;
                    isPreviewFallback = 1.0h;
                }
                half depthFactor = isPreviewFallback > 0.5h
                    ? 1.0h
                    : saturate((half)_HoUrpScreenPostParams.y <= 0.0h ? 1.0h : normalDepth.a / (half)_HoUrpScreenPostParams.y);
                return half4(saturate(coverage * depthFactor), 0.0h, 0.0h, 1.0h);
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

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            half4 FragComposite(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_HoUrpSourceColorTexture, sampler_LinearClamp, uv);
                half mask = SAMPLE_TEXTURE2D_X(_HoUrpScreenPostMaskTexture, sampler_PointClamp, uv).r;
                half opacity = saturate(mask * (half)_HoUrpScreenPostParams.x * (half)_HoUrpScreenPostTintColor.a);
                color.rgb = lerp(color.rgb, (half3)_HoUrpScreenPostTintColor.rgb, opacity);
                return color;
            }
            ENDHLSL
        }
    }
}
