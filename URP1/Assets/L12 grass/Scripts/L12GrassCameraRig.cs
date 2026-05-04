using UnityEngine;

public sealed class L12GrassCameraRig : MonoBehaviour
{
    public Transform target;

    [Header("跟随")]
    [Min(0.1f)] public float followSharpness = 8f;
    [Min(0.1f)] public float lookHeight = 1.2f;

    [Header("轨道视角")]
    [Min(0.1f)] public float distance = 30f;
    [Min(0.1f)] public float minDistance = 8f;
    [Min(0.1f)] public float maxDistance = 70f;
    public float yaw = 0f;
    [Range(5f, 85f)] public float pitch = 36f;

    [Header("输入")]
    [Min(0.1f)] public float rotateSensitivity = 4.5f;
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
        if (target == null)
        {
            return;
        }

        HandleInput();

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * lookHeight + focusOffset;
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
            pitch = Mathf.Clamp(pitch, 8f, 78f);
        }

        if (Input.GetMouseButtonUp(1))
        {
            RestoreCursor();
            isRotating = false;
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
            focusOffset = Vector3.ClampMagnitude(focusOffset, 12f);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            yaw = 0f;
            pitch = 36f;
            distance = 30f;
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
