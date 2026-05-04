using UnityEngine;

public sealed class L14SnowDemoHud : MonoBehaviour
{
    public L14SnowField snowField;
    public L14SnowWalker walker;

    private void OnGUI()
    {
        const float width = 500f;
        GUILayout.BeginArea(new Rect(16f, 16f, width, 238f), GUI.skin.box);
        GUILayout.Label("L14 可交互雪地 - GPU Heightfield / Compute Stamp / URP Vertex Displacement");
        GUILayout.Label("WASD / 方向键：移动    Shift：加速");
        GUILayout.Label("右键拖拽：旋转视角    滚轮：缩放    中键拖拽：平移观察中心    R：复位相机");

        if (snowField != null)
        {
            GUILayout.Label($"雪地尺寸：{snowField.fieldSize:0}m    拓扑细分：{snowField.MeshResolution}x{snowField.MeshResolution}");
            GUILayout.Label($"高度场：{snowField.TextureResolution}x{snowField.TextureResolution} ARGBHalf Compute RT");
            GUILayout.Label($"本帧交互点：{snowField.ActiveStampCount}/{snowField.MaxStampCount}");
            GUILayout.Label($"压痕深度：{snowField.maxDepression:0.00}m    边缘堆雪：{snowField.ridgeHeight:0.00}m");
            GUILayout.Label($"基础雪面起伏：{snowField.baseReliefStrength:0.00}m    起伏尺度：{snowField.baseReliefScale:0.00}");
            GUILayout.Label($"恢复速度：{snowField.recoverySpeed:0.000}    堆雪沉降：{snowField.ridgeSettleSpeed:0.000}");
        }

        if (walker != null)
        {
            GUILayout.Label($"角色速度：{walker.CurrentSpeed:0.0} m/s");
        }

        GUILayout.Label("算法：局部 Compute Stamp 写入压实高度场，高细分网格顶点位移形成真实凹陷，片元阶段补充梯度法线。");
        GUILayout.EndArea();
    }
}
