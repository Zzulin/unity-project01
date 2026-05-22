using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(L12GrassRenderer))]
[CanEditMultipleObjects]
public sealed class L12GrassRendererEditor : Editor
{
    private void OnEnable()
    {
        RefreshProperties();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        DrawSection("渲染资源");
        Draw("grassMaterial", "草地材质", "草叶使用的 GPU 草地材质。");
        Draw("cullingCompute", "GPU 剔除计算", "负责密度、距离、视锥和 LOD 剔除的 Compute Shader。");
        Draw("densityMap", "密度贴图", "控制草地哪里密、哪里稀的密度图。");

        DrawSection("草地规模");
        Draw("bladesPerSide", "基础草株数", "基础采样数量。启用保持密度后，实际草株数主要由目标草间距决定。");
        Draw("fieldSize", "基础覆盖边长", "未缩放时的草地边长，单位为米。");
        Draw("preserveDensityWhenResized", "缩放时保持密度", "放大草地时自动增加草株数量，避免越放大越稀。");
        Draw("targetBladeSpacing", "目标草间距", "世界空间草株间距。值越小越密。");
        Draw("maxBladesPerAxis", "单轴安全上限", "只限制每个方向最多生成多少株草，防止面积放大后数量失控。");
        Draw("chunksPerSide", "性能分块数", "用于整块预筛和调度；块内仍按单株距离剔除，所以可见边界通常还是圆弧。");
        Draw("bladeHeight", "基础草高", "草叶基础高度，会再乘以高低层次随机。");
        Draw("bladeWidth", "草叶宽度", "每片草叶的基础宽度。");
        Draw("bladeRootWidthScale", "根部宽度倍率", "控制草叶底部是否更宽、更扎实。");
        Draw("minBladeHeightScale", "高低层次：矮草倍率", "最低草高倍率。");
        Draw("maxBladeHeightScale", "高低层次：高草倍率", "最高草高倍率。");
        Draw("shapeVariation", "叶形随机度", "控制宽窄、自旋、倾斜、斜尖偏移等随机变化。");

        DrawSection("剔除与 LOD");
        Draw("maxDrawDistance", "最远绘制距离", "超过这个距离的草不绘制。");
        Draw("cullPadding", "剔除安全边距", "视锥剔除的额外边距，避免边缘草突然消失。");
        Draw("lod0Distance", "近景精细距离", "近处使用最高细分草叶的距离。");
        Draw("lod1Distance", "中景过渡距离", "中景使用较低细分草叶的距离。");
        Draw("densityThreshold", "密度裁剪阈值", "密度低于该值的位置不生成草。");
        Draw("densityInfluence", "密度贴图影响", "放大或减弱密度贴图对草量的影响。");

        DrawSection("风");
        Draw("windStrength", "微风摆动强度", "持续小风对草叶的影响。");
        Draw("windScale", "风纹大小", "风纹空间尺度。值越小变化越细。");
        Draw("windSpeed", "微风速度", "持续小风流动速度。");
        Draw("windDirection", "风吹方向", "风在 XZ 平面的方向。");
        Draw("gustStrength", "阵风压弯强度", "阵风经过时草叶被压弯的程度。");
        Draw("gustFrequency", "阵风间距", "阵风带之间的距离感。");
        Draw("gustSpeed", "阵风推进速度", "阵风向前移动的速度。");
        Draw("gustWidth", "阵风带宽", "阵风影响区域的宽度。");
        Draw("gustNoiseScale", "阵风噪声碎度", "阵风边缘和强弱的破碎感。");

        DrawSection("交互压草纹理");
        Draw("interactionTextureSize", "压草纹理精度", "压草轨迹纹理大小。越高边缘越细，但更新成本更高。");
        Draw("interactionStrength", "压草推开力度", "角色经过时草叶向外倒伏的强度。");
        Draw("interactionFlattenStrength", "压草塌陷强度", "角色经过时草叶向下压低的强度。");
        Draw("interactionFadeSpeed", "草痕恢复速度", "压草痕迹恢复到原状的速度。");

        DrawSection("颜色");
        Draw("baseColor", "草根深色", "草叶根部颜色。");
        Draw("tipColor", "草尖浅色", "草叶尖端颜色。");
        Draw("tipBrightness", "草尖发光感", "草尖颜色的额外提亮倍率。");

        serializedObject.ApplyModifiedProperties();
    }

    private void RefreshProperties()
    {
        // Forces Unity to rebuild cached SerializedProperty display data after script reloads.
        serializedObject.UpdateIfRequiredOrScript();
    }

    private static void DrawSection(string label)
    {
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    private void Draw(string propertyName, string label, string tooltip)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            EditorGUILayout.HelpBox($"找不到参数：{propertyName}", MessageType.Warning);
            return;
        }

        EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip), true);
    }
}
