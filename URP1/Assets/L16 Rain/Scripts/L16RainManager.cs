using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public sealed class L16RainManager : MonoBehaviour
{
    private static readonly int RainDropsId = Shader.PropertyToID("_RainDrops");
    private static readonly int CountId = Shader.PropertyToID("_Count");
    private static readonly int CameraPositionId = Shader.PropertyToID("_CameraPosition");
    private static readonly int VolumeSizeId = Shader.PropertyToID("_VolumeSize");
    private static readonly int TimeId = Shader.PropertyToID("_RainTime");
    private static readonly int WindId = Shader.PropertyToID("_Wind");
    private static readonly int FallSpeedId = Shader.PropertyToID("_FallSpeed");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int RainIntensityId = Shader.PropertyToID("_L16RainIntensity");
    private static readonly int RainWindId = Shader.PropertyToID("_L16RainWind");
    private static readonly int DropLengthId = Shader.PropertyToID("_DropLength");
    private static readonly int DropWidthId = Shader.PropertyToID("_DropWidth");
    private static readonly int DropTintId = Shader.PropertyToID("_DropTint");
    private static readonly int MaxDrawDistanceId = Shader.PropertyToID("_MaxDrawDistance");

    [Header("Resources")]
    public Material rainMaterial;
    public ComputeShader rainCompute;
    public Camera targetCamera;

    [Header("Quality")]
    [Range(0, 2)] public int qualityPreset = 1;
    [Min(512)] public int lowDropCount = 7000;
    [Min(512)] public int mediumDropCount = 16000;
    [Min(512)] public int highDropCount = 32000;

    [Header("Rain")]
    [Range(0f, 1f)] public float rainIntensity = 0.78f;
    public Vector2 wind = new Vector2(-0.75f, 0.28f);
    [Min(1f)] public float fallSpeed = 25f;
    [Min(1f)] public float volumeWidth = 62f;
    [Min(1f)] public float volumeHeight = 34f;
    [Min(1f)] public float volumeDepth = 62f;
    [Min(1f)] public float maxDrawDistance = 58f;

    [Header("Streak Shape")]
    [Min(0.01f)] public float dropLength = 1.85f;
    [Min(0.001f)] public float dropWidth = 0.018f;
    public Color dropTint = new Color(0.66f, 0.82f, 1f, 0.58f);

    private ComputeBuffer rainDropBuffer;
    private ComputeBuffer argsBuffer;
    private Mesh rainMesh;
    private Material runtimeRainMaterial;
    private int populateKernel = -1;
    private int currentDropCount = -1;
    private readonly uint[] args = new uint[5];

    public int ActiveDropCount => Mathf.RoundToInt(CurrentDropCount * rainIntensity);
    public int CurrentDropCount => qualityPreset <= 0 ? lowDropCount : qualityPreset == 1 ? mediumDropCount : highDropCount;
    public string QualityLabel => qualityPreset <= 0 ? "Low" : qualityPreset == 1 ? "Medium" : "High";

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        EnsureResources();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Shader.SetGlobalFloat(RainIntensityId, 0f);
        Shader.SetGlobalVector(RainWindId, Vector4.zero);
        ReleaseResources();
    }

    private void OnValidate()
    {
        qualityPreset = Mathf.Clamp(qualityPreset, 0, 2);
        lowDropCount = Mathf.Max(512, lowDropCount);
        mediumDropCount = Mathf.Max(512, mediumDropCount);
        highDropCount = Mathf.Max(512, highDropCount);
        fallSpeed = Mathf.Max(1f, fallSpeed);
        volumeWidth = Mathf.Max(1f, volumeWidth);
        volumeHeight = Mathf.Max(1f, volumeHeight);
        volumeDepth = Mathf.Max(1f, volumeDepth);
        maxDrawDistance = Mathf.Max(1f, maxDrawDistance);
        dropLength = Mathf.Max(0.01f, dropLength);
        dropWidth = Mathf.Max(0.001f, dropWidth);
    }

    private void Update()
    {
        PublishGlobals();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
        {
            return;
        }

        Camera renderCamera = targetCamera != null ? targetCamera : Camera.main;
        if (renderCamera != null && camera != renderCamera)
        {
            return;
        }

        EnsureResources();
        if (rainDropBuffer == null || argsBuffer == null || runtimeRainMaterial == null || rainMesh == null)
        {
            return;
        }

        DispatchRain(camera);
        DrawRain(camera);
    }

    private void EnsureResources()
    {
        if (runtimeRainMaterial == null && rainMaterial != null)
        {
            runtimeRainMaterial = Application.isPlaying ? new Material(rainMaterial) { name = "L16 Rain Runtime Material", hideFlags = HideFlags.DontSave } : rainMaterial;
        }

        if (rainMesh == null)
        {
            rainMesh = CreateRainQuad();
        }

        int targetCount = Mathf.Max(512, CurrentDropCount);
        if (rainDropBuffer == null || currentDropCount != targetCount)
        {
            ReleaseBuffers();
            currentDropCount = targetCount;
            rainDropBuffer = new ComputeBuffer(currentDropCount, sizeof(float) * 4, ComputeBufferType.Structured);
            argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
            args[0] = rainMesh.GetIndexCount(0);
            args[1] = (uint)currentDropCount;
            args[2] = rainMesh.GetIndexStart(0);
            args[3] = rainMesh.GetBaseVertex(0);
            args[4] = 0;
            argsBuffer.SetData(args);
        }

        if (rainCompute != null && populateKernel < 0)
        {
            populateKernel = rainCompute.FindKernel("PopulateRain");
        }
    }

    private void DispatchRain(Camera camera)
    {
        int activeCount = Mathf.Clamp(ActiveDropCount, 0, currentDropCount);
        args[1] = (uint)activeCount;
        argsBuffer.SetData(args);

        if (activeCount <= 0)
        {
            return;
        }

        if (SystemInfo.supportsComputeShaders && rainCompute != null && populateKernel >= 0)
        {
            rainCompute.SetBuffer(populateKernel, RainDropsId, rainDropBuffer);
            rainCompute.SetInt(CountId, activeCount);
            rainCompute.SetVector(CameraPositionId, camera.transform.position);
            rainCompute.SetVector(VolumeSizeId, new Vector4(volumeWidth, volumeHeight, volumeDepth, 0f));
            rainCompute.SetFloat(TimeId, Application.isPlaying ? Time.time : Time.realtimeSinceStartup);
            rainCompute.SetVector(WindId, new Vector4(wind.x, wind.y, 0f, 0f));
            rainCompute.SetFloat(FallSpeedId, fallSpeed);
            rainCompute.SetFloat(SeedId, transform.position.sqrMagnitude + currentDropCount * 0.0137f);
            rainCompute.Dispatch(populateKernel, Mathf.CeilToInt(activeCount / 64f), 1, 1);
        }

        runtimeRainMaterial.SetBuffer(RainDropsId, rainDropBuffer);
        runtimeRainMaterial.SetFloat(RainIntensityId, rainIntensity);
        runtimeRainMaterial.SetFloat(DropLengthId, dropLength);
        runtimeRainMaterial.SetFloat(DropWidthId, dropWidth);
        runtimeRainMaterial.SetColor(DropTintId, dropTint);
        runtimeRainMaterial.SetVector(WindId, new Vector4(wind.x, wind.y, 0f, 0f));
        runtimeRainMaterial.SetFloat(MaxDrawDistanceId, maxDrawDistance);
    }

    private void DrawRain(Camera camera)
    {
        Bounds bounds = new Bounds(camera.transform.position, new Vector3(volumeWidth + 20f, volumeHeight + 20f, volumeDepth + 20f));
        Graphics.DrawMeshInstancedIndirect(rainMesh, 0, runtimeRainMaterial, bounds, argsBuffer, 0, null, ShadowCastingMode.Off, false, gameObject.layer, camera);
    }

    private void PublishGlobals()
    {
        Shader.SetGlobalFloat(RainIntensityId, rainIntensity);
        Shader.SetGlobalVector(RainWindId, new Vector4(wind.x, wind.y, 0f, 0f));
    }

    private static Mesh CreateRainQuad()
    {
        Mesh mesh = new Mesh { name = "L16 Rain Streak Quad", hideFlags = HideFlags.DontSave };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 1f, 0f),
            new Vector3(0.5f, 1f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ReleaseResources()
    {
        ReleaseBuffers();
        if (Application.isPlaying && runtimeRainMaterial != null && runtimeRainMaterial != rainMaterial)
        {
            Destroy(runtimeRainMaterial);
        }

        if (rainMesh != null)
        {
            DestroyImmediate(rainMesh);
        }

        runtimeRainMaterial = null;
        rainMesh = null;
        populateKernel = -1;
    }

    private void ReleaseBuffers()
    {
        if (rainDropBuffer != null)
        {
            rainDropBuffer.Release();
            rainDropBuffer = null;
        }

        if (argsBuffer != null)
        {
            argsBuffer.Release();
            argsBuffer = null;
        }

        currentDropCount = -1;
    }
}
