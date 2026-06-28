using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class L18AtmosphereDirector : MonoBehaviour
{
    [System.Serializable]
    public struct AtmosphereProfile
    {
        [Header("Sun")]
        public Color sunColor;
        [Min(0f)] public float sunIntensity;
        [Range(0f, 1f)] public float shadowStrength;

        [Header("Procedural Skybox")]
        public Color skyTint;
        public Color groundColor;
        [Range(0f, 5f)] public float atmosphereThickness;
        [Range(0f, 8f)] public float exposure;
        [Range(0f, 1f)] public float sunSize;
        [Range(1f, 20f)] public float sunSizeConvergence;

        [Header("Environment")]
        public Color ambientSky;
        public Color ambientEquator;
        public Color ambientGround;
        public Color fogColor;

        [Header("L13 Cloud")]
        public Color cloudColor;
        public Color cloudShadowColor;
        public Color cloudAmbientColor;
        [Range(0f, 12f)] public float cloudDensity;
        [Range(0f, 1f)] public float cloudCoverage;
        [Range(0f, 1f)] public float weatherStrength;
        [Range(0f, 1f)] public float macroGapStrength;
        [Range(0.05f, 8f)] public float shapeScale;
        [Range(0.25f, 24f)] public float detailScale;
        [Range(0f, 1f)] public float detailStrength;
        [Range(0.01f, 0.45f)] public float bottomSoftness;
        [Range(0.01f, 0.45f)] public float topSoftness;
        [Range(0f, 1f)] public float anvilBias;
        [Range(0.2f, 8f)] public float absorption;
        [Range(0.2f, 8f)] public float lightAbsorption;
        [Range(0f, 0.85f)] public float forwardPhase;
        [Range(-0.65f, 0f)] public float backwardPhase;
        [Range(0f, 4f)] public float silverIntensity;
        [Range(0f, 3f)] public float powderStrength;
        [Range(0f, 1f)] public float cloudOpacity;

        [Header("L17 Volumetric Lighting")]
        public Color scatteringColor;
        [Min(4f)] public float maxDistance;
        [Min(0f)] public float volumeDensity;
        [Range(0.01f, 1.5f)] public float extinction;
        [Range(0f, 30f)] public float intensity;
        [Range(0f, 0.92f)] public float anisotropy;
        [Range(1f, 3.5f)] public float forwardPhaseCeiling;
        [Range(0f, 0.08f)] public float shadowFloor;
        [Range(0f, 0.6f)] public float multiScatter;
        [Range(0f, 1f)] public float compositeOpacity;
    }

    private static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
    private static readonly int GroundColorId = Shader.PropertyToID("_GroundColor");
    private static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");
    private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
    private static readonly int SunSizeId = Shader.PropertyToID("_SunSize");
    private static readonly int SunSizeConvergenceId = Shader.PropertyToID("_SunSizeConvergence");

    [Header("References")]
    public Light sunLight;
    public Material skyboxMaterial;
    public L13VolumeCloudController cloudController;
    public L17VolumetricLightingController volumetricLighting;

    [Header("Sun X Mapping")]
    [Tooltip("主光 X 轴等于该角度时使用正午 Profile。")]
    public float noonSunX = 90f;
    [Tooltip("主光 X 轴等于该角度时使用黄昏 Profile。")]
    public float sunsetSunX = 20f;
    [Tooltip("平滑 daytime blend，避免昼夜色调硬切。")]
    public bool smoothBlend = true;
    [Range(0f, 1f)] public float manualBlendPreview = 1f;
    public bool useManualBlend;

    [Header("Profiles")]
    public AtmosphereProfile noon = CreateNoonProfile();
    public AtmosphereProfile sunset = CreateSunsetProfile();

    [Header("Runtime")]
    public bool applyInEditMode = true;
    public bool instantiateSkybox = true;

    private Material runtimeSkybox;
    private Material sourceSkybox;
    private float lastAppliedNoonBlend = float.NaN;

    private void Reset()
    {
        AutoBindReferences();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
#endif
        AutoBindReferences();
        EnsureRuntimeSkybox();
        Apply(true);
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
        if (runtimeSkybox != null)
        {
            if (RenderSettings.skybox == runtimeSkybox)
            {
                RenderSettings.skybox = sourceSkybox;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeSkybox);
            }
            else
            {
                DestroyImmediate(runtimeSkybox);
            }
        }

        runtimeSkybox = null;
        sourceSkybox = null;
        lastAppliedNoonBlend = float.NaN;
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
    }

    private void OnValidate()
    {
        noonSunX = Mathf.Repeat(noonSunX, 360f);
        sunsetSunX = Mathf.Repeat(sunsetSunX, 360f);
        RefreshImmediate();
    }

    private void Update()
    {
        if (!Application.isPlaying && !applyInEditMode)
        {
            return;
        }

        AutoBindReferences();
        EnsureRuntimeSkybox();
        Apply();
    }

    [ContextMenu("Refresh Atmosphere Now")]
    public void RefreshImmediate()
    {
        if (!Application.isPlaying && !applyInEditMode)
        {
            return;
        }

        AutoBindReferences();
        EnsureRuntimeSkybox();
        Apply(true);
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (this == null)
        {
            EditorApplication.update -= EditorUpdate;
            return;
        }

        if (Application.isPlaying || !applyInEditMode || !isActiveAndEnabled)
        {
            return;
        }

        AutoBindReferences();
        EnsureRuntimeSkybox();
        Apply();
    }
