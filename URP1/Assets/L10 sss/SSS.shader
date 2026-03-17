Shader "Unlit/SSS"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor("Main Color", Color) = (1,1,1,1)
        _SpecularPower("Specular Power", Float) = 10
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
                float3 V=GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 L=mainlight.direction;
                float3 H=normalize(N+L);
                float3 LightColor=mainlight.color;
                // 采样纹理
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // 计算法线点乘光照
                float VdotfuH = dot(V,-H);
                float NdotL = dot(N, L);
                float3 diffuse=NdotL*LightColor*_MainColor;
                float Spc=pow(max(0,dot(N,H)),_SpecularPower)*LightColor*_MainColor;
                // 简单光照：基础颜色乘以光照强度
                 
                float3 finalcolor=diffuse+Spc;
                return float4(VdotfuH,VdotfuH,VdotfuH,1);
            }   
            ENDHLSL
        }
    }
}
