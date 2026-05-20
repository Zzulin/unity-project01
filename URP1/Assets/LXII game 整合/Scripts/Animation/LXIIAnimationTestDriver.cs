using UnityEngine;

public sealed class LXIIAnimationTestDriver : MonoBehaviour
{
    public const string ModeParameter = "Mode";
    public const string ActionTriggerParameter = "ActionTrigger";

    private static readonly int ModeHash = Animator.StringToHash(ModeParameter);
    private static readonly int ActionTriggerHash = Animator.StringToHash(ActionTriggerParameter);

    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform viewReference;
    [SerializeField] private float moveSpeed = 3.4f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float inputDeadZone = 0.1f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float groundedStickVelocity = 2f;
    [SerializeField] private KeyCode actionKey = KeyCode.Alpha3;

    private float verticalVelocity;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        CacheMainCamera();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        CacheMainCamera();
    }

    private void Start()
    {
        if (animator != null)
        {
            animator.SetInteger(ModeHash, 0);
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        if (Input.GetKeyDown(actionKey))
        {
            animator.SetTrigger(ActionTriggerHash);
        }

        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 worldMoveDirection = CalculateWorldMoveDirection(moveInput);
        bool isMoving = worldMoveDirection.sqrMagnitude > inputDeadZone * inputDeadZone;

        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        MoveCharacter(worldMoveDirection);
        animator.SetInteger(ModeHash, isMoving ? 1 : 0);
    }

    public void SetViewReference(Transform newViewReference)
    {
        viewReference = newViewReference;
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

    private void MoveCharacter(Vector3 worldMoveDirection)
    {
        if (characterController == null || !characterController.enabled)
        {
            transform.position += worldMoveDirection * moveSpeed * Time.deltaTime;
            return;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -groundedStickVelocity;
        }

        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 motion = worldMoveDirection * moveSpeed;
        motion.y = verticalVelocity;

        CollisionFlags flags = characterController.Move(motion * Time.deltaTime);
        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
        {
            verticalVelocity = -groundedStickVelocity;
        }
    }
}