#endif

    public float EvaluateNoonBlend()
    {
        if (useManualBlend)
        {
            return manualBlendPreview;
        }

        if (sunLight == null)
        {
            return 0f;
        }

        float sunX = Mathf.Repeat(sunLight.transform.eulerAngles.x, 360f);
        float blend = Mathf.InverseLerp(sunsetSunX, noonSunX, sunX);
        blend = Mathf.Clamp01(blend);
        return smoothBlend ? blend * blend * (3f - 2f * blend) : blend;
    }

    private void Apply(bool force = false)
    {
        float noonBlend = EvaluateNoonBlend();
        if (!force && !float.IsNaN(lastAppliedNoonBlend) && Mathf.Abs(noonBlend - lastAppliedNoonBlend) < 0.0005f)
        {
            return;
        }

        lastAppliedNoonBlend = noonBlend;
        AtmosphereProfile profile = LerpProfile(sunset, noon, noonBlend);

        ApplySun(profile);
        ApplySky(profile);
        ApplyEnvironment(profile);
        ApplyCloud(profile);
        ApplyVolumetricLight(profile);
    }

    private void ApplySun(AtmosphereProfile profile)
    {
        if (sunLight == null)
        {
            return;
        }

        sunLight.color = profile.sunColor;
        sunLight.intensity = profile.sunIntensity;
        sunLight.shadowStrength = profile.shadowStrength;
        sunLight.shadows = LightShadows.Soft;
    }

    private void ApplySky(AtmosphereProfile profile)
    {
        Material targetSkybox = runtimeSkybox != null ? runtimeSkybox : skyboxMaterial;
        if (targetSkybox == null)
        {
            return;
        }

        if (targetSkybox.HasProperty(SkyTintId)) targetSkybox.SetColor(SkyTintId, profile.skyTint);
        if (targetSkybox.HasProperty(GroundColorId)) targetSkybox.SetColor(GroundColorId, profile.groundColor);
        if (targetSkybox.HasProperty(AtmosphereThicknessId)) targetSkybox.SetFloat(AtmosphereThicknessId, profile.atmosphereThickness);
        if (targetSkybox.HasProperty(ExposureId)) targetSkybox.SetFloat(ExposureId, profile.exposure);
        if (targetSkybox.HasProperty(SunSizeId)) targetSkybox.SetFloat(SunSizeId, profile.sunSize);
        if (targetSkybox.HasProperty(SunSizeConvergenceId)) targetSkybox.SetFloat(SunSizeConvergenceId, profile.sunSizeConvergence);

        if (RenderSettings.skybox != targetSkybox)
        {
            RenderSettings.skybox = targetSkybox;
        }

        DynamicGI.UpdateEnvironment();
    }

    private void ApplyEnvironment(AtmosphereProfile profile)
    {
        RenderSettings.sun = sunLight;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = profile.ambientSky;
        RenderSettings.ambientEquatorColor = profile.ambientEquator;
        RenderSettings.ambientGroundColor = profile.ambientGround;
        RenderSettings.fogColor = profile.fogColor;
    }

    private void ApplyCloud(AtmosphereProfile profile)
    {
        if (cloudController == null)
        {
            return;
        }

        cloudController.cloudColor = profile.cloudColor;
        cloudController.shadowColor = profile.cloudShadowColor;
        cloudController.ambientColor = profile.cloudAmbientColor;
        cloudController.density = profile.cloudDensity;
        cloudController.coverage = profile.cloudCoverage;
        cloudController.weatherStrength = profile.weatherStrength;
        cloudController.macroGapStrength = profile.macroGapStrength;
        cloudController.shapeScale = profile.shapeScale;
        cloudController.detailScale = profile.detailScale;
        cloudController.detailStrength = profile.detailStrength;
        cloudController.bottomSoftness = profile.bottomSoftness;
        cloudController.topSoftness = profile.topSoftness;
        cloudController.anvilBias = profile.anvilBias;
        cloudController.absorption = profile.absorption;
        cloudController.lightAbsorption = profile.lightAbsorption;
        cloudController.forwardPhase = profile.forwardPhase;
        cloudController.backwardPhase = profile.backwardPhase;
        cloudController.silverIntensity = profile.silverIntensity;
        cloudController.powderStrength = profile.powderStrength;
        cloudController.opacity = profile.cloudOpacity;
        cloudController.sunLight = sunLight;
        cloudController.RefreshImmediate(false);
    }

    private void ApplyVolumetricLight(AtmosphereProfile profile)
    {
        if (volumetricLighting == null)
        {
            return;
        }

        volumetricLighting.sunLight = sunLight;
        volumetricLighting.sunColor = profile.sunColor;
        volumetricLighting.sunIntensity = profile.sunIntensity;
        volumetricLighting.ambientSky = profile.ambientSky;
        volumetricLighting.ambientEquator = profile.ambientEquator;
        volumetricLighting.ambientGround = profile.ambientGround;
        volumetricLighting.scatteringColor = profile.scatteringColor;
        volumetricLighting.maxDistance = profile.maxDistance;
        volumetricLighting.density = profile.volumeDensity;
        volumetricLighting.extinction = profile.extinction;
        volumetricLighting.intensity = profile.intensity;
        volumetricLighting.anisotropy = profile.anisotropy;
        volumetricLighting.forwardPhaseCeiling = profile.forwardPhaseCeiling;
        volumetricLighting.shadowFloor = profile.shadowFloor;
        volumetricLighting.multiScatter = profile.multiScatter;
        volumetricLighting.compositeOpacity = profile.compositeOpacity;
        volumetricLighting.RefreshImmediate();
    }

    private void EnsureRuntimeSkybox()
    {
        if (!instantiateSkybox)
        {
            return;
        }

        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }

        if (skyboxMaterial == null)
        {
            return;
        }

        if (runtimeSkybox != null && sourceSkybox == skyboxMaterial)
        {
            return;
        }

        if (runtimeSkybox != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeSkybox);
            }
            else
            {
                DestroyImmediate(runtimeSkybox);
            }
        }

        sourceSkybox = skyboxMaterial;
        runtimeSkybox = new Material(skyboxMaterial)
        {
            name = $"{skyboxMaterial.name} (L18 Runtime)"
        };
        runtimeSkybox.hideFlags = HideFlags.HideAndDontSave;
        RenderSettings.skybox = runtimeSkybox;
    }

    private void AutoBindReferences()
    {
        if (sunLight == null)
        {
            GameObject sunObject = GameObject.Find("L18 Low Storm Sun");
            sunLight = sunObject != null ? sunObject.GetComponent<Light>() : FindObjectOfType<Light>();
        }

        if (cloudController == null)
        {
            cloudController = FindObjectOfType<L13VolumeCloudController>();
        }

        if (volumetricLighting == null)
        {
            volumetricLighting = FindObjectOfType<L17VolumetricLightingController>();
        }

        if (skyboxMaterial == null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }
    }

    private static AtmosphereProfile LerpProfile(AtmosphereProfile a, AtmosphereProfile b, float t)
    {
        return new AtmosphereProfile
        {
            sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
            sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
            shadowStrength = Mathf.Lerp(a.shadowStrength, b.shadowStrength, t),
            skyTint = Color.Lerp(a.skyTint, b.skyTint, t),
            groundColor = Color.Lerp(a.groundColor, b.groundColor, t),
            atmosphereThickness = Mathf.Lerp(a.atmosphereThickness, b.atmosphereThickness, t),
            exposure = Mathf.Lerp(a.exposure, b.exposure, t),
            sunSize = Mathf.Lerp(a.sunSize, b.sunSize, t),
            sunSizeConvergence = Mathf.Lerp(a.sunSizeConvergence, b.sunSizeConvergence, t),
            ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, t),
            ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, t),
            ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, t),
            fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
            cloudColor = Color.Lerp(a.cloudColor, b.cloudColor, t),
            cloudShadowColor = Color.Lerp(a.cloudShadowColor, b.cloudShadowColor, t),
            cloudAmbientColor = Color.Lerp(a.cloudAmbientColor, b.cloudAmbientColor, t),
            cloudDensity = Mathf.Lerp(a.cloudDensity, b.cloudDensity, t),
            cloudCoverage = Mathf.Lerp(a.cloudCoverage, b.cloudCoverage, t),
            weatherStrength = Mathf.Lerp(a.weatherStrength, b.weatherStrength, t),
            macroGapStrength = Mathf.Lerp(a.macroGapStrength, b.macroGapStrength, t),
            shapeScale = Mathf.Lerp(a.shapeScale, b.shapeScale, t),
            detailScale = Mathf.Lerp(a.detailScale, b.detailScale, t),
            detailStrength = Mathf.Lerp(a.detailStrength, b.detailStrength, t),
            bottomSoftness = Mathf.Lerp(a.bottomSoftness, b.bottomSoftness, t),
            topSoftness = Mathf.Lerp(a.topSoftness, b.topSoftness, t),
            anvilBias = Mathf.Lerp(a.anvilBias, b.anvilBias, t),
            absorption = Mathf.Lerp(a.absorption, b.absorption, t),
            lightAbsorption = Mathf.Lerp(a.lightAbsorption, b.lightAbsorption, t),
            forwardPhase = Mathf.Lerp(a.forwardPhase, b.forwardPhase, t),
            backwardPhase = Mathf.Lerp(a.backwardPhase, b.backwardPhase, t),
            silverIntensity = Mathf.Lerp(a.silverIntensity, b.silverIntensity, t),
            powderStrength = Mathf.Lerp(a.powderStrength, b.powderStrength, t),
            cloudOpacity = Mathf.Lerp(a.cloudOpacity, b.cloudOpacity, t),
            scatteringColor = Color.Lerp(a.scatteringColor, b.scatteringColor, t),
            maxDistance = Mathf.Lerp(a.maxDistance, b.maxDistance, t),
            volumeDensity = Mathf.Lerp(a.volumeDensity, b.volumeDensity, t),
            extinction = Mathf.Lerp(a.extinction, b.extinction, t),
            intensity = Mathf.Lerp(a.intensity, b.intensity, t),
            anisotropy = Mathf.Lerp(a.anisotropy, b.anisotropy, t),
            forwardPhaseCeiling = Mathf.Lerp(a.forwardPhaseCeiling, b.forwardPhaseCeiling, t),
            shadowFloor = Mathf.Lerp(a.shadowFloor, b.shadowFloor, t),
            multiScatter = Mathf.Lerp(a.multiScatter, b.multiScatter, t),
            compositeOpacity = Mathf.Lerp(a.compositeOpacity, b.compositeOpacity, t),
        };
    }

    private static AtmosphereProfile CreateNoonProfile()
    {
        return new AtmosphereProfile
        {
            sunColor = new Color(1f, 0.97f, 0.9f, 1f),
            sunIntensity = 1.25f,
            shadowStrength = 0.72f,
            skyTint = new Color(0.22f, 0.48f, 1f, 1f),
            groundColor = new Color(0.36f, 0.48f, 0.62f, 1f),
            atmosphereThickness = 0.58f,
            exposure = 1.02f,
            sunSize = 0.018f,
            sunSizeConvergence = 6.5f,
            ambientSky = new Color(0.68f, 0.79f, 0.98f, 1f),
            ambientEquator = new Color(0.56f, 0.68f, 0.78f, 1f),
            ambientGround = new Color(0.38f, 0.46f, 0.38f, 1f),
            fogColor = new Color(0.74f, 0.86f, 0.98f, 1f),
            cloudColor = new Color(1f, 0.98f, 0.94f, 1f),
            cloudShadowColor = new Color(0.62f, 0.72f, 0.86f, 1f),
            cloudAmbientColor = new Color(0.78f, 0.88f, 1f, 1f),
            cloudDensity = 5.8f,
            cloudCoverage = 0.08f,
            weatherStrength = 0.68f,
            macroGapStrength = 0f,
            shapeScale = 2.8f,
            detailScale = 14f,
            detailStrength = 0.46f,
            bottomSoftness = 0.18f,
            topSoftness = 0.34f,
            anvilBias = 0.52f,
            absorption = 1.15f,
            lightAbsorption = 1.35f,
            forwardPhase = 0.42f,
            backwardPhase = -0.12f,
            silverIntensity = 0.85f,
            powderStrength = 0.72f,
            cloudOpacity = 0.58f,
            scatteringColor = new Color(0.92f, 0.98f, 1f, 1f),
            maxDistance = 2000f,
            volumeDensity = 0.00055f,
            extinction = 0.13f,
            intensity = 1.65f,
            anisotropy = 0.58f,
            forwardPhaseCeiling = 1.2f,
            shadowFloor = 0.006f,
            multiScatter = 0.018f,
            compositeOpacity = 0.38f,
        };
    }

    private static AtmosphereProfile CreateSunsetProfile()
    {
        return new AtmosphereProfile
        {
            sunColor = new Color(1f, 0.78f, 0.56f, 1f),
            sunIntensity = 1f,
            shadowStrength = 0.55f,
            skyTint = new Color(0.38f, 0.36f, 0.55f, 1f),
            groundColor = new Color(0.55f, 0.24f, 0.07f, 1f),
            atmosphereThickness = 1.35f,
            exposure = 1.08f,
            sunSize = 0.025f,
            sunSizeConvergence = 7f,
            ambientSky = new Color(0.35f, 0.28f, 0.38f, 1f),
            ambientEquator = new Color(0.38f, 0.24f, 0.18f, 1f),
            ambientGround = new Color(0.2f, 0.14f, 0.12f, 1f),
            fogColor = new Color(0.58f, 0.28f, 0.12f, 1f),
            cloudColor = new Color(1f, 0.68f, 0.46f, 1f),
            cloudShadowColor = new Color(0.31f, 0.13f, 0.18f, 1f),
            cloudAmbientColor = new Color(0.5f, 0.25f, 0.22f, 1f),
            cloudDensity = 12f,
            cloudCoverage = 0.117f,
            weatherStrength = 0.82f,
            macroGapStrength = 0f,
            shapeScale = 2.4f,
            detailScale = 14f,
            detailStrength = 0.52f,
            bottomSoftness = 0.16f,
            topSoftness = 0.3f,
            anvilBias = 0.56f,
            absorption = 2.45f,
            lightAbsorption = 2.7f,
            forwardPhase = 0.52f,
            backwardPhase = -0.22f,
            silverIntensity = 1.25f,
            powderStrength = 0.86f,
            cloudOpacity = 0.84f,
            scatteringColor = new Color(1f, 0.66f, 0.34f, 1f),
            maxDistance = 2000f,
            volumeDensity = 0.0018297182f,
            extinction = 0.28f,
            intensity = 8.5f,
            anisotropy = 0.72f,
            forwardPhaseCeiling = 1.5f,
            shadowFloor = 0.004f,
            multiScatter = 0.03f,
            compositeOpacity = 1f,
        };
    }
}
