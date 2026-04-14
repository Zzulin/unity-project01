using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SetNormalsInTangentSmoothNormal : MonoBehaviour
{
    public string NewMeshPath = "Assets/Toon/Export";

    [ContextMenu("导出共享法线模型（到NV7）")]//右键inspector中的挂载脚本菜单执行ExportSharedNormalsToTangent方法
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
        yield return null;//等待一帧，确保mesh加载完成

        //声明一个Vector3数组，长度与mesh.normals一样，用于存放
        //与mesh.vertices中顶点一一对应的光滑处理后的法线值
        Vector4[] avgNormals = new Vector4[mesh.normals.Length]; // 24
        Vector3[] meshVerts = mesh.vertices; // 避免属性数组拷贝开销
        Vector3[] meshNormals = mesh.normals;

        // 优化步骤：计算每个顶点到模型本地原点的长度
        SortedList<float, List<int>> sl = new SortedList<float, List<int>>(); // 距离-顶点序号对应表
        for (int i = 0; i < meshVerts.Length; i++)
        {
            Vector3 v = meshVerts[i]; // 取得顶点的第i个向量
            float f = Vector3.Magnitude(v); // 计算该向量距离模型本地原点的长度
            if (sl.ContainsKey(f) == false)
                sl[f] = new List<int>();
            sl[f].Add(i);
        }

        //开始一个循环，循环的次数 = mesh.normals.Length = mesh.vertices.Length = meshNormals.Length
        int len = avgNormals.Length;
        for (int i = 0; i < len; i++)
        {
            //定义一个零值法线
            Vector3 normal = Vector3.zero;

            var slIndices = sl[Vector3.Magnitude(meshVerts[i])];
            
            //遍历mesh.vertices数组，如果遍历到的值与当前序号顶点值相同，则将其对应的法线与Normal相加
            int sharedCnt = 0;
            foreach (var j in slIndices)
            {
                if (Vector3.Distance(meshVerts[j], meshVerts[i])<0.01f)
                {
                    normal += meshNormals[j]; // 把邻接的顶点的法线加到总法线向量
                    sharedCnt++;//统计共享的顶点数量
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

        // 直接克隆原 mesh，完整保留 submeshes、blendshapes、skin/bone 等数据
        Mesh newMesh = Instantiate(mesh);
        newMesh.name = mesh.name;
        newMesh.SetUVs(7, avgNormals);// channel 7 对应 UV8
        //将新mesh保存为.asset文件，路径可以是"Assets/Character/Shader/VertexColorTest/TestMesh2.asset"                          
        AssetDatabase.CreateAsset( newMesh, $"{NewMeshPath}/{mesh.name}.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("Done: All finished!");
    }
}
