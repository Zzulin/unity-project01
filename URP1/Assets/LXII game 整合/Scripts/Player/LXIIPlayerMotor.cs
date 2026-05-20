using UnityEngine;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public sealed class LXIIPlayerMotor : MonoBehaviour
{
    [SerializeField, HideInInspector] private CharacterController characterController;
    [SerializeField, HideInInspector] private Transform viewReference;
    [SerializeField, HideInInspector] private float walkSpeed = 3.4f;
    [SerializeField, HideInInspector] private float sprintSpeed = 5.2f;
    [SerializeField, HideInInspector] private float rotationSpeed = 540f;
    [SerializeField, HideInInspector] private float inputDeadZone = 0.1f;
    [SerializeField, HideInInspector] private float gravity = 20f;
    [SerializeField, HideInInspector] private float groundedStickVelocity = 2f;

    private float verticalVelocity;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
        CacheMainCamera();
    }

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        CacheMainCamera();
    }

    public void Configure(
        CharacterController newCharacterController,
        Transform newViewReference,
        float newWalkSpeed,
        float newSprintSpeed,
        float newRotationSpeed,
        float newInputDeadZone,
        float newGravity,
        float newGroundedStickVelocity)
    {
        characterController = newCharacterController != null ? newCharacterController : GetComponent<CharacterController>();
        viewReference = newViewReference;
        walkSpeed = Mathf.Max(0f, newWalkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, newSprintSpeed);
        rotationSpeed = Mathf.Max(0f, newRotationSpeed);
        inputDeadZone = Mathf.Clamp01(newInputDeadZone);
        gravity = Mathf.Max(0f, newGravity);
        groundedStickVelocity = Mathf.Max(0f, newGroundedStickVelocity);
    }

    public LXIIPlayerLocomotionState Tick(Vector2 moveInput, bool sprintHeld, float deltaTime)
    {
        Vector3 worldMoveDirection = CalculateWorldMoveDirection(moveInput);
        bool isMoving = worldMoveDirection.sqrMagnitude > inputDeadZone * inputDeadZone;
        float currentMoveSpeed = sprintHeld && isMoving ? sprintSpeed : walkSpeed;

        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        MoveCharacter(worldMoveDirection, currentMoveSpeed, deltaTime);
        return new LXIIPlayerLocomotionState(worldMoveDirection, isMoving, sprintHeld && isMoving, currentMoveSpeed);
    }

    private void CacheMainCamera()
    {
        if (viewReference != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            viewReference = mainCamera.transform;
        }
    }

    private Vector3 CalculateWorldMoveDirection(Vector2 moveInput)
    {
        CacheMainCamera();

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (viewReference != null)
        {
            forward = Vector3.ProjectOnPlane(viewReference.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(viewReference.right, Vector3.up).normalized;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }

    private void MoveCharacter(Vector3 worldMoveDirection, float currentMoveSpeed, float deltaTime)
    {
        if (characterController == null || !characterController.enabled)
        {
            transform.position += worldMoveDirection * currentMoveSpeed * deltaTime;
            return;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -groundedStickVelocity;
        }

        verticalVelocity -= gravity * deltaTime;

        Vector3 motion = worldMoveDirection * currentMoveSpeed;
        motion.y = verticalVelocity;

        CollisionFlags flags = characterController.Move(motion * deltaTime);
        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
        {
            verticalVelocity = -groundedStickVelocity;
        }
    }
}
