using UnityEngine;

public sealed class L14SnowWalker : MonoBehaviour
{
    [Min(0.1f)] public float moveSpeed = 6.5f;
    [Min(0.1f)] public float sprintMultiplier = 1.65f;
    [Min(1f)] public float fieldLimit = 43f;
    [Min(0f)] public float contactHeight = 0.34f;
    [Min(0.1f)] public float jumpHeight = 2.2f;
    [Min(0.1f)] public float gravity = 18f;

    [Header("脚印")]
    public Transform leftFoot;
    public Transform rightFoot;
    [Min(0.05f)] public float footSpacing = 0.34f;
    [Min(0f)] public float stepLength = 0.48f;
    [Min(0f)] public float footHeight = 0.06f;
    [Min(0.1f)] public float stepFrequency = 6.8f;

    private float stepPhase;
    private Vector3 previousPosition;
    private L14SnowInteractor contactInteractor;
    private float verticalVelocity;

    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; } = true;

    private void OnEnable()
    {
        previousPosition = transform.position;
    }

    private void Update()
    {
        if (contactInteractor == null)
        {
            contactInteractor = GetComponent<L14SnowInteractor>();
        }

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        Vector3 motion = input * (speed * Time.deltaTime);
        transform.position += motion;

        Vector3 position = transform.position;
        bool wasGrounded = IsGrounded;
        if (wasGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            IsGrounded = false;
        }

        if (!IsGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
            position.y += verticalVelocity * Time.deltaTime;
            if (position.y <= contactHeight)
            {
                position.y = contactHeight;
                verticalVelocity = 0f;
                IsGrounded = true;
            }
        }
        else
        {
            position.y = contactHeight;
            verticalVelocity = 0f;
        }

        position.x = Mathf.Clamp(position.x, -fieldLimit, fieldLimit);
        position.z = Mathf.Clamp(position.z, -fieldLimit, fieldLimit);
        transform.position = position;

        if (contactInteractor != null)
        {
            contactInteractor.canStamp = IsGrounded;
        }

        CurrentSpeed = (transform.position - previousPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        previousPosition = transform.position;

        if (input.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(input, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);
            stepPhase += Time.deltaTime * stepFrequency * Mathf.Lerp(0.75f, 1.35f, Mathf.Clamp01(CurrentSpeed / (moveSpeed * sprintMultiplier)));
        }
        else
        {
            stepPhase = Mathf.Lerp(stepPhase, 0f, Time.deltaTime * 6f);
        }

        UpdateFoot(leftFoot, -footSpacing, Mathf.Sin(stepPhase) * stepLength);
        UpdateFoot(rightFoot, footSpacing, -Mathf.Sin(stepPhase) * stepLength);
    }

    private void UpdateFoot(Transform foot, float lateralOffset, float forwardOffset)
    {
        if (foot == null)
        {
            return;
        }

        Vector3 local = new Vector3(lateralOffset, -0.9f + footHeight, forwardOffset);
        foot.position = transform.TransformPoint(local);
        foot.rotation = transform.rotation;
    }
}
