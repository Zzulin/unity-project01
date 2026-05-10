using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BlendShapesController : MonoBehaviour
{
    public SkinnedMeshRenderer FaceRenderer;
    public List<BlendShapeState> BlendShapes = new List<BlendShapeState>();

    [Serializable]
    public class BlendShapeState
    {
        public string name;
        [Range(0, 100)]
        public float weight;
        public int index { get; }


        public BlendShapeState(string _name, int _index)
        {
            name = _name;
            index = _index;
        }
    }
    private void OnEnable()
    {
        if (FaceRenderer == null)
            return;

        BlendShapes.Clear();
        var mesh = FaceRenderer.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            BlendShapeState bs = new(mesh.GetBlendShapeName(i), i);
            bs.weight = FaceRenderer.GetBlendShapeWeight(i);
            BlendShapes.Add(bs);
        }
    }

    void Start()
    {
        
    }

    /// <summary>
    /// 设置权重
    /// </summary>
    /// <param name="faceid">脸部变形动画序号从0开始</param>
    /// <param name="weight">权重0-100</param>
    public void SetWeight(int faceid, float weight)
    {
        if (faceid < 0 || faceid >= BlendShapes.Count)
        {
            Debug.LogError("输入下标错误或者超出变形动画上限");
            return;
        }
            
        FaceRenderer.SetBlendShapeWeight(faceid, weight);
    }
    
}

#if UNITY_EDITOR
[CustomEditor(typeof(BlendShapesController))]
public class BlendShapesControllerEditor : Editor 
{
    public SerializedProperty FaceRenderer;
    public SerializedProperty BlendShapes;
    public SerializedProperty m_Script;
    BlendShapesController blendshapes;

    private void OnEnable()
    {
        blendshapes = target as BlendShapesController;
        m_Script = serializedObject.FindProperty("m_Script");
        FaceRenderer = serializedObject.FindProperty("FaceRenderer");
        BlendShapes = serializedObject.FindProperty("BlendShapes");
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(m_Script, true);
        }
        EditorGUILayout.PropertyField(FaceRenderer);
        EditorGUILayout.PropertyField(BlendShapes);

        serializedObject.ApplyModifiedProperties();

        EditorGUI.EndChangeCheck();
        UpdateBlendShapeState();


    }

    void UpdateBlendShapeState()
    {
        for(int i = 0; i < blendshapes.BlendShapes.Count; i++)
        {
            blendshapes.FaceRenderer.SetBlendShapeWeight(i, blendshapes.BlendShapes[i].weight);
        }
    }

}


#endif
