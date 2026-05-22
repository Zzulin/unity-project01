using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[ExecuteAlways]
public sealed class L12GrassRenderer : MonoBehaviour
{
    private const int LodCount = 3;
    private const int ComputeThreadGroupSize = 64;

    private static readonly int SourceBladeDataId = Shader.PropertyToID("_SourceBladeData");
    private static readonly int VisibleBladeData0Id = Shader.PropertyToID("_VisibleBladeData0");
    private static readonly int VisibleBladeData1Id = Shader.PropertyToID("_VisibleBladeData1");
    private static readonly int VisibleBladeData2Id = Shader.PropertyToID("_VisibleBladeData2");
    private static readonly int VisibleBladeDataId = Shader.PropertyToID("_VisibleBladeData");
    private static readonly int FieldOriginId = Shader.PropertyToID("_FieldOrigin");
    private static readonly int FieldScaleId = Shader.PropertyToID("_FieldScale");
    private static readonly int FieldSizeId = Shader.PropertyToID("_FieldSize");
    private static readonly int BladeHeightId = Shader.PropertyToID("_BladeHeight");
    private static readonly int BladeWidthId = Shader.PropertyToID("_BladeWidth");
    private static readonly int WindStrengthId = Shader.PropertyToID("_WindStrength");
    private static readonly int WindScaleId = Shader.PropertyToID("_WindScale");
    private static readonly int WindSpeedId = Shader.PropertyToID("_WindSpeed");
    private static readonly int WindDirectionId = Shader.PropertyToID("_WindDirection");
    private static readonly int GustStrengthId = Shader.PropertyToID("_GustStrength");
    private static readonly int GustFrequencyId = Shader.PropertyToID("_GustFrequency");
    private static readonly int GustSpeedId = Shader.PropertyToID("_GustSpeed");
    private static readonly int GustWidthId = Shader.PropertyToID("_GustWidth");
    private static readonly int GustNoiseScaleId = Shader.PropertyToID("_GustNoiseScale");
    private static readonly int ShapeVariationId = Shader.PropertyToID("_ShapeVariation");
    private static readonly int TipBrightnessId = Shader.PropertyToID("_TipBrightness");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int TipColorId = Shader.PropertyToID("_TipColor");
    private static readonly int DensityTextureId = Shader.PropertyToID("_DensityTexture");
    private static readonly int InteractionTextureId = Shader.PropertyToID("_InteractionTexture");
    private static readonly int InteractionStrengthId = Shader.PropertyToID("_InteractionStrength");
    private static readonly int InteractionFlattenStrengthId = Shader.PropertyToID("_InteractionFlattenStrength");
    private static readonly int CameraPositionId = Shader.PropertyToID("_CameraPosition");
    private static readonly int FrustumPlanesId = Shader.PropertyToID("_FrustumPlanes");
    private static readonly int SourceOffsetId = Shader.PropertyToID("_SourceOffset");
    private static readonly int SourceCountId = Shader.PropertyToID("_SourceCount");
    private static readonly int MaxDrawDistanceId = Shader.PropertyToID("_MaxDrawDistance");
    private static readonly int CullPaddingId = Shader.PropertyToID("_CullPadding");
    private static readonly int Lod0DistanceId = Shader.PropertyToID("_Lod0Distance");
    private static readonly int Lod1DistanceId = Shader.PropertyToID("_Lod1Distance");
    private static readonly int DensityThresholdId = Shader.PropertyToID("_DensityThreshold");
    private static readonly int DensityInfluenceId = Shader.PropertyToID("_DensityInfluence");
    private static readonly int UseDensityTextureId = Shader.PropertyToID("_UseDensityTexture");

    [Header("渲染资源")]
    [InspectorName("草地材质")]
    public Material grassMaterial;
    [InspectorName("GPU 剔除计算")]
    public ComputeShader cullingCompute;
    [InspectorName("密度贴图")]
    public Texture2D densityMap;

