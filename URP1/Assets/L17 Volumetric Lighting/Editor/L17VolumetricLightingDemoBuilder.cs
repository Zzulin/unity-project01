using System;
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
    private const string BeamMaterialPath = Root + "/Materials/L17_WindowBeam.mat";
    private const string WallMaterialPath = Root + "/Materials/L17_RoomWall.mat";
    private const float BeamWidthPad = 1.42f;
    private const float BeamHeightPad = 1.26f;
    private const float UpperBeamDepth = 8.9f;
    private const float LowerBeamDepth = 9.3f;
    private static readonly Vector3 BeamDirection = new Vector3(0f, -0.35f, -1f).normalized;

    [MenuItem("Tools/Volumetric Lighting/Build L17 Modern Window Shafts Demo")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("L17 volumetric lighting demo builder cannot rebuild during Play Mode.");
            return;
        }

        Directory.CreateDirectory(Root + "/Editor");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Scripts");
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Docs");
        AssetDatabase.Refresh();

        float upperBeamWidth = 2.72f * BeamWidthPad;
        float upperBeamHeight = 1.06f * BeamHeightPad;
        float lowerBeamWidth = 2.72f * BeamWidthPad;
        float lowerBeamHeight = 1.46f * BeamHeightPad;

        Material beamMaterial = LoadOrCreateBeamMaterial(BeamMaterialPath, "L17_WindowBeam");
        Material wallMaterial = LoadOrCreateInteriorMaterial(
            WallMaterialPath,
            "L17_RoomWall",
            new Color(0.72f, 0.68f, 0.6f, 1f),
            new Color(0.18f, 0.16f, 0.14f, 1f),
            0.12f,
            0.06f,
            0.03f,
            0.38f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L17 Modern Window Shafts";

        ConfigureRenderSettings();
        Light sunLight = CreateSun();
        CreateCamera();

        CreateCube("Room Floor", new Vector3(0f, -0.2f, 0f), new Vector3(16f, 0.4f, 16f), wallMaterial, true, true);
        CreateCube("Room Ceiling", new Vector3(0f, 6.6f, 0f), new Vector3(16f, 0.4f, 16f), wallMaterial, true, true);
        CreateCube("Room Wall Left", new Vector3(-8f, 3.2f, 0f), new Vector3(0.4f, 6.8f, 16f), wallMaterial, true, true);
        CreateCube("Room Wall Right", new Vector3(8f, 3.2f, 0f), new Vector3(0.4f, 6.8f, 16f), wallMaterial, true, true);
        CreateCube("Room Back Wall", new Vector3(0f, 3.2f, -8f), new Vector3(16f, 6.8f, 0.4f), wallMaterial, true, true);

        CreateCube("Window Wall Left Fill", new Vector3(-5.6f, 3.2f, 8f), new Vector3(4.8f, 6.8f, 0.4f), wallMaterial, true, true);
        CreateCube("Window Wall Right Fill", new Vector3(5.6f, 3.2f, 8f), new Vector3(4.8f, 6.8f, 0.4f), wallMaterial, true, true);
        CreateCube("Window Wall Lower Fill", new Vector3(0f, 0.9f, 8f), new Vector3(6.4f, 1.8f, 0.4f), wallMaterial, true, true);
        CreateCube("Window Wall Upper Fill", new Vector3(0f, 5.75f, 8f), new Vector3(6.4f, 1.9f, 0.4f), wallMaterial, true, true);
        CreateCube("Window Center Mullion", new Vector3(0f, 3.3f, 8f), new Vector3(0.55f, 4.7f, 0.45f), wallMaterial, true, true);
        CreateCube("Window Left Transom", new Vector3(-1.7375f, 3.5f, 8f), new Vector3(2.925f, 0.24f, 0.45f), wallMaterial, true, true);
        CreateCube("Window Right Transom", new Vector3(1.7375f, 3.5f, 8f), new Vector3(2.925f, 0.24f, 0.45f), wallMaterial, true, true);

        CreateCube("Sun Catcher Plinth", new Vector3(3.15f, 0.8f, -1.2f), new Vector3(1.7f, 1.6f, 1.7f), wallMaterial, false, false);
        CreateBeam("Volumetric Beam Left Upper", new Vector3(-1.7375f, 4.21f, 7.78f), upperBeamWidth, upperBeamHeight, UpperBeamDepth, beamMaterial);
        CreateBeam("Volumetric Beam Right Upper", new Vector3(1.7375f, 4.21f, 7.78f), upperBeamWidth, upperBeamHeight, UpperBeamDepth, beamMaterial);
        CreateBeam("Volumetric Beam Left Lower", new Vector3(-1.7375f, 2.59f, 7.78f), lowerBeamWidth, lowerBeamHeight, LowerBeamDepth, beamMaterial);
        CreateBeam("Volumetric Beam Right Lower", new Vector3(1.7375f, 2.59f, 7.78f), lowerBeamWidth, lowerBeamHeight, LowerBeamDepth, beamMaterial);
        CreateLightingController(beamMaterial, wallMaterial, sunLight);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"L17 modern window shafts demo rebuilt: {ScenePath}");
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.12f, 0.11f, 0.1f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.06f, 0.055f, 0.05f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.025f, 0.02f, 0.018f, 1f);
        RenderSettings.fog = false;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.07f, 0.08f, 1f);
        camera.fieldOfView = 43f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 120f;
        camera.allowHDR = true;
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.SetPositionAndRotation(
            new Vector3(-2.2f, 2.45f, -6.8f),
            new Quaternion(-0.015484f, 0.102866f, 0.001601f, 0.994573f));
    }

    private static Light CreateSun()
    {
        GameObject sun = new GameObject("Window Sun");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.6f;
        light.color = new Color(1f, 0.95f, 0.86f, 1f);
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 1f;
        light.shadowBias = 0.035f;
        light.shadowNormalBias = 0.3f;
        sun.transform.rotation = Quaternion.LookRotation(BeamDirection, Vector3.up);
        RenderSettings.sun = light;
        return light;
    }

    private static Material LoadOrCreateBeamMaterial(string path, string materialName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("L17 Volumetric Lighting/Window Beam");
        if (material == null)
        {
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_BeamColor", new Color(1f, 0.93f, 0.72f, 1f));
        material.SetColor("_ShadowColor", new Color(0.5f, 0.43f, 0.31f, 1f));
        material.SetFloat("_Density", 2.2f);
        material.SetFloat("_Extinction", 1.06f);
        material.SetFloat("_Intensity", 7.2f);
        material.SetFloat("_Opacity", 1f);
        material.SetFloat("_Anisotropy", 0.7f);
        material.SetFloat("_NoiseScale", 1.28f);
        material.SetFloat("_NoiseStrength", 0.18f);
        material.SetVector("_WindDirection", new Vector4(0.58f, 0f, -0.18f, 0f));
        material.SetFloat("_WindSpeed", 0.36f);
        material.SetFloat("_EdgeFade", 0.11f);
        material.SetFloat("_AxialFade", 0.1f);
        material.SetFloat("_ShadowContrast", 1.15f);
        material.SetFloat("_ShadowFloor", 0.14f);
        material.SetFloat("_LightBoost", 1.22f);
        material.SetFloat("_StepCount", 72f);
        return material;
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
        return material;
    }

    private static void CreateBeam(string name, Vector3 windowCenter, float width, float height, float depth, Material material)
    {
        Vector3 center = windowCenter + BeamDirection * (depth * 0.5f);
        Vector3 rotationEuler = Quaternion.LookRotation(BeamDirection, Vector3.up).eulerAngles;
        CreateCube(name, center, new Vector3(width, height, depth), rotationEuler, material, false, false);
    }

    private static void CreateLightingController(Material beamMaterial, Material wallMaterial, Light sunLight)
    {
        GameObject controllerObject = new GameObject("L17 Lighting Controller");
        Type controllerType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => type.Name == "L17VolumetricLightingController");

        if (controllerType == null)
        {
            Debug.LogWarning("L17VolumetricLightingController type was not found. Reimport scripts and rebuild the scene.");
            return;
        }

        Component controller = controllerObject.AddComponent(controllerType);
        SetControllerField(controller, "beamMaterial", beamMaterial);
        SetControllerField(controller, "wallMaterial", wallMaterial);
        SetControllerField(controller, "sunLight", sunLight);
        SetControllerField(controller, "stepCount", 24);
        SetControllerField(controller, "opacity", 1f);
        SetControllerField(controller, "intensity", 7.2f);
        SetControllerField(controller, "shadowContrast", 1.15f);
        SetControllerField(controller, "wallAmbientBoost", 0.55f);
        SetControllerField(controller, "ambientSky", new Color(0.16f, 0.145f, 0.13f, 1f));
        SetControllerField(controller, "ambientEquator", new Color(0.09f, 0.082f, 0.074f, 1f));
        SetControllerField(controller, "ambientGround", new Color(0.04f, 0.032f, 0.028f, 1f));

        controllerType.GetMethod("RefreshImmediate")?.Invoke(controller, null);
    }

    private static void SetControllerField(Component controller, string fieldName, object value)
    {
        controller.GetType().GetField(fieldName)?.SetValue(controller, value);
    }

    private static void CreateCube(string name, Vector3 position, Vector3 scale, Material material, bool castShadows, bool receiveShadows)
    {
        CreateCube(name, position, scale, Vector3.zero, material, castShadows, receiveShadows);
    }

    private static void CreateCube(string name, Vector3 position, Vector3 scale, Vector3 rotationEuler, Material material, bool castShadows, bool receiveShadows)
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
            UnityEngine.Object.DestroyImmediate(collider);
        }
    }
}
