using UnityEngine;

[ExecuteAlways]
public sealed class L13VolumeCloudController : MonoBehaviour
{
    private static readonly int CloudColorId = Shader.PropertyToID("_CloudColor");
    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int AmbientColorId = Shader.PropertyToID("_AmbientColor");
    private static readonly int DensityId = Shader.PropertyToID("_Density");
    private static readonly int CoverageId = Shader.PropertyToID("_Coverage");
    private static readonly int WeatherStrengthId = Shader.PropertyToID("_WeatherStrength");
    private static readonly int ShapeScaleId = Shader.PropertyToID("_ShapeScale");
    private static readonly int DetailScaleId = Shader.PropertyToID("_DetailScale");
    private static readonly int DetailStrengthId = Shader.PropertyToID("_DetailStrength");
    private static readonly int BottomSoftnessId = Shader.PropertyToID("_BottomSoftness");
    private static readonly int TopSoftnessId = Shader.PropertyToID("_TopSoftness");
    private static readonly int AnvilBiasId = Shader.PropertyToID("_AnvilBias");
    private static readonly int AbsorptionId = Shader.PropertyToID("_Absorption");
    private static readonly int LightAbsorptionId = Shader.PropertyToID("_LightAbsorption");
    private static readonly int PhaseForwardId = Shader.PropertyToID("_PhaseForward");
    private static readonly int PhaseBackwardId = Shader.PropertyToID("_PhaseBackward");
    private static readonly int SilverIntensityId = Shader.PropertyToID("_SilverIntensity");
    private static readonly int PowderStrengthId = Shader.PropertyToID("_PowderStrength");
    private static readonly int WindDirectionId = Shader.PropertyToID("_WindDirection");
    private static readonly int WindSpeedId = Shader.PropertyToID("_WindSpeed");
    private static readonly int StepCountId = Shader.PropertyToID("_StepCount");
    private static readonly int LightStepCountId = Shader.PropertyToID("_LightStepCount");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int SunDirectionId = Shader.PropertyToID("_SunDirectionWS");
    private static readonly int SunColorId = Shader.PropertyToID("_SunColor");

    [Header("References")]
    public Material cloudMaterial;
    public Light sunLight;

    [Header("Shape")]
    public Color cloudColor = new Color(1f, 0.92f, 0.78f, 1f);
    public Color shadowColor = new Color(0.48f, 0.56f, 0.68f, 1f);
    public Color ambientColor = new Color(0.46f, 0.55f, 0.72f, 1f);
    [Range(0f, 12f)] public float density = 3.2f;
    [Range(0f, 1f)] public float coverage = 0.6f;
    [Range(0f, 1f)] public float weatherStrength = 0.72f;
    [Range(1f, 24f)] public float shapeScale = 10.5f;
    [Range(4f, 96f)] public float detailScale = 38f;
    [Range(0f, 1f)] public float detailStrength = 0.42f;
    [Range(0.01f, 0.45f)] public float bottomSoftness = 0.18f;
    [Range(0.01f, 0.45f)] public float topSoftness = 0.22f;
    [Range(0f, 1f)] public float anvilBias = 0.62f;
    [Range(0.2f, 8f)] public float absorption = 2.6f;
    [Range(0.2f, 8f)] public float lightAbsorption = 2.9f;
    [Range(0f, 0.85f)] public float forwardPhase = 0.58f;
    [Range(-0.65f, 0f)] public float backwardPhase = -0.28f;
    [Range(0f, 4f)] public float silverIntensity = 1.65f;
    [Range(0f, 3f)] public float powderStrength = 1.15f;
    public Vector4 windDirection = new Vector4(1f, 0f, 0.25f, 0f);
    [Range(0f, 30f)] public float windSpeed = 7f;

    [Header("Quality")]
    [Range(24, 160)] public int stepCount = 48;
    [Range(1, 12)] public int lightStepCount = 4;
    [Range(0f, 1f)] public float opacity = 0.92f;

    private MeshRenderer cachedRenderer;

    public void ApplyPreset(L13CloudPreset preset)
    {
        density = preset.density;
        coverage = preset.coverage;
        weatherStrength = preset.weatherStrength;
        shapeScale = preset.shapeScale;
        detailScale = preset.detailScale;
        detailStrength = preset.detailStrength;
        bottomSoftness = preset.bottomSoftness;
        topSoftness = preset.topSoftness;
        anvilBias = preset.anvilBias;
        absorption = preset.absorption;
        lightAbsorption = preset.lightAbsorption;
        forwardPhase = preset.forwardPhase;
        backwardPhase = preset.backwardPhase;
        silverIntensity = preset.silverIntensity;
        powderStrength = preset.powderStrength;
        windSpeed = preset.windSpeed;
        stepCount = preset.stepCount;
        lightStepCount = preset.lightStepCount;
        PushProperties();
    }

