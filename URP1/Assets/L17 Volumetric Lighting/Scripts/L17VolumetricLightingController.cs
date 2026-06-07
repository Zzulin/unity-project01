using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class L17VolumetricLightingController : MonoBehaviour
{
    [Header("Renderer Feature")]
    public UniversalRendererData rendererData;

    [Header("Froxel Grid")]
    [Range(1, 4)] public int downsample = 2;
    [Range(16, 96)] public int froxelDepth = 64;
    [Range(4f, 120f)] public float maxDistance = 58f;
    [Range(0.5f, 4f)] public float depthDistribution = 1.9f;

    [Header("Medium")]
    [Range(0f, 1.5f)] public float density = 0.24f;
    [Range(0.01f, 4f)] public float extinction = 0.68f;
    [Range(0f, 10f)] public float intensity = 3.25f;
    [Range(0f, 0.92f)] public float anisotropy = 0.78f;
    [Range(0f, 1f)] public float shadowFloor = 0.035f;
    [Range(0f, 2f)] public float multiScatter = 0.32f;
    [Range(-10f, 10f)] public float heightOrigin = -0.4f;
    [Range(0.01f, 8f)] public float heightFalloff = 0.22f;
    [Range(0f, 1f)] public float noiseStrength = 0.2f;
    [Range(0.05f, 8f)] public float noiseScale = 1.25f;
    public Color scatteringColor = new Color(1f, 0.84f, 0.52f, 1f);

    [Header("Stability")]
    [Range(0f, 0.98f)] public float temporalBlend = 0.88f;
    [Range(0.001f, 2f)] public float temporalDepthRejection = 0.12f;
    [Range(0f, 1f)] public float bilateralDepthScale = 0.08f;
    [Range(0f, 1f)] public float compositeOpacity = 0.94f;

    [Header("Sun")]
    public Light sunLight;
    [Range(0f, 8f)] public float sunIntensity = 2.35f;
    public Color sunColor = new Color(1f, 0.93f, 0.78f, 1f);

    [Header("Room Bounce")]
    public Color ambientSky = new Color(0.08f, 0.082f, 0.09f, 1f);
    public Color ambientEquator = new Color(0.045f, 0.04f, 0.034f, 1f);
    public Color ambientGround = new Color(0.018f, 0.015f, 0.012f, 1f);

    private L17FrustumVolumetricRendererFeature cachedFeature;

    private void OnEnable()
    {
        Apply(false);
    }

    private void OnValidate()
    {
        Apply(true);
    }

    private void Update()
    {
        Apply(false);
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
            sunLight.shadowStrength = 1f;
        }

        L17FrustumVolumetricRendererFeature feature = GetFeature();
        if (feature == null)
        {
            return;
        }

        L17FrustumVolumetricRendererFeature.Settings settings = feature.settings;
        settings.enabled = true;
        settings.downsample = downsample;
        settings.froxelDepth = froxelDepth;
        settings.maxDistance = maxDistance;
        settings.depthDistribution = depthDistribution;
        settings.density = density;
        settings.extinction = extinction;
        settings.intensity = intensity;
        settings.anisotropy = anisotropy;
        settings.shadowFloor = shadowFloor;
        settings.multiScatter = multiScatter;
        settings.heightOrigin = heightOrigin;
        settings.heightFalloff = heightFalloff;
        settings.noiseStrength = noiseStrength;
        settings.noiseScale = noiseScale;
        settings.temporalBlend = temporalBlend;
        settings.temporalDepthRejection = temporalDepthRejection;
        settings.bilateralDepthScale = bilateralDepthScale;
        settings.compositeOpacity = compositeOpacity;
        settings.scatteringColor = scatteringColor;
        settings.passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

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
}
