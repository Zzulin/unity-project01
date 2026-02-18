Shader "Mypbr/pbr_1"
{
   Properties
   {
       _BaseMap("RGB basecolor A smoothness", 2D) = "white" {}
       _Metallic("Metallic add", Range(0,1)) = 1
       _MetallicMap("Metallic", 2D) = "gray" {}
       _NormalMap("Normal", 2D) = "bump" {}
       _Roughness("Roughness add", Range(0,1)) = 0.5
       
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
           #pragma multi_compile _ _ADDITIONAL_LIGHTS 
           #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS//开启附加光阴影
           //主pass阴影添加
           #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE//开启主光级联阴影
           #pragma multi_compile _ _MAIN_LIGHT_SHADOWS//开启主光阴影
           
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
           //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
           float _Metallic;
           float _Roughness; 
           
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
               float4 positionOS : POSITION;
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
               float4 shadowCoord : TEXCOORD6;//阴影贴图坐标 float4
           };
           v2f vert (appdata v)
           {
               v2f o;
               //阴影坐标
               VertexPositionInputs vertexinput=GetVertexPositionInputs(v.positionOS);//获取顶点世界 相机 裁剪 NDC坐标
               o.shadowCoord=GetShadowCoord(vertexinput);
               
               o.positionCS = vertexinput.positionCS;
               o.positionWS = vertexinput.positionWS;
               o.NormalWS = TransformObjectToWorldNormal(v.normal);
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
               return NdotV/max(0.01,denom);//避免除0出现白色噪点
           }
           float GeometrySmith(float NdotV,float NdotL,float roughness)
           {
               float ggx_v=GeometrySchlickGGX(NdotV,roughness);
               float ggx_l=GeometrySchlickGGX(NdotL,roughness);
               return ggx_v*ggx_l;
           }
           float FresnelSchlick(float VdotH,float F0)
           {
               return F0+(1-F0)*pow(max(0,1-VdotH),5);
           }
           float3 CalculateBRDF(float3 normalWS,float3 viewDirWS,Light mainLight,float3 LightDir,float3 LightColor,float roughness,float metallic,float3 Albedo,float3 F0)
           {
               float3 halfDir=normalize(LightDir+viewDirWS);
               float NdotL=dot(normalWS,LightDir);
               float NdotV=dot(normalWS,viewDirWS);
               float NdotH=dot(normalWS,halfDir);
               float VdotH=dot(viewDirWS,halfDir);
               //直接光
               
               float D=DistributionGGX(NdotH,roughness);
               float G=GeometrySmith(NdotV,NdotL,roughness);
               float F=FresnelSchlick(VdotH,F0);//菲涅尔项 反射率随角度变化
               
               float3 KS=F;
               float3 KD=float3(1,1,1)-KS;
               KD*=1-metallic;//非金属才会漫反射
               
               float3 numerator=F*D*G;
               float demonimator=4*NdotL*NdotV;
               float3 specular= numerator/demonimator;
               float3 diffuse=KD*Albedo/(PI);
               float3 radiance=LightColor*mainLight.shadowAttenuation*mainLight.distanceAttenuation;//阴影衰减*距离衰减
               float3 mainlight=(diffuse+specular)*radiance*max(0,NdotL);
               //return float3(max(0,NdotL),max(0,NdotL),max(0,NdotL));
               return mainlight;
           }
           float4 frag (v2f i) : SV_Target
           {
               //采样 向量
               float3 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv));
               //切线空间->世界空间
               float3x3 TBN=float3x3(i.tangentWS,i.bitangentWS,i.NormalWS);
               float3 normalWS=normalize(mul(normalTS,TBN));
               float4 Albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
               float metallic= saturate(SAMPLE_TEXTURE2D(_MetallicMap,sampler_MetallicMap,i.uv).r+_Metallic);
               float3 viewDirWS=normalize(i.viewDirWS);
               float smoothness= Albedo.a;
               float roughness=saturate(1-smoothness+_Roughness);
               Light mainLight =GetMainLight(i.shadowCoord);//getMainLight 传入阴影坐标
               float3 LightDir=mainLight.direction;
               float3 LightColor=mainLight.color;
               //float3 shadowAttenuation=float3(mainLight.shadowAttenuation,mainLight.shadowAttenuation,mainLight.shadowAttenuation);
               float3 F0=lerp(0.04,Albedo,metallic);//根据金属度插值 金属度为0时 为0.04 金属的F0为Albedo记录的
               float3 mainlight=CalculateBRDF(normalWS,viewDirWS,mainLight,LightDir,LightColor,roughness,metallic,Albedo,F0);
               #ifdef _ADDITIONAL_LIGHTS
               uint additionalLightCount=GetAdditionalLightsCount();
                   #ifdef _ADDITIONAL_LIGHT_SHADOWS
                    float4 shadowMask=1;
                        #ifdef _LIGHTMAP_ON
                        shadowMask=SAMPLE_SHADOWMASK(i.uv);//采样静态烘焙的全局光照贴图
                        #else
                        shadowMask=unity_ProbesOcclusion;//采样动态烘焙的光照贴
                        #endif
                   #endif
               for(uint index=0;index<additionalLightCount;index++)
               {
                   #ifdef _ADDITIONAL_LIGHT_SHADOWS
                   Light addLight=GetAdditionalLight(index,i.positionWS,shadowMask);
                   #else
                   Light addLight=GetAdditionalLight(index,i.positionWS);
                   #endif
                   mainlight+=CalculateBRDF(normalWS,viewDirWS,addLight,addLight.direction,addLight.color,roughness,metallic,Albedo,F0);
               }
               #endif
               
               // 正确的PBR环境光实现 - IBL
               // // 1. 漫反射环境光
               // float3 diffuseGI=SampleSH(normalWS);
               // float3 ambientDiffuse=diffuseGI*diffuseColor;
               //
               // // 2. 镜面反射环境光 - 使用反射探针
               // float3 reflectDir=reflect(-viewDirWS,normalWS);
               // float4 encodedIrradiance=SAMPLE_TEXTURECUBE(unity_SpecCube0, samplerunity_SpecCube0, reflectDir);
               // float3 decodedIrradiance=DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
               //
               // // 菲涅尔反射已经包含了能量守恒信息
               // //环境镜面反射比例
               // //float specularGIlerp=lerp(0.1,1,F);
               // float3 specularGI=decodedIrradiance*F;
               //
               // // 3. 最终环境光 = 漫反射环境光 + 镜面反射环境光
               // float3 ambient=ambientDiffuse+specularGI;
               //
               float3 ambient =Albedo*unity_AmbientSky.rgb;
               float3 finalColor=mainlight+ambient;
               return float4(finalColor,1);  
           }
           ENDHLSL
       }
        pass
        {
            Name "ShadowCaster"
            Tags{"LightMode"="ShadowCaster"}
            ZWrite On
            ZTest LEqual
            Cull Back
            ColorMask 0//不写入颜色
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            ENDHLSL
            
        }
   }
}