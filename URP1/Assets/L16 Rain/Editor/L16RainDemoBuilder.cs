using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class L16RainDemoBuilder
{
    private const string Root = "Assets/L16 Rain";
    private const string ScenePath = Root + "/L16.unity";
    private const string RainMaterialPath = Root + "/Materials/L16_GPU_Rain_Streak.mat";
    private const string GroundMaterialPath = Root + "/Materials/L16_Plain_Rain_Ground.mat";
    private const string BackdropMaterialPath = Root + "/Materials/L16_Plain_Rain_Backdrop.mat";
    private const string ScreenMaterialPath = Root + "/Materials/L16_Screen_Rain.mat";
    private const string SkyboxMaterialPath = Root + "/Materials/L16_Rain_Skybox.mat";

    [MenuItem("Tools/Rain/Build L16 Advanced Rain Demo")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("L16 rain demo builder cannot rebuild scenes during Play Mode. Stop Play Mode and run the menu again.");
            return;
        }

        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory(Root + "/Editor");
        Directory.CreateDirectory(Root + "/Docs");
        AssetDatabase.Refresh();

        EnableUrpDepthOpaqueAndNormals();

        Material rainMaterial = LoadOrCreateRainMaterial();
        Material groundMaterial = LoadOrCreatePlainLitMaterial(GroundMaterialPath, "L16_Plain_Rain_Ground", new Color(0.52f, 0.55f, 0.58f, 1f), 0f, 0.38f);
        Material backdropMaterial = LoadOrCreatePlainLitMaterial(BackdropMaterialPath, "L16_Plain_Rain_Backdrop", new Color(0.66f, 0.69f, 0.72f, 1f), 0f, 0.32f);
        Material screenMaterial = LoadOrCreateScreenRainMaterial();

        ConfigureRendererFeature(screenMaterial);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L16 Advanced Rain";

        ConfigureSimpleLighting();
        RenderSettings.skybox = LoadOrCreateSkyboxMaterial();
        CreateRainOnlyStage(groundMaterial, backdropMaterial);

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 180f;
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;

        L16RainCameraRig cameraRig = cameraObject.AddComponent<L16RainCameraRig>();
        cameraRig.focusPoint = new Vector3(0f, 1.5f, 3.5f);
        cameraRig.distance = 17f;
        cameraRig.yaw = -24f;
        cameraRig.pitch = 16f;
        Vector3 focus = cameraRig.focusPoint;
        Quaternion rotation = Quaternion.Euler(cameraRig.pitch, cameraRig.yaw, 0f);
        cameraObject.transform.position = focus + rotation * new Vector3(0f, 0f, -cameraRig.distance);
        cameraObject.transform.rotation = Quaternion.LookRotation(focus - cameraObject.transform.position, Vector3.up);

        GameObject rainObject = new GameObject("L16 GPU Rain Volume - Compute Indirect");
        L16RainManager rainManager = rainObject.AddComponent<L16RainManager>();
        rainManager.rainMaterial = rainMaterial;
        rainManager.rainCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(Root + "/Shaders/L16RainPopulate.compute");
        rainManager.targetCamera = camera;
        rainManager.qualityPreset = 1;
        rainManager.rainIntensity = 0.78f;
        rainManager.wind = new Vector2(-0.75f, 0.28f);

        GameObject hud = new GameObject("Demo HUD");
        L16RainDemoHud demoHud = hud.AddComponent<L16RainDemoHud>();
        demoHud.rainManager = rainManager;
        demoHud.screenRainMaterial = screenMaterial;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = rainObject;
        Debug.Log($"L16 rain-only demo rebuilt: {ScenePath}");
    }

    private static void EnableUrpDepthOpaqueAndNormals()
    {
        string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null || !path.StartsWith("Assets/Settings/", System.StringComparison.Ordinal))
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(asset);
            bool changed = SetBoolIfPresent(serialized, "m_RequireDepthTexture", true);
            changed |= SetBoolIfPresent(serialized, "m_RequireOpaqueTexture", true);
            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }
    }

    private static bool SetBoolIfPresent(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.Boolean || property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }

    private static void ConfigureRendererFeature(Material screenMaterial)
    {
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData == null || !path.StartsWith("Assets/Settings/", System.StringComparison.Ordinal))
            {
                continue;
            }

            bool exists = false;
            foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
            {
                if (feature is L16RainScreenFeature)
                {
                    exists = true;
                    SerializedObject featureSerialized = new SerializedObject(feature);
                    SerializedProperty materialProperty = featureSerialized.FindProperty("settings").FindPropertyRelative("material");
                    materialProperty.objectReferenceValue = screenMaterial;
                    featureSerialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(feature);
                }
            }

            if (exists)
            {
                continue;
            }

            L16RainScreenFeature rainFeature = ScriptableObject.CreateInstance<L16RainScreenFeature>();
            rainFeature.name = "L16RainScreenFeature";
            rainFeature.settings.material = screenMaterial;
            rainFeature.settings.passEvent = RenderPassEvent.AfterRenderingTransparents;
            AssetDatabase.AddObjectToAsset(rainFeature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rainFeature, out string _, out long localId);

            SerializedObject serializedRenderer = new SerializedObject(rendererData);
            SerializedProperty features = serializedRenderer.FindProperty("m_RendererFeatures");
            SerializedProperty featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = rainFeature;
            featureMap.arraySize++;
            featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
            Debug.Log($"L16 rain screen RendererFeature installed on {path}");
        }
    }

    private static void ConfigureSimpleLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.48f, 0.55f, 0.64f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.33f, 0.37f, 0.42f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.20f, 0.22f, 0.25f, 1f);
        RenderSettings.fog = false;

        GameObject sun = new GameObject("Directional Light - Simple Rain Key");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.95f;
        light.color = new Color(0.86f, 0.92f, 1f, 1f);
        sun.transform.rotation = Quaternion.Euler(48f, -36f, 0f);
        RenderSettings.sun = light;
    }

    private static Material LoadOrCreateSkyboxMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        Shader shader = Shader.Find("Skybox/Procedural");
        if (material == null)
        {
            material = new Material(shader) { name = "L16_Rain_Skybox" };
            AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_SkyTint", new Color(0.55f, 0.64f, 0.76f, 1f));
        material.SetColor("_GroundColor", new Color(0.32f, 0.35f, 0.38f, 1f));
        material.SetFloat("_AtmosphereThickness", 0.82f);
        material.SetFloat("_Exposure", 0.78f);
        return material;
    }

    private static void CreateRainOnlyStage(Material groundMaterial, Material backdropMaterial)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Plain Rain Ground";
        ground.transform.position = new Vector3(0f, -0.055f, 0f);
        ground.transform.localScale = new Vector3(44f, 0.1f, 44f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backdrop.name = "Plain Rain Backdrop";
        backdrop.transform.position = new Vector3(0f, 3.0f, 17.5f);
        backdrop.transform.localScale = new Vector3(44f, 6f, 0.35f);
        backdrop.GetComponent<MeshRenderer>().sharedMaterial = backdropMaterial;
    }

    private static Material LoadOrCreateRainMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(RainMaterialPath);
        Shader shader = Shader.Find("L16 Rain/GPU Rain Streak");
        if (material == null)
        {
            material = new Material(shader) { name = "L16_GPU_Rain_Streak" };
            AssetDatabase.CreateAsset(material, RainMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_DropTint", new Color(0.66f, 0.82f, 1f, 0.58f));
        material.SetFloat("_DropLength", 1.85f);
        material.SetFloat("_DropWidth", 0.018f);
        material.SetFloat("_MaxDrawDistance", 58f);
        material.SetFloat("_SoftDepthDistance", 0.85f);
        return material;
    }

    private static Material LoadOrCreatePlainLitMaterial(string path, string name, Color baseColor, float metallic, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_BaseColor", baseColor);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static Material LoadOrCreateScreenRainMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ScreenMaterialPath);
        Shader shader = Shader.Find("Hidden/L16/Rain Screen Pass");
        if (material == null)
        {
            material = new Material(shader) { name = "L16_Screen_Rain" };
            AssetDatabase.CreateAsset(material, ScreenMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetFloat("_ScreenRainStrength", 0.72f);
        material.SetFloat("_LensDropletStrength", 0.12f);
        material.SetFloat("_RefractionStrength", 0.010f);
        material.SetFloat("_StreakScale", 38f);
        return material;
    }
}
