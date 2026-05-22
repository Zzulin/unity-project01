using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class L15WaterDemoBuilder
{
    private const string Root = "Assets/L15 Water";
    private const string ScenePath = Root + "/L15.unity";
    private const string WaterMaterialPath = Root + "/Materials/L15_Modern_Anime_Water.mat";
    private const string SeabedMaterialPath = Root + "/Materials/L15_Caustic_Seabed.mat";
    private const string WaterMeshPath = Root + "/Textures/L15_Water_Surface_Mesh.asset";
    private const string SeabedMeshPath = Root + "/Textures/L15_Seabed_Basin_Mesh.asset";
    private const string NormalAPath = Root + "/Textures/L15_Water_Normal_A.asset";
    private const string NormalBPath = Root + "/Textures/L15_Water_Normal_B.asset";
    private const string CausticPath = Root + "/Textures/L15_Caustic_Ribbon.asset";
    private const string SandPath = Root + "/Textures/L15_Sand_Detail.asset";
    private const string VolumeProfilePath = Root + "/Textures/L15_Water_Post_Profile.asset";
    private const string SkyboxMaterialPath = Root + "/Materials/L15_Procedural_Skybox.mat";

    [MenuItem("Tools/Water/Build L15 Modern Anime Water Demo")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("L15 water demo builder cannot rebuild scenes during Play Mode. Stop Play Mode and run the menu again.");
            return;
        }

        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory(Root + "/Editor");
        Directory.CreateDirectory(Root + "/Docs");
        AssetDatabase.Refresh();

        EnableUrpDepthAndOpaqueTextures();

        Texture2D normalA = LoadOrCreateNormalTexture(NormalAPath, "L15_Water_Normal_A", 512, 2.2f, 0.78f);
        Texture2D normalB = LoadOrCreateNormalTexture(NormalBPath, "L15_Water_Normal_B", 512, 6.4f, 0.58f);
        Texture2D caustic = LoadOrCreateCausticTexture();
        Texture2D sand = LoadOrCreateSandTexture();
        Material waterMaterial = LoadOrCreateWaterMaterial(normalA, normalB, caustic);
        Material seabedMaterial = LoadOrCreateSeabedMaterial(sand, caustic);

        Mesh waterMesh = LoadOrCreateWaterMesh();
        Mesh seabedMesh = LoadOrCreateSeabedMesh();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L15 Modern Anime Water";

        ConfigureLightingAndFog();
        RenderSettings.skybox = LoadOrCreateSkyboxMaterial();
        CreatePostVolume();

        GameObject seabed = new GameObject("Caustic Seabed - Concave Basin");
        MeshFilter seabedFilter = seabed.AddComponent<MeshFilter>();
        seabedFilter.sharedMesh = seabedMesh;
        MeshRenderer seabedRenderer = seabed.AddComponent<MeshRenderer>();
        seabedRenderer.sharedMaterial = seabedMaterial;
        MeshCollider seabedCollider = seabed.AddComponent<MeshCollider>();
        seabedCollider.sharedMesh = seabedMesh;

        GameObject water = new GameObject("Modern Anime Water Surface - Gerstner Depth Refraction");
        MeshFilter waterFilter = water.AddComponent<MeshFilter>();
        waterFilter.sharedMesh = waterMesh;
        MeshRenderer waterRenderer = water.AddComponent<MeshRenderer>();
        waterRenderer.sharedMaterial = waterMaterial;

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 50f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 180f;
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        L15WaterCameraRig cameraRig = cameraObject.AddComponent<L15WaterCameraRig>();
        cameraRig.target = null;
        cameraRig.focusPoint = new Vector3(0f, 0f, -4f);
        cameraRig.distance = 24f;
        cameraRig.minDistance = 5f;
        cameraRig.maxDistance = 64f;
        cameraRig.yaw = -28f;
        cameraRig.pitch = 31f;
        Vector3 focus = cameraRig.focusPoint + Vector3.up * cameraRig.lookHeight;
        Quaternion initialRotation = Quaternion.Euler(cameraRig.pitch, cameraRig.yaw, 0f);
        cameraObject.transform.position = focus + initialRotation * new Vector3(0f, 0f, -cameraRig.distance);
        cameraObject.transform.rotation = Quaternion.LookRotation(focus - cameraObject.transform.position, Vector3.up);

        GameObject hud = new GameObject("Demo HUD");
        L15WaterDemoHud demoHud = hud.AddComponent<L15WaterDemoHud>();
        demoHud.waterMaterial = waterMaterial;
        demoHud.seabedMaterial = seabedMaterial;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = water;
        Debug.Log($"L15 modern anime water demo rebuilt: {ScenePath}");
    }

    private static void EnableUrpDepthAndOpaqueTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/Settings/", System.StringComparison.Ordinal))
            {
                continue;
            }

            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null)
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
                Debug.Log($"L15 water enabled URP Depth/Opaque Texture on {path}");
            }
        }
    }

    private static bool SetBoolIfPresent(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.Boolean)
        {
            return false;
        }

        if (property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }

    private static void ConfigureLightingAndFog()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.52f, 0.73f, 0.88f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.18f, 0.46f, 0.56f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.025f, 0.10f, 0.16f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.42f, 0.72f, 0.82f, 1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 38f;
        RenderSettings.fogEndDistance = 118f;

        GameObject sun = new GameObject("Directional Light - Warm Low Sun");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.65f;
        light.color = new Color(1f, 0.94f, 0.78f, 1f);
        sun.transform.rotation = Quaternion.Euler(36f, -32f, 0f);
        RenderSettings.sun = light;

        GameObject fill = new GameObject("Soft Cyan Fill Light");
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.18f;
        fillLight.color = new Color(0.38f, 0.78f, 1f, 1f);
        fill.transform.rotation = Quaternion.Euler(18f, 132f, 0f);
    }

    private static Material LoadOrCreateSkyboxMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        Shader shader = Shader.Find("Skybox/Procedural");
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "L15_Procedural_Skybox"
            };
            AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_SkyTint", new Color(0.55f, 0.78f, 0.92f, 1f));
        material.SetColor("_GroundColor", new Color(0.28f, 0.46f, 0.50f, 1f));
        material.SetFloat("_AtmosphereThickness", 0.74f);
        material.SetFloat("_Exposure", 1.12f);
        return material;
    }

    private static void CreatePostVolume()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "L15_Water_Post_Profile";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
        }
        bloom.intensity.Override(0.28f);
        bloom.threshold.Override(1.05f);
        bloom.scatter.Override(0.55f);

        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }
        colorAdjustments.postExposure.Override(0.05f);
        colorAdjustments.contrast.Override(8f);
        colorAdjustments.saturation.Override(12f);

        if (!profile.TryGet(out Tonemapping tonemapping))
        {
            tonemapping = profile.Add<Tonemapping>(true);
        }
        tonemapping.mode.Override(TonemappingMode.ACES);

        GameObject volumeObject = new GameObject("Global Post Volume - Anime Water Grade");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
    }

    private static Mesh LoadOrCreateWaterMesh()
    {
        const int resolution = 260;
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(WaterMeshPath);
        if (mesh != null
            && mesh.vertexCount == (resolution + 1) * (resolution + 1)
            && mesh.indexFormat == IndexFormat.UInt32)
        {
            return mesh;
        }

        mesh = CreateGridMesh("L15_Water_Surface_Mesh", 74f, resolution, false);
        ReplaceMeshAsset(mesh, WaterMeshPath);
        return mesh;
    }

    private static Mesh LoadOrCreateSeabedMesh()
    {
        const int resolution = 220;
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SeabedMeshPath);
        if (mesh != null
            && mesh.vertexCount == (resolution + 1) * (resolution + 1)
            && mesh.indexFormat == IndexFormat.UInt32)
        {
            return mesh;
        }

        mesh = CreateGridMesh("L15_Seabed_Basin_Mesh", 78f, resolution, true);
        ReplaceMeshAsset(mesh, SeabedMeshPath);
        return mesh;
    }

    private static void ReplaceMeshAsset(Mesh mesh, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(mesh, path);
    }

    private static Mesh CreateGridMesh(string name, float size, int resolution, bool basin)
    {
        int vertCount = (resolution + 1) * (resolution + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[resolution * resolution * 6];

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float u = x / (float)resolution;
                float v = z / (float)resolution;
                float px = (u - 0.5f) * size;
                float pz = (v - 0.5f) * size;
                float y = basin ? BasinHeight(px, pz, size) : 0f;
                int index = z * (resolution + 1) + x;
                vertices[index] = new Vector3(px, y, pz);
                uvs[index] = new Vector2(u, v);
            }
        }

        int ti = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int a = z * (resolution + 1) + x;
                int b = a + 1;
                int c = a + resolution + 1;
                int d = c + 1;
                triangles[ti++] = a;
                triangles[ti++] = c;
                triangles[ti++] = b;
                triangles[ti++] = b;
                triangles[ti++] = c;
                triangles[ti++] = d;
            }
        }

        Mesh mesh = new Mesh
        {
            name = name,
            indexFormat = vertCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            vertices = vertices,
            uv = uvs
        };
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float BasinHeight(float x, float z, float size)
    {
        float r = Mathf.Clamp01(new Vector2(x, z).magnitude / (size * 0.49f));
        float shore = Mathf.SmoothStep(0.18f, 1f, r);
        float y = Mathf.Lerp(-6.4f, -0.72f, shore);
        float sandbar = Mathf.Sin(x * 0.12f + Mathf.Sin(z * 0.08f) * 1.2f) * 0.10f;
        float ripples = Mathf.PerlinNoise(x * 0.052f + 17.2f, z * 0.052f + 4.7f) * 0.24f - 0.12f;
        return y + sandbar + ripples;
    }

    private static Material LoadOrCreateWaterMaterial(Texture2D normalA, Texture2D normalB, Texture2D caustic)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
        Shader shader = Shader.Find("L15 Water/Modern Anime Water Surface");
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "L15_Modern_Anime_Water"
            };
            AssetDatabase.CreateAsset(material, WaterMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_ShallowColor", new Color(0.36f, 0.96f, 0.90f, 0.82f));
        material.SetColor("_MidColor", new Color(0.04f, 0.55f, 0.92f, 0.88f));
        material.SetColor("_DeepColor", new Color(0.012f, 0.065f, 0.28f, 0.94f));
        material.SetColor("_SkyColor", new Color(0.70f, 0.97f, 1.0f, 1f));
        material.SetColor("_HorizonColor", new Color(0.16f, 0.64f, 0.86f, 1f));
        material.SetFloat("_DepthMax", 9.4f);
        material.SetFloat("_DepthSteps", 6f);
        material.SetFloat("_DepthBandStrength", 0.34f);
        material.SetFloat("_WaterOpacity", 0.38f);
        material.SetFloat("_RefractionStrength", 0.014f);
        material.SetFloat("_ReflectionStrength", 0.58f);
        material.SetTexture("_NormalA", normalA);
        material.SetTexture("_NormalB", normalB);
        material.SetTexture("_CausticTex", caustic);
        material.SetFloat("_NormalScaleA", 0.68f);
        material.SetFloat("_NormalScaleB", 2.2f);
        material.SetFloat("_NormalStrength", 0.52f);
        material.SetFloat("_FoamDepth", 1.45f);
        material.SetFloat("_FoamAmount", 1.05f);
        material.SetFloat("_FoamScale", 0.34f);
        material.SetFloat("_FoamCutoff", 0.50f);
        material.SetFloat("_CausticStrength", 0.025f);
        material.SetFloat("_CausticScale", 0.30f);
        material.SetFloat("_FresnelPower", 3.2f);
        material.SetFloat("_FresnelStrength", 0.68f);
        material.SetFloat("_SpecularPower", 96f);
        material.SetFloat("_SpecularIntensity", 0.72f);
        return material;
    }

    private static Material LoadOrCreateSeabedMaterial(Texture2D sand, Texture2D caustic)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SeabedMaterialPath);
        Shader shader = Shader.Find("L15 Water/Caustic Seabed");
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "L15_Caustic_Seabed"
            };
            AssetDatabase.CreateAsset(material, SeabedMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetTexture("_SandDetail", sand);
        material.SetTexture("_CausticTex", caustic);
        material.SetFloat("_WaterLevel", 0f);
        material.SetFloat("_DepthRange", 8.5f);
        material.SetFloat("_CausticScale", 0.21f);
        material.SetFloat("_CausticSpeed", 0.72f);
        material.SetFloat("_CausticStrength", 1.55f);
        material.SetFloat("_CausticSharpness", 1.55f);
        material.SetFloat("_CausticWidth", 0.095f);
        material.SetFloat("_CausticWarp", 0.48f);
        material.SetFloat("_SandScale", 0.82f);
        material.SetFloat("_SlopeShade", 0.28f);
        return material;
    }

    private static Texture2D LoadOrCreateNormalTexture(string path, string name, int size, float frequency, float strength)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null || texture.width != size || texture.height != size)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = name
            };
            AssetDatabase.CreateAsset(texture, path);
        }

        float[] heights = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float h = TileableFractal(u, v, frequency, 11.7f, 3.5f, 5, 0.55f);
                h += Mathf.Sin((u * frequency * 1.7f + v * frequency * 0.42f) * Mathf.PI * 2f) * 0.075f;
                heights[y * size + x] = h;
            }
        }

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float hL = heights[y * size + ((x - 1 + size) % size)];
                float hR = heights[y * size + ((x + 1) % size)];
                float hD = heights[((y - 1 + size) % size) * size + x];
                float hU = heights[((y + 1) % size) * size + x];
                Vector3 normal = new Vector3((hL - hR) * strength, (hD - hU) * strength, 1f).normalized;
                pixels[y * size + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
            }
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Texture2D LoadOrCreateCausticTexture()
    {
        const int size = 512;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CausticPath);
        if (texture == null || texture.width != size || texture.height != size)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = "L15_Caustic_Ribbon"
            };
            AssetDatabase.CreateAsset(texture, CausticPath);
        }

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float a = CausticRibbon(u, v, 4.0f, 0.0f);
                float b = CausticRibbon(u + 0.17f, v - 0.11f, 6.0f, 1.7f);
                float c = CausticRibbon(u - 0.08f, v + 0.21f, 9.0f, 3.4f);
                pixels[y * size + x] = new Color(a, b, c, 1f);
            }
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Texture2D LoadOrCreateSandTexture()
    {
        const int size = 512;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SandPath);
        if (texture == null || texture.width != size || texture.height != size)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = "L15_Sand_Detail"
            };
            AssetDatabase.CreateAsset(texture, SandPath);
        }

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float broad = TileableFractal(u, v, 2.0f, 9.2f, 6.4f, 4, 0.54f);
                float grain = TileableFractal(u, v, 18f, 3.2f, 15.4f, 2, 0.44f);
                float ripple = Mathf.Sin((u * 5.0f + v * 1.2f + broad * 0.65f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                float value = Mathf.Clamp01(0.72f + (broad - 0.5f) * 0.14f + (grain - 0.5f) * 0.045f + (ripple - 0.5f) * 0.025f);
                pixels[y * size + x] = new Color(value, value * 0.97f, value * 0.86f, 1f);
            }
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static float CausticRibbon(float u, float v, float frequency, float phase)
    {
        float warpA = TileableFractal(u, v, frequency * 0.18f, phase + 1.9f, phase + 7.1f, 3, 0.52f);
        float warpB = TileableFractal(u + 0.37f, v - 0.21f, frequency * 0.28f, phase + 5.2f, phase + 2.6f, 3, 0.50f);
        Vector2 p = new Vector2(
            Mathf.Repeat(u + (warpA - 0.5f) * 0.10f, 1f),
            Mathf.Repeat(v + (warpB - 0.5f) * 0.10f, 1f));
        float web = TileableWorleyEdge(p.x, p.y, Mathf.RoundToInt(frequency));
        float shimmer = TileableFractal(u, v, frequency * 0.75f, phase + 12.3f, phase + 4.1f, 2, 0.55f);
        return Mathf.Clamp01(Mathf.Pow(web, 2.15f) * Mathf.Lerp(0.72f, 1.25f, shimmer));
    }

    private static float TileableWorleyEdge(float u, float v, int cellCount)
    {
        cellCount = Mathf.Max(2, cellCount);
        float x = u * cellCount;
        float y = v * cellCount;
        int ix = Mathf.FloorToInt(x);
        int iy = Mathf.FloorToInt(y);
        float f1 = 99f;
        float f2 = 99f;

        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                int cx = PositiveModulo(ix + ox, cellCount);
                int cy = PositiveModulo(iy + oy, cellCount);
                Vector2 point = Hash22(cx, cy);
                float px = ix + ox + point.x;
                float py = iy + oy + point.y;
                float dx = px - x;
                float dy = py - y;
                float d = dx * dx + dy * dy;
                if (d < f1)
                {
                    f2 = f1;
                    f1 = d;
                }
                else if (d < f2)
                {
                    f2 = d;
                }
            }
        }

        float edge = 1f - Mathf.Clamp01((Mathf.Sqrt(f2) - Mathf.Sqrt(f1)) * 6.4f);
        return Mathf.SmoothStep(0.20f, 0.92f, edge);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static Vector2 Hash22(int x, int y)
    {
        float hx = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
        float hy = Mathf.Sin(x * 269.5f + y * 183.3f) * 24634.6345f;
        return new Vector2(hx - Mathf.Floor(hx), hy - Mathf.Floor(hy));
    }

    private static float TileableFractal(float u, float v, float baseFrequency, float offsetX, float offsetY, int octaves, float persistence)
    {
        float sum = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            float tileFrequency = baseFrequency * frequency;
            sum += TileablePerlin(u, v, tileFrequency, offsetX + i * 31.7f, offsetY + i * 18.3f) * amplitude;
            norm += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }

        return norm > 0f ? sum / norm : 0f;
    }

    private static float TileablePerlin(float u, float v, float frequency, float offsetX, float offsetY)
    {
        float x = u * frequency;
        float y = v * frequency;
        float a = Mathf.PerlinNoise(offsetX + x, offsetY + y);
        float b = Mathf.PerlinNoise(offsetX + x - frequency, offsetY + y);
        float c = Mathf.PerlinNoise(offsetX + x, offsetY + y - frequency);
        float d = Mathf.PerlinNoise(offsetX + x - frequency, offsetY + y - frequency);
        float xBlend = u * u * (3f - 2f * u);
        float yBlend = v * v * (3f - 2f * v);
        return Mathf.Lerp(Mathf.Lerp(a, b, xBlend), Mathf.Lerp(c, d, xBlend), yBlend);
    }
}
