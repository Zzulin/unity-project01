Shader "L12 Grass/Interactive GPU Grass"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.11, 0.34, 0.12, 1)
        _TipColor ("Tip Color", Color) = (0.46, 0.68, 0.22, 1)
        _BladeHeight ("Blade Height", Float) = 1.25
        _BladeWidth ("Blade Width", Float) = 0.085
        _WindStrength ("Wind Strength", Range(0, 1.5)) = 0.32
        _WindScale ("Wind Scale", Float) = 0.18
        _WindSpeed ("Wind Speed", Float) = 1.8
        _WindDirection ("Wind Direction", Vector) = (0.86, 0.42, 0, 0)
        _GustStrength ("Gust Strength", Range(0, 2)) = 0.85
        _GustFrequency ("Gust Frequency", Float) = 0.065
        _GustSpeed ("Gust Speed", Float) = 5.8
        _GustWidth ("Gust Width", Range(0.05, 0.95)) = 0.34
        _GustNoiseScale ("Gust Noise Scale", Float) = 0.055
        _ShapeVariation ("Shape Variation", Range(0, 1)) = 0.72
        _TipBrightness ("Tip Brightness", Range(0.5, 2)) = 1.22
        _InteractionStrength ("Interaction Strength", Float) = 3.6
        _InteractionFlattenStrength ("Interaction Flatten Strength", Range(0, 2)) = 0.85
        _DensityTexture ("Density Texture", 2D) = "white" {}
        _InteractionTexture ("Interaction Texture", 2D) = "black" {}
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
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            StructuredBuffer<float4> _VisibleBladeData;
            TEXTURE2D(_DensityTexture);
            SAMPLER(sampler_DensityTexture);
            TEXTURE2D(_InteractionTexture);
            SAMPLER(sampler_InteractionTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 _FieldOrigin;
                float4 _FieldScale;
                float4 _FieldSize;
                float _BladeHeight;
                float _BladeWidth;
                float _WindStrength;
                float _WindScale;
                float _WindSpeed;
                float4 _WindDirection;
                float _GustStrength;
                float _GustFrequency;
                float _GustSpeed;
                float _GustWidth;
                float _GustNoiseScale;
                float _ShapeVariation;
                float _TipBrightness;
                float _InteractionStrength;
                float _InteractionFlattenStrength;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half3 color : COLOR0;
                half fogCoord : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1;
            };

            float Hash12(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash12(i);
                float b = Hash12(i + float2(1.0, 0.0));
                float c = Hash12(i + float2(0.0, 1.0));
                float d = Hash12(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float4 blade = _VisibleBladeData[input.instanceID];
                float random = Hash12(blade.xy);
                float widthRandom = Hash12(blade.xy + 17.31);
                float leanRandom = Hash12(blade.xy + 41.73);
                float twistRandom = Hash12(blade.xy + 83.19);
                float colorRandom = Hash12(blade.xy + 127.53);
                float yaw = blade.z;
                float height01 = saturate(input.uv.y);

                float3 local = input.positionOS;
                float widthScale = lerp(1.0, lerp(0.62, 1.46, widthRandom), _ShapeVariation);
                float twist = (twistRandom - 0.5) * _ShapeVariation * 0.78 * height01;
                float twistS;
                float twistC;
                sincos(twist, twistS, twistC);
                local.xz = float2(local.x * twistC - local.z * twistS, local.x * twistS + local.z * twistC);
                local.xz *= _BladeWidth * widthScale;
                local.y = height01 * _BladeHeight * blade.w;

                float s;
                float c;
                sincos(yaw, s, c);
                float2 rotatedXZ = float2(local.x * c - local.z * s, local.x * s + local.z * c);

                float2 scaledBladeXZ = blade.xy * _FieldScale.xy;
                float3 rootWS = float3(_FieldOrigin.x + scaledBladeXZ.x, _FieldOrigin.y, _FieldOrigin.z + scaledBladeXZ.y);
                float2 bendXZ = 0;
                float2 fieldUV = saturate((scaledBladeXZ + _FieldSize.xy * 0.5) / max(_FieldSize.xy, 0.001));

                float2 windDir = normalize(_WindDirection.xy + 0.0001);
                float2 crossWind = float2(-windDir.y, windDir.x);
                float windCoord = dot(rootWS.xz, windDir);
                float sideCoord = dot(rootWS.xz, crossWind);
                float gustNoise = ValueNoise(rootWS.xz * _GustNoiseScale + _Time.y * 0.08);
                float gustPhase = (windCoord - _Time.y * _GustSpeed + gustNoise * 12.0) * _GustFrequency;
                float gustBand = sin(gustPhase) * 0.5 + 0.5;
                float gustFront = smoothstep(1.0 - _GustWidth, 1.0, gustBand);
                float gustTrail = smoothstep(0.0, 0.55, gustBand);
                float coherentGust = gustFront * gustTrail;
                float ripplePhase = dot(rootWS.xz, float2(_WindScale, _WindScale * 1.37)) + _Time.y * _WindSpeed + random * 1.7;
                float localFlutter = (sin(ripplePhase) + sin(ripplePhase * 2.17 + sideCoord * 0.06) * 0.34) * 0.28;
                bendXZ += windDir * ((coherentGust * _GustStrength + localFlutter) * _WindStrength);

                float4 interaction = SAMPLE_TEXTURE2D_LOD(_InteractionTexture, sampler_InteractionTexture, fieldUV, 0);
                float2 interactionDir = interaction.rg * 2.0 - 1.0;
                float interactionPressure = saturate(interaction.b);
                bendXZ += interactionDir * interactionPressure * _InteractionStrength;

                float bendMask = pow(height01, 1.45);
                float3 positionWS = rootWS;
                positionWS.xz += rotatedXZ;
                positionWS.y += local.y;
                float leanAngle = leanRandom * 6.2831853;
                positionWS.xz += float2(cos(leanAngle), sin(leanAngle)) * ((colorRandom - 0.5) * _ShapeVariation * 0.18 * height01 * height01 * blade.w);
                positionWS.xz += bendXZ * bendMask;
                positionWS.y -= length(bendXZ) * 0.16 * height01 * height01;
                positionWS.y -= interactionPressure * _InteractionFlattenStrength * height01 * _BladeHeight;

                half3 normalWS = normalize(half3(-bendXZ.x * 0.35, 1.0, -bendXZ.y * 0.35));
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);

                Light mainLight = GetMainLight(output.shadowCoord);
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS) * 0.45;
                half shadowAttenuation = mainLight.shadowAttenuation;
                half3 lit = ambient + mainLight.color * shadowAttenuation * (ndl * 0.72 + 0.28);
                half3 tipColor = saturate(_TipColor.rgb * (half)_TipBrightness);
                half3 albedo = lerp(_BaseColor.rgb, tipColor, smoothstep(0.0h, 1.0h, height01));
                half densityTint = SAMPLE_TEXTURE2D_LOD(_DensityTexture, sampler_DensityTexture, fieldUV, 0).r;
                albedo *= lerp(0.82, 1.12, densityTint);
                albedo *= lerp(1.0, 0.72, interactionPressure * height01);
                albedo *= lerp(0.86, 1.18, random);

                output.positionHCS = TransformWorldToHClip(positionWS);
                output.color = albedo * lit;
                output.fogCoord = ComputeFogFactor(output.positionHCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 color = MixFog(input.color, input.fogCoord);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
