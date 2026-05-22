Shader "L15 Water/Modern Anime Water Surface"
{
    Properties
    {
        [Header(Color And Depth)]
        _ShallowColor ("Shallow Color", Color) = (0.24, 0.92, 0.95, 0.82)
        _MidColor ("Mid Color", Color) = (0.05, 0.53, 0.92, 0.88)
        _DeepColor ("Deep Color", Color) = (0.02, 0.13, 0.42, 0.94)
        _DepthMax ("Depth Max", Range(0.1, 30)) = 10
        _DepthSteps ("Anime Depth Bands", Range(1, 12)) = 5
        _DepthBandStrength ("Depth Band Strength", Range(0, 1)) = 0.42
        _WaterOpacity ("Water Opacity", Range(0, 1)) = 0.72
        _RefractionStrength ("Refraction Strength", Range(0, 0.08)) = 0.018
        _SkyColor ("Sky Reflection Color", Color) = (0.68, 0.96, 1.0, 1)
        _HorizonColor ("Horizon Reflection Color", Color) = (0.22, 0.72, 0.92, 1)
        _ReflectionStrength ("Reflection Strength", Range(0, 2)) = 0.55

        [Header(Waves)]
        _Wave1 ("Wave 1 Amp Len Speed Steep", Vector) = (0.34, 12.0, 1.25, 0.45)
        _Wave2 ("Wave 2 Amp Len Speed Steep", Vector) = (0.18, 7.2, 1.85, 0.34)
        _Wave3 ("Wave 3 Amp Len Speed Steep", Vector) = (0.10, 3.8, 2.40, 0.20)
        _Wave4 ("Wave 4 Amp Len Speed Steep", Vector) = (0.055, 2.2, 3.15, 0.12)
        _WaveDir1 ("Wave Dir 1", Vector) = (0.86, 0.36, 0, 0)
        _WaveDir2 ("Wave Dir 2", Vector) = (-0.38, 0.92, 0, 0)
        _WaveDir3 ("Wave Dir 3", Vector) = (0.18, -0.98, 0, 0)
        _WaveDir4 ("Wave Dir 4", Vector) = (-0.95, -0.24, 0, 0)

        [Header(Normals And Foam)]
        _NormalA ("Normal A", 2D) = "bump" {}
        _NormalB ("Normal B", 2D) = "bump" {}
        _NormalScaleA ("Normal Scale A", Range(0.01, 5)) = 0.72
        _NormalScaleB ("Normal Scale B", Range(0.01, 8)) = 2.4
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.82
        _FoamColor ("Foam Color", Color) = (0.86, 1.0, 0.98, 1)
        _FoamDepth ("Foam Depth", Range(0.02, 4)) = 0.92
        _FoamAmount ("Foam Amount", Range(0, 2)) = 1.05
        _FoamScale ("Foam Scale", Range(0.01, 4)) = 0.42
        _FoamCutoff ("Foam Cutoff", Range(0, 1)) = 0.52

        [Header(Caustics And Highlights)]
        _CausticTex ("Caustic Texture", 2D) = "white" {}
        _CausticColor ("Surface Caustic Color", Color) = (0.65, 1.0, 0.95, 1)
        _CausticStrength ("Surface Caustic Strength", Range(0, 2)) = 0.24
        _CausticScale ("Surface Caustic Scale", Range(0.01, 6)) = 0.38
        _FresnelColor ("Fresnel Color", Color) = (0.72, 1.0, 1.0, 1)
        _FresnelPower ("Fresnel Power", Range(0.2, 8)) = 3.6
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 0.62
        _SpecularColor ("Anime Sparkle Color", Color) = (1, 0.96, 0.78, 1)
        _SpecularPower ("Specular Power", Range(8, 256)) = 96
        _SpecularIntensity ("Specular Intensity", Range(0, 4)) = 1.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_NormalA); SAMPLER(sampler_NormalA);
            TEXTURE2D(_NormalB); SAMPLER(sampler_NormalB);
            TEXTURE2D(_CausticTex); SAMPLER(sampler_CausticTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _MidColor;
                half4 _DeepColor;
                half _DepthMax;
                half _DepthSteps;
                half _DepthBandStrength;
                half _WaterOpacity;
                half _RefractionStrength;
                half4 _SkyColor;
                half4 _HorizonColor;
                half _ReflectionStrength;
                float4 _Wave1;
                float4 _Wave2;
                float4 _Wave3;
                float4 _Wave4;
                float4 _WaveDir1;
                float4 _WaveDir2;
                float4 _WaveDir3;
                float4 _WaveDir4;
                half _NormalScaleA;
                half _NormalScaleB;
                half _NormalStrength;
                half4 _FoamColor;
                half _FoamDepth;
                half _FoamAmount;
                half _FoamScale;
                half _FoamCutoff;
                half4 _CausticColor;
                half _CausticStrength;
                half _CausticScale;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelStrength;
                half4 _SpecularColor;
                half _SpecularPower;
                half _SpecularIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float3 Gerstner(float3 worldPos, float4 wave, float2 dir, float time)
            {
                dir = normalize(dir);
                float amp = wave.x;
                float wavelength = max(0.01, wave.y);
                float speed = wave.z;
                float steepness = wave.w;
                float k = TWO_PI / wavelength;
                float phase = k * (dot(dir, worldPos.xz) - speed * time);
                float s = sin(phase);
                float c = cos(phase);
                float q = steepness * amp;
                return float3(dir.x * q * c, amp * s, dir.y * q * c);
            }

            float3 SampleWaterNormal(float3 positionWS)
            {
                float t = _Time.y;
                float2 uvA = positionWS.xz * _NormalScaleA * 0.055 + float2(0.035, 0.017) * t;
                float2 uvB = positionWS.xz * _NormalScaleB * 0.055 + float2(-0.026, 0.041) * t;
                half3 nA = SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, uvA).xyz * 2.0h - 1.0h;
                half3 nB = SAMPLE_TEXTURE2D(_NormalB, sampler_NormalB, uvB).xyz * 2.0h - 1.0h;
                half3 tangentNormal = normalize(half3(nA.xy + nB.xy * 0.58h, 1.0h));
                tangentNormal.xy *= _NormalStrength;
                return normalize(float3(tangentNormal.x, tangentNormal.z, tangentNormal.y));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float t = _Time.y;
                positionWS += Gerstner(positionWS, _Wave1, _WaveDir1.xy, t);
                positionWS += Gerstner(positionWS, _Wave2, _WaveDir2.xy, t);
                positionWS += Gerstner(positionWS, _Wave3, _WaveDir3.xy, t);
                positionWS += Gerstner(positionWS, _Wave4, _WaveDir4.xy, t);

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float3 normalWS = SampleWaterNormal(input.positionWS);
                float2 distortion = normalWS.xz * _RefractionStrength;
                float2 refractedUV = saturate(screenUV + distortion);

                float rawDepth = SampleSceneDepth(refractedUV);
                float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceEye = input.screenPos.w;
                float waterDepth = max(0.0, sceneEye - surfaceEye);
                float aboveSurface = step(sceneEye, surfaceEye + 0.001);
                refractedUV = lerp(refractedUV, screenUV, aboveSurface);

                float depth01 = saturate(waterDepth / max(_DepthMax, 0.001));
                float bands = max(_DepthSteps, 1.0);
                float depthBands = floor(depth01 * bands) / bands;
                float bandFeather = smoothstep(0.18, 0.92, frac(depth01 * bands));
                float softBandedDepth = lerp(depthBands, min(depthBands + rcp(bands), 1.0), bandFeather);
                float colorDepth = lerp(depth01, softBandedDepth, _DepthBandStrength);
                half3 shallowMid = lerp(_ShallowColor.rgb, _MidColor.rgb, smoothstep(0.04, 0.62, colorDepth));
                half3 waterColor = lerp(shallowMid, _DeepColor.rgb, smoothstep(0.42, 1.0, colorDepth));
                half3 sceneColor = SampleSceneColor(refractedUV);

                float foamNoise = ValueNoise(input.positionWS.xz * _FoamScale + _Time.y * float2(0.28, -0.18));
                foamNoise = saturate(foamNoise * 0.72 + ValueNoise(input.positionWS.xz * (_FoamScale * 2.1) - _Time.y * 0.21) * 0.42);
                float shoreMask = pow(saturate(1.0 - waterDepth / max(_FoamDepth, 0.001)), 1.7);
                float foam = smoothstep(_FoamCutoff, 1.0, shoreMask * foamNoise * _FoamAmount);

                float2 causticUV1 = input.positionWS.xz * _CausticScale + _Time.y * float2(0.08, 0.05);
                float2 causticUV2 = input.positionWS.xz * (_CausticScale * 1.37) - _Time.y * float2(0.04, 0.07);
                float caustic = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, causticUV1).r;
                caustic = max(caustic, SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, causticUV2).g);
                caustic = pow(saturate(caustic), 2.2) * saturate(1.0 - depth01 * 0.68);

                half3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 halfDir = normalize(lightDir + viewDir);
                half nDotL = saturate(dot(normalWS, lightDir)) * 0.5h + 0.5h;
                half fresnel = pow(saturate(1.0h - dot(normalWS, viewDir)), _FresnelPower);
                half specularCore = pow(saturate(dot(normalWS, halfDir)), _SpecularPower);
                float2 streakDir = normalize(float2(0.92, 0.38));
                float2 crossDir = float2(-streakDir.y, streakDir.x);
                float along = dot(input.positionWS.xz, streakDir);
                float across = dot(input.positionWS.xz, crossDir);
                float streakA = pow(saturate(1.0 - abs(frac(along * 0.18 - _Time.y * 0.16) * 2.0 - 1.0)), 10.0);
                float streakB = pow(saturate(1.0 - abs(frac(along * 0.31 + _Time.y * 0.11) * 2.0 - 1.0)), 16.0);
                float brokenLine = smoothstep(0.22, 0.88, ValueNoise(float2(across * 0.22, along * 0.055) + _Time.y * 0.08));
                half specular = specularCore * (streakA * 0.75h + streakB * 0.35h) * brokenLine * _SpecularIntensity;

                half transmittance = saturate((1.0h - depth01) * (1.0h - _WaterOpacity) * 0.08h);
                half3 refracted = lerp(waterColor, sceneColor, transmittance);
                half3 skyReflection = lerp(_HorizonColor.rgb, _SkyColor.rgb, saturate(viewDir.y * 0.5h + 0.5h));
                half3 finalColor = refracted * nDotL;
                finalColor += _CausticColor.rgb * caustic * _CausticStrength * 0.18h;
                finalColor = lerp(finalColor, skyReflection, saturate(fresnel * _ReflectionStrength));
                finalColor += _FresnelColor.rgb * fresnel * _FresnelStrength * 0.38h;
                finalColor += _SpecularColor.rgb * specular;
                finalColor = lerp(finalColor, _FoamColor.rgb, foam);
                finalColor = MixFog(finalColor, input.fogFactor);

                half depthOpacity = lerp(_WaterOpacity, 0.92h, smoothstep(0.06h, 1.0h, depth01));
                half alpha = saturate(depthOpacity + foam * 0.24h + fresnel * 0.10h);
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
