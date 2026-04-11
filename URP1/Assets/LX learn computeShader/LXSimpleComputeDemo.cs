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

        kernelHandle = computeShader.FindKernel("CSMain");
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
            runtimeTexture.Release();
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
        runtimeTexture.Create();
    }

    private void CreateRuntimeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        runtimeMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Unlit/Texture"));
        runtimeMaterial.name = "LX Compute Runtime Material";

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

        int threadGroups = Mathf.CeilToInt(textureSize / (float)ThreadGroupSize);
        computeShader.Dispatch(kernelHandle, threadGroups, threadGroups, 1);
    }
}
