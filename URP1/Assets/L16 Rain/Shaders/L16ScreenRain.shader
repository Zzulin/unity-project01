Shader "Hidden/L16/Rain Screen Pass"
{
    Properties
    {
        _ScreenRainStrength ("Screen Rain Strength", Range(0, 1)) = 0.65
        _LensDropletStrength ("Lens Droplet Strength", Range(0, 1)) = 0.18
        _RefractionStrength ("Refraction Strength", Range(0, 0.04)) = 0.012
        _StreakScale ("Streak Scale", Range(4, 96)) = 38
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "L16ScreenRain"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _ScreenRainStrength;
                half _LensDropletStrength;
                half _RefractionStrength;
                half _StreakScale;
            CBUFFER_END

            float _L16RainIntensity;
            float4 _L16RainWind;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float StreakLayer(float2 uv, float scale, float speed, float width)
            {
                float2 dir = normalize(float2(_L16RainWind.x * 0.35 - 0.18, -1.0));
                float2 side = float2(-dir.y, dir.x);
                float along = dot(uv, dir) * scale + _Time.y * speed;
                float across = dot(uv, side) * scale;
                float cell = floor(across);
                float rnd = Hash21(float2(cell, floor(along * 0.22)));
                float lineMask = 1.0 - smoothstep(0.0, width, abs(frac(across) - 0.5));
                float dash = smoothstep(0.16, 0.98, frac(along + rnd));
                dash *= 1.0 - smoothstep(0.45, 1.0, frac(along + rnd));
                return lineMask * dash * step(0.48, rnd);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float rawDepth = SampleSceneDepth(uv);
                float depthFade = saturate(Linear01Depth(rawDepth, _ZBufferParams) * 2.4);

                float streak = StreakLayer(uv, _StreakScale, 7.2, 0.18);
                streak += StreakLayer(uv + 0.37, _StreakScale * 1.7, 11.3, 0.12) * 0.55;
                streak *= _ScreenRainStrength * _L16RainIntensity * depthFade;

                float2 dropletGrid = uv * 13.0;
                float2 cell = floor(dropletGrid);
                float2 local = frac(dropletGrid) - 0.5;
                float seed = Hash21(cell);
                float age = frac(_Time.y * 0.18 + seed);
                local.y += age * 0.42;
                float droplet = smoothstep(0.19, 0.02, length(local)) * step(0.82, seed);
                droplet *= (1.0 - age) * _LensDropletStrength * _L16RainIntensity;

                float2 distortion = normalize(float2(_L16RainWind.x * 0.25, -1.0)) * (streak * 0.65 + droplet) * _RefractionStrength;
                half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + distortion).rgb;
                color += streak * half3(0.12h, 0.17h, 0.24h);
                color = lerp(color, color * half3(0.84h, 0.89h, 0.96h), saturate(_L16RainIntensity * 0.08h));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
