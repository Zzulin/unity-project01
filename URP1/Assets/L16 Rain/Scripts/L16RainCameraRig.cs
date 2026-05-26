using UnityEngine;

public sealed class L16RainCameraRig : MonoBehaviour
{
    public Transform target;
    public Vector3 focusPoint = new Vector3(0f, 1.6f, 0f);

    [Header("Orbit")]
    [Min(0.1f)] public float distance = 18f;
    [Min(0.1f)] public float minDistance = 5f;
    [Min(0.1f)] public float maxDistance = 46f;
    public float yaw = -34f;
    [Range(6f, 74f)] public float pitch = 22f;
    [Min(0.1f)] public float followSharpness = 8f;

    [Header("Input")]
    [Min(0.1f)] public float rotateSensitivity = 4.2f;
    [Min(0.1f)] public float zoomSensitivity = 7f;
    [Min(0.001f)] public float panSensitivity = 0.032f;
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

        Vector3 baseFocus = target != null ? target.position : focusPoint;
        Vector3 focus = baseFocus + focusOffset;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = focus + rotation * new Vector3(0f, 0f, -distance);
        float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position, Vector3.up), t);
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
            pitch = Mathf.Clamp(pitch, 6f, 74f);
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
            focusOffset = Vector3.ClampMagnitude(focusOffset, 18f);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            yaw = -34f;
            pitch = 22f;
            distance = 18f;
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
