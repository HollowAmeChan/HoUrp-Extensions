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

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float depth = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, input.uv, 0).r;
#if UNITY_REVERSED_Z
                float visibleDepth = 1.0 - depth;
#else
                float visibleDepth = depth;
#endif
                float2 grid = abs(frac(input.uv * 8.0) - 0.5);
                float gridLine = step(0.485, max(grid.x, grid.y));
                half3 gridColor = _HoUrpShadowCastDebugMode > 1.5
                    ? half3(1.0h, 0.72h, 0.18h)
                    : half3(0.1h, 0.8h, 1.0h);
                half3 color = lerp(half3(visibleDepth, visibleDepth, visibleDepth), gridColor, gridLine * 0.35h);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
