using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class L12GrassExampleBuilder
{
    private const string Root = "Assets/L12 grass";
    private const string ScenePath = Root + "/L12.unity";
    private const string GrassMaterialPath = Root + "/Materials/L12_InteractiveGrass.mat";
    private const string GroundMaterialPath = Root + "/Materials/L12_Ground.mat";
    private const string InteractorMaterialPath = Root + "/Materials/L12_Interactor.mat";
    private const string DensityMapPath = Root + "/Textures/L12_GrassDensity.asset";
    private const string CullingComputePath = Root + "/Shaders/L12GrassCull.compute";

    [MenuItem("Tools/Grass/Build L12 Interactive Grass Demo")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Textures");

        Material grassMaterial = LoadOrCreateGrassMaterial();
        Material groundMaterial = LoadOrCreateGroundMaterial();
        Material interactorMaterial = LoadOrCreateInteractorMaterial();
        Texture2D densityMap = LoadOrCreateDensityMap();
        ComputeShader cullingCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(CullingComputePath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L12 Interactive Grass";

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.46f, 0.55f, 0.64f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.23f, 0.29f, 0.24f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.11f, 0.07f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.58f, 0.68f, 0.76f, 1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 120f;

        GameObject sun = new GameObject("Directional Light");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.94f, 0.78f, 1f);
        sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        RenderSettings.sun = light;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground Plane";
        ground.transform.localScale = new Vector3(9.5f, 1f, 9.5f);
        MeshRenderer groundRenderer = ground.GetComponent<MeshRenderer>();
        groundRenderer.sharedMaterial = groundMaterial;

        GameObject grass = new GameObject("GPU Grass Field - Indirect Chunked LOD");
        L12GrassRenderer grassRenderer = grass.AddComponent<L12GrassRenderer>();
        grassRenderer.grassMaterial = grassMaterial;
        grassRenderer.cullingCompute = cullingCompute;
        grassRenderer.densityMap = densityMap;
        grassRenderer.bladesPerSide = 300;
        grassRenderer.fieldSize = 90f;
        grassRenderer.chunksPerSide = 12;
        grassRenderer.bladeHeight = 1.25f;
        grassRenderer.bladeWidth = 0.085f;
        grassRenderer.maxDrawDistance = 115f;
        grassRenderer.lod0Distance = 26f;
        grassRenderer.lod1Distance = 62f;
        grassRenderer.densityThreshold = 0.08f;
        grassRenderer.densityInfluence = 1f;
        grassRenderer.interactionTextureResolution = 256;
        grassRenderer.interactionStrength = 3.6f;
        grassRenderer.interactionRecovery = 0.88f;
        grassRenderer.windStrength = 0.32f;
        grassRenderer.windScale = 0.18f;
        grassRenderer.windSpeed = 1.8f;
        grassRenderer.baseColor = new Color(0.11f, 0.34f, 0.12f, 1f);
        grassRenderer.tipColor = new Color(0.46f, 0.68f, 0.22f, 1f);

        GameObject walker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        walker.name = "Player Grass Interactor";
        walker.transform.position = new Vector3(0f, 0.92f, 0f);
        walker.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        MeshRenderer walkerRenderer = walker.GetComponent<MeshRenderer>();
        walkerRenderer.sharedMaterial = interactorMaterial;
        CharacterController controller = walker.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.45f;
        controller.center = Vector3.zero;
        L12GrassInteractor walkerInteractor = walker.AddComponent<L12GrassInteractor>();
        walkerInteractor.radius = 3.2f;
        walkerInteractor.strength = 0.95f;
        L12GrassWalker walkerController = walker.AddComponent<L12GrassWalker>();
        walkerController.fieldLimit = 42f;

        CreateAutoInteractor("Auto Interactor A", new Vector3(0f, 0.4f, 0f), 17f, 0.32f, 0.3f, interactorMaterial);
        CreateAutoInteractor("Auto Interactor B", new Vector3(8f, 0.4f, -7f), 22f, -0.24f, 2.4f, interactorMaterial);

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 52f;
        camera.farClipPlane = 180f;
        cameraObject.AddComponent<AudioListener>();
        L12GrassCameraRig cameraRig = cameraObject.AddComponent<L12GrassCameraRig>();
        cameraRig.target = walker.transform;
        cameraRig.distance = 30f;
        cameraRig.minDistance = 8f;
        cameraRig.maxDistance = 70f;
        cameraRig.yaw = 0f;
        cameraRig.pitch = 36f;
        cameraRig.rotateSensitivity = 4.5f;
        cameraRig.zoomSensitivity = 8f;
        cameraRig.panSensitivity = 0.035f;
        Vector3 initialFocus = walker.transform.position + Vector3.up * cameraRig.lookHeight;
        Quaternion initialRotation = Quaternion.Euler(cameraRig.pitch, cameraRig.yaw, 0f);
        cameraObject.transform.position = initialFocus + initialRotation * new Vector3(0f, 0f, -cameraRig.distance);
        cameraObject.transform.rotation = Quaternion.LookRotation(initialFocus - cameraObject.transform.position, Vector3.up);

        GameObject hud = new GameObject("Demo HUD");
        L12GrassDemoHud demoHud = hud.AddComponent<L12GrassDemoHud>();
        demoHud.grassRenderer = grassRenderer;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = grass;
        Debug.Log($"L12 interactive grass demo rebuilt: {ScenePath}");
    }

    private static GameObject CreateAutoInteractor(string name, Vector3 center, float pathRadius, float speed, float phase, Material material)
    {
        GameObject interactor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        interactor.name = name;
        interactor.transform.position = center + new Vector3(pathRadius, 0f, 0f);
        interactor.transform.localScale = Vector3.one * 0.75f;
        interactor.GetComponent<MeshRenderer>().sharedMaterial = material;
        SphereCollider collider = interactor.GetComponent<SphereCollider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        L12GrassInteractor grassInteractor = interactor.AddComponent<L12GrassInteractor>();
        grassInteractor.radius = 4.2f;
        grassInteractor.strength = 0.7f;

        L12GrassAutoInteractor autoInteractor = interactor.AddComponent<L12GrassAutoInteractor>();
        autoInteractor.center = center;
        autoInteractor.pathRadius = pathRadius;
        autoInteractor.angularSpeed = speed;
        autoInteractor.phase = phase;
        return interactor;
    }

    private static Material LoadOrCreateGrassMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
        if (material != null)
        {
            material.SetColor("_BaseColor", new Color(0.11f, 0.34f, 0.12f, 1f));
            material.SetColor("_TipColor", new Color(0.46f, 0.68f, 0.22f, 1f));
            material.SetTexture("_DensityTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(DensityMapPath));
            return material;
        }

        Shader shader = Shader.Find("L12 Grass/Interactive GPU Grass");
        material = new Material(shader)
        {
            name = "L12_InteractiveGrass"
        };
        material.SetColor("_BaseColor", new Color(0.11f, 0.34f, 0.12f, 1f));
        material.SetColor("_TipColor", new Color(0.46f, 0.68f, 0.22f, 1f));
        material.SetTexture("_DensityTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(DensityMapPath));
        AssetDatabase.CreateAsset(material, GrassMaterialPath);
        return material;
    }

    private static Material LoadOrCreateGroundMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader)
        {
            name = "L12_Ground"
        };
        material.SetColor("_BaseColor", new Color(0.2f, 0.29f, 0.16f, 1f));
        material.SetFloat("_Smoothness", 0.18f);
        AssetDatabase.CreateAsset(material, GroundMaterialPath);
        return material;
    }

    private static Material LoadOrCreateInteractorMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(InteractorMaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader)
        {
            name = "L12_Interactor"
        };
        material.SetColor("_BaseColor", new Color(0.16f, 0.55f, 0.9f, 1f));
        material.SetFloat("_Smoothness", 0.42f);
        AssetDatabase.CreateAsset(material, InteractorMaterialPath);
        return material;
    }

    private static Texture2D LoadOrCreateDensityMap()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DensityMapPath);
        if (texture != null)
        {
            return texture;
        }

        const int size = 256;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "L12_GrassDensity",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (size - 1f);
                float v = y / (size - 1f);
                float centerFalloff = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 1.12f);
                float pathA = Mathf.SmoothStep(0.08f, 0f, Mathf.Abs(v - 0.5f - Mathf.Sin(u * 9.5f) * 0.045f));
                float pathB = Mathf.SmoothStep(0.06f, 0f, Mathf.Abs(u - 0.66f - Mathf.Sin(v * 11.0f) * 0.035f));
                float noise = Mathf.PerlinNoise(u * 9.2f + 4.1f, v * 9.2f + 8.7f) * 0.25f;
                float density = Mathf.Clamp01(centerFalloff * 0.74f + 0.28f + noise - Mathf.Max(pathA, pathB) * 0.75f);
                pixels[y * size + x] = new Color(density, density, density, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        AssetDatabase.CreateAsset(texture, DensityMapPath);
        return texture;
    }
}
