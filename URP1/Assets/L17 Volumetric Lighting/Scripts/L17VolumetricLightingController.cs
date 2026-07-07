using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
// L17 场景级体积光控制器，负责把参数交给 RendererFeature。
public sealed class L17VolumetricLightingController : MonoBehaviour
{
    // RendererFeature 绑定。
    public UniversalRendererData rendererData;

    // Froxel 网格分辨率与深度分布。
    [Range(1, 4)] public int downsample = 2;
    [Range(16, 128)] public int froxelDepth = 96;
    [Min(4f)] public float maxDistance = 58f;
    [Range(0.5f, 3f)] public float depthDistribution = 1.9f;

    // 参与介质散射与局部体积范围。
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

    // 时域稳定、降噪与最终合成。
    public bool temporalAccumulation = true;
    [Range(0f, 1f)] public float jitterStrength = 0.9f;
    [Range(0f, 0.95f)] public float temporalBlend = 0.8f;
    [Range(0.001f, 0.5f)] public float temporalDepthRejection = 0.12f;
    [Range(0f, 0.3f)] public float bilateralDepthScale = 0.08f;
    [Range(0f, 1f)] public float compositeOpacity = 0.94f;

    // 主光参数。
    public Light sunLight;
    [Range(0f, 5f)] public float sunIntensity = 2.35f;
    public Color sunColor = new Color(1f, 0.93f, 0.78f, 1f);

    // TriLight 环境反弹光颜色。
    public Color ambientSky = new Color(0.08f, 0.082f, 0.09f, 1f);
    public Color ambientEquator = new Color(0.045f, 0.04f, 0.034f, 1f);
    public Color ambientGround = new Color(0.018f, 0.015f, 0.012f, 1f);

    // 运行时缓存与变化追踪。
    private L17FrustumVolumetricRendererFeature cachedFeature;
    private L13VolumeCloudController cachedCloudOccluder;
    private bool hasApplied;
    private Vector3 lastVolumeBoundsPosition;
    private Vector3 lastVolumeBoundsScale;

    // 注册当前场景控制器，并应用初始参数。
    private void OnEnable()
    {
        L17FrustumVolumetricRendererFeature.RegisterSceneController(this);
        Apply(false);
    }

    // 从场景控制器注册表中移除。
    private void OnDisable()
    {
        L17FrustumVolumetricRendererFeature.UnregisterSceneController(this);
    }

    // Inspector 修改后重新应用参数。
    private void OnValidate()
    {
        Apply(true);
    }

    // 只在体积盒 Transform 变化时刷新缓存。
    private void Update()
    {
        if (NeedsRuntimeApply())
        {
            Apply(false);
        }
    }

    // 供外部系统强制立即刷新参数。
    public void RefreshImmediate()
    {
        Apply(true);
    }

    // 应用场景光照、RendererFeature 状态和体积范围。
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

        settings.passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
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

    // 从指定 RendererData 中查找并缓存 L17 RendererFeature。
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

    // 判断运行时体积范围是否需要重新应用。
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

    // 记录最新体积范围快照。
    private void RememberVolumeBounds(Vector3 boundsCenter, Vector3 boundsSize)
    {
        lastVolumeBoundsPosition = boundsCenter;
        lastVolumeBoundsScale = boundsSize;
    }

    // 优先返回实时 Transform 范围，否则返回序列化范围。
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

    // 查找同场景启用的 L13 云，用于体积光云影。
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
    // 按名称查找序列化字段。
    private SerializedProperty Property(string name)
    {
        return serializedObject.FindProperty(name);
    }

    // 绘制简洁的 Inspector 分组标题。
    private static void Header(string label)
    {
        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    // 绘制幂曲线滑条，提高低值区调参精度。
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

    // 绘制分组后的 L17 控制器 Inspector。
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
