Shader "Unlit/shichayun"
{
     Properties
    {
       _MainTex ("Texture", 2D) = "white" {}
        _Color("Color",Color)=(1,1,1,1)
        _Alpha("Alpha",Range(0,1))=0.5
        _HeightOffset("HeightOffset",Range(0,1))=0.15
        _StepLayer("StepLayer",Range(2,64))=16
        _MinLayers("MinLayers",Range(1,64))=16
        _MaxLayers("MaxLayers",Range(1,64))=16
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent-50"
            "IgnoreProjector"="True"
        }
        //LOD 100
            
        Pass
        {
            
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 normalDir : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float4 posWorld : TEXCOORD3;
                float2 uv1: TEXCOORD4;
                float4 color: TEXCOORD5;
                
                UNITY_FOG_COORDS(6)
                
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _HeightOffset;

            float4 _Color;
            half _Alpha;
            half _StepLayer;
            half _MinLayers;
            half _MaxLayers;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex)+float2(frac(_Time.y*0.03),0);
                o.uv1=v.uv;
                o.normalDir = UnityObjectToWorldNormal(v.vertex);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 binormal = cross( normalize(v.normal), normalize(v.tangent.xyz) ) * v.tangent.w;
                float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal );
                o.viewDir = mul(rotation,ObjSpaceViewDir(v.vertex));
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir=normalize(i.viewDir);
                // viewDir.xy*=_HeightOffset;
                // viewDir.z+=0.4;
                float2 uv=float2(i.uv);
                //float2 uv1=float2(i.uv1);
                //float4 MainTex=tex2D(_MainTex,uv1.xy);
                float numlayers=lerp(_MinLayers,_MaxLayers,abs(dot((0,0,1),viewDir)));
                float layerDepth=1.0/numlayers;
                float currentLayerDepth=0.0;
                float2 P=viewDir.xy/viewDir.z*_HeightOffset;
                float2 deltaTexcoords=P/numlayers;
                float finiNoise=tex2D(_MainTex,uv.xy).r;
                //float2 prev_uv=uv;
                [unroll(64)] 
                while (currentLayerDepth<finiNoise)
                {
                    uv+=deltaTexcoords;
                    finiNoise=tex2D(_MainTex,uv).r;
                    currentLayerDepth+=layerDepth;
                }
                float2 prev_uv=uv-deltaTexcoords;
                float afterDepth=finiNoise-currentLayerDepth;
                float beforeDepth=tex2D(_MainTex,prev_uv).r-currentLayerDepth+layerDepth;
                float w=afterDepth/(afterDepth-beforeDepth);
                uv=lerp(uv,prev_uv,w);
                fixed col=tex2D(_MainTex,uv).r;
                fixed opcity=smoothstep(0.15,0.85,col);
                return (col,opcity);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
