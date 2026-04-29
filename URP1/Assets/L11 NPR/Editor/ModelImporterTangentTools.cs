using System.Text;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ModelImporterTangentTools
{
    [MenuItem("Tools/NPR/模型/输出所选模型切线诊断")]
    private static void ReportSelectedModelTangents()
    {
        var assetPath = GetSelectedModelAssetPath();
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("请先在 Project 里选中一个 FBX/OBJ 模型资源。");
            return;
        }

        ReportModelTangents(assetPath);
    }

    [MenuItem("Tools/NPR/模型/导出所选模型 Mesh 为 .asset 并重算切线")]
    private static void ExportSelectedModelMeshesWithRecalculatedTangents()
    {
        ExportSelectedModelMeshes(includeBlendShapes: true, recalculateTangents: true, folderSuffix: "Generated 重新计算切线");
    }

    [MenuItem("Tools/NPR/模型/轻量导出所选模型 Mesh 为 .asset 并重算切线")]
    private static void ExportSelectedModelMeshesLightweight()
    {
        ExportSelectedModelMeshes(includeBlendShapes: false, recalculateTangents: true, folderSuffix: "Generated 轻量重算切线");
    }

    private static void ExportSelectedModelMeshes(bool includeBlendShapes, bool recalculateTangents, string folderSuffix)
    {
        var assetPath = GetSelectedModelAssetPath();
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("请先在 Project 里选中一个 FBX/OBJ 模型资源。");
            return;
        }

        var meshes = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var directory = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        var fileName = Path.GetFileNameWithoutExtension(assetPath);
        var exportFolder = $"{directory}/{fileName}_{folderSuffix}";

        if (!AssetDatabase.IsValidFolder(exportFolder))
        {
            var parent = directory;
            var folderName = $"{fileName}_{folderSuffix}";
            AssetDatabase.CreateFolder(parent, folderName);
        }

        int exportCount = 0;
        foreach (var asset in meshes)
        {
            if (asset is not Mesh sourceMesh)
            {
                continue;
            }

            var newMesh = includeBlendShapes
                ? Object.Instantiate(sourceMesh)
                : CloneMeshWithoutBlendShapes(sourceMesh);

            newMesh.name = sourceMesh.name;

            if (recalculateTangents)
            {
                newMesh.RecalculateTangents();
            }

            var outputPath = AssetDatabase.GenerateUniqueAssetPath($"{exportFolder}/{sourceMesh.name}_RecalcTangents.asset");
            AssetDatabase.CreateAsset(newMesh, outputPath);
            exportCount++;
            Debug.Log($"[TangentTools] 已导出: {outputPath} (includeBlendShapes={includeBlendShapes})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TangentTools] 导出完成，共 {exportCount} 个 Mesh。");
    }

    private static string GetSelectedModelAssetPath()
    {
        var obj = Selection.activeObject;
        if (obj == null)
        {
            return null;
        }

        var assetPath = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        return importer != null ? assetPath : null;
    }

    private static void ReportModelTangents(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"不是模型资源: {assetPath}");
            return;
        }

        var meshes = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var sb = new StringBuilder();
        sb.AppendLine($"[TangentTools] Asset: {assetPath}");
        sb.AppendLine($"Normals: {importer.importNormals}");
        sb.AppendLine($"Tangents: {importer.importTangents}");
        sb.AppendLine($"TangentImportSupported: {importer.isTangentImportSupported}");
        sb.AppendLine($"BlendShapeNormals: {importer.importBlendShapeNormals}");
        sb.AppendLine($"WeldVertices: {importer.weldVertices}");

        foreach (var asset in meshes)
        {
            if (asset is not Mesh mesh)
            {
                continue;
            }

            var tangents = mesh.tangents;
            var uv = mesh.uv;
            sb.AppendLine(
                $"Mesh: {mesh.name}, vertices={mesh.vertexCount}, subMeshes={mesh.subMeshCount}, uv0={uv.Length}, tangents={tangents.Length}");
        }

        Debug.Log(sb.ToString());
    }

    private static Mesh CloneMeshWithoutBlendShapes(Mesh sourceMesh)
    {
        var newMesh = new Mesh
        {
            name = sourceMesh.name,
            indexFormat = sourceMesh.indexFormat,
            vertices = sourceMesh.vertices,
            normals = sourceMesh.normals,
            tangents = sourceMesh.tangents,
            colors = sourceMesh.colors,
            colors32 = sourceMesh.colors32,
            uv = sourceMesh.uv,
            uv2 = sourceMesh.uv2,
            uv3 = sourceMesh.uv3,
            uv4 = sourceMesh.uv4,
            uv5 = sourceMesh.uv5,
            uv6 = sourceMesh.uv6,
            uv7 = sourceMesh.uv7,
            uv8 = sourceMesh.uv8,
            bindposes = sourceMesh.bindposes,
            boneWeights = sourceMesh.boneWeights,
            bounds = sourceMesh.bounds
        };

        newMesh.subMeshCount = sourceMesh.subMeshCount;
        for (int i = 0; i < sourceMesh.subMeshCount; i++)
        {
            newMesh.SetIndices(
                sourceMesh.GetIndices(i),
                sourceMesh.GetTopology(i),
                i,
                calculateBounds: false);
        }

        return newMesh;
    }
}
