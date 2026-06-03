Shader "L17 Volumetric Lighting/Two Sided Interior Lit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.74, 0.66, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.18, 0.16, 0.14, 1.0)
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.18
        _SpecularStrength ("Specular Strength", Range(0.0, 2.0)) = 0.2
        _WrapDiffuse ("Wrap Diffuse", Range(0.0, 1.0)) = 0.06
        _AmbientBoost ("Ambient Boost", Range(0.0, 3.0)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float _Smoothness;
                float _SpecularStrength;
                float _WrapDiffuse;
                float _AmbientBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE faceSign : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float facing = IS_FRONT_VFACE(faceSign, 1.0, -1.0);
                float3 normalWS = normalize(input.normalWS * facing);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDirWS = normalize(mainLight.direction);

                float ndl = dot(normalWS, lightDirWS);
                float wrappedDiffuse = saturate((ndl + _WrapDiffuse) / (1.0 + _WrapDiffuse));
                float shadow = mainLight.shadowAttenuation;
                float3 directDiffuse = _BaseColor.rgb * wrappedDiffuse * shadow;

                float3 halfDirWS = SafeNormalize(lightDirWS + viewDirWS);
                float specular = pow(saturate(dot(normalWS, halfDirWS)), lerp(16.0, 96.0, _Smoothness)) * _SpecularStrength * shadow;
                float3 ambientTint = lerp(_ShadowColor.rgb, _BaseColor.rgb, 0.28);
                float3 ambient = SampleSH(normalWS) * _AmbientBoost * ambientTint;

                return half4(ambient + mainLight.color * directDiffuse + specular, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
