using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class L17VolumetricLightingController : MonoBehaviour
{
    private static readonly int BeamColorId = Shader.PropertyToID("_BeamColor");
    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int DensityId = Shader.PropertyToID("_Density");
    private static readonly int ExtinctionId = Shader.PropertyToID("_Extinction");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int AnisotropyId = Shader.PropertyToID("_Anisotropy");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    private static readonly int WindDirectionId = Shader.PropertyToID("_WindDirection");
    private static readonly int WindSpeedId = Shader.PropertyToID("_WindSpeed");
    private static readonly int EdgeFadeId = Shader.PropertyToID("_EdgeFade");
    private static readonly int AxialFadeId = Shader.PropertyToID("_AxialFade");
    private static readonly int ShadowContrastId = Shader.PropertyToID("_ShadowContrast");
    private static readonly int ShadowFloorId = Shader.PropertyToID("_ShadowFloor");
    private static readonly int LightBoostId = Shader.PropertyToID("_LightBoost");
    private static readonly int StepCountId = Shader.PropertyToID("_StepCount");

    private static readonly int WallBaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int WallShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int WallSmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int WallSpecularStrengthId = Shader.PropertyToID("_SpecularStrength");
    private static readonly int WallWrapDiffuseId = Shader.PropertyToID("_WrapDiffuse");
    private static readonly int WallAmbientBoostId = Shader.PropertyToID("_AmbientBoost");

    [Header("References")]
    public Material beamMaterial;
    public Material wallMaterial;
    public Light sunLight;

    [Header("Quality")]
    [Range(8, 96)] public int stepCount = 24;
    [Range(0f, 1f)] public float opacity = 1f;

    [Header("Beam")]
    public Color beamColor = new Color(1f, 0.93f, 0.72f, 1f);
    public Color beamShadowColor = new Color(0.5f, 0.43f, 0.31f, 1f);
    [Range(0f, 6f)] public float density = 2.2f;
    [Range(0.1f, 8f)] public float extinction = 1.06f;
    [Range(0f, 16f)] public float intensity = 7.2f;
    [Range(0f, 0.92f)] public float anisotropy = 0.7f;
    [Range(0.1f, 8f)] public float noiseScale = 1.28f;
    [Range(0f, 1f)] public float noiseStrength = 0.18f;
    public Vector4 windDirection = new Vector4(0.58f, 0f, -0.18f, 0f);
    [Range(0f, 4f)] public float windSpeed = 0.36f;
    [Range(0.01f, 0.45f)] public float edgeFade = 0.11f;
    [Range(0.01f, 0.45f)] public float axialFade = 0.1f;
    [Range(0.2f, 4f)] public float shadowContrast = 1.15f;
    [Range(0f, 1f)] public float shadowFloor = 0.14f;
    [Range(0f, 4f)] public float lightBoost = 1.22f;

    [Header("Room Bounce")]
    public Color ambientSky = new Color(0.16f, 0.145f, 0.13f, 1f);
    public Color ambientEquator = new Color(0.09f, 0.082f, 0.074f, 1f);
    public Color ambientGround = new Color(0.04f, 0.032f, 0.028f, 1f);
    [Range(0f, 3f)] public float wallAmbientBoost = 0.55f;

    [Header("Wall")]
    public Color wallBaseColor = new Color(0.72f, 0.68f, 0.6f, 1f);
    public Color wallShadowColor = new Color(0.18f, 0.16f, 0.14f, 1f);
    [Range(0f, 1f)] public float wallSmoothness = 0.12f;
    [Range(0f, 2f)] public float wallSpecularStrength = 0.06f;
    [Range(0f, 1f)] public float wallWrapDiffuse = 0.03f;

    [Header("Sun")]
    [Range(0f, 8f)] public float sunIntensity = 1.6f;
    public Color sunColor = new Color(1f, 0.95f, 0.86f, 1f);

    private void OnEnable()
    {
        Apply(false);
    }

    private void OnValidate()
    {
        stepCount = Mathf.Clamp(stepCount, 8, 96);
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
        if (beamMaterial != null)
        {
            beamMaterial.SetColor(BeamColorId, beamColor);
            beamMaterial.SetColor(ShadowColorId, beamShadowColor);
            beamMaterial.SetFloat(DensityId, density);
            beamMaterial.SetFloat(ExtinctionId, extinction);
            beamMaterial.SetFloat(IntensityId, intensity);
            beamMaterial.SetFloat(OpacityId, opacity);
            beamMaterial.SetFloat(AnisotropyId, anisotropy);
            beamMaterial.SetFloat(NoiseScaleId, noiseScale);
            beamMaterial.SetFloat(NoiseStrengthId, noiseStrength);
            beamMaterial.SetVector(WindDirectionId, windDirection);
            beamMaterial.SetFloat(WindSpeedId, windSpeed);
            beamMaterial.SetFloat(EdgeFadeId, edgeFade);
            beamMaterial.SetFloat(AxialFadeId, axialFade);
            beamMaterial.SetFloat(ShadowContrastId, shadowContrast);
            beamMaterial.SetFloat(ShadowFloorId, shadowFloor);
            beamMaterial.SetFloat(LightBoostId, lightBoost);
            beamMaterial.SetFloat(StepCountId, stepCount);

#if UNITY_EDITOR
            if (persistAssets && !Application.isPlaying)
            {
                EditorUtility.SetDirty(beamMaterial);
            }
#endif
        }

        if (wallMaterial != null)
        {
            wallMaterial.SetColor(WallBaseColorId, wallBaseColor);
            wallMaterial.SetColor(WallShadowColorId, wallShadowColor);
            wallMaterial.SetFloat(WallSmoothnessId, wallSmoothness);
            wallMaterial.SetFloat(WallSpecularStrengthId, wallSpecularStrength);
            wallMaterial.SetFloat(WallWrapDiffuseId, wallWrapDiffuse);
            wallMaterial.SetFloat(WallAmbientBoostId, wallAmbientBoost);

#if UNITY_EDITOR
            if (persistAssets && !Application.isPlaying)
            {
                EditorUtility.SetDirty(wallMaterial);
            }
#endif
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;

        if (sunLight != null)
        {
            sunLight.intensity = sunIntensity;
            sunLight.color = sunColor;
        }
    }
}
