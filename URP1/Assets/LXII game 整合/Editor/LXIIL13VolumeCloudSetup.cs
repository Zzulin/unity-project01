using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class LXIIL13VolumeCloudSetup
{
    private const string ScenePath = "Assets/LXII game 整合/game.unity";
    private const string RootName = "LXII L13 VolumeCloud Root";
    private const string CloudName = "LXII Sky Volume Cloud";
    private const string DistantCloudLayerName = "LXII Distant Cloud Layer";
    private const string DistantCloudPuffRootName = "LXII Distant Cloud Puffs";

    private const string MaterialsFolder = "Assets/LXII game 整合/Materials";
    private const string CloudMaterialPath = MaterialsFolder + "/LXII_L13_RaymarchedCloud_Performance.mat";

    private const string SourceCloudMaterialPath = "Assets/L13 VolumeCloud/Materials/L13_RaymarchedCloud.mat";
    private const string SkyboxMaterialPath = "Assets/L13 VolumeCloud/Materials/L13_ProceduralSky.mat";
    private const string ShapeNoisePath = "Assets/L13 VolumeCloud/Textures/ShapeNoise3D.asset";
    private const string DetailNoisePath = "Assets/L13 VolumeCloud/Textures/DetailNoise3D.asset";
    private const string WeatherMapPath = "Assets/L13 VolumeCloud/Textures/WeatherMap.png";

    private static readonly Vector3 CloudPosition = new Vector3(0f, 90f, 120f);
    private static readonly Vector3 CloudScale = new Vector3(1600f, 240f, 1600f);
    private static readonly Vector3 NoiseWorldSize = new Vector3(920f, 260f, 920f);

    [MenuItem("Tools/LXII/Setup L13 VolumeCloud In Game Scene")]
    public static void SetupL13VolumeCloudInGameScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Material cloudMaterial = LoadOrCreateCloudMaterial();
        if (cloudMaterial == null)
        {
            Debug.LogError("[LXII] L13 体积云材质创建失败，已停止。");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CleanupLegacyDistantCloudObjects();
        GameObject root = FindOrCreateRoot();
        CleanupDuplicateCloudObjects(root.transform);
        GameObject cloud = FindOrCreateCloud(root.transform, cloudMaterial);
        ConfigureCloudObject(cloud, cloudMaterial);
        ConfigureSceneSky();
        ConfigureMainCamera();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(cloud);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = cloud;
        Debug.Log("[LXII] L13 VolumeCloud 已接入 game.unity：大范围天空云盒 + 10 view steps / 0 light steps 性能档。");
    }

    private static GameObject FindOrCreateRoot()
    {
        GameObject keptRoot = null;
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        foreach (Transform transform in transforms)
        {
            if (transform == null || transform.name != RootName)
            {
                continue;
            }

            if (keptRoot == null)
            {
                keptRoot = transform.gameObject;
                continue;
            }

            Object.DestroyImmediate(transform.gameObject);
        }

        if (keptRoot != null)
        {
            return keptRoot;
        }

        GameObject root = new GameObject(RootName);
        root.transform.position = Vector3.zero;
        return root;
    }

    private static GameObject FindOrCreateCloud(Transform parent, Material cloudMaterial)
    {
        Transform existing = parent.Find(CloudName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloud.name = CloudName;
        cloud.transform.SetParent(parent, false);

        Collider collider = cloud.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        MeshRenderer renderer = cloud.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = cloudMaterial;
        }

        return cloud;
    }

    private static void ConfigureCloudObject(GameObject cloud, Material cloudMaterial)
    {
        cloud.transform.position = CloudPosition;
        cloud.transform.localRotation = Quaternion.identity;
        cloud.transform.localScale = CloudScale;

        MeshRenderer renderer = cloud.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = cloud.AddComponent<MeshRenderer>();
        }

        renderer.sharedMaterial = cloudMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        L13VolumeCloudController controller = cloud.GetComponent<L13VolumeCloudController>();
        if (controller == null)
        {
            controller = cloud.AddComponent<L13VolumeCloudController>();
        }

        controller.cloudMaterial = cloudMaterial;
        controller.sunLight = FindSunLight();
        controller.cloudColor = new Color(0.92f, 0.90f, 0.84f, 1f);
        controller.shadowColor = new Color(0.34f, 0.42f, 0.52f, 1f);
        controller.ambientColor = new Color(0.30f, 0.42f, 0.58f, 1f);
        controller.density = 3.55f;
        controller.coverage = 0.50f;
        controller.weatherStrength = 0.82f;
        controller.shapeScale = 5.4f;
        controller.detailScale = 14f;
        controller.detailStrength = 0.52f;
        controller.bottomSoftness = 0.16f;
        controller.topSoftness = 0.30f;
        controller.anvilBias = 0.56f;
        controller.absorption = 2.45f;
        controller.lightAbsorption = 2.7f;
        controller.forwardPhase = 0.52f;
        controller.backwardPhase = -0.22f;
        controller.silverIntensity = 1.25f;
        controller.powderStrength = 0.86f;
        controller.windDirection = new Vector4(1f, 0f, 0.18f, 0f);
        controller.windSpeed = 3.2f;
        controller.noiseWorldSize = NoiseWorldSize;
        controller.stepCount = 10;
        controller.lightStepCount = 0;
        controller.opacity = 0.84f;

        ConfigureCloudMaterial(cloudMaterial);
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureSceneSky()
    {
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.38f, 0.50f, 0.68f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.30f, 0.37f, 0.48f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.14f, 0.15f, 0.18f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.60f, 0.72f, 0.78f, 1f);
        RenderSettings.fogStartDistance = 150f;
        RenderSettings.fogEndDistance = 900f;
    }

    private static void ConfigureMainCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 1200f);
        EditorUtility.SetDirty(camera);
    }

    private static void CleanupLegacyDistantCloudObjects()
    {
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        foreach (Transform transform in transforms)
        {
            if (transform == null)
            {
                continue;
            }

            bool isLegacyLayer = transform.name == DistantCloudLayerName
                || transform.name.StartsWith(DistantCloudLayerName + " ");
            bool isLegacyPuff = transform.name == DistantCloudPuffRootName
                || transform.name.StartsWith("Cloud Puff ");
            if (isLegacyLayer || isLegacyPuff)
            {
                Object.DestroyImmediate(transform.gameObject);
            }
        }
    }

    private static void CleanupDuplicateCloudObjects(Transform root)
    {
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        foreach (Transform transform in transforms)
        {
            if (transform == null || transform.name != CloudName || transform.parent == root)
            {
                continue;
            }

            Object.DestroyImmediate(transform.gameObject);
        }
    }

    private static Light FindSunLight()
    {
        Light renderSun = RenderSettings.sun;
        if (renderSun != null)
        {
            return renderSun;
        }

        Light[] lights = Object.FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light != null && light.type == LightType.Directional)
            {
                RenderSettings.sun = light;
                return light;
            }
        }

        return null;
    }

    private static Material LoadOrCreateCloudMaterial()
    {
        Directory.CreateDirectory(MaterialsFolder);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);
        if (material == null)
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(SourceCloudMaterialPath);
            Shader shader = source != null ? source.shader : Shader.Find("L13 VolumeCloud/Raymarched Volume Cloud");
            if (shader == null)
            {
                return null;
            }

            material = source != null ? new Material(source) : new Material(shader);
            material.name = "LXII_L13_RaymarchedCloud_Performance";
            AssetDatabase.CreateAsset(material, CloudMaterialPath);
        }

        ConfigureCloudMaterial(material);
        return material;
    }

    private static void ConfigureCloudMaterial(Material material)
    {
        material.SetTexture("_ShapeNoise", AssetDatabase.LoadAssetAtPath<Texture3D>(ShapeNoisePath));
        material.SetTexture("_DetailNoise", AssetDatabase.LoadAssetAtPath<Texture3D>(DetailNoisePath));
        material.SetTexture("_WeatherMap", AssetDatabase.LoadAssetAtPath<Texture2D>(WeatherMapPath));

        material.SetColor("_CloudColor", new Color(0.92f, 0.90f, 0.84f, 1f));
        material.SetColor("_ShadowColor", new Color(0.34f, 0.42f, 0.52f, 1f));
        material.SetColor("_AmbientColor", new Color(0.30f, 0.42f, 0.58f, 1f));
        material.SetFloat("_Density", 3.55f);
        material.SetFloat("_Coverage", 0.50f);
        material.SetFloat("_WeatherStrength", 0.82f);
        material.SetFloat("_ShapeScale", 5.4f);
        material.SetFloat("_DetailScale", 14f);
        material.SetVector("_NoiseWorldSize", new Vector4(NoiseWorldSize.x, NoiseWorldSize.y, NoiseWorldSize.z, 0f));
        material.SetFloat("_DetailStrength", 0.52f);
        material.SetFloat("_BottomSoftness", 0.16f);
        material.SetFloat("_TopSoftness", 0.30f);
        material.SetFloat("_AnvilBias", 0.56f);
        material.SetFloat("_Absorption", 2.45f);
        material.SetFloat("_LightAbsorption", 2.7f);
        material.SetFloat("_PhaseForward", 0.52f);
        material.SetFloat("_PhaseBackward", -0.22f);
        material.SetFloat("_SilverIntensity", 1.25f);
        material.SetFloat("_PowderStrength", 0.86f);
        material.SetVector("_WindDirection", new Vector4(1f, 0f, 0.18f, 0f));
        material.SetFloat("_WindSpeed", 3.2f);
        material.SetInt("_StepCount", 10);
        material.SetInt("_LightStepCount", 0);
        material.SetFloat("_Opacity", 0.84f);
        material.renderQueue = 3020;

        EditorUtility.SetDirty(material);
    }
}
