Shader "HoURP/Generated/HoUrpDebugLitMinimal"
{
    Properties
    {
        _HoUrpBaseColor("Base Color", Color) = (1, 1, 1, 1)
        // Only material-owned semantics are exposed here. Object/RSUV fields are resolved
        // through HoUrpObjectSemantic.hlsl and should be authored by ObjectSemanticAuthoring.
        _HoUrpGeneratedMaterialClass("Material Class", Float) = 1
        _HoUrpGeneratedMaterialSssProfile("SSS Profile", Float) = 1
        _HoUrpGeneratedMaterialThickness("Thickness", Range(0, 1)) = 0.5
        _HoUrpGeneratedMaterialCurvature("Curvature", Range(-1, 1)) = 0
        _HoUrpGeneratedMaterialCustom0_3("Material Custom 0-3", Vector) = (0, 0, 0, 0)
        _HoUrpGeneratedSssSourceColor("SSS Source Color", Color) = (1, 0.75, 0.6, 1)
        _HoUrpGeneratedSssWeight("SSS Weight", Range(0, 1)) = 0.5
        _HoUrpSupportsOit("Supports OIT", Float) = 1
        _HoUrpParticipatesOit("Participates OIT", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragForward

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half4 _HoUrpBaseColor;
            float _HoUrpSupportsOit;
            float _HoUrpParticipatesOit;
            float _HoUrpOitActive;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                return output;
            }

            half4 FragForward(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                HoUrpSurfaceData surface = HoUrpCreateSurfaceData(_HoUrpBaseColor.rgb, _HoUrpBaseColor.a, input.normalWS);
                clip(0.5h - half(_HoUrpOitActive * _HoUrpSupportsOit * _HoUrpParticipatesOit));
                half ndotl = saturate(dot(normalize(surface.normalWS), normalize(half3(0.3h, 0.6h, 0.7h))));
                half3 debugLighting = surface.baseColor * (0.25h + 0.75h * ndotl);
                return half4(debugLighting, surface.alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HoUrpAovOutput"
            Tags { "LightMode" = "HoUrpAovOutput" }

            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragAov

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpObjectSemantic.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                float2 depthZW : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct AovOutput
            {
                half4 maskId : SV_Target0;
                half4 normalDepth : SV_Target1;
                half4 objectCustom0 : SV_Target2;
                half4 objectCustom1 : SV_Target3;
                half4 surfaceData : SV_Target4;
                half4 materialCustom0 : SV_Target5;
                half4 sssSource : SV_Target6;
            };

            half4 _HoUrpBaseColor;
            float _HoUrpGeneratedMaterialClass;
            float _HoUrpGeneratedMaterialSssProfile;
            float _HoUrpGeneratedMaterialThickness;
            float _HoUrpGeneratedMaterialCurvature;
            float4 _HoUrpGeneratedMaterialCustom0_3;
            float4 _HoUrpGeneratedSssSourceColor;
            float _HoUrpGeneratedSssWeight;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.depthZW = positionInputs.positionCS.zw;
                return output;
            }

            AovOutput FragAov(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                HoUrpObjectSemanticData objectSemantic = HoUrpResolveObjectSemanticData();
                half maskWeight = objectSemantic.maskWeight;
                half3 normalWS = normalize(input.normalWS);
                float rawDepth = input.depthZW.x / max(input.depthZW.y, 1.0e-6);
                half linear01Depth = half(saturate(Linear01Depth(rawDepth, _ZBufferParams)));

                HoUrpMaterialSemanticData semantic = HoUrpCreateMaterialSemanticData(
                    half(_HoUrpGeneratedMaterialClass),
                    half(_HoUrpGeneratedMaterialSssProfile),
                    half(_HoUrpGeneratedMaterialThickness),
                    half(_HoUrpGeneratedMaterialCurvature),
                    half4(_HoUrpGeneratedMaterialCustom0_3),
                    half3(_HoUrpGeneratedSssSourceColor.rgb),
                    half(_HoUrpGeneratedSssWeight));
                HoUrpAovOutputData materialAov = HoUrpEncodeMaterialAov(semantic, maskWeight);

                AovOutput output;
                output.maskId = HoUrpEncodeObjectMaskId(objectSemantic);
                output.normalDepth = half4(normalWS * 0.5h + 0.5h, linear01Depth);
                output.objectCustom0 = HoUrpEncodeObjectCustom0_3(objectSemantic);
                output.objectCustom1 = HoUrpEncodeObjectCustom4_7(objectSemantic);
                output.surfaceData = materialAov.surfaceData;
                output.materialCustom0 = materialAov.materialCustom0_3;
                output.sssSource = materialAov.sssSource;
                return output;
            }
            ENDHLSL
        }

        Pass
        {
            Name "HoUrpOitAccumulation"
            Tags { "LightMode" = "HoUrpOitAccumulation" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend 0 One One
            Blend 1 Zero OneMinusSrcColor

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragOit

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl"
            #include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct OitOutput
            {
                half4 accumulation : SV_Target0;
                half revealage : SV_Target1;
            };

            half4 _HoUrpBaseColor;
            float _HoUrpSupportsOit;
            float _HoUrpParticipatesOit;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                return output;
            }

            OitOutput FragOit(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                HoUrpSurfaceData surface = HoUrpCreateSurfaceData(_HoUrpBaseColor.rgb, _HoUrpBaseColor.a, input.normalWS);
                HoUrpTransparentOutputData transparentData = HoUrpCreateTransparentOutputData(
                    surface,
                    half(_HoUrpSupportsOit),
                    half(_HoUrpParticipatesOit));
                transparentData.alpha *= transparentData.supportsOit * transparentData.participatesOit;
                half ndotl = saturate(dot(normalize(surface.normalWS), normalize(half3(0.3h, 0.6h, 0.7h))));
                transparentData.color = surface.baseColor * (0.25h + 0.75h * ndotl);

                HoUrpOitAccumulationData accumulation = HoUrpEncodeOitAccumulation(transparentData);
                OitOutput output;
                output.accumulation = half4(accumulation.weightedColor, accumulation.weightedAlpha);
                output.revealage = accumulation.revealage;
                return output;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex VertShadow
            #pragma fragment FragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertShadow(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 FragShadow(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
