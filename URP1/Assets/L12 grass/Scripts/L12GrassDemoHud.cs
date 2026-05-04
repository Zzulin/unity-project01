using UnityEngine;

public sealed class L12GrassDemoHud : MonoBehaviour
{
    public L12GrassRenderer grassRenderer;

    private void OnGUI()
    {
        const float width = 460f;
        GUILayout.BeginArea(new Rect(16f, 16f, width, 190f), GUI.skin.box);
        GUILayout.Label("L12 大规模可交互草地 - Indirect / Chunk / LOD");
        GUILayout.Label("WASD / 方向键：移动交互体");
        GUILayout.Label("Shift：加速    右键拖拽：旋转视角");
        GUILayout.Label("滚轮：缩放    中键拖拽：平移观察中心    R：复位相机");

        if (grassRenderer != null)
        {
            GUILayout.Label($"源草簇数量：{grassRenderer.SourceBladeCount:N0}");
            GUILayout.Label($"Chunk：{grassRenderer.VisibleChunkCount}/{grassRenderer.ChunkCount}");
            GUILayout.Label($"Draw：DrawMeshInstancedIndirect + Compute Shader 剔除");
            GUILayout.Label($"LOD：0<{grassRenderer.lod0Distance:0}m，1<{grassRenderer.lod1Distance:0}m，2<{grassRenderer.maxDrawDistance:0}m");
            GUILayout.Label($"密度图：{(grassRenderer.densityMap != null ? grassRenderer.densityMap.name : "Runtime White")}");
            GUILayout.Label($"交互体数量：{L12GrassInteractor.ActiveInteractors.Count}");
        }

        GUILayout.EndArea();
    }
}
