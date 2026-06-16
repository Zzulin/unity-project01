Shader "L17 Volumetric Lighting/Two Sided Interior Lit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map (RGB)", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (0.8, 0.74, 0.66, 1.0)
        _ShadowColor ("Indirect Shadow Tint", Color) = (0.18, 0.16, 0.14, 1.0)
        _Cutoff ("Alpha Clip Threshold", Range(0.0, 1.0)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0

        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
        [NoScaleOffset] _MetallicMap ("Metallic Map (R)", 2D) = "black" {}
        _Roughness ("Roughness", Range(0.02, 1.0)) = 0.72
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.1

        [NoScaleOffset] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalMapScale ("Normal Scale", Range(0.0, 2.0)) = 1.0
        [NoScaleOffset] _OcclusionMap ("Occlusion Map (R)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        _SpecularStrength ("Specular Strength", Range(0.0, 2.0)) = 0.6
        _EnvironmentStrength ("Environment Strength", Range(0.0, 2.0)) = 0.7
        _WrapDiffuse ("Diffuse Wrap", Range(0.0, 0.5)) = 0.03
        _AmbientBoost ("Ambient Boost", Range(0.0, 3.0)) = 0.45
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
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
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicMap);
            SAMPLER(sampler_MetallicMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float _Cutoff;
                float _AlphaClip;
                float _Metallic;
                float _Roughness;
                float _Smoothness;
                float _NormalMapScale;
                float _OcclusionStrength;
                float _SpecularStrength;
                float _EnvironmentStrength;
                float _WrapDiffuse;
                float _AmbientBoost;
                float _Cull;
            CBUFFER_END

            #define L17_MAX_BEAM_COUNT 4

            float _L17BeamCount;
            float4x4 _L17BeamWorldToLocal[L17_MAX_BEAM_COUNT];
            float4 _L17BeamSurfaceColor;
            float _L17BeamEdgeFade;
            float _L17BeamAxialFade;
            float _L17BeamSurfaceBoost;
            float _L17BeamSurfaceWrap;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD6;
            #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(positionInputs);
            #endif

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                return output;
            }

            float L17DistributionGGX(float noH, float roughness)
            {
                float a = roughness * roughness;
                float a2 = a * a;
                float d = noH * noH * (a2 - 1.0) + 1.0;
                return a2 / max(PI * d * d, 0.0001);
            }

            float L17VisibilitySmithGGX(float noV, float noL, float roughness)
            {
                float a = roughness * roughness;
                float gv = noL * sqrt(max(noV * noV * (1.0 - a) + a, 0.0001));
                float gl = noV * sqrt(max(noL * noL * (1.0 - a) + a, 0.0001));
                return 0.5 / max(gv + gl, 0.0001);
            }

            float3 L17FresnelSchlick(float voH, float3 f0)
            {
                float f = pow(1.0 - saturate(voH), 5.0);
                return f0 + (1.0 - f0) * f;
            }

            float3 L17FresnelSchlickRoughness(float noV, float3 f0, float roughness)
            {
                float f = pow(1.0 - saturate(noV), 5.0);
                return f0 + (max(1.0 - roughness, f0) - f0) * f;
            }

            float3 L17DirectPBR(
                Light lightData,
                float3 normalWS,
                float3 viewDirWS,
                float3 albedo,
                float metallic,
                float roughness,
                float3 f0)
            {
                float3 lightDirWS = normalize(lightData.direction);
                float3 halfDirWS = SafeNormalize(lightDirWS + viewDirWS);
                float rawNoL = dot(normalWS, lightDirWS);
                float noL = saturate((rawNoL + _WrapDiffuse) / (1.0 + _WrapDiffuse));
                float noV = saturate(dot(normalWS, viewDirWS));
                float noH = saturate(dot(normalWS, halfDirWS));
                float voH = saturate(dot(viewDirWS, halfDirWS));

                float3 fresnel = L17FresnelSchlick(voH, f0);
                float distribution = L17DistributionGGX(noH, roughness);
                float visibility = L17VisibilitySmithGGX(noV, noL, roughness);
                float3 specular = fresnel * distribution * visibility * _SpecularStrength;
                float3 diffuse = (1.0 - fresnel) * (1.0 - metallic) * albedo * rcp(PI);
                float attenuation = lightData.distanceAttenuation * lightData.shadowAttenuation;
                return (diffuse + specular) * lightData.color * attenuation * noL;
            }

            float SampleBeamSurfaceMask(float3 positionWS)
            {
                float mask = 0.0;
                float edgeFade = max(_L17BeamEdgeFade, 0.001);
                float axialFadeWidth = max(_L17BeamAxialFade, 0.001);

                [unroll]
                for (int index = 0; index < L17_MAX_BEAM_COUNT; index++)
                {
                    float active = step((float)index + 0.5, _L17BeamCount);
                    float3 localPosition = mul(_L17BeamWorldToLocal[index], float4(positionWS, 1.0)).xyz;
                    float3 sideDistance = 0.5.xxx - abs(localPosition);
                    float inside = step(0.0, min(min(sideDistance.x, sideDistance.y), sideDistance.z));

                    float crossSectionFade = smoothstep(0.0, edgeFade, min(sideDistance.x, sideDistance.y));
                    float axial01 = localPosition.z + 0.5;
                    float axialFade = smoothstep(0.0, axialFadeWidth, axial01) *
                        (1.0 - smoothstep(1.0 - axialFadeWidth, 1.0, axial01));

                    mask = max(mask, active * inside * crossSectionFade * axialFade);
                }

                return saturate(mask);
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE faceSign : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float facing = IS_FRONT_VFACE(faceSign, 1.0, -1.0);
                float3 geometricNormalWS = normalize(input.normalWS * facing);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = normalize(cross(input.normalWS, tangentWS) * input.tangentWS.w * facing);
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalMapScale);
                float3 normalWS = normalize(TransformTangentToWorld(normalTS, float3x3(tangentWS, bitangentWS, geometricNormalWS)));
                float3 viewDirWS = SafeNormalize(input.viewDirWS);

                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
            #if defined(_ALPHATEST_ON)
                clip(baseSample.a - _Cutoff);
            #endif

                float metallicMap = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv).r;
                float metallic = saturate(metallicMap + _Metallic);
                float smoothness = saturate(_Smoothness);
                float roughness = max(saturate(_Roughness * (1.0 - smoothness)), 0.045);
                float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                occlusion = lerp(1.0, occlusion, _OcclusionStrength);
                float3 albedo = baseSample.rgb;
                float3 f0 = lerp(0.04.xxx, albedo, metallic);

            #if defined(LIGHTMAP_ON)
                float4 shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
            #else
                float4 shadowMask = unity_ProbesOcclusion;
            #endif

            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                float4 shadowCoord = 0;
            #endif

                Light mainLight = GetMainLight(shadowCoord, input.positionWS, shadowMask);
                float3 color = L17DirectPBR(mainLight, normalWS, viewDirWS, albedo, metallic, roughness, f0);

            #if defined(_ADDITIONAL_LIGHTS)
                uint additionalLightCount = GetAdditionalLightsCount();
                [loop]
                for (uint lightIndex = 0u; lightIndex < additionalLightCount; lightIndex++)
                {
                    Light lightData = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    color += L17DirectPBR(lightData, normalWS, viewDirWS, albedo, metallic, roughness, f0);
                }
            #endif

                float3 bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalWS);
                float noV = saturate(dot(normalWS, viewDirWS));
                float3 ks = L17FresnelSchlickRoughness(noV, f0, roughness);
                float3 kd = (1.0 - ks) * (1.0 - metallic);
                float3 indirectTint = lerp(_ShadowColor.rgb, 1.0.xxx, 0.75);
                float3 diffuseGI = bakedGI * albedo * kd * indirectTint * _AmbientBoost;

                float3 reflectDir = reflect(-viewDirWS, normalWS);
                float3 reflection = GlossyEnvironmentReflection(reflectDir, input.positionWS, roughness, occlusion);
                float3 specularGI = reflection * ks * _SpecularStrength;
                color += (diffuseGI * occlusion + specularGI) * _EnvironmentStrength;

                float beamMask = SampleBeamSurfaceMask(input.positionWS);
                float beamWrap = max(_L17BeamSurfaceWrap, 0.001);
                float beamFacing = saturate((dot(normalWS, mainLight.direction) + beamWrap) / (1.0 + beamWrap));
                beamFacing = lerp(0.72, 1.0, beamFacing);
                color += _L17BeamSurfaceColor.rgb * mainLight.color * beamMask * beamFacing * _L17BeamSurfaceBoost;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex UniversalVertexMeta
            #pragma fragment L17FragmentMeta

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicMap);
            SAMPLER(sampler_MetallicMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float _Cutoff;
                float _AlphaClip;
                float _Metallic;
                float _Roughness;
                float _Smoothness;
                float _NormalMapScale;
                float _OcclusionStrength;
                float _SpecularStrength;
                float _EnvironmentStrength;
                float _WrapDiffuse;
                float _AmbientBoost;
                float _Cull;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

            half4 L17FragmentMeta(Varyings input) : SV_Target
            {
                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
            #if defined(_ALPHATEST_ON)
                clip(baseSample.a - _Cutoff);
            #endif

                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo = baseSample.rgb;
                metaInput.Emission = 0.0.xxx;
                return UniversalFragmentMeta(input, metaInput);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
