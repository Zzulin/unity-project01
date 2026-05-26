Shader "L16 Rain/GPU Rain Streak"
{
    Properties
    {
        _DropTint ("Drop Tint", Color) = (0.66, 0.82, 1.0, 0.58)
        _DropLength ("Drop Length", Range(0.05, 5)) = 1.8
        _DropWidth ("Drop Width", Range(0.001, 0.08)) = 0.018
        _MaxDrawDistance ("Max Draw Distance", Range(4, 140)) = 58
        _SoftDepthDistance ("Soft Depth Distance", Range(0.01, 4)) = 0.85
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
            Name "RainStreak"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            StructuredBuffer<float4> _RainDrops;

            CBUFFER_START(UnityPerMaterial)
                half4 _DropTint;
                half _DropLength;
                half _DropWidth;
                half _MaxDrawDistance;
                half _SoftDepthDistance;
            CBUFFER_END

            float4 _Wind;
            float _L16RainIntensity;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceId : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half alpha : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 drop = _RainDrops[input.instanceId];
                float3 center = drop.xyz;
                float3 cameraRight = UNITY_MATRIX_I_V._m00_m01_m02;
                float3 fallDirection = normalize(float3(_Wind.x * 0.42, -1.0, _Wind.y * 0.42));
                float seed = drop.w;
                float length = _DropLength * lerp(0.62, 1.42, seed);
                float width = _DropWidth * lerp(0.72, 1.35, frac(seed * 17.3));
                float3 positionWS = center + cameraRight * input.positionOS.x * width + fallDirection * input.positionOS.y * length;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.uv = input.uv;

                float distanceToCamera = distance(center, _WorldSpaceCameraPos);
                float distanceFade = saturate(1.0 - distanceToCamera / max(_MaxDrawDistance, 1.0));
                float nearFade = smoothstep(0.6, 3.0, distanceToCamera);
                output.alpha = distanceFade * nearFade * lerp(0.42, 1.0, seed) * saturate(_L16RainIntensity);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                float streakEye = input.screenPos.w;
                half softDepth = saturate((sceneEye - streakEye) / max(_SoftDepthDistance, 0.01));
                half core = smoothstep(0.0h, 0.42h, input.uv.y) * smoothstep(1.0h, 0.52h, input.uv.y);
                half widthMask = smoothstep(0.0h, 0.45h, input.uv.x) * smoothstep(1.0h, 0.55h, input.uv.x);
                half alpha = input.alpha * core * widthMask * softDepth * _DropTint.a;
                half3 color = _DropTint.rgb * (1.1h + core * 0.55h);
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
