using UnityEngine;

public sealed class L13VolumeCloudCameraRig : MonoBehaviour
{
    public Transform focus;
    public Vector3 focusPoint = new Vector3(0f, 34f, 0f);
    public float orbitDistance = 145f;
    public float yaw = -34f;
    public float pitch = 12f;
    public float orbitSpeed = 90f;
    public float panSpeed = 38f;
    public float zoomSpeed = 35f;

    private void Start()
    {
        SnapToRig();
    }

    private void OnValidate()
    {
        pitch = Mathf.Clamp(pitch, -18f, 62f);
        orbitDistance = Mathf.Clamp(orbitDistance, 28f, 260f);
    }

    private void Update()
    {
        Vector3 target = focus != null ? focus.position : focusPoint;

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -18f, 62f);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            orbitDistance = Mathf.Clamp(orbitDistance - scroll * zoomSpeed, 28f, 260f);
        }

        Vector3 right = transform.right;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 pan = Vector3.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) pan -= right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) pan += right;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) pan += forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) pan -= forward;
        if (Input.GetKey(KeyCode.E)) pan += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) pan -= Vector3.up;

        if (pan.sqrMagnitude > 0.001f)
        {
            focusPoint += pan.normalized * panSpeed * Time.deltaTime;
            target = focusPoint;
        }

        ApplyTransform(target);
    }

    public void SnapToRig()
    {
        ApplyTransform(focus != null ? focus.position : focusPoint);
    }

    private void ApplyTransform(Vector3 target)
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target - rotation * Vector3.forward * orbitDistance;
        transform.rotation = rotation;
    }
}
