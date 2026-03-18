Shader "Unlit/SSS"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor("Main Color", Color) = (1,1,1,1)
        _SpecularPower("Specular Power", Float) = 10
        _Distortion("法线扰动背光", Range(0,1)) = 0.5
        _BehindPower("背光pow", Range(0,10)) = 1
        _BehindStrenth("背光强度", Range(1,4)) = 1
        _BehindAmbient("背光环境", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _SpecularPower;
                half _Distortion;
                half _BehindPower;
                half _BehindStrenth;
                half _BehindAmbient;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert (Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                Light mainlight = GetMainLight();
                half3 N = input.normalWS;
                half3 L = mainlight.direction;
                half3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 H = normalize(L + V);

                half NdotL = saturate(dot(N, L));
                half3 mainLightColor = mainlight.color;

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 diffuse = NdotL * mainLightColor * _BaseColor.rgb;
                half3 specular = pow(saturate(dot(N, H)), _SpecularPower) * mainLightColor * _BaseColor.rgb;

                // SSS backlight term
                half3 Ldistort = L + N * _Distortion;
                half backlight = pow(saturate(dot(V, -Ldistort)), _BehindPower) * _BehindStrenth + _BehindAmbient;
                half3 sss = backlight * mainLightColor * _BaseColor.rgb;

                half3 finalColor = diffuse + specular + sss;
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}
