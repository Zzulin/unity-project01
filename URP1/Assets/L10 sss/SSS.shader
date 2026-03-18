Shader "Unlit/SSS"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor("Main Color", Color) = (1,1,1,1)
        _SpecularPower("Specular Power", Float) = 10
        _Distortion("法线扰动背光", Range(0,1)) = 0.5
        _BehindPower("背光pow", Range(0,10)) = 1
        _BehindStrenth("背光强度", Range(1,4)) = 1
        _BehindAmbient("背光环境", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP 核心包含文件
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainColor;
            float _SpecularPower;
            float _Distortion;
            float _BehindPower;
            float _BehindStrenth;
            float _BehindAmbient;
            
            Varyings vert (Attributes input)    
            {
                Varyings output;
                
                // 获取顶点位置输入
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                // 变换法线到世界空间
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // 处理纹理坐标
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                //
                Light mainlight=GetMainLight();
                float3 N=input.normalWS;
                float3 L=mainlight.direction;
                float3 V=GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 H=normalize(L+V);
                
                float NdotL = saturate(dot(N, L));
                float3 MainLightColor=mainlight.color;
                // 采样纹理
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 diffuse=NdotL*MainLightColor*_MainColor;
                float3 Spc=pow(saturate(dot(N,H)),_SpecularPower)*MainLightColor*_MainColor;
                // 简单光照：基础颜色乘以光照强度
                float3 LaddN=L+N*_Distortion;
                float3 BehindLight = (pow(saturate(dot(V,-LaddN)),_BehindPower)*_BehindStrenth+_BehindAmbient)*MainLightColor*_MainColor;
                
                float3 finalcolor=diffuse+Spc+BehindLight;
                return float4(finalcolor,1);
            }   
            ENDHLSL
        }
    }
}
