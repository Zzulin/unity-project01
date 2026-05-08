using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(L13CloudNoiseSettings))]
public sealed class L13CloudNoiseSettingsEditor : Editor
{
    private const float DefaultDetailPeriodA = 8f;
    private const float DefaultDetailPeriodB = 13f;
    private const float DefaultDetailPeriodC = 24f;
    private const float DefaultDetailWeightA = 0.52f;
    private const float DefaultDetailWeightB = 0.32f;
    private const float DefaultDetailWeightC = 0.22f;

    private static L13CloudNoiseSettings pendingSettings;
    private static double regenerateAt;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("生成方式", EditorStyles.boldLabel);
        PropertyField("autoRegenerate", "自动延迟生成", "勾选后，修改参数会在短暂延迟后自动重新生成噪声贴图。");
        if (Find("autoRegenerate").boolValue)
        {
            Slider("autoRegenerateDelay", "延迟秒数", 0.25f, 3f, "拖动滑条后等待多久再生成，避免每一帧都重算 3D 贴图。");
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("贴图质量", EditorStyles.boldLabel);
        IntSlider("shapeNoiseSize", "主体精度", 16, 128, "主体 3D 噪声分辨率。越高轮廓越细，生成越慢。");
        IntSlider("detailNoiseSize", "细节精度", 16, 96, "细节 3D 噪声分辨率。越高边缘细节越丰富，生成越慢。");
        IntSlider("weatherMapSize", "分布图精度", 64, 512, "天气分布图分辨率。越高大片云区边缘越细。");

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("主体云团", EditorStyles.boldLabel);
        IntSlider("shapeBasePeriod", "大云团数量", 1, 16, "值越大，同一范围内的大云团越多、越碎；值越小，云团越大。");
        IntSlider("shapeWorleyPeriod", "块状边缘", 1, 24, "控制云边蜂窝状、块状轮廓的尺度。");
        Slider("shapeBaseWeight", "柔和云量", 0f, 2f, "提高后云体更柔和、连续。");
        Slider("shapeWorleyWeight", "块状云量", 0f, 2f, "提高后云体边缘更块状、更硬朗。");
        Slider("shapeBias", "云体饱满度", -1f, 1f, "提高后云更厚更满；降低后云更稀疏。");
        Slider("shapeBillowWeight", "棉絮起伏", 0f, 2f, "提高后云体内部有更多棉絮状起伏。");

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("细节破碎", EditorStyles.boldLabel);
        DrawDetailScale();
        DrawDetailStrength();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("天气分布", EditorStyles.boldLabel);
        IntSlider("weatherSystemPeriod", "云区大小", 1, 16, "值越小，天气图上的云区越大；值越大，云区越碎。");
        Slider("weatherBreakupWeight", "云区破碎度", 0f, 2f, "提高后大片云区会被打散，空洞更多。");
        DrawRange("coverageSmoothMin", "coverageSmoothMax", "覆盖范围", 0f, 1f, "控制天气图里有云区域的阈值范围。");
        DrawRange("densityMin", "densityMax", "密度范围", 0f, 2f, "控制天气图输出的局部密度上下限。");
        DrawRange("detailAmountMin", "detailAmountMax", "细节侵蚀范围", 0f, 2f, "控制不同区域受到细节噪声侵蚀的强弱。");

        bool changed = EditorGUI.EndChangeCheck() || serializedObject.hasModifiedProperties;
        serializedObject.ApplyModifiedProperties();

        L13CloudNoiseSettings settings = (L13CloudNoiseSettings)target;

        EditorGUILayout.Space(10f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("生成噪声贴图", GUILayout.Height(30f)))
            {
                L13VolumeCloudDemoBuilder.RegenerateNoiseTextures(settings);
            }

            if (GUILayout.Button("选中生成贴图", GUILayout.Height(30f)))
            {
                Object weatherMap = AssetDatabase.LoadAssetAtPath<Object>("Assets/L13 VolumeCloud/Textures/WeatherMap.png");
                Selection.activeObject = weatherMap != null ? weatherMap : settings;
            }
        }

        EditorGUILayout.HelpBox("这里省略了 seed、octaves 和分层权重等研发参数，只保留主要的形状控制。生成贴图只在点击按钮或自动延迟触发时执行，运行时仍然只是采样贴图。", MessageType.Info);

