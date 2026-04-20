using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ShaderTemplateEditor : Editor
{
    [MenuItem("Assets/Create/Shader/Unlit URP Shader")]
    static void UnlitURPShader()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        string templatePath = AssetDatabase.GUIDToAssetPath("a37f94b4c6176704eb3cdc5ea34d04be");
        string newPath = string.Format("{0}/New Unlit URP Shader.shader", path);
        AssetDatabase.CopyAsset(templatePath, newPath);
        AssetDatabase.ImportAsset(newPath);
    }
}
