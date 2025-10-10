using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageEffet : MonoBehaviour
{
    private Material ssaoMaterial;
    private Camera cam;
    [Range(0f,1f)]
    public float aoStrength = 0f; 
    [Range(4, 64)]
    public int SampleKernelCount = 64;
    private List<Vector4> sampleKernelList = new List<Vector4>();
    [Range(0.0001f,10f)]
    public float sampleKeneralRadius = 0.01f;
    [Range(0.0001f,1f)]
    public float rangeStrength = 0.001f;
    public float depthBiasValue;
    public Texture Nosie;//噪声贴图

    [Range(0, 2)]
    public int DownSample = 0;

    [Range(1, 4)]
    public int BlurRadius = 2;
    [Range(0, 0.2f)]
    public float bilaterFilterStrength = 0.2f;
    public bool OnlyShowAO = false;

    public enum SSAOPassName
    {
        GenerateAO = 0,
        BilateralFilter = 1,
        Composite = 2,
    }

    private void Awake()
    {
        var shader = Shader.Find("ImageEffect/SSAO");
        ssaoMaterial = new Material(shader);
    }

    private void Start()
    {
        //获取当前GameObject上附加的Camera组件
        cam = this.GetComponent<Camera>();
        //功能：设置相机的深度纹理模式，启用深度+法线纹理的生成
        //作用：通过位或操作符|，在保持原有深度纹理模式的基础上，额外启用DepthTextureMode.DepthNormals模式
        //重要性：这是SSAO效果实现的关键，因为SSAO需要深度和法线信息来计算环境光遮蔽
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.DepthNormals;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        GenerateAOSampleKernel();
        int rtW = source.width >> DownSample;//>> 是位右移操作符，相当于将数值除以2的DownSample次方
        int rtH = source.height >> DownSample;

        //AO
        RenderTexture aoRT = RenderTexture.GetTemporary(rtW,rtH,0);
        ssaoMaterial.SetVectorArray("_SampleKernelArray", sampleKernelList.ToArray());//sampleKernelList.ToArray()这行代码的作用是将C#中的List转换为Vector4数组，然后传递给Shader使用。
        ssaoMaterial.SetFloat("_RangeStrength", rangeStrength);
        ssaoMaterial.SetFloat("_AOStrength", aoStrength);
        ssaoMaterial.SetFloat("_SampleKernelCount", sampleKernelList.Count);
        ssaoMaterial.SetFloat("_SampleKeneralRadius",sampleKeneralRadius);
        ssaoMaterial.SetFloat("_DepthBiasValue",depthBiasValue);
        ssaoMaterial.SetTexture("_NoiseTex", Nosie);
        Graphics.Blit(source, aoRT, ssaoMaterial,(int)SSAOPassName.GenerateAO);
        //Blur
        RenderTexture blurRT = RenderTexture.GetTemporary(rtW,rtH,0);
        ssaoMaterial.SetFloat("_BilaterFilterFactor", 1.0f - bilaterFilterStrength);
        ssaoMaterial.SetVector("_BlurRadius", new Vector4(BlurRadius, 0, 0, 0));
        Graphics.Blit(aoRT, blurRT, ssaoMaterial, (int)SSAOPassName.BilateralFilter);

        ssaoMaterial.SetVector("_BlurRadius", new Vector4(0, BlurRadius, 0, 0));
        if (OnlyShowAO)
        {
            Graphics.Blit(blurRT, destination, ssaoMaterial, (int)SSAOPassName.BilateralFilter);
        }
        else
        {
            Graphics.Blit(blurRT, aoRT, ssaoMaterial, (int)SSAOPassName.BilateralFilter);
            ssaoMaterial.SetTexture("_AOTex", aoRT);
            Graphics.Blit(source, destination, ssaoMaterial, (int)SSAOPassName.Composite);
        }

        RenderTexture.ReleaseTemporary(aoRT);
        RenderTexture.ReleaseTemporary(blurRT);
    }

    private void GenerateAOSampleKernel()
    {
        if (SampleKernelCount == sampleKernelList.Count)
            return;
        sampleKernelList.Clear();
        for (int i = 0; i < SampleKernelCount; i++)
        {
            var vec = new Vector4(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(0, 1.0f), 1.0f);
            vec.Normalize();
            var scale = (float)i / SampleKernelCount;
            //使分布符合二次方程的曲线
            scale = Mathf.Lerp(0.01f, 1.0f, scale * scale);//重新分布SSAO（屏幕空间环境光遮蔽）算法中的采样点使采样点在空间中呈现更合理的分布密度
            vec *= scale;
            sampleKernelList.Add(vec);
        }
    }
}
