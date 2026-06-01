using System.IO;
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
    private const string FloorMaterialPath = Root + "/Materials/L17_DustyFloor.mat";
    private const string FrameMaterialPath = Root + "/Materials/L17_WindowFrame.mat";

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
        Directory.CreateDirectory(Root + "/Shaders");
        Directory.CreateDirectory(Root + "/Docs");
        AssetDatabase.Refresh();

        Material beamMaterial = LoadOrCreateBeamMaterial();
        Material wallMaterial = LoadOrCreateInteriorMaterial(WallMaterialPath, "L17_RoomWall", new Color(0.79f, 0.73f, 0.66f, 1f), new Color(0.62f, 0.54f, 0.44f, 1f), 0.16f, 0.14f, 0.38f, 1.45f);
        Material floorMaterial = LoadOrCreateInteriorMaterial(FloorMaterialPath, "L17_DustyFloor", new Color(0.5f, 0.4f, 0.3f, 1f), new Color(0.37f, 0.29f, 0.21f, 1f), 0.12f, 0.08f, 0.28f, 1.2f);
        Material frameMaterial = LoadOrCreateInteriorMaterial(FrameMaterialPath, "L17_WindowFrame", new Color(0.27f, 0.23f, 0.18f, 1f), new Color(0.18f, 0.15f, 0.11f, 1f), 0.28f, 0.28f, 0.24f, 1.05f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "L17 Modern Window Shafts";

        ConfigureRenderSettings();
        CreateSun();
        CreateCamera();

        CreateCube("Room Floor", new Vector3(0f, -0.2f, 0f), new Vector3(16f, 0.4f, 16f), floorMaterial, true, true);
        CreateCube("Room Ceiling", new Vector3(0f, 6.6f, 0f), new Vector3(16f, 0.4f, 16f), wallMaterial, true, true);
        CreateCube("Room Wall Left", new Vector3(-8f, 3.2f, 0f), new Vector3(0.4f, 6.8f, 16f), wallMaterial, true, true);
        CreateCube("Room Wall Right", new Vector3(8f, 3.2f, 0f), new Vector3(0.4f, 6.8f, 16f), wallMaterial, true, true);
        CreateCube("Room Back Wall", new Vector3(0f, 3.2f, -8f), new Vector3(16f, 6.8f, 0.4f), wallMaterial, true, true);

        CreateCube("Window Wall Left Fill", new Vector3(-5.6f, 3.2f, 8f), new Vector3(4.8f, 6.8f, 0.4f), wallMaterial, true, true);
        CreateCube("Window Wall Right Fill", new Vector3(5.6f, 3.2f, 8f), new Vector3(4.8f, 6.8f, 0.4f), wallMaterial, true, true);
        CreateCube("Window Wall Lower Fill", new Vector3(0f, 0.9f, 8f), new Vector3(6.4f, 1.8f, 0.4f), wallMaterial, true, true);
        CreateCube("Window Wall Upper Fill", new Vector3(0f, 5.75f, 8f), new Vector3(6.4f, 1.9f, 0.4f), wallMaterial, true, true);
        CreateCube("Window Center Mullion", new Vector3(0f, 3.3f, 8f), new Vector3(0.55f, 4.7f, 0.45f), frameMaterial, true, true);
        CreateCube("Window Left Transom", new Vector3(-2.25f, 3.5f, 8f), new Vector3(3.25f, 0.24f, 0.45f), frameMaterial, true, true);
        CreateCube("Window Right Transom", new Vector3(2.25f, 3.5f, 8f), new Vector3(3.25f, 0.24f, 0.45f), frameMaterial, true, true);

        CreateCube("Sun Catcher Plinth", new Vector3(0f, 0.8f, -0.45f), new Vector3(1.7f, 1.6f, 1.7f), frameMaterial, true, true);
        CreateCube("Volumetric Beam Volume", new Vector3(0f, 2.8f, 2.2f), new Vector3(10.5f, 4.8f, 9.5f), beamMaterial, false, false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"L17 modern window shafts demo rebuilt: {ScenePath}");
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.46f, 0.42f, 0.36f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.26f, 0.22f, 0.18f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.13f, 0.10f, 0.08f, 1f);
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

    private static void CreateSun()
    {
        GameObject sun = new GameObject("Window Sun");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2.35f;
        light.color = new Color(1f, 0.94f, 0.84f, 1f);
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 1f;
        light.shadowBias = 0.035f;
        light.shadowNormalBias = 0.3f;
        sun.transform.rotation = new Quaternion(0f, 0.93358f, -0.358368f, 0f);
        RenderSettings.sun = light;
    }

    private static Material LoadOrCreateBeamMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(BeamMaterialPath);
        Shader shader = Shader.Find("L17 Volumetric Lighting/Window Beam");
        if (material == null)
        {
            material = new Material(shader) { name = "L17_WindowBeam" };
            AssetDatabase.CreateAsset(material, BeamMaterialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        material.SetColor("_BeamColor", new Color(1f, 0.93f, 0.72f, 1f));
        material.SetColor("_ShadowColor", new Color(0.5f, 0.43f, 0.31f, 1f));
        material.SetFloat("_Density", 2.85f);
        material.SetFloat("_Extinction", 1.18f);
        material.SetFloat("_Intensity", 8.4f);
        material.SetFloat("_Opacity", 1f);
        material.SetFloat("_Anisotropy", 0.7f);
        material.SetFloat("_NoiseScale", 1.28f);
        material.SetFloat("_NoiseStrength", 0.32f);
        material.SetVector("_WindDirection", new Vector4(0.58f, 0f, -0.18f, 0f));
        material.SetFloat("_WindSpeed", 0.36f);
        material.SetFloat("_EdgeFade", 0.075f);
        material.SetFloat("_AxialFade", 0.09f);
        material.SetFloat("_ShadowContrast", 0.9f);
        material.SetFloat("_ShadowFloor", 0.22f);
        material.SetFloat("_LightBoost", 1.8f);
        material.SetFloat("_StepCount", 56f);
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

    private static void CreateCube(string name, Vector3 position, Vector3 scale, Material material, bool castShadows, bool receiveShadows)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
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
    }
}
