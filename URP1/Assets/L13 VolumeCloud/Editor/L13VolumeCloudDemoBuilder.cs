using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class L13VolumeCloudDemoBuilder
{
    private const string Root = "Assets/L13 VolumeCloud";
    private const string ScenePath = Root + "/L13.unity";
    private const string CloudMaterialPath = Root + "/Materials/L13_RaymarchedCloud.mat";
    private const string GroundMaterialPath = Root + "/Materials/L13_Ground.mat";
    private const string MountainMaterialPath = Root + "/Materials/L13_Mountain.mat";
    private const string SkyboxMaterialPath = Root + "/Materials/L13_ProceduralSky.mat";
    private const string VolumeProfilePath = Root + "/Materials/L13_CloudLookProfile.asset";
    private const string ShapeNoisePath = Root + "/Textures/ShapeNoise3D.asset";
    private const string DetailNoisePath = Root + "/Textures/DetailNoise3D.asset";
    private const string WeatherMapPath = Root + "/Textures/WeatherMap.png";
    private const int ShapeNoiseSize = 64;
    private const int DetailNoiseSize = 32;
    private const int WeatherMapSize = 256;

    [MenuItem("Tools/Volume Cloud/Build L13 Raymarched Volume Cloud Demo")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Textures");

        Texture3D shapeNoise = LoadOrCreateShapeNoise();
        Texture3D detailNoise = LoadOrCreateDetailNoise();
        Texture2D weatherMap = LoadOrCreateWeatherMap();
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
        cloudVolume.transform.localScale = new Vector3(240f, 76f, 160f);
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
        cloudController.shapeScale = 10.5f;
        cloudController.detailScale = 38f;
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
        cloudController.stepCount = 48;
        cloudController.lightStepCount = 4;

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
        RenderSettings.ambientSkyColor = new Color(0.43f, 0.52f, 0.66f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.28f, 0.34f, 0.43f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.11f, 0.13f, 0.15f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.fogColor = new Color(0.56f, 0.65f, 0.76f, 1f);
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
        Directory.CreateDirectory(Root + "/Textures");
        Texture3D shapeNoise = LoadOrCreateShapeNoise();
        Texture3D detailNoise = LoadOrCreateDetailNoise();
        Texture2D weatherMap = LoadOrCreateWeatherMap();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);
        if (material != null)
        {
            ConfigureCloudMaterial(material, shapeNoise, detailNoise, weatherMap);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("L13 noise textures regenerated and rebound.");
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
        material.SetColor("_CloudColor", new Color(1f, 0.92f, 0.78f, 1f));
        material.SetColor("_ShadowColor", new Color(0.48f, 0.56f, 0.68f, 1f));
        material.SetColor("_AmbientColor", new Color(0.46f, 0.55f, 0.72f, 1f));
        material.SetTexture("_ShapeNoise", shapeNoise);
        material.SetTexture("_DetailNoise", detailNoise);
        material.SetTexture("_WeatherMap", weatherMap);
        material.SetFloat("_Density", 3.2f);
        material.SetFloat("_Coverage", 0.6f);
        material.SetFloat("_WeatherStrength", 0.72f);
        material.SetFloat("_ShapeScale", 10.5f);
        material.SetFloat("_DetailScale", 38f);
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
        material.SetInt("_StepCount", 48);
        material.SetInt("_LightStepCount", 4);
        material.SetFloat("_Opacity", 0.92f);
        material.renderQueue = 3020;
    }

    private static Texture3D LoadOrCreateShapeNoise()
    {
        Texture3D texture = AssetDatabase.LoadAssetAtPath<Texture3D>(ShapeNoisePath);
        if (texture == null)
        {
            texture = new Texture3D(ShapeNoiseSize, ShapeNoiseSize, ShapeNoiseSize, TextureFormat.RGBA32, true)
            {
                name = "ShapeNoise3D"
            };
            AssetDatabase.CreateAsset(texture, ShapeNoisePath);
        }

        Color[] colors = new Color[ShapeNoiseSize * ShapeNoiseSize * ShapeNoiseSize];
        int cursor = 0;
        for (int z = 0; z < ShapeNoiseSize; z++)
        {
            for (int y = 0; y < ShapeNoiseSize; y++)
            {
                for (int x = 0; x < ShapeNoiseSize; x++)
                {
                    Vector3 p = new Vector3(x, y, z) / ShapeNoiseSize;
                    float baseNoise = Fbm(p * 4.0f, 5, 1001);
                    float broadWorley = Worley(p * 4.5f, 2001);
                    float softBillow = 1f - Mathf.Abs(baseNoise * 2f - 1f);
                    float perlinWorley = Mathf.Clamp01(baseNoise * 0.66f + broadWorley * 0.42f - 0.08f);
                    colors[cursor++] = new Color(perlinWorley, broadWorley, Mathf.Clamp01(perlinWorley * 0.72f + softBillow * 0.36f), baseNoise);
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

    private static Texture3D LoadOrCreateDetailNoise()
    {
        Texture3D texture = AssetDatabase.LoadAssetAtPath<Texture3D>(DetailNoisePath);
        if (texture == null)
        {
            texture = new Texture3D(DetailNoiseSize, DetailNoiseSize, DetailNoiseSize, TextureFormat.RGBA32, true)
            {
                name = "DetailNoise3D"
            };
            AssetDatabase.CreateAsset(texture, DetailNoisePath);
        }

        Color[] colors = new Color[DetailNoiseSize * DetailNoiseSize * DetailNoiseSize];
        int cursor = 0;
        for (int z = 0; z < DetailNoiseSize; z++)
        {
            for (int y = 0; y < DetailNoiseSize; y++)
            {
                for (int x = 0; x < DetailNoiseSize; x++)
                {
                    Vector3 p = new Vector3(x, y, z) / DetailNoiseSize;
                    float fineA = Worley(p * 7.5f, 3001);
                    float fineB = Worley(p * 13.0f + Vector3.one * 3.17f, 4001);
                    float fineC = Worley(p * 24.0f + Vector3.one * 7.31f, 5001);
                    float combined = Mathf.Clamp01(fineA * 0.52f + fineB * 0.32f + fineC * 0.22f);
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

    private static Texture2D LoadOrCreateWeatherMap()
    {
        Texture2D texture = new Texture2D(WeatherMapSize, WeatherMapSize, TextureFormat.RGBA32, true, false)
        {
            name = "WeatherMap"
        };

        Color[] colors = new Color[WeatherMapSize * WeatherMapSize];
        int cursor = 0;
        for (int y = 0; y < WeatherMapSize; y++)
        {
            for (int x = 0; x < WeatherMapSize; x++)
            {
                Vector3 p = new Vector3(x / (float)WeatherMapSize, 0.37f, y / (float)WeatherMapSize);
                float system = Fbm(p * 2.8f, 5, 6001);
                float breakup = Fbm(p * 9.5f + Vector3.one * 2.73f, 4, 7001);
                float coverage = Mathf.SmoothStep(0.34f, 0.86f, system * 0.78f + breakup * 0.28f);
                float cloudType = Mathf.SmoothStep(0.28f, 0.78f, Fbm(p * 4.2f + Vector3.one * 5.91f, 4, 8001));
                float density = Mathf.Lerp(0.68f, 1.0f, Fbm(p * 5.8f + Vector3.one * 11.7f, 4, 9001));
                float detailAmount = Mathf.Lerp(0.72f, 1.0f, breakup);
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
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(WeatherMapPath);
    }

    private static float Fbm(Vector3 p, int octaves, int seed)
    {
        float sum = 0f;
        float amplitude = 0.5f;
        float normalization = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += ValueNoise(p, seed + i * 131) * amplitude;
            normalization += amplitude;
            p = p * 2.03f + Vector3.one * 17.13f;
            amplitude *= 0.5f;
        }

        return sum / Mathf.Max(normalization, 0.0001f);
    }

    private static float ValueNoise(Vector3 p, int seed)
    {
        int ix = Mathf.FloorToInt(p.x);
        int iy = Mathf.FloorToInt(p.y);
        int iz = Mathf.FloorToInt(p.z);
        float fx = Smooth01(p.x - ix);
        float fy = Smooth01(p.y - iy);
        float fz = Smooth01(p.z - iz);

        float n000 = Hash01(ix, iy, iz, seed);
        float n100 = Hash01(ix + 1, iy, iz, seed);
        float n010 = Hash01(ix, iy + 1, iz, seed);
        float n110 = Hash01(ix + 1, iy + 1, iz, seed);
        float n001 = Hash01(ix, iy, iz + 1, seed);
        float n101 = Hash01(ix + 1, iy, iz + 1, seed);
        float n011 = Hash01(ix, iy + 1, iz + 1, seed);
        float n111 = Hash01(ix + 1, iy + 1, iz + 1, seed);

        float nx00 = Mathf.Lerp(n000, n100, fx);
        float nx10 = Mathf.Lerp(n010, n110, fx);
        float nx01 = Mathf.Lerp(n001, n101, fx);
        float nx11 = Mathf.Lerp(n011, n111, fx);
        return Mathf.Lerp(Mathf.Lerp(nx00, nx10, fy), Mathf.Lerp(nx01, nx11, fy), fz);
    }

    private static float Worley(Vector3 p, int seed)
    {
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
                    Vector3 feature = new Vector3(
                        px + Hash01(px, py, pz, seed),
                        py + Hash01(px, py, pz, seed + 17),
                        pz + Hash01(px, py, pz, seed + 31));
                    minDistance = Mathf.Min(minDistance, Vector3.Distance(p, feature));
                }
            }
        }

        return Mathf.Clamp01(1f - minDistance / 1.15f);
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
            return material;
        }

        Shader shader = Shader.Find("Skybox/Procedural");
        material = new Material(shader)
        {
            name = "L13_ProceduralSky"
        };
        if (material.HasProperty("_SkyTint")) material.SetColor("_SkyTint", new Color(0.54f, 0.65f, 0.85f, 1f));
        if (material.HasProperty("_GroundColor")) material.SetColor("_GroundColor", new Color(0.26f, 0.29f, 0.33f, 1f));
        if (material.HasProperty("_AtmosphereThickness")) material.SetFloat("_AtmosphereThickness", 1.08f);
        if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 1.2f);
        AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
        return material;
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
