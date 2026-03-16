Shader "Unlit/plane reflect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ReflectionIntensity ("Reflection Intensity", Range(0, 2)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
        _BaseReflect ("Base Reflect", Range(0, 1)) = 0.6
        _FresnelPower ("Fresnel Power", Range(0.1, 8)) = 2.5
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float3 worldPos : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ReflectionIntensity;
            float _Smoothness;
            float _BaseReflect;
            float _FresnelPower;

            float3 BoxProjection(float3 reflDir, float3 worldPos, float4 probePos, float4 boxMin, float4 boxMax)
            {
                if (probePos.w>0.0)
                {
                    float3 factors=((reflDir>0?boxMax.xyz:boxMin.xyz)-worldPos)/reflDir;
                    float scalar =min(min(factors.x,factors.y),factors.z);  
                    reflDir=reflDir*scalar+(worldPos-probePos);
                }
                return reflDir;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 reflDir = reflect(-viewDir, normal);
                reflDir = BoxProjection(reflDir, i.worldPos, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
                reflDir = normalize(reflDir);

                half4 reflData;
                    float mip = (1.0 - _Smoothness)*6;
                    reflData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflDir, mip);
               

                half3 reflCol = DecodeHDR(reflData, unity_SpecCube0_HDR);
                float ndotv = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - ndotv, _FresnelPower);
                float reflectStrength = saturate(_ReflectionIntensity * lerp(_BaseReflect, 1.0, fresnel));
                col.rgb = lerp(col.rgb, reflCol, reflectStrength);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
