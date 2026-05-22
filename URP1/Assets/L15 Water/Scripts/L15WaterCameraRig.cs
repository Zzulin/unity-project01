using UnityEngine;

public sealed class L15WaterCameraRig : MonoBehaviour
{
    public Transform target;
    public Vector3 focusPoint = new Vector3(0f, 0f, -4f);

    [Header("Follow")]
    [Min(0.1f)] public float followSharpness = 8f;
    [Min(0.1f)] public float lookHeight = 1.15f;

    [Header("Orbit")]
    [Min(0.1f)] public float distance = 24f;
    [Min(0.1f)] public float minDistance = 5f;
    [Min(0.1f)] public float maxDistance = 64f;
    public float yaw = -28f;
    [Range(8f, 76f)] public float pitch = 31f;

    [Header("Input")]
    [Min(0.1f)] public float rotateSensitivity = 4.4f;
    [Min(0.1f)] public float zoomSensitivity = 8f;
    [Min(0.001f)] public float panSensitivity = 0.035f;
    public bool lockCursorWhileRotating = true;

    private Vector3 focusOffset;
    private bool cursorWasVisible;
    private bool isRotating;
    private CursorLockMode previousLockMode;

    private void OnEnable()
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void OnDisable()
    {
        RestoreCursor();
    }

    private void LateUpdate()
    {
        HandleInput();

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 baseFocus = target != null ? target.position : focusPoint;
        Vector3 focus = baseFocus + Vector3.up * lookHeight + focusOffset;
        Vector3 desiredPosition = focus + rotation * new Vector3(0f, 0f, -distance);
        float lerp = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, lerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position, Vector3.up), lerp);
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            cursorWasVisible = Cursor.visible;
            previousLockMode = Cursor.lockState;
            if (lockCursorWhileRotating)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxisRaw("Mouse X") * rotateSensitivity;
            pitch -= Input.GetAxisRaw("Mouse Y") * rotateSensitivity;
            pitch = Mathf.Clamp(pitch, 8f, 76f);
        }

        if (Input.GetMouseButtonUp(1))
        {
            RestoreCursor();
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSensitivity * Mathf.Max(1f, distance * 0.12f), minDistance, maxDistance);
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 right = transform.right;
            Vector3 up = Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized;
            if (up.sqrMagnitude < 0.001f)
            {
                up = Vector3.forward;
            }

            focusOffset -= right * (Input.GetAxisRaw("Mouse X") * panSensitivity * distance);
            focusOffset -= up * (Input.GetAxisRaw("Mouse Y") * panSensitivity * distance);
            focusOffset = Vector3.ClampMagnitude(focusOffset, 14f);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            yaw = -28f;
            pitch = 31f;
            distance = 24f;
            focusOffset = Vector3.zero;
        }
    }

    private void RestoreCursor()
    {
        if (!lockCursorWhileRotating || !isRotating)
        {
            return;
        }

        Cursor.visible = cursorWasVisible;
        Cursor.lockState = previousLockMode;
        isRotating = false;
    }
}
