using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class L13VolumeCloudDemoBuilder
{
    private static readonly Vector3 DefaultCloudVolumeSize = new Vector3(240f, 76f, 160f);

    private const string Root = "Assets/L13 VolumeCloud";
    private const string ScenePath = Root + "/L13.unity";
    private const string CloudMaterialPath = Root + "/Materials/L13_RaymarchedCloud.mat";
    private const string GroundMaterialPath = Root + "/Materials/L13_Ground.mat";
    private const string MountainMaterialPath = Root + "/Materials/L13_Mountain.mat";
    private const string SkyboxMaterialPath = Root + "/Materials/L13_ProceduralSky.mat";
    private const string VolumeProfilePath = Root + "/Materials/L13_CloudLookProfile.asset";
    private const string NoiseSettingsPath = Root + "/Settings/L13CloudNoiseSettings.asset";
    private const string ShapeNoisePath = Root + "/Textures/ShapeNoise3D.asset";
    private const string DetailNoisePath = Root + "/Textures/DetailNoise3D.asset";
    private const string WeatherMapPath = Root + "/Textures/WeatherMap.png";

    [MenuItem("Tools/Volume Cloud/Build L13 Raymarched Volume Cloud Demo")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory(Root + "/Settings");

        L13CloudNoiseSettings noiseSettings = LoadOrCreateNoiseSettings();
        Texture3D shapeNoise = LoadOrCreateShapeNoise(noiseSettings);
        Texture3D detailNoise = LoadOrCreateDetailNoise(noiseSettings);
        Texture2D weatherMap = LoadOrCreateWeatherMap(noiseSettings);
        Material cloudMaterial = LoadOrCreateCloudMaterial(shapeNoise, detailNoise, weatherMap);
        Material groundMaterial = LoadOrCreateLitMaterial(GroundMaterialPath, "L13_Ground", new Color(0.18f, 0.22f, 0.24f, 1f), 0.35f);
        Material mountainMaterial = LoadOrCreateLitMaterial(MountainMaterialPath, "L13_Mountain", new Color(0.25f, 0.27f, 0.3f, 1f), 0.2f);
        Material skyboxMaterial = LoadOrCreateSkyboxMaterial();
        VolumeProfile volumeProfile = LoadOrCreateVolumeProfile();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L13 Raymarched Volume Cloud";

        ConfigureRenderSettings(skyboxMaterial);

        GameObject sun = CreateSun();
        Light sunLight = sun.GetComponent<Light>();

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Atmospheric Ground Plane";
        ground.transform.position = new Vector3(0f, -8f, 0f);
        ground.transform.localScale = new Vector3(32f, 1f, 32f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

        GameObject landmarks = new GameObject("Distant Landmarks");
        CreateMountain("Mountain Ridge A", landmarks.transform, new Vector3(-82f, -2f, 78f), new Vector3(34f, 7f, 16f), mountainMaterial);
        CreateMountain("Mountain Ridge B", landmarks.transform, new Vector3(-26f, -1f, 92f), new Vector3(46f, 9f, 20f), mountainMaterial);
        CreateMountain("Mountain Ridge C", landmarks.transform, new Vector3(46f, -2f, 82f), new Vector3(40f, 8f, 18f), mountainMaterial);
        CreateMountain("Mountain Ridge D", landmarks.transform, new Vector3(96f, -3f, 66f), new Vector3(26f, 6f, 14f), mountainMaterial);

        GameObject cloudVolume = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloudVolume.name = "Raymarched Volume Cloud Box";
        cloudVolume.transform.position = new Vector3(0f, 58f, 8f);
        cloudVolume.transform.localScale = DefaultCloudVolumeSize;
        MeshRenderer cloudRenderer = cloudVolume.GetComponent<MeshRenderer>();
        cloudRenderer.sharedMaterial = cloudMaterial;
        cloudRenderer.shadowCastingMode = ShadowCastingMode.Off;
        cloudRenderer.receiveShadows = false;
        Collider cloudCollider = cloudVolume.GetComponent<Collider>();
        if (cloudCollider != null)
        {
            Object.DestroyImmediate(cloudCollider);
        }

        L13VolumeCloudController cloudController = cloudVolume.AddComponent<L13VolumeCloudController>();
        cloudController.cloudMaterial = cloudMaterial;
        cloudController.sunLight = sunLight;
        cloudController.shadowColor = new Color(0.48f, 0.56f, 0.68f, 1f);
        cloudController.ambientColor = new Color(0.46f, 0.55f, 0.72f, 1f);
        cloudController.density = 3.2f;
        cloudController.coverage = 0.6f;
        cloudController.weatherStrength = 0.72f;
        cloudController.shapeScale = 6f;
        cloudController.detailScale = 18f;
        cloudController.detailStrength = 0.42f;
        cloudController.bottomSoftness = 0.18f;
        cloudController.topSoftness = 0.22f;
        cloudController.anvilBias = 0.62f;
        cloudController.absorption = 2.6f;
        cloudController.lightAbsorption = 2.9f;
        cloudController.forwardPhase = 0.58f;
        cloudController.backwardPhase = -0.28f;
        cloudController.silverIntensity = 1.65f;
        cloudController.powderStrength = 1.15f;
        cloudController.windDirection = new Vector4(1f, 0f, 0.25f, 0f);
        cloudController.windSpeed = 7f;
        cloudController.noiseWorldSize = DefaultCloudVolumeSize;
        cloudController.stepCount = 16;
        cloudController.lightStepCount = 0;

        GameObject volume = new GameObject("Global Cloud Look Volume");
        Volume globalVolume = volume.AddComponent<Volume>();
        globalVolume.isGlobal = true;
        globalVolume.priority = 1f;
        globalVolume.sharedProfile = volumeProfile;

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 650f;
        cameraObject.AddComponent<AudioListener>();
        L13VolumeCloudCameraRig cameraRig = cameraObject.AddComponent<L13VolumeCloudCameraRig>();
        cameraRig.focusPoint = new Vector3(0f, 58f, 12f);
        cameraRig.orbitDistance = 132f;
        cameraRig.yaw = -31f;
        cameraRig.pitch = -7f;
        cameraRig.SnapToRig();

        GameObject hud = new GameObject("Demo HUD");
        L13VolumeCloudDemoHud demoHud = hud.AddComponent<L13VolumeCloudDemoHud>();
        demoHud.cloud = cloudController;
        demoHud.sunLight = sunLight;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = cloudVolume;
        Debug.Log($"L13 volume cloud demo rebuilt: {ScenePath}");
    }

    [MenuItem("Tools/Volume Cloud/Capture L13 Preview")]
    public static void CapturePreview()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("L13 preview capture skipped: no MainCamera found.");
            return;
        }

        const int width = 1280;
        const int height = 720;
        Directory.CreateDirectory("Assets/Screenshots");

        RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);

        camera.targetTexture = target;
        RenderTexture.active = target;
        camera.Render();
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        File.WriteAllBytes("Assets/Screenshots/L13_VolumeCloud_menu_preview.png", image.EncodeToPNG());

        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(target);

        AssetDatabase.Refresh();
        Debug.Log("L13 preview captured: Assets/Screenshots/L13_VolumeCloud_menu_preview.png");
    }

    private static void ConfigureRenderSettings(Material skyboxMaterial)
    {
        RenderSettings.skybox = skyboxMaterial;
        RenderSettings.sun = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.38f, 0.50f, 0.68f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.30f, 0.37f, 0.48f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.fogColor = new Color(0.50f, 0.60f, 0.74f, 1f);
    }

    private static GameObject CreateSun()
    {
        GameObject sun = new GameObject("Low Sun Directional Light");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.2f;
        light.color = new Color(1f, 0.78f, 0.52f, 1f);
        light.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(33f, -43f, 0f);
        RenderSettings.sun = light;
        return sun;
    }

    private static void CreateMountain(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mountain.name = name;
        mountain.transform.SetParent(parent);
        mountain.transform.position = position;
        mountain.transform.localScale = scale;
        mountain.GetComponent<MeshRenderer>().sharedMaterial = material;
        Collider collider = mountain.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    [MenuItem("Tools/Volume Cloud/Regenerate L13 Noise Textures")]
    public static void RegenerateNoiseTextures()
    {
        RegenerateNoiseTextures(LoadOrCreateNoiseSettings());
    }

    public static void RegenerateNoiseTextures(L13CloudNoiseSettings settings)
    {
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory(Root + "/Settings");
        settings = settings != null ? settings : LoadOrCreateNoiseSettings();
        Texture3D shapeNoise = LoadOrCreateShapeNoise(settings);
        Texture3D detailNoise = LoadOrCreateDetailNoise(settings);
        Texture2D weatherMap = LoadOrCreateWeatherMap(settings);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);
        if (material != null)
        {
            BindCloudTextures(material, shapeNoise, detailNoise, weatherMap);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("L13 noise textures regenerated and rebound.");
    }

    [MenuItem("Tools/Volume Cloud/Select L13 Noise Settings")]
    public static void SelectNoiseSettings()
    {
        Selection.activeObject = LoadOrCreateNoiseSettings();
    }

    private static Material LoadOrCreateCloudMaterial(Texture3D shapeNoise, Texture3D detailNoise, Texture2D weatherMap)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);
        if (material != null)
        {
            ConfigureCloudMaterial(material, shapeNoise, detailNoise, weatherMap);
            return material;
        }

        Shader shader = Shader.Find("L13 VolumeCloud/Raymarched Volume Cloud");
        material = new Material(shader)
        {
            name = "L13_RaymarchedCloud"
        };
        ConfigureCloudMaterial(material, shapeNoise, detailNoise, weatherMap);
        AssetDatabase.CreateAsset(material, CloudMaterialPath);
        return material;
    }

    private static void ConfigureCloudMaterial(Material material, Texture3D shapeNoise, Texture3D detailNoise, Texture2D weatherMap)
    {
        BindCloudTextures(material, shapeNoise, detailNoise, weatherMap);
        material.SetColor("_CloudColor", new Color(1f, 0.92f, 0.78f, 1f));
        material.SetColor("_ShadowColor", new Color(0.48f, 0.56f, 0.68f, 1f));
        material.SetColor("_AmbientColor", new Color(0.46f, 0.55f, 0.72f, 1f));
        material.SetFloat("_Density", 3.2f);
        material.SetFloat("_Coverage", 0.6f);
        material.SetFloat("_WeatherStrength", 0.72f);
        material.SetFloat("_ShapeScale", 6f);
        material.SetFloat("_DetailScale", 18f);
        material.SetVector("_NoiseWorldSize", DefaultCloudVolumeSize);
        material.SetFloat("_DetailStrength", 0.42f);
        material.SetFloat("_BottomSoftness", 0.18f);
        material.SetFloat("_TopSoftness", 0.22f);
        material.SetFloat("_AnvilBias", 0.62f);
        material.SetFloat("_Absorption", 2.6f);
        material.SetFloat("_LightAbsorption", 2.9f);
        material.SetFloat("_PhaseForward", 0.58f);
        material.SetFloat("_PhaseBackward", -0.28f);
        material.SetFloat("_WindSpeed", 7f);
        material.SetFloat("_SilverIntensity", 1.65f);
        material.SetFloat("_PowderStrength", 1.15f);
        material.SetVector("_WindDirection", new Vector4(1f, 0f, 0.25f, 0f));
        material.SetInt("_StepCount", 16);
        material.SetInt("_LightStepCount", 0);
        material.SetFloat("_Opacity", 0.92f);
        material.renderQueue = 3020;
        EditorUtility.SetDirty(material);
    }

    private static void BindCloudTextures(Material material, Texture3D shapeNoise, Texture3D detailNoise, Texture2D weatherMap)
    {
        material.SetTexture("_ShapeNoise", shapeNoise);
        material.SetTexture("_DetailNoise", detailNoise);
        material.SetTexture("_WeatherMap", weatherMap);
        material.renderQueue = 3020;
        EditorUtility.SetDirty(material);
    }

    private static L13CloudNoiseSettings LoadOrCreateNoiseSettings()
    {
        L13CloudNoiseSettings settings = AssetDatabase.LoadAssetAtPath<L13CloudNoiseSettings>(NoiseSettingsPath);
        if (settings != null)
        {
            return settings;
        }

        Directory.CreateDirectory(Root + "/Settings");
        settings = ScriptableObject.CreateInstance<L13CloudNoiseSettings>();
        AssetDatabase.CreateAsset(settings, NoiseSettingsPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static Texture3D LoadOrCreateShapeNoise(L13CloudNoiseSettings settings)
    {
        int size = Mathf.Clamp(settings.shapeNoiseSize, 16, 128);
        Texture3D texture = LoadOrCreateTexture3D(ShapeNoisePath, "ShapeNoise3D", size);

        Color[] colors = new Color[size * size * size];
        int cursor = 0;
        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 p = new Vector3(x, y, z) / size;
                    float baseNoise = FbmTile(p, settings.shapeBasePeriod, settings.shapeOctaves, settings.shapeSeed);
                    float broadWorley = TileableWorley(p, settings.shapeWorleyPeriod, settings.shapeWorleySeed);
                    float softBillow = 1f - Mathf.Abs(baseNoise * 2f - 1f);
                    float perlinWorley = Mathf.Clamp01(baseNoise * settings.shapeBaseWeight + broadWorley * settings.shapeWorleyWeight + settings.shapeBias);
                    colors[cursor++] = new Color(perlinWorley, broadWorley, Mathf.Clamp01(perlinWorley * settings.shapeBlueWeight + softBillow * settings.shapeBillowWeight), baseNoise);
                }
            }
        }

        texture.SetPixels(colors);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 1;
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Texture3D LoadOrCreateDetailNoise(L13CloudNoiseSettings settings)
    {
        int size = Mathf.Clamp(settings.detailNoiseSize, 16, 96);
        Texture3D texture = LoadOrCreateTexture3D(DetailNoisePath, "DetailNoise3D", size);

        Color[] colors = new Color[size * size * size];
        int cursor = 0;
        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 p = new Vector3(x, y, z) / size;
                    float fineA = TileableWorley(p, settings.detailPeriodA, settings.detailSeedA);
                    float fineB = TileableWorley(p, settings.detailPeriodB, settings.detailSeedB);
                    float fineC = TileableWorley(p, settings.detailPeriodC, settings.detailSeedC);
                    float combined = Mathf.Clamp01(fineA * settings.detailWeightA + fineB * settings.detailWeightB + fineC * settings.detailWeightC);
                    colors[cursor++] = new Color(fineA, fineB, combined, fineC);
                }
            }
        }

        texture.SetPixels(colors);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 1;
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Texture2D LoadOrCreateWeatherMap(L13CloudNoiseSettings settings)
    {
        int size = Mathf.Clamp(settings.weatherMapSize, 64, 512);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
        {
            name = "WeatherMap"
        };

        Color[] colors = new Color[size * size];
        int cursor = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x / (float)size, y / (float)size);
                float system = FbmTile2D(p, settings.weatherSystemPeriod, settings.weatherSystemOctaves, settings.weatherSystemSeed);
                float breakup = FbmTile2D(p, settings.weatherBreakupPeriod, settings.weatherBreakupOctaves, settings.weatherBreakupSeed);
                float coverage = Mathf.SmoothStep(settings.coverageSmoothMin, settings.coverageSmoothMax, system * settings.weatherSystemWeight + breakup * settings.weatherBreakupWeight);
                float cloudType = Mathf.SmoothStep(settings.cloudTypeSmoothMin, settings.cloudTypeSmoothMax, FbmTile2D(p, settings.cloudTypePeriod, settings.cloudTypeOctaves, settings.cloudTypeSeed));
                float density = Mathf.Lerp(settings.densityMin, settings.densityMax, FbmTile2D(p, settings.densityPeriod, settings.densityOctaves, settings.densitySeed));
                float detailAmount = Mathf.Lerp(settings.detailAmountMin, settings.detailAmountMax, breakup);
                colors[cursor++] = new Color(coverage, cloudType, density, detailAmount);
            }
        }

        texture.SetPixels(colors);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.Apply(true, false);
        File.WriteAllBytes(WeatherMapPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(WeatherMapPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(WeatherMapPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = size;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(WeatherMapPath);
    }

    private static Texture3D LoadOrCreateTexture3D(string path, string textureName, int size)
    {
        Texture3D texture = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if (texture == null)
        {
            texture = new Texture3D(size, size, size, TextureFormat.RGBA32, true)
            {
                name = textureName
            };
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        if (texture.width != size || texture.height != size || texture.depth != size)
        {
            Object.DestroyImmediate(texture, true);
            AssetDatabase.SaveAssets();
            texture = new Texture3D(size, size, size, TextureFormat.RGBA32, true)
            {
                name = textureName
            };
            AssetDatabase.CreateAsset(texture, path);
        }

        texture.name = textureName;
        return texture;
    }

    private static float FbmTile(Vector3 uv, int basePeriod, int octaves, int seed)
    {
        float sum = 0f;
        float amplitude = 0.5f;
        float normalization = 0f;
        basePeriod = Mathf.Max(1, basePeriod);
        octaves = Mathf.Max(1, octaves);
        for (int i = 0; i < octaves; i++)
        {
            int period = basePeriod << i;
            sum += TileableValueNoise(uv, period, seed + i * 131) * amplitude;
            normalization += amplitude;
            amplitude *= 0.5f;
        }

        return sum / Mathf.Max(normalization, 0.0001f);
    }

    private static float FbmTile2D(Vector2 uv, int basePeriod, int octaves, int seed)
    {
        float sum = 0f;
        float amplitude = 0.5f;
        float normalization = 0f;
        basePeriod = Mathf.Max(1, basePeriod);
        octaves = Mathf.Max(1, octaves);
        for (int i = 0; i < octaves; i++)
        {
            int period = basePeriod << i;
            sum += TileableValueNoise2D(uv, period, seed + i * 131) * amplitude;
            normalization += amplitude;
            amplitude *= 0.5f;
        }

        return sum / Mathf.Max(normalization, 0.0001f);
    }

    private static float TileableValueNoise(Vector3 uv, int period, int seed)
    {
        Vector3 p = uv * period;
        int ix = Mathf.FloorToInt(p.x);
        int iy = Mathf.FloorToInt(p.y);
        int iz = Mathf.FloorToInt(p.z);
        float fx = Smooth01(p.x - ix);
        float fy = Smooth01(p.y - iy);
        float fz = Smooth01(p.z - iz);

        int x0 = Mod(ix, period);
        int y0 = Mod(iy, period);
        int z0 = Mod(iz, period);
        int x1 = Mod(ix + 1, period);
        int y1 = Mod(iy + 1, period);
        int z1 = Mod(iz + 1, period);

        float n000 = Hash01(x0, y0, z0, seed);
        float n100 = Hash01(x1, y0, z0, seed);
        float n010 = Hash01(x0, y1, z0, seed);
        float n110 = Hash01(x1, y1, z0, seed);
        float n001 = Hash01(x0, y0, z1, seed);
        float n101 = Hash01(x1, y0, z1, seed);
        float n011 = Hash01(x0, y1, z1, seed);
        float n111 = Hash01(x1, y1, z1, seed);

        float nx00 = Mathf.Lerp(n000, n100, fx);
        float nx10 = Mathf.Lerp(n010, n110, fx);
        float nx01 = Mathf.Lerp(n001, n101, fx);
        float nx11 = Mathf.Lerp(n011, n111, fx);
        return Mathf.Lerp(Mathf.Lerp(nx00, nx10, fy), Mathf.Lerp(nx01, nx11, fy), fz);
    }

    private static float TileableValueNoise2D(Vector2 uv, int period, int seed)
    {
        Vector2 p = uv * period;
        int ix = Mathf.FloorToInt(p.x);
        int iy = Mathf.FloorToInt(p.y);
        float fx = Smooth01(p.x - ix);
        float fy = Smooth01(p.y - iy);

        int x0 = Mod(ix, period);
        int y0 = Mod(iy, period);
        int x1 = Mod(ix + 1, period);
        int y1 = Mod(iy + 1, period);

        float n00 = Hash01(x0, y0, 0, seed);
        float n10 = Hash01(x1, y0, 0, seed);
        float n01 = Hash01(x0, y1, 0, seed);
        float n11 = Hash01(x1, y1, 0, seed);

        float nx0 = Mathf.Lerp(n00, n10, fx);
        float nx1 = Mathf.Lerp(n01, n11, fx);
        return Mathf.Lerp(nx0, nx1, fy);
    }

    private static float TileableWorley(Vector3 uv, int period, int seed)
    {
        period = Mathf.Max(1, period);
        Vector3 p = uv * period;
        int cx = Mathf.FloorToInt(p.x);
        int cy = Mathf.FloorToInt(p.y);
        int cz = Mathf.FloorToInt(p.z);
        float minDistance = 10f;

        for (int z = -1; z <= 1; z++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    int px = cx + x;
                    int py = cy + y;
                    int pz = cz + z;
                    int wx = Mod(px, period);
                    int wy = Mod(py, period);
                    int wz = Mod(pz, period);
                    Vector3 feature = new Vector3(
                        px + Hash01(wx, wy, wz, seed),
                        py + Hash01(wx, wy, wz, seed + 17),
                        pz + Hash01(wx, wy, wz, seed + 31));
                    minDistance = Mathf.Min(minDistance, Vector3.Distance(p, feature));
                }
            }
        }

        return Mathf.Clamp01(1f - minDistance / 1.15f);
    }

    private static int Mod(int value, int period)
    {
        int result = value % period;
        return result < 0 ? result + period : result;
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static float Hash01(int x, int y, int z, int seed)
    {
        unchecked
        {
            uint h = (uint)seed;
            h ^= (uint)x * 374761393u;
            h = (h << 13) | (h >> 19);
            h ^= (uint)y * 668265263u;
            h = (h << 11) | (h >> 21);
            h ^= (uint)z * 2246822519u;
            h *= 3266489917u;
            return (h & 0x00FFFFFFu) / 16777215f;
        }
    }

    private static Material LoadOrCreateLitMaterial(string path, string name, Color color, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader)
        {
            name = name
        };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material LoadOrCreateSkyboxMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        if (material != null)
        {
            ConfigureSkyboxMaterial(material);
            return material;
        }

        Shader shader = Shader.Find("Skybox/Procedural");
        material = new Material(shader)
        {
            name = "L13_ProceduralSky"
        };
        ConfigureSkyboxMaterial(material);
        AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
        return material;
    }

    private static void ConfigureSkyboxMaterial(Material material)
    {
        if (material.HasProperty("_SkyTint")) material.SetColor("_SkyTint", new Color(0.42f, 0.58f, 0.86f, 1f));
        if (material.HasProperty("_GroundColor")) material.SetColor("_GroundColor", new Color(0.18f, 0.20f, 0.24f, 1f));
        if (material.HasProperty("_AtmosphereThickness")) material.SetFloat("_AtmosphereThickness", 0.75f);
        if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 1.0f);
        EditorUtility.SetDirty(material);
    }

    private static VolumeProfile LoadOrCreateVolumeProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile != null)
        {
            return profile;
        }

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "L13_CloudLookProfile";

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.22f);
        bloom.threshold.Override(1.15f);
        bloom.scatter.Override(0.58f);

        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(0.15f);
        colorAdjustments.contrast.Override(14f);
        colorAdjustments.saturation.Override(6f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.42f);

        AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        return profile;
    }
}
