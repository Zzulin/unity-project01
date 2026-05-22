using UnityEngine;

public sealed class L15WaterDemoHud : MonoBehaviour
{
    public Material waterMaterial;
    public Material seabedMaterial;

    private void OnGUI()
    {
        const int width = 410;
        GUI.Box(new Rect(16, 16, width, 170), "L15 Modern Anime Water");
        GUILayout.BeginArea(new Rect(30, 42, width - 28, 135));
        GUILayout.Label("WASD move  |  Shift sprint  |  RMB orbit  |  MMB pan  |  Wheel zoom  |  R reset");
        GUILayout.Space(5);
        if (waterMaterial != null)
        {
            GUILayout.Label($"Surface: Gerstner x4, depth bands {_DepthSteps()}, refraction, Fresnel, foam");
            GUILayout.Label($"Water opacity {waterMaterial.GetFloat("_WaterOpacity"):0.00}  foam {waterMaterial.GetFloat("_FoamAmount"):0.00}");
        }

        if (seabedMaterial != null)
        {
            GUILayout.Label($"Seabed: basin mesh + triplanar caustics x {seabedMaterial.GetFloat("_CausticStrength"):0.00}");
        }
        GUILayout.EndArea();
    }

    private float _DepthSteps()
    {
        return waterMaterial != null ? waterMaterial.GetFloat("_DepthSteps") : 0f;
    }
}
