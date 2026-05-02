using System.Collections.Generic;
using UnityEngine;

public sealed class L12GrassInteractor : MonoBehaviour
{
    private static readonly List<L12GrassInteractor> ActiveList = new List<L12GrassInteractor>(16);

    [Header("交互范围")]
    [Min(0.1f)] public float radius = 2.6f;
    [Range(0f, 2f)] public float strength = 0.8f;

    [Header("调试")]
    public Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);

    public static IReadOnlyList<L12GrassInteractor> ActiveInteractors => ActiveList;

    private void OnEnable()
    {
        if (!ActiveList.Contains(this))
        {
            ActiveList.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveList.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
