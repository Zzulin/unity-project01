Shader "Hidden/L17/Froxel Volumetric Composite"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        HLSLINCLUDE
        #pragma target 4.5

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #define L17_MAX_STEPS 96

        TEXTURE2D_X(_L17IntegratedTexture);
        TEXTURE2D_X(_L17HistoryTexture);
        TEXTURE2D_X_FLOAT(_L17LowDepthTexture);
        TEXTURE2D(_L17BlueNoiseTexture);
        SAMPLER(sampler_L17BlueNoiseTexture);

        float4 _L17FroxelSize;
        float4 _L17CameraSize;
        float4 _L17Params0;
        float4 _L17Params1;
        float4 _L17Params2;
        float4 _L17TemporalParams;
        float4 _L17ScatteringColor;
        float4x4 _L17PreviousViewProjection;
        float _L17HistoryValid;
        float _L17FrameIndex;
        float _L17FroxelDepth;

        float DeviceDepthFromRawDepth(float rawDepth)
        {
        #if UNITY_REVERSED_Z
            return rawDepth;
        #else
            return rawDepth * 2.0 - 1.0;
        #endif
        }

        float FarDeviceDepth()
        {
        #if UNITY_REVERSED_Z
            return 0.0;
        #else
            return 1.0;
        #endif
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

        float BlueNoise(float2 pixel)
        {
            float2 noiseUv = (fmod(pixel + float2(_L17FrameIndex * 7.0, _L17FrameIndex * 13.0), 64.0) + 0.5) / 64.0;
            float textureNoise = SAMPLE_TEXTURE2D_LOD(_L17BlueNoiseTexture, sampler_L17BlueNoiseTexture, noiseUv, 0).r;
            float fallbackNoise = frac(52.9829189 * frac(dot(pixel + _L17FrameIndex, float2(0.06711056, 0.00583715))));
            return frac(textureNoise + fallbackNoise);
        }

        float HenyeyGreenstein(float cosTheta, float anisotropy)
        {
            float g2 = anisotropy * anisotropy;
            float phaseBase = max(1.0 + g2 - 2.0 * anisotropy * cosTheta, 0.001);
            return (1.0 - g2) / (4.0 * PI * pow(phaseBase, 1.5));
        }

        float3 RayDirection(float2 uv)
        {
            float3 farPositionWS = ComputeWorldSpacePosition(uv, FarDeviceDepth(), unity_MatrixInvVP);
            return SafeNormalize(farPositionWS - _WorldSpaceCameraPos);
        }

        float SceneEyeDepth(float2 uv)
        {
            return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
        }

        float DensityAtPosition(float3 positionWS)
        {
            float heightTerm = exp(-max(positionWS.y - _L17Params2.x, 0.0) * max(_L17Params2.y, 0.001));
            float largeNoise = ValueNoise(positionWS * _L17Params2.z * 0.12);
            float fineNoise = ValueNoise(positionWS * _L17Params2.z * 0.43 + 19.73);
            float noise = lerp(largeNoise, largeNoise * 0.68 + fineNoise * 0.32, 0.55);
            float noiseTerm = lerp(1.0, saturate(noise * 1.45), saturate(_L17Params2.w));
            return max(_L17Params0.z, 0.0) * heightTerm * noiseTerm;
        }

        float SliceDistance(float slice01)
        {
            return pow(saturate(slice01), max(_L17Params0.y, 0.5)) * max(_L17Params0.x, 0.01);
        }

        half4 FragmentLowDepth(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return SceneEyeDepth(input.texcoord).xxxx;
        }

        half4 FragmentBuildVolume(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float3 rayDirWS = RayDirection(uv);
            float sceneDistance = min(SceneEyeDepth(uv), _L17Params0.x);
            int stepCount = (int)clamp(round(_L17FroxelDepth), 16.0, (float)L17_MAX_STEPS);
            float jitter = BlueNoise(input.positionCS.xy);

            Light mainLight = GetMainLight();
            float3 lightDirWS = normalize(mainLight.direction);
            float phase = HenyeyGreenstein(dot(rayDirWS, lightDirWS), saturate(_L17Params1.y));
            float3 scattering = 0.0;
            float transmittance = 1.0;

            [loop]
            for (int index = 0; index < L17_MAX_STEPS; index++)
            {
                if (index >= stepCount)
                {
                    break;
                }

                float slice0 = (index + jitter) / stepCount;
                float slice1 = (index + 1.0 + jitter) / stepCount;
                float t = SliceDistance(slice0);
                float nextT = SliceDistance(slice1);
                if (t > sceneDistance)
                {
                    break;
                }

                float stepLength = max(min(nextT, sceneDistance) - t, 0.001);
                float3 sampleWS = _WorldSpaceCameraPos + rayDirWS * t;
                float density = DensityAtPosition(sampleWS);
                float4 shadowCoord = TransformWorldToShadowCoord(sampleWS);
                Light shadowedLight = GetMainLight(shadowCoord);
                float shadow = lerp(_L17Params1.z, 1.0, saturate(shadowedLight.shadowAttenuation));
                float opticalDepth = density * max(_L17Params0.w, 0.001) * stepLength;
                float stepTransmittance = exp(-opticalDepth);
                float3 singleScatter = shadowedLight.color * _L17ScatteringColor.rgb * density * phase * shadow * stepLength;
                float3 multiScatter = shadowedLight.color * _L17ScatteringColor.rgb * density * saturate(_L17Params1.w) * 0.08 * stepLength;

                scattering += transmittance * (singleScatter + multiScatter) * max(_L17Params1.x, 0.0);
                transmittance *= stepTransmittance;
                if (transmittance < 0.01)
                {
                    break;
                }
            }

            float4 current = float4(scattering, transmittance);
            if (_L17HistoryValid > 0.5)
            {
                float reprojectionDistance = min(sceneDistance, _L17Params0.x * 0.72);
                float3 reprojectionWS = _WorldSpaceCameraPos + rayDirWS * reprojectionDistance;
                float4 previousClip = mul(_L17PreviousViewProjection, float4(reprojectionWS, 1.0));
                float2 previousUv = previousClip.xy / max(previousClip.w, 0.0001) * 0.5 + 0.5;
                if (all(previousUv > 0.001) && all(previousUv < 0.999))
                {
                    float4 history = SAMPLE_TEXTURE2D_X(_L17HistoryTexture, sampler_LinearClamp, previousUv);
                    float luminanceDelta = abs(dot(history.rgb - current.rgb, float3(0.2126, 0.7152, 0.0722)));
                    float historyWeight = saturate(_L17TemporalParams.x) * saturate(1.0 - luminanceDelta * 4.0);
                    current = lerp(current, history, historyWeight);
                }
            }

            return current;
        }

        float BilateralWeight(float fullDepth, float lowDepth, float spatialWeight)
        {
            float depthScale = max(_L17TemporalParams.z, 0.0001);
            return spatialWeight * exp(-abs(fullDepth - lowDepth) * depthScale);
        }

        float4 SampleBilateralVolume(float2 uv, float fullDepth)
        {
            float2 lowPixel = uv * _L17FroxelSize.xy - 0.5;
            float2 basePixel = floor(lowPixel);
            float2 fracPixel = saturate(lowPixel - basePixel);
            float2 texel = _L17FroxelSize.zw;

            float4 accum = 0.0;
            float weightSum = 0.0;

            [unroll]
            for (int y = 0; y <= 1; y++)
            {
                [unroll]
                for (int x = 0; x <= 1; x++)
                {
                    float2 pixel = basePixel + float2(x, y);
                    float2 sampleUv = (pixel + 0.5) * texel;
                    float spatial = lerp(1.0 - fracPixel.x, fracPixel.x, x) * lerp(1.0 - fracPixel.y, fracPixel.y, y);
                    float lowDepth = SAMPLE_TEXTURE2D_X(_L17LowDepthTexture, sampler_PointClamp, sampleUv).r;
                    float weight = BilateralWeight(fullDepth, lowDepth, spatial);
                    accum += SAMPLE_TEXTURE2D_X(_L17IntegratedTexture, sampler_LinearClamp, sampleUv) * weight;
                    weightSum += weight;
                }
            }

            return weightSum > 0.0001 ? accum / weightSum : SAMPLE_TEXTURE2D_X(_L17IntegratedTexture, sampler_LinearClamp, uv);
        }

        half4 FragmentComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            float fullDepth = SceneEyeDepth(uv);
            float4 volume = SampleBilateralVolume(uv, fullDepth);
            float opacity = saturate(_L17TemporalParams.w);
            half3 volumetricColor = sceneColor.rgb * saturate(volume.a) + volume.rgb;
            sceneColor.rgb = lerp(sceneColor.rgb, volumetricColor, opacity);
            return sceneColor;
        }
        ENDHLSL

        Pass
        {
            Name "BuildLowDepth"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentLowDepth
            ENDHLSL
        }

        Pass
        {
            Name "BuildLowResolutionVolume"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentBuildVolume
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            ENDHLSL
        }

        Pass
        {
            Name "BilateralComposite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentComposite
            ENDHLSL
        }
    }
}
