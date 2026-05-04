using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    private static readonly int FieldSizeId = Shader.PropertyToID("_FieldSize");
    private static readonly int BladeHeightId = Shader.PropertyToID("_BladeHeight");
    private static readonly int BladeWidthId = Shader.PropertyToID("_BladeWidth");
    private static readonly int WindStrengthId = Shader.PropertyToID("_WindStrength");
    private static readonly int WindScaleId = Shader.PropertyToID("_WindScale");
    private static readonly int WindSpeedId = Shader.PropertyToID("_WindSpeed");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int TipColorId = Shader.PropertyToID("_TipColor");
    private static readonly int DensityTextureId = Shader.PropertyToID("_DensityTexture");
    private static readonly int InteractionTextureId = Shader.PropertyToID("_InteractionTexture");
    private static readonly int InteractionStrengthId = Shader.PropertyToID("_InteractionStrength");
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
    public Material grassMaterial;
    public ComputeShader cullingCompute;
    public Texture2D densityMap;

    [Header("草地规模")]
    [Min(8)] public int bladesPerSide = 300;
    [Min(1f)] public float fieldSize = 90f;
    [Range(1, 32)] public int chunksPerSide = 12;
    [Min(0.05f)] public float bladeHeight = 1.25f;
    [Min(0.005f)] public float bladeWidth = 0.085f;

    [Header("剔除与 LOD")]
    [Min(5f)] public float maxDrawDistance = 115f;
    [Min(0.1f)] public float cullPadding = 3f;
    [Min(1f)] public float lod0Distance = 26f;
    [Min(1f)] public float lod1Distance = 62f;
    [Range(0f, 1f)] public float densityThreshold = 0.08f;
    [Range(0f, 1f)] public float densityInfluence = 1f;

    [Header("风")]
    [Range(0f, 1.5f)] public float windStrength = 0.32f;
    [Min(0.01f)] public float windScale = 0.18f;
    [Min(0f)] public float windSpeed = 1.8f;

    [Header("交互压草纹理")]
    [Range(64, 512)] public int interactionTextureResolution = 256;
    [Range(0f, 8f)] public float interactionStrength = 3.6f;
    [Range(0f, 1f)] public float interactionRecovery = 0.88f;

    [Header("颜色")]
    public Color baseColor = new Color(0.11f, 0.34f, 0.12f, 1f);
    public Color tipColor = new Color(0.46f, 0.68f, 0.22f, 1f);

    private readonly Vector4[] frustumPlaneData = new Vector4[6];
    private readonly MaterialPropertyBlock[] lodPropertyBlocks = new MaterialPropertyBlock[LodCount];
    private readonly Mesh[] lodMeshes = new Mesh[LodCount];
    private readonly ComputeBuffer[] visibleBladeBuffers = new ComputeBuffer[LodCount];
    private readonly ComputeBuffer[] argsBuffers = new ComputeBuffer[LodCount];
    private readonly uint[][] argsData = new uint[LodCount][];
    private readonly List<GrassChunk> chunks = new List<GrassChunk>(256);

    private Texture2D runtimeWhiteTexture;
    private Texture2D interactionTexture;
    private Color32[] interactionPixels;
    private ComputeBuffer bladeDataBuffer;
    private Material runtimeMaterial;
    private int currentBladesPerSide;
    private int currentChunksPerSide;
    private int currentInteractionTextureResolution;
    private int cullKernel = -1;
    private int bladeCount;
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
        bladesPerSide = Mathf.Clamp(bladesPerSide, 8, 600);
        chunksPerSide = Mathf.Clamp(chunksPerSide, 1, 32);
        fieldSize = Mathf.Max(1f, fieldSize);
        bladeHeight = Mathf.Max(0.05f, bladeHeight);
        bladeWidth = Mathf.Max(0.005f, bladeWidth);
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

        Bounds bounds = new Bounds(
            transform.position + Vector3.up * bladeHeight,
            new Vector3(fieldSize + 8f, bladeHeight * 4f + 4f, fieldSize + 8f));

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

        if (lodMeshes[0] == null)
        {
            lodMeshes[0] = CreateBladeMesh(3, 5);
            lodMeshes[0].name = "L12 Grass Blade LOD0";
        }

        if (lodMeshes[1] == null)
        {
            lodMeshes[1] = CreateBladeMesh(2, 3);
            lodMeshes[1].name = "L12 Grass Blade LOD1";
        }

        if (lodMeshes[2] == null)
        {
            lodMeshes[2] = CreateBladeMesh(1, 1);
            lodMeshes[2].name = "L12 Grass Blade LOD2";
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

        if (interactionTexture == null || currentInteractionTextureResolution != interactionTextureResolution)
        {
            RebuildInteractionTexture();
        }

        if (needsRebuild || currentBladesPerSide != bladesPerSide || currentChunksPerSide != chunksPerSide || bladeDataBuffer == null)
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

        currentBladesPerSide = Mathf.Clamp(bladesPerSide, 8, 600);
        currentChunksPerSide = Mathf.Clamp(chunksPerSide, 1, 32);
        bladeCount = currentBladesPerSide * currentBladesPerSide;

        Vector4[] bladeData = new Vector4[bladeCount];
        chunks.Clear();

        float spacing = fieldSize / currentBladesPerSide;
        float halfSize = fieldSize * 0.5f;
        float chunkSize = fieldSize / currentChunksPerSide;
        var random = new System.Random(1205);
        int writeIndex = 0;

        for (int chunkZ = 0; chunkZ < currentChunksPerSide; chunkZ++)
        {
            for (int chunkX = 0; chunkX < currentChunksPerSide; chunkX++)
            {
                int xStart = Mathf.FloorToInt(chunkX * currentBladesPerSide / (float)currentChunksPerSide);
                int xEnd = Mathf.FloorToInt((chunkX + 1) * currentBladesPerSide / (float)currentChunksPerSide);
                int zStart = Mathf.FloorToInt(chunkZ * currentBladesPerSide / (float)currentChunksPerSide);
                int zEnd = Mathf.FloorToInt((chunkZ + 1) * currentBladesPerSide / (float)currentChunksPerSide);
                int offset = writeIndex;

                for (int z = zStart; z < zEnd; z++)
                {
                    for (int x = xStart; x < xEnd; x++)
                    {
                        float jitterX = ((float)random.NextDouble() - 0.5f) * spacing * 0.92f;
                        float jitterZ = ((float)random.NextDouble() - 0.5f) * spacing * 0.92f;
                        float px = (x + 0.5f) * spacing - halfSize + jitterX;
                        float pz = (z + 0.5f) * spacing - halfSize + jitterZ;
                        float yaw = (float)random.NextDouble() * Mathf.PI * 2f;
                        float scale = Mathf.Lerp(0.62f, 1.32f, (float)random.NextDouble());
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

        cullingCompute.SetBuffer(cullKernel, SourceBladeDataId, bladeDataBuffer);
        cullingCompute.SetBuffer(cullKernel, VisibleBladeData0Id, visibleBladeBuffers[0]);
        cullingCompute.SetBuffer(cullKernel, VisibleBladeData1Id, visibleBladeBuffers[1]);
        cullingCompute.SetBuffer(cullKernel, VisibleBladeData2Id, visibleBladeBuffers[2]);
        cullingCompute.SetTexture(cullKernel, DensityTextureId, densityMap != null ? densityMap : runtimeWhiteTexture);
        cullingCompute.SetVector(FieldOriginId, transform.position);
        cullingCompute.SetFloat(FieldSizeId, fieldSize);
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
            Bounds chunkBounds = chunk.ToWorldBounds(transform.position, bladeHeight);
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
        for (int lod = 0; lod < LodCount; lod++)
        {
            MaterialPropertyBlock block = lodPropertyBlocks[lod];
            block.Clear();
            block.SetVector(FieldOriginId, transform.position);
            block.SetFloat(FieldSizeId, fieldSize);
            block.SetFloat(BladeHeightId, bladeHeight);
            block.SetFloat(BladeWidthId, bladeWidth);
            block.SetFloat(WindStrengthId, windStrength);
            block.SetFloat(WindScaleId, windScale);
            block.SetFloat(WindSpeedId, windSpeed);
            block.SetColor(BaseColorId, baseColor);
            block.SetColor(TipColorId, tipColor);
            block.SetTexture(DensityTextureId, densityMap != null ? densityMap : runtimeWhiteTexture);
            block.SetTexture(InteractionTextureId, interactionTexture != null ? interactionTexture : runtimeWhiteTexture);
            block.SetFloat(InteractionStrengthId, interactionStrength);
        }
    }

    private void RebuildInteractionTexture()
    {
        currentInteractionTextureResolution = Mathf.Clamp(interactionTextureResolution, 64, 512);
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

        byte neutral = 128;
        float recovery = Mathf.Clamp01(interactionRecovery);
        for (int i = 0; i < interactionPixels.Length; i++)
        {
            Color32 pixel = interactionPixels[i];
            pixel.r = (byte)Mathf.RoundToInt(Mathf.Lerp(neutral, pixel.r, recovery));
            pixel.g = (byte)Mathf.RoundToInt(Mathf.Lerp(neutral, pixel.g, recovery));
            pixel.b = (byte)Mathf.RoundToInt(pixel.b * recovery);
            pixel.a = 255;
            interactionPixels[i] = pixel;
        }

        IReadOnlyList<L12GrassInteractor> interactors = L12GrassInteractor.ActiveInteractors;
        float invFieldSize = 1f / Mathf.Max(fieldSize, 0.001f);
        int resolution = currentInteractionTextureResolution;
        float halfSize = fieldSize * 0.5f;

        for (int i = 0; i < interactors.Count; i++)
        {
            L12GrassInteractor interactor = interactors[i];
            if (interactor == null || !interactor.isActiveAndEnabled || interactor.radius <= 0.01f || interactor.strength <= 0f)
            {
                continue;
            }

            Vector3 position = interactor.transform.position;
            float localX = position.x - transform.position.x;
            float localZ = position.z - transform.position.z;
            float centerU = (localX + halfSize) * invFieldSize;
            float centerV = (localZ + halfSize) * invFieldSize;
            int centerX = Mathf.RoundToInt(centerU * (resolution - 1));
            int centerY = Mathf.RoundToInt(centerV * (resolution - 1));
            int radiusPixels = Mathf.CeilToInt(interactor.radius * invFieldSize * resolution);

            int minX = Mathf.Clamp(centerX - radiusPixels, 0, resolution - 1);
            int maxX = Mathf.Clamp(centerX + radiusPixels, 0, resolution - 1);
            int minY = Mathf.Clamp(centerY - radiusPixels, 0, resolution - 1);
            int maxY = Mathf.Clamp(centerY + radiusPixels, 0, resolution - 1);
            float radiusSqr = Mathf.Max(1f, radiusPixels * radiusPixels);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distSqr = dx * dx + dy * dy;
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    float distance01 = Mathf.Sqrt(distSqr / radiusSqr);
                    float influence = Mathf.SmoothStep(1f, 0f, distance01) * interactor.strength;
                    if (influence <= 0.001f)
                    {
                        continue;
                    }

                    float invLength = 1f / Mathf.Max(0.001f, Mathf.Sqrt(dx * dx + dy * dy));
                    float directionX = dx * invLength;
                    float directionY = dy * invLength;
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

    private static Mesh CreateBladeMesh(int cardCount, int segmentCount)
    {
        int vertsPerCard = (segmentCount + 1) * 2;
        int vertCount = vertsPerCard * cardCount;
        int trisPerCard = segmentCount * 6;
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

            for (int segment = 0; segment <= segmentCount; segment++)
            {
                float t = segment / (float)segmentCount;
                float halfWidth = (1f - t * 0.72f);
                Vector3 centerOffset = Vector3.forward * (t * t * 0.08f);

                vertices[vertexCursor] = centerOffset - sideAxis * halfWidth;
                uvs[vertexCursor] = new Vector2(0f, t);
                vertexCursor++;

                vertices[vertexCursor] = centerOffset + sideAxis * halfWidth;
                uvs[vertexCursor] = new Vector2(1f, t);
                vertexCursor++;
            }

            for (int segment = 0; segment < segmentCount; segment++)
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
        currentChunksPerSide = 0;
        bladeCount = 0;
        needsRebuild = true;
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

        public Bounds ToWorldBounds(Vector3 origin, float height)
        {
            return new Bounds(origin + localCenter, new Vector3(size + 2f, height * 3f + 2f, size + 2f));
        }
    }
}
