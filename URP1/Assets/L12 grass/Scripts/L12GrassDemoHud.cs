using UnityEngine;

public sealed class L12GrassDemoHud : MonoBehaviour
{
    public L12GrassRenderer grassRenderer;

    private void OnGUI()
    {
        const float width = 420f;
        GUILayout.BeginArea(new Rect(16f, 16f, width, 150f), GUI.skin.box);
        GUILayout.Label("L12 大规模可交互草地");
        GUILayout.Label("WASD / 方向键：移动交互体");
        GUILayout.Label("Shift：加速");

        if (grassRenderer != null)
        {
            int count = grassRenderer.bladesPerSide * grassRenderer.bladesPerSide;
            GUILayout.Label($"GPU Instancing 草簇数量：{count:N0}");
            GUILayout.Label($"交互体数量：{L12GrassInteractor.ActiveInteractors.Count}");
        }

        GUILayout.EndArea();
    }
}