    private void OnEnable()
    {
        CacheMaterialFromRenderer();
        PushProperties();
    }

    private void OnValidate()
    {
        CacheMaterialFromRenderer();
        PushProperties();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PushProperties();
    }

    private void CacheMaterialFromRenderer()
    {
        if (cloudMaterial != null)
        {
            return;
        }

        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<MeshRenderer>();
        }

        if (cachedRenderer != null)
        {
            cloudMaterial = cachedRenderer.sharedMaterial;
        }
    }

    private void PushProperties()
    {
        if (cloudMaterial == null)
        {
            return;
        }

        cloudMaterial.SetColor(CloudColorId, cloudColor);
        cloudMaterial.SetColor(ShadowColorId, shadowColor);
        cloudMaterial.SetColor(AmbientColorId, ambientColor);
        cloudMaterial.SetFloat(DensityId, density);
        cloudMaterial.SetFloat(CoverageId, coverage);
        cloudMaterial.SetFloat(WeatherStrengthId, weatherStrength);
        cloudMaterial.SetFloat(ShapeScaleId, shapeScale);
        cloudMaterial.SetFloat(DetailScaleId, detailScale);
        cloudMaterial.SetFloat(DetailStrengthId, detailStrength);
        cloudMaterial.SetFloat(BottomSoftnessId, bottomSoftness);
        cloudMaterial.SetFloat(TopSoftnessId, topSoftness);
        cloudMaterial.SetFloat(AnvilBiasId, anvilBias);
        cloudMaterial.SetFloat(AbsorptionId, absorption);
        cloudMaterial.SetFloat(LightAbsorptionId, lightAbsorption);
        cloudMaterial.SetFloat(PhaseForwardId, forwardPhase);
        cloudMaterial.SetFloat(PhaseBackwardId, backwardPhase);
        cloudMaterial.SetFloat(SilverIntensityId, silverIntensity);
        cloudMaterial.SetFloat(PowderStrengthId, powderStrength);
        cloudMaterial.SetVector(WindDirectionId, windDirection);
        cloudMaterial.SetFloat(WindSpeedId, windSpeed);
        cloudMaterial.SetInt(StepCountId, stepCount);
        cloudMaterial.SetInt(LightStepCountId, lightStepCount);
        cloudMaterial.SetFloat(OpacityId, opacity);

        Vector3 sunDirection = Vector3.Normalize(new Vector3(0.35f, 0.72f, 0.4f));
        Color sunColor = Color.white;
        if (sunLight != null)
        {
            sunDirection = -sunLight.transform.forward;
            sunColor = sunLight.color * Mathf.Max(sunLight.intensity, 0f);
        }

        cloudMaterial.SetVector(SunDirectionId, new Vector4(sunDirection.x, sunDirection.y, sunDirection.z, 0f));
        cloudMaterial.SetColor(SunColorId, sunColor);
    }
}

[System.Serializable]
public struct L13CloudPreset
{
    public float density;
    public float coverage;
    public float weatherStrength;
    public float shapeScale;
    public float detailScale;
    public float detailStrength;
    public float bottomSoftness;
    public float topSoftness;
    public float anvilBias;
    public float absorption;
    public float lightAbsorption;
    public float forwardPhase;
    public float backwardPhase;
    public float silverIntensity;
    public float powderStrength;
    public float windSpeed;
    public int stepCount;
    public int lightStepCount;

    public L13CloudPreset(float density, float coverage, float weatherStrength, float shapeScale, float detailScale, float detailStrength, float bottomSoftness, float topSoftness, float anvilBias, float absorption, float lightAbsorption, float forwardPhase, float backwardPhase, float silverIntensity, float powderStrength, float windSpeed, int stepCount, int lightStepCount)
    {
        this.density = density;
        this.coverage = coverage;
        this.weatherStrength = weatherStrength;
        this.shapeScale = shapeScale;
        this.detailScale = detailScale;
        this.detailStrength = detailStrength;
        this.bottomSoftness = bottomSoftness;
        this.topSoftness = topSoftness;
        this.anvilBias = anvilBias;
        this.absorption = absorption;
        this.lightAbsorption = lightAbsorption;
        this.forwardPhase = forwardPhase;
        this.backwardPhase = backwardPhase;
        this.silverIntensity = silverIntensity;
        this.powderStrength = powderStrength;
        this.windSpeed = windSpeed;
        this.stepCount = stepCount;
        this.lightStepCount = lightStepCount;
    }
}
