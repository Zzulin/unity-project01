//https://blog.csdn.net/tianhai110/article/details/5684128?utm_medium=distribute.pc_relevant.none-task-blog-BlogCommendFromMachineLearnPai2-7.channel_param&depth_1-utm_source=distribute.pc_relevant.none-task-blog-BlogCommendFromMachineLearnPai2-7.channel_param
Shader "ImageEffect/SSAO"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
    }

	CGINCLUDE
    #include "UnityCG.cginc"
	struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;//使用后处理的话uv就是屏幕空间坐标
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
		float3 viewVec : TEXCOORD1;
		float3 viewRay : TEXCOORD2;
    };

	#define MAX_SAMPLE_KERNEL_COUNT 64

	sampler2D _MainTex;
	//获取深度法线图
	sampler2D _CameraDepthNormalsTexture;
    
	//Ao
	sampler2D _NoiseTex;
	float4 _SampleKernelArray[MAX_SAMPLE_KERNEL_COUNT];
	float _SampleKernelCount;
	float _SampleKeneralRadius;
	float _DepthBiasValue;
	float _RangeStrength;
	float _AOStrength;
    v2f vert_Ao (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
		
		//计算相机空间中的像素方向（相机到像素的方向）
		//https://zhuanlan.zhihu.com/p/92315967
		//屏幕纹理坐标
		float4 screenPos = ComputeScreenPos(o.vertex);//输入的是裁剪空间坐标 输出的是屏幕空间坐标包含齐次坐标信息
		// NDC position
		float4 ndcPos = (screenPos / screenPos.w) * 2 - 1;
		// 计算至裁剪空间远屏幕方向 因为这时候没有深度值 所以只能计算至远屏幕方向
		float3 clipVec = float3(ndcPos.x, ndcPos.y, 1.0) * _ProjectionParams.z;//_ProjectionParams.z远裁剪平面距离值far
		o.viewVec = mul(unity_CameraInvProjection, clipVec.xyzz).xyz;//转到相机空间
        return o;
    }

	//Ao计算
    fixed4 frag_Ao (v2f i) : SV_Target
    {
        //采样屏幕纹理
        fixed4 col = tex2D(_MainTex, i.uv);

		//采样获得深度值和法线值
		float3 viewNormal;
		float linear01Depth;
		float4 depthnormal = tex2D(_CameraDepthNormalsTexture,i.uv);
		DecodeDepthNormal(depthnormal,linear01Depth,viewNormal);

		//获取像素相机屏幕坐标位置
		float3 viewPos = linear01Depth * i.viewVec;//原本i.viewVec是相机空间下限速的最远坐标 乘以Depth就是像素实际深度的坐标

		//获取像素相机屏幕法线，法相z方向相对于相机为负（so 需要乘以-1置反），并处理成单位向量
		viewNormal = normalize(viewNormal) * float3(1, 1, -1);

		//铺平纹理 因为使用了一个4*4的噪声纹理 所以需要将屏幕空间坐标映射到噪声纹理的坐标空间
		float2 noiseScale = _ScreenParams.xy / 4.0;//_ScreenParams.xy：这是Unity内置的变量，表示当前屏幕的像素尺寸（宽度和高度）这里将屏幕尺寸除以4，意味着噪声纹理将在屏幕上以4倍的大小进行平铺。这是一个优化手段，使用较小的噪声纹理来覆盖整个屏幕，减少内存占用。
		float2 noiseUV = i.uv * noiseScale;
    	// 计算出的noiseScale变量随后用于调整UV坐标，使噪声纹理在整个屏幕上重复平铺。
    	// 这在SSAO实现中很重要，因为我们需要为每个像素生成随机方向，但又不想为每个像素存储一个独立的随机向量（那样会消耗大量内存）
    	
		//randvec法线半球的随机向量
		float3 randvec = tex2D(_NoiseTex,noiseUV).xyz;//使构建法线半球的切向量随机(就能使法线半球随机旋转) 也就是构建随机正交基
		//Gramm-Schimidt处理创建正交基
		//法线&切线&副切线构成的坐标空间
		float3 tangent = normalize(randvec - viewNormal * dot(randvec,viewNormal));
		float3 bitangent = cross(viewNormal,tangent);
		float3x3 TBN = float3x3(tangent,bitangent,viewNormal);//这个矩阵可以将切线空间中的向量转换到视图空间view空间

		//采样核心
		float ao = 0;
		int sampleCount = _SampleKernelCount;//每个像素点上的采样次数
		//https://blog.csdn.net/qq_39300235/article/details/102460405
		for(int i=0;i<sampleCount;i++){
			
			float3 randomVec = mul(_SampleKernelArray[i].xyz,TBN);//得到法线半球上的随机向量并将切线空间值转换到view空间
			
			// ao权重
			// 这个权重值用于调整采样点对最终AO值的贡献
			// 距离中心较近的采样点（length(randomVec.xy)较小）会获得较高的权重
			// 距离中心较远的采样点（length(randomVec.xy)接近或超过0.2）会获得较低的权重
			// 这样做的目的是让距离较近的采样点在环境光遮蔽计算中起到更大的作用，这符合环境光遮蔽的物理特性，
			float weight = 1-smoothstep(0,0.2,length(randomVec.xy));
			
			//计算随机法线半球后的向量
			//将当前像素位置与缩放后的随机向量相加，得到新的采样点位置
			//转换到屏幕坐标 用来获取新的采样点的深度 _SampleKeneralRadius：这是采样半径，控制SSAO效果的影响范围
			float3 randomPos = viewPos + randomVec * _SampleKeneralRadius;
			
			//将视图空间中的采样点位置转换到裁剪空间
			//float(3x3)unity_CameraProjection：Unity内置的相机投影矩阵，强制转换为3x3矩阵
			//忽略投影矩阵的第四行（透视除法相关部分）为我们只需要坐标的相对位置关系，不需要完整的齐次坐标
			float3 rclipPos = mul((float3x3)unity_CameraProjection, randomPos);
			
			//执行透视除法并将裁剪空间坐标转换到屏幕空间坐标
			//透视除法：将齐次坐标转换为笛卡尔坐标在透视投影中，裁剪空间的坐标需要除以w分量（这里用z分量代替）得到归一化的设备坐标（NDC），范围[-1, 1]
			float2 rscreenPos = (rclipPos.xy / rclipPos.z) * 0.5 + 0.5;
			
			float randomDepth;
			float3 randomNormal;
			float4 rcdn = tex2D(_CameraDepthNormalsTexture, rscreenPos);//使用新采样点的屏幕空间坐标从深度法线纹理中采样得到该像素点的深度和法线
			DecodeDepthNormal(rcdn, randomDepth, randomNormal);//解码新采样点的深度法线纹理，获取随机采样点的深度值和法线值
			
			//判断累加ao值
			// 检查随机采样点与当前像素之间的深度差是否超过了一个阈值（_RangeStrength）。如果深度差过大，
			// 说明这两个点可能不在同一个表面上，因此不应该贡献遮蔽值，range被设为0.0。否则，range为1.0，表示该采样点可以贡献遮蔽值。
			float range = abs(randomDepth - linear01Depth) > _RangeStrength ? 0.0 : 1.0;
			// 自遮蔽问题
			// 这行代码通过比较采样点的深度值（加上一个小的偏移量）与当前像素的深度值来判断遮蔽关系。如果采样点的深度值小于当前像素的深度值，
			// 说明采样点在当前像素的前方，因此当前像素被采样点遮挡，selfCheck被设为1.0。否则，selfCheck为0.0，表示没有遮蔽关系。
			float selfCheck = randomDepth + _DepthBiasValue < linear01Depth ? 1.0 : 0.0;

			//只有当range和selfCheck都为1.0时，采样点才会对当前像素的环境光遮蔽值产生贡献。
			ao += range * selfCheck * weight;
		}

		ao = ao/sampleCount;
		ao = max(0.0, 1 - ao * _AOStrength);
		return float4(ao,ao,ao,1);
    }
	
	//Blur
	float _BilaterFilterFactor;
	float2 _MainTex_TexelSize;
	float2 _BlurRadius;

	///基于法线的双边滤波（Bilateral Filter）
	//https://blog.csdn.net/puppet_master/article/details/83066572
	float3 GetNormal(float2 uv)
	{
		float4 cdn = tex2D(_CameraDepthNormalsTexture, uv);	
		return DecodeViewNormalStereo(cdn);
	}

	half CompareNormal(float3 nor1,float3 nor2)
	{
		return smoothstep(_BilaterFilterFactor,1.0,dot(nor1,nor2));
	}
	
	fixed4 frag_Blur (v2f i) : SV_Target
	{
		//_MainTex_TexelSize -> https://forum.unity.com/threads/_maintex_texelsize-whats-the-meaning.110278/
		float2 delta = _MainTex_TexelSize.xy * _BlurRadius.xy;//计算采样步长，基于纹理像素大小和模糊半径
		
		float2 uv = i.uv;
		float2 uv0a = i.uv - delta;
		float2 uv0b = i.uv + delta;	
		float2 uv1a = i.uv - 2.0 * delta;
		float2 uv1b = i.uv + 2.0 * delta;
		float2 uv2a = i.uv - 3.0 * delta;
		float2 uv2b = i.uv + 3.0 * delta;
		
		float3 normal = GetNormal(uv);
		float3 normal0a = GetNormal(uv0a);
		float3 normal0b = GetNormal(uv0b);
		float3 normal1a = GetNormal(uv1a);
		float3 normal1b = GetNormal(uv1b);
		float3 normal2a = GetNormal(uv2a);
		float3 normal2b = GetNormal(uv2b);
		
		fixed4 col = tex2D(_MainTex, uv);
		fixed4 col0a = tex2D(_MainTex, uv0a);
		fixed4 col0b = tex2D(_MainTex, uv0b);
		fixed4 col1a = tex2D(_MainTex, uv1a);
		fixed4 col1b = tex2D(_MainTex, uv1b);
		fixed4 col2a = tex2D(_MainTex, uv2a);
		fixed4 col2b = tex2D(_MainTex, uv2b);
		
		half w = 0.37004405286;
		half w0a = CompareNormal(normal, normal0a) * 0.31718061674;
		half w0b = CompareNormal(normal, normal0b) * 0.31718061674;
		half w1a = CompareNormal(normal, normal1a) * 0.19823788546;
		half w1b = CompareNormal(normal, normal1b) * 0.19823788546;
		half w2a = CompareNormal(normal, normal2a) * 0.11453744493;
		half w2b = CompareNormal(normal, normal2b) * 0.11453744493;
		
		half3 result;
		result = w * col.rgb;
		result += w0a * col0a.rgb;
		result += w0b * col0b.rgb;
		result += w1a * col1a.rgb;
		result += w1b * col1b.rgb;
		result += w2a * col2a.rgb;
		result += w2b * col2b.rgb;
		
		result /= w + w0a + w0b + w1a + w1b + w2a + w2b;
		return fixed4(result, 1.0);
	}

	//应用AO贴图
	
	sampler2D _AOTex;

	fixed4 frag_Composite(v2f i) : SV_Target
	{
		fixed4 col = tex2D(_MainTex, i.uv);
		fixed4 ao = tex2D(_AOTex, i.uv);
		col.rgb *= ao.r;
		return col;
	}

	ENDCG

    SubShader
    {	
		Cull Off ZWrite Off ZTest Always

		//Pass 0 : Generate AO 
		Pass
        {
            CGPROGRAM
            #pragma vertex vert_Ao
            #pragma fragment frag_Ao
            ENDCG
        }
		//Pass 1 : Bilateral Filter Blur
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_Ao
			#pragma fragment frag_Blur
			ENDCG
		}

		//Pass 2 : Composite AO
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_Ao
			#pragma fragment frag_Composite
			ENDCG
		}
    }
}
