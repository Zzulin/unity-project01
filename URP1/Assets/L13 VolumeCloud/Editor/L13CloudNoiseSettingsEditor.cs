using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(L13CloudNoiseSettings))]
public sealed class L13CloudNoiseSettingsEditor : Editor
{
    private static L13CloudNoiseSettings pendingSettings;
    private static double regenerateAt;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        L13CloudNoiseSettings settings = (L13CloudNoiseSettings)target;

        EditorGUILayout.Space(10f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Regenerate Noise Textures", GUILayout.Height(28f)))
            {
                L13VolumeCloudDemoBuilder.RegenerateNoiseTextures(settings);
            }

            if (GUILayout.Button("Select Generated Textures", GUILayout.Height(28f)))
            {
                Object weatherMap = AssetDatabase.LoadAssetAtPath<Object>("Assets/L13 VolumeCloud/Textures/WeatherMap.png");
                Selection.activeObject = weatherMap != null ? weatherMap : settings;
            }
        }

        EditorGUILayout.HelpBox("Auto Regenerate is debounced. Dragging sliders edits this asset immediately, but texture generation runs after the delay instead of every repaint.", MessageType.Info);

        if (changed && settings.autoRegenerate)
        {
            ScheduleRegenerate(settings);
        }
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
