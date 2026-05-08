Shader "L14 Snow/GPU Heightfield Snow"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.86, 0.92, 0.96, 1)
        _ShadowColor ("Shadow Color", Color) = (0.42, 0.55, 0.72, 1)
        _PackedColor ("Packed Snow Color", Color) = (0.62, 0.72, 0.86, 1)
        _RidgeColor ("Ridge Color", Color) = (1.0, 0.98, 0.9, 1)
        _SubsurfaceColor ("Subsurface Color", Color) = (0.72, 0.9, 1.0, 1)
        _SnowState ("Snow State", 2D) = "black" {}
        _SnowBaseMap ("Snow Base Map", 2D) = "white" {}
        _SnowNormalMap ("Snow Normal Map", 2D) = "bump" {}
        _SnowHeightMap ("Snow Height Map", 2D) = "gray" {}
        _SnowRoughnessMap ("Snow Roughness Map", 2D) = "white" {}
        _SnowSparkleMask ("Snow Sparkle Mask", 2D) = "black" {}
        _FieldSize ("Field Size", Float) = 96
        _MaxDepression ("Max Depression", Range(0.05, 1.2)) = 0.38
        _RidgeHeight ("Ridge Height", Range(0, 0.45)) = 0.16
        _PowderNoiseStrength ("Powder Noise Strength", Range(0, 1.2)) = 0.08
        _BaseReliefStrength ("Base Relief Strength", Range(0, 0.35)) = 0.13
        _BaseReliefScale ("Base Relief Scale", Range(0.25, 6)) = 1.55
        _SnowTextureScale ("Snow Texture Scale", Range(0.25, 24)) = 6
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.72
        _TextureHeightStrength ("Texture Height Strength", Range(0, 1)) = 0.75
        _SubsurfaceStrength ("Subsurface Strength", Range(0, 2)) = 0.48
        _GlancingSheenStrength ("Glancing Sheen Strength", Range(0, 1)) = 0.24
        _CrystalGlintStrength ("Crystal Glint Strength", Range(0, 1.5)) = 0.95
        _CrystalGlintDensity ("Crystal Glint Density", Range(16, 512)) = 220
        _CrystalGlintSharpness ("Crystal Glint Sharpness", Range(32, 512)) = 72
        _Smoothness ("Smoothness", Range(0, 1)) = 0.62
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
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_SnowState);
            SAMPLER(sampler_SnowState);
            float4 _SnowState_TexelSize;
            TEXTURE2D(_SnowBaseMap);
            SAMPLER(sampler_SnowBaseMap);
            TEXTURE2D(_SnowNormalMap);
            SAMPLER(sampler_SnowNormalMap);
            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);
            TEXTURE2D(_SnowRoughnessMap);
            SAMPLER(sampler_SnowRoughnessMap);
            TEXTURE2D(_SnowSparkleMask);
            SAMPLER(sampler_SnowSparkleMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _PackedColor;
                float4 _RidgeColor;
                float4 _SubsurfaceColor;
                float _FieldSize;
                float _MaxDepression;
                float _RidgeHeight;
                float _PowderNoiseStrength;
                float _BaseReliefStrength;
                float _BaseReliefScale;
                float _SnowTextureScale;
                float _NormalStrength;
                float _TextureHeightStrength;
                float _SubsurfaceStrength;
                float _GlancingSheenStrength;
                float _CrystalGlintStrength;
                float _CrystalGlintDensity;
                float _CrystalGlintSharpness;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogCoord : TEXCOORD3;
                half3 viewDirWS : TEXCOORD4;
                half depression : TEXCOORD5;
                half ridge : TEXCOORD6;
                half compaction : TEXCOORD7;
            };

            float HeightFromState(float4 state, float2 uv)
            {
                float2 textureUv = uv * _SnowTextureScale;
                float heightA = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, textureUv, 0).r;
                float heightB = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, textureUv * 0.37 + float2(0.17, 0.43), 0).r;
                float naturalRelief = ((heightA - 0.5) * 0.82 + (heightB - 0.5) * 0.38) * _BaseReliefStrength * _TextureHeightStrength;
                float powder = (heightA - heightB) * _PowderNoiseStrength * 0.02;
                return naturalRelief + state.g * _RidgeHeight - state.r * _MaxDepression + powder;
            }

            float SampleHeight(float2 uv)
            {
                float4 state = SAMPLE_TEXTURE2D_LOD(_SnowState, sampler_SnowState, saturate(uv), 0);
                return HeightFromState(state, uv);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 uv = input.uv;
                float4 state = SAMPLE_TEXTURE2D_LOD(_SnowState, sampler_SnowState, uv, 0);
                float h = HeightFromState(state, uv);
                float2 texel = max(_SnowState_TexelSize.xy, 1.0 / 512.0);
                float hL = SampleHeight(uv - float2(texel.x, 0.0));
                float hR = SampleHeight(uv + float2(texel.x, 0.0));
                float hD = SampleHeight(uv - float2(0.0, texel.y));
                float hU = SampleHeight(uv + float2(0.0, texel.y));
                float3 normalOS = normalize(float3(
                    -(hR - hL) / max(texel.x * _FieldSize * 2.0, 0.001),
                    1.0,
                    -(hU - hD) / max(texel.y * _FieldSize * 2.0, 0.001)));

                float3 positionOS = input.positionOS + float3(0.0, h, 0.0);
                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.uv = uv;
                output.fogCoord = ComputeFogFactor(output.positionHCS.z);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                output.depression = state.r;
                output.ridge = state.g;
                output.compaction = state.b;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                float2 textureUv = input.uv * _SnowTextureScale;
                half3 normalSample = SAMPLE_TEXTURE2D(_SnowNormalMap, sampler_SnowNormalMap, textureUv).xyz * 2.0h - 1.0h;
                half compactMask = saturate(input.compaction);
                half detailBlend = saturate(_NormalStrength * lerp(1.0h, 0.38h, compactMask));
                half3 detailNormalWS = normalize(half3(normalSample.x * detailBlend, normalSample.z, normalSample.y * detailBlend));
                normalWS = normalize(lerp(normalWS, normalize(normalWS + detailNormalWS), detailBlend));

                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half ndv = saturate(dot(normalWS, viewDirWS));
                half wrapDiffuse = saturate((dot(normalWS, mainLight.direction) + 0.42h) / 1.42h);
                wrapDiffuse *= wrapDiffuse;
                half3 ambient = SampleSH(normalWS) * lerp(0.56h, 0.78h, saturate(1.0h - input.depression));
                half3 lightColor = mainLight.color * (wrapDiffuse * 0.72h + ndl * 0.28h + 0.08h);

                half3 textureAlbedo = SAMPLE_TEXTURE2D(_SnowBaseMap, sampler_SnowBaseMap, textureUv).rgb;
                half roughness = SAMPLE_TEXTURE2D(_SnowRoughnessMap, sampler_SnowRoughnessMap, textureUv).r;
                roughness = saturate(lerp(roughness, 0.72h, input.depression * 0.85h));
                roughness = saturate(lerp(roughness, 0.88h, compactMask * 0.75h));

                half3 albedo = _BaseColor.rgb * textureAlbedo;
                half3 compactedAlbedo = _PackedColor.rgb * textureAlbedo * 0.82h;
                albedo = lerp(albedo, compactedAlbedo, saturate(max(input.depression * 0.82h, compactMask * 0.72h)));
                albedo = lerp(albedo, _RidgeColor.rgb * textureAlbedo, saturate(input.ridge * 0.42h));
                albedo *= lerp(_ShadowColor.rgb, 1.0h.xxx, wrapDiffuse);

                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half snowSmoothness = saturate(_Smoothness * (1.0h - roughness * 0.52h));
                half broadSpec = pow(saturate(dot(normalWS, halfDir)), lerp(28.0h, 84.0h, snowSmoothness)) * lerp(0.08h, 0.18h, 1.0h - roughness);
                half tightSpec = pow(saturate(dot(normalWS, halfDir)), lerp(90.0h, 260.0h, snowSmoothness)) * lerp(0.04h, 0.18h, 1.0h - roughness);
                half fresnel = pow(saturate(1.0h - ndv), 4.0h);
                half rimSheen = fresnel * _GlancingSheenStrength * (0.35h + ndl * 0.65h);

                half glintMask = SAMPLE_TEXTURE2D(_SnowSparkleMask, sampler_SnowSparkleMask, textureUv * max(_CrystalGlintDensity / 160.0, 0.1)).r;
                half mirrorGlint = pow(saturate(dot(reflect(-viewDirWS, normalWS), mainLight.direction)), _CrystalGlintSharpness);
                half crystalFacet = pow(saturate(dot(normalWS, halfDir)), max(_CrystalGlintSharpness * 0.45h, 1.0h));
                half grazingFlash = fresnel * 0.22h;
                half glintFacing = mirrorGlint * 0.75h + crystalFacet * 0.35h + grazingFlash;
                half crystalGlint = glintMask * glintFacing * saturate(1.0h - input.depression * 0.7h - compactMask * 0.45h) * _CrystalGlintStrength;

                half backScatter = pow(saturate(dot(viewDirWS, -mainLight.direction) * 0.5h + 0.5h), 3.0h);
                half powderMask = saturate(1.0h - input.depression * 0.65h + input.ridge * 0.35h);
                half3 subsurface = _SubsurfaceColor.rgb * mainLight.color * (backScatter * powderMask * _SubsurfaceStrength * 0.22h);
                half3 specular = (broadSpec + tightSpec + rimSheen + crystalGlint) * mainLight.color;
                specular *= saturate(1.0h - compactMask * 0.42h + input.ridge * 0.10h);

                half3 color = albedo * (ambient + lightColor) + subsurface + specular;
                color = MixFog(color, input.fogCoord);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SnowState);
            SAMPLER(sampler_SnowState);
            TEXTURE2D(_SnowHeightMap);
            SAMPLER(sampler_SnowHeightMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _PackedColor;
                float4 _RidgeColor;
                float4 _SubsurfaceColor;
                float _FieldSize;
                float _MaxDepression;
                float _RidgeHeight;
                float _PowderNoiseStrength;
                float _BaseReliefStrength;
                float _BaseReliefScale;
                float _SnowTextureScale;
                float _NormalStrength;
                float _TextureHeightStrength;
                float _SubsurfaceStrength;
                float _GlancingSheenStrength;
                float _CrystalGlintStrength;
                float _CrystalGlintDensity;
                float _CrystalGlintSharpness;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float BaseRelief(float2 uv)
            {
                float2 textureUv = uv * _SnowTextureScale;
                float heightA = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, textureUv, 0).r;
                float heightB = SAMPLE_TEXTURE2D_LOD(_SnowHeightMap, sampler_SnowHeightMap, textureUv * 0.37 + float2(0.17, 0.43), 0).r;
                return ((heightA - 0.5) * 0.82 + (heightB - 0.5) * 0.38) * _BaseReliefStrength * _TextureHeightStrength;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 state = SAMPLE_TEXTURE2D_LOD(_SnowState, sampler_SnowState, input.uv, 0);
                float h = BaseRelief(input.uv) + state.g * _RidgeHeight - state.r * _MaxDepression;
                float3 positionOS = input.positionOS + float3(0.0, h, 0.0);
                output.positionHCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
