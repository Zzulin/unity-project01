using UnityEngine;

public sealed class LXIIThirdPersonCameraFollow : MonoBehaviour
{
    private const string DefaultTargetName = "LXII Nilou Player";

    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.45f, 0f);
    [SerializeField] private float distance = 3.6f;
    [SerializeField] private float minDistance = 2.2f;
    [SerializeField] private float maxDistance = 5.2f;
    [SerializeField] private float yaw = 180f;
    [SerializeField] private float pitch = 12f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 45f;
    [SerializeField] private float yawSensitivity = 180f;
    [SerializeField] private float pitchSensitivity = 120f;
    [SerializeField] private float zoomSensitivity = 1.2f;
    [SerializeField] private float positionSharpness = 10f;
    [SerializeField] private float rotationSharpness = 14f;
    [SerializeField] private bool requireRightMouseButton = true;

    private bool initialized;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        initialized = false;
    }

    public void SnapBehindTarget()
    {
        if (target == null)
        {
            return;
        }

        yaw = target.eulerAngles.y;
        initialized = true;
        ApplyCameraPose(1f, true);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find(DefaultTargetName);
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target == null)
        {
            return;
        }

        if (!initialized)
        {
            yaw = target.eulerAngles.y;
            initialized = true;
        }

        bool canOrbit = !requireRightMouseButton || Input.GetMouseButton(1);
        if (canOrbit)
        {
            yaw += Input.GetAxis("Mouse X") * yawSensitivity * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * pitchSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * zoomSensitivity, minDistance, maxDistance);
        ApplyCameraPose(Time.deltaTime, false);
    }

    private void ApplyCameraPose(float deltaTime, bool snap)
    {
        Vector3 pivot = target.position + followOffset;
        Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = pivot - desiredRotation * Vector3.forward * distance;

        if (snap)
        {
            transform.SetPositionAndRotation(desiredPosition, desiredRotation);
            return;
        }

        float positionLerp = 1f - Mathf.Exp(-positionSharpness * deltaTime);
        float rotationLerp = 1f - Mathf.Exp(-rotationSharpness * deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationLerp);
    }
}
