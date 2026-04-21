using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SetNormalsInTangentSmoothNormal : MonoBehaviour
{
    public string NewMeshPath = "Assets/Toon/Export";
    [Min(0.000001f)] public float PositionTolerance = 0.0001f;
    [Range(-1f, 1f)] public float MinNormalDot = -0.25f;
    public bool StoreTangentSpaceNormal = true;
    public bool AssignExportedMeshToRenderer = true;

    [ContextMenu("导出共享法线模型（到UV8）")]
    void ExportSharedNormalsToTangent()
    {
        EditorCoroutineLooper.StartLoop(this, ExportSharedNormalsToTangentCo());
    }

    IEnumerator ExportSharedNormalsToTangentCo()
    {
        Mesh mesh = null;
        SkinnedMeshRenderer skinnedMeshRenderer = null;
        MeshFilter meshFilter = null;
        if (TryGetComponent(out skinnedMeshRenderer))
        {
            mesh = skinnedMeshRenderer.sharedMesh;
        }
        else if (TryGetComponent(out meshFilter))
        {
            mesh = meshFilter.sharedMesh;
        }

        if (mesh == null)
        {
            Debug.LogError("No Mesh found on this GameObject.");
            yield break;
        }

        Debug.Log(mesh.name);
        yield return null;

        Vector3[] meshVerts = mesh.vertices;
        Vector3[] meshNormals = mesh.normals;
        Vector3[] avgNormals = new Vector3[meshNormals.Length];
        int[] avgCounts = new int[meshNormals.Length];

        // Per-submesh smoothing avoids mixing front/back duplicate parts from different material slots.
        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
        {
            int[] indices = mesh.GetIndices(subMeshIndex);
            Dictionary<Vector3Int, List<int>> positionGroups = new Dictionary<Vector3Int, List<int>>();

            foreach (int index in indices.Distinct())
            {
                Vector3Int key = Quantize(meshVerts[index], PositionTolerance);
                if (!positionGroups.TryGetValue(key, out List<int> group))
                {
                    group = new List<int>();
                    positionGroups.Add(key, group);
                }

                group.Add(index);
            }

            int processed = 0;
            int total = positionGroups.Values.Sum(group => group.Count);
            foreach (List<int> group in positionGroups.Values)
            {
                foreach (int index in group)
                {
                    Vector3 sourceNormal = meshNormals[index].normalized;
                    Vector3 normal = Vector3.zero;
                    int sharedCnt = 0;

                    foreach (int otherIndex in group)
                    {
                        Vector3 otherNormal = meshNormals[otherIndex].normalized;
                        if (Vector3.Dot(sourceNormal, otherNormal) < MinNormalDot)
                        {
                            continue;
                        }

                        normal += otherNormal;
                        sharedCnt++;
                    }

                    if (normal.sqrMagnitude < 0.000001f)
                    {
                        normal = sourceNormal;
                    }
                    else
                    {
                        normal.Normalize();
                    }

                    avgNormals[index] += normal;
                    avgCounts[index]++;

                    processed++;
                    if (processed % 200 != 0)
                    {
                        continue;
                    }

                    Debug.Log($"Processing submesh {subMeshIndex + 1} / {mesh.subMeshCount}, vertex {processed} / {total}, shared count = {sharedCnt}");
                    yield return null;
                }
            }
        }

        for (int i = 0; i < avgNormals.Length; i++)
        {
            if (avgCounts[i] == 0 || avgNormals[i].sqrMagnitude < 0.000001f)
            {
                avgNormals[i] = meshNormals[i];
            }
            else
            {
                avgNormals[i].Normalize();
            }
        }

        Vector3[] storedNormals = StoreTangentSpaceNormal
            ? ConvertObjectNormalsToTangentSpace(avgNormals, meshNormals, mesh.tangents)
            : avgNormals;

        Mesh newMesh = Instantiate(mesh);
        newMesh.name = mesh.name;
        newMesh.SetUVs(7, storedNormals); // Mesh UV channel 7 maps to shader TEXCOORD7 / UV8.

        Directory.CreateDirectory(NewMeshPath);
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{NewMeshPath}/{mesh.name}.asset");
        AssetDatabase.CreateAsset(newMesh, assetPath);
        AssetDatabase.SaveAssets();

        if (AssignExportedMeshToRenderer)
        {
            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.sharedMesh = newMesh;
            }
            else if (meshFilter != null)
            {
                meshFilter.sharedMesh = newMesh;
            }
        }

        Debug.Log($"Done: All finished! Saved to {assetPath}");
    }

    static Vector3Int Quantize(Vector3 position, float tolerance)
    {
        float invTolerance = 1f / tolerance;
        return new Vector3Int(
            Mathf.RoundToInt(position.x * invTolerance),
            Mathf.RoundToInt(position.y * invTolerance),
            Mathf.RoundToInt(position.z * invTolerance));
    }

    static Vector3[] ConvertObjectNormalsToTangentSpace(Vector3[] objectNormals, Vector3[] meshNormals, Vector4[] meshTangents)
    {
        Vector3[] tangentSpaceNormals = new Vector3[objectNormals.Length];

        if (meshTangents == null || meshTangents.Length != objectNormals.Length)
        {
            Debug.LogWarning("Mesh tangents are missing or invalid. Smooth normals are stored in object space instead.");
            objectNormals.CopyTo(tangentSpaceNormals, 0);
            return tangentSpaceNormals;
        }

        for (int i = 0; i < objectNormals.Length; i++)
        {
            Vector3 normal = meshNormals[i].normalized;
            Vector3 tangent = new Vector3(meshTangents[i].x, meshTangents[i].y, meshTangents[i].z).normalized;
            Vector3 bitangent = Vector3.Cross(normal, tangent).normalized * meshTangents[i].w;

            Vector3 objectNormal = objectNormals[i].normalized;
            tangentSpaceNormals[i] = new Vector3(
                Vector3.Dot(objectNormal, tangent),
                Vector3.Dot(objectNormal, bitangent),
                Vector3.Dot(objectNormal, normal)).normalized;
        }

        return tangentSpaceNormals;
    }
}
