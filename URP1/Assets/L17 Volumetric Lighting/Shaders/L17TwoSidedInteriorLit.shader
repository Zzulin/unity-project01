Shader "L17 Volumetric Lighting/Two Sided Interior Lit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.74, 0.66, 1.0)
        _ShadowColor ("Shadow Color", Color) = (0.18, 0.16, 0.14, 1.0)
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.18
        _SpecularStrength ("Specular Strength", Range(0.0, 2.0)) = 0.2
        _WrapDiffuse ("Wrap Diffuse", Range(0.0, 1.0)) = 0.06
        _AmbientBoost ("Ambient Boost", Range(0.0, 3.0)) = 0.35
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float _Smoothness;
                float _SpecularStrength;
                float _WrapDiffuse;
                float _AmbientBoost;
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                return output;
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
                float3 normalWS = normalize(input.normalWS * facing);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDirWS = normalize(mainLight.direction);

                float ndl = dot(normalWS, lightDirWS);
                float wrappedDiffuse = saturate((ndl + _WrapDiffuse) / (1.0 + _WrapDiffuse));
                float shadow = mainLight.shadowAttenuation;
                float3 directDiffuse = _BaseColor.rgb * wrappedDiffuse * shadow;

                float3 halfDirWS = SafeNormalize(lightDirWS + viewDirWS);
                float specular = pow(saturate(dot(normalWS, halfDirWS)), lerp(16.0, 96.0, _Smoothness)) * _SpecularStrength * shadow;
                float3 ambientTint = lerp(_ShadowColor.rgb, _BaseColor.rgb, 0.28);
                float3 ambient = SampleSH(normalWS) * _AmbientBoost * ambientTint;

                float beamMask = SampleBeamSurfaceMask(input.positionWS);
                float beamWrap = max(_L17BeamSurfaceWrap, 0.001);
                float beamFacing = saturate((ndl + beamWrap) / (1.0 + beamWrap));
                beamFacing = lerp(0.72, 1.0, beamFacing);
                float3 beamSurface = _L17BeamSurfaceColor.rgb * mainLight.color * beamMask * beamFacing * _L17BeamSurfaceBoost;

                return half4(ambient + mainLight.color * directDiffuse + specular + beamSurface, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
