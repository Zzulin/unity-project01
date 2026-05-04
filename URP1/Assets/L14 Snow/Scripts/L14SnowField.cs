using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class L14SnowField : MonoBehaviour
{
    private const int ComputeThreadGroupSize = 8;

    private static readonly int SnowStateId = Shader.PropertyToID("_SnowState");
    private static readonly int TextureResolutionId = Shader.PropertyToID("_TextureResolution");
    private static readonly int DeltaTimeId = Shader.PropertyToID("_DeltaTime");
    private static readonly int RecoverySpeedId = Shader.PropertyToID("_RecoverySpeed");
    private static readonly int RidgeSettleSpeedId = Shader.PropertyToID("_RidgeSettleSpeed");
    private static readonly int SnowStampsId = Shader.PropertyToID("_SnowStamps");
    private static readonly int SnowStampCountId = Shader.PropertyToID("_SnowStampCount");
    private static readonly int SnowStampIndexId = Shader.PropertyToID("_SnowStampIndex");
    private static readonly int SnowStampRectId = Shader.PropertyToID("_SnowStampRect");
    private static readonly int FieldSizeId = Shader.PropertyToID("_FieldSize");
    private static readonly int MaxDepressionId = Shader.PropertyToID("_MaxDepression");
    private static readonly int RidgeHeightId = Shader.PropertyToID("_RidgeHeight");
    private static readonly int PowderNoiseStrengthId = Shader.PropertyToID("_PowderNoiseStrength");
    private static readonly int BaseReliefStrengthId = Shader.PropertyToID("_BaseReliefStrength");
    private static readonly int BaseReliefScaleId = Shader.PropertyToID("_BaseReliefScale");

    [Header("渲染资源")]
    public Material snowMaterial;
    public ComputeShader snowCompute;

    [Header("雪地范围")]
    [Min(8f)] public float fieldSize = 96f;
    [Tooltip("雪面实际几何细分。数值越高，脚印凹陷越接近真实拓扑位移。")]
    [Range(64, 640)] public int meshResolution = 420;
    [Range(128, 1024)] public int textureResolution = 512;

    [Header("雪体参数")]
    [Range(0.05f, 1.2f)] public float maxDepression = 0.38f;
    [Range(0f, 0.45f)] public float ridgeHeight = 0.16f;
    [Range(0f, 1.2f)] public float powderNoiseStrength = 0.08f;
    [Range(0f, 0.35f)] public float baseReliefStrength = 0.13f;
    [Range(0.25f, 6f)] public float baseReliefScale = 1.55f;

    [Header("恢复与沉降")]
    [Tooltip("压痕恢复速度，0 表示永久保留。")]
    [Range(0f, 0.25f)] public float recoverySpeed = 0.012f;
    [Tooltip("脚印边缘堆雪沉降速度。")]
    [Range(0f, 0.5f)] public float ridgeSettleSpeed = 0.035f;

    [Header("交互容量")]
    [Range(1, 64)] public int maxStampCount = 32;

    private readonly Dictionary<L14SnowInteractor, Vector3> previousInteractorPositions = new Dictionary<L14SnowInteractor, Vector3>(32);
    private SnowStamp[] stampData = new SnowStamp[32];
    private ComputeBuffer stampBuffer;
    private Material runtimeMaterial;
    private bool ownsRuntimeMaterial;
    private Mesh runtimeMesh;
    private RenderTexture snowState;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int clearKernel = -1;
    private int decayKernel = -1;
    private int stampKernel = -1;
    private int stampOneKernel = -1;
    private int currentMeshResolution = -1;
    private int currentTextureResolution = -1;
    private int currentMaxStampCount = -1;
    private bool needsMeshRebuild = true;

    public int MeshResolution => currentMeshResolution > 0 ? currentMeshResolution : meshResolution;
    public int TextureResolution => currentTextureResolution > 0 ? currentTextureResolution : textureResolution;
    public int ActiveStampCount { get; private set; }
    public int MaxStampCount => currentMaxStampCount > 0 ? currentMaxStampCount : maxStampCount;
    public RenderTexture SnowState => snowState;

    private struct SnowStamp
    {
        public Vector4 path;
        public Vector4 shape;
    }

    private void OnEnable()
    {
        EnsureResources();
    }

    private void OnDisable()
    {
        ReleaseResources();
    }

    private void OnValidate()
    {
        fieldSize = Mathf.Max(8f, fieldSize);
        meshResolution = Mathf.Clamp(meshResolution, 64, 640);
        textureResolution = Mathf.Clamp(textureResolution, 128, 1024);
        maxStampCount = Mathf.Clamp(maxStampCount, 1, 64);
        needsMeshRebuild = true;
    }

    private void Update()
    {
        EnsureResources();

        if (snowState == null || snowCompute == null || !SystemInfo.supportsComputeShaders)
        {
            return;
        }

        float deltaTime = Application.isPlaying ? Time.deltaTime : 1f / 30f;
        DispatchDecay(deltaTime);
        BuildStampData(deltaTime);
        DispatchStamps();
        PushMaterialProperties();
    }

    [ContextMenu("Clear Snow State")]
    public void ClearSnowState()
    {
        EnsureResources();
        DispatchClear();
    }

    private void EnsureResources()
    {
        meshFilter = meshFilter != null ? meshFilter : GetComponent<MeshFilter>();
        meshRenderer = meshRenderer != null ? meshRenderer : GetComponent<MeshRenderer>();

        EnsureRuntimeMaterial();
        EnsureComputeKernels();
        EnsureStampBuffer();

        if (needsMeshRebuild || runtimeMesh == null || currentMeshResolution != meshResolution)
        {
            RebuildMesh();
        }

        if (snowState == null || currentTextureResolution != textureResolution)
        {
            RebuildSnowStateTexture();
        }

        PushMaterialProperties();
    }

    private void EnsureRuntimeMaterial()
    {
        if (runtimeMaterial != null)
        {
            return;
        }

        ownsRuntimeMaterial = false;
        if (snowMaterial != null && !Application.isPlaying)
        {
            runtimeMaterial = snowMaterial;
        }
        else if (snowMaterial != null)
        {
            runtimeMaterial = new Material(snowMaterial)
            {
                name = "L14 Snow Runtime Material",
                hideFlags = HideFlags.DontSave
            };
            ownsRuntimeMaterial = true;
        }
        else
        {
            Shader shader = Shader.Find("L14 Snow/GPU Heightfield Snow");
            if (shader != null)
            {
                runtimeMaterial = new Material(shader)
                {
                    name = "L14 Snow Runtime Material",
                    hideFlags = HideFlags.DontSave
                };
                ownsRuntimeMaterial = true;
            }
        }

        if (meshRenderer != null && runtimeMaterial != null)
        {
            meshRenderer.sharedMaterial = runtimeMaterial;
        }
    }

    private void EnsureComputeKernels()
    {
        if (snowCompute == null || clearKernel >= 0)
        {
            return;
        }

        clearKernel = snowCompute.FindKernel("ClearSnow");
        decayKernel = snowCompute.FindKernel("DecaySnow");
        stampKernel = snowCompute.FindKernel("StampSnow");
        stampOneKernel = snowCompute.FindKernel("StampOneSnow");
    }

    private void EnsureStampBuffer()
    {
        int targetCount = Mathf.Clamp(maxStampCount, 1, 64);
        if (stampBuffer != null && currentMaxStampCount == targetCount)
        {
            return;
        }

        if (stampBuffer != null)
        {
            stampBuffer.Release();
        }

        currentMaxStampCount = targetCount;
        stampData = new SnowStamp[currentMaxStampCount];
        stampBuffer = new ComputeBuffer(currentMaxStampCount, sizeof(float) * 8, ComputeBufferType.Structured);
    }

    private void RebuildMesh()
    {
        currentMeshResolution = Mathf.Clamp(meshResolution, 64, 640);
        int vertexSide = currentMeshResolution + 1;
        int vertexCount = vertexSide * vertexSide;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] indices = new int[currentMeshResolution * currentMeshResolution * 6];
        float halfSize = fieldSize * 0.5f;

        int vertex = 0;
        for (int z = 0; z < vertexSide; z++)
        {
            float v = z / (float)currentMeshResolution;
            for (int x = 0; x < vertexSide; x++)
            {
                float u = x / (float)currentMeshResolution;
                vertices[vertex] = new Vector3(Mathf.Lerp(-halfSize, halfSize, u), 0f, Mathf.Lerp(-halfSize, halfSize, v));
                uvs[vertex] = new Vector2(u, v);
                vertex++;
            }
        }

        int index = 0;
        for (int z = 0; z < currentMeshResolution; z++)
        {
            for (int x = 0; x < currentMeshResolution; x++)
            {
                int i0 = z * vertexSide + x;
                int i1 = i0 + 1;
                int i2 = i0 + vertexSide;
                int i3 = i2 + 1;
                indices[index++] = i0;
                indices[index++] = i2;
                indices[index++] = i1;
                indices[index++] = i1;
                indices[index++] = i2;
                indices[index++] = i3;
            }
        }

        if (runtimeMesh != null)
        {
            DestroyImmediate(runtimeMesh);
        }

        runtimeMesh = new Mesh
        {
            name = "L14 Snow Heightfield Mesh",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        runtimeMesh.SetVertices(vertices);
        runtimeMesh.SetUVs(0, uvs);
        runtimeMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        runtimeMesh.RecalculateBounds();
        runtimeMesh.bounds = new Bounds(Vector3.zero, new Vector3(fieldSize, maxDepression + ridgeHeight + baseReliefStrength + 2f, fieldSize));
        meshFilter.sharedMesh = runtimeMesh;
        needsMeshRebuild = false;
    }

    private void RebuildSnowStateTexture()
    {
        if (snowState != null)
        {
            snowState.Release();
            DestroyImmediate(snowState);
        }

        currentTextureResolution = Mathf.Clamp(textureResolution, 128, 1024);
        snowState = new RenderTexture(currentTextureResolution, currentTextureResolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
        {
            name = "L14 Runtime Snow Heightfield",
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        snowState.Create();
        DispatchClear();
    }

    private void DispatchClear()
    {
        if (snowCompute == null || snowState == null || clearKernel < 0)
        {
            return;
        }

        snowCompute.SetTexture(clearKernel, SnowStateId, snowState);
        snowCompute.SetInt(TextureResolutionId, currentTextureResolution);
        int groups = Mathf.CeilToInt(currentTextureResolution / (float)ComputeThreadGroupSize);
        snowCompute.Dispatch(clearKernel, groups, groups, 1);
    }

    private void DispatchDecay(float deltaTime)
    {
        if (snowCompute == null || snowState == null || decayKernel < 0)
        {
            return;
        }

        snowCompute.SetTexture(decayKernel, SnowStateId, snowState);
        snowCompute.SetInt(TextureResolutionId, currentTextureResolution);
        snowCompute.SetFloat(DeltaTimeId, deltaTime);
        snowCompute.SetFloat(RecoverySpeedId, recoverySpeed);
        snowCompute.SetFloat(RidgeSettleSpeedId, ridgeSettleSpeed);
        int groups = Mathf.CeilToInt(currentTextureResolution / (float)ComputeThreadGroupSize);
        snowCompute.Dispatch(decayKernel, groups, groups, 1);
    }

    private void BuildStampData(float deltaTime)
    {
        ActiveStampCount = 0;
        IReadOnlyList<L14SnowInteractor> interactors = L14SnowInteractor.ActiveInteractors;
        float halfSize = fieldSize * 0.5f;
        float invFieldSize = 1f / Mathf.Max(fieldSize, 0.001f);

        for (int i = 0; i < interactors.Count && ActiveStampCount < currentMaxStampCount; i++)
        {
            L14SnowInteractor interactor = interactors[i];
            if (interactor == null || !interactor.isActiveAndEnabled || interactor.radius <= 0.01f || interactor.strength <= 0f)
            {
                continue;
            }

            Vector3 current = interactor.transform.position;
            if (!previousInteractorPositions.TryGetValue(interactor, out Vector3 previous))
            {
                previous = current;
            }

            previousInteractorPositions[interactor] = current;

            Vector2 previousUv = WorldToSnowUv(previous, halfSize, invFieldSize);
            Vector2 currentUv = WorldToSnowUv(current, halfSize, invFieldSize);
            float radiusUv = Mathf.Max(1f / Mathf.Max(currentTextureResolution, 1), interactor.radius * invFieldSize);
            float velocity = Vector3.Distance(current, previous) / Mathf.Max(deltaTime, 0.0001f);
            float velocityBoost = Mathf.Lerp(0.72f, 1.28f, Mathf.Clamp01(velocity / 9f));

            stampData[ActiveStampCount] = new SnowStamp
            {
                path = new Vector4(previousUv.x, previousUv.y, currentUv.x, currentUv.y),
                shape = new Vector4(
                    radiusUv,
                    interactor.strength * velocityBoost,
                    interactor.ridgeStrength * velocityBoost,
                    interactor.hardness)
            };
            ActiveStampCount++;
        }
    }

    private Vector2 WorldToSnowUv(Vector3 worldPosition, float halfSize, float invFieldSize)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        return new Vector2((local.x + halfSize) * invFieldSize, (local.z + halfSize) * invFieldSize);
    }

    private void DispatchStamps()
    {
        if (ActiveStampCount <= 0 || snowCompute == null || snowState == null || stampBuffer == null)
        {
            return;
        }

        stampBuffer.SetData(stampData, 0, 0, ActiveStampCount);

        if (stampOneKernel >= 0)
        {
            snowCompute.SetTexture(stampOneKernel, SnowStateId, snowState);
            snowCompute.SetBuffer(stampOneKernel, SnowStampsId, stampBuffer);
            snowCompute.SetInt(TextureResolutionId, currentTextureResolution);

            for (int i = 0; i < ActiveStampCount; i++)
            {
                SnowStamp stamp = stampData[i];
                float radiusPixels = Mathf.Max(2f, stamp.shape.x * currentTextureResolution);
                Vector2 previous = new Vector2(stamp.path.x, stamp.path.y) * currentTextureResolution;
                Vector2 current = new Vector2(stamp.path.z, stamp.path.w) * currentTextureResolution;
                int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(previous.x, current.x) - radiusPixels * 1.45f), 0, currentTextureResolution - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(previous.x, current.x) + radiusPixels * 1.45f), 0, currentTextureResolution - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(previous.y, current.y) - radiusPixels * 1.45f), 0, currentTextureResolution - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(previous.y, current.y) + radiusPixels * 1.45f), 0, currentTextureResolution - 1);
                int width = maxX - minX + 1;
                int height = maxY - minY + 1;

                snowCompute.SetInt(SnowStampIndexId, i);
                snowCompute.SetInts(SnowStampRectId, minX, minY, width, height);
                snowCompute.Dispatch(
                    stampOneKernel,
                    Mathf.CeilToInt(width / (float)ComputeThreadGroupSize),
                    Mathf.CeilToInt(height / (float)ComputeThreadGroupSize),
                    1);
            }

            return;
        }

        if (stampKernel < 0)
        {
            return;
        }

        snowCompute.SetTexture(stampKernel, SnowStateId, snowState);
        snowCompute.SetBuffer(stampKernel, SnowStampsId, stampBuffer);
        snowCompute.SetInt(TextureResolutionId, currentTextureResolution);
        snowCompute.SetInt(SnowStampCountId, ActiveStampCount);
        int groups = Mathf.CeilToInt(currentTextureResolution / (float)ComputeThreadGroupSize);
        snowCompute.Dispatch(stampKernel, groups, groups, 1);
    }

    private void PushMaterialProperties()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        if (snowState != null)
        {
            runtimeMaterial.SetTexture(SnowStateId, snowState);
        }

        runtimeMaterial.SetFloat(FieldSizeId, fieldSize);
        runtimeMaterial.SetFloat(MaxDepressionId, maxDepression);
        runtimeMaterial.SetFloat(RidgeHeightId, ridgeHeight);
        runtimeMaterial.SetFloat(PowderNoiseStrengthId, powderNoiseStrength);
        runtimeMaterial.SetFloat(BaseReliefStrengthId, baseReliefStrength);
        runtimeMaterial.SetFloat(BaseReliefScaleId, baseReliefScale);
    }

    private void ReleaseResources()
    {
        if (stampBuffer != null)
        {
            stampBuffer.Release();
            stampBuffer = null;
        }

        if (snowState != null)
        {
            snowState.Release();
            DestroyImmediate(snowState);
            snowState = null;
        }

        if (runtimeMesh != null)
        {
            DestroyImmediate(runtimeMesh);
            runtimeMesh = null;
        }

        if (runtimeMaterial != null && ownsRuntimeMaterial)
        {
            DestroyImmediate(runtimeMaterial);
        }

        runtimeMaterial = null;
        ownsRuntimeMaterial = false;
    }
}
