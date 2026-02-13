Shader "Unlit/pbr_1"
{
   Properties
   {
       _BaseMap("RGB basecolor A smoothness", 2D) = "white" {}
       _Metallic("Metallic", Range(0,1)) = 1
       _MetallicMap("Metallic", 2D) = "gray" {}
       _NormalMap("Normal", 2D) = "bump" {}
       _Smoothness("_Smoothness", Range(0,1)) = 0.5
       
   }
   SubShader
   {
       Tags
       {
           "RenderType"="Opaque"
           "RenderPipeline"="UniversalPipeline"
       }
       pass//urp管线不能一次运行多个pass 多pass需要定制渲染管线 renderfuture
       {
           Name"Forward"
           Tags
           {
               //urp管线下能调用这个标签的pass一次 再用第二次或其他LightMode标签都用不了
               "LightMode"="UniversalForward"
           }
           HLSLPROGRAM
           #pragma vertex vert
           #pragma fragment frag
           
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

           float _Metallic;
           
           float _Smoothness; 
           
           SAMPLER(sampler_BaseMap);
           Texture2D _BaseMap;
           float4 _BaseMap_ST;

           SAMPLER(sampler_MetallicMap);
           Texture2D _MetallicMap;
           float4 _MetallicMap_ST;

           SAMPLER(sampler_NormalMap);
           Texture2D _NormalMap;
           float4 _NormalMap_ST;
           
           struct appdata
           {
               float4 vertex : POSITION;
               float3 normal : NORMAL;
               float2 uv : TEXCOORD0;
               float4 tangent : TANGENT;
           };
           struct v2f
           {
               float4 positionCS : SV_POSITION;
               float2 uv : TEXCOORD0;
               float3 NormalWS : TEXCOORD1;
               float3 positionWS : TEXCOORD2;
               float3 viewDirWS : TEXCOORD3;
               float3 tangentWS : TEXCOORD4;
               float3 bitangentWS : TEXCOORD5;
           };
           v2f vert (appdata v)
           {
               v2f o;
               o.positionCS = TransformObjectToHClip(v.vertex.xyz);
               o.NormalWS = TransformObjectToWorldNormal(v.normal);
               o.positionWS = TransformObjectToWorld(v.vertex.xyz);
               o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
               o.viewDirWS=GetWorldSpaceViewDir(o.positionWS);
               o.tangentWS=TransformObjectToWorld(v.tangent.xyz);
               o.bitangentWS=cross(o.NormalWS,o.tangentWS)*v.tangent.w;
               return o;
           }
           float DistributionGGX(float NdotH,float roughness)
           {
               float a=roughness*roughness;
               float a2=a*a;
               float NdotH2=NdotH*NdotH;
               float denom=NdotH2*(a2-1)+1;
               return a2/max(0.001,PI*denom*denom);
           }
           float GeometrySchlickGGX(float NdotV,float roughness)
           {
               float r =(roughness+1);
               float k=r*r/8;
               float denom=NdotV*(1-k)+k;
               return NdotV/max(0.001,denom);//避免除0出现白色噪点
               
           }
           float GeometrySmith(float NdotV,float NdotL,float roughness)
           {
               float ggx_v=GeometrySchlickGGX(NdotV,roughness);
               float ggx_l=GeometrySchlickGGX(NdotL,roughness);
               return ggx_v*ggx_l;
           }
           
           float4 frag (v2f i) : SV_Target
           {
               //采样 向量
               float3 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv));
               //切线空间->世界空间
               float3x3 TBN=float3x3(i.tangentWS,i.bitangentWS,i.NormalWS);
               float3 normalWS=normalize(mul(normalTS,TBN));
               float4 Albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
               float metallic=SAMPLE_TEXTURE2D(_MetallicMap,sampler_MetallicMap,i.uv).r*_Metallic;
               float3 viewDirWS=normalize(i.viewDirWS);
               float3 LightDir=GetMainLight().direction;
               float3 halfDir=normalize(LightDir+viewDirWS);
               float smoothness= Albedo.a*_Smoothness;
               float roughness=1-smoothness;
               
               //dot
               float NdotL=dot(normalWS,LightDir);
               float NdotV=dot(normalWS,viewDirWS);
               float NdotH=dot(normalWS,halfDir);
               float VdotH=dot(viewDirWS,halfDir);

               //
               float3 LightColor=GetMainLight().color;
               float3 diffuse=NdotL*LightColor*Albedo.xyz*(1-metallic);//能量守恒
               float3 SpecularColor=lerp(float3(0.04,0.04,0.04),Albedo.xyz,metallic);//能量守恒
               float D=DistributionGGX(NdotH,roughness);
               float G=GeometrySmith(NdotV,NdotL,roughness);
               float3 numerator=SpecularColor*D*G;
               float demonimator=4*NdotL*NdotV;
               float3 specular= numerator/demonimator*LightColor*NdotL*0.25;
                
               
               float3 ambient=Albedo.xyz*unity_AmbientSky.rgb;
               float3 finalColor=diffuse+specular+ambient;
               return float4(finalColor,1);
           }
           ENDHLSL
       }
   }
}
