using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public sealed class L12GrassRenderer : MonoBehaviour
{
    private const int MaxInteractors = 8;
    private const int BladeMeshSegments = 4;
    private const int BladeMeshCards = 3;

    private static readonly int BladeDataId = Shader.PropertyToID("_BladeData");
    private static readonly int FieldOriginId = Shader.PropertyToID("_FieldOrigin");
    private static readonly int FieldSizeId = Shader.PropertyToID("_FieldSize");
    private static readonly int BladeHeightId = Shader.PropertyToID("_BladeHeight");
    private static readonly int BladeWidthId = Shader.PropertyToID("_BladeWidth");
    private static readonly int WindStrengthId = Shader.PropertyToID("_WindStrength");
    private static readonly int WindScaleId = Shader.PropertyToID("_WindScale");
    private static readonly int WindSpeedId = Shader.PropertyToID("_WindSpeed");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int TipColorId = Shader.PropertyToID("_TipColor");
    private static readonly int InteractorsId = Shader.PropertyToID("_Interactors");
    private static readonly int InteractorCountId = Shader.PropertyToID("_InteractorCount");

    [Header("渲染资源")]
    public Material grassMaterial;

    [Header("草地规模")]
    [Min(8)] public int bladesPerSide = 300;
    [Min(1f)] public float fieldSize = 90f;
    [Min(0.05f)] public float bladeHeight = 1.25f;
    [Min(0.005f)] public float bladeWidth = 0.085f;

    [Header("风")]
    [Range(0f, 1.5f)] public float windStrength = 0.32f;
    [Min(0.01f)] public float windScale = 0.18f;
    [Min(0f)] public float windSpeed = 1.8f;

    [Header("颜色")]
    public Color baseColor = new Color(0.18f, 0.46f, 0.13f, 1f);
    public Color tipColor = new Color(0.74f, 0.9f, 0.32f, 1f);

    private readonly Vector4[] interactorData = new Vector4[MaxInteractors];
    private ComputeBuffer bladeDataBuffer;
    private Material runtimeMaterial;
    private MaterialPropertyBlock propertyBlock;
    private Mesh bladeMesh;
    private int currentBladesPerSide;
    private int bladeCount;
    private bool needsRebuild = true;

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
        bladesPerSide = Mathf.Clamp(bladesPerSide, 8, 600);
        fieldSize = Mathf.Max(1f, fieldSize);
        bladeHeight = Mathf.Max(0.05f, bladeHeight);
        bladeWidth = Mathf.Max(0.005f, bladeWidth);
        needsRebuild = true;
    }

    private void Update()
    {
        EnsureResources();

        if (bladeMesh == null || bladeDataBuffer == null || runtimeMaterial == null)
        {
            return;
        }

        PushMaterialProperties();

        Bounds bounds = new Bounds(
            transform.position + Vector3.up * bladeHeight,
            new Vector3(fieldSize + 8f, bladeHeight * 4f + 4f, fieldSize + 8f));

        Graphics.DrawMeshInstancedProcedural(
            bladeMesh,
            0,
            runtimeMaterial,
            bounds,
            bladeCount,
            propertyBlock,
            ShadowCastingMode.Off,
            true,
            gameObject.layer);
    }

    private void EnsureResources()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

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

        if (bladeMesh == null)
        {
            bladeMesh = CreateBladeMesh();
            bladeMesh.name = "L12 Procedural Grass Blade";
        }

        if (needsRebuild || currentBladesPerSide != bladesPerSide || bladeDataBuffer == null)
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
        bladeCount = currentBladesPerSide * currentBladesPerSide;

        Vector4[] bladeData = new Vector4[bladeCount];
        float spacing = fieldSize / currentBladesPerSide;
        float halfSize = fieldSize * 0.5f;
        var random = new System.Random(1205);

        for (int z = 0; z < currentBladesPerSide; z++)
        {
            for (int x = 0; x < currentBladesPerSide; x++)
            {
                int index = z * currentBladesPerSide + x;
                float jitterX = ((float)random.NextDouble() - 0.5f) * spacing * 0.92f;
                float jitterZ = ((float)random.NextDouble() - 0.5f) * spacing * 0.92f;
                float px = (x + 0.5f) * spacing - halfSize + jitterX;
                float pz = (z + 0.5f) * spacing - halfSize + jitterZ;
                float yaw = (float)random.NextDouble() * Mathf.PI * 2f;
                float scale = Mathf.Lerp(0.62f, 1.32f, (float)random.NextDouble());
                bladeData[index] = new Vector4(px, pz, yaw, scale);
            }
        }

        bladeDataBuffer = new ComputeBuffer(bladeCount, sizeof(float) * 4, ComputeBufferType.Structured);
        bladeDataBuffer.SetData(bladeData);
        needsRebuild = false;
    }

    private void PushMaterialProperties()
    {
        propertyBlock.Clear();
        propertyBlock.SetBuffer(BladeDataId, bladeDataBuffer);
        propertyBlock.SetVector(FieldOriginId, transform.position);
        propertyBlock.SetFloat(FieldSizeId, fieldSize);
        propertyBlock.SetFloat(BladeHeightId, bladeHeight);
        propertyBlock.SetFloat(BladeWidthId, bladeWidth);
        propertyBlock.SetFloat(WindStrengthId, windStrength);
        propertyBlock.SetFloat(WindScaleId, windScale);
        propertyBlock.SetFloat(WindSpeedId, windSpeed);
        propertyBlock.SetColor(BaseColorId, baseColor);
        propertyBlock.SetColor(TipColorId, tipColor);

        int count = FillInteractorData();
        propertyBlock.SetInt(InteractorCountId, count);
        propertyBlock.SetVectorArray(InteractorsId, interactorData);
    }

    private int FillInteractorData()
    {
        for (int i = 0; i < interactorData.Length; i++)
        {
            interactorData[i] = Vector4.zero;
        }

        int written = 0;
        IReadOnlyList<L12GrassInteractor> interactors = L12GrassInteractor.ActiveInteractors;
        for (int i = 0; i < interactors.Count && written < MaxInteractors; i++)
        {
            L12GrassInteractor interactor = interactors[i];
            if (interactor == null || !interactor.isActiveAndEnabled || interactor.radius <= 0.01f || interactor.strength <= 0f)
            {
                continue;
            }

            Vector3 position = interactor.transform.position;
            interactorData[written] = new Vector4(position.x, position.z, interactor.radius, interactor.strength);
            written++;
        }

        return written;
    }

    private static Mesh CreateBladeMesh()
    {
        int vertsPerCard = (BladeMeshSegments + 1) * 2;
        int vertCount = vertsPerCard * BladeMeshCards;
        int trisPerCard = BladeMeshSegments * 6;
        int triIndexCount = trisPerCard * BladeMeshCards;

        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] indices = new int[triIndexCount];

        int vertexCursor = 0;
        int indexCursor = 0;
        for (int card = 0; card < BladeMeshCards; card++)
        {
            float angle = card * Mathf.PI / BladeMeshCards;
            Vector3 sideAxis = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            int cardStart = vertexCursor;

            for (int segment = 0; segment <= BladeMeshSegments; segment++)
            {
                float t = segment / (float)BladeMeshSegments;
                float halfWidth = (1f - t * 0.72f);
                Vector3 centerOffset = Vector3.forward * (t * t * 0.08f);

                vertices[vertexCursor] = centerOffset - sideAxis * halfWidth;
                uvs[vertexCursor] = new Vector2(0f, t);
                vertexCursor++;

                vertices[vertexCursor] = centerOffset + sideAxis * halfWidth;
                uvs[vertexCursor] = new Vector2(1f, t);
                vertexCursor++;
            }

            for (int segment = 0; segment < BladeMeshSegments; segment++)
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

        if (bladeMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(bladeMesh);
            }
            else
            {
                DestroyImmediate(bladeMesh);
            }

            bladeMesh = null;
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

        runtimeMaterial = null;
        currentBladesPerSide = 0;
        bladeCount = 0;
        needsRebuild = true;
    }
}
