using UnityEngine;

[DisallowMultipleComponent]
public sealed class L17RuntimeCameraMotion : MonoBehaviour
{
    [Header("Runtime Navigation")]
    public bool enableRuntimeControls = true;
    [Range(0.5f, 12f)] public float moveSpeed = 3.2f;
    [Range(1f, 8f)] public float fastMoveMultiplier = 2.2f;
    [Range(0.5f, 8f)] public float lookSensitivity = 2.4f;
    [Range(-85f, 85f)] public float minPitch = -65f;
    [Range(-85f, 85f)] public float maxPitch = 68f;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        CacheAngles();
    }

    private void OnEnable()
    {
        CacheAngles();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || !enableRuntimeControls)
        {
            return;
        }

        RotateFromMouse();
        MoveFromKeyboard();
    }

    private void RotateFromMouse()
    {
        if (!Input.GetMouseButton(1))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
        pitch -= Input.GetAxisRaw("Mouse Y") * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void MoveFromKeyboard()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            input += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            input += Vector3.back;
        }
        if (Input.GetKey(KeyCode.D))
        {
            input += Vector3.right;
        }
        if (Input.GetKey(KeyCode.A))
        {
            input += Vector3.left;
        }
        if (Input.GetKey(KeyCode.E))
        {
            input += Vector3.up;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            input += Vector3.down;
        }

        if (input.sqrMagnitude <= 0f)
        {
            return;
        }

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? fastMoveMultiplier : 1f);
        Vector3 localMove = input.normalized * (speed * Time.deltaTime);
        transform.position += transform.rotation * localMove;
    }

    private void CacheAngles()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
