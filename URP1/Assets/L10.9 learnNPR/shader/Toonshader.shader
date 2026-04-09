Shader "Toon/ToonShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        [Header(Outline)]
        [Toggle(_USE_SMOOTH_NORMAL)] _UseSNormal ("Use Smooth Normal", Float) = 1
        _StencilRef ("Stencil Ref", int) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Float) = 0.005
        _CameraDistance ("相机距离描边调节系数 0不调节 1调节",range(0,1)) = 0

        [Header(Shade)]
        [IntRange] _StepCount ("Step Count", Range(1,4)) = 2
        _StepLevel ("Step Level", Range(0,1)) = 0.5
        _StepSmooth ("Step Smooth", Range(0,1)) = 0.2
        [Toggle]_StepMode("StepMode",int) = 0 //0 step函数 1 ramptex
        _RampColormap ("Ramp Colormap", 2D) = "white" {}//ramp暗部乘上MainTex当作暗部颜色 
        [Toggle]_UseAO("Use AO",int) = 1 //是否使用环境光遮挡
        _AOmap ("AO Map", 2D) = "white" {}//环境光遮挡贴图
        [Toggle]_UseFaceInfo("Use Face Info",int) = 1 
        _FaceInfo("xyz面部中心坐标 w插值球面法线与原始法线",Vector) = (0,0,0,0)

        [Toggle]_SpecON("Spec ON",int) = 1 //是否使用高光
        _SpecColor("Spec Color",Color) = (1,1,1,1)
        _SpecPow("Spec Pow",Range(1,200)) = 10
        _SpecSmooth("Spec Smooth",Range(0,1)) = 0.5
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
            TEXTURE2D(_GradientTex);SAMPLER(sampler_GradientTex);//全局纹理 渐变纹理
            TEXTURE2D(_RampColormap);SAMPLER(sampler_RampColormap);
            TEXTURE2D(_AOmap);SAMPLER(sampler_AOmap);//环境光遮挡贴图
            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                half4 _BaseColor;
                float4 _MainTex_ST;
                float _CameraDistance;
                half4 _OutlineColor;
                bool _UseSNormal;
                int _StepCount;
                float _StepLevel;
                float _StepSmooth;
                bool _StepMode;//0 step函数 1 ramptex
                bool _UseAO;//是否使用环境光遮挡
                bool _UseFaceInfo;//是否使用面部中心坐标
                float4 _FaceInfo;//面部中心坐标 0,0,0,0
                half4 _SpecColor;//高光颜色
                float _SpecPow;//高光强度
                bool _SpecON;//是否使用高光
                float _SpecSmooth;//高光平滑度
                
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                //float4 Color : COLOR;
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
                if(_UseFaceInfo)
                {
                    half3 SphNormal = normalize(input.positionOS.xyz-_FaceInfo.xyz);
                    input.normalOS = lerp(input.normalOS,SphNormal,_FaceInfo.w);
                }
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            
            half4 frag (Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 LightDir = normalize(GetMainLight().direction);
                half NdotL = dot(normalWS, LightDir)*0.5+0.5;
                half3 viewDirectionWS = -normalize(input.positionWS-_WorldSpaceCameraPos);
                half3 H = normalize(viewDirectionWS+LightDir);
                //根据_stepMode判断使用step函数还是ramptex
                half lvl;//色阶
                if(_StepMode)
                {
                    half NdotLSmoothStep = Smootherstep(_StepLevel,_StepLevel+_StepSmooth,NdotL);
                    half NdotLStep=step(_StepLevel,NdotL);
                    lvl = ceil(NdotLSmoothStep*_StepCount)/_StepCount;
                }
                else
                {
                    lvl = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, NdotL).r;
                }
                if(_UseAO)
                {
                    half AO = SAMPLE_TEXTURE2D(_AOmap, sampler_AOmap, input.uv).r;
                    lvl *= AO;
                }
                half3 positionWS = input.positionWS;
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                half3 ramptex = SAMPLE_TEXTURE2D(_RampColormap, sampler_RampColormap, input.uv);
                half3 diffuse = lerp(albedo*ramptex,albedo,lvl);
                half3 spec=0;

                if(_SpecON > 0.5)
                {
                    spec = pow(saturate(dot(normalWS, H)), _SpecPow) * _SpecColor.rgb;
                    spec = Smootherstep(0.3,0.3+_SpecSmooth,spec)/5;
                }
                half3 color = diffuse+spec;
                
                return half4(color, 1);
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
            #pragma multi_compile _ _USE_SMOOTH_NORMAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                half4 _BaseColor;
                float4 _MainTex_ST;
                float _CameraDistance;
                half4 _OutlineColor;
                bool _UseSNormal;
                int _StepCount;
                float _StepLevel;
                float _StepSmooth;  
                bool _StepMode;//0 step函数 1 ramptex
                bool _UseAO;//是否使用环境光遮挡
                bool _UseFaceInfo;//是否使用面部中心坐标
                float4 _FaceInfo;//面部中心坐标 0,0,0,0
                half4 _SpecColor;//高光颜色
                float _SpecPow;//高光强度
                bool _SpecON;//是否使用高光
                float _SpecSmooth;//高光平滑度
            CBUFFER_END
            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float3 tangentOS : TANGENT;
                float3 SmoothNormal : TEXCOORD7;
                float4 Color : COLOR;
            };
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float4 Color : COLOR;
            };
            Varyings vert (Attributes input)
            {
                Varyings output;
                float3 positionWS = GetVertexPositionInputs(input.positionOS.xyz).positionWS;
                float camDistance = length(positionWS - GetCameraPositionWS());
                camDistance = lerp(1,camDistance,_CameraDistance);
                float camFactor = camDistance*_ProjectionParams.w;//_ProjectionParams.w远裁剪平面距离倒数 做摄像机距离调节描边宽度系数
                float3 pos;   
                pos=input.positionOS.xyz+input.normalOS*camFactor*_OutlineWidth;   
                    #ifdef _USE_SMOOTH_NORMAL
                    pos=input.positionOS.xyz+input.SmoothNormal*camFactor*_OutlineWidth;
                    #endif
                output.positionCS = GetVertexPositionInputs(pos).positionCS;                
                output.normalWS = GetVertexNormalInputs(input.normalOS).normalWS;
                output.uv = input.uv;
                output.Color = input.Color;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                return float4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
