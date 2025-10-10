Shader "Unlit/VolumeCloud"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShapeNoise ("Shape Noise", 3D) = "white" {}
        _DetailNoise ("Detail Noise", 3D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewVector : TEXCOORD1;
            };

            // Uniforms
            float4 _BoundsMin;
            float4 _BoundsMax;
            float3 _CameraPos;
            float4x4 _InverseView;
            float4x4 _InverseProjection;
            
            int _NumSteps;
            int _NumLightSteps;
            float _StepSize;
            float _LightStepSize;
            
            float3 _CloudScale;
            float _DensityThreshold;
            float _DensityMultiplier;
            
            float _LightAbsorption;
            float _DarknessThreshold;
            float _PhaseFactor;
            
            float3 _WindDirection;
            float _WindSpeed;
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE3D(_ShapeNoise);
            SAMPLER(sampler_ShapeNoise);
            TEXTURE3D(_DetailNoise);
            SAMPLER(sampler_DetailNoise);

            // ==================== Step 3: 云盒光线追踪 ====================
            float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir)
            {
                float3 t0 = (boundsMin - rayOrigin) / rayDir;
                float3 t1 = (boundsMax - rayOrigin) / rayDir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(min(tmax.x, tmax.y), tmax.z);
                
                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }

            // ==================== Step 4: 云形状采样 ====================
            float sampleDensity(float3 position)
            {
                // 应用风场动画
                float3 windOffset = _WindDirection * _WindSpeed * _Time;
                float3 samplePos = position * _CloudScale + windOffset;
                
                // 采样形状噪声
                float4 shape = SAMPLE_TEXTURE3D_LOD(_ShapeNoise, sampler_ShapeNoise, samplePos, 0);
                float baseDensity = shape.r;
                
                // 采样细节噪声
                float3 detailPos = samplePos * 2.0 + windOffset * 2.0;
                float4 detail = SAMPLE_TEXTURE3D_LOD(_DetailNoise, sampler_DetailNoise, detailPos, 0);
                float detailDensity = detail.r * 0.5;
                
                // 结合基础形状和细节
                float density = baseDensity + detailDensity;
                
                // 应用密度阈值和乘数
                density = max(0, density - _DensityThreshold) * _DensityMultiplier;
                
                return density;
            }

            // ==================== Step 5: 光线散射和阴影 ====================
            float lightmarch(float3 startPos)
            {
                float3 lightDir = _MainLightPosition.xyz;
                float totalDensity = 0;
                
                for (int i = 0; i < _NumLightSteps; i++)
                {
                    startPos += lightDir * _LightStepSize;
                    float density = sampleDensity(startPos);
                    totalDensity += density * _LightStepSize;
                }
                
                // 比尔-朗伯定律：透射率 = e^(-光学厚度)
                return exp(-totalDensity * _LightAbsorption);
            }

            // 相位函数（近似Henyey-Greenstein）
            float phaseFunction(float cosTheta)
            {
                float g = _PhaseFactor;
                return (1 - g * g) / (4 * 3.14159265 * pow(1 + g * g - 2 * g * cosTheta, 1.5));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                
                // 计算视图向量
                float4 clip = float4(v.uv * 2.0 - 1.0, 1.0, 1.0);
                float4 view = mul(_InverseProjection, clip);
                view = float4(view.xy / view.w, -1.0, 0.0);
                o.viewVector = mul(_InverseView, view).xyz;
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // 采样原始场景颜色
                float4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // 获取深度信息
                float depth = SampleSceneDepth(i.uv);
                depth = LinearEyeDepth(depth, _ZBufferParams);
                
                float3 rayOrigin = _CameraPos;
                float3 rayDir = normalize(i.viewVector);
                
                // Step 3: 计算与云盒的交点
                float2 rayBoxInfo = rayBoxDst(_BoundsMin, _BoundsMax, rayOrigin, rayDir);
                float dstToBox = rayBoxInfo.x;
                float dstInsideBox = rayBoxInfo.y;
                
                // 如果没有击中云盒，直接返回场景颜色
                if (dstInsideBox <= 0) return sceneColor;
                
                // 考虑场景深度限制
                float dstLimit = min(depth - dstToBox, dstInsideBox);
                dstLimit = max(0, dstLimit);
                
                // Raymarching初始化
                float dstTravelled = 0;
                float transmittance = 1.0;
                float3 lightEnergy = 0;
                float3 currentPos = rayOrigin + rayDir * dstToBox;
                
                // 主光线步进循环
                while (dstTravelled < dstLimit && transmittance > _DarknessThreshold)
                {
                    currentPos = rayOrigin + rayDir * (dstToBox + dstTravelled);
                    
                    // 采样当前点密度
                    float density = sampleDensity(currentPos);
                    
                    if (density > 0)
                    {
                        // Step 5: 计算光照透射率
                        float lightTransmittance = lightmarch(currentPos);
                        
                        // 计算相位函数
                        float cosTheta = dot(rayDir, _MainLightPosition.xyz);
                        float phaseVal = phaseFunction(cosTheta);
                        
                        // 累加光能
                        lightEnergy += density * _StepSize * transmittance * lightTransmittance * phaseVal * _MainLightColor.rgb;
                        
                        // 更新透射率
                        transmittance *= exp(-density * _StepSize);
                    }
                    
                    dstTravelled += _StepSize;
                }
                
                // 最终颜色合成
                float3 cloudColor = lightEnergy;
                float3 finalColor = sceneColor.rgb * transmittance + cloudColor;
                
                return float4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
