using System.IO;
using System.Linq;
using System.Collections.Generic;
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
    private const string VolumeProfilePath = Root + "/Materials/L17_PostProcessProfile.asset";
    private const string LightingSettingsPath = Root + "/L17/L17_LightingSettings.lighting";
    private const string LightmapCubeMeshPath = Root + "/Meshes/L17_LightmapReadyCube.asset";
    private const string LightmapPanelMeshPath = Root + "/Meshes/L17_LightmapReadyPanel.asset";

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

        Material roomMaterial = LoadOrCreateInteriorMaterial(WallMaterialPath, "L17_RoomWall", new Color(0.66f, 0.61f, 0.52f, 1f), new Color(0.12f, 0.105f, 0.09f, 1f), 0f, 0.78f, 0.1f, 0.22f, 0.45f, 0.02f, 1.35f);
        VolumeProfile profile = LoadOrCreatePostProfile();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L17 Modern Froxel Volumetric Lighting";

        ConfigureRenderSettings();
        AssignLightingSettings();
        Light sunLight = CreateSun();
        CreateCamera();
        CreatePostVolume(profile);
        Transform geometryRoot = CreateGeometryRoot();
        CreateRoom(roomMaterial, geometryRoot);
        CreateInteriorCatchers(roomMaterial, geometryRoot);
        Transform localVolumeBounds = FindOrCreateLocalVolumeBounds();
        if (feature != null)
        {
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
        }
        CreateLightingController(feature, sunLight, localVolumeBounds);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"L17 modern froxel volumetric lighting demo rebuilt: {ScenePath}");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(Root + "/Editor");
        Directory.CreateDirectory(Root + "/L17");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Meshes");
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
        SetFloat(serialized, "m_ShadowDistance", 350f);
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
            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = value;
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = value ? 1 : 0;
            }
        }
    }

    private static void SetFloat(SerializedObject serialized, string name, float value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            if (property.propertyType == SerializedPropertyType.Float)
            {
                property.floatValue = value;
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = Mathf.RoundToInt(value);
            }
        }
    }

    private static void SetInt(SerializedObject serialized, string name, int value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = value;
            }
            else if (property.propertyType == SerializedPropertyType.Enum)
            {
                property.enumValueIndex = Mathf.Clamp(value, 0, property.enumDisplayNames.Length - 1);
            }
            else if (property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = value != 0;
            }
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
        L17FrustumVolumetricRendererFeature.Settings settings = feature.settings;
        settings.enabled = true;
        settings.requireSceneController = true;
        settings.downsample = 2;
        settings.froxelDepth = 96;
        settings.maxDistance = 58f;
        settings.depthDistribution = 1.9f;
        settings.density = 0.24f;
        settings.extinction = 0.68f;
        settings.intensity = 3.25f;
        settings.anisotropy = 0.78f;
        settings.shadowFloor = 0.015f;
        settings.multiScatter = 0.32f;
        settings.heightOrigin = -0.4f;
        settings.heightFalloff = 0.22f;
        settings.noiseStrength = 0f;
        settings.noiseScale = 1.25f;
        settings.volumeBoundsCenter = new Vector3(0f, 3.1f, -0.1f);
        settings.volumeBoundsSize = new Vector3(15.8f, 6.2f, 16.2f);
        settings.volumeBoundsSoftness = 0.45f;
        settings.temporalAccumulation = true;
        settings.jitterStrength = 0.9f;
        settings.temporalBlend = 0.8f;
        settings.temporalDepthRejection = 0.12f;
        settings.bilateralDepthScale = 0.08f;
        settings.compositeOpacity = 0.94f;
        settings.scatteringColor = new Color(1f, 0.84f, 0.52f, 1f);
        settings.passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
        return feature;
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.08f, 0.082f, 0.09f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.045f, 0.04f, 0.034f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.018f, 0.015f, 0.012f, 1f);
        RenderSettings.fog = false;
    }

    private static void AssignLightingSettings()
    {
        LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
        if (settings == null)
        {
            settings = new LightingSettings { name = "L17_LightingSettings" };
            AssetDatabase.CreateAsset(settings, LightingSettingsPath);
        }

        SerializedObject serialized = new SerializedObject(settings);
        SetBool(serialized, "m_EnableBakedLightmaps", true);
        SetBool(serialized, "m_EnableRealtimeLightmaps", false);
        SetFloat(serialized, "m_BounceScale", 1.25f);
        SetFloat(serialized, "m_AlbedoBoost", 1.1f);
        SetFloat(serialized, "m_IndirectOutputScale", 1.8f);
        SetInt(serialized, "m_BakeBackend", 2);
        SetInt(serialized, "m_LightmapMaxSize", 1024);
        SetFloat(serialized, "m_BakeResolution", 16f);
        SetInt(serialized, "m_Padding", 3);
        SetBool(serialized, "m_AO", false);
        SetFloat(serialized, "m_AOMaxDistance", 0.35f);
        SetInt(serialized, "m_MixedBakeMode", 0);
        SetInt(serialized, "m_LightmapsBakeMode", 1);
        SetBool(serialized, "m_PVRCulling", false);
        SetInt(serialized, "m_PVRSampleCount", 512);
        SetInt(serialized, "m_PVRBounces", 4);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Lightmapping.lightingSettings = settings;
        EditorUtility.SetDirty(settings);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.Skybox;
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

        L17RuntimeCameraMotion cameraMotion = cameraObject.AddComponent<L17RuntimeCameraMotion>();
        cameraMotion.enableRuntimeControls = true;
        cameraMotion.moveSpeed = 3.2f;
        cameraMotion.fastMoveMultiplier = 2.2f;
        cameraMotion.lookSensitivity = 2.4f;
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
        light.lightmapBakeType = LightmapBakeType.Mixed;
        light.bounceIntensity = 2f;
        light.lightShadowCasterMode = LightShadowCasterMode.Everything;
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

        profile.components.RemoveAll(component => component == null);

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
        AssetDatabase.ImportAsset(VolumeProfilePath);
        return profile;
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
        }

        if (!AssetDatabase.Contains(component))
        {
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
        }

        EditorUtility.SetDirty(component);
        return component;
    }

    private static Transform CreateGeometryRoot()
    {
        GameObject root = new GameObject("L17 Room Geometry");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return root.transform;
    }

    private static void CreateRoom(Material roomMaterial, Transform parent)
    {
        CreatePanel("Room Floor", new Vector3(0f, -0.18f, 0f), new Vector3(-90f, 0f, 0f), new Vector3(17f, 18f, 1f), roomMaterial, true, true, parent);
        CreatePanel("Room Ceiling", new Vector3(0f, 6.5f, 0f), new Vector3(90f, 0f, 0f), new Vector3(17f, 18f, 1f), roomMaterial, true, true, parent);
        CreatePanel("Room Wall Left", new Vector3(-8.25f, 3.1f, 0f), new Vector3(0f, 90f, 0f), new Vector3(18f, 6.7f, 1f), roomMaterial, true, true, parent);
        CreatePanel("Room Wall Right", new Vector3(8.25f, 3.1f, 0f), new Vector3(0f, -90f, 0f), new Vector3(18f, 6.7f, 1f), roomMaterial, true, true, parent);
        CreatePanel("Room Back Wall", new Vector3(0f, 3.1f, -8.7f), Vector3.zero, new Vector3(17f, 6.7f, 1f), roomMaterial, true, true, parent);

        CreateCube("Window Wall Left Fill", new Vector3(-5.65f, 3.1f, 8.45f), new Vector3(5.3f, 6.7f, 0.35f), roomMaterial, true, true, parent, false);
        CreateCube("Window Wall Right Fill", new Vector3(5.65f, 3.1f, 8.45f), new Vector3(5.3f, 6.7f, 0.35f), roomMaterial, true, true, parent, false);
        CreateCube("Window Wall Lower Fill", new Vector3(0f, 0.78f, 8.45f), new Vector3(6.25f, 1.56f, 0.35f), roomMaterial, true, true, parent, false);
        CreateCube("Window Wall Upper Fill", new Vector3(0f, 5.78f, 8.45f), new Vector3(6.25f, 1.44f, 0.35f), roomMaterial, true, true, parent, false);

        CreateCube("Deep Window Sill", new Vector3(0f, 1.68f, 8.18f), new Vector3(6.6f, 0.28f, 1.3f), roomMaterial, true, true, parent, false);
        CreateCube("Window Top Frame", new Vector3(0f, 5.06f, 8.1f), new Vector3(6.7f, 0.25f, 1.2f), roomMaterial, true, true, parent, false);
        CreateCube("Window Left Frame", new Vector3(-3.22f, 3.36f, 8.1f), new Vector3(0.25f, 3.55f, 1.2f), roomMaterial, true, true, parent, false);
        CreateCube("Window Right Frame", new Vector3(3.22f, 3.36f, 8.1f), new Vector3(0.25f, 3.55f, 1.2f), roomMaterial, true, true, parent, false);
        CreateCube("Window Center Mullion", new Vector3(0f, 3.36f, 8.03f), new Vector3(0.32f, 3.5f, 1.25f), roomMaterial, true, true, parent, false);
        CreateCube("Window Upper Transom", new Vector3(0f, 4.12f, 8.02f), new Vector3(6.25f, 0.2f, 1.22f), roomMaterial, true, true, parent, false);
        CreateCube("Window Lower Transom", new Vector3(0f, 2.75f, 8.02f), new Vector3(6.25f, 0.18f, 1.22f), roomMaterial, true, true, parent, false);
    }

    private static void CreateInteriorCatchers(Material material, Transform parent)
    {
        CreateCube("Dusty Pedestal", new Vector3(2.6f, 0.78f, -1.4f), new Vector3(1.6f, 1.55f, 1.6f), material, true, true, parent, false);
        CreateCube("Thin Standing Panel", new Vector3(-2.9f, 1.65f, -0.2f), new Vector3(0.18f, 3.3f, 1.25f), new Vector3(0f, 24f, 0f), material, true, true, parent, false);
    }

    private static Transform FindOrCreateLocalVolumeBounds()
    {
        GameObject boundsObject = GameObject.Find("L17 Local Volume Bounds");
        if (boundsObject == null)
        {
            boundsObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundsObject.name = "L17 Local Volume Bounds";
        }

        boundsObject.transform.SetPositionAndRotation(new Vector3(0f, 3.1f, -0.1f), Quaternion.identity);
        boundsObject.transform.localScale = new Vector3(15.8f, 6.2f, 16.2f);

        MeshRenderer renderer = boundsObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Object.DestroyImmediate(renderer);
        }

        Collider collider = boundsObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        if (boundsObject.GetComponent<L17LocalVolumeBoundsGizmo>() == null)
        {
            boundsObject.AddComponent<L17LocalVolumeBoundsGizmo>();
        }

        return boundsObject.transform;
    }

    private static void CreateLightingController(L17FrustumVolumetricRendererFeature feature, Light sunLight, Transform localVolumeBounds)
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        L17FrustumVolumetricRendererFeature.Settings settings =
            feature != null ? feature.settings : new L17FrustumVolumetricRendererFeature.Settings();
        GameObject controllerObject = new GameObject("L17 Froxel Lighting Controller");
        L17VolumetricLightingController controller = controllerObject.AddComponent<L17VolumetricLightingController>();
        controller.rendererData = rendererData;
        controller.sunLight = sunLight;
        controller.volumeBoundsTransform = localVolumeBounds;
        controller.downsample = settings.downsample;
        controller.froxelDepth = settings.froxelDepth;
        controller.maxDistance = settings.maxDistance;
        controller.depthDistribution = settings.depthDistribution;
        controller.density = settings.density;
        controller.extinction = settings.extinction;
        controller.intensity = settings.intensity;
        controller.anisotropy = settings.anisotropy;
        controller.shadowFloor = settings.shadowFloor;
        controller.multiScatter = settings.multiScatter;
        controller.heightOrigin = settings.heightOrigin;
        controller.heightFalloff = settings.heightFalloff;
        controller.noiseStrength = settings.noiseStrength;
        controller.noiseScale = settings.noiseScale;
        controller.volumeBoundsCenter = settings.volumeBoundsCenter;
        controller.volumeBoundsSize = settings.volumeBoundsSize;
        controller.volumeBoundsSoftness = settings.volumeBoundsSoftness;
        controller.temporalAccumulation = settings.temporalAccumulation;
        controller.jitterStrength = settings.jitterStrength;
        controller.temporalBlend = settings.temporalBlend;
        controller.bilateralDepthScale = settings.bilateralDepthScale;
        controller.compositeOpacity = settings.compositeOpacity;
        controller.scatteringColor = settings.scatteringColor;
        controller.RefreshImmediate();
    }

    private static Material LoadOrCreateInteriorMaterial(
        string path,
        string name,
        Color baseColor,
        Color shadowColor,
        float metallic,
        float roughness,
        float smoothness,
        float specularStrength,
        float environmentStrength,
        float wrapDiffuse,
        float ambientBoost)
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
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Roughness", roughness);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_SpecularStrength", specularStrength);
        material.SetFloat("_EnvironmentStrength", environmentStrength);
        material.SetFloat("_NormalMapScale", 1f);
        material.SetFloat("_OcclusionStrength", 1f);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cutoff", 0.5f);
        material.SetFloat("_WrapDiffuse", wrapDiffuse);
        material.SetFloat("_AmbientBoost", ambientBoost);
        material.doubleSidedGI = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, bool castShadows, bool receiveShadows, Transform parent, bool contributeGi)
    {
        return CreateCube(name, position, scale, Vector3.zero, material, castShadows, receiveShadows, parent, contributeGi);
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Vector3 rotationEuler, Material material, bool castShadows, bool receiveShadows, Transform parent, bool contributeGi = true)
    {
        GameObject cube = new GameObject(name);
        MeshFilter filter = cube.AddComponent<MeshFilter>();
        filter.sharedMesh = LoadOrCreateLightmapCubeMesh();
        MeshRenderer renderer = cube.AddComponent<MeshRenderer>();
        cube.transform.position = position;
        cube.transform.rotation = Quaternion.Euler(rotationEuler);
        cube.transform.localScale = scale;
        if (parent != null)
        {
            cube.transform.SetParent(parent, true);
        }

        ConfigureRenderer(cube, renderer, material, castShadows, receiveShadows, contributeGi);

        return cube;
    }

    private static GameObject CreatePanel(string name, Vector3 position, Vector3 rotationEuler, Vector3 scale, Material material, bool castShadows, bool receiveShadows, Transform parent)
    {
        GameObject panel = new GameObject(name);
        MeshFilter filter = panel.AddComponent<MeshFilter>();
        filter.sharedMesh = LoadOrCreateLightmapPanelMesh();
        MeshRenderer renderer = panel.AddComponent<MeshRenderer>();
        panel.transform.position = position;
        panel.transform.rotation = Quaternion.Euler(rotationEuler);
        panel.transform.localScale = scale;
        if (parent != null)
        {
            panel.transform.SetParent(parent, true);
        }

        ConfigureRenderer(panel, renderer, material, castShadows, receiveShadows, true);
        return panel;
    }

    private static void ConfigureRenderer(GameObject target, MeshRenderer renderer, Material material, bool castShadows, bool receiveShadows, bool contributeGi)
    {
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = receiveShadows;
        SerializedObject rendererSerialized = new SerializedObject(renderer);
        SetBool(rendererSerialized, "m_PreserveUVs", false);
        SerializedProperty scaleInLightmap = rendererSerialized.FindProperty("m_ScaleInLightmap");
        if (scaleInLightmap != null)
        {
            scaleInLightmap.floatValue = contributeGi ? 1f : 0f;
        }

        SerializedProperty receiveGi = rendererSerialized.FindProperty("m_ReceiveGI");
        if (receiveGi != null)
        {
            receiveGi.intValue = contributeGi ? 1 : 2;
        }

        rendererSerialized.ApplyModifiedPropertiesWithoutUndo();
        StaticEditorFlags staticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;
        if (contributeGi)
        {
            staticFlags |= StaticEditorFlags.ContributeGI;
        }

        GameObjectUtility.SetStaticEditorFlags(target, staticFlags);
    }

    private static Mesh LoadOrCreateLightmapCubeMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(LightmapCubeMeshPath);
        EnsureFolders();
        bool createAsset = mesh == null;
        if (createAsset)
        {
            mesh = new Mesh
            {
                name = "L17_LightmapReadyCube"
            };
        }

        RebuildLightmapCubeMesh(mesh);
        EditorUtility.SetDirty(mesh);
        if (createAsset)
        {
            AssetDatabase.CreateAsset(mesh, LightmapCubeMeshPath);
        }

        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static void RebuildLightmapCubeMesh(Mesh mesh)
    {
        List<Vector3> vertices = new List<Vector3>(24);
        List<Vector3> normals = new List<Vector3>(24);
        List<Vector4> tangents = new List<Vector4>(24);
        List<Vector2> uv0 = new List<Vector2>(24);
        List<Vector2> uv2 = new List<Vector2>(24);
        List<int> indices = new List<int>(36);

        AddCubeFace(vertices, normals, tangents, uv0, uv2, indices, 0, new Vector3(0, 0, 0.5f), Vector3.right, Vector3.up, Vector3.forward);
        AddCubeFace(vertices, normals, tangents, uv0, uv2, indices, 1, new Vector3(0, 0, -0.5f), Vector3.left, Vector3.up, Vector3.back);
        AddCubeFace(vertices, normals, tangents, uv0, uv2, indices, 2, new Vector3(0.5f, 0, 0), Vector3.back, Vector3.up, Vector3.right);
        AddCubeFace(vertices, normals, tangents, uv0, uv2, indices, 3, new Vector3(-0.5f, 0, 0), Vector3.forward, Vector3.up, Vector3.left);
        AddCubeFace(vertices, normals, tangents, uv0, uv2, indices, 4, new Vector3(0, 0.5f, 0), Vector3.right, Vector3.back, Vector3.up);
        AddCubeFace(vertices, normals, tangents, uv0, uv2, indices, 5, new Vector3(0, -0.5f, 0), Vector3.right, Vector3.forward, Vector3.down);

        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTangents(tangents);
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(1, uv2);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
    }

    private static Mesh LoadOrCreateLightmapPanelMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(LightmapPanelMeshPath);
        EnsureFolders();
        bool createAsset = mesh == null;
        if (createAsset)
        {
            mesh = new Mesh
            {
                name = "L17_LightmapReadyPanel"
            };
        }

        RebuildLightmapPanelMesh(mesh);
        EditorUtility.SetDirty(mesh);
        if (createAsset)
        {
            AssetDatabase.CreateAsset(mesh, LightmapPanelMeshPath);
        }

        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static void RebuildLightmapPanelMesh(Mesh mesh)
    {
        Vector3[] vertices =
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        Vector3[] normals =
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };
        Vector4[] tangents =
        {
            new Vector4(1f, 0f, 0f, 1f),
            new Vector4(1f, 0f, 0f, 1f),
            new Vector4(1f, 0f, 0f, 1f),
            new Vector4(1f, 0f, 0f, 1f)
        };
        Vector2[] uv =
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        int[] triangles = { 0, 1, 2, 0, 2, 3 };

        mesh.Clear();
        mesh.indexFormat = IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.uv = uv;
        mesh.uv2 = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private static void AddCubeFace(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector4> tangents,
        List<Vector2> uv0,
        List<Vector2> uv2,
        List<int> indices,
        int faceIndex,
        Vector3 center,
        Vector3 axisU,
        Vector3 axisV,
        Vector3 normal)
    {
        int start = vertices.Count;
        vertices.Add(center - axisU * 0.5f - axisV * 0.5f);
        vertices.Add(center + axisU * 0.5f - axisV * 0.5f);
        vertices.Add(center + axisU * 0.5f + axisV * 0.5f);
        vertices.Add(center - axisU * 0.5f + axisV * 0.5f);

        for (int index = 0; index < 4; index++)
        {
            normals.Add(normal);
            tangents.Add(new Vector4(axisU.x, axisU.y, axisU.z, 1f));
        }

        uv0.Add(new Vector2(0f, 0f));
        uv0.Add(new Vector2(1f, 0f));
        uv0.Add(new Vector2(1f, 1f));
        uv0.Add(new Vector2(0f, 1f));

        int column = faceIndex % 3;
        int row = faceIndex / 3;
        Vector2 cellMin = new Vector2(column / 3f, row / 2f);
        Vector2 cellSize = new Vector2(1f / 3f, 1f / 2f);
        Vector2 padding = new Vector2(0.012f, 0.018f);
        Vector2 min = cellMin + padding;
        Vector2 max = cellMin + cellSize - padding;
        uv2.Add(new Vector2(min.x, min.y));
        uv2.Add(new Vector2(max.x, min.y));
        uv2.Add(new Vector2(max.x, max.y));
        uv2.Add(new Vector2(min.x, max.y));

        indices.Add(start + 0);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start + 0);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }

}
