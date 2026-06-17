using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LXIIL12GrassSetup
{
    private const string ScenePath = "Assets/LXII game 整合/game.unity";
    private const string GrassRootName = "LXII L12 Grass Root";
    private const string GroundName = "LXII Grass Ground";
    private const string GrassFieldName = "LXII Grass Field";
    private const string PlayerName = "LXII Nilou Player";

    private const string GrassMaterialPath = "Assets/L12 grass/Materials/L12_InteractiveGrass.mat";
    private const string GroundMaterialPath = "Assets/L12 grass/Materials/L12_Ground.mat";
    private const string DensityMapPath = "Assets/L12 grass/Textures/L12_GrassDensity.asset";
    private const string CullingComputePath = "Assets/L12 grass/Shaders/L12GrassCull.compute";
    private const string MeshesFolder = "Assets/LXII game 整合/Meshes";
    private const string RollingGroundMeshPath = MeshesFolder + "/LXII_OpenWorldRollingGrassGround.asset";

    private const float GroundSize = 960f;
    private const float GrassFieldSize = 420f;
    private const int GroundResolution = 129;
    private const float TerrainHeightBase = 0f;
    private const float TerrainHeightAmplitude = 9f;
    private const float TerrainHeightFrequency = 0.018f;
    private const float TerrainDetailFrequency = 0.045f;
    private static readonly Vector2 TerrainHeightOffset = Vector2.zero;

    [MenuItem("Tools/LXII/Setup L12 Grass In Game Scene")]
    public static void SetupL12GrassInGameScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Material grassMaterial = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
        Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
        Texture2D densityMap = AssetDatabase.LoadAssetAtPath<Texture2D>(DensityMapPath);
        ComputeShader cullingCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(CullingComputePath);
        if (grassMaterial == null || groundMaterial == null || densityMap == null || cullingCompute == null)
        {
            Debug.LogError("[LXII] L12 草地资源加载不完整，已停止场景写入。");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = GameObject.Find(PlayerName);
        if (player == null)
        {
            LXIIAnimationTestSetup.SetupLxiAnimationTestInGameScene();
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            player = GameObject.Find(PlayerName);
        }

        if (player == null)
        {
            Debug.LogError("[LXII] game.unity 中仍未找到 LXII Nilou Player。");
            return;
        }

        RemoveExistingGrassRoot();

        GameObject grassRoot = new GameObject(GrassRootName);
        Undo.RegisterCreatedObjectUndo(grassRoot, "Setup LXII L12 Grass");

        CreateGround(grassRoot.transform, groundMaterial);
        CreateGrassField(grassRoot.transform, grassMaterial, densityMap, cullingCompute, GrassFieldSize);
        ConfigurePlayerForGrass(player);

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(grassRoot);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = grassRoot;
        Debug.Log("[LXII] 已在 game.unity 中接入 L12 大世界草地：960m 起伏地表 + 420m 贴坡草海 + 妮露压草交互。");
    }

    private static void CreateGround(Transform parent, Material groundMaterial)
    {
        GameObject ground = new GameObject(GroundName);
        ground.name = GroundName;
        ground.transform.SetParent(parent, false);

        Mesh terrainMesh = CreateOrUpdateRollingGroundMesh();
        MeshFilter meshFilter = ground.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = terrainMesh;

        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = ground.AddComponent<MeshRenderer>();
        }

        if (renderer != null)
        {
            renderer.sharedMaterial = groundMaterial;
        }

        MeshCollider collider = ground.AddComponent<MeshCollider>();
        collider.sharedMesh = terrainMesh;
    }

    private static void CreateGrassField(Transform parent, Material grassMaterial, Texture2D densityMap, ComputeShader cullingCompute, float fieldSize)
    {
        GameObject grass = new GameObject(GrassFieldName);
        grass.transform.SetParent(parent, false);

        L12GrassRenderer renderer = grass.AddComponent<L12GrassRenderer>();
        renderer.grassMaterial = grassMaterial;
        renderer.cullingCompute = cullingCompute;
        renderer.densityMap = densityMap;
        renderer.bladesPerSide = 1024;
        renderer.fieldSize = fieldSize;
        renderer.preserveDensityWhenResized = true;
        renderer.targetBladeSpacing = 0.4f;
        renderer.maxBladesPerAxis = 2048;
        renderer.chunksPerSide = 20;
        renderer.bladeHeight = 1.16f;
        renderer.bladeWidth = 0.076f;
        renderer.bladeRootWidthScale = 1f;
        renderer.maxDrawDistance = 210f;
        renderer.lod0Distance = 30f;
        renderer.lod1Distance = 92f;
        renderer.densityThreshold = 0.04f;
        renderer.densityInfluence = 1.35f;
        renderer.interactionTextureSize = 512;
        renderer.interactionStrength = 3.6f;
        renderer.interactionFlattenStrength = 0.85f;
        renderer.interactionFadeSpeed = 2.6f;
        renderer.windStrength = 0.28f;
        renderer.windScale = 0.15f;
        renderer.windSpeed = 1.55f;
        renderer.windDirection = new Vector2(0.86f, 0.42f).normalized;
        renderer.gustStrength = 0.72f;
        renderer.gustFrequency = 0.045f;
        renderer.gustSpeed = 4.6f;
        renderer.gustWidth = 0.38f;
        renderer.gustNoiseScale = 0.042f;
        renderer.baseColor = new Color(0.08f, 0.26f, 0.10f, 1f);
        renderer.tipColor = new Color(0.34f, 0.58f, 0.18f, 1f);
        renderer.tipBrightness = 1.04f;
        renderer.terrainHeightBase = TerrainHeightBase;
        renderer.terrainHeightAmplitude = TerrainHeightAmplitude;
        renderer.terrainHeightFrequency = TerrainHeightFrequency;
        renderer.terrainDetailFrequency = TerrainDetailFrequency;
        renderer.terrainHeightOffset = TerrainHeightOffset;
    }

    private static void ConfigurePlayerForGrass(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<CharacterController>(player);
        }

        GetPlayerBounds(player, out Bounds bounds);
        float height = Mathf.Clamp(bounds.size.y * 0.9f, 1.5f, 2.1f);
        float radius = Mathf.Clamp(Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.35f, 0.22f, 0.38f);
        controller.height = height;
        controller.radius = radius;
        controller.center = new Vector3(0f, height * 0.5f, 0f);
        controller.skinWidth = 0.03f;
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 50f;
        controller.minMoveDistance = 0f;

        L12GrassInteractor interactor = player.GetComponent<L12GrassInteractor>();
        if (interactor == null)
        {
            interactor = Undo.AddComponent<L12GrassInteractor>(player);
        }

        interactor.radius = 3f;
        interactor.strength = 1f;

        Vector3 position = player.transform.position;
        position.y = SampleRollingHeight(position.x, position.z) + 0.25f;
        player.transform.position = position;
    }

    private static Mesh CreateOrUpdateRollingGroundMesh()
    {
        if (!AssetDatabase.IsValidFolder(MeshesFolder))
        {
            AssetDatabase.CreateFolder("Assets/LXII game 整合", "Meshes");
        }

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RollingGroundMeshPath);
        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "LXII_OpenWorldRollingGrassGround"
            };
            AssetDatabase.CreateAsset(mesh, RollingGroundMeshPath);
        }
        else
        {
            mesh.Clear();
        }

        int resolution = Mathf.Max(3, GroundResolution);
        int vertexCount = resolution * resolution;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        float halfSize = GroundSize * 0.5f;
        float step = GroundSize / (resolution - 1);

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int index = z * resolution + x;
                float worldX = x * step - halfSize;
                float worldZ = z * step - halfSize;
                vertices[index] = new Vector3(worldX, SampleRollingHeight(worldX, worldZ), worldZ);
                normals[index] = Vector3.up;
                uvs[index] = new Vector2(x / (float)(resolution - 1), z / (float)(resolution - 1));
            }
        }

        int write = 0;
        for (int z = 0; z < resolution - 1; z++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int i0 = z * resolution + x;
                int i1 = i0 + 1;
                int i2 = i0 + resolution;
                int i3 = i2 + 1;

                triangles[write++] = i0;
                triangles[write++] = i2;
                triangles[write++] = i1;
                triangles[write++] = i1;
                triangles[write++] = i2;
                triangles[write++] = i3;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static float SampleRollingHeight(float worldX, float worldZ)
    {
        float x = worldX + TerrainHeightOffset.x;
        float z = worldZ + TerrainHeightOffset.y;
        float broad = Mathf.Sin(x * TerrainHeightFrequency) * Mathf.Cos(z * TerrainHeightFrequency * 0.82f);
        float longWave = Mathf.Sin((x * 0.62f + z * 0.78f) * TerrainHeightFrequency * 0.62f);
        float detail = Mathf.Sin(x * TerrainDetailFrequency) * Mathf.Sin(z * TerrainDetailFrequency * 0.73f);
        return TerrainHeightBase + TerrainHeightAmplitude * (broad * 0.58f + longWave * 0.30f + detail * 0.12f);
    }

    private static void GetPlayerBounds(GameObject player, out Bounds bounds)
    {
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = new Bounds(player.transform.position + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));
            return;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
    }

    private static void RemoveExistingGrassRoot()
    {
        GameObject existing = GameObject.Find(GrassRootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }
    }
}
