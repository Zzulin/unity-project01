Shader "Mypbr/pbr_1"
{
   Properties
   {
       [MainTexture]_BaseMap("RGB basecolor A smoothness", 2D) = "white" {}   //假如自己加metapass要有 最好加入MainTexture前
       
       _Metallic("Metallic add", Range(-1,1)) = 1
       [NoScaleOffset]_MetallicMap("Metallic", 2D) = "gray" {}
       [NoScaleOffset]_NormalMap("Normal", 2D) = "bump" {}
       _NormalMapScale("NormalMapScale", Range(0,2)) = 1
       _Roughness("Roughness add", Range(-1,1)) = 0.5
       _OcclusionMap("Occlusion", 2D) = "white" {}
       _OcclusionStrength("OcclusionStrength", Range(0,1)) = 1
       
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
           #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN//main light shadows
           #pragma multi_compile _ _SHADOWS_SOFT//soft shadows
           #pragma multi_compile _ LIGHTMAP_ON//开启lightmap
           #pragma multi_compile _ SHADOWS_SHADOWMASK//开启阴影贴图
           
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
           //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
           float _Metallic;
           float _Roughness; 
           float _NormalMapScale;
           float _OcclusionStrength;
           
           SAMPLER(sampler_BaseMap);
           Texture2D _BaseMap;
           float4 _BaseMap_ST;

           SAMPLER(sampler_MetallicMap);
           Texture2D _MetallicMap;
           

           SAMPLER(sampler_NormalMap);
           Texture2D _NormalMap;
         
           
           SAMPLER(sampler_OcclusionMap);
           Texture2D _OcclusionMap;
    
           
           struct appdata
           {
               float4 positionOS : POSITION;
               float3 normal : NORMAL;
               float2 uv : TEXCOORD0;
               float4 tangent : TANGENT;
               #ifdef LIGHTMAP_ON
               float2 lightmapUV : TEXCOORD1;
               #endif
               
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
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD6;//shadow coord
                #endif
               #ifdef LIGHTMAP_ON
               float2 lightmapUV : TEXCOORD7;
               #endif
           };
           v2f vert (appdata v)
           {
               v2f o;
               //阴影坐标
               VertexPositionInputs vertexinput=GetVertexPositionInputs(v.positionOS);//获取顶点世界 相机 裁剪 NDC坐标
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                o.shadowCoord=GetShadowCoord(vertexinput);
                #endif
               
               o.positionCS = vertexinput.positionCS;
               o.positionWS = vertexinput.positionWS;
               o.NormalWS = TransformObjectToWorldNormal(v.normal);
               o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
               o.viewDirWS=GetWorldSpaceViewDir(o.positionWS);
               o.tangentWS=TransformObjectToWorld(v.tangent.xyz);
               //乘以切向的w分量保证如果有模型对称的情况让副切线的方向一致
               o.bitangentWS=cross(o.NormalWS,o.tangentWS)*v.tangent.w;
               #ifdef LIGHTMAP_ON
               //需要平移和缩放uv 适应烘焙出来的Lightmap
               o.lightmapUV.xy=v.lightmapUV*unity_LightmapST.xy+unity_LightmapST.zw;
               #endif
               
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
               float OneminF0=1-F0;
               return F0+OneminF0*pow(max(0,1-VdotH),5);
           }
           float3 FresnelSchlickNdotVRoughness(float NdotV,float F0,float roughness)
           {
               float OneminF0=max((1-roughness),F0)-F0;
               return F0+OneminF0*pow(max(0,1-NdotV),5);
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
               //return float3(D,D,D);
               return mainlight;
           }
           float4 frag (v2f i) : SV_Target
           {
               //采样 向量
               float3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv),_NormalMapScale);
               //切线空间->世界空间
               float3x3 TBN=float3x3(i.tangentWS,i.bitangentWS,i.NormalWS);
               float3 normalWS=normalize(mul(normalTS,TBN));
               float4 Albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
               float metallic= saturate(SAMPLE_TEXTURE2D(_MetallicMap,sampler_MetallicMap,i.uv).r+_Metallic);
               float3 viewDirWS=normalize(i.viewDirWS);
               float smoothness= Albedo.a;
               float roughness=saturate(1-smoothness+_Roughness);
               //getMainLight 传入阴影坐标实时阴影
               //Light mainLight =GetMainLight(i.shadowCoord);
               float4 shadowMask=1;
                    #ifdef LIGHTMAP_ON
                    //unity的自动LOD远的时候采样静态烘焙的全局光照贴图 进的时候动态阴影  
                    shadowMask=SAMPLE_SHADOWMASK(i.lightmapUV);
                    #else
                    shadowMask=unity_ProbesOcclusion;
                    #endif
               //mainLight unity自动lod 摄像机近时实时阴影 远时采样烘焙阴影
                float4 shadowCoord;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                shadowCoord=i.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                #else
                shadowCoord=float4(0,0,0,0);
                #endif
                Light mainLight =GetMainLight(shadowCoord,i.positionWS,shadowMask);
               float3 LightDir=mainLight.direction;
               float3 LightColor=mainLight.color;
               //float3 shadowAttenuation=float3(mainLight.shadowAttenuation,mainLight.shadowAttenuation,mainLight.shadowAttenuation);
               float3 F0=lerp(0.04,Albedo,metallic);//根据金属度插值 金属度为0时 为0.04 金属的F0为Albedo记录的
               float3 mainlight=CalculateBRDF(normalWS,viewDirWS,mainLight,LightDir,LightColor,roughness,metallic,Albedo,F0);
               #ifdef _ADDITIONAL_LIGHTS
               uint additionalLightCount=GetAdditionalLightsCount();
                   #ifdef _ADDITIONAL_LIGHT_SHADOWS
                    
                   #endif
               //return float4(shadowMask.rgb,1);
               for(uint index=0;index<additionalLightCount;index++)
               {
                   #ifdef _ADDITIONAL_LIGHT_SHADOWS
                   Light addLight=GetAdditionalLight(index,i.positionWS,shadowMask);//additionallight屏幕空间阴影不需要传入阴影坐标
                   #else
                   Light addLight=GetAdditionalLight(index,i.positionWS);
                   #endif
                   mainlight+=CalculateBRDF(normalWS,viewDirWS,addLight,addLight.direction,addLight.color,roughness,metallic,Albedo,F0);
               }
               #endif
               
              
               //间接光镜面反射
               float3 KS=FresnelSchlickNdotVRoughness(dot(normalWS,viewDirWS),F0,roughness);//环境光KS是float3
               float3 reflectDir=reflect(-viewDirWS,normalWS);
               float4 encodedIrradiance=SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectDir,roughness*6);
               float3 decodedIrradiance=DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
               float3 specularAmbient=KS*decodedIrradiance;
               //间接光漫反射
               float3 irradience;
               #if LIGHTMAP_ON
               irradience=SampleLightmap(i.lightmapUV,i.NormalWS);
               #else
               irradience=SampleSH(normalWS);
               #endif
               float KD=(1-KS)*(1-metallic);
               float3 diffuseAmbient=KD*irradience*Albedo;
               //间接光漫反射+间接光镜面反射
               float occlusion=SAMPLE_TEXTURE2D(_OcclusionMap,sampler_OcclusionMap,i.uv).r;
               occlusion=lerp(1,occlusion,_OcclusionStrength);
               diffuseAmbient*=occlusion;
               specularAmbient*=lerp(occlusion,1.0,roughness*roughness);    
               float3 ambient =diffuseAmbient+specularAmbient;
               //直接光+环境光
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
//       Pass
//        {
//            Name "Meta"
//            Tags{"LightMode" = "Meta"}
//            Cull Off
//
//            HLSLPROGRAM
//            #pragma vertex CustomMetaVertex
//            #pragma fragment CustomMetaFragment
//
//            // 只引入最核心的两个基础库，避免多余的变量冲突
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
//
//            // 显式声明你的贴图和 ST (用于处理 UV 缩放/偏移)
//            SAMPLER(sampler_BaseMap);
//            Texture2D _BaseMap;
//            float4 _BaseMap_ST;
//
//            // 1. 完全由自己定义的顶点输入结构
//            struct AttributesMeta
//            {
//                float4 positionOS   : POSITION;
//                float2 uv           : TEXCOORD0;
//                float2 lightmapUV   : TEXCOORD1;
//            };
//
//            // 2. 完全由自己定义的片段输入结构（彻底干掉 v2f_meta 的报错）
//            struct VaryingsMeta
//            {
//                float4 positionCS   : SV_POSITION;
//                float2 uv           : TEXCOORD0;
//            };
//
//            // 3. 手写顶点着色器：把 3D 模型“展开”成 2D 光照贴图的形状
//            VaryingsMeta CustomMetaVertex(AttributesMeta input)
//            {
//                VaryingsMeta output;
//                // MetaVertexPosition 是底层核心函数，负责坐标转换
//                output.positionCS = MetaVertexPosition(input.positionOS, input.uv, input.lightmapUV, unity_LightmapST, unity_DynamicLightmapST);
//                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
//                return output;
//            }
//
//            // 4. 手写片段着色器
//            float4 CustomMetaFragment(VaryingsMeta input) : SV_Target
//            {
//                // 正常采样你的贴图
//                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
//
//                // 填充最终给烘焙器的数据
//                MetaInput metaInput;
//                ZERO_INITIALIZE(MetaInput, metaInput);
//                
//                // 乘以 0.85 是为了压低能量，防止过曝
//                metaInput.Albedo = albedo.rgb * 0.85;    
//                metaInput.Emission = half3(0,0,0);
//
//                // 把处理好的数据丢给引擎 (注意这里传的是 input.uv)
//                return MetaFragment(metaInput);
//            }
//            ENDHLSL
//        }
   }
}