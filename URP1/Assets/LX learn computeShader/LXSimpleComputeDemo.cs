using UnityEngine;

public class LXSimpleComputeDemo : MonoBehaviour
{
    [Header("核心引用")]
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private MeshRenderer targetRenderer;

    [Header("演示参数")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private float waveFrequency = 18f;
    [SerializeField] private float waveSpeed = 4f;

    private const int ThreadGroupSize = 8;

    private RenderTexture runtimeTexture;
    private Material runtimeMaterial;
    private int kernelHandle = -1;

    private void Start()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogWarning("当前设备不支持 ComputeShader，这个演示不会运行。");
            enabled = false;
            return;
        }

        if (computeShader == null || targetRenderer == null)
        {
            Debug.LogWarning("LXSimpleComputeDemo 缺少引用，请检查 ComputeShader 和目标 MeshRenderer。");
            enabled = false;
            return;
        }

        kernelHandle = computeShader.FindKernel("CSMain");//获取 ComputeShader 中的 CSMain 函数的句柄
        CreateRuntimeTexture();
        CreateRuntimeMaterial();
        DispatchCompute();
    }

    private void Update()
    {
        if (runtimeTexture != null)
        {
            DispatchCompute();
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }

        if (runtimeTexture != null)
        {
            runtimeTexture.Release();//释放 RenderTexture 资源
            //必须调用 Release() ，否则 GPU 资源会泄漏！
            Destroy(runtimeTexture);
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12f, 12f, 520f, 150f), GUI.skin.box);
        GUILayout.Label("ComputeShader 小例子");
        GUILayout.Label("CPU 只负责发指令和参数，GPU 里的很多线程一起算整张纹理。");
        GUILayout.Label("这个场景里：每个像素都由一个 GPU 线程计算颜色，然后实时贴到前方的面片上。");
        GUILayout.Label("你看到的流动波纹，就是 ComputeShader 在并行处理大量像素后的结果。");
        GUILayout.EndArea();
    }

    private void CreateRuntimeTexture()
    {
        runtimeTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        //RenderTexture 在 CPU 端（内存）分配后，还需要调用 Create() 在 GPU 端真正创建它。
        //可以在着色器中读写
        runtimeTexture.Create();
    }

    private void CreateRuntimeMaterial()
    {   
        // 1. 查找 URP Unlit Shader，如果找不到则使用内置 Unlit/Texture
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        //2. 三元运算符：如果 shader 存在用它，否则用备用 Shader
        runtimeMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Unlit/Texture"));
        //3. 给材质起个名字，方便调试时识别
        runtimeMaterial.name = "LX Compute Runtime Material";

        // 4. 兼容不同 Shader 的属性名称
        if (runtimeMaterial.HasProperty("_BaseMap"))
        {
            runtimeMaterial.SetTexture("_BaseMap", runtimeTexture);
        }
        else
        {
            runtimeMaterial.mainTexture = runtimeTexture;
        }

        targetRenderer.sharedMaterial = runtimeMaterial;
    }

    private void DispatchCompute()
    {
        computeShader.SetFloat("_TimeValue", Time.time);
        computeShader.SetFloat("_Resolution", textureSize);
        computeShader.SetFloat("_WaveFrequency", waveFrequency);
        computeShader.SetFloat("_WaveSpeed", waveSpeed);
        computeShader.SetTexture(kernelHandle, "Result", runtimeTexture);

        // 计算线程组数量，使用了 MathF.CeilToInt 和浮点数除法来向上取整。
        int threadGroups = Mathf.CeilToInt(textureSize / (float)ThreadGroupSize);
        //启用 ComputeShader  并行处理
        //每个线程处理一个像素，所以需要 threadGroups * threadGroups 个线程
        //2D 并行，覆盖 512×512 像素
        computeShader.Dispatch(kernelHandle, threadGroups, threadGroups, 1);
    }
}
