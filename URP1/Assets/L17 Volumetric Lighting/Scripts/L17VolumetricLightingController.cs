using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class L17VolumetricLightingController : MonoBehaviour
{
    public UniversalRendererData rendererData;

    [Range(1, 4)] public int downsample = 2;
    [Range(16, 128)] public int froxelDepth = 96;
    [Min(4f)] public float maxDistance = 58f;
    [Range(0.5f, 3f)] public float depthDistribution = 1.9f;

    [Min(0f)] public float density = 0.24f;
    [Range(0.01f, 1.5f)] public float extinction = 0.68f;
    [Range(0f, 30f)] public float intensity = 3.25f;
    [Range(0f, 0.92f)] public float anisotropy = 0.78f;
    [Tooltip("Multiplier of the isotropic phase used as the maximum forward-scattering peak.")]
    [Range(1f, 3.5f)] public float forwardPhaseCeiling = 3.5f;
    [Range(0f, 0.08f)] public float shadowFloor = 0.015f;
    [Range(0f, 0.6f)] public float multiScatter = 0.32f;
    [Range(-50f, 150f)] public float heightOrigin = -0.4f;
    [Min(0.0001f)] public float heightFalloff = 0.22f;
    [Range(0f, 1f)] public float noiseStrength = 0f;
    [Min(0.001f)] public float noiseScale = 1.25f;
    public Transform volumeBoundsTransform;
    public Vector3 volumeBoundsCenter = new Vector3(0f, 3.1f, -0.1f);
    public Vector3 volumeBoundsSize = new Vector3(15.8f, 6.2f, 16.2f);
    [Min(0.01f)] public float volumeBoundsSoftness = 0.45f;
    public Color scatteringColor = new Color(1f, 0.84f, 0.52f, 1f);

    public bool temporalAccumulation = true;
    [Range(0f, 1f)] public float jitterStrength = 0.9f;
    [Range(0f, 0.95f)] public float temporalBlend = 0.8f;
    [Range(0.001f, 0.5f)] public float temporalDepthRejection = 0.12f;
    [Range(0f, 0.3f)] public float bilateralDepthScale = 0.08f;
    [Range(0f, 1f)] public float compositeOpacity = 0.94f;

    public Light sunLight;
    [Range(0f, 5f)] public float sunIntensity = 2.35f;
    public Color sunColor = new Color(1f, 0.93f, 0.78f, 1f);

    public Color ambientSky = new Color(0.08f, 0.082f, 0.09f, 1f);
    public Color ambientEquator = new Color(0.045f, 0.04f, 0.034f, 1f);
    public Color ambientGround = new Color(0.018f, 0.015f, 0.012f, 1f);

    private L17FrustumVolumetricRendererFeature cachedFeature;
    private L13VolumeCloudController cachedCloudOccluder;
    private bool hasApplied;
    private Vector3 lastVolumeBoundsPosition;
    private Vector3 lastVolumeBoundsScale;

    private void OnEnable()
    {
        L17FrustumVolumetricRendererFeature.RegisterSceneController(this);
        Apply(false);
    }

    private void OnDisable()
    {
        L17FrustumVolumetricRendererFeature.UnregisterSceneController(this);
    }

    private void OnValidate()
    {
        Apply(true);
    }

    private void Update()
    {
        if (NeedsRuntimeApply())
        {
            Apply(false);
        }
    }

    public void RefreshImmediate()
    {
        Apply(true);
    }

    private void Apply(bool persistAssets)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;

        if (sunLight != null)
        {
            sunLight.intensity = sunIntensity;
            sunLight.color = sunColor;
            sunLight.shadows = LightShadows.Soft;
        }

        L17FrustumVolumetricRendererFeature feature = GetFeature();
        if (feature == null)
        {
            return;
        }

        L17FrustumVolumetricRendererFeature.Settings settings = feature.settings;
        settings.enabled = true;
        settings.requireSceneController = true;
        Vector3 boundsCenter = volumeBoundsCenter;
        Vector3 boundsSize = volumeBoundsSize;
        if (volumeBoundsTransform != null)
        {
            boundsCenter = volumeBoundsTransform.position;
            boundsSize = volumeBoundsTransform.lossyScale;
            volumeBoundsCenter = boundsCenter;
            volumeBoundsSize = boundsSize;
        }

        settings.passEvent = RenderPassEvent.BeforeRenderingTransparents;
        RememberVolumeBounds(boundsCenter, boundsSize);
        hasApplied = true;

#if UNITY_EDITOR
        if (persistAssets && !Application.isPlaying)
        {
            EditorUtility.SetDirty(feature);
            if (rendererData != null)
            {
                EditorUtility.SetDirty(rendererData);
            }
        }
#endif
    }

    private L17FrustumVolumetricRendererFeature GetFeature()
    {
        if (cachedFeature != null)
        {
            return cachedFeature;
        }

        if (rendererData == null)
        {
            return null;
        }

        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature is L17FrustumVolumetricRendererFeature froxelFeature)
            {
                cachedFeature = froxelFeature;
                return cachedFeature;
            }
        }

        return null;
    }

    private bool NeedsRuntimeApply()
    {
        if (!hasApplied)
        {
            return true;
        }

        if (volumeBoundsTransform == null)
        {
            return false;
        }

        Vector3 currentPosition = volumeBoundsTransform.position;
        Vector3 currentScale = volumeBoundsTransform.lossyScale;
        return (currentPosition - lastVolumeBoundsPosition).sqrMagnitude > 0.000001f
            || (currentScale - lastVolumeBoundsScale).sqrMagnitude > 0.000001f;
    }

    private void RememberVolumeBounds(Vector3 boundsCenter, Vector3 boundsSize)
    {
        lastVolumeBoundsPosition = boundsCenter;
        lastVolumeBoundsScale = boundsSize;
    }

    public void GetVolumeBounds(out Vector3 center, out Vector3 size)
    {
        if (volumeBoundsTransform != null)
        {
            center = volumeBoundsTransform.position;
            size = volumeBoundsTransform.lossyScale;
            return;
        }

        center = volumeBoundsCenter;
        size = volumeBoundsSize;
    }

    public L13VolumeCloudController GetCloudOccluder()
    {
        if (cachedCloudOccluder != null
            && cachedCloudOccluder.isActiveAndEnabled
            && cachedCloudOccluder.gameObject.scene == gameObject.scene)
        {
            return cachedCloudOccluder;
        }

        cachedCloudOccluder = null;
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            L13VolumeCloudController cloud = root.GetComponentInChildren<L13VolumeCloudController>(false);
            if (cloud != null && cloud.isActiveAndEnabled)
            {
                cachedCloudOccluder = cloud;
                break;
            }
        }

        return cachedCloudOccluder;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(L17VolumetricLightingController))]
