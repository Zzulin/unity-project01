Shader "Unlit/SSS"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _SpecularPower("Specular Power", Float) = 10
        _NormalMap("法线贴图", 2D) = "bump" {}
        _NormalMapScale("法线强度", Range(0, 2)) = 1

        [Toggle(_SSS_ON)]_SSS("是否开启SSS", Float) = 1
        _Distortion("法线扰动背光", Range(0,1)) = 0.5
        _BehindPower("背光pow", Range(0,10)) = 1
        _BehindStrenth("背光强度", Range(1,4)) = 1
        _BehindAmbient("背光环境", Range(0,1)) = 0.5
        _Thickness("厚度图", 2D) = "white" {}
        _ThicknessScale("厚度缩放", Range(0, 1)) = 1
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
            #pragma shader_feature _ _SSS_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
                float3 tangentWS    : TEXCOORD3;
                float3 bitangentWS   : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _SpecularPower;
                half _Distortion;
                half _BehindPower;
                half _BehindStrenth;
                half _BehindAmbient;
                half _NormalMapScale;
                half _ThicknessScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Thickness);
            SAMPLER(sampler_Thickness);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);


            Varyings vert (Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = TransformObjectToWorld(input.tangentOS.xyz);
                // 乘以 tangent.w 分量保证副切线方向一致（处理模型对称情况）
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }
            half3 LightingSSS(half3 LightDir,half3 mainLightColor, half3 N, 
            half3 V,half thickness,half NdotL,half3 H)
            {
                half3 Ldistort = LightDir + N * _Distortion;
                half backintensity = pow(saturate(dot(V, -Ldistort)), _BehindPower) * _BehindStrenth + _BehindAmbient;
                half3 diffuse = NdotL * mainLightColor * _BaseColor.rgb;
                half3 specular = pow(saturate(dot(N, H)), _SpecularPower) * mainLightColor * _BaseColor.rgb;
                half3 sss=0;
                    #ifdef _SSS_ON
                        sss= backintensity * mainLightColor * _BaseColor.rgb * thickness;
                    #endif
                 return diffuse + specular+sss;
            }
            half4 frag (Varyings input) : SV_Target
            {
                // 采样法线贴图并转换到世界空间
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalMapScale);
                half3x3 TBN = half3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                half3 N = normalize(mul(normalTS, TBN));

                Light mainlight = GetMainLight();
                half3 L = mainlight.direction;
                half3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 H = normalize(L + V);
                half NdotL = saturate(dot(N, L));
                //half3 additionalLight = GetAdditionalLightsColor(0);
                half3 mainLightColor = mainlight.color;
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 sss = 0;
                half thickness = lerp(SAMPLE_TEXTURE2D(_Thickness, sampler_Thickness, input.uv).r, 1, _ThicknessScale);
                half3 finalColor = LightingSSS(L, mainLightColor, N, V, thickness,NdotL,H);    
                    #ifdef _ADDITIONAL_LIGHTS
                        uint additionalLightCount=GetAdditionalLightsCount();
                         for(uint index=0;index<additionalLightCount;index++)
                            {
                                
                                Light addLight=GetAdditionalLight(index,input.positionWS);
                                half3 addlightColor=addLight.color;
                                half addlightattenuation = addLight.distanceAttenuation;//额外光距离衰减
                                addlightColor *= addlightattenuation;
                                half3 addlightDir=addLight.direction;
                                half NdotAddLight=saturate(dot(N,addlightDir));
                                half3 Haddlight=normalize(addlightDir + V);
                                finalColor+=LightingSSS(addlightDir,addlightColor,N, V, 
                                thickness,NdotAddLight,Haddlight);
                                
                            }
                    #endif
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}
