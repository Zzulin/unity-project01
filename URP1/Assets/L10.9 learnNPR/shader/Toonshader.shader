Shader "Toon/ToonShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        _OutlineWidth ("Outline Width", Float) = 0.005
        _StencilRef ("Stencil Ref", int) = 1
        _CameraDistance ("相机距离描边调节系数 0不调节 1调节",range(0,1)) = 0
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
            Cull [_Cull]
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                half4 _BaseColor;
                float4 _MainTex_ST;
                float _CameraDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };
            Varyings vert (Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 ShadeToon (Varyings input)
            {
                half3 normalWS = normalize(input.normalWS);
                half NdotL = dot(normalWS, GetMainLight().direction);
                half3 positionWS = input.positionWS;
                half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _BaseColor.rgb;

                half4 color = half4(albedo, 0);
                return color;
            }

            
            half4 frag (Varyings input) : SV_Target
            {
                return ShadeToon(input);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Outline"}
            Cull front
            blend SrcAlpha OneMinusSrcAlpha
            Stencil
            {
                Ref [_StencilRef]
                Comp NotEqual
                Pass Replace
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                half4 _BaseColor;
                float4 _MainTex_ST;
                float _CameraDistance;
            CBUFFER_END
            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                
            };
            Varyings vert (Attributes input)
            {
                Varyings output;

                float3 positionWS = GetVertexPositionInputs(input.positionOS.xyz).positionWS;
                float camDistance = length(positionWS - GetCameraPositionWS());
                camDistance = lerp(1,camDistance,_CameraDistance);
                float3 pos=input.positionOS.xyz+input.normalOS*camDistance*_OutlineWidth;

                output.positionCS = GetVertexPositionInputs(pos).positionCS;
                
                output.normalWS = GetVertexNormalInputs(input.normalOS).normalWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                return half4(0, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