public sealed class L17VolumetricLightingControllerEditor : Editor
{
    private SerializedProperty Property(string name)
    {
        return serializedObject.FindProperty(name);
    }

    private static void Header(string label)
    {
        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private static void PowerSlider(
        SerializedProperty property,
        GUIContent label,
        float maximum,
        float power)
    {
        float value = Mathf.Clamp(property.floatValue, 0f, maximum);
        float normalized = Mathf.Pow(value / maximum, 1f / power);

        EditorGUILayout.BeginHorizontal();
        float nextNormalized = EditorGUILayout.Slider(label, normalized, 0f, 1f);
        float nextValue = Mathf.Pow(nextNormalized, power) * maximum;
        nextValue = EditorGUILayout.FloatField(nextValue, GUILayout.Width(72f));
        EditorGUILayout.EndHorizontal();

        property.floatValue = Mathf.Clamp(nextValue, 0f, maximum);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(Property("m_Script"));
        }

        Header("Renderer Feature");
        EditorGUILayout.PropertyField(Property("rendererData"));

        Header("Froxel Grid");
        EditorGUILayout.PropertyField(Property("downsample"));
        EditorGUILayout.PropertyField(Property("froxelDepth"));
        PowerSlider(Property("maxDistance"), new GUIContent("Max Distance"), 2000f, 2.2f);
        EditorGUILayout.PropertyField(Property("depthDistribution"));

        Header("Medium");
        PowerSlider(Property("density"), new GUIContent("Density"), 0.4f, 3f);
        EditorGUILayout.PropertyField(Property("extinction"));
        EditorGUILayout.PropertyField(Property("intensity"));
        EditorGUILayout.PropertyField(Property("anisotropy"));
        EditorGUILayout.PropertyField(Property("forwardPhaseCeiling"));
        EditorGUILayout.PropertyField(Property("shadowFloor"));
        EditorGUILayout.PropertyField(Property("multiScatter"));
        EditorGUILayout.PropertyField(Property("heightOrigin"));
        PowerSlider(Property("heightFalloff"), new GUIContent("Height Falloff"), 0.5f, 3f);
        EditorGUILayout.PropertyField(Property("noiseStrength"));
        PowerSlider(Property("noiseScale"), new GUIContent("Noise Scale"), 2f, 2.2f);
        EditorGUILayout.PropertyField(Property("volumeBoundsTransform"));
        EditorGUILayout.PropertyField(Property("volumeBoundsCenter"));
        EditorGUILayout.PropertyField(Property("volumeBoundsSize"));
        PowerSlider(Property("volumeBoundsSoftness"), new GUIContent("Volume Bounds Softness"), 100f, 2.2f);
        EditorGUILayout.PropertyField(Property("scatteringColor"));

        Header("Stability");
        EditorGUILayout.PropertyField(Property("temporalAccumulation"));
        EditorGUILayout.PropertyField(Property("jitterStrength"));
        EditorGUILayout.PropertyField(Property("temporalBlend"));
        EditorGUILayout.PropertyField(Property("temporalDepthRejection"));
        EditorGUILayout.PropertyField(Property("bilateralDepthScale"));
        EditorGUILayout.PropertyField(Property("compositeOpacity"));

        Header("Sun");
        EditorGUILayout.PropertyField(Property("sunLight"));
        EditorGUILayout.PropertyField(Property("sunIntensity"));
        EditorGUILayout.PropertyField(Property("sunColor"));

        Header("Room Bounce");
        EditorGUILayout.PropertyField(Property("ambientSky"));
        EditorGUILayout.PropertyField(Property("ambientEquator"));
        EditorGUILayout.PropertyField(Property("ambientGround"));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
