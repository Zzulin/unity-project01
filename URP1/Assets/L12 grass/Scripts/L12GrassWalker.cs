using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class L12GrassWalker : MonoBehaviour
{
    [Min(0.1f)] public float moveSpeed = 7f;
    [Min(0.1f)] public float sprintMultiplier = 1.7f;
    [Min(1f)] public float fieldLimit = 42f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        Vector3 motion = input * (speed * Time.deltaTime);
        controller.Move(motion);

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, -fieldLimit, fieldLimit);
        position.z = Mathf.Clamp(position.z, -fieldLimit, fieldLimit);
        position.y = 0.92f;
        transform.position = position;

        if (input.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(input, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);
        }
    }
}
