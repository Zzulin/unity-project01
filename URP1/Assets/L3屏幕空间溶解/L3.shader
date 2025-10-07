Shader "Unlit/L3"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FogNear("Fog Near", Range(0,1)) = 0.0
        _FogFar("Fog Far", Range(0,1)) = 1.0
        _Test("Test",Range(0,1))=0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 WorldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FogNear;
            float _FogFar;
            float _Test;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                
                o.WorldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.screenPos = ComputeScreenPos(o.vertex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                //物体顶点到摄像机距离
                float DistanceRamp=distance(i.WorldPos,_WorldSpaceCameraPos.xyz);
                //使用smoothstep和_FogNear _FogFar约束进0-1
                DistanceRamp=smoothstep(_FogNear,_FogFar,DistanceRamp);
                //构建抖动顺序矩阵
                float4x4 thresholdMatrix = 
                {
                    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                    13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                    16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                };
                //获取屏幕坐标0-1 ，w为缩放变量
                float2 pos=i.screenPos.xy/i.screenPos.w;
                //0-1 坐标乘以画面像素总数
                pos*=_ScreenParams.xy;//像素坐标->实际屏幕位置 这段代码为溶解效果提供准确的像素级定位 可以使用像素坐标进行特效计算
                //依据动态抖动计算透明度
                //使用 fmod 创建平铺的网格图案
                // _Test 参数的作用
                // 控制噪声密度：4*_Test 决定网格的大小
                // 值越大：噪声图案越密集，溶解更细腻
                // 值越小：噪声图案越稀疏，溶解更粗糙
                half noise=DistanceRamp-thresholdMatrix[fmod(pos.x,4*_Test)]*thresholdMatrix[fmod(pos.y,4*_Test)];
                clip(noise);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