        if (changed && settings.autoRegenerate)
        {
            ScheduleRegenerate(settings);
        }
    }

    private SerializedProperty Find(string propertyName)
    {
        return serializedObject.FindProperty(propertyName);
    }

    private void PropertyField(string propertyName, string label, string tooltip)
    {
        EditorGUILayout.PropertyField(Find(propertyName), new GUIContent(label, tooltip));
    }

    private void Slider(string propertyName, string label, float min, float max, string tooltip)
    {
        SerializedProperty property = Find(propertyName);
        property.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), property.floatValue, min, max);
    }

    private void IntSlider(string propertyName, string label, int min, int max, string tooltip)
    {
        SerializedProperty property = Find(propertyName);
        property.intValue = EditorGUILayout.IntSlider(new GUIContent(label, tooltip), property.intValue, min, max);
    }

    private void DrawDetailScale()
    {
        SerializedProperty periodA = Find("detailPeriodA");
        SerializedProperty periodB = Find("detailPeriodB");
        SerializedProperty periodC = Find("detailPeriodC");
        int scale = Mathf.Clamp(periodB.intValue, 1, 64);
        EditorGUI.BeginChangeCheck();
        scale = EditorGUILayout.IntSlider(new GUIContent("细节颗粒大小", "值越大，细节噪声颗粒越密、边缘越碎。"), scale, 1, 64);
        if (EditorGUI.EndChangeCheck())
        {
            periodA.intValue = Mathf.Clamp(Mathf.RoundToInt(scale * (DefaultDetailPeriodA / DefaultDetailPeriodB)), 1, 32);
            periodB.intValue = Mathf.Clamp(scale, 1, 48);
            periodC.intValue = Mathf.Clamp(Mathf.RoundToInt(scale * (DefaultDetailPeriodC / DefaultDetailPeriodB)), 1, 64);
        }
    }

    private void DrawDetailStrength()
    {
        SerializedProperty weightA = Find("detailWeightA");
        SerializedProperty weightB = Find("detailWeightB");
        SerializedProperty weightC = Find("detailWeightC");
        float defaultSum = DefaultDetailWeightA + DefaultDetailWeightB + DefaultDetailWeightC;
        float currentSum = Mathf.Max(0.0001f, weightA.floatValue + weightB.floatValue + weightC.floatValue);
        float strength = Mathf.Clamp(currentSum / defaultSum, 0f, 2f);
        EditorGUI.BeginChangeCheck();
        strength = EditorGUILayout.Slider(new GUIContent("边缘破碎强度", "提高后云边被细节噪声侵蚀得更碎。"), strength, 0f, 2f);
        if (EditorGUI.EndChangeCheck())
        {
            weightA.floatValue = DefaultDetailWeightA * strength;
            weightB.floatValue = DefaultDetailWeightB * strength;
            weightC.floatValue = DefaultDetailWeightC * strength;
        }
    }

    private void DrawRange(string minPropertyName, string maxPropertyName, string label, float minLimit, float maxLimit, string tooltip)
    {
        SerializedProperty minProperty = Find(minPropertyName);
        SerializedProperty maxProperty = Find(maxPropertyName);
        float minValue = minProperty.floatValue;
        float maxValue = maxProperty.floatValue;
        EditorGUILayout.MinMaxSlider(new GUIContent(label, tooltip), ref minValue, ref maxValue, minLimit, maxLimit);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(EditorGUIUtility.labelWidth);
            minValue = EditorGUILayout.FloatField(minValue);
            maxValue = EditorGUILayout.FloatField(maxValue);
        }

        minProperty.floatValue = Mathf.Clamp(minValue, minLimit, maxLimit);
        maxProperty.floatValue = Mathf.Clamp(Mathf.Max(minProperty.floatValue + 0.001f, maxValue), minLimit, maxLimit);
    }

    private static void ScheduleRegenerate(L13CloudNoiseSettings settings)
    {
        pendingSettings = settings;
        regenerateAt = EditorApplication.timeSinceStartup + Mathf.Max(0.25f, settings.autoRegenerateDelay);
        EditorApplication.update -= TryRegenerate;
        EditorApplication.update += TryRegenerate;
    }

    private static void TryRegenerate()
    {
        if (pendingSettings == null)
        {
            EditorApplication.update -= TryRegenerate;
            return;
        }

        if (EditorApplication.timeSinceStartup < regenerateAt)
        {
            return;
        }

        L13CloudNoiseSettings settings = pendingSettings;
        pendingSettings = null;
        EditorApplication.update -= TryRegenerate;
        L13VolumeCloudDemoBuilder.RegenerateNoiseTextures(settings);
    }
}
