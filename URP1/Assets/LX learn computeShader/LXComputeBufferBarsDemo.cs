using UnityEngine;

public class LXComputeBufferBarsDemo : MonoBehaviour
{
    [Header("核心引用")]
    [SerializeField] private ComputeShader computeShader;

    [Header("Buffer 演示参数")]
    [SerializeField] private int sampleCount = 24;
    [SerializeField] private float amplitude = 1.4f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float frequency = 0.45f;
    [SerializeField] private float spacing = 0.42f;
    [SerializeField] private Vector3 baseBarSize = new Vector3(0.28f, 1f, 0.28f);

    private const int ThreadGroupSize = 64;

    private int kernelHandle = -1;
    private float[] inputData;
    private float[] outputData;
    private ComputeBuffer inputBuffer;
    private ComputeBuffer outputBuffer;
    private Transform[] barTransforms;
    private Material runtimeMaterial;

    private void Start()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogWarning("当前设备不支持 ComputeShader，这个 ComputeBuffer 演示不会运行。");
            enabled = false;
            return;
        }

        if (computeShader == null)
        {
            Debug.LogWarning("LXComputeBufferBarsDemo 缺少 ComputeShader 引用。");
            enabled = false;
            return;
        }

        sampleCount = Mathf.Max(1, sampleCount);
        kernelHandle = computeShader.FindKernel("CSMain");

        CreateBuffers();
        CreateBars();
        DispatchAndApply();
    }

    private void Update()
    {
        if (inputBuffer == null || outputBuffer == null)
        {
            return;
        }

        DispatchAndApply();
    }

    private void OnDestroy()
    {
        if (inputBuffer != null)
        {
            inputBuffer.Release();
            inputBuffer = null;
        }

        if (outputBuffer != null)
        {
            outputBuffer.Release();
            outputBuffer = null;
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 430f, 12f, 410f, 170f), GUI.skin.box);
        GUILayout.Label("ComputeBuffer 小例子");
        GUILayout.Label("1. CPU 创建 InputBuffer / OutputBuffer。");
        GUILayout.Label("2. CPU 把索引数组传给 GPU。");
        GUILayout.Label("3. GPU 并行算出每根柱子的高度。");
        GUILayout.Label("4. CPU 把结果 GetData 回来，再更新场景里的柱子。");
        GUILayout.EndArea();
    }

    private void CreateBuffers()
    {
        inputData = new float[sampleCount];
        outputData = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            inputData[i] = i;
        }

        inputBuffer = new ComputeBuffer(sampleCount, sizeof(float));
        outputBuffer = new ComputeBuffer(sampleCount, sizeof(float));

        inputBuffer.SetData(inputData);
        computeShader.SetBuffer(kernelHandle, "InputData", inputBuffer);
        computeShader.SetBuffer(kernelHandle, "OutputData", outputBuffer);
    }

    private void CreateBars()
    {
        barTransforms = new Transform[sampleCount];

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        runtimeMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
        runtimeMaterial.name = "LX ComputeBuffer Runtime Material";
        runtimeMaterial.color = new Color(0.2f, 0.8f, 1f, 1f);

        for (int i = 0; i < sampleCount; i++)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = $"Buffer Bar {i}";
            bar.transform.SetParent(transform, false);

            Collider barCollider = bar.GetComponent<Collider>();
            if (barCollider != null)
            {
                Destroy(barCollider);
            }

            MeshRenderer barRenderer = bar.GetComponent<MeshRenderer>();
            barRenderer.sharedMaterial = runtimeMaterial;

            barTransforms[i] = bar.transform;
        }
    }

    private void DispatchAndApply()
    {
        computeShader.SetInt("_Count", sampleCount);
        computeShader.SetFloat("_TimeValue", Time.time * speed);
        computeShader.SetFloat("_Amplitude", amplitude);
        computeShader.SetFloat("_Frequency", frequency);

        int threadGroups = Mathf.CeilToInt(sampleCount / (float)ThreadGroupSize);
        computeShader.Dispatch(kernelHandle, threadGroups, 1, 1);
        outputBuffer.GetData(outputData);

        float centerOffset = (sampleCount - 1) * 0.5f;

        for (int i = 0; i < sampleCount; i++)
        {
            float height = Mathf.Max(0.1f, outputData[i]);
            Transform bar = barTransforms[i];

            bar.localScale = new Vector3(baseBarSize.x, height, baseBarSize.z);
            bar.localPosition = new Vector3((i - centerOffset) * spacing, height * 0.5f, 0f);
        }
    }
}
