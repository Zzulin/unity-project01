using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetStencilRef : MonoBehaviour
{
    // Start is called before the first frame update
    public int StencilRef;
    void Start()
    {
        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.material.SetInt("_StencilRef", StencilRef);
        }
    }
}
