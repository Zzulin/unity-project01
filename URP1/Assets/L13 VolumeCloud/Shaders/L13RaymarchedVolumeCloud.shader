Shader "L13 VolumeCloud/Raymarched Volume Cloud"
{
    Properties
    {
        //颜色
        _CloudColor ("Cloud Tint", Color) = (1.0, 0.92, 0.78, 1)
        _ShadowColor ("Shadow Tint", Color) = (0.48, 0.56, 0.68, 1)
        _AmbientColor ("Ambient Color", Color) = (0.46, 0.55, 0.72, 1)
        //云形贴图
        _ShapeNoise ("Shape Noise 3D", 3D) = "" {}
        _DetailNoise ("Detail Noise 3D", 3D) = "" {}
        _WeatherMap ("Weather Map", 2D) = "white" {}
        //密度控制
        _Density ("Density", Range(0, 12)) = 3.2
        _Coverage ("Coverage", Range(0, 1)) = 0.6
        _WeatherStrength ("Weather Strength", Range(0, 1)) = 0.72
        _MacroGapStrength ("Macro Gap Strength", Range(0, 1)) = 0
        //噪声控制
        _ShapeScale ("Shape Scale", Range(0.05, 8)) = 7.5
        _DetailScale ("Detail Scale", Range(0.25, 24)) = 18
        _NoiseWorldSize ("Noise World Size", Vector) = (240, 76, 160, 0)
        
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.42
        _BottomSoftness ("Bottom Softness", Range(0.01, 0.45)) = 0.18
        _TopSoftness ("Top Softness", Range(0.01, 0.45)) = 0.22
        _AnvilBias ("Anvil Bias", Range(0, 1)) = 0.62
        
        //光照控制
        _Absorption ("Absorption", Range(0.2, 8)) = 2.6
        _LightAbsorption ("Light Absorption", Range(0.2, 8)) = 2.9
        _PhaseForward ("Forward Phase", Range(0, 0.85)) = 0.58
        _ForwardPhaseClamp ("Forward Phase Clamp (0 = Off)", Range(0, 1)) = 0
        _PhaseBackward ("Backward Phase", Range(-0.65, 0)) = -0.28
        _SilverIntensity ("Silver Intensity", Range(0, 4)) = 1.65
        _PowderStrength ("Powder Strength", Range(0, 3)) = 1.15
        //动画控制
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.25, 0)
        _WindSpeed ("Wind Speed", Range(0, 30)) = 7
        //性能控制
        _StepCount ("View Steps", Range(3, 96)) = 16
        _LightStepCount ("Light Steps", Range(0, 8)) = 0
        
        _Opacity ("Opacity", Range(0, 1)) = 0.92
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+20"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Raymarch"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define MAX_VIEW_STEPS 160
            #define MAX_LIGHT_STEPS 16
            #define PI 3.14159265359

            Texture3D _ShapeNoise;
            SamplerState sampler_ShapeNoise;
            Texture3D _DetailNoise;
            SamplerState sampler_DetailNoise;
            Texture2D _WeatherMap;
            SamplerState sampler_WeatherMap;

            CBUFFER_START(UnityPerMaterial)
                float4 _CloudColor;
                float4 _ShadowColor;
                float4 _AmbientColor;
                float4 _SunDirectionWS;
                float4 _SunColor;
                float4 _WindDirection;
                float4 _NoiseWorldSize;
                float _Density;
                float _Coverage;
                float _WeatherStrength;
                float _MacroGapStrength;
                float _ShapeScale;
                float _DetailScale;
                float _DetailStrength;
                float _BottomSoftness;
                float _TopSoftness;
                float _AnvilBias;
                float _Absorption;
                float _LightAbsorption;
                float _PhaseForward;
                float _ForwardPhaseClamp;
                float _PhaseBackward;
                float _SilverIntensity;
                float _PowderStrength;
                float _WindSpeed;
                float _Opacity;
                int _StepCount;
                int _LightStepCount;
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
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float2 IntersectUnitBox(float3 ro, float3 rd)
            {
                float3 invDir = 1.0 / max(abs(rd), 0.00001) * sign(rd);
                float3 t0 = (-0.5 - ro) * invDir;
                float3 t1 = (0.5 - ro) * invDir;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);
                return float2(max(max(tMin.x, tMin.y), tMin.z), min(min(tMax.x, tMax.y), tMax.z));
            }

            float HeightGradient(float h)
            {
                float bottom = smoothstep(0.0, _BottomSoftness, h);
                float top = 1.0 - smoothstep(1.0 - _TopSoftness, 1.0, h);
                float anvil = lerp(0.72, 1.2, smoothstep(_AnvilBias, 1.0, h));
                return saturate(bottom * top * anvil);
            }

            float Hash12(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float EdgeFade(float3 p01)
            {
                float2 edgeDistance = min(p01.xz, 1.0 - p01.xz);
                return smoothstep(0.0, 0.075, min(edgeDistance.x, edgeDistance.y));
            }

            float SampleCloudDensity(float3 pOS, bool includeDetail)
            {
                float3 p01 = pOS + 0.5;
                if (any(p01 < 0.0) || any(p01 > 1.0))
                {
                    return 0.0;
                }

                float heightMask = HeightGradient(p01.y);
                float3 pWS = TransformObjectToWorld(pOS);
                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float3 noiseWorldSize = max(abs(_NoiseWorldSize.xyz), float3(0.001, 0.001, 0.001));
                float3 noiseCoord = (pWS - centerWS) / noiseWorldSize + 0.5;
                float3 windDirection = normalize(_WindDirection.xyz + float3(0.0001, 0.0, 0.0001));
                float3 wind = windDirection * (_Time.y * _WindSpeed * 0.015);
                float2 weatherUV = noiseCoord.xz * 0.72 + wind.xz * 0.035;
                float4 weather = _WeatherMap.SampleLevel(sampler_WeatherMap, weatherUV, 0);

                float coverage = saturate(_Coverage + (weather.r - 0.5) * _WeatherStrength);
                float cloudType = weather.g;
                float localDensity = lerp(0.65, 1.25, weather.b);

                float3 shapeUVW = noiseCoord * _ShapeScale * 0.12 + wind;
                float4 shapeNoise = _ShapeNoise.SampleLevel(sampler_ShapeNoise, shapeUVW, 0);
                float baseShape = lerp(shapeNoise.r, shapeNoise.b, 0.72);
                float cellularEdge = shapeNoise.g;
                float verticalBoost = lerp(0.88, 1.16, cloudType);
                float threshold = lerp(0.82, 0.24, coverage);
                float body = smoothstep(threshold, 1.0, baseShape + cellularEdge * 0.18) * verticalBoost;

                if (includeDetail)
                {
                    float3 detailUVW = noiseCoord * _DetailScale * 0.08 + wind * 2.2;
                    float4 detailNoise = _DetailNoise.SampleLevel(sampler_DetailNoise, detailUVW, 0);
                    float detailErosion = lerp(detailNoise.r, detailNoise.b, saturate(p01.y));
                    body = saturate(body - (1.0 - detailErosion) * _DetailStrength * weather.a * saturate(body * 1.65));
                }

                float macroGap = lerp(1.0, smoothstep(0.43, 0.62, weather.r), saturate(_MacroGapStrength));
                return body * heightMask * EdgeFade(p01) * _Density * localDensity * macroGap;
            }

            float HenyeyGreenstein(float cosTheta, float g)
            {
                float g2 = g * g;
                float denom = max(pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5), 0.001);
                return (1.0 - g2) / (4.0 * PI * denom);
            }

            float LightTransmittance(float3 pOS, float3 lightDirOS)
            {
                if (_LightStepCount <= 0)
                {
                    float height = saturate(pOS.y + 0.5);
                    float sunFacing = saturate(dot(normalize(lightDirOS), float3(0.0, 1.0, 0.0)) * 0.5 + 0.5);
                    return lerp(0.45, 0.95, saturate(height * 0.7 + sunFacing * 0.3));
                }

                float2 hit = IntersectUnitBox(pOS, lightDirOS);
                float end = max(hit.y, 0.0);
                int lightSteps = clamp(_LightStepCount, 1, MAX_LIGHT_STEPS);
                float stepSize = end / lightSteps;
                float opticalDepth = 0.0;
                float t = stepSize * 0.5;

                [loop]
                for (int i = 0; i < MAX_LIGHT_STEPS; i++)
                {
                    if (i >= lightSteps)
                    {
                        break;
                    }

                    opticalDepth += SampleCloudDensity(pOS + lightDirOS * t, false) * stepSize;
                    t += stepSize;
                }

                return exp(-opticalDepth * _LightAbsorption);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 roOS = TransformWorldToObject(_WorldSpaceCameraPos);
                float3 rdOS = normalize(input.positionOS - roOS);
                float2 hit = IntersectUnitBox(roOS, rdOS);

                if (hit.y <= max(hit.x, 0.0))
                {
                    discard;
                }

                int viewSteps = clamp(_StepCount, 3, MAX_VIEW_STEPS);
                float start = max(hit.x, 0.0);
                float end = hit.y;
                float rayLength = end - start;
                float stepSize = rayLength / viewSteps;
                float jitter = Hash12(input.positionHCS.xy);
                float t = start + stepSize * jitter;

                float3 sunDirWS = normalize(_SunDirectionWS.xyz);
                float3 sunDirOS = normalize(TransformWorldToObjectDir(sunDirWS));
                float3 viewDirWS = normalize(input.positionWS - _WorldSpaceCameraPos);
                float forwardPhase = HenyeyGreenstein(dot(viewDirWS, sunDirWS), _PhaseForward);
                float backwardPhase = HenyeyGreenstein(dot(viewDirWS, sunDirWS), _PhaseBackward);
                float phase = forwardPhase * _SilverIntensity + backwardPhase * 0.35 + 0.18;
                if (_ForwardPhaseClamp > 0.0001)
                {
                    phase = min(phase, _ForwardPhaseClamp);
                }

                float transmittance = 1.0;
                float3 color = 0.0;

                [loop]
                for (int i = 0; i < MAX_VIEW_STEPS; i++)
                {
                    if (i >= viewSteps || transmittance < 0.015)
                    {
                        break;
                    }

                    float3 pOS = roOS + rdOS * t;
                    float density = SampleCloudDensity(pOS, true);

                    if (density > 0.001)
                    {
                        float lightTrans = LightTransmittance(pOS, sunDirOS);
                        float attenuation = exp(-density * stepSize * _Absorption);
                        float alphaSlice = saturate(1.0 - attenuation);
                        float powder = 1.0 - exp(-density * _PowderStrength);
                        float3 litCloud = lerp(_ShadowColor.rgb, _CloudColor.rgb, lightTrans);
                        float directLighting = phase * lightTrans * 2.1 + powder * 0.32;
                        if (_ForwardPhaseClamp > 0.0001)
                        {
                            directLighting = min(directLighting, _ForwardPhaseClamp * 1.8);
                        }
                        float3 scattering = _AmbientColor.rgb * 0.82 + litCloud * _SunColor.rgb * directLighting;

                        color += transmittance * alphaSlice * scattering;
                        transmittance *= attenuation;
                    }

                    t += stepSize;
                }

                float alpha = saturate(1.0 - transmittance) * _Opacity;
                return half4(color * _Opacity, alpha);
            }
            ENDHLSL
        }
    }
}