    [Header("草地规模")]
    [InspectorName("每边基础草株数")]
    [Min(8)] public int bladesPerSide = 300;
    [Tooltip("基础草地覆盖尺寸（米）。最终范围 = Field Size * Transform Scale(XZ)。")]
    [InspectorName("基础覆盖边长")]
    [Min(1f)] public float fieldSize = 90f;
    [Tooltip("推荐开启。缩放草地区域时自动补充实例，尽量保持草间距不变。")]
    [InspectorName("缩放时保持密度")]
    public bool preserveDensityWhenResized = true;
    [Tooltip("目标草间距（世界米）。值越小越密；0.4 以上通常会显得过稀。0 表示按当前 Field Size / Blades Per Side 自动初始化一次。")]
    [InspectorName("目标草间距")]
    [Range(0.01f, 0.4f)] public float targetBladeSpacing = 0f;
    [Tooltip("为防止缩放过大时显存暴涨，自动补密会受这个上限保护。")]
    [InspectorName("单轴草株上限")]
    [Range(128, 1024)] public int maxBladesPerAxis = 1024;
    [InspectorName("草地分块数")]
    [Range(1, 32)] public int chunksPerSide = 12;
    [InspectorName("基础草高")]
    [Min(0.05f)] public float bladeHeight = 1.25f;
    [InspectorName("草叶宽度")]
    [Min(0.005f)] public float bladeWidth = 0.085f;
    [Tooltip("草叶面片根部宽度倍率。数值越大，底部越宽；配合 Blade Width 控制最终宽度。")]
    [InspectorName("根部宽度倍率")]
    [Range(0.35f, 2.5f)] public float bladeRootWidthScale = 1f;
    [Tooltip("矮草高度倍率。与 Max Height Scale 拉开后，草海会有更明显的高低层次。")]
    [InspectorName("高低层次：矮草倍率")]
    [Range(0.2f, 1f)] public float minBladeHeightScale = 0.45f;
    [Tooltip("高草高度倍率。推荐大于 1，让少量高草穿出整体草面。")]
    [InspectorName("高低层次：高草倍率")]
    [Range(1f, 2.5f)] public float maxBladeHeightScale = 1.55f;
    [Tooltip("草叶形状随机强度：宽窄、顶端偏移、轻微自旋和倾斜都会随它增强。")]
    [InspectorName("叶形随机度")]
    [Range(0f, 1f)] public float shapeVariation = 0.72f;

    [Header("剔除与 LOD")]
    [InspectorName("最远绘制距离")]
    [Min(5f)] public float maxDrawDistance = 115f;
    [InspectorName("剔除安全边距")]
    [Min(0.1f)] public float cullPadding = 3f;
    [InspectorName("近景精细距离")]
    [Min(1f)] public float lod0Distance = 26f;
    [InspectorName("中景过渡距离")]
    [Min(1f)] public float lod1Distance = 62f;
    [InspectorName("密度裁剪阈值")]
    [Range(0f, 1f)] public float densityThreshold = 0.08f;
    [InspectorName("密度贴图影响")]
    [Range(0f, 3f)] public float densityInfluence = 1f;

    [Header("风")]
    [InspectorName("微风摆动强度")]
    [Range(0f, 1.5f)] public float windStrength = 0.32f;
    [InspectorName("风纹大小")]
    [Min(0.01f)] public float windScale = 0.18f;
    [InspectorName("微风速度")]
    [Min(0f)] public float windSpeed = 1.8f;
    [InspectorName("风吹方向")]
    public Vector2 windDirection = new Vector2(0.86f, 0.42f);
    [InspectorName("阵风压弯强度")]
    [Range(0f, 2f)] public float gustStrength = 0.85f;
    [InspectorName("阵风间距")]
    [Min(0.01f)] public float gustFrequency = 0.065f;
    [InspectorName("阵风推进速度")]
    [Min(0f)] public float gustSpeed = 5.8f;
    [InspectorName("阵风带宽")]
    [Range(0.05f, 0.95f)] public float gustWidth = 0.34f;
    [InspectorName("阵风噪声碎度")]
    [Min(0.01f)] public float gustNoiseScale = 0.055f;

    [Header("交互压草纹理")]
    [FormerlySerializedAs("interactionTextureResolution")]
    [Tooltip("压草纹理尺寸。它影响压草边缘细腻程度，不直接改变压草强度。")]
    [InspectorName("压草纹理精度")]
    [Range(64, 512)] public int interactionTextureSize = 256;
    [Tooltip("压草力度。数值越大，草被推倒得越明显。")]
    [InspectorName("压草推开力度")]
    [Range(0f, 8f)] public float interactionStrength = 3.6f;
    [Tooltip("压草垂直压低强度。数值越大，走过路径越明显地塌下去。")]
    [InspectorName("压草塌陷强度")]
    [Range(0f, 2f)] public float interactionFlattenStrength = 0.85f;
    [FormerlySerializedAs("interactionRecovery")]
    [Tooltip("压草痕迹恢复速度。0 表示几乎不恢复；1-2 保留较久；3 为常用恢复；5 为快速恢复。")]
    [InspectorName("草痕恢复速度")]
    [Range(0f, 5f)] public float interactionFadeSpeed = 2.6f;

    [Header("颜色")]
    [InspectorName("草根深色")]
    public Color baseColor = new Color(0.11f, 0.34f, 0.12f, 1f);
    [InspectorName("草尖浅色")]
    public Color tipColor = new Color(0.46f, 0.68f, 0.22f, 1f);
    [InspectorName("草尖发光感")]
    [Range(0.5f, 2f)] public float tipBrightness = 1.22f;

    private readonly Vector4[] frustumPlaneData = new Vector4[6];
    private readonly MaterialPropertyBlock[] lodPropertyBlocks = new MaterialPropertyBlock[LodCount];
    private readonly Mesh[] lodMeshes = new Mesh[LodCount];
    private readonly ComputeBuffer[] visibleBladeBuffers = new ComputeBuffer[LodCount];
    private readonly ComputeBuffer[] argsBuffers = new ComputeBuffer[LodCount];
    private readonly uint[][] argsData = new uint[LodCount][];
    private readonly List<GrassChunk> chunks = new List<GrassChunk>(256);
    private readonly Dictionary<L12GrassInteractor, Vector3> previousInteractorPositions = new Dictionary<L12GrassInteractor, Vector3>(16);

