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

        const float fieldSize = 72f;
        CreateGround(grassRoot.transform, groundMaterial, fieldSize);
        CreateGrassField(grassRoot.transform, grassMaterial, densityMap, cullingCompute, fieldSize);
        ConfigurePlayerForGrass(player);

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(grassRoot);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = grassRoot;
        Debug.Log("[LXII] 已在 game.unity 中接入 L12 草地：72x72 草地区域 + 妮露压草交互 + CharacterController 落地碰撞。");
    }

    private static void CreateGround(Transform parent, Material groundMaterial, float fieldSize)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = GroundName;
        ground.transform.SetParent(parent, false);
        float planeScale = fieldSize / 10f;
        ground.transform.localScale = new Vector3(planeScale, 1f, planeScale);

        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = groundMaterial;
        }
    }

    private static void CreateGrassField(Transform parent, Material grassMaterial, Texture2D densityMap, ComputeShader cullingCompute, float fieldSize)
    {
        GameObject grass = new GameObject(GrassFieldName);
        grass.transform.SetParent(parent, false);

        L12GrassRenderer renderer = grass.AddComponent<L12GrassRenderer>();
        renderer.grassMaterial = grassMaterial;
        renderer.cullingCompute = cullingCompute;
        renderer.densityMap = densityMap;
        renderer.bladesPerSide = 520;
        renderer.fieldSize = fieldSize;
        renderer.preserveDensityWhenResized = true;
        renderer.targetBladeSpacing = fieldSize / 520f;
        renderer.maxBladesPerAxis = 3072;
        renderer.chunksPerSide = 12;
        renderer.bladeHeight = 1.25f;
        renderer.bladeWidth = 0.085f;
        renderer.bladeRootWidthScale = 1f;
        renderer.maxDrawDistance = 96f;
        renderer.lod0Distance = 24f;
        renderer.lod1Distance = 54f;
        renderer.densityThreshold = 0f;
        renderer.densityInfluence = 1.85f;
        renderer.interactionTextureSize = 256;
        renderer.interactionStrength = 3.6f;
        renderer.interactionFlattenStrength = 0.85f;
        renderer.interactionFadeSpeed = 2.6f;
        renderer.windStrength = 0.32f;
        renderer.windScale = 0.18f;
        renderer.windSpeed = 1.8f;
        renderer.windDirection = new Vector2(0.86f, 0.42f).normalized;
        renderer.gustStrength = 0.85f;
        renderer.gustFrequency = 0.065f;
        renderer.gustSpeed = 5.8f;
        renderer.gustWidth = 0.34f;
        renderer.gustNoiseScale = 0.055f;
        renderer.baseColor = new Color(0.11f, 0.34f, 0.12f, 1f);
        renderer.tipColor = new Color(0.46f, 0.68f, 0.22f, 1f);
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
        position.y = 0f;
        player.transform.position = position;
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
