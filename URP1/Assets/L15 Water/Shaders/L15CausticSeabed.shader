Shader "L15 Water/Caustic Seabed"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.70, 0.66, 0.49, 1)
        _DeepColor ("Deep Color", Color) = (0.08, 0.25, 0.36, 1)
        _RidgeColor ("Ridge Color", Color) = (0.95, 0.82, 0.54, 1)
        _SandDetail ("Sand Detail", 2D) = "white" {}
        _CausticTex ("Caustic Texture", 2D) = "white" {}
        _WaterLevel ("Water Level", Float) = 0
        _DepthRange ("Depth Range", Range(0.1, 30)) = 9
        _CausticScale ("Caustic Scale", Range(0.01, 8)) = 0.46
        _CausticSpeed ("Caustic Speed", Range(0, 3)) = 0.55
        _CausticStrength ("Caustic Strength", Range(0, 5)) = 1.65
        _CausticSharpness ("Caustic Sharpness", Range(0.5, 8)) = 3.4
        _CausticWidth ("Caustic Edge Width", Range(0.01, 0.45)) = 0.12
        _CausticWarp ("Caustic Warp", Range(0, 1)) = 0.36
        _SandScale ("Sand Scale", Range(0.01, 12)) = 1.15
        _SlopeShade ("Slope Shade", Range(0, 1)) = 0.42
        _Smoothness ("Smoothness", Range(0, 1)) = 0.22
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

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_SandDetail); SAMPLER(sampler_SandDetail);
            TEXTURE2D(_CausticTex); SAMPLER(sampler_CausticTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DeepColor;
                half4 _RidgeColor;
                float _WaterLevel;
                half _DepthRange;
                half _CausticScale;
                half _CausticSpeed;
                half _CausticStrength;
                half _CausticSharpness;
                half _CausticWidth;
                half _CausticWarp;
                half _SandScale;
                half _SlopeShade;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float n = sin(dot(p, float2(127.1, 311.7)));
                float m = sin(dot(p, float2(269.5, 183.3)));
                return frac(float2(n, m) * 43758.5453);
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

            float2 CausticDomainWarp(float2 uv, float time)
            {
                float n1 = ValueNoise(uv * 0.72 + float2(time * 0.10, -time * 0.07));
                float n2 = ValueNoise(uv * 1.13 + float2(-time * 0.06, time * 0.09) + 17.3);
                float2 curl = float2(n1 - 0.5, n2 - 0.5);
                float waveA = sin((uv.x * 1.55 + uv.y * 0.62 + time * 0.72) * 6.28318);
                float waveB = cos((uv.x * -0.44 + uv.y * 1.28 - time * 0.54) * 6.28318);
                return uv + (curl * 0.42 + float2(waveA, waveB) * 0.075) * _CausticWarp;
            }

            float AnimatedVoronoiEdge(float2 uv, float cells, float time, float phase)
            {
                float2 p = uv * cells;
                float2 baseCell = floor(p);
                float2 local = frac(p);
                float nearest = 8.0;
                float secondNearest = 8.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 cell = baseCell + float2(x, y);
                        float2 random = Hash22(cell + phase);
                        float2 orbit = sin(float2(random.y, random.x) * 6.28318 + time * (0.85 + random.x * 0.9) + phase);
                        float2 site = float2(x, y) + random + orbit * 0.18;
                        float2 delta = site - local;
                        float distSq = dot(delta, delta);
                        if (distSq < nearest)
                        {
                            secondNearest = nearest;
                            nearest = distSq;
                        }
                        else if (distSq < secondNearest)
                        {
                            secondNearest = distSq;
                        }
                    }
                }

                float boundary = sqrt(secondNearest) - sqrt(nearest);
                float edge = 1.0 - smoothstep(_CausticWidth, _CausticWidth + 0.045, boundary);
                float focus = exp(-boundary * boundary * 42.0);
                return saturate(edge * 0.68 + focus * 0.55);
            }

            float ProceduralCaustic(float2 uv, float time)
            {
                float2 warpedA = CausticDomainWarp(uv, time);
                float2 warpedB = CausticDomainWarp(float2(uv.y * 0.82 - uv.x * 0.18, uv.x * 0.74 + uv.y * 0.31) + 9.7, time * 1.17);
                float webA = AnimatedVoronoiEdge(warpedA, 6.5, time, 1.3);
                float webB = AnimatedVoronoiEdge(warpedB, 10.5, -time * 0.83, 7.1);
                float pulse = 0.72 + 0.28 * sin(time * 2.4 + ValueNoise(uv * 2.1) * 6.28318);
                float caustic = max(webA, webB * 0.72) * pulse;
                return pow(saturate(caustic), _CausticSharpness);
            }

            float TriplanarCaustic(float3 positionWS, float3 normalWS)
            {
                float3 blend = pow(abs(normalWS), 4.0);
                blend /= max(blend.x + blend.y + blend.z, 0.001);
                float t = _Time.y * _CausticSpeed;
                float cx = ProceduralCaustic(positionWS.zy * _CausticScale, t + 2.7);
                float cy = ProceduralCaustic(positionWS.xz * _CausticScale, t);
                float cz = ProceduralCaustic(positionWS.xy * _CausticScale, t - 1.9);
                return cx * blend.x + cy * blend.y + cz * blend.z;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half nDotL = saturate(dot(normalWS, lightDir));
                half wrap = saturate(nDotL * 0.65h + 0.45h);

                float waterDepth = max(0.0, _WaterLevel - input.positionWS.y);
                half depth01 = saturate(waterDepth / max(_DepthRange, 0.001));
                half3 sandDetail = SAMPLE_TEXTURE2D(_SandDetail, sampler_SandDetail, input.positionWS.xz * _SandScale * 0.07).rgb;
                half ridge = saturate((normalWS.y - 0.58h) * 1.8h);
                half3 baseColor = lerp(_BaseColor.rgb, _DeepColor.rgb, depth01);
                baseColor = lerp(baseColor, _RidgeColor.rgb, ridge * 0.18h);
                baseColor *= lerp(0.82h, 1.18h, sandDetail.r);

                half shallowCaustic = pow(saturate(1.0h - depth01), 3.0h);
                half causticMask = saturate(shallowCaustic * (normalWS.y * 0.75h + 0.25h));
                half caustic = saturate(TriplanarCaustic(input.positionWS, normalWS));
                half3 causticColor = half3(0.62h, 1.0h, 0.88h) * caustic * causticMask * _CausticStrength;

                half slopeShade = lerp(1.0h - _SlopeShade, 1.0h, saturate(normalWS.y));
                half3 color = baseColor * wrap * slopeShade + causticColor;
                color += mainLight.color * pow(saturate(dot(reflect(-lightDir, normalWS), normalize(GetWorldSpaceViewDir(input.positionWS)))), 64.0h) * _Smoothness * 0.22h;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
