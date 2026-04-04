using System.Text;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class L11NprContextReporter
{
    private const string TargetScenePath = "Assets/L11 NPR/L11.unity";
    private const string TargetRootName = "Avatar_Ruanmei_Body";
    private const string TargetRendererName = "0_mesh_mesh";

    [MenuItem("Tools/NPR/输出 L11 上下文报告")]
    public static void PrintReport()
    {
        Debug.Log(BuildReportText());
    }

    [MenuItem("Tools/NPR/导出 L11 上下文报告到 codex")]
    public static void ExportReportToCodex()
    {
        string report = BuildReportText();
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
        string codexDir = Path.Combine(projectRoot, "codex");
        string outputPath = Path.Combine(codexDir, "l11-context-report.txt");

        Directory.CreateDirectory(codexDir);
        File.WriteAllText(outputPath, report, Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"L11 context report exported: {outputPath}");
    }

    private static string BuildReportText()
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("===== L11 NPR Context Report =====");
        sb.AppendLine($"Active Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}");
        sb.AppendLine($"Expected Scene: {TargetScenePath}");
        sb.AppendLine();
        AppendRenderPipelineInfo(sb);
        sb.AppendLine();
        AppendQualityPipelines(sb);
        sb.AppendLine();
        AppendRendererFeatureInfo(sb);
        sb.AppendLine();
        AppendCharacterMaterialMapping(sb);
        return sb.ToString();
    }

    private static void AppendRenderPipelineInfo(StringBuilder sb)
    {
        RenderPipelineAsset graphicsAsset = GraphicsSettings.defaultRenderPipeline;
        RenderPipelineAsset qualityAsset = QualitySettings.renderPipeline;
        var currentPipeline = GraphicsSettings.currentRenderPipeline;

        sb.AppendLine("[Render Pipeline]");
        sb.AppendLine($"GraphicsSettings.defaultRenderPipeline: {GetAssetLabel(graphicsAsset)}");
        sb.AppendLine($"QualitySettings.renderPipeline: {GetAssetLabel(qualityAsset)}");
        sb.AppendLine($"GraphicsSettings.currentRenderPipeline: {(currentPipeline == null ? "(Built-in)" : currentPipeline.GetType().Name)}");

        if (graphicsAsset is UniversalRenderPipelineAsset urpAsset)
        {
            sb.AppendLine($"URP Rendering Mode: via renderer config");
        }
    }

    private static void AppendQualityPipelines(StringBuilder sb)
    {
        sb.AppendLine("[Quality -> RP Asset]");

        string[] names = QualitySettings.names;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < names.Length; i++)
        {
            RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(i);
            string currentMark = i == current ? " (Current)" : string.Empty;
            sb.AppendLine($"- {names[i]}{currentMark}: {GetAssetLabel(asset)}");
        }
    }

    private static void AppendRendererFeatureInfo(StringBuilder sb)
    {
        sb.AppendLine("[URP Renderer & Features]");

        var urp = ResolveActiveUrpAsset();
        if (urp == null)
        {
            sb.AppendLine("- Active pipeline is not URP.");
            return;
        }

        sb.AppendLine($"- Active URP Asset: {GetAssetLabel(urp)}");

        if (!TryGetRendererDataInfo(urp, out var defaultRendererData, out int defaultRendererIndex, out int rendererCount))
        {
            sb.AppendLine("- Unable to read renderer data list from URP asset.");
            return;
        }

        sb.AppendLine($"- Renderer count: {rendererCount}");
        sb.AppendLine($"- Default renderer index: {defaultRendererIndex}");
        sb.AppendLine($"- Default renderer: {GetAssetLabel(defaultRendererData)}");

        if (defaultRendererData == null)
        {
            sb.AppendLine("- Default renderer data is null.");
            return;
        }

        bool foundStarRailFeature = false;
        foreach (var feature in defaultRendererData.rendererFeatures)
        {
            if (feature == null)
            {
                sb.AppendLine("  - <missing feature reference>");
                continue;
            }

            bool isStarRail = feature.GetType().Name == "StarRailRendererFeature";
            if (isStarRail)
            {
                foundStarRailFeature = true;
            }

            string activeMark = feature.isActive ? "Active" : "Inactive";
            string hitMark = isStarRail ? " [StarRail]" : string.Empty;
            sb.AppendLine($"  - {feature.name} ({feature.GetType().Name}) [{activeMark}]{hitMark}");
        }

        if (!foundStarRailFeature)
        {
            sb.AppendLine("- WARNING: StarRailRendererFeature not found on default renderer.");
        }
    }

    private static void AppendCharacterMaterialMapping(StringBuilder sb)
    {
        sb.AppendLine("[Character Material Mapping]");

        GameObject root = GameObject.Find(TargetRootName);
        if (root == null)
        {
            sb.AppendLine($"- Not found GameObject: {TargetRootName}");
            return;
        }

        var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0)
        {
            sb.AppendLine("- No SkinnedMeshRenderer under Avatar_Ruanmei_Body.");
            return;
        }

        int charBodyCount = 0;
        int charFaceCount = 0;
        int charHairCount = 0;
        int urpLitCount = 0;
        int embeddedCount = 0;

        foreach (var renderer in renderers)
        {
            string rendererFlag = renderer.name == TargetRendererName ? " (Target)" : string.Empty;
            sb.AppendLine($"- Renderer: {renderer.name}{rendererFlag}");

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null)
                {
                    sb.AppendLine($"  [{i}] <null>");
                    continue;
                }

                Shader shader = mat.shader;
                string shaderName = shader == null ? "<null>" : shader.name;
                string materialPath = AssetDatabase.GetAssetPath(mat);
                string usage = ClassifyUsage(mat);
                bool isEmbedded = string.IsNullOrEmpty(materialPath);

                if (shaderName.Contains("Char/Body"))
                {
                    charBodyCount++;
                }
                else if (shaderName.Contains("Char/Face"))
                {
                    charFaceCount++;
                }
                else if (shaderName.Contains("Char/Hair"))
                {
                    charHairCount++;
                }
                else if (shaderName.Contains("Universal Render Pipeline/Lit"))
                {
                    urpLitCount++;
                }

                if (isEmbedded)
                {
                    embeddedCount++;
                }

                sb.AppendLine($"  [{i}] {mat.name}");
                sb.AppendLine($"      shader: {shaderName}");
                sb.AppendLine($"      path: {(isEmbedded ? "(Embedded in Scene)" : materialPath)}");
                sb.AppendLine($"      inferred: {usage}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("[Character Material Summary]");
        sb.AppendLine($"- CharBody slots: {charBodyCount}");
        sb.AppendLine($"- CharFace slots: {charFaceCount}");
        sb.AppendLine($"- CharHair slots: {charHairCount}");
        sb.AppendLine($"- URP Lit slots: {urpLitCount}");
        sb.AppendLine($"- Embedded material slots: {embeddedCount}");
    }

    private static string ClassifyUsage(Material material)
    {
        if (material.shader == null)
        {
            return "Unknown";
        }

        string shaderName = material.shader.name;
        if (shaderName.Contains("Char/Hair"))
        {
            return "Hair";
        }

        if (shaderName.Contains("Char/Face"))
        {
            return "Face";
        }

        if (shaderName.Contains("Char/Body"))
        {
            return "Body";
        }

        if (material.HasProperty("_HairShadowDistance") || material.HasProperty("_FaceMap"))
        {
            return "Face?";
        }

        if (material.HasProperty("_StockingsMap") || material.HasProperty("_RampMapCool"))
        {
            return "Body/Hair NPR?";
        }

        return "Unknown";
    }

    private static string GetAssetLabel(Object asset)
    {
        if (asset == null)
        {
            return "(null)";
        }

        string path = AssetDatabase.GetAssetPath(asset);
        return string.IsNullOrEmpty(path) ? asset.name : $"{asset.name} ({path})";
    }

    private static UniversalRenderPipelineAsset ResolveActiveUrpAsset()
    {
        if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrp)
        {
            return qualityUrp;
        }

        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset graphicsUrp)
        {
            return graphicsUrp;
        }

        return null;
    }

    private static bool TryGetRendererDataInfo(
        UniversalRenderPipelineAsset urpAsset,
        out ScriptableRendererData defaultRendererData,
        out int defaultRendererIndex,
        out int rendererCount)
    {
        defaultRendererData = null;
        defaultRendererIndex = -1;
        rendererCount = 0;

        var serializedObject = new SerializedObject(urpAsset);
        var rendererDataList = serializedObject.FindProperty("m_RendererDataList");
        var defaultRendererIndexProperty = serializedObject.FindProperty("m_DefaultRendererIndex");
        if (rendererDataList == null || defaultRendererIndexProperty == null || !rendererDataList.isArray)
        {
            return false;
        }

        rendererCount = rendererDataList.arraySize;
        defaultRendererIndex = Mathf.Clamp(defaultRendererIndexProperty.intValue, 0, Mathf.Max(0, rendererCount - 1));
        if (rendererCount == 0)
        {
            return true;
        }

        defaultRendererData = rendererDataList.GetArrayElementAtIndex(defaultRendererIndex).objectReferenceValue as ScriptableRendererData;
        return true;
    }
}
