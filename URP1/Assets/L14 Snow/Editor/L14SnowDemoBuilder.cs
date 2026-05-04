using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class L14SnowDemoBuilder
{
    private const string Root = "Assets/L14 Snow";
    private const string ScenePath = Root + "/L14.unity";
    private const string SnowMaterialPath = Root + "/Materials/L14_GPU_Heightfield_Snow.mat";
    private const string PlayerMaterialPath = Root + "/Materials/L14_Player.mat";
    private const string BootMaterialPath = Root + "/Materials/L14_Boots.mat";
    private const string PlayerPantsMaterialPath = Root + "/Materials/L14_Player_Pants.mat";
    private const string PlayerAccentMaterialPath = Root + "/Materials/L14_Player_Accent.mat";
    private const string SkinMaterialPath = Root + "/Materials/L14_Skin.mat";
    private const string VisorMaterialPath = Root + "/Materials/L14_Visor.mat";
    private const string SkierMaterialPath = Root + "/Materials/L14_Visible_Skier.mat";
    private const string GroomerMaterialPath = Root + "/Materials/L14_Visible_Groomer.mat";
    private const string GroomerCabMaterialPath = Root + "/Materials/L14_Groomer_Cab.mat";
    private const string MetalMaterialPath = Root + "/Materials/L14_Dark_Metal.mat";
    private const string ComputePath = Root + "/Shaders/L14SnowSim.compute";
    private const string SnowBaseMapPath = Root + "/Textures/L14_Snow_BaseColor.asset";
    private const string SnowNormalMapPath = Root + "/Textures/L14_Snow_Normal.asset";
    private const string SnowHeightMapPath = Root + "/Textures/L14_Snow_Height.asset";
    private const string SnowRoughnessMapPath = Root + "/Textures/L14_Snow_Roughness.asset";
    private const string SnowSparkleMaskPath = Root + "/Textures/L14_Snow_SparkleMask.asset";

    [MenuItem("Tools/Snow/Build L14 Interactive Snow Demo")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("L14 snow demo builder cannot rebuild scenes during Play Mode. Stop Play Mode and run the menu again.");
            return;
        }

        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Editor");
        Directory.CreateDirectory(Root + "/Textures");
        AssetDatabase.Refresh();

        SnowTextureSet snowTextures = LoadOrCreateSnowTextures();
        Material snowMaterial = LoadOrCreateSnowMaterial(snowTextures);
        Material playerMaterial = LoadOrCreateLitMaterial(PlayerMaterialPath, "L14_Player", new Color(0.13f, 0.28f, 0.42f, 1f), 0.46f);
        Material bootMaterial = LoadOrCreateLitMaterial(BootMaterialPath, "L14_Boots", new Color(0.035f, 0.037f, 0.04f, 1f), 0.28f);
        Material playerPantsMaterial = LoadOrCreateLitMaterial(PlayerPantsMaterialPath, "L14_Player_Pants", new Color(0.07f, 0.095f, 0.13f, 1f), 0.34f);
        Material playerAccentMaterial = LoadOrCreateLitMaterial(PlayerAccentMaterialPath, "L14_Player_Accent", new Color(0.95f, 0.48f, 0.12f, 1f), 0.38f);
        Material skinMaterial = LoadOrCreateLitMaterial(SkinMaterialPath, "L14_Skin", new Color(0.92f, 0.68f, 0.48f, 1f), 0.24f);
        Material visorMaterial = LoadOrCreateLitMaterial(VisorMaterialPath, "L14_Visor", new Color(0.025f, 0.08f, 0.12f, 1f), 0.82f);
        Material skierMaterial = LoadOrCreateLitMaterial(SkierMaterialPath, "L14_Visible_Skier", new Color(0.92f, 0.28f, 0.12f, 1f), 0.36f);
        Material groomerMaterial = LoadOrCreateLitMaterial(GroomerMaterialPath, "L14_Visible_Groomer", new Color(0.95f, 0.65f, 0.08f, 1f), 0.32f);
        Material groomerCabMaterial = LoadOrCreateLitMaterial(GroomerCabMaterialPath, "L14_Groomer_Cab", new Color(0.1f, 0.2f, 0.26f, 1f), 0.68f);
        Material metalMaterial = LoadOrCreateLitMaterial(MetalMaterialPath, "L14_Dark_Metal", new Color(0.035f, 0.04f, 0.045f, 1f), 0.42f);
        ComputeShader snowCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L14 Interactive Snow";

        ConfigureLighting();

        GameObject snow = new GameObject("GPU Snow Field - Compute Heightfield");
        MeshRenderer snowRenderer = snow.AddComponent<MeshRenderer>();
        snowRenderer.sharedMaterial = snowMaterial;
        snow.AddComponent<MeshFilter>();
        L14SnowField snowField = snow.AddComponent<L14SnowField>();
        snowField.snowMaterial = snowMaterial;
        snowField.snowCompute = snowCompute;
        snowField.fieldSize = 72f;
        snowField.meshResolution = 520;
        snowField.textureResolution = 1024;
        snowField.maxDepression = 0.46f;
        snowField.ridgeHeight = 0.20f;
        snowField.powderNoiseStrength = 0.04f;
        snowField.baseReliefStrength = 0.105f;
        snowField.baseReliefScale = 1.45f;
        snowField.recoverySpeed = 0.006f;
        snowField.ridgeSettleSpeed = 0.018f;
        snowField.maxStampCount = 16;
        snowField.ClearSnowState();

        GameObject player = new GameObject("Player Snow Explorer");
        player.transform.position = new Vector3(0f, 0.95f, 0f);

        L14SnowWalker walker = player.AddComponent<L14SnowWalker>();
        walker.fieldLimit = 43f;
        walker.moveSpeed = 6.6f;
        walker.sprintMultiplier = 1.65f;

        GameObject leftBoot = CreateBoot("Left Boot Stamp", player.transform, -0.34f, bootMaterial);
        GameObject rightBoot = CreateBoot("Right Boot Stamp", player.transform, 0.34f, bootMaterial);
        walker.leftFoot = leftBoot.transform;
        walker.rightFoot = rightBoot.transform;
        CreatePlayerVisual(player, walker, leftBoot.transform, rightBoot.transform, playerMaterial, playerPantsMaterial, playerAccentMaterial, skinMaterial, visorMaterial, bootMaterial);

        CreateSkierInteractor(skierMaterial, playerAccentMaterial, skinMaterial, visorMaterial, bootMaterial, metalMaterial);
        CreateGroomerInteractor(groomerMaterial, groomerCabMaterial, bootMaterial, metalMaterial);

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 180f;
        cameraObject.AddComponent<AudioListener>();
        L14SnowCameraRig cameraRig = cameraObject.AddComponent<L14SnowCameraRig>();
        cameraRig.target = player.transform;
        cameraRig.distance = 30f;
        cameraRig.minDistance = 7f;
        cameraRig.maxDistance = 72f;
        cameraRig.yaw = -20f;
        cameraRig.pitch = 35f;
        Vector3 initialFocus = player.transform.position + Vector3.up * cameraRig.lookHeight;
        Quaternion initialRotation = Quaternion.Euler(cameraRig.pitch, cameraRig.yaw, 0f);
        cameraObject.transform.position = initialFocus + initialRotation * new Vector3(0f, 0f, -cameraRig.distance);
        cameraObject.transform.rotation = Quaternion.LookRotation(initialFocus - cameraObject.transform.position, Vector3.up);

        GameObject hud = new GameObject("Demo HUD");
        L14SnowDemoHud demoHud = hud.AddComponent<L14SnowDemoHud>();
        demoHud.snowField = snowField;
        demoHud.walker = walker;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = snow;
        Debug.Log($"L14 interactive snow demo rebuilt: {ScenePath}");
    }

    private static void ConfigureLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.56f, 0.65f, 0.78f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.35f, 0.43f, 0.52f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.16f, 0.22f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.64f, 0.72f, 0.82f, 1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 38f;
        RenderSettings.fogEndDistance = 125f;

        GameObject sun = new GameObject("Directional Light");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.35f;
        light.color = new Color(1f, 0.94f, 0.84f, 1f);
        sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
        RenderSettings.sun = light;
    }

    private static GameObject CreateBoot(string name, Transform parent, float lateral, Material material)
    {
        GameObject boot = CreatePart(
            name,
            PrimitiveType.Cube,
            parent,
            new Vector3(lateral, -0.84f, 0.08f),
            new Vector3(0.34f, 0.11f, 0.74f),
            Quaternion.identity,
            material);

        L14SnowInteractor interactor = boot.AddComponent<L14SnowInteractor>();
        interactor.radius = 0.82f;
        interactor.strength = 1.08f;
        interactor.ridgeStrength = 0.82f;
        interactor.hardness = 1.55f;
        return boot;
    }

    private static void CreatePlayerVisual(
        GameObject root,
        L14SnowWalker walker,
        Transform leftBoot,
        Transform rightBoot,
        Material coatMaterial,
        Material pantsMaterial,
        Material accentMaterial,
        Material skinMaterial,
        Material visorMaterial,
        Material bootMaterial)
    {
        CreatePart("Explorer Parka", PrimitiveType.Capsule, root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.42f, 0.48f, 0.34f), Quaternion.identity, coatMaterial);
        CreatePart("Fur Collar", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.86f, 0f), new Vector3(0.5f, 0.055f, 0.4f), Quaternion.Euler(90f, 0f, 0f), accentMaterial);
        CreatePart("Explorer Head", PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.13f, 0.02f), new Vector3(0.32f, 0.32f, 0.32f), Quaternion.identity, skinMaterial);
        CreatePart("Snow Helmet", PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.22f, -0.01f), new Vector3(0.36f, 0.23f, 0.34f), Quaternion.identity, coatMaterial);
        CreatePart("Goggle Lens", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.15f, 0.285f), new Vector3(0.46f, 0.12f, 0.035f), Quaternion.identity, visorMaterial);
        CreatePart("Scarf Tail", PrimitiveType.Cube, root.transform, new Vector3(-0.38f, 0.76f, -0.13f), new Vector3(0.13f, 0.44f, 0.08f), Quaternion.Euler(0f, 0f, -18f), accentMaterial);
        CreatePart("Compact Backpack", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.47f, -0.32f), new Vector3(0.5f, 0.52f, 0.18f), Quaternion.identity, pantsMaterial);

        Transform leftLeg = CreatePart("Left Snow Pants Leg", PrimitiveType.Capsule, root.transform, new Vector3(-0.22f, -0.22f, 0f), new Vector3(0.11f, 0.34f, 0.11f), Quaternion.identity, pantsMaterial).transform;
        Transform rightLeg = CreatePart("Right Snow Pants Leg", PrimitiveType.Capsule, root.transform, new Vector3(0.22f, -0.22f, 0f), new Vector3(0.11f, 0.34f, 0.11f), Quaternion.identity, pantsMaterial).transform;
        Transform leftArm = CreatePart("Left Padded Sleeve", PrimitiveType.Capsule, root.transform, new Vector3(-0.49f, 0.46f, 0f), new Vector3(0.075f, 0.3f, 0.075f), Quaternion.identity, coatMaterial).transform;
        Transform rightArm = CreatePart("Right Padded Sleeve", PrimitiveType.Capsule, root.transform, new Vector3(0.49f, 0.46f, 0f), new Vector3(0.075f, 0.3f, 0.075f), Quaternion.identity, coatMaterial).transform;
        CreatePart("Left Glove", PrimitiveType.Sphere, root.transform, new Vector3(-0.53f, 0.22f, 0f), new Vector3(0.14f, 0.14f, 0.14f), Quaternion.identity, bootMaterial);
        CreatePart("Right Glove", PrimitiveType.Sphere, root.transform, new Vector3(0.53f, 0.22f, 0f), new Vector3(0.14f, 0.14f, 0.14f), Quaternion.identity, bootMaterial);

        L14SnowCharacterRig rig = root.AddComponent<L14SnowCharacterRig>();
        rig.walker = walker;
        rig.leftFoot = leftBoot;
        rig.rightFoot = rightBoot;
        rig.leftLeg = leftLeg;
        rig.rightLeg = rightLeg;
        rig.leftArm = leftArm;
        rig.rightArm = rightArm;
    }

    private static void CreateSkierInteractor(Material bodyMaterial, Material accentMaterial, Material skinMaterial, Material visorMaterial, Material skiMaterial, Material poleMaterial)
    {
        GameObject root = new GameObject("Visible Auto Skier - Figure Eight");
        root.transform.position = new Vector3(-8f, 0.12f, -4f);

        CreatePart("Left Carving Ski", PrimitiveType.Cube, root.transform, new Vector3(-0.24f, 0.08f, 0f), new Vector3(0.16f, 0.055f, 2.9f), Quaternion.Euler(0f, 0f, -2f), skiMaterial);
        CreatePart("Right Carving Ski", PrimitiveType.Cube, root.transform, new Vector3(0.24f, 0.08f, 0f), new Vector3(0.16f, 0.055f, 2.9f), Quaternion.Euler(0f, 0f, 2f), skiMaterial);
        CreatePart("Skier Boots", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.23f, 0.04f), new Vector3(0.62f, 0.18f, 0.46f), Quaternion.identity, poleMaterial);
        CreatePart("Skier Jacket", PrimitiveType.Capsule, root.transform, new Vector3(0f, 0.82f, 0f), new Vector3(0.42f, 0.55f, 0.34f), Quaternion.identity, bodyMaterial);
        CreatePart("Skier Helmet", PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.42f, 0.04f), new Vector3(0.26f, 0.26f, 0.26f), Quaternion.identity, accentMaterial);
        CreatePart("Skier Face", PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.36f, 0.15f), new Vector3(0.2f, 0.16f, 0.11f), Quaternion.identity, skinMaterial);
        CreatePart("Skier Goggles", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.39f, 0.245f), new Vector3(0.34f, 0.08f, 0.025f), Quaternion.identity, visorMaterial);
        CreatePart("Left Ski Pole", PrimitiveType.Cylinder, root.transform, new Vector3(-0.62f, 0.66f, 0.08f), new Vector3(0.025f, 0.62f, 0.025f), Quaternion.Euler(18f, 0f, 16f), poleMaterial);
        CreatePart("Right Ski Pole", PrimitiveType.Cylinder, root.transform, new Vector3(0.62f, 0.66f, 0.08f), new Vector3(0.025f, 0.62f, 0.025f), Quaternion.Euler(18f, 0f, -16f), poleMaterial);
        CreatePart("Left Bent Arm", PrimitiveType.Capsule, root.transform, new Vector3(-0.42f, 0.86f, 0.06f), new Vector3(0.065f, 0.34f, 0.065f), Quaternion.Euler(0f, 0f, -36f), bodyMaterial);
        CreatePart("Right Bent Arm", PrimitiveType.Capsule, root.transform, new Vector3(0.42f, 0.86f, 0.06f), new Vector3(0.065f, 0.34f, 0.065f), Quaternion.Euler(0f, 0f, 36f), bodyMaterial);

        L14SnowInteractor interactor = root.AddComponent<L14SnowInteractor>();
        interactor.radius = 1.12f;
        interactor.strength = 0.95f;
        interactor.ridgeStrength = 0.55f;
        interactor.hardness = 1.35f;

        L14SnowAutoInteractor auto = root.AddComponent<L14SnowAutoInteractor>();
        auto.center = root.transform.position;
        auto.pathRadius = 10f;
        auto.pathScale = new Vector2(1f, 0.62f);
        auto.angularSpeed = 0.38f;
        auto.phase = 1.6f;
    }

    private static void CreateGroomerInteractor(Material bodyMaterial, Material cabMaterial, Material trackMaterial, Material metalMaterial)
    {
        GameObject root = new GameObject("Visible Auto Groomer - Wide Track");
        root.transform.position = new Vector3(10f, 0.18f, 8f);

        CreatePart("Wide Compression Plate", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.08f, 0.62f), new Vector3(3.6f, 0.12f, 1.15f), Quaternion.identity, trackMaterial);
        CreatePart("Left Rubber Track", PrimitiveType.Cube, root.transform, new Vector3(-1.16f, 0.24f, -0.08f), new Vector3(0.46f, 0.25f, 1.9f), Quaternion.identity, metalMaterial);
        CreatePart("Right Rubber Track", PrimitiveType.Cube, root.transform, new Vector3(1.16f, 0.24f, -0.08f), new Vector3(0.46f, 0.25f, 1.9f), Quaternion.identity, metalMaterial);
        CreatePart("Snowcat Body", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.63f, -0.08f), new Vector3(2.25f, 0.66f, 1.28f), Quaternion.identity, bodyMaterial);
        CreatePart("Angled Cabin", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.08f, 0.16f), new Vector3(1.32f, 0.54f, 0.84f), Quaternion.Euler(-8f, 0f, 0f), cabMaterial);
        CreatePart("Front Blade", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.42f, 1.28f), new Vector3(2.7f, 0.24f, 0.18f), Quaternion.Euler(12f, 0f, 0f), metalMaterial);
        CreatePart("Roof Beacon", PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.42f, 0.16f), new Vector3(0.18f, 0.1f, 0.18f), Quaternion.identity, bodyMaterial);

        L14SnowInteractor interactor = root.AddComponent<L14SnowInteractor>();
        interactor.radius = 1.9f;
        interactor.strength = 1.35f;
        interactor.ridgeStrength = 0.95f;
        interactor.hardness = 1.05f;

        L14SnowAutoInteractor auto = root.AddComponent<L14SnowAutoInteractor>();
        auto.center = root.transform.position;
        auto.pathRadius = 12f;
        auto.pathScale = new Vector2(1f, 0.56f);
        auto.angularSpeed = -0.2f;
        auto.phase = 1.1f;
    }

    private static GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return part;
    }

    private struct SnowTextureSet
    {
        public Texture2D baseMap;
        public Texture2D normalMap;
        public Texture2D heightMap;
        public Texture2D roughnessMap;
        public Texture2D sparkleMask;
    }

    private static Material LoadOrCreateSnowMaterial(SnowTextureSet textures)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SnowMaterialPath);
        Shader shader = Shader.Find("L14 Snow/GPU Heightfield Snow");
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "L14_GPU_Heightfield_Snow"
            };
            AssetDatabase.CreateAsset(material, SnowMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_BaseColor", new Color(0.86f, 0.92f, 0.96f, 1f));
        material.SetColor("_ShadowColor", new Color(0.42f, 0.55f, 0.72f, 1f));
        material.SetColor("_PackedColor", new Color(0.62f, 0.72f, 0.86f, 1f));
        material.SetColor("_RidgeColor", new Color(1f, 0.98f, 0.9f, 1f));
        material.SetColor("_SubsurfaceColor", new Color(0.72f, 0.9f, 1f, 1f));
        material.SetTexture("_SnowBaseMap", textures.baseMap);
        material.SetTexture("_SnowNormalMap", textures.normalMap);
        material.SetTexture("_SnowHeightMap", textures.heightMap);
        material.SetTexture("_SnowRoughnessMap", textures.roughnessMap);
        material.SetTexture("_SnowSparkleMask", textures.sparkleMask);
        material.SetFloat("_MaxDepression", 0.46f);
        material.SetFloat("_RidgeHeight", 0.20f);
        material.SetFloat("_PowderNoiseStrength", 0.04f);
        material.SetFloat("_BaseReliefStrength", 0.105f);
        material.SetFloat("_BaseReliefScale", 1.45f);
        material.SetFloat("_SnowTextureScale", 1.0f);
        material.SetFloat("_NormalStrength", 0.64f);
        material.SetFloat("_TextureHeightStrength", 0.66f);
        material.SetFloat("_SubsurfaceStrength", 0.48f);
        material.SetFloat("_GlancingSheenStrength", 0.24f);
        material.SetFloat("_CrystalGlintStrength", 0.95f);
        material.SetFloat("_CrystalGlintDensity", 220f);
        material.SetFloat("_CrystalGlintSharpness", 72f);
        material.SetFloat("_Smoothness", 0.62f);
        return material;
    }

    private static SnowTextureSet LoadOrCreateSnowTextures()
    {
        const int textureSize = 512;
        Texture2D heightMap = LoadOrCreateTexture(SnowHeightMapPath, "L14_Snow_Height", textureSize, false, GenerateHeightPixel);
        return new SnowTextureSet
        {
            heightMap = heightMap,
            baseMap = LoadOrCreateTexture(SnowBaseMapPath, "L14_Snow_BaseColor", textureSize, false, GenerateBasePixel),
            normalMap = LoadOrCreateNormalTexture(SnowNormalMapPath, "L14_Snow_Normal", heightMap),
            roughnessMap = LoadOrCreateTexture(SnowRoughnessMapPath, "L14_Snow_Roughness", textureSize, false, GenerateRoughnessPixel),
            sparkleMask = LoadOrCreateSparkleMask(SnowSparkleMaskPath, "L14_Snow_SparkleMask", textureSize)
        };
    }

    private delegate Color TextureGenerator(int x, int y, int size);

    private static Texture2D LoadOrCreateTexture(string path, string name, int size, bool normalMap, TextureGenerator generator)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null || texture.width != size || texture.height != size)
        {
            texture = new Texture2D(size, size, normalMap ? TextureFormat.RGBA32 : TextureFormat.RGBA32, true, true)
            {
                name = name
            };
            AssetDatabase.CreateAsset(texture, path);
        }

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = generator(x, y, size);
            }
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 6;
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Texture2D LoadOrCreateNormalTexture(string path, string name, Texture2D heightMap)
    {
        const int size = 512;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Color[] heightPixels = heightMap.GetPixels();
        Color[] normalPixels = new Color[size * size];
        const float strength = 2.35f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float hL = heightPixels[y * size + ((x - 1 + size) % size)].r;
                float hR = heightPixels[y * size + ((x + 1) % size)].r;
                float hD = heightPixels[((y - 1 + size) % size) * size + x].r;
                float hU = heightPixels[((y + 1) % size) * size + x].r;
                Vector3 normal = new Vector3((hL - hR) * strength, (hD - hU) * strength, 1f).normalized;
                normalPixels[y * size + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
            }
        }

        if (texture == null || texture.width != size || texture.height != size)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = name
            };
            AssetDatabase.CreateAsset(texture, path);
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 6;
        texture.SetPixels(normalPixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Texture2D LoadOrCreateSparkleMask(string path, string name, int size)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        float[] values = new float[size * size];
        var random = new System.Random(1414);
        int pointCount = size * size / 95;
        for (int i = 0; i < pointCount; i++)
        {
            float cx = (float)random.NextDouble() * size;
            float cy = (float)random.NextDouble() * size;
            float radius = Mathf.Lerp(1.2f, 3.6f, (float)random.NextDouble());
            float intensity = Mathf.Lerp(0.35f, 1f, (float)random.NextDouble());
            int minX = Mathf.FloorToInt(cx - radius * 2f);
            int maxX = Mathf.CeilToInt(cx + radius * 2f);
            int minY = Mathf.FloorToInt(cy - radius * 2f);
            int maxY = Mathf.CeilToInt(cy + radius * 2f);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int px = (x + size) % size;
                    int py = (y + size) % size;
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Max(radius, 0.001f);
                    float value = Mathf.Exp(-dist * dist * 1.8f) * intensity;
                    int index = py * size + px;
                    values[index] = Mathf.Max(values[index], value);
                }
            }
        }

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            float v = Mathf.Pow(Mathf.Clamp01(values[i]), 1.45f);
            pixels[i] = new Color(v, v, v, 1f);
        }

        if (texture == null || texture.width != size || texture.height != size)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = name
            };
            AssetDatabase.CreateAsset(texture, path);
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 6;
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Color GenerateBasePixel(int x, int y, int size)
    {
        float u = x / (float)size;
        float v = y / (float)size;
        float broad = FractalTileable(u, v, 2.0f, 17.3f, 3.1f, 4, 0.55f);
        float fine = FractalTileable(u, v, 13.0f, 4.7f, 9.6f, 3, 0.48f);
        float tint = Mathf.Clamp01(0.84f + (broad - 0.5f) * 0.18f + (fine - 0.5f) * 0.055f);
        return new Color(0.92f * tint, 0.97f * tint, 1.0f * tint, 1f);
    }

    private static Color GenerateHeightPixel(int x, int y, int size)
    {
        float u = x / (float)size;
        float v = y / (float)size;
        float wind = FractalTileable(u, v, 2.0f, 2.1f, 8.7f, 5, 0.54f);
        float ripple = Mathf.Sin((u * 3.0f + v * 0.82f + wind * 0.34f) * Mathf.PI * 2f) * 0.5f + 0.5f;
        float crust = FractalTileable(u, v, 9.0f, 11.4f, 1.9f, 4, 0.48f);
        float granular = FractalTileable(u, v, 31.0f, 7.0f, 5.0f, 2, 0.45f);
        float height = Mathf.Clamp01(0.5f + (wind - 0.5f) * 0.30f + (ripple - 0.5f) * 0.12f + (crust - 0.5f) * 0.09f + (granular - 0.5f) * 0.025f);
        return new Color(height, height, height, 1f);
    }

    private static Color GenerateRoughnessPixel(int x, int y, int size)
    {
        float u = x / (float)size;
        float v = y / (float)size;
        float packed = FractalTileable(u, v, 4.0f, 13.1f, 2.5f, 4, 0.5f);
        float crystals = FractalTileable(u, v, 23.0f, 0.7f, 19.4f, 3, 0.52f);
        float roughness = Mathf.Clamp01(0.72f + (packed - 0.5f) * 0.16f - (crystals - 0.5f) * 0.12f);
        return new Color(roughness, roughness, roughness, 1f);
    }

    private static float FractalTileable(float u, float v, float baseFrequency, float offsetX, float offsetY, int octaves, float persistence)
    {
        float sum = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            float tileFrequency = baseFrequency * frequency;
            sum += TileablePerlin(u, v, tileFrequency, offsetX + i * 37.2f, offsetY + i * 19.7f) * amplitude;
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

    private static Material LoadOrCreateLitMaterial(string path, string name, Color color, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (material == null)
        {
            material = new Material(shader)
            {
                name = name
            };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }

}
