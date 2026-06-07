using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class L17VolumetricLightingDemoBuilder
{
    private const string Root = "Assets/L17 Volumetric Lighting";
    private const string ScenePath = Root + "/L17.unity";
    private const string RendererPath = "Assets/Settings/NPR Render Pipeline Asset_Renderer.asset";
    private const string NprPipelinePath = "Assets/Settings/NPR Render Pipeline.asset";
    private const string HighPipelinePath = "Assets/Settings/UniversalRP-HighQuality.asset";
    private const string CompositeShaderPath = Root + "/Shaders/L17FrustumVolumetricLighting.shader";
    private const string BlueNoisePath = Root + "/Textures/L17_BlueNoise64.asset";
    private const string WallMaterialPath = Root + "/Materials/L17_RoomWall.mat";
    private const string FloorMaterialPath = Root + "/Materials/L17_DustyFloor.mat";
    private const string WoodMaterialPath = Root + "/Materials/L17_WindowFrame.mat";
    private const string VolumeProfilePath = Root + "/Materials/L17_PostProcessProfile.asset";

    private static readonly Vector3 SunDirection = new Vector3(-0.18f, -0.55f, -1f).normalized;

    [MenuItem("Tools/Volumetric Lighting/Build L17 Modern Window Shafts Demo")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("L17 volumetric lighting demo builder cannot rebuild during Play Mode.");
            return;
        }

        EnsureFolders();
        AssetDatabase.Refresh();
        ConfigureUrpAsset(NprPipelinePath);
        ConfigureUrpAsset(HighPipelinePath);

        Texture2D blueNoise = LoadOrCreateBlueNoise();
        L17FrustumVolumetricRendererFeature feature = EnsureFroxelRendererFeature(blueNoise);
        if (feature != null)
        {
            feature.SetActive(false);
        }

        Material wallMaterial = LoadOrCreateInteriorMaterial(WallMaterialPath, "L17_RoomWall", new Color(0.62f, 0.58f, 0.5f, 1f), new Color(0.12f, 0.105f, 0.09f, 1f), 0.1f, 0.04f, 0.02f, 0.44f);
        Material floorMaterial = LoadOrCreateInteriorMaterial(FloorMaterialPath, "L17_DustyFloor", new Color(0.42f, 0.37f, 0.29f, 1f), new Color(0.08f, 0.07f, 0.06f, 1f), 0.18f, 0.08f, 0.05f, 0.38f);
        Material woodMaterial = LoadOrCreateLitMaterial(WoodMaterialPath, "L17_WindowFrame", new Color(0.21f, 0.14f, 0.08f, 1f), 0.35f);
        VolumeProfile profile = LoadOrCreatePostProfile();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L17 Modern Froxel Volumetric Lighting";

        ConfigureRenderSettings();
        Light sunLight = CreateSun();
        CreateCamera();
        CreatePostVolume(profile);
        CreateRoom(wallMaterial, floorMaterial, woodMaterial);
        CreateInteriorCatchers(floorMaterial);
        if (feature != null)
        {
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
        }
        CreateLightingController(feature, sunLight);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"L17 modern froxel volumetric lighting demo rebuilt: {ScenePath}");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(Root + "/Editor");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory(Root + "/Docs");
    }

    private static void ConfigureUrpAsset(string path)
    {
        UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (asset == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(asset);
        SetBool(serialized, "m_RequireDepthTexture", true);
        SetBool(serialized, "m_RequireOpaqueTexture", true);
        SetBool(serialized, "m_SupportsHDR", true);
        SetBool(serialized, "m_MainLightShadowsSupported", true);
        SetBool(serialized, "m_SoftShadowsSupported", true);
        SetFloat(serialized, "m_ShadowDistance", 72f);
        SetInt(serialized, "m_MainLightShadowmapResolution", 4096);
        SetInt(serialized, "m_ShadowCascadeCount", 4);
        SetInt(serialized, "m_SoftShadowQuality", 3);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    private static void SetBool(SerializedObject serialized, string name, bool value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetFloat(SerializedObject serialized, string name, float value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetInt(SerializedObject serialized, string name, int value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static Texture2D LoadOrCreateBlueNoise()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BlueNoisePath);
        if (texture != null)
        {
            return texture;
        }

        texture = new Texture2D(64, 64, TextureFormat.R8, false, true)
        {
            name = "L17_BlueNoise64",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        Color32[] pixels = new Color32[64 * 64];
        int[] ranks = Enumerable.Range(0, pixels.Length).ToArray();
        uint state = 0x9E3779B9u;
        for (int index = ranks.Length - 1; index > 0; index--)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            int swap = (int)(state % (uint)(index + 1));
            (ranks[index], ranks[swap]) = (ranks[swap], ranks[index]);
        }

        for (int index = 0; index < pixels.Length; index++)
        {
            byte value = (byte)Mathf.RoundToInt(ranks[index] / (float)(pixels.Length - 1) * 255f);
            pixels[index] = new Color32(value, value, value, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        AssetDatabase.CreateAsset(texture, BlueNoisePath);
        return texture;
    }

    private static L17FrustumVolumetricRendererFeature EnsureFroxelRendererFeature(Texture2D blueNoise)
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (rendererData == null)
        {
            Debug.LogWarning($"L17 could not find URP renderer data at {RendererPath}.");
            return null;
        }

        L17FrustumVolumetricRendererFeature feature = rendererData.rendererFeatures
            .OfType<L17FrustumVolumetricRendererFeature>()
            .FirstOrDefault();

        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<L17FrustumVolumetricRendererFeature>();
            feature.name = "L17 Froxel Volumetric Lighting";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
        }

        Shader compositeShader = AssetDatabase.LoadAssetAtPath<Shader>(CompositeShaderPath);
        if (compositeShader == null)
        {
            compositeShader = Shader.Find("Hidden/L17/Froxel Volumetric Composite");
        }

        feature.SetActive(true);
        feature.SetResources(compositeShader, blueNoise);
        feature.settings.enabled = true;
        feature.settings.downsample = 2;
        feature.settings.froxelDepth = 64;
        feature.settings.maxDistance = 58f;
        feature.settings.depthDistribution = 1.9f;
        feature.settings.density = 0.24f;
        feature.settings.extinction = 0.68f;
        feature.settings.intensity = 3.25f;
        feature.settings.anisotropy = 0.78f;
        feature.settings.shadowFloor = 0.035f;
        feature.settings.multiScatter = 0.32f;
        feature.settings.heightOrigin = -0.4f;
        feature.settings.heightFalloff = 0.22f;
        feature.settings.noiseStrength = 0.2f;
        feature.settings.noiseScale = 1.25f;
        feature.settings.temporalBlend = 0.88f;
        feature.settings.temporalDepthRejection = 0.12f;
        feature.settings.bilateralDepthScale = 0.08f;
        feature.settings.compositeOpacity = 0.94f;
        feature.settings.scatteringColor = new Color(1f, 0.84f, 0.52f, 1f);
        feature.settings.passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
        return feature;
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.08f, 0.082f, 0.09f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.045f, 0.04f, 0.034f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.018f, 0.015f, 0.012f, 1f);
        RenderSettings.fog = false;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.038f, 0.045f, 1f);
        camera.fieldOfView = 42f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 130f;
        camera.allowHDR = true;
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.requiresDepthTexture = true;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.SetPositionAndRotation(
            new Vector3(-2.4f, 2.35f, -7.4f),
            Quaternion.Euler(5.2f, 13.5f, 0f));
    }

    private static Light CreateSun()
    {
        GameObject sun = new GameObject("Low Angle Sun");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.35f;
        light.color = new Color(1f, 0.93f, 0.78f, 1f);
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 1f;
        light.shadowBias = 0.018f;
        light.shadowNormalBias = 0.18f;
        light.shadowNearPlane = 0.2f;
        sun.transform.position = new Vector3(-2.8f, 7.2f, 13.5f);
        sun.transform.rotation = Quaternion.LookRotation(SunDirection, Vector3.up);
        RenderSettings.sun = light;
        return light;
    }

    private static void CreatePostVolume(VolumeProfile profile)
    {
        GameObject volumeObject = new GameObject("L17 Bloom Tonemapping Volume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
    }

    private static VolumeProfile LoadOrCreatePostProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "L17_PostProcessProfile";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.72f);
        bloom.intensity.Override(0.42f);
        bloom.scatter.Override(0.62f);
        bloom.tint.Override(new Color(1f, 0.88f, 0.64f, 1f));

        Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.Override(-0.15f);
        color.contrast.Override(18f);
        color.saturation.Override(-4f);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.active = true;
        vignette.intensity.Override(0.18f);
        vignette.smoothness.Override(0.58f);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
        }

        return component;
    }

    private static void CreateRoom(Material wallMaterial, Material floorMaterial, Material woodMaterial)
    {
        CreateCube("Room Floor", new Vector3(0f, -0.18f, 0f), new Vector3(17f, 0.36f, 18f), floorMaterial, true, true);
        CreateCube("Room Ceiling", new Vector3(0f, 6.5f, 0f), new Vector3(17f, 0.35f, 18f), wallMaterial, true, true);
        CreateCube("Room Wall Left", new Vector3(-8.25f, 3.1f, 0f), new Vector3(0.35f, 6.7f, 18f), wallMaterial, true, true);
        CreateCube("Room Wall Right", new Vector3(8.25f, 3.1f, 0f), new Vector3(0.35f, 6.7f, 18f), wallMaterial, true, true);
        CreateCube("Room Back Wall", new Vector3(0f, 3.1f, -8.7f), new Vector3(17f, 6.7f, 0.35f), wallMaterial, true, true);

        CreateCube("Window Wall Left Fill", new Vector3(-5.65f, 3.1f, 8.45f), new Vector3(5.3f, 6.7f, 0.35f), wallMaterial, true, true);
        CreateCube("Window Wall Right Fill", new Vector3(5.65f, 3.1f, 8.45f), new Vector3(5.3f, 6.7f, 0.35f), wallMaterial, true, true);
        CreateCube("Window Wall Lower Fill", new Vector3(0f, 0.78f, 8.45f), new Vector3(6.25f, 1.56f, 0.35f), wallMaterial, true, true);
        CreateCube("Window Wall Upper Fill", new Vector3(0f, 5.78f, 8.45f), new Vector3(6.25f, 1.44f, 0.35f), wallMaterial, true, true);

        CreateCube("Deep Window Sill", new Vector3(0f, 1.68f, 8.18f), new Vector3(6.6f, 0.28f, 1.3f), woodMaterial, true, true);
        CreateCube("Window Top Frame", new Vector3(0f, 5.06f, 8.1f), new Vector3(6.7f, 0.25f, 1.2f), woodMaterial, true, true);
        CreateCube("Window Left Frame", new Vector3(-3.22f, 3.36f, 8.1f), new Vector3(0.25f, 3.55f, 1.2f), woodMaterial, true, true);
        CreateCube("Window Right Frame", new Vector3(3.22f, 3.36f, 8.1f), new Vector3(0.25f, 3.55f, 1.2f), woodMaterial, true, true);
        CreateCube("Window Center Mullion", new Vector3(0f, 3.36f, 8.03f), new Vector3(0.32f, 3.5f, 1.25f), woodMaterial, true, true);
        CreateCube("Window Upper Transom", new Vector3(0f, 4.12f, 8.02f), new Vector3(6.25f, 0.2f, 1.22f), woodMaterial, true, true);
        CreateCube("Window Lower Transom", new Vector3(0f, 2.75f, 8.02f), new Vector3(6.25f, 0.18f, 1.22f), woodMaterial, true, true);
    }

    private static void CreateInteriorCatchers(Material material)
    {
        CreateCube("Dusty Pedestal", new Vector3(2.6f, 0.78f, -1.4f), new Vector3(1.6f, 1.55f, 1.6f), material, true, true);
        CreateCube("Thin Standing Panel", new Vector3(-2.9f, 1.65f, -0.2f), new Vector3(0.18f, 3.3f, 1.25f), new Vector3(0f, 24f, 0f), material, true, true);
    }

    private static void CreateLightingController(L17FrustumVolumetricRendererFeature feature, Light sunLight)
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        GameObject controllerObject = new GameObject("L17 Froxel Lighting Controller");
        L17VolumetricLightingController controller = controllerObject.AddComponent<L17VolumetricLightingController>();
        controller.rendererData = rendererData;
        controller.sunLight = sunLight;
        controller.downsample = feature != null ? feature.settings.downsample : 2;
        controller.froxelDepth = feature != null ? feature.settings.froxelDepth : 64;
        controller.maxDistance = feature != null ? feature.settings.maxDistance : 58f;
        controller.depthDistribution = feature != null ? feature.settings.depthDistribution : 1.9f;
        controller.density = feature != null ? feature.settings.density : 0.24f;
        controller.extinction = feature != null ? feature.settings.extinction : 0.68f;
        controller.intensity = feature != null ? feature.settings.intensity : 3.25f;
        controller.anisotropy = feature != null ? feature.settings.anisotropy : 0.78f;
        controller.shadowFloor = feature != null ? feature.settings.shadowFloor : 0.035f;
        controller.multiScatter = feature != null ? feature.settings.multiScatter : 0.32f;
        controller.heightOrigin = feature != null ? feature.settings.heightOrigin : -0.4f;
        controller.heightFalloff = feature != null ? feature.settings.heightFalloff : 0.22f;
        controller.noiseStrength = feature != null ? feature.settings.noiseStrength : 0.2f;
        controller.noiseScale = feature != null ? feature.settings.noiseScale : 1.25f;
        controller.temporalBlend = feature != null ? feature.settings.temporalBlend : 0.88f;
        controller.bilateralDepthScale = feature != null ? feature.settings.bilateralDepthScale : 0.08f;
        controller.compositeOpacity = feature != null ? feature.settings.compositeOpacity : 0.94f;
        controller.scatteringColor = feature != null ? feature.settings.scatteringColor : new Color(1f, 0.84f, 0.52f, 1f);
        controller.RefreshImmediate();
    }

    private static Material LoadOrCreateInteriorMaterial(string path, string name, Color baseColor, Color shadowColor, float smoothness, float specularStrength, float wrapDiffuse, float ambientBoost)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("L17 Volumetric Lighting/Two Sided Interior Lit");
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
        material.SetColor("_ShadowColor", shadowColor);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_SpecularStrength", specularStrength);
        material.SetFloat("_WrapDiffuse", wrapDiffuse);
        material.SetFloat("_AmbientBoost", ambientBoost);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material LoadOrCreateLitMaterial(string path, string name, Color color, float smoothness)
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

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, bool castShadows, bool receiveShadows)
    {
        return CreateCube(name, position, scale, Vector3.zero, material, castShadows, receiveShadows);
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Vector3 rotationEuler, Material material, bool castShadows, bool receiveShadows)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.rotation = Quaternion.Euler(rotationEuler);
        cube.transform.localScale = scale;

        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = receiveShadows;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return cube;
    }

}
