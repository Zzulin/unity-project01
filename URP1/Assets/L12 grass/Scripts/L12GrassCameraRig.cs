using UnityEngine;

public sealed class L12GrassCameraRig : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 18f, -22f);
    [Min(0.1f)] public float followSharpness = 8f;
    [Min(0.1f)] public float lookHeight = 1.2f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        float lerp = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, lerp);
        transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * lookHeight - transform.position, Vector3.up);
    }
}
