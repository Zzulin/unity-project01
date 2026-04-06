using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SetNormalsInTangent : MonoBehaviour
{
    public string NewMeshPath = "Assets/Toon/Export";

    [ContextMenu("导出共享法线模型（到切线分量）")]//右键inspector中的挂载脚本菜单执行ExportSharedNormalsToTangent方法
    void ExportSharedNormalsToTangent()
    {
        EditorCoroutineLooper.StartLoop(this, ExportSharedNormalsToTangentCo());
    }
    IEnumerator ExportSharedNormalsToTangentCo()
    {
        //获取Mesh
        Mesh mesh = new Mesh();
        if (GetComponent<SkinnedMeshRenderer>())
        {
            mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
        }
        if (GetComponent<MeshFilter>())
        {
            mesh = GetComponent<MeshFilter>().sharedMesh;
        }
        Debug.Log(mesh.name);
        yield return null;

        //声明一个Vector3数组，长度与mesh.normals一样，用于存放
        //与mesh.vertices中顶点一一对应的光滑处理后的法线值
        Vector4[] avgNormals = new Vector4[mesh.normals.Length]; // 24
        Vector3[] meshVerts = mesh.vertices; // 避免属性数组拷贝开销
        Vector3[] meshNormals = mesh.normals;

        // 优化步骤：计算每个顶点距离游戏世界原点的长度
        SortedList<float, List<int>> sl = new SortedList<float, List<int>>(); // 距离-顶点序号对应表
        for (int i = 0; i < meshVerts.Length; i++)
        {
            Vector3 v = meshVerts[i]; // 取得顶点的第i个向量
            float f = Vector3.Magnitude(v); // 计算该向量距离游戏世界的长度
            if (sl.ContainsKey(f) == false)
                sl[f] = new List<int>();
            sl[f].Add(i);
        }

        //开始一个循环，循环的次数 = mesh.normals.Length = mesh.vertices.Length = meshNormals.Length
        int len = avgNormals.Length;
        for (int i = 0; i < len; i++)
        {
            //定义一个零值法线
            Vector3 normal = meshVerts[i];

            var slIndices = sl[Vector3.Magnitude(meshVerts[i])];
            
            //遍历mesh.vertices数组，如果遍历到的值与当前序号顶点值相同，则将其对应的法线与Normal相加
            int sharedCnt = 0;
            foreach (var j in slIndices)
            {
                Vector3 vj = meshVerts[j];
                
                if (Vector3.Distance(vj, meshVerts[i])<0.01f)
                {
                    normal += meshNormals[j]; // 把邻接的顶点的法线加到总法线向量
                    sharedCnt++;
                }
            }
            //归一化Normal并将meshNormals数列对应位置赋值为Normal,到此序号为i的顶点的对应法线光滑处理完成
            //此时求得的法线为模型空间下的法线
            normal.Normalize(); // 对总法线向量进行单位化
            avgNormals[i] = normal;

            if (i % 10 != 0)
                continue;

            Debug.Log($"Processing normal {i} / {avgNormals.Length}, shared count = {sharedCnt}");
            yield return null;
        }

        //新建一个mesh，将之前mesh的所有信息copy过去
        Mesh newMesh = new Mesh();
        newMesh.vertices = mesh.vertices;
        newMesh.triangles = mesh.triangles;
        newMesh.normals = mesh.normals;
        newMesh.tangents = mesh.tangents;//avgNormals;
        newMesh.uv = mesh.uv;
        newMesh.uv2 = mesh.uv2;
        newMesh.uv3 = mesh.uv3;
        newMesh.uv4 = mesh.uv4;
        newMesh.uv5 = mesh.uv5;
        newMesh.uv6 = mesh.uv6;
        newMesh.uv7 = mesh.uv7;
        newMesh.uv8 = mesh.uv8;
        newMesh.SetUVs(7, avgNormals);
        //将新模型的颜色赋值为计算好的颜色
        //newMesh.colors = meshColors;
        newMesh.colors32 = mesh.colors32;
        newMesh.bounds = mesh.bounds;
        newMesh.indexFormat = mesh.indexFormat;
        newMesh.bindposes = mesh.bindposes;
        newMesh.boneWeights = mesh.boneWeights;
        //将新mesh保存为.asset文件，路径可以是"Assets/Character/Shader/VertexColorTest/TestMesh2.asset"                          
        AssetDatabase.CreateAsset( newMesh, $"{NewMeshPath}/{mesh.name}.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("Done: All finished!");
    }
}