    private Texture2D runtimeWhiteTexture;
    private Texture2D interactionTexture;
    private Color32[] interactionPixels;
    private ComputeBuffer bladeDataBuffer;
    private Material runtimeMaterial;
    private int currentBladesPerSide;
    private int currentBladesPerAxisX;
    private int currentBladesPerAxisZ;
    private int currentChunksPerSide;
    private int currentInteractionTextureResolution;
    private int cullKernel = -1;
    private int bladeCount;
    private float currentBladeRootWidthScale = -1f;
    private bool needsRebuild = true;

    public int SourceBladeCount => bladeCount;
    public int ChunkCount => chunks.Count;
    public int VisibleChunkCount { get; private set; }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        EnsureResources();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        ReleaseResources();
    }

    private void OnValidate()
    {
        bladesPerSide = Mathf.Clamp(bladesPerSide, 8, 1200);
        if (targetBladeSpacing <= 0.001f)
        {
            targetBladeSpacing = fieldSize / Mathf.Clamp(bladesPerSide, 8, 1200);
        }
        targetBladeSpacing = Mathf.Clamp(targetBladeSpacing, 0.01f, 0.4f);
        maxBladesPerAxis = Mathf.Clamp(maxBladesPerAxis, 128, 1024);
        chunksPerSide = Mathf.Clamp(chunksPerSide, 1, 32);
        fieldSize = Mathf.Max(1f, fieldSize);
        bladeHeight = Mathf.Max(0.05f, bladeHeight);
        bladeWidth = Mathf.Max(0.005f, bladeWidth);
        bladeRootWidthScale = Mathf.Clamp(bladeRootWidthScale, 0.35f, 2.5f);
        minBladeHeightScale = Mathf.Clamp(minBladeHeightScale, 0.2f, 1f);
        maxBladeHeightScale = Mathf.Clamp(maxBladeHeightScale, 1f, 2.5f);
        if (maxBladeHeightScale < minBladeHeightScale)
        {
            maxBladeHeightScale = minBladeHeightScale;
        }
        shapeVariation = Mathf.Clamp01(shapeVariation);
        tipBrightness = Mathf.Clamp(tipBrightness, 0.5f, 2f);
        windDirection = windDirection.sqrMagnitude < 0.001f ? new Vector2(0.86f, 0.42f) : windDirection.normalized;
        maxDrawDistance = Mathf.Max(5f, maxDrawDistance);
        lod0Distance = Mathf.Clamp(lod0Distance, 1f, maxDrawDistance);
        lod1Distance = Mathf.Clamp(Mathf.Max(lod0Distance + 1f, lod1Distance), lod0Distance + 1f, maxDrawDistance);
        needsRebuild = true;
    }

    private void Update()
    {
        EnsureResources();

        if (bladeDataBuffer == null || runtimeMaterial == null || cullingCompute == null || !SystemInfo.supportsComputeShaders)
        {
            return;
        }

        UpdateInteractionTexture();
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderCamera)
    {
        if (!isActiveAndEnabled || renderCamera == null || renderCamera.cameraType == CameraType.Preview)
        {
            return;
        }

        EnsureResources();

        if (bladeDataBuffer == null || runtimeMaterial == null || cullingCompute == null || !SystemInfo.supportsComputeShaders)
        {
            return;
        }

        if (renderCamera == null)
        {
            return;
        }

        DispatchVisibleBladeCulling(renderCamera);
        PushCommonMaterialProperties();

        Vector2 scaledFieldSize = GetScaledFieldSizeXZ();
        Bounds bounds = new Bounds(
            transform.position + Vector3.up * bladeHeight,
            new Vector3(scaledFieldSize.x + 8f, bladeHeight * 4f + 4f, scaledFieldSize.y + 8f));

        for (int lod = 0; lod < LodCount; lod++)
        {
            if (lodMeshes[lod] == null || argsBuffers[lod] == null || visibleBladeBuffers[lod] == null)
            {
                continue;
            }

            lodPropertyBlocks[lod].SetBuffer(VisibleBladeDataId, visibleBladeBuffers[lod]);
            Graphics.DrawMeshInstancedIndirect(
                lodMeshes[lod],
                0,
                runtimeMaterial,
                bounds,
                argsBuffers[lod],
                0,
                lodPropertyBlocks[lod],
                ShadowCastingMode.Off,
                true,
                gameObject.layer,
                renderCamera);
        }
    }

    private void EnsureResources()
    {
        if (runtimeMaterial == null)
        {
            runtimeMaterial = grassMaterial;
            if (runtimeMaterial == null)
            {
                Shader shader = Shader.Find("L12 Grass/Interactive GPU Grass");
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader)
                    {
                        name = "L12 Grass Runtime Material"
                    };
                }
            }
        }

        if (cullingCompute != null && cullKernel < 0)
        {
            cullKernel = cullingCompute.FindKernel("CullGrass");
        }

        for (int i = 0; i < LodCount; i++)
        {
            if (lodPropertyBlocks[i] == null)
            {
                lodPropertyBlocks[i] = new MaterialPropertyBlock();
            }
        }

        float rootWidthScale = Mathf.Clamp(bladeRootWidthScale, 0.35f, 2.5f);
        if (lodMeshes[0] == null || !Mathf.Approximately(currentBladeRootWidthScale, rootWidthScale))
        {
            RebuildLodMeshes(rootWidthScale);
        }

        if (runtimeWhiteTexture == null)
        {
            runtimeWhiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
            {
                name = "L12 Runtime White Density"
            };
            runtimeWhiteTexture.SetPixel(0, 0, Color.white);
            runtimeWhiteTexture.Apply(false, true);
        }

        if (interactionTexture == null || currentInteractionTextureResolution != interactionTextureSize)
        {
            RebuildInteractionTexture();
        }

        Vector2Int effectiveBladeCounts = GetEffectiveBladeCounts();
        if (needsRebuild
            || currentBladesPerSide != bladesPerSide
            || currentBladesPerAxisX != effectiveBladeCounts.x
            || currentBladesPerAxisZ != effectiveBladeCounts.y
            || currentChunksPerSide != chunksPerSide
            || bladeDataBuffer == null)
        {
            RebuildBladeBuffer();
        }
    }

    private void RebuildBladeBuffer()
    {
        if (bladeDataBuffer != null)
        {
            bladeDataBuffer.Release();
            bladeDataBuffer = null;
        }

        currentBladesPerSide = Mathf.Clamp(bladesPerSide, 8, 1200);
        currentChunksPerSide = Mathf.Clamp(chunksPerSide, 1, 32);
        Vector2Int effectiveBladeCounts = GetEffectiveBladeCounts();
        currentBladesPerAxisX = effectiveBladeCounts.x;
        currentBladesPerAxisZ = effectiveBladeCounts.y;
        bladeCount = currentBladesPerAxisX * currentBladesPerAxisZ;

        Vector4[] bladeData = new Vector4[bladeCount];
        chunks.Clear();

        float spacingX = fieldSize / currentBladesPerAxisX;
        float spacingZ = fieldSize / currentBladesPerAxisZ;
        float halfSize = fieldSize * 0.5f;
        float chunkSize = fieldSize / currentChunksPerSide;
        var random = new System.Random(1205);
        int writeIndex = 0;

        for (int chunkZ = 0; chunkZ < currentChunksPerSide; chunkZ++)
        {
            for (int chunkX = 0; chunkX < currentChunksPerSide; chunkX++)
            {
                int xStart = Mathf.FloorToInt(chunkX * currentBladesPerAxisX / (float)currentChunksPerSide);
                int xEnd = Mathf.FloorToInt((chunkX + 1) * currentBladesPerAxisX / (float)currentChunksPerSide);
                int zStart = Mathf.FloorToInt(chunkZ * currentBladesPerAxisZ / (float)currentChunksPerSide);
                int zEnd = Mathf.FloorToInt((chunkZ + 1) * currentBladesPerAxisZ / (float)currentChunksPerSide);
                int offset = writeIndex;

                for (int z = zStart; z < zEnd; z++)
                {
                    for (int x = xStart; x < xEnd; x++)
                    {
                        float jitterX = ((float)random.NextDouble() - 0.5f) * spacingX * 0.92f;
                        float jitterZ = ((float)random.NextDouble() - 0.5f) * spacingZ * 0.92f;
                        float px = (x + 0.5f) * spacingX - halfSize + jitterX;
                        float pz = (z + 0.5f) * spacingZ - halfSize + jitterZ;
                        float yaw = (float)random.NextDouble() * Mathf.PI * 2f;
                        float heightT = Mathf.Pow((float)random.NextDouble(), 1.35f);
                        float scale = Mathf.Lerp(minBladeHeightScale, maxBladeHeightScale, heightT);
                        bladeData[writeIndex] = new Vector4(px, pz, yaw, scale);
                        writeIndex++;
                    }
                }

                float localX = (chunkX + 0.5f) * chunkSize - halfSize;
                float localZ = (chunkZ + 0.5f) * chunkSize - halfSize;
                chunks.Add(new GrassChunk(offset, writeIndex - offset, new Vector3(localX, bladeHeight, localZ), chunkSize));
            }
        }

        bladeDataBuffer = new ComputeBuffer(bladeCount, sizeof(float) * 4, ComputeBufferType.Structured);
        bladeDataBuffer.SetData(bladeData);
        RebuildVisibleBuffers();
        needsRebuild = false;
    }

    private void RebuildVisibleBuffers()
    {
        ReleaseVisibleBuffers();

        for (int lod = 0; lod < LodCount; lod++)
        {
            visibleBladeBuffers[lod] = new ComputeBuffer(bladeCount, sizeof(float) * 4, ComputeBufferType.Append);
            argsBuffers[lod] = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            argsData[lod] = new uint[5];
            argsData[lod][0] = lodMeshes[lod] != null ? lodMeshes[lod].GetIndexCount(0) : 0;
            argsData[lod][1] = 0;
            argsData[lod][2] = lodMeshes[lod] != null ? lodMeshes[lod].GetIndexStart(0) : 0;
            argsData[lod][3] = lodMeshes[lod] != null ? lodMeshes[lod].GetBaseVertex(0) : 0;
            argsData[lod][4] = 0;
            argsBuffers[lod].SetData(argsData[lod]);
        }
    }

    private void DispatchVisibleBladeCulling(Camera renderCamera)
    {
        for (int lod = 0; lod < LodCount; lod++)
        {
            visibleBladeBuffers[lod].SetCounterValue(0);
            argsData[lod][1] = 0;
            argsBuffers[lod].SetData(argsData[lod]);
        }

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(renderCamera);
        for (int i = 0; i < frustumPlaneData.Length; i++)
        {
            Vector3 normal = frustumPlanes[i].normal;
            frustumPlaneData[i] = new Vector4(normal.x, normal.y, normal.z, frustumPlanes[i].distance);
        }

        Vector2 fieldScale = GetFieldScaleXZ();
        Vector2 scaledFieldSize = GetScaledFieldSizeXZ();

        cullingCompute.SetBuffer(cullKernel, SourceBladeDataId, bladeDataBuffer);
        cullingCompute.SetBuffer(cullKernel, VisibleBladeData0Id, visibleBladeBuffers[0]);
        cullingCompute.SetBuffer(cullKernel, VisibleBladeData1Id, visibleBladeBuffers[1]);
        cullingCompute.SetBuffer(cullKernel, VisibleBladeData2Id, visibleBladeBuffers[2]);
        cullingCompute.SetTexture(cullKernel, DensityTextureId, densityMap != null ? densityMap : runtimeWhiteTexture);
        cullingCompute.SetVector(FieldOriginId, transform.position);
        cullingCompute.SetVector(FieldScaleId, new Vector4(fieldScale.x, fieldScale.y, 0f, 0f));
        cullingCompute.SetVector(FieldSizeId, new Vector4(scaledFieldSize.x, scaledFieldSize.y, 0f, 0f));
        cullingCompute.SetFloat(BladeHeightId, bladeHeight);
        cullingCompute.SetFloat(MaxDrawDistanceId, maxDrawDistance);
        cullingCompute.SetFloat(CullPaddingId, cullPadding);
        cullingCompute.SetFloat(Lod0DistanceId, lod0Distance);
        cullingCompute.SetFloat(Lod1DistanceId, lod1Distance);
        cullingCompute.SetFloat(DensityThresholdId, densityThreshold);
        cullingCompute.SetFloat(DensityInfluenceId, densityInfluence);
        cullingCompute.SetInt(UseDensityTextureId, densityMap != null ? 1 : 0);
        cullingCompute.SetVector(CameraPositionId, renderCamera.transform.position);
        cullingCompute.SetVectorArray(FrustumPlanesId, frustumPlaneData);

        VisibleChunkCount = 0;
        Vector3 cameraPosition = renderCamera.transform.position;
        float maxDistanceSqr = maxDrawDistance * maxDrawDistance;

        for (int i = 0; i < chunks.Count; i++)
        {
            GrassChunk chunk = chunks[i];
            Bounds chunkBounds = chunk.ToWorldBounds(transform.position, bladeHeight, fieldScale);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, chunkBounds))
            {
                continue;
            }

            Vector3 closestPoint = chunkBounds.ClosestPoint(cameraPosition);
            if ((closestPoint - cameraPosition).sqrMagnitude > maxDistanceSqr)
            {
                continue;
            }

            VisibleChunkCount++;
            cullingCompute.SetInt(SourceOffsetId, chunk.sourceOffset);
            cullingCompute.SetInt(SourceCountId, chunk.sourceCount);
            cullingCompute.Dispatch(cullKernel, Mathf.CeilToInt(chunk.sourceCount / (float)ComputeThreadGroupSize), 1, 1);
        }

        for (int lod = 0; lod < LodCount; lod++)
        {
            ComputeBuffer.CopyCount(visibleBladeBuffers[lod], argsBuffers[lod], sizeof(uint));
        }
    }

    private void PushCommonMaterialProperties()
    {
        Vector2 fieldScale = GetFieldScaleXZ();
        Vector2 scaledFieldSize = GetScaledFieldSizeXZ();

        for (int lod = 0; lod < LodCount; lod++)
        {
            MaterialPropertyBlock block = lodPropertyBlocks[lod];
            block.Clear();
            block.SetVector(FieldOriginId, transform.position);
            block.SetVector(FieldScaleId, new Vector4(fieldScale.x, fieldScale.y, 0f, 0f));
            block.SetVector(FieldSizeId, new Vector4(scaledFieldSize.x, scaledFieldSize.y, 0f, 0f));
            block.SetFloat(BladeHeightId, bladeHeight);
            block.SetFloat(BladeWidthId, bladeWidth);
            block.SetFloat(WindStrengthId, windStrength);
            block.SetFloat(WindScaleId, windScale);
            block.SetFloat(WindSpeedId, windSpeed);
            Vector2 normalizedWind = windDirection.sqrMagnitude < 0.001f ? new Vector2(0.86f, 0.42f) : windDirection.normalized;
            block.SetVector(WindDirectionId, new Vector4(normalizedWind.x, normalizedWind.y, 0f, 0f));
            block.SetFloat(GustStrengthId, gustStrength);
            block.SetFloat(GustFrequencyId, gustFrequency);
            block.SetFloat(GustSpeedId, gustSpeed);
            block.SetFloat(GustWidthId, gustWidth);
            block.SetFloat(GustNoiseScaleId, gustNoiseScale);
            block.SetFloat(ShapeVariationId, shapeVariation);
            block.SetFloat(TipBrightnessId, tipBrightness);
            block.SetColor(BaseColorId, baseColor);
            block.SetColor(TipColorId, tipColor);
            block.SetTexture(DensityTextureId, densityMap != null ? densityMap : runtimeWhiteTexture);
            block.SetTexture(InteractionTextureId, interactionTexture != null ? interactionTexture : runtimeWhiteTexture);
            block.SetFloat(InteractionStrengthId, interactionStrength);
            block.SetFloat(InteractionFlattenStrengthId, interactionFlattenStrength);
        }
    }

    private void RebuildInteractionTexture()
    {
        currentInteractionTextureResolution = Mathf.Clamp(interactionTextureSize, 64, 512);
        interactionPixels = new Color32[currentInteractionTextureResolution * currentInteractionTextureResolution];
        for (int i = 0; i < interactionPixels.Length; i++)
        {
            interactionPixels[i] = new Color32(128, 128, 0, 255);
        }

        interactionTexture = new Texture2D(currentInteractionTextureResolution, currentInteractionTextureResolution, TextureFormat.RGBA32, false, true)
        {
            name = "L12 Runtime Grass Interaction Texture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        interactionTexture.SetPixels32(interactionPixels);
        interactionTexture.Apply(false, false);
    }

    private void UpdateInteractionTexture()
    {
        if (interactionPixels == null || interactionTexture == null)
        {
            return;
        }

        const byte neutral = 128;
        float deltaTime = Application.isPlaying ? Time.deltaTime : 1f / 30f;
        float fadeRate = interactionFadeSpeed <= 0f ? 0f : interactionFadeSpeed * interactionFadeSpeed * 0.16f;
        float fade = 1f - Mathf.Exp(-fadeRate * deltaTime);
        for (int i = 0; i < interactionPixels.Length; i++)
        {
            Color32 pixel = interactionPixels[i];
            pixel.r = (byte)Mathf.RoundToInt(Mathf.Lerp(pixel.r, neutral, fade));
            pixel.g = (byte)Mathf.RoundToInt(Mathf.Lerp(pixel.g, neutral, fade));
            pixel.b = (byte)Mathf.RoundToInt(Mathf.Lerp(pixel.b, 0f, fade));
            pixel.a = 255;
            interactionPixels[i] = pixel;
        }

        IReadOnlyList<L12GrassInteractor> interactors = L12GrassInteractor.ActiveInteractors;
        Vector2 scaledFieldSize = GetScaledFieldSizeXZ();
        Vector2 invFieldSize = new Vector2(
            1f / Mathf.Max(scaledFieldSize.x, 0.001f),
            1f / Mathf.Max(scaledFieldSize.y, 0.001f));
        int resolution = currentInteractionTextureResolution;
        Vector2 halfSize = scaledFieldSize * 0.5f;

        for (int i = 0; i < interactors.Count; i++)
        {
            L12GrassInteractor interactor = interactors[i];
            if (interactor == null || !interactor.isActiveAndEnabled || interactor.radius <= 0.01f || interactor.strength <= 0f)
            {
                continue;
            }

            Vector3 position = interactor.transform.position;
            if (!previousInteractorPositions.TryGetValue(interactor, out Vector3 previousPosition))
            {
                previousPosition = position;
            }

            previousInteractorPositions[interactor] = position;

            float previousLocalX = previousPosition.x - transform.position.x;
            float previousLocalZ = previousPosition.z - transform.position.z;
            float currentLocalX = position.x - transform.position.x;
            float currentLocalZ = position.z - transform.position.z;

            float previousU = (previousLocalX + halfSize.x) * invFieldSize.x;
            float previousV = (previousLocalZ + halfSize.y) * invFieldSize.y;
            float currentU = (currentLocalX + halfSize.x) * invFieldSize.x;
            float currentV = (currentLocalZ + halfSize.y) * invFieldSize.y;

            Vector2 previousPixel = new Vector2(previousU * (resolution - 1), previousV * (resolution - 1));
            Vector2 currentPixel = new Vector2(currentU * (resolution - 1), currentV * (resolution - 1));
            Vector2 segment = currentPixel - previousPixel;
            float segmentLengthSqr = Mathf.Max(0.0001f, segment.sqrMagnitude);
            float radiusPixelsX = Mathf.Max(1f, interactor.radius * invFieldSize.x * resolution);
            float radiusPixelsY = Mathf.Max(1f, interactor.radius * invFieldSize.y * resolution);

            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(previousPixel.x, currentPixel.x)) - Mathf.CeilToInt(radiusPixelsX), 0, resolution - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(previousPixel.x, currentPixel.x)) + Mathf.CeilToInt(radiusPixelsX), 0, resolution - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(previousPixel.y, currentPixel.y)) - Mathf.CeilToInt(radiusPixelsY), 0, resolution - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(previousPixel.y, currentPixel.y)) + Mathf.CeilToInt(radiusPixelsY), 0, resolution - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 pixelPosition = new Vector2(x, y);
                    float t = Mathf.Clamp01(Vector2.Dot(pixelPosition - previousPixel, segment) / segmentLengthSqr);
                    Vector2 closestPoint = previousPixel + segment * t;
                    Vector2 delta = pixelPosition - closestPoint;
                    Vector2 normalizedDelta = new Vector2(delta.x / radiusPixelsX, delta.y / radiusPixelsY);
                    float distance01 = normalizedDelta.magnitude;
                    if (distance01 > 1f)
                    {
                        continue;
                    }

                    float influence = Mathf.SmoothStep(1f, 0f, distance01) * interactor.strength;
                    if (influence <= 0.001f)
                    {
                        continue;
                    }

                    float invLength = 1f / Mathf.Max(0.001f, delta.magnitude);
                    float directionX = delta.x * invLength;
                    float directionY = delta.y * invLength;
                    if (segment.sqrMagnitude > 0.001f && delta.sqrMagnitude <= 0.001f)
                    {
                        Vector2 moveDirection = segment.normalized;
                        directionX = moveDirection.x;
                        directionY = moveDirection.y;
                    }

                    int index = y * resolution + x;
                    Color32 pixel = interactionPixels[index];
                    byte pressure = (byte)Mathf.Clamp(Mathf.RoundToInt(influence * 255f), pixel.b, 255);
                    pixel.r = (byte)Mathf.Clamp(Mathf.RoundToInt(128f + directionX * influence * 127f), 0, 255);
                    pixel.g = (byte)Mathf.Clamp(Mathf.RoundToInt(128f + directionY * influence * 127f), 0, 255);
                    pixel.b = pressure;
                    pixel.a = 255;
                    interactionPixels[index] = pixel;
                }
            }
        }

        interactionTexture.SetPixels32(interactionPixels);
        interactionTexture.Apply(false, false);
    }

    private Vector2 GetFieldScaleXZ()
    {
        Vector3 scale = transform.lossyScale;
        return new Vector2(
            Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            Mathf.Max(0.001f, Mathf.Abs(scale.z)));
    }

    private Vector2 GetScaledFieldSizeXZ()
    {
        Vector2 scale = GetFieldScaleXZ();
        return new Vector2(
            Mathf.Max(0.001f, fieldSize * scale.x),
            Mathf.Max(0.001f, fieldSize * scale.y));
    }

    private Vector2Int GetEffectiveBladeCounts()
    {
        int baseBladesPerSide = Mathf.Clamp(bladesPerSide, 8, 1200);
        if (!preserveDensityWhenResized)
        {
            return new Vector2Int(baseBladesPerSide, baseBladesPerSide);
        }

        float spacing = targetBladeSpacing <= 0.001f
            ? fieldSize / baseBladesPerSide
            : Mathf.Clamp(targetBladeSpacing, 0.01f, 0.4f);
        Vector2 scaledFieldSize = GetScaledFieldSizeXZ();
        int maxAxis = Mathf.Clamp(maxBladesPerAxis, 128, 1024);
        int bladesX = Mathf.Clamp(Mathf.CeilToInt(scaledFieldSize.x / spacing), 8, maxAxis);
        int bladesZ = Mathf.Clamp(Mathf.CeilToInt(scaledFieldSize.y / spacing), 8, maxAxis);
        return new Vector2Int(bladesX, bladesZ);
    }

    private void RebuildLodMeshes(float rootWidthScale)
    {
        ReleaseLodMeshes();

        lodMeshes[0] = CreateBladeMesh(2, 5, rootWidthScale);
        lodMeshes[0].name = "L12 Grass Blade LOD0";

        lodMeshes[1] = CreateBladeMesh(2, 3, rootWidthScale);
        lodMeshes[1].name = "L12 Grass Blade LOD1";

        lodMeshes[2] = CreateBladeMesh(1, 1, rootWidthScale);
        lodMeshes[2].name = "L12 Grass Blade LOD2";

        currentBladeRootWidthScale = rootWidthScale;
        if (bladeDataBuffer != null)
        {
            RebuildVisibleBuffers();
        }
    }

    private static Mesh CreateBladeMesh(int cardCount, int segmentCount, float rootWidthScale)
    {
        segmentCount = Mathf.Max(1, segmentCount);
        rootWidthScale = Mathf.Max(0.05f, rootWidthScale);
        int vertsPerCard = segmentCount * 2 + 1;
        int vertCount = vertsPerCard * cardCount;
        int trisPerCard = Mathf.Max(0, segmentCount - 1) * 6 + 3;
        int triIndexCount = trisPerCard * cardCount;

        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] indices = new int[triIndexCount];

        int vertexCursor = 0;
        int indexCursor = 0;
        for (int card = 0; card < cardCount; card++)
        {
            float angle = card * Mathf.PI / cardCount;
            Vector3 sideAxis = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            int cardStart = vertexCursor;

            for (int segment = 0; segment < segmentCount; segment++)
            {
                float t = segment / (float)segmentCount;
                float halfWidth = Mathf.Pow(1f - t, 1.15f) * rootWidthScale;
                Vector3 centerOffset = Vector3.forward * (t * t * 0.08f);

                vertices[vertexCursor] = centerOffset - sideAxis * halfWidth;
                uvs[vertexCursor] = new Vector2(0f, t);
                vertexCursor++;

                vertices[vertexCursor] = centerOffset + sideAxis * halfWidth;
                uvs[vertexCursor] = new Vector2(1f, t);
                vertexCursor++;
            }

            int tipVertex = vertexCursor;
            vertices[vertexCursor] = Vector3.forward * 0.08f;
            uvs[vertexCursor] = new Vector2(0.5f, 1f);
            vertexCursor++;

            for (int segment = 0; segment < segmentCount - 1; segment++)
            {
                int bottomLeft = cardStart + segment * 2;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + 2;
                int topRight = bottomLeft + 3;

                indices[indexCursor++] = bottomLeft;
                indices[indexCursor++] = topLeft;
                indices[indexCursor++] = bottomRight;
                indices[indexCursor++] = bottomRight;
                indices[indexCursor++] = topLeft;
                indices[indexCursor++] = topRight;
            }

            int lastLeft = cardStart + (segmentCount - 1) * 2;
            int lastRight = lastLeft + 1;
            indices[indexCursor++] = lastLeft;
            indices[indexCursor++] = tipVertex;
            indices[indexCursor++] = lastRight;
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            uv = uvs,
            triangles = indices
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ReleaseResources()
    {
        if (bladeDataBuffer != null)
        {
            bladeDataBuffer.Release();
            bladeDataBuffer = null;
        }

        ReleaseVisibleBuffers();

        ReleaseLodMeshes();

        if (runtimeMaterial != null && runtimeMaterial != grassMaterial)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }
        }

        if (interactionTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(interactionTexture);
            }
            else
            {
                DestroyImmediate(interactionTexture);
            }

            interactionTexture = null;
            interactionPixels = null;
        }

        if (runtimeWhiteTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeWhiteTexture);
            }
            else
            {
                DestroyImmediate(runtimeWhiteTexture);
            }

            runtimeWhiteTexture = null;
        }

        runtimeMaterial = null;
        cullKernel = -1;
        currentBladesPerSide = 0;
        currentBladesPerAxisX = 0;
        currentBladesPerAxisZ = 0;
        currentChunksPerSide = 0;
        bladeCount = 0;
        previousInteractorPositions.Clear();
        needsRebuild = true;
    }

    private void ReleaseLodMeshes()
    {
        for (int i = 0; i < lodMeshes.Length; i++)
        {
            if (lodMeshes[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(lodMeshes[i]);
            }
            else
            {
                DestroyImmediate(lodMeshes[i]);
            }

            lodMeshes[i] = null;
        }

        currentBladeRootWidthScale = -1f;
    }

    private void ReleaseVisibleBuffers()
    {
        for (int i = 0; i < LodCount; i++)
        {
            if (visibleBladeBuffers[i] != null)
            {
                visibleBladeBuffers[i].Release();
                visibleBladeBuffers[i] = null;
            }

            if (argsBuffers[i] != null)
            {
                argsBuffers[i].Release();
                argsBuffers[i] = null;
            }
        }
    }

    private readonly struct GrassChunk
    {
        public readonly int sourceOffset;
        public readonly int sourceCount;
        private readonly Vector3 localCenter;
        private readonly float size;

        public GrassChunk(int sourceOffset, int sourceCount, Vector3 localCenter, float size)
        {
            this.sourceOffset = sourceOffset;
            this.sourceCount = sourceCount;
            this.localCenter = localCenter;
            this.size = size;
        }

        public Bounds ToWorldBounds(Vector3 origin, float height, Vector2 fieldScale)
        {
            Vector3 scaledCenter = origin + new Vector3(localCenter.x * fieldScale.x, localCenter.y, localCenter.z * fieldScale.y);
            return new Bounds(scaledCenter, new Vector3(size * fieldScale.x + 2f, height * 3f + 2f, size * fieldScale.y + 2f));
        }
    }
}
