using UnityEngine;

public readonly struct LXIIPlayerInputFrame
{
    public LXIIPlayerInputFrame(Vector2 move, bool actionPressed, bool sprintHeld)
    {
        Move = move;
        ActionPressed = actionPressed;
        SprintHeld = sprintHeld;
    }

    public Vector2 Move { get; }
    public bool ActionPressed { get; }
    public bool SprintHeld { get; }
}

public readonly struct LXIIPlayerLocomotionState
{
    public LXIIPlayerLocomotionState(Vector3 worldMoveDirection, bool isMoving, bool isSprinting, float moveSpeed)
    {
        WorldMoveDirection = worldMoveDirection;
        IsMoving = isMoving;
        IsSprinting = isSprinting;
        MoveSpeed = moveSpeed;
    }

    public Vector3 WorldMoveDirection { get; }
    public bool IsMoving { get; }
    public bool IsSprinting { get; }
    public float MoveSpeed { get; }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class LXIIPlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string verticalAxis = "Vertical";
    [SerializeField] private KeyCode actionKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.4f;
    [SerializeField] private float sprintSpeed = 5.2f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float inputDeadZone = 0.1f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float groundedStickVelocity = 2f;

    [Header("References")]
    [SerializeField] private Transform viewReference;
    [SerializeField] private bool showInternalComponents;

    [SerializeField, HideInInspector] private CharacterController characterController;
    [SerializeField, HideInInspector] private Animator animator;
    [SerializeField, HideInInspector] private LXIIPlayerInputReader inputReader;
    [SerializeField, HideInInspector] private LXIIPlayerMotor motor;
    [SerializeField, HideInInspector] private LXIIPlayerAnimationDriver animationDriver;

    private void Reset()
    {
        EnsureDependencies();
        CacheMainCamera();
        ApplyConfiguration();
    }

    private void Awake()
    {
        EnsureDependencies();
        CacheMainCamera();
        ApplyConfiguration();
    }

    private void Start()
    {
        animationDriver?.Initialize();
    }

    private void Update()
    {
        if (viewReference == null)
        {
            CacheMainCamera();
            ApplyConfiguration();
        }

        LXIIPlayerInputFrame inputFrame = inputReader.ReadFrame();
        LXIIPlayerLocomotionState locomotionState = motor.Tick(inputFrame.Move, inputFrame.SprintHeld, Time.deltaTime);
        animationDriver.ApplyLocomotion(locomotionState);

        if (inputFrame.ActionPressed)
        {
            animationDriver.TriggerAction();
        }
    }

    public void SetViewReference(Transform newViewReference)
    {
        viewReference = newViewReference;
        ApplyConfiguration();
    }

    public void ConfigureForScene(Transform newViewReference)
    {
        viewReference = newViewReference;
        EnsureDependencies();
        ApplyConfiguration();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        inputDeadZone = Mathf.Clamp01(inputDeadZone);
        gravity = Mathf.Max(0f, gravity);
        groundedStickVelocity = Mathf.Max(0f, groundedStickVelocity);

        CacheExistingDependencies();
        if (inputReader != null && motor != null && animationDriver != null)
        {
            ApplyConfiguration();
        }
    }
#endif

    private void EnsureDependencies()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        inputReader = GetOrAdd<LXIIPlayerInputReader>();
        motor = GetOrAdd<LXIIPlayerMotor>();
        animationDriver = GetOrAdd<LXIIPlayerAnimationDriver>();
    }

    private void CacheExistingDependencies()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        inputReader = GetComponent<LXIIPlayerInputReader>();
        motor = GetComponent<LXIIPlayerMotor>();
        animationDriver = GetComponent<LXIIPlayerAnimationDriver>();
    }

    private T GetOrAdd<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private void ApplyConfiguration()
    {
        if (inputReader == null || motor == null || animationDriver == null)
        {
            return;
        }

        inputReader.Configure(horizontalAxis, verticalAxis, actionKey, sprintKey);
        motor.Configure(characterController, viewReference, walkSpeed, sprintSpeed, rotationSpeed, inputDeadZone, gravity, groundedStickVelocity);
        animationDriver.Configure(animator);
        ApplyInternalVisibility();
    }

    private void ApplyInternalVisibility()
    {
        HideFlags flags = showInternalComponents ? HideFlags.None : HideFlags.HideInInspector;
        if (inputReader != null)
        {
            inputReader.hideFlags = flags;
        }

        if (motor != null)
        {
            motor.hideFlags = flags;
        }

        if (animationDriver != null)
        {
            animationDriver.hideFlags = flags;
        }
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
}
