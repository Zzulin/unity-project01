using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class LXIIL18AtmosphereSync
{
    private const string TargetScenePath = "Assets/LXII game 整合/game.unity";
    private const string SourceScenePath = "Assets/L18 VolumeCloud+VollumetricLighting/L18.unity";
    private const string L18CloudMaterialPath = "Assets/L18 VolumeCloud+VollumetricLighting/Materials/L18_L13_RaymarchedCloud.mat";
    private const string L18SkyboxMaterialPath = "Assets/L18 VolumeCloud+VollumetricLighting/Materials/L18_SunsetSky.mat";
    private const string RendererDataPath = "Assets/Settings/NPR Render Pipeline Asset_Renderer.asset";
    private static readonly Vector3 InitialSunEuler = new Vector3(0f, 149.8f, 0f);

    private static readonly string[] EffectRootNames =
    {
        "L18 Low Storm Sun",
        "L18 Atmosphere Director",
        "L17 Outdoor Volume Bounds",
        "L13 Integrated Cloud Layer",
        "L18 Cinematic Post Process",
        "Global Volume",
        "L17 Integrated Volumetric Lighting",
    };

    private static readonly string[] TargetCleanupNames =
    {
        "Directional Light",
        "LXII L13 VolumeCloud Root",
        "LXII Sky Volume Cloud",
        "L18 Low Storm Sun",
        "L18 Atmosphere Director",
        "L17 Outdoor Volume Bounds",
        "L13 Integrated Cloud Layer",
        "L18 Cinematic Post Process",
        "Global Volume",
        "L17 Integrated Volumetric Lighting",
    };

    [MenuItem("Tools/LXII/Sync L18 Atmosphere To LXII")]
    public static void SyncL18AtmosphereToLXII()
    {
        EditorSceneManager.SaveOpenScenes();

        Scene targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        CleanupTargetScene(targetScene);

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
        Dictionary<string, GameObject> copiedRoots = CopyEffectRoots(sourceScene, targetScene);
        EditorSceneManager.CloseScene(sourceScene, true);

        RebindCopiedReferences(copiedRoots);
        ConfigureTargetScene(copiedRoots);
        ConfigureMainCamera();

        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = TryGet(copiedRoots, "L18 Atmosphere Director");
        Debug.Log("[LXII] 已从 L18 同步体积云、体积光、动态昼夜色调与后处理到 LXII game.unity。");
    }

    private static void CleanupTargetScene(Scene targetScene)
    {
        foreach (string objectName in TargetCleanupNames)
        {
            GameObject existing;
            while ((existing = FindInScene(targetScene, objectName)) != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }

    private static Dictionary<string, GameObject> CopyEffectRoots(Scene sourceScene, Scene targetScene)
    {
        var copiedRoots = new Dictionary<string, GameObject>();
        foreach (string objectName in EffectRootNames)
        {
            GameObject source = FindInScene(sourceScene, objectName);
            if (source == null)
            {
                Debug.LogWarning($"[LXII] L18 源场景缺少 {objectName}，跳过。");
                continue;
            }

            GameObject copy = Object.Instantiate(source);
            copy.name = source.name;
            SceneManager.MoveGameObjectToScene(copy, targetScene);
            copiedRoots[objectName] = copy;
        }

        return copiedRoots;
    }

    private static void RebindCopiedReferences(Dictionary<string, GameObject> copiedRoots)
    {
        GameObject sunObject = TryGet(copiedRoots, "L18 Low Storm Sun");
        GameObject directorObject = TryGet(copiedRoots, "L18 Atmosphere Director");
        GameObject cloudObject = TryGet(copiedRoots, "L13 Integrated Cloud Layer");
        GameObject volumeObject = TryGet(copiedRoots, "L17 Integrated Volumetric Lighting");
        GameObject boundsObject = TryGet(copiedRoots, "L17 Outdoor Volume Bounds");

        Light sunLight = sunObject != null ? sunObject.GetComponent<Light>() : null;
        L13VolumeCloudController cloudController = cloudObject != null ? cloudObject.GetComponent<L13VolumeCloudController>() : null;
        L17VolumetricLightingController volumetricLighting = volumeObject != null
            ? volumeObject.GetComponent<L17VolumetricLightingController>()
            : null;
        L18AtmosphereDirector director = directorObject != null ? directorObject.GetComponent<L18AtmosphereDirector>() : null;

        Material cloudMaterial = AssetDatabase.LoadAssetAtPath<Material>(L18CloudMaterialPath);
        Material skyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(L18SkyboxMaterialPath);
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(InitialSunEuler);
            RenderSettings.sun = sunLight;
            EditorUtility.SetDirty(sunLight);
            EditorUtility.SetDirty(sunLight.transform);
        }

        if (cloudController != null)
        {
            cloudController.sunLight = sunLight;
            if (cloudMaterial != null)
            {
                cloudController.cloudMaterial = cloudMaterial;
            }

            MeshRenderer cloudRenderer = cloudController.GetComponent<MeshRenderer>();
            if (cloudRenderer != null && cloudMaterial != null)
            {
                cloudRenderer.sharedMaterial = cloudMaterial;
                cloudRenderer.shadowCastingMode = ShadowCastingMode.Off;
                cloudRenderer.receiveShadows = false;
                EditorUtility.SetDirty(cloudRenderer);
            }

            cloudController.RefreshImmediate(false);
            EditorUtility.SetDirty(cloudController);
        }

        if (volumetricLighting != null)
        {
            volumetricLighting.sunLight = sunLight;
            volumetricLighting.volumeBoundsTransform = boundsObject != null ? boundsObject.transform : null;
            if (rendererData != null)
            {
                volumetricLighting.rendererData = rendererData;
            }

            volumetricLighting.RefreshImmediate();
            EditorUtility.SetDirty(volumetricLighting);
        }

        if (director != null)
        {
            director.sunLight = sunLight;
            director.cloudController = cloudController;
            director.volumetricLighting = volumetricLighting;
            if (skyboxMaterial != null)
            {
                director.skyboxMaterial = skyboxMaterial;
            }

            director.RefreshImmediate();
            EditorUtility.SetDirty(director);
        }

        if (sunLight != null)
        {
            RenderSettings.sun = sunLight;
            EditorUtility.SetDirty(sunLight);
        }
    }

    private static void ConfigureTargetScene(Dictionary<string, GameObject> copiedRoots)
    {
        Material skyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(L18SkyboxMaterialPath);
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;

        L18AtmosphereDirector director = TryGet(copiedRoots, "L18 Atmosphere Director")?.GetComponent<L18AtmosphereDirector>();
        if (director != null)
        {
            director.RefreshImmediate();
        }
    }

    private static void ConfigureMainCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject mainCamera = GameObject.Find("Main Camera");
            camera = mainCamera != null ? mainCamera.GetComponent<Camera>() : null;
        }

        if (camera == null)
        {
            return;
        }

        camera.clearFlags = CameraClearFlags.Skybox;
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 3500f);

        UniversalAdditionalCameraData additionalData = camera.GetComponent<UniversalAdditionalCameraData>();
        if (additionalData != null)
        {
            additionalData.renderPostProcessing = true;
            EditorUtility.SetDirty(additionalData);
        }

        EditorUtility.SetDirty(camera);
    }

    private static GameObject TryGet(Dictionary<string, GameObject> roots, string name)
    {
        return roots.TryGetValue(name, out GameObject result) ? result : null;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject result = FindRecursive(root.transform, objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static GameObject FindRecursive(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current.gameObject;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            GameObject result = FindRecursive(current.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
