using UnityEngine;

public sealed class L14SnowAutoInteractor : MonoBehaviour
{
    public Vector3 center;
    [Min(0.1f)] public float pathRadius = 18f;
    public Vector2 pathScale = new Vector2(1f, 0.65f);
    public float angularSpeed = 0.32f;
    public float phase;
    public bool rotateToVelocity = true;

    private Vector3 previousPosition;

    private void OnEnable()
    {
        previousPosition = transform.position;
    }

    private void Update()
    {
        float t = Time.time * angularSpeed + phase;
        Vector3 next = center + new Vector3(
            Mathf.Cos(t) * pathRadius * pathScale.x,
            0f,
            Mathf.Sin(t * 0.73f + phase * 0.37f) * pathRadius * pathScale.y);

        if (rotateToVelocity)
        {
            Vector3 velocity = next - previousPosition;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(velocity.normalized, Vector3.up),
                    Time.deltaTime * 8f);
            }
        }

        transform.position = next;
        previousPosition = next;
    }
}
