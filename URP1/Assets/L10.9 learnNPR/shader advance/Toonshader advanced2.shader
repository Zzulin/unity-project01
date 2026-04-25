Shader "Toon/Toonshader advanced2"
{
    Properties
    {
        [Main(Base, _, on, off)] _Base ("主要纹理", Float) = 0
        [Tex(Base)] _MainTex ("主纹理", 2D) = "" { }
        [Tex(Base)] _IlmMap ("Lightmap", 2D) = "" {}
        [Tex(Base)] _NormalMap ("NormalMap", 2D) = "bump" { }
        
        [Main(Albedo,_ALBEDO_ON,on)] _Albedo ("漫反射", Float) = 0
        [Tex(Albedo)] _RampColorMap ("色条图", 2D) = "" { }
        [SubToggle(Albedo)] _IsNight ("是否夜晚", Float) = 0
        [Sub(Albedo)] _Threshold ("NdotL Step", Range(0, 1)) = 0
        [Sub(Albedo)] _Hardness ("NdotL Smooth", Range(1, 50)) = 1
        [Sub(Albedo)] _grey ("Half Lambert Grey", Range(0.001, 1)) = 0.5
        [Sub(Albedo)] _dark ("Half Lambert Dark Offset", Range(-1, 1)) = 0
        [Sub(Albedo)] _bright ("Bright Lambert", Range(0.001, 1)) = 0.8
        [SubIntRange(Albedo)] _lightmapA0 ("lightmapA0", Range(1, 5)) = 1
        [SubIntRange(Albedo)] _lightmapA1 ("lightmapA1", Range(1, 5)) = 2
        [SubIntRange(Albedo)] _lightmapA2 ("lightmapA2", Range(1, 5)) = 3
        [SubIntRange(Albedo)] _lightmapA3 ("lightmapA3", Range(1, 5)) = 4
        [SubIntRange(Albedo)] _lightmapA4 ("lightmapA4", Range(1, 5)) = 5
        
        [Main(Specular,_SPECULAR_ON,on)] _Specular ("高光反射", Float) = 0
        [Sub(Specular)] _GlossBlinnMargin ("GlossBlinnMargin", Range(0, 1)) = 0.2
        [Sub(Specular)] _GlossStep ("GlossStep", Range(0, 1)) = 0.5
        [Sub(Specular)] _GlossIntensity ("GlossIntensity", Range(0, 8)) = 1
        [Sub(Specular)] _BlinnIntensity ("BlinnIntensity", Range(0, 8)) = 1
        [Sub(Specular)] _BlinnStep ("BlinnStep", Range(0, 1)) = 0.5
        [Sub(Specular)] _MetalIntensity ("MetalIntensity", Range(0, 8)) = 1
        [Tex(Specular)] _MetalMap ("MetalMap", 2D) = "gray" { }
        
        [Main(Emission,_EMISSION_ON,on)] _Emission ("自发光", Float) = 0
        [Sub(Emission)] _EmissionIntensity ("EmissionIntensity", Range(0, 2)) = 0.5

        [Main(Rim,_RIM_ON,on)] _Rim ("菲涅尔", Float) = 0
        [Sub(Rim)] _RimIntensity ("菲涅尔强度", Range(0, 2)) = 1
        [Sub(Rim)] _RimRadius ("菲涅尔半径", Range(0, 1)) = 1

        [Main(Face,_FACE_ON,on)] _Face ("是否面部", Float) = 0
        [Sub(Face)] _FaceColor ("面部颜色", Color) = (0, 0, 0, 1)
        
        [SubRange(Face)]_FaceDarkIntensity ("暗部暗度", Range(0, 1)) = 0.7
        [SubToggle(Face)] _IsConvertFaceCoord ("是否转换坐标", Float) = 0
        [SubRange(Face)]_FaceSmoothRange ("面部阴影Smooth", Range(0, 0.05)) = 0.03
        [Tex(Face)] _FaceLightMap ("面部SDF图", 2D) = "gray" { }
        
        [Main(Shadow,_SHADOW_ON,on)] _Shadow ("阴影", Float) = 0
        
        [Main(Outline,_OUTLINE_ON,on)] _Outline ("描边", Float) = 0
        [SubToggle(Outline)] _USE_SMOOTH_NORMAL ("启用平均化法线", Float) = 1
        [SubRange(Outline)] _OutlineWidth ("轮廓宽度", Range(0,3)) = 1
        [Sub(Outline)] _OutlineZOffset ("Outline Z Offset", Range(-0.1, 0.1)) = 0
        [Sub(Outline)] _OutlineColor ("轮廓颜色0", Color) = (0, 0, 0, 1)
        [Sub(Outline)] _outlineColor1 ("轮廓颜色1", Color) = (0, 0, 0, 1)
        [Sub(Outline)] _outlineColor2 ("轮廓颜色2", Color) = (0, 0, 0, 1)
        [Sub(Outline)] _outlineColor3 ("轮廓颜色3", Color) = (0, 0, 0, 1)
        [Sub(Outline)] _outlineColor4 ("轮廓颜色4", Color) = (0, 0, 0, 1)
        
        
        [Main(_NearDissolve,_NearDissolve_ON,on)] _NearDissolve ("启用近距离屏幕溶解", Float) = 0
        [Sub(_NearDissolve)] _NearDissolveStart ("开始溶解距离", Range(0.01, 3)) = 0.65
        [Sub(_NearDissolve)] _NearDissolveEnd ("完全溶解距离", Range(0.01, 3)) = 0.28
        [Sub(_NearDissolve)] _NearDissolvePatternScale ("溶解颗粒大小", Range(1, 12)) = 3
        [Sub(_NearDissolve)] _NearDissolveEdgeWidth ("溶解边缘宽度", Range(0.001, 0.35)) = 0.08
        [Sub(_NearDissolve)] _NearDissolveEdgeIntensity ("溶解边缘强度", Range(0, 4)) = 1.2
        [Sub(_NearDissolve)] _NearDissolveEdgeColor ("溶解边缘颜色", Color) = (0.35, 0.75, 1, 1)
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
        Pass
        {
            
            Name "ToonShading"
            Tags
            {
                "LightMode" = "UniversalForward"
                // "PassFlags" = "OnlyDirectional"
                // "RequireOptions" = "SoftVegetation"
            }

             /*Stencil // (Ref & ReadMask) Comp (StencilBufferValue & ReadMask)
             {
                 Ref [_StencilRef]
                 Comp Always
                 Pass Replace
             }*/

            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma shader_feature _ALBEDO_ON
            #pragma shader_feature _SPECULAR_ON
            #pragma shader_feature _EMISSION_ON
            #pragma shader_feature _FACE_ON
            
            #pragma shader_feature _RIM_ON
            #pragma shader_feature _OUTLINE_ON
            #pragma shader_feature _AVG_NORMAL_ON
            #pragma shader_feature _SHADOW_ON
            
          
            #pragma shader_feature _NORMALMAP_ON

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_IlmMap);
            SAMPLER(sampler_IlmMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RampColorMap);
            SAMPLER(sampler_RampColorMap);
            
            TEXTURE2D(_FaceLightMap);
            SAMPLER(sampler_FaceLightMap);
            TEXTURE2D(_MetalMap);
            SAMPLER(sampler_MetalMap);
            TEXTURE2D(_SpecMap);
            SAMPLER(sampler_SpecMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _OutlineWidth;
            float _OutlineZOffset;
            float3 _OutlineColor;
            float3 _outlineColor1;
            float3 _outlineColor2;
            float3 _outlineColor3;
            float3 _outlineColor4;

            float _DiffuseCutLocation;
            float _DiffuseCutSmoothness;
            float _IsNight;
            
            float _UseRimLight;
            float _RimIntensity;
            float _RimRadius;

            float _FaceDarkIntensity;
            float3 _FaceColor;
            float _IsConvertFaceCoord;
            float _UseSdfShadow;
            float _FaceSmoothRange;
            float _EmissionIntensity;
            
            float _UseVertexColor;
        
            float _Threshold;
            float _Hardness;
            float _grey;
            float _dark;
            float _bright;
            float _lightmapA0;
            float _lightmapA1;
            float _lightmapA2;
            float _lightmapA3;
            float _lightmapA4;
            
            
            float _GlossIntensity;
            float _MetalIntensity;
            float _BlinnIntensity;
            float _BlinnStep;
            float _GlossStep;
            float _GlossBlinnMargin;
            float _NearDissolve;
            float _NearDissolveStart;
            float _NearDissolveEnd;
            float _NearDissolvePatternScale;
            float _NearDissolveEdgeWidth;
            float _NearDissolveEdgeIntensity;
            float4 _NearDissolveEdgeColor;
            CBUFFER_END
            
            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL; 
                float4 tangentOS : TANGENT;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1; 
                float3 viewWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float3 posWS : TEXCOORD4;
                float3 tangentWS : TEXCOORD5;
                float3 binormalWS : TEXCOORD6;
                float4 color : TEXCOORD7;
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
                o.color = v.color;
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
                //计算“角色脸部正前方”的世界空间方向 frontWS
                //_isConvertFaceCoord float3(0, 1, 0)表示模型的 Y坐标 当做 Z坐标
                float3 frontWS = mul((float3x3)unity_ObjectToWorld,
                    lerp(float3(0, 0, 1), float3(0, 1, 0), _isConvertFaceCoord));

                // 原神角色模型局部空间和 Unity 世界空间不是对齐的，这里做坐标转换。
                //_isConvertFaceCoord float3(0, 0, -1)表示模型的 Z坐标的反方向 当做 X坐标
                float3 rightWS = mul((float3x3)unity_ObjectToWorld,
                    lerp(float3(1, 0, 0), float3(0, 0, -1), _isConvertFaceCoord));

                float FdotL = dot(normalize(frontWS.xz), normalize(L.xz));
                float RdotL = dot(normalize(rightWS.xz), normalize(L.xz));

                // 判断灯光方向是否在面部前方
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

                 //return step(faceLight, FdotL);
                float smoothRange = 0.03;
                return 1 - smoothstep(FdotL - _FaceSmoothRange, FdotL + _FaceSmoothRange, faceLight);
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
            float3 NPR_Base_Ramp2(float4 ilm, float NdotL ,float isnight)
            {
                ilm.g = smoothstep(0.2,0.3,ilm.g);//ao
                float halfLambert = smoothstep(0.0, _grey, NdotL + _dark) * ilm.g;  //半Lambert x  ao
                float brightMask = step(_bright, halfLambert); //亮面
                float rampSampling = 0.0;
                if(isnight == 0){rampSampling = 0.5;}
                
                float ramp0 = _lightmapA0 * -0.1 + 1.05 - rampSampling;
                float ramp1 = _lightmapA1 * -0.1 + 1.05 - rampSampling;  
                float ramp2 = _lightmapA2 * -0.1 + 1.05 - rampSampling;  
                float ramp3 = _lightmapA3 * -0.1 + 1.05 - rampSampling;  
                float ramp4 = _lightmapA4 * -0.1 + 1.05 - rampSampling;  
                
                float lightmapA2 = step(0.25, ilm.a); //0.3
                float lightmapA3 = step(0.45, ilm.a); //0.5
                float lightmapA4 = step(0.65, ilm.a); //0.7
                float lightmapA5 = step(0.95, ilm.a);//1.0
                
                float rampV = ramp0;                 //ilm.a <0.25 默认值为 ramp0
                rampV = lerp(rampV,ramp1,lightmapA2);//ilm.a >0.25 的时为 ramp1
                rampV = lerp(rampV,ramp2,lightmapA3);//ilm.a >0.45 的时为 ramp2
                rampV = lerp(rampV,ramp3,lightmapA4);//ilm.a >0.65 的时为 ramp3
                rampV = lerp(rampV,ramp4,lightmapA5);//ilm.a >0.95 的时为 ramp4
                
                float3 ramp = SAMPLE_TEXTURE2D(_RampColorMap, sampler_RampColorMap, float2(halfLambert, rampV)).rgb;
                float3 shadowRamp = lerp(ramp,halfLambert,brightMask);
                //return float3(ilm.a,ilm.a,ilm.a);
                //return float3(halfLambert,halfLambert,halfLambert);
                //return float3(brightMask,brightMask,brightMask);
                //return float3(ramp);
                return shadowRamp;
                
            }

            float NPR_Base_Metallic(float3 normalWS)
            {
                // 把世界空间法线 normalWS 转到视图空间
                float3 normalizeVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
                //用视图空间法线的 xy 生成MatCap UV
                float2 matcapUV = normalizeVS.xy * 0.5 + 0.5; // 金属镜面采样 UV
                return SAMPLE_TEXTURE2D(_MetalMap, sampler_MetalMap, matcapUV).r;
            }
            
            float3 NPR_Base_Specular(
                float NdotH,
                float NdotV,
                float3 normalWS,
                float3 baseColor,
                float specLayerMask, // ilm.r
                float specIntensity, // ilm.b（图像素）
                float aoMask // ilm.g (aoMask)
            )
            {
                float GlossLayerMask = step(specLayerMask, _GlossBlinnMargin);// specLayerMask<= _GlossBlinnMargin 时为1

                float BlinnLayerMask = step(_GlossBlinnMargin, specLayerMask);

                // 模拟视角高光
                float GlossStep = step(_GlossStep, NdotV) * specIntensity * GlossLayerMask * _GlossIntensity;

                // Blinn-Phong 高光
                float BlinnStep = step(_BlinnStep, pow(NdotH, specIntensity)) * BlinnLayerMask * _BlinnIntensity;

                float MetalFactor = NPR_Base_Metallic(normalWS) * _MetalIntensity; // 截断高光
                //return float3(MetalFactor,MetalFactor,MetalFactor);
                return (BlinnStep + GlossStep + MetalFactor) * aoMask * baseColor;
            }
            
            float3 NPR_Base_RimLight(float NdotV, float3 baseColor)
            {
                return step(1 - _RimRadius, 1 - saturate(NdotV)) * _RimIntensity * baseColor;
            }

            float3 NPR_Emission(float4 baseColor)
            {
                return max(0,baseColor.a * baseColor.rgb * _EmissionIntensity * (sin(_Time.z * 0.5) + 0.5));
            }

            float NearDissolveHash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float NearDissolveBayer(float2 pixel)
            {
                static const float4 thresholds[4] =
                {
                    float4(01.0 / 17.0, 09.0 / 17.0, 03.0 / 17.0, 11.0 / 17.0),
                    float4(13.0 / 17.0, 05.0 / 17.0, 15.0 / 17.0, 07.0 / 17.0),
                    float4(04.0 / 17.0, 12.0 / 17.0, 02.0 / 17.0, 10.0 / 17.0),
                    float4(16.0 / 17.0, 08.0 / 17.0, 14.0 / 17.0, 06.0 / 17.0)
                };

                uint xIndex = (uint)fmod(pixel.x, 4);
                uint yIndex = (uint)fmod(pixel.y, 4);
                return thresholds[yIndex][xIndex];
            }

            float ApplyNearCameraDissolve(float4 positionCS, float3 positionWS)
            {
                if (_NearDissolve < 0.5)
                {
                    return 0;
                }

                float dissolveStart = max(_NearDissolveStart, _NearDissolveEnd + 0.001);
                float viewDistance = distance(positionWS, _WorldSpaceCameraPos.xyz);
                float dissolveAlpha = saturate((viewDistance - _NearDissolveEnd) / (dissolveStart - _NearDissolveEnd));

                float2 pixel = floor(positionCS.xy / max(_NearDissolvePatternScale, 1.0));
                float orderedThreshold = NearDissolveBayer(pixel);
                float organicThreshold = NearDissolveHash(pixel * 0.73 + floor(viewDistance * 16.0));
                float threshold = lerp(orderedThreshold, organicThreshold, 0.35);

                clip(dissolveAlpha - threshold);

                float edge = 1.0 - smoothstep(0.0, max(_NearDissolveEdgeWidth, 0.001), abs(dissolveAlpha - threshold));
                return edge * step(dissolveAlpha, 0.995);
            }

            float4 frag(v2f i) : SV_Target
            {
                float nearDissolveEdge = ApplyNearCameraDissolve(i.pos, i.posWS);

                Light mainLight;
                float4 shadowCoord=TransformWorldToShadowCoord(i.posWS);//必须在fs中计算shadowcoord
                //shadowCoord = i.shadowCoord;
                mainLight = GetMainLight(shadowCoord);
                
                float3 n;
                #if _NORMALMAP_ON
                    float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
                    n = TransformTangentToWorld(normalTS, half3x3(i.tangentWS, i.binormalWS, i.normalWS));
                #else
                    n = normalize(i.normalWS);
                #endif
                
                
                float3 v = normalize(i.viewWS);//顶点计算View方向
                 //v = SafeNormalize(_WorldSpaceCameraPos.xyz - i.posWS);像素计算View方向
                float3 l = mainLight.direction;
                float3 h = SafeNormalize(l + v);

                // blinn
                float nl = dot(n, l); 
                float nh = dot(n, h); 
                float nv = dot(n, v); // 外发光相关菲涅尔
                
                float selfShadow = saturate(0.3+mainLight.shadowAttenuation*mainLight.distanceAttenuation);
                float darkArea = saturate((nl-_Threshold)*_Hardness);//[0,2]->[-0.5,1.5]
                darkArea = lerp(0.5, 1.0, darkArea);

                float shadowAtt = 1;
                #if _SHADOW_ON
                    shadowAtt = selfShadow*darkArea;
                #else
                    shadowAtt = 1;
                #endif
                //return float4(shadowAtt,shadowAtt,shadowAtt, 1);
                float4 ilm = SAMPLE_TEXTURE2D(_IlmMap, sampler_IlmMap, i.uv);
                float rampRange = ilm.a;
                float aoMask = ilm.g; // 角色的遮蔽范围(AO)
                float specLayerMask = ilm.r;
                float specIntensity = ilm.b;

                // 对纹理进行采样。
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv); // baseColor.a 是星星角度

                float3 albedoFinal = 0;
                #if _ALBEDO_ON
                    float3 rampColor;
                    #if _FACE_ON
                        //rampColor = NPR_Base_Ramp(nl, _IsNight, rampRange);
                        rampColor = SAMPLE_TEXTURE2D(_RampColorMap, sampler_RampColorMap, i.uv);//脸ramp是个单色
                        albedoFinal = baseColor.rgb * rampColor * lerp(_FaceDarkIntensity, 1,
                        FaceShadowAttenuation(l, i.uv, _IsConvertFaceCoord));
                        // 面部阴影不接受实时光。
                    #else
                        rampColor = NPR_Base_Ramp(nl, _IsNight, rampRange);
                        rampColor = NPR_Base_Ramp2(ilm,nl,_IsNight);
                        albedoFinal = baseColor.rgb * rampColor * shadowAtt;
                    #endif
                //return float4 (rampColor ,1);
                /*#else
                    albedoFinal = baseColor.rgb;*/
                #endif

                float3 specFinal = 0;
                #if _SPECULAR_ON
                    
                        specFinal = NPR_Base_Specular(nh, nv, n, baseColor.rgb,
                        specLayerMask, specIntensity, aoMask)* shadowAtt;
                        //return float4(specFinal, 1);
                    
                #endif

                // return float4(specFinal, 1);

                float3 rimFinal = 0;
                #if _RIM_ON
                    rimFinal = NPR_Base_RimLight(nv, baseColor.rgb * shadowAtt);
                #endif

                float3 emissionFinal = 0;
                #if _EMISSION_ON
                    emissionFinal = NPR_Emission(baseColor);
                //return float4(emissionFinal, 1);
                #endif
                //return  float4(ilm.a,ilm.a,ilm.a,1);
                float3 nearDissolveEdgeColor = _NearDissolveEdgeColor.rgb * _NearDissolveEdgeIntensity * nearDissolveEdge;
                return float4(albedoFinal + specFinal + rimFinal + emissionFinal + nearDissolveEdgeColor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Outline"}
            Cull front
            blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual

            /*Stencil
            {
                Ref [_StencilRef]
                Comp NotEqual
                Pass Replace
            }*/
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_SMOOTH_NORMAL_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_IlmMap);
            SAMPLER(sampler_IlmMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RampColorMap);
            SAMPLER(sampler_RampColorMap);
            
            TEXTURE2D(_FaceLightMap);
            SAMPLER(sampler_FaceLightMap);
            TEXTURE2D(_MetalMap);
            SAMPLER(sampler_MetalMap);
            TEXTURE2D(_SpecMap);
            SAMPLER(sampler_SpecMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _OutlineWidth;
                float _OutlineZOffset;
                float3 _OutlineColor;
                float3 _outlineColor1;
                float3 _outlineColor2;
                float3 _outlineColor3;
                float3 _outlineColor4;

                float _DiffuseCutLocation;
                float _DiffuseCutSmoothness;
                float _IsNight;

                float _UseRimLight;
                float _RimIntensity;
                float _RimRadius;

                float _FaceDarkIntensity;
                float3 _FaceColor;
                float _IsConvertFaceCoord;
                float _UseSdfShadow;
                float _FaceSmoothRange;
                float _EmissionIntensity;

                float _UseVertexColor;
            
                float _Threshold;
                float _Hardness;
                float _grey;
                float _dark;
                
                float _GlossIntensity;
                float _MetalIntensity;
                float _BlinnIntensity;
                float _BlinnStep;
                float _GlossStep;
                float _GlossBlinnMargin;
                float _NearDissolve;
                float _NearDissolveStart;
                float _NearDissolveEnd;
                float _NearDissolvePatternScale;
                float _NearDissolveEdgeWidth;
                float _NearDissolveEdgeIntensity;
                float4 _NearDissolveEdgeColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float3 SmoothNormal : TEXCOORD7;
                float4 Color : COLOR;
                float4 tangentOS : TANGENT;
            };
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float outlineMask : TEXCOORD2;
                float4 Color : COLOR;
                float3 positionWS : TEXCOORD3;
            };
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.uv = input.uv;

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                float4 positionCS = vertexInputs.positionCS;

                #if _USE_SMOOTH_NORMAL_ON
                    float3 bitangentOS = cross(input.normalOS, input.tangentOS.xyz) * input.tangentOS.w;
                    float3x3 tangentToObject = float3x3(input.tangentOS.xyz, bitangentOS, input.normalOS);//tbn
                    float3 outlineNormalOS = normalize(mul(input.SmoothNormal.xyz, tangentToObject));//tbn将裁剪空间的smooth法线转到Object空间
                #else
                    float3 outlineNormalOS = input.normalOS;
                #endif
                
                //将轮廓法线从对象空间变换到观察空间（逆转模型-视图矩阵） 正常的法线空间转换到view空间
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, outlineNormalOS);
                
                viewNormal.z = -0.1;// View Space 的 z 方向做一点深度偏移
                float outlineAmount = _OutlineWidth * 0.01 * input.Color.a;
                float viewZOffset = normalize(viewNormal).z * outlineAmount + _OutlineZOffset;
                //先在View Space移动z 再转换到 Clip Space
                positionCS = TransformWViewToHClip(vertexInputs.positionVS + float3(0, 0, viewZOffset));
 
                
                //这行是在把 View Space 法线方向 通过投影矩阵转换成一个 可以加到 positionCS.xy 上的屏幕/裁剪空间偏移方向。
                float3 projectedNormal = mul((float3x3)UNITY_MATRIX_P, viewNormal.xyz) * positionCS.w;
                //修正描边正确宽度 没有修正会因为NDC空间是1：1的 屏幕显示不是1：1的 导致描边的上下宽度与左右宽度不普配 
                //将近裁剪面右上角位置的顶点变换到观察空间
                float4 nearUpperRight = mul(unity_CameraInvProjection, float4(1, 1, UNITY_NEAR_CLIP_VALUE, _ProjectionParams.y));  
                float aspect = abs(nearUpperRight.y / nearUpperRight.x);//求得屏幕宽高比
                projectedNormal.x *= aspect;
                
                //扩宽描边
                positionCS.xy += projectedNormal.xy * outlineAmount;
                o.positionCS = positionCS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.outlineMask = outlineAmount;
                o.Color = input.Color;
                o.positionWS = TransformViewToWorld(vertexInputs.positionVS + float3(0, 0, viewZOffset));
                return o;
            }

            float NearDissolveHash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float NearDissolveBayer(float2 pixel)
            {
                static const float4 thresholds[4] =
                {
                    float4(01.0 / 17.0, 09.0 / 17.0, 03.0 / 17.0, 11.0 / 17.0),
                    float4(13.0 / 17.0, 05.0 / 17.0, 15.0 / 17.0, 07.0 / 17.0),
                    float4(04.0 / 17.0, 12.0 / 17.0, 02.0 / 17.0, 10.0 / 17.0),
                    float4(16.0 / 17.0, 08.0 / 17.0, 14.0 / 17.0, 06.0 / 17.0)
                };

                uint xIndex = (uint)fmod(pixel.x, 4);
                uint yIndex = (uint)fmod(pixel.y, 4);
                return thresholds[yIndex][xIndex];
            }

            float ApplyNearCameraDissolve(float4 positionCS, float3 positionWS)
            {
                if (_NearDissolve < 0.5)
                {
                    return 0;
                }

                float dissolveStart = max(_NearDissolveStart, _NearDissolveEnd + 0.001);
                float viewDistance = distance(positionWS, _WorldSpaceCameraPos.xyz);
                float dissolveAlpha = saturate((viewDistance - _NearDissolveEnd) / (dissolveStart - _NearDissolveEnd));

                float2 pixel = floor(positionCS.xy / max(_NearDissolvePatternScale, 1.0));
                float orderedThreshold = NearDissolveBayer(pixel);
                float organicThreshold = NearDissolveHash(pixel * 0.73 + floor(viewDistance * 16.0));
                float threshold = lerp(orderedThreshold, organicThreshold, 0.35);

                clip(dissolveAlpha - threshold);

                float edge = 1.0 - smoothstep(0.0, max(_NearDissolveEdgeWidth, 0.001), abs(dissolveAlpha - threshold));
                return edge * step(dissolveAlpha, 0.995);
            }

            half4 frag (Varyings input) : SV_Target
            {
                //当 outlineMask < 0.0001时（即非轮廓区域），片元被 discard
                clip(input.outlineMask - 0.0001);
                float nearDissolveEdge = ApplyNearCameraDissolve(input.positionCS, input.positionWS);
                //采样贴图
                float4 lightmap = SAMPLE_TEXTURE2D(_IlmMap, sampler_IlmMap, input.uv).rgba;
                //分离lightmap.a各材质
                float lightmapA2 = step(0.25, lightmap.a);  //0.3
                float lightmapA3 = step(0.45, lightmap.a);  //0.5
                float lightmapA4 = step(0.65, lightmap.a);  //0.7
                float lightmapA5 = step(0.95, lightmap.a);  //1.0
                //重组lightmap.a
                float3 outlineColor = _OutlineColor;  //0.0
                outlineColor = lerp(outlineColor, _outlineColor1, lightmapA2);  //0.3
                outlineColor = lerp(outlineColor, _outlineColor2, lightmapA3);  //0.5
                outlineColor = lerp(outlineColor, _outlineColor3, lightmapA4);  //0.7
                outlineColor = lerp(outlineColor, _outlineColor4, lightmapA5);  //1.0
                outlineColor += _NearDissolveEdgeColor.rgb * _NearDissolveEdgeIntensity * nearDissolveEdge;
                return half4(outlineColor, 1.0);
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
    CustomEditor "LWGUI.LWGUI"
}
