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
        [Toggle]_StepMode("StepMode",int) = 0 
        _RampColormap ("Ramp Colormap", 2D) = "gray" {}//ramp暗部乘上MainTex当作暗部颜色 
        [Toggle]_UseAO("Use AO",int) = 1 //是否使用环境光遮挡
        _AOmap ("AO Map", 2D) = "white" {}//环境光遮挡贴图
        //是否使用球型法线
        [Toggle]_UseFaceInfo("Use Face Info",int) = 0 
        _FaceInfo("xyz面部中心坐标 w插值球面法线与原始法线",Vector) = (0,0,0,0)

        [Toggle]_SpecON("Spec ON",int) = 1 //是否使用高光
        _SpecColor("Spec Color",Color) = (1,1,1,1)
        [Toggle]_UseAnisotropic("Use Anisotropic",int) = 0 //是否使用各向异性高光
        _ShiftMap("Shift Map",2D) = "white" {}//各向异性高光贴图
        _SpecShiftIntensity("Spec Shift Intensity",Range(0,1)) = 1
        _SpecPow("Spec Pow",Range(1,200)) = 10
        _SpecStep("Spec Step",Range(0,1)) = 0.5
        _SpecSmooth("Spec Smooth",Range(0,1)) = 0.5

        [Toggle]_RimON("Rim ON",int) = 1 //是否使用菲涅尔
        _RimColor("Rim Color",Color) = (1,1,1,1)
        _RimPow("Rim Pow",Range(1,10)) = 1
        _RimStep("Rim Step",Range(0,1)) = 0.369
        _RimStepSmooth("Rim Step Smooth",Range(0,0.2)) = 0.1
        _RimIntensity("Rim Intensity",Range(0,10)) =1
        
        [Toggle]_UseShadow("Use Shadow",int) = 1 //是否使用阴影
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_GradientTex);SAMPLER(sampler_GradientTex);//全局纹理 渐变纹理
            TEXTURE2D(_RampColormap);SAMPLER(sampler_RampColormap);
            TEXTURE2D(_AOmap);SAMPLER(sampler_AOmap);//环境光遮挡贴图
            TEXTURE2D(_ShiftMap);SAMPLER(sampler_ShiftMap);//各向异性高光贴图
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
                bool _StepMode;
                bool _UseAO;//是否使用环境光遮挡
                bool _UseFaceInfo;//是否使用面部中心坐标
                float4 _FaceInfo;//面部中心坐标 0,0,0,0
                half4 _SpecColor;//高光颜色
                float _SpecPow;//高光强度
                bool _SpecON;//是否使用高光
                bool _UseAnisotropic;//是否使用各向异性高光
                float _SpecShiftIntensity;//各向异性高光强度
                float _SpecStep;//高光步长
                float _SpecSmooth;//高光平滑度  
                bool _RimON;//是否使用菲涅尔
                half4 _RimColor;//Rim颜色
                float _RimPow;//Rim强度
                float _RimStep;//Rim步长
                float _RimStepSmooth;//Rim步长平滑度
                float _RimIntensity;//Rim强度
                bool _UseShadow;//是否使用阴影
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 Color : COLOR;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD5;
                #endif
                float4 Color : COLOR;
            };
            Varyings vert (Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                if(_UseFaceInfo)
                {
                    half3 SphNormal = normalize(input.positionOS.xyz-_FaceInfo.xyz);
                    input.normalOS = lerp(SphNormal,input.normalOS,_FaceInfo.w);
                }
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.bitangentWS=normalize(cross(output.normalWS,output.tangentWS)*input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                    //output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
                #endif
                output.Color = input.Color;
                return output;
            }
            
            float kajiyaKay(float3 H,float3 B,float _SpecPow)
            {
                float dotBH = dot(B,H);
                float sinBH = sqrt(1-dotBH*dotBH);
                float dirAtten = Smootherstep(-1,0,dotBH);
                return dirAtten*pow(sinBH,_SpecPow);    
            }
             
            half4 frag (Varyings input) : SV_Target
            {
                float4 shadowCoord=float4(0,0,0,0);//没有阴影坐标;
                if(_UseShadow>0.5)
                {
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    shadowCoord=input.shadowCoord;//使用顶点着色器传来的逐顶点阴影坐标
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    shadowCoord=TransformWorldToShadowCoord(input.positionWS);//重新计算逐像素阴影坐标
                #else
                    shadowCoord=float4(0,0,0,0);//没有阴影坐标
                #endif
                }

                #ifdef LIGHTMAP_ON
                    shadowMask=SAMPLE_SHADOWMASK(input.lightmapUV);
                #endif
                Light mainLight =GetMainLight(shadowCoord);
                half3 N = normalize(input.normalWS);
                half3 L = normalize(mainLight.direction);
                half NdotL = dot(N, L)*0.5+0.5;
                NdotL*=mainLight.shadowAttenuation;
                half3 V = -normalize(input.positionWS-_WorldSpaceCameraPos);
                half3 H = normalize(V+L);
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
                    spec = pow(saturate(dot(N, H)), _SpecPow);
                    
                    if(_UseAnisotropic)
                    {
                        half shift = SAMPLE_TEXTURE2D(_ShiftMap, sampler_ShiftMap, input.uv).r*2-1.0;
                        half3 B = normalize(input.bitangentWS)+shift*N*_SpecShiftIntensity;
                        spec = kajiyaKay(H,B,_SpecPow);
                    }
                    spec = Smootherstep(_SpecStep,_SpecStep+_SpecSmooth,spec)*_SpecColor.rgb;
                }
                half3 fresnelColor=0;
                if (_RimON > 0.5)
                {
                    fresnelColor = pow(1-saturate(dot(N, V)),_RimPow)*_RimColor.rgb*_RimIntensity;
                    fresnelColor=Smootherstep(_RimStep,_RimStep+_RimStepSmooth,fresnelColor);     
                }
                half3 color = (diffuse+spec+fresnelColor)*_BaseColor.rgb;
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
                bool _UseAnisotropic;//是否使用各向异性高光
                float _SpecShiftIntensity;//各向异性高光强度
                float _SpecStep;//高光步长
                float _SpecSmooth;//高光平滑度  
                bool _RimON;//是否使用菲涅尔
                half4 _RimColor;//Rim颜色
                float _RimPow;//Rim强度
                float _RimStep;//Rim步长
                float _RimStepSmooth;//Rim步长平滑度
                float _RimIntensity;//Rim强度
                bool _UseShadow;//是否使用阴影
            CBUFFER_END
            

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
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
                    pos=input.positionOS.xyz+input.SmoothNormal*camFactor*_OutlineWidth*input.Color.xyz;
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
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
