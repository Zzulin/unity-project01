Shader "L17 Volumetric Lighting/Window Beam"
{
    Properties
    {
        _BeamColor ("Beam Color", Color) = (1.0, 0.86, 0.62, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.20, 0.18, 0.14, 1.0)
        _Density ("Density", Range(0.0, 6.0)) = 1.8
        _Extinction ("Extinction", Range(0.1, 8.0)) = 1.35
        _Intensity ("Intensity", Range(0.0, 16.0)) = 4.8
        _Opacity ("Opacity", Range(0.0, 1.0)) = 1.0
        _Anisotropy ("Anisotropy", Range(0.0, 0.92)) = 0.68
        _NoiseScale ("Noise Scale", Range(0.1, 8.0)) = 1.45
        _NoiseStrength ("Noise Strength", Range(0.0, 1.0)) = 0.18
        _WindDirection ("Wind Direction", Vector) = (0.7, 0.0, -0.35, 0.0)
        _WindSpeed ("Wind Speed", Range(0.0, 4.0)) = 0.35
        _EdgeFade ("Edge Fade", Range(0.01, 0.45)) = 0.06
        _AxialFade ("Axial Fade", Range(0.01, 0.45)) = 0.16
        _ShadowContrast ("Shadow Contrast", Range(0.2, 4.0)) = 1.15
        _ShadowFloor ("Shadow Floor", Range(0.0, 1.0)) = 0.1
        _LightBoost ("Light Boost", Range(0.0, 4.0)) = 1.1
        _StepCount ("Step Count", Range(8, 96)) = 72
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+40"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "VolumetricBeam"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define MAX_STEPS 96
            #define PI 3.14159265359

            CBUFFER_START(UnityPerMaterial)
                float4 _BeamColor;
                float4 _ShadowColor;
                float4 _WindDirection;
                float _Density;
                float _Extinction;
                float _Intensity;
                float _Opacity;
                float _Anisotropy;
                float _NoiseScale;
                float _NoiseStrength;
                float _WindSpeed;
                float _EdgeFade;
                float _AxialFade;
                float _ShadowContrast;
                float _ShadowFloor;
                float _LightBoost;
                float _StepCount;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.screenPos = ComputeScreenPos(output.positionHCS);
                return output;
            }

            float2 IntersectUnitBox(float3 rayOriginOS, float3 rayDirOS)
            {
                float3 invDir = 1.0 / max(abs(rayDirOS), 0.0001) * sign(rayDirOS);
                float3 t0 = (-0.5 - rayOriginOS) * invDir;
                float3 t1 = (0.5 - rayOriginOS) * invDir;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);
                return float2(max(max(tMin.x, tMin.y), tMin.z), min(min(tMax.x, tMax.y), tMax.z));
            }

            float InterleavedGradientNoise(float2 pixel)
            {
                return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
            }

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash13(i + float3(0.0, 0.0, 0.0));
                float n100 = Hash13(i + float3(1.0, 0.0, 0.0));
                float n010 = Hash13(i + float3(0.0, 1.0, 0.0));
                float n110 = Hash13(i + float3(1.0, 1.0, 0.0));
                float n001 = Hash13(i + float3(0.0, 0.0, 1.0));
                float n101 = Hash13(i + float3(1.0, 0.0, 1.0));
                float n011 = Hash13(i + float3(0.0, 1.0, 1.0));
                float n111 = Hash13(i + float3(1.0, 1.0, 1.0));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            float FractalNoise(float3 p)
            {
                float result = 0.0;
                float amplitude = 0.58;
                float frequency = 1.0;

                [unroll]
                for (int octave = 0; octave < 3; octave++)
                {
                    result += ValueNoise(p * frequency) * amplitude;
                    frequency *= 2.07;
                    amplitude *= 0.5;
                }

                return result;
            }

            float HenyeyGreenstein(float cosTheta, float anisotropy)
            {
                float g2 = anisotropy * anisotropy;
                float denom = max(pow(1.0 + g2 - 2.0 * anisotropy * cosTheta, 1.5), 0.001);
                return (1.0 - g2) / (4.0 * PI * denom);
            }

            float SampleDensity(float3 sampleOS, float3 sampleWS)
            {
                float3 sample01 = sampleOS + 0.5;
                float2 edgeDistance = 0.5 - abs(sampleOS.xy);
                float radialFade = smoothstep(0.0, _EdgeFade, min(edgeDistance.x, edgeDistance.y));
                float axialFade = smoothstep(0.0, _AxialFade, sample01.z) * (1.0 - smoothstep(1.0 - _AxialFade, 1.0, sample01.z));

                float3 wind = normalize(_WindDirection.xyz + float3(0.0001, 0.0, 0.0001)) * (_Time.y * _WindSpeed);
                float noise = FractalNoise(sampleWS * _NoiseScale * 0.22 + wind);
                float densityNoise = lerp(1.0, saturate(noise * 1.25), _NoiseStrength);

                return _Density * radialFade * axialFade * densityNoise;
            }

            float ComputeSceneClampDistanceOS(float2 screenUV, float3 rayOriginWS, float3 rayDirWS, float3 rayOriginOS, float3 rayDirOS, float fallbackEnd)
            {
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                if (sceneEyeDepth >= (_ProjectionParams.z - 0.01))
                {
                    return fallbackEnd;
                }

            #if UNITY_REVERSED_Z
                float deviceDepth = rawDepth;
            #else
                float deviceDepth = rawDepth * 2.0 - 1.0;
            #endif

                float3 scenePositionWS = ComputeWorldSpacePosition(screenUV, deviceDepth, unity_MatrixInvVP);
                float3 sceneOffsetOS = TransformWorldToObject(scenePositionWS) - rayOriginOS;
                float sceneDistanceOS = dot(sceneOffsetOS, rayDirOS);
                if (sceneDistanceOS <= 0.0)
                {
                    return fallbackEnd;
                }

                float sceneDistanceWS = dot(scenePositionWS - rayOriginWS, rayDirWS);
                if (sceneDistanceWS <= 0.0)
                {
                    return fallbackEnd;
                }

                return min(fallbackEnd, sceneDistanceOS);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 rayOriginWS = _WorldSpaceCameraPos;
                float3 rayOriginOS = TransformWorldToObject(rayOriginWS);
                float3 rayDirWS = normalize(input.positionWS - rayOriginWS);
                float3 rayDirOS = normalize(TransformWorldToObjectDir(rayDirWS));
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                float2 hit = IntersectUnitBox(rayOriginOS, rayDirOS);
                if (hit.y <= max(hit.x, 0.0))
                {
                    discard;
                }

                int stepCount = (int)clamp(round(_StepCount), 8.0, (float)MAX_STEPS);
                float start = max(hit.x, 0.0);
                float end = ComputeSceneClampDistanceOS(screenUV, rayOriginWS, rayDirWS, rayOriginOS, rayDirOS, hit.y);
                if (end <= start)
                {
                    discard;
                }

                float stepSize = max((end - start) / stepCount, 0.0001);
                float t = start + stepSize * InterleavedGradientNoise(input.positionHCS.xy);

                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);
                float phase = HenyeyGreenstein(dot(-rayDirWS, lightDirWS), _Anisotropy);
                float transmittance = 1.0;
                float3 scattering = 0.0;

                [loop]
                for (int index = 0; index < MAX_STEPS; index++)
                {
                    if (index >= stepCount)
                    {
                        break;
                    }

                    float3 sampleOS = rayOriginOS + rayDirOS * t;
                    float3 sampleWS = TransformObjectToWorld(sampleOS);
                    float density = SampleDensity(sampleOS, sampleWS);

                    float4 shadowCoord = TransformWorldToShadowCoord(sampleWS);
                    Light shadowedMainLight = GetMainLight(shadowCoord);
                    float shadow = lerp(_ShadowFloor, 1.0, pow(saturate(shadowedMainLight.shadowAttenuation), _ShadowContrast));
                    float3 beamTint = lerp(_ShadowColor.rgb, _BeamColor.rgb, shadow);
                    float opticalDepth = density * stepSize * _Extinction;
                    float stepTransmittance = exp(-opticalDepth);
                    float3 stepLighting = shadowedMainLight.color * beamTint * shadow * max(phase * _LightBoost, 0.001) * density * stepSize;

                    scattering += transmittance * stepLighting;
                    transmittance *= stepTransmittance;
                    if (transmittance <= 0.01)
                    {
                        break;
                    }

                    t += stepSize;
                }

                float3 color = scattering * _Intensity;
                float alpha = saturate((1.0 - transmittance) * _Opacity);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
