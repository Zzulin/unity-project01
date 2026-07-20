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

        #define L17_MAX_STEPS 128
        #define CLOUD_METERS_TO_KILOMETERS 0.001

        TEXTURE2D_X(_L17IntegratedTexture);
        TEXTURE2D_X(_L17HistoryTexture);
        TEXTURE2D_X_FLOAT(_L17HistoryDepthTexture);
        TEXTURE2D_X_FLOAT(_L17LowDepthTexture);
        TEXTURE2D(_L17CloudShadowTexture);
        SAMPLER(sampler_L17CloudShadowTexture);
        TEXTURE2D(_L17BlueNoiseTexture);
        SAMPLER(sampler_L17BlueNoiseTexture);
        TEXTURE3D(_L17CloudShapeNoise);
        SAMPLER(sampler_L17CloudShapeNoise);
        TEXTURE3D(_L17CloudDetailNoise);
        SAMPLER(sampler_L17CloudDetailNoise);
        TEXTURE2D(_L17CloudWeatherMap);
        SAMPLER(sampler_L17CloudWeatherMap);

        float4 _L17FroxelSize;
        float4 _L17Params0;
        float4 _L17Params1;
        float4 _L17Params2;
        float4 _L17VolumeBoundsCenter;
        float4 _L17VolumeBoundsSize;
        float4 _L17TemporalParams;
        float _L17ForwardPhaseCeiling;
        float4 _L17ScatteringColor;
        float4x4 _L17PreviousViewProjection;
        float _L17HistoryValid;
        float _L17TemporalDepthRejection;
        float _L17FrameIndex;
        float _L17FroxelDepth;
        float4x4 _L17CloudWorldToLocal;
        float4x4 _L17CloudLocalToWorld;
        float4 _L17CloudNoiseWorldSize;
        float4 _L17CloudWind;
        float4 _L17CloudParams0;
        float4 _L17CloudParams1;
        float4 _L17CloudParams2;
        float _L17CloudShadowContrast;
        float _L17CloudMacroGapStrength;
        float4 _L17CloudShadowBounds;
        float _L17CloudShadowReceiverHeight;

        float DeviceDepthFromRawDepth(float rawDepth)
        {
        #if UNITY_REVERSED_Z
            return rawDepth;
        #else
            return lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
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
            return SAMPLE_TEXTURE2D_LOD(_L17BlueNoiseTexture, sampler_L17BlueNoiseTexture, noiseUv, 0).r;
        }

        float HenyeyGreenstein(float cosTheta, float anisotropy)
        {
            float g2 = anisotropy * anisotropy;
            float phaseBase = max(1.0 + g2 - 2.0 * anisotropy * cosTheta, 0.001);
            return (1.0 - g2) / (4.0 * PI * pow(phaseBase, 1.5));
        }

        float ProductionHenyeyGreenstein(float cosTheta, float anisotropy)
        {
            float stableAnisotropy = clamp(anisotropy, 0.0, 0.9);
            float phase = HenyeyGreenstein(cosTheta, stableAnisotropy);
            float isotropicPhase = 1.0 / (4.0 * PI);
            float phaseCeiling = isotropicPhase * clamp(_L17ForwardPhaseCeiling, 1.0, 3.5);
            float softRegion = max(phaseCeiling * 0.2, 0.0001);
            float softStart = phaseCeiling - softRegion;

            // Keep the standard HG phase function, but approach the realtime safety
            // ceiling smoothly. A hard min creates a constant angular plateau that
            // appears as a circular halo around the sun.
            return phase <= softStart
                ? phase
                : phaseCeiling - softRegion * exp(-(phase - softStart) / softRegion);
        }

        float2 IntersectCloudBox(float3 rayOriginOS, float3 rayDirectionOS)
        {
            float3 inverseDirection = 1.0 / max(abs(rayDirectionOS), 0.00001) * sign(rayDirectionOS);
            float3 t0 = (-0.5 - rayOriginOS) * inverseDirection;
            float3 t1 = (0.5 - rayOriginOS) * inverseDirection;
            float3 tMin = min(t0, t1);
            float3 tMax = max(t0, t1);
            return float2(max(max(tMin.x, tMin.y), tMin.z), min(min(tMax.x, tMax.y), tMax.z));
        }

        float CoupledCloudHeightGradient(float height01)
        {
            float bottom = smoothstep(0.0, max(_L17CloudParams1.z, 0.01), height01);
            float top = 1.0 - smoothstep(1.0 - max(_L17CloudParams1.w, 0.01), 1.0, height01);
            float anvil = lerp(0.72, 1.2, smoothstep(_L17CloudParams2.x, 1.0, height01));
            return saturate(bottom * top * anvil);
        }

        float CoupledCloudEdgeFade(float3 position01)
        {
            float2 edgeDistance = min(position01.xz, 1.0 - position01.xz);
            return smoothstep(0.0, 0.075, min(edgeDistance.x, edgeDistance.y));
        }

        float SampleCoupledCloudDensity(float3 positionOS)
        {
            float3 position01 = positionOS + 0.5;
            if (any(position01 < 0.0) || any(position01 > 1.0))
            {
                return 0.0;
            }

            float3 positionWS = mul(_L17CloudLocalToWorld, float4(positionOS, 1.0)).xyz;
            float3 centerWS = mul(_L17CloudLocalToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;
            float3 noiseWorldSize = max(abs(_L17CloudNoiseWorldSize.xyz), float3(0.001, 0.001, 0.001));
            float3 noiseCoord = (positionWS - centerWS) / noiseWorldSize + 0.5;
            float3 windDirection = normalize(_L17CloudWind.xyz + float3(0.0001, 0.0, 0.0001));
            float3 wind = windDirection * (_Time.y * _L17CloudWind.w * 0.015);
            float2 weatherUv = noiseCoord.xz * 0.72 + wind.xz * 0.035;
            float4 weather = SAMPLE_TEXTURE2D_LOD(_L17CloudWeatherMap, sampler_L17CloudWeatherMap, weatherUv, 0);

            float coverage = saturate(_L17CloudParams0.y + (weather.r - 0.5) * _L17CloudParams0.z);
            float cloudType = weather.g;
            float localDensity = lerp(0.65, 1.25, weather.b);
            float3 shapeUv = noiseCoord * _L17CloudParams0.w * 0.12 + wind;
            float4 shapeNoise = SAMPLE_TEXTURE3D_LOD(_L17CloudShapeNoise, sampler_L17CloudShapeNoise, shapeUv, 0);
            float baseShape = lerp(shapeNoise.r, shapeNoise.b, 0.72);
            float threshold = lerp(0.82, 0.24, coverage);
            float body = smoothstep(threshold, 1.0, baseShape + shapeNoise.g * 0.18)
                * lerp(0.88, 1.16, cloudType);

            float3 detailUv = noiseCoord * _L17CloudParams1.x * 0.08 + wind * 2.2;
            float4 detailNoise = SAMPLE_TEXTURE3D_LOD(_L17CloudDetailNoise, sampler_L17CloudDetailNoise, detailUv, 0);
            float detailErosion = lerp(detailNoise.r, detailNoise.b, saturate(position01.y));
            body = saturate(body - (1.0 - detailErosion) * _L17CloudParams1.y * weather.a * saturate(body * 1.65));

            float macroGap = lerp(1.0, smoothstep(0.43, 0.62, weather.r), saturate(_L17CloudMacroGapStrength));
            return body
                * CoupledCloudHeightGradient(position01.y)
                * CoupledCloudEdgeFade(position01)
                * _L17CloudParams0.x
                * localDensity
                * macroGap;
        }

        float CoupledCloudTransmittance(float3 positionWS, float3 lightDirectionWS)
        {
            if (_L17CloudParams2.w <= 0.0001 || _L17CloudParams0.x <= 0.0001)
            {
                return 1.0;
            }

            float3 rayOriginOS = mul(_L17CloudWorldToLocal, float4(positionWS, 1.0)).xyz;
            float3 rayDirectionOSPerMeter = mul((float3x3)_L17CloudWorldToLocal, lightDirectionWS);
            float2 hit = IntersectCloudBox(rayOriginOS, rayDirectionOSPerMeter);
            float startDistance = max(hit.x, 0.0);
            float endDistance = hit.y;
            if (endDistance <= startDistance)
            {
                return 1.0;
            }

            int stepCount = (int)clamp(round(_L17CloudParams2.z), 1.0, 8.0);
            float stepLengthMeters = (endDistance - startDistance) / stepCount;
            float opticalDepth = 0.0;
            float travelMeters = startDistance + stepLengthMeters * 0.5;

            [loop]
            for (int index = 0; index < 8; index++)
            {
                if (index >= stepCount)
                {
                    break;
                }

                opticalDepth += SampleCoupledCloudDensity(
                    rayOriginOS + rayDirectionOSPerMeter * travelMeters)
                    * stepLengthMeters
                    * CLOUD_METERS_TO_KILOMETERS;
                if (opticalDepth * max(_L17CloudParams2.y, 0.001) >= 6.0)
                {
                    break;
                }

                travelMeters += stepLengthMeters;
            }

            float transmission = exp(-opticalDepth * max(_L17CloudParams2.y, 0.001));
            // A low-step cloud ray march tends to underestimate optical depth.
            // Restore the lost contrast before the artistic contrast curve so
            // the same cloud openings remain visible in the aerial medium.
            transmission = saturate(1.0 - (1.0 - transmission) * 1.65);
            transmission = pow(saturate(transmission), max(_L17CloudShadowContrast, 0.25));
            return lerp(1.0, transmission, saturate(_L17CloudParams2.w));
        }

        float3 RayDirection(float2 uv)
        {
            float3 farPositionWS = ComputeWorldSpacePosition(uv, FarDeviceDepth(), unity_MatrixInvVP);
            return SafeNormalize(farPositionWS - _WorldSpaceCameraPos);
        }

        bool IsSkyDepth(float rawDepth)
        {
        #if UNITY_REVERSED_Z
            return rawDepth <= 0.000001;
        #else
            return rawDepth >= 0.999999;
        #endif
        }

        float SceneRayDistance(float2 uv)
        {
            float rawDepth = SampleSceneDepth(uv);
            if (IsSkyDepth(rawDepth))
            {
                return _L17Params0.x;
            }

            float3 scenePositionWS = ComputeWorldSpacePosition(
                uv,
                DeviceDepthFromRawDepth(rawDepth),
                unity_MatrixInvVP);
            return distance(scenePositionWS, _WorldSpaceCameraPos);
        }

        float2 IntersectVolumeBounds(float3 rayOriginWS, float3 rayDirectionWS)
        {
            float3 halfBounds = max(_L17VolumeBoundsSize.xyz * 0.5, 0.001);
            float3 boxMinimum = _L17VolumeBoundsCenter.xyz - halfBounds;
            float3 boxMaximum = _L17VolumeBoundsCenter.xyz + halfBounds;
            float3 directionSign = step(0.0, rayDirectionWS) * 2.0 - 1.0;
            float3 inverseDirection = directionSign / max(abs(rayDirectionWS), 0.00001);
            float3 t0 = (boxMinimum - rayOriginWS) * inverseDirection;
            float3 t1 = (boxMaximum - rayOriginWS) * inverseDirection;
            float3 tMinimum = min(t0, t1);
            float3 tMaximum = max(t0, t1);
            return float2(
                max(max(tMinimum.x, tMinimum.y), tMinimum.z),
                min(min(tMaximum.x, tMaximum.y), tMaximum.z));
        }

        float DensityAtPosition(float3 positionWS)
        {
            float3 halfBounds = max(_L17VolumeBoundsSize.xyz * 0.5, 0.001);
            float3 boundsDistance = halfBounds - abs(positionWS - _L17VolumeBoundsCenter.xyz);
            float boundsFadeDistance = min(boundsDistance.x, min(boundsDistance.y, boundsDistance.z));
            float boundsMask = saturate(boundsFadeDistance / max(_L17VolumeBoundsCenter.w, 0.001));

            float heightTerm = exp(-max(positionWS.y - _L17Params2.x, 0.0) * max(_L17Params2.y, 0.001));
            float noiseTerm = 1.0;
            if (_L17Params2.w > 0.0001)
            {
                float largeNoise = ValueNoise(positionWS * _L17Params2.z * 0.12);
                float fineNoise = ValueNoise(positionWS * _L17Params2.z * 0.43 + 19.73);
                float noise = lerp(largeNoise, largeNoise * 0.68 + fineNoise * 0.32, 0.55);
                noiseTerm = lerp(1.0, saturate(noise * 1.45), saturate(_L17Params2.w));
            }
            return max(_L17Params0.z, 0.0) * heightTerm * noiseTerm * boundsMask;
        }

        float SliceDistance(float slice01, float rayStart, float rayEnd)
        {
            float distributedSlice = pow(saturate(slice01), max(_L17Params0.y, 0.5));
            return lerp(rayStart, rayEnd, distributedSlice);
        }

        float SampleCloudShadow(float3 positionWS, float3 lightDirectionWS)
        {
            if (_L17CloudParams2.w <= 0.0001)
            {
                return 1.0;
            }

            float safeLightY = abs(lightDirectionWS.y) < 0.15
                ? (lightDirectionWS.y < 0.0 ? -0.15 : 0.15)
                : lightDirectionWS.y;
            float distanceToReceiver = (_L17CloudShadowReceiverHeight - positionWS.y) / safeLightY;
            float2 projectedXZ = positionWS.xz + lightDirectionWS.xz * distanceToReceiver;
            float2 shadowUv = (projectedXZ - _L17CloudShadowBounds.xy)
                / max(_L17CloudShadowBounds.zw, 0.01)
                + 0.5;
            float2 edgeDistance = min(shadowUv, 1.0 - shadowUv);
            float mapWeight = saturate(min(edgeDistance.x, edgeDistance.y) * 32.0);
            float cachedTransmission = SAMPLE_TEXTURE2D_LOD(
                _L17CloudShadowTexture,
                sampler_L17CloudShadowTexture,
                saturate(shadowUv),
                0).r;
            return lerp(1.0, cachedTransmission, mapWeight);
        }

        float4 IntegrateVolume(float3 rayDirWS, float sceneDistance, float jitter)
        {
            int stepCount = (int)clamp(round(_L17FroxelDepth), 16.0, (float)L17_MAX_STEPS);
            Light mainLight = GetMainLight();
            float3 lightDirWS = normalize(mainLight.direction);
            float viewLightCosine = dot(rayDirWS, lightDirWS);
            float phase = ProductionHenyeyGreenstein(viewLightCosine, saturate(_L17Params1.y));
            float3 scattering = 0.0;
            float transmittance = 1.0;
            float2 boundsHit = IntersectVolumeBounds(_WorldSpaceCameraPos, rayDirWS);
            float rayStart = max(boundsHit.x, 0.0);
            float rayEnd = min(boundsHit.y, min(sceneDistance, _L17Params0.x));
            if (rayEnd <= rayStart)
            {
                return float4(0.0, 0.0, 0.0, 1.0);
            }

            float cameraFadeDistance = clamp(_L17VolumeBoundsCenter.w * 0.15, 1.0, 18.0);
            [loop]
            for (int index = 0; index < L17_MAX_STEPS; index++)
            {
                if (index >= stepCount)
                {
                    break;
                }

                float slice0 = (index + jitter) / stepCount;
                float slice1 = (index + 1.0 + jitter) / stepCount;
                float t = SliceDistance(slice0, rayStart, rayEnd);
                float nextT = SliceDistance(slice1, rayStart, rayEnd);
                float stepLength = max(nextT - t, 0.001);
                float3 sampleWS = _WorldSpaceCameraPos + rayDirWS * t;
                float density = DensityAtPosition(sampleWS);
                density *= smoothstep(0.0, cameraFadeDistance, t);
                float4 shadowCoord = TransformWorldToShadowCoord(sampleWS);
                Light shadowedLight = GetMainLight(shadowCoord);
                float cloudTransmission = SampleCloudShadow(sampleWS, lightDirWS);
                float shadowAttenuation = saturate(shadowedLight.shadowAttenuation * cloudTransmission);
                float shadow = lerp(_L17Params1.z, 1.0, shadowAttenuation);
                float multiScatterShadow = shadowAttenuation * shadowAttenuation;
                float opticalDepth = density * max(_L17Params0.w, 0.001) * stepLength;
                float stepTransmittance = exp(-opticalDepth);
                float3 singleScatter = shadowedLight.color * _L17ScatteringColor.rgb * density * phase * shadow * stepLength;
                float3 multiScatter = shadowedLight.color * _L17ScatteringColor.rgb * density * saturate(_L17Params1.w) * 0.08 * multiScatterShadow * stepLength;

                scattering += transmittance * (singleScatter + multiScatter) * max(_L17Params1.x, 0.0);
                transmittance *= stepTransmittance;
                if (transmittance < 0.01)
                {
                    break;
                }
            }

            return float4(scattering, transmittance);
        }

        float2 PreviousClipToHistoryUv(float4 previousClip)
        {
            float2 previousUv = previousClip.xy / max(previousClip.w, 0.0001) * 0.5 + 0.5;
        #if UNITY_UV_STARTS_AT_TOP
            previousUv.y = 1.0 - previousUv.y;
        #endif
            return previousUv;
        }

        half4 FragmentLowDepth(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return SceneRayDistance(input.texcoord).xxxx;
        }

        half4 FragmentBuildCloudShadow(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 shadowPositionXZ = (input.texcoord - 0.5) * _L17CloudShadowBounds.zw
                + _L17CloudShadowBounds.xy;
            Light mainLight = GetMainLight();
            float transmission = CoupledCloudTransmittance(
                float3(shadowPositionXZ.x, _L17CloudShadowReceiverHeight, shadowPositionXZ.y),
                normalize(mainLight.direction));
            return transmission.xxxx;
        }

        half4 FragmentBuildVolume(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float3 rayDirWS = RayDirection(uv);
            float sceneDistance = min(
                SAMPLE_TEXTURE2D_X(_L17LowDepthTexture, sampler_PointClamp, uv).r,
                _L17Params0.x);
            float jitter = 0.5;
            if (_L17TemporalParams.y > 0.0001)
            {
                jitter += (BlueNoise(input.positionCS.xy) - 0.5) * saturate(_L17TemporalParams.y);
            }

            return IntegrateVolume(rayDirWS, sceneDistance, saturate(jitter));
        }

        float BilateralWeight(float fullDepth, float lowDepth, float spatialWeight)
        {
            float depthScale = max(_L17TemporalParams.z, 0.0001);
            return spatialWeight * exp(-abs(fullDepth - lowDepth) * depthScale);
        }

        void AccumulateDenoiseSample(
            float2 uv,
            float2 texel,
            float centerDepth,
            float2 offset,
            float spatialWeight,
            inout float4 accum,
            inout float weightSum)
        {
            float2 sampleUv = uv + offset * texel;
            float sampleDepth = SAMPLE_TEXTURE2D_X(
                _L17LowDepthTexture,
                sampler_PointClamp,
                sampleUv).r;
            float weight = BilateralWeight(centerDepth, sampleDepth, spatialWeight);
            accum += SAMPLE_TEXTURE2D_X(
                _L17IntegratedTexture,
                sampler_LinearClamp,
                sampleUv) * weight;
            weightSum += weight;
        }

        half4 FragmentDenoiseVolume(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float centerDepth = SAMPLE_TEXTURE2D_X(_L17LowDepthTexture, sampler_PointClamp, uv).r;
            float2 texel = _L17FroxelSize.zw;
            float4 accum = 0.0;
            float weightSum = 0.0;

            AccumulateDenoiseSample(uv, texel, centerDepth, float2(0.0, 0.0), 1.0, accum, weightSum);
            AccumulateDenoiseSample(uv, texel, centerDepth, float2(1.0, 0.0), 0.55, accum, weightSum);
            AccumulateDenoiseSample(uv, texel, centerDepth, float2(-1.0, 0.0), 0.55, accum, weightSum);
            AccumulateDenoiseSample(uv, texel, centerDepth, float2(0.0, 1.0), 0.55, accum, weightSum);
            AccumulateDenoiseSample(uv, texel, centerDepth, float2(0.0, -1.0), 0.55, accum, weightSum);

            return accum / max(weightSum, 0.0001);
        }

        half4 FragmentResolveTemporal(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float4 current = SAMPLE_TEXTURE2D_X(_L17IntegratedTexture, sampler_LinearClamp, uv);
            UNITY_BRANCH
            if (_L17HistoryValid <= 0.5)
            {
                return current;
            }

            float sceneDistance = min(
                SAMPLE_TEXTURE2D_X(_L17LowDepthTexture, sampler_PointClamp, uv).r,
                _L17Params0.x);
            float3 rayDirWS = RayDirection(uv);
            float reprojectionDistance = min(sceneDistance, _L17Params0.x * 0.72);
            float3 reprojectionWS = _WorldSpaceCameraPos + rayDirWS * reprojectionDistance;
            float4 previousClip = mul(_L17PreviousViewProjection, float4(reprojectionWS, 1.0));
            if (previousClip.w <= 0.0001)
            {
                return current;
            }

            float2 previousUv = PreviousClipToHistoryUv(previousClip);
            if (!all(previousUv > 0.001) || !all(previousUv < 0.999))
            {
                return current;
            }

            float previousSceneDistance = SAMPLE_TEXTURE2D_X(
                _L17HistoryDepthTexture,
                sampler_PointClamp,
                previousUv).r;
            bool currentIsSky = sceneDistance >= _L17Params0.x * 0.999;
            bool previousIsSky = previousSceneDistance >= _L17Params0.x * 0.999;
            if (currentIsSky != previousIsSky)
            {
                return current;
            }

            if (!currentIsSky)
            {
                float relativeDepthDelta = abs(previousSceneDistance - sceneDistance)
                    / max(sceneDistance, 1.0);
                if (relativeDepthDelta > max(_L17TemporalDepthRejection, 0.0001))
                {
                    return current;
                }
            }

            float3 minColor = current.rgb;
            float3 maxColor = current.rgb;
            float2 texel = _L17FroxelSize.zw;
            [unroll]
            for (int y = -1; y <= 1; y++)
            {
                [unroll]
                for (int x = -1; x <= 1; x++)
                {
                    float3 sampleColor = SAMPLE_TEXTURE2D_X(_L17IntegratedTexture, sampler_LinearClamp, uv + float2(x, y) * texel).rgb;
                    minColor = min(minColor, sampleColor);
                    maxColor = max(maxColor, sampleColor);
                }
            }

            float4 history = SAMPLE_TEXTURE2D_X(_L17HistoryTexture, sampler_LinearClamp, previousUv);
            history.rgb = clamp(history.rgb, minColor - 0.035, maxColor + 0.035);
            float luminanceDelta = abs(dot(history.rgb - current.rgb, float3(0.2126, 0.7152, 0.0722)));
            float historyWeight = saturate(_L17TemporalParams.x) * saturate(1.0 - luminanceDelta * 0.9);
            return lerp(current, history, historyWeight);
        }

        float4 SampleBilateralVolume(float2 uv, float fullDepth)
        {
            float2 lowPixel = uv * _L17FroxelSize.xy - 0.5;
            float2 centerPixel = floor(lowPixel);
            float2 fracPixel = lowPixel - centerPixel;
            float2 texel = _L17FroxelSize.zw;

            float4 accum = 0.0;
            float weightSum = 0.0;

            [unroll]
            for (int y = -1; y <= 1; y++)
            {
                [unroll]
                for (int x = -1; x <= 1; x++)
                {
                    float2 offset = float2(x, y);
                    float2 pixel = centerPixel + offset;
                    float2 sampleUv = (pixel + 0.5) * texel;
                    float2 distanceFromPixel = abs(offset - fracPixel);
                    float spatial = exp(-dot(distanceFromPixel, distanceFromPixel) * 0.42);
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
            float fullDepth = SceneRayDistance(uv);
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
            Name "BuildCloudShadow"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentBuildCloudShadow
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
            Name "DenoiseVolume"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentDenoiseVolume
            ENDHLSL
        }

        Pass
        {
            Name "TemporalResolve"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentResolveTemporal
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
