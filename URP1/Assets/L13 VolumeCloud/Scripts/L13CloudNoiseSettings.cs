using UnityEngine;

[CreateAssetMenu(menuName = "L13 VolumeCloud/Cloud Noise Settings", fileName = "L13CloudNoiseSettings")]
public sealed class L13CloudNoiseSettings : ScriptableObject
{
    [Header("Generation")]
    public bool autoRegenerate;
    [Range(0.25f, 3f)] public float autoRegenerateDelay = 0.75f;

    [Header("Resolution")]
    [Range(16, 128)] public int shapeNoiseSize = 64;
    [Range(16, 96)] public int detailNoiseSize = 32;
    [Range(64, 512)] public int weatherMapSize = 256;

    [Header("Shape Noise")]
    [Range(1, 16)] public int shapeBasePeriod = 4;
    [Range(1, 8)] public int shapeOctaves = 5;
    public int shapeSeed = 1001;
    [Range(1, 24)] public int shapeWorleyPeriod = 5;
    public int shapeWorleySeed = 2001;
    [Range(0f, 2f)] public float shapeBaseWeight = 0.66f;
    [Range(0f, 2f)] public float shapeWorleyWeight = 0.42f;
    [Range(-1f, 1f)] public float shapeBias = -0.08f;
    [Range(0f, 2f)] public float shapeBlueWeight = 0.72f;
    [Range(0f, 2f)] public float shapeBillowWeight = 0.36f;

    [Header("Detail Noise")]
    [Range(1, 32)] public int detailPeriodA = 8;
    [Range(1, 48)] public int detailPeriodB = 13;
    [Range(1, 64)] public int detailPeriodC = 24;
    public int detailSeedA = 3001;
    public int detailSeedB = 4001;
    public int detailSeedC = 5001;
    [Range(0f, 2f)] public float detailWeightA = 0.52f;
    [Range(0f, 2f)] public float detailWeightB = 0.32f;
    [Range(0f, 2f)] public float detailWeightC = 0.22f;

    [Header("Weather Map")]
    [Range(1, 16)] public int weatherSystemPeriod = 3;
    [Range(1, 8)] public int weatherSystemOctaves = 5;
    public int weatherSystemSeed = 6001;
    [Range(1, 32)] public int weatherBreakupPeriod = 9;
    [Range(1, 8)] public int weatherBreakupOctaves = 4;
    public int weatherBreakupSeed = 7001;
    [Range(0f, 2f)] public float weatherSystemWeight = 0.78f;
    [Range(0f, 2f)] public float weatherBreakupWeight = 0.28f;
    [Range(0f, 1f)] public float coverageSmoothMin = 0.34f;
    [Range(0f, 1f)] public float coverageSmoothMax = 0.86f;

    [Header("Weather Channels")]
    [Range(1, 24)] public int cloudTypePeriod = 4;
    [Range(1, 8)] public int cloudTypeOctaves = 4;
    public int cloudTypeSeed = 8001;
    [Range(0f, 1f)] public float cloudTypeSmoothMin = 0.28f;
    [Range(0f, 1f)] public float cloudTypeSmoothMax = 0.78f;
    [Range(1, 24)] public int densityPeriod = 6;
    [Range(1, 8)] public int densityOctaves = 4;
    public int densitySeed = 9001;
    [Range(0f, 1f)] public float densityMin = 0.68f;
    [Range(0f, 2f)] public float densityMax = 1.0f;
    [Range(0f, 1f)] public float detailAmountMin = 0.72f;
    [Range(0f, 2f)] public float detailAmountMax = 1.0f;

    private void OnValidate()
    {
        coverageSmoothMax = Mathf.Max(coverageSmoothMin + 0.001f, coverageSmoothMax);
        cloudTypeSmoothMax = Mathf.Max(cloudTypeSmoothMin + 0.001f, cloudTypeSmoothMax);
        densityMax = Mathf.Max(densityMin, densityMax);
        detailAmountMax = Mathf.Max(detailAmountMin, detailAmountMax);
    }
}
