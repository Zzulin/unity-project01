using System.Collections.Generic;
using UnityEngine;

public sealed class L14SnowInteractor : MonoBehaviour
{
    private static readonly List<L14SnowInteractor> ActiveList = new List<L14SnowInteractor>(32);

    [Header("压痕")]
    [Min(0.05f)] public float radius = 0.9f;
    [Range(0f, 2f)] public float strength = 1f;
    [Range(0f, 2f)] public float ridgeStrength = 0.8f;
    [Range(0.35f, 3f)] public float hardness = 1.25f;
    public bool canStamp = true;

    [Header("调试")]
    public Color gizmoColor = new Color(0.42f, 0.72f, 1f, 0.32f);

    public static IReadOnlyList<L14SnowInteractor> ActiveInteractors => ActiveList;

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
