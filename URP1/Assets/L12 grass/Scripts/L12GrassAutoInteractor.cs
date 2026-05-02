using UnityEngine;

public sealed class L12GrassAutoInteractor : MonoBehaviour
{
    public Vector3 center;
    [Min(0.1f)] public float pathRadius = 18f;
    public float angularSpeed = 0.35f;
    public float phase;

    private void Update()
    {
        float t = Time.time * angularSpeed + phase;
        transform.position = center + new Vector3(Mathf.Cos(t) * pathRadius, 0.45f, Mathf.Sin(t * 0.78f) * pathRadius);
    }
}
