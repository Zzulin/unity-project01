using System.Text;
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
        var sb = new StringBuilder(2048);
        sb.AppendLine("===== L11 NPR Context Report =====");
        sb.AppendLine($"Active Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}");
        sb.AppendLine($"Expected Scene: {TargetScenePath}");
        sb.AppendLine();

        AppendRenderPipelineInfo(sb);
        sb.AppendLine();
        AppendQualityPipelines(sb);
        sb.AppendLine();
        AppendCharacterMaterialMapping(sb);

        Debug.Log(sb.ToString());
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

                sb.AppendLine($"  [{i}] {mat.name}");
                sb.AppendLine($"      shader: {shaderName}");
                sb.AppendLine($"      path: {(string.IsNullOrEmpty(materialPath) ? "(Embedded in Scene)" : materialPath)}");
                sb.AppendLine($"      inferred: {usage}");
            }
        }
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
}
