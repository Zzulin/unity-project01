using UnityEngine;

[ExecuteAlways]
public sealed class L17LocalVolumeBoundsGizmo : MonoBehaviour
{
    public Color boundsColor = new Color(1f, 0.78f, 0.28f, 0.2f);
    public Color wireColor = new Color(1f, 0.72f, 0.18f, 0.9f);

    private void OnDrawGizmos()
    {
        DrawBounds(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawBounds(true);
    }

    private void DrawBounds(bool selected)
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.color = selected ? boundsColor : new Color(boundsColor.r, boundsColor.g, boundsColor.b, boundsColor.a * 0.35f);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
