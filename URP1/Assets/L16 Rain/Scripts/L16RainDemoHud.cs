using UnityEngine;

public sealed class L16RainDemoHud : MonoBehaviour
{
    public L16RainManager rainManager;
    public Material screenRainMaterial;

    private void OnGUI()
    {
        const int width = 430;
        GUI.Box(new Rect(16, 16, width, 214), "L16 Rain Only");
        GUILayout.BeginArea(new Rect(30, 42, width - 28, 176));
        GUILayout.Label("RMB orbit  |  MMB pan  |  Wheel zoom  |  R reset");

        if (rainManager != null)
        {
            DrawSlider("Rain Intensity", ref rainManager.rainIntensity, 0f, 1f);
            DrawSlider("Wind X", ref rainManager.wind.x, -2f, 2f);
            DrawSlider("Wind Z", ref rainManager.wind.y, -2f, 2f);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Quality: {rainManager.QualityLabel}", GUILayout.Width(120));
            if (GUILayout.Button("Low")) rainManager.qualityPreset = 0;
            if (GUILayout.Button("Medium")) rainManager.qualityPreset = 1;
            if (GUILayout.Button("High")) rainManager.qualityPreset = 2;
            GUILayout.EndHorizontal();
            GUILayout.Label($"GPU rain streaks: {rainManager.ActiveDropCount} / {rainManager.CurrentDropCount}");

            if (screenRainMaterial != null)
            {
                screenRainMaterial.SetFloat("_ScreenRainStrength", Mathf.Lerp(0.15f, 0.92f, rainManager.rainIntensity));
                screenRainMaterial.SetFloat("_LensDropletStrength", Mathf.Lerp(0.02f, 0.16f, rainManager.rainIntensity));
            }
        }

        GUILayout.EndArea();
    }

    private static void DrawSlider(string label, ref float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(145));
        value = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
    }
}
