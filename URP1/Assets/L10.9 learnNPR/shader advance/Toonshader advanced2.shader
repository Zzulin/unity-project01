Shader "Toon/Toonshader advanced2"
{
    Properties
    {
        [Main(Base, _, on, off)] _Base ("基础", Float) = 0
        [Tex(Base)] _MainTex ("主纹理", 2D) = "" { }
        [Tex(Base)] _IlmMap ("IlmMap", 2D) = "" { }
        [Tex(Base)] _NormalMap ("NormalMap", 2D) = "bump" { }

        [Main(Outline)] _Outline ("描边", Float) = 0
        [SubIntRange(Outline)] _OutlineWidth ("轮廓宽度", Range(1, 10)) = 1
        [SubToggle(Outline)] _PixelWidth ("使用屏幕像素宽度(Game视图)", Float) = 1
        [Sub(Outline)] _OutlineColor ("轮廓颜色", Color) = (0, 0, 0, 1)
        [SubIntRange(Outline)] _StencilRef ("描边ID", Range(1, 8)) = 1
        [SubToggle(Outline)] _AVG_NORMAL ("启用平均化法线", Float) = 1
        [SubToggle(Outline)] _VERTEX_COLOR ("使用顶点色", Float) = 1
        [SubToggle(Outline)] _VERTEX_COLOR_MAP ("使用顶点色贴图", Float) = 1
        [ShowIf(_VERTEX_COLOR_MAP, Equal, 1)][Tex(Outline)] _VertexColorMap ("顶点色贴图", 2D) = "white" { }

        [Main(Albedo)] _Albedo ("基础色", Float) = 0
        [Tex(Albedo)] _RampColorMap ("色条图", 2D) = "white" { }
        [SubToggle(Albedo)] _IsNight ("是否夜晚", Float) = 0
        [Sub(Albedo)] _Threshold ("明暗分界阈值", Range(0, 1)) = 0
        [Sub(Albedo)] _Hardness ("硬度", Range(1, 50)) = 1

        [Main(Specular)] _Specular ("高光", Float) = 0
        [Sub(Specular)] _GlossBlinnMargin ("GlossBlinnMargin", Range(0, 1)) = 0.2
        [Sub(Specular)] _GlossStep ("GlossStep", Range(0, 1)) = 0.5
        [Sub(Specular)] _GlossIntensity ("GlossIntensity", Range(0, 8)) = 1
        [Sub(Specular)] _BlinnIntensity ("BlinnIntensity", Range(0, 8)) = 1
        [Sub(Specular)] _BlinnStep ("BlinnStep", Range(0, 1)) = 0.5
        [Sub(Specular)] _MetalIntensity ("MetalIntensity", Range(0, 8)) = 1
        [Tex(Specular)] _MetalMap ("MetalMap", 2D) = "gray" { }
        
        [Main(Emission)] _Emission ("自发光", Float) = 0
        [Sub(Emission)] _EmissionIntensity ("EmissionIntensity", Range(0, 2)) = 0.5

        [Main(Rim)] _Rim ("外发光", Float) = 0
        [Sub(Rim)] _RimIntensity ("外发光强度", Range(0, 2)) = 1
        [Sub(Rim)] _RimRadius ("外发光半径", Range(0, 1)) = 1

        [Main(Face)] _Face ("面部", Float) = 0
        [SubToggle(Face)] _IsConvertFaceCoord ("是否转换坐标", Float) = 0
        [Tex(Face)] _FaceLightMap ("面部SDF图", 2D) = "gray" { }

        [Main(Hair)] _Hair ("头发", Float) = 0
        [Sub(Hair)] _AnisoPower ("头发高光集中度", Range(1, 4)) = 1
        [Tex(Hair)] _ShiftMap ("发丝贴图", 2D) = "" { }

        [Main(Shadow)] _Shadow ("阴影", Float) = 0
        [SubToggle(Shadow)] _SHADOW_OPT ("阴影优化", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            // "DisableBatching" = "True"
            // "ForceNoShadowCasting" = "True"
            // "IgnoreProjector" = "True"
            // "CanUseSpriteAtlas" = "False"
            // "PreviewType" = "Plane/Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        #pragma shader_feature _ALBEDO_ON
        #pragma shader_feature _SPECULAR_ON
        #pragma shader_feature _EMISSION_ON
        #pragma shader_feature _FACE_ON
        #pragma shader_feature _HAIR_ON
        #pragma shader_feature _RIM_ON
        #pragma shader_feature _OUTLINE_ON
        #pragma shader_feature _AVG_NORMAL_ON
        #pragma shader_feature _VERTEX_COLOR_ON
        #pragma shader_feature _SHADOW_ON
        #pragma shader_feature _SHADOW_OPT_ON
        #pragma shader_feature _VERTEX_COLOR_MAP_ON
        #pragma shader_feature _NORMALMAP_ON

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_IlmMap);
        SAMPLER(sampler_IlmMap);
        TEXTURE2D(_NormalMap);
        SAMPLER(sampler_NormalMap);
        TEXTURE2D(_VertexColorMap);
        SAMPLER(sampler_VertexColorMap);// 区块暗部颜色贴图
        TEXTURE2D(_RampColorMap);
        SAMPLER(sampler_RampColorMap);
        TEXTURE2D(_ShiftMap);
        SAMPLER(sampler_ShiftMap);
        TEXTURE2D(_FaceLightMap);
        SAMPLER(sampler_FaceLightMap);
        TEXTURE2D(_MetalMap);
        SAMPLER(sampler_MetalMap);
        TEXTURE2D(_SpecMap);
        SAMPLER(sampler_SpecMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _OutlineWidth;
            float3 _OutlineColor;
            float _PixelWidth;

            float _DiffuseCutLocation;
            float _DiffuseCutSmoothness;
            float _IsNight;
            float _AnisoPower;

            float _UseRimLight;
            float _RimIntensity;
            float _RimRadius;

            float _IsConvertFaceCoord;
            float _UseSdfShadow;
        
            float _EmissionIntensity;

            float _UseVertexColor;
        
            float _Threshold;
            float _Hardness;
            
            float _GlossIntensity;
            float _MetalIntensity;
            float _BlinnIntensity;
            float _BlinnStep;
            float _GlossStep;
            float _GlossBlinnMargin;
        
        CBUFFER_END
        ENDHLSL
        Pass
        {
            Name "ToonShading"
            Tags
            {
                "LightMode" = "UniversalForward"
                // "PassFlags" = "OnlyDirectional"
                // "RequireOptions" = "SoftVegetation"
            }

            // Stencil // (Ref & ReadMask) Comp (StencilBufferValue & ReadMask)
            // {
            //     Ref [_StencilRef]
            //     Comp Always
            //     Pass Replace
            // }

            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            #include "Poisson.hlsl"
            
            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL; 
                float4 tangentOS : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1; 
                float3 viewWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float3 posWS : TEXCOORD4;
                float3 tangentWS : TANGENT;
                float3 binormalWS : BTANGEN;
            };

            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.posWS = TransformObjectToWorld(v.vertex);
                o.viewWS = normalize(_WorldSpaceCameraPos - o.posWS);

                VertexNormalInputs vm = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.normalWS = vm.normalWS;
                o.tangentWS = vm.tangentWS;
                o.binormalWS = vm.bitangentWS; // 左手规则

                o.shadowCoord = TransformWorldToShadowCoord(o.posWS);
                return o;
            }

            float StrandSpecular(float3 T, float3 H, float exponent)
            {
                float dotTH = dot(T, H);
                float sinTH = sqrt(1.0 - dotTH * dotTH);
                float dirAtten = smoothstep(-1.0, 0.0, dotTH);

                return dirAtten * pow(sinTH, exponent);
            }

            /*float3 shiftTangent(float3 T, float3 N, float shift)
            {
                return normalize(T + shift * N);
            }*/

            float FaceShadowAttenuation(float3 L, float2 uv, float _isConvertFaceCoord)
            {
                // ref: https://zhuanlan.zhihu.com/p/279334552
                float3 frontWS = mul((float3x3)unity_ObjectToWorld,
                    lerp(float3(0, 0, 1), float3(0, 1, 0), _isConvertFaceCoord));

                // 原神角色模型局部空间和 Unity 世界空间不是对齐的，这里做坐标转换。
                float3 rightWS = mul((float3x3)unity_ObjectToWorld,
                    lerp(float3(1, 0, 0), float3(0, 0, -1), _isConvertFaceCoord));

                float FdotL = dot(normalize(frontWS.xz), normalize(L.xz));
                float RdotL = dot(normalize(rightWS.xz), normalize(L.xz));

                // 左右各采样一次 FaceLightMap 的明暗数据存于 FaceLight。
                if (FdotL < 0)
                {
                    return 0;
                }

                float faceLight;
                if (RdotL > 0) // 灯光位于面部右侧
                {
                    faceLight = SAMPLE_TEXTURE2D(_FaceLightMap, sampler_FaceLightMap, float2(uv.x, uv.y)).r;
                }
                else
                {
                    faceLight = SAMPLE_TEXTURE2D(_FaceLightMap, sampler_FaceLightMap, float2(1 - uv.x, uv.y)).r;
                }

                // return step(faceLight, FdotL);
                float smoothRange = 0.03;
                return 1 - smoothstep(FdotL - smoothRange, FdotL + smoothRange, faceLight);
            }

            float NPR_Toon_Shading(float NdotL)
            {
                half diff = NdotL; // min(NdotL, shadowAtt);
                half diffuseMin = saturate(_DiffuseCutLocation - _DiffuseCutSmoothness);
                half diffuseMax = saturate(_DiffuseCutLocation + _DiffuseCutSmoothness);

                if (diff < diffuseMin)
                {
                    return diff;
                }
                else if (diff > diffuseMax)
                {
                    return diff;
                }
                else
                {
                    return smoothstep(diffuseMin, diffuseMax, diff) * (diffuseMax - diffuseMin) + diffuseMin;
                }
            }

            float3 NPR_Base_Ramp(float NdotL, float Night, float rampRange)
            {
                float3 rampFinal;
                float2 uv;
                uv.x = smoothstep(0, 1, NdotL);
                uv.y = 0.05 + rampRange * 0.4 + Night * 0.5; // 这里采用 openGL 的 uv 坐标约定
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1 - uv.y; // 转换到 dx 约定
                #endif

                rampFinal = SAMPLE_TEXTURE2D(_RampColorMap, sampler_RampColorMap, uv).rgb;

                // float2 uvRtShadow = uvRampColor;
                // uvRtShadow.x = lerp(0.9, 0.99, rtShadow);
                // rampFinal *= SAMPLE_TEXTURE2D(_RampColorMap, sampler_RampColorMap, uvRtShadow).rgb;

                // return float3(uv, 0);
                return rampFinal;
            }

            float NPR_Base_Metallic(float3 normalWS)
            {
                float3 normalizeVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
                float2 matcapUV = normalizeVS.xy * 0.5 + 0.5; // 金属镜面采样 UV
                return SAMPLE_TEXTURE2D(_MetalMap, sampler_MetalMap, matcapUV).r;
            }

            /*float3 NPR_Base_Specular(
                float NdotH,
                float3 normalWS,
                float3 baseColor,
                float specLayerMask, // ilm.r
                float specIntensity, // ilm.b
                float aoMask // ilm.g (aoMask)
            )
            {
                float3 blinnPhongSpec = baseColor * specLayerMask * saturate(pow(NdotH, specIntensity));
                // Blinn-Phong 高光

                float isMetal = step(0.95, specLayerMask); // 镜面金属区域
                float metalFactor = lerp(0, NPR_Base_Metallic(specLayerMask, normalWS) * 5, isMetal);
                float3 stepSpec = baseColor * metalFactor * step(0.95, specLayerMask); // 截断高光

                return (blinnPhongSpec + stepSpec) * aoMask;
            }*/

            float3 NPR_Base_Specular2(
                float NdotH,
                float NdotV,
                float3 normalWS,
                float3 baseColor,
                float specLayerMask, // ilm.r
                float specIntensity, // ilm.b（图像素）
                float aoMask // ilm.g (aoMask)
            )
            {
                float GlossLayerMask = step(specLayerMask, _GlossBlinnMargin);
                float BlinnLayerMask = step(_GlossBlinnMargin, specLayerMask);

                // 模拟视角高光
                float GlossStep = step(_GlossStep, NdotV) * specIntensity * GlossLayerMask * _GlossIntensity;

                // Blinn-Phong 高光
                float BlinnStep = step(_BlinnStep, pow(NdotH, specIntensity)) * BlinnLayerMask * _BlinnIntensity;

                float MetalFactor = NPR_Base_Metallic(normalWS) * _MetalIntensity; // 截断高光

                return (BlinnStep + GlossStep + MetalFactor) * aoMask * baseColor;
            }

            float3 NPR_Base_RimLight(float NdotV, float NdotL, float3 baseColor)
            {
                // 截边外发光
                return (1 - smoothstep(_RimRadius, _RimRadius + 0.03, NdotV)) * _RimIntensity * (1 - NdotL) * baseColor;
            }

            float3 NPR_Base_RimLight2(float NdotV, float3 baseColor)
            {
                // 截边外发光：canvas 中 rimlight2 注释指向的替代版本。
                return step(1 - _RimRadius, 1 - saturate(NdotV)) * _RimIntensity * baseColor;
            }

            float3 NPR_Emission(float4 baseColor)
            {
                return baseColor.a * baseColor.rgb * _EmissionIntensity * (sin(_Time.z * 0.5) + 0.5);
            }

            float4 frag(v2f i) : SV_Target
            {
                Light mainLight;
                #if _SHADOW_OPT_ON
                    mainLight = get_main_light_poisson(i.shadowCoord, i.posWS);
                #else
                    mainLight = GetMainLight(i.shadowCoord);
                #endif

                float3 n;
                #if _NORMALMAP_ON
                    float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
                    n = TransformTangentToWorld(normalTS, half3x3(i.tangentWS, i.binormalWS, i.normalWS));
                #else
                    n = normalize(i.normalWS);
                #endif

                float3 v = normalize(i.viewWS);
                float3 l = mainLight.direction;
                float3 h = SafeNormalize(l + v);

                // 用于二次元的 blinn。
                float nl = dot(n, l) * 0.5 + 0.5; // 背面 [0,0.5]，正面 [0.5,1]
                float nh = dot(n, h); // BlinnPhong 高光定义相关
                float nv = dot(n, v); // 视线相关
                
                float selfShadow = saturate(0.3+mainLight.shadowAttenuation*mainLight.distanceAttenuation);
                float darkArea = saturate((nl-_Threshold)*_Hardness);//[0,2]->[-0.5,1.5]
                darkArea = lerp(0.5, 1.0, darkArea);

                float shadowAtt = 1;
                #if _SHADOW_ON
                    shadowAtt = darkArea * selfShadow;
                #else
                shadowAtt = 1;
                #endif

                float4 ilm = SAMPLE_TEXTURE2D(_IlmMap, sampler_IlmMap, i.uv);
                float rampRange = 1 - ilm.a;
                float aoMask = ilm.g; // 角色的遮蔽范围(AO)
                float specLayerMask = ilm.r;
                float specIntensity = ilm.b;

                // 对纹理进行采样。
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv); // baseColor.a 是星星角度

                float3 albedoFinal = 0;
                #if _ALBEDO_ON
                    float3 rampColor;
                    #if _FACE_ON
                        rampColor = NPR_Base_Ramp(nl, _IsNight, rampRange);
                        albedoFinal = baseColor.rgb * rampColor * lerp(0.7, 1,
                            FaceShadowAttenuation(l, i.uv, _IsConvertFaceCoord));
                        // 面部阴影不接受实时光。
                    #else
                        rampColor = NPR_Base_Ramp(nl, _IsNight, rampRange);
                        albedoFinal = baseColor.rgb * rampColor * shadowAtt;
                    #endif

                    // 截图第 366 行有一条调试返回：return float4(rampColor, 1);
                #else
                    albedoFinal = baseColor.rgb;
                #endif

                float3 specFinal = 0;
                #if _SPECULAR_ON
                    #if _HAIR_ON
                        // 是否开启各向异性高光。
                        float3 t = normalize(i.binormalWS); // 取片元的法线数据
                        float shift = SAMPLE_TEXTURE2D(_ShiftMap, sampler_ShiftMap, i.uv).r * 2 - 1; // 模型发丝偏移效果
                        t = shiftTangent(t, n, shift);
                        specFinal = StrandSpecular(t, h, _AnisoPower); // 天使环高光衰减系数
                    #else
                        specFinal = NPR_Base_Specular2(nh, nv, n, baseColor.rgb,
                            specLayerMask, specIntensity, aoMask);
                    #endif
                #endif

                // return float4(specFinal, 1);

                float3 rimFinal = 0;
                #if _RIM_ON
                    rimFinal = NPR_Base_RimLight2(nv, baseColor.rgb * shadowAtt);
                #endif

                float3 emissionFinal = 0;
                #if _EMISSION_ON
                    emissionFinal = NPR_Emission(baseColor);
                #endif

                return float4(albedoFinal + specFinal + rimFinal + emissionFinal, 1);
            }
            ENDHLSL
        }

        /*Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
                // "PassFlags" = "OnlyDirectional"
                // "RequireOptions" = "SoftVegetation"
            }

            Cull Front

            HLSLPROGRAM
            #pragma vertex outline_vert
            #pragma fragment outline_frag

            v2f outline_vert(appdata v)
            {
                v2f o;

                float camDistance = length(_WorldSpaceCameraPos - mul(GetObjectToWorldMatrix(), float4(v.vertex, 1.0)).xyz);
                camDistance = lerp(1, camDistance, _PixelWidth);
                float camFactor = camDistance * _ProjectionParams.w; // 透视相机远调节系数

                float3 outDir;
                #if _AVG_NORMAL_ON
                    outDir = v.avgNormal;
                #else
                    outDir = v.normalOS;
                #endif

                float outLength;
                #if _VERTEX_COLOR_MAP_ON
                    float4 vColorMap = SAMPLE_TEXTURE2D_LOD(_VertexColorMap, sampler_VertexColorMap, v.uv, 0); // need vs 4.0
                    outLength = vColorMap.a;
                #else
                    outLength = v.color.a;
                #endif

                outDir *= _OutlineWidth * camFactor * outLength;

                float3 pos = v.vertex + outDir; // Object Space
                o.pos = TransformObjectToHClip(pos); // Clip Space
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;

                return o;
            }

            float4 outline_frag(v2f i) : SV_Target
            {
                #if !_OUTLINE_ON
                    discard;
                #endif

                #if _VERTEX_COLOR_ON
                    #if _VERTEX_COLOR_MAP_ON
                        float4 vColorMap = SAMPLE_TEXTURE2D(_VertexColorMap, sampler_VertexColorMap, i.uv);
                        return float4(vColorMap.rgb, 1);
                    #else
                        return float4(i.color.rgb, 1);
                    #endif
                #else
                    return float4(_OutlineColor, 1);
                #endif
            }
            ENDHLSL
        }*/

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
    CustomEditor "LWGUI.LWGUI"
}
