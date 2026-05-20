using UnityEngine;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public sealed class LXIIPlayerAnimationDriver : MonoBehaviour
{
    public const string ModeParameter = "Mode";
    public const string ActionTriggerParameter = "ActionTrigger";

    private static readonly int ModeHash = Animator.StringToHash(ModeParameter);
    private static readonly int ActionTriggerHash = Animator.StringToHash(ActionTriggerParameter);

    [SerializeField, HideInInspector] private Animator animator;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void Initialize()
    {
        if (animator != null)
        {
            animator.SetInteger(ModeHash, 0);
        }
    }

    public void Configure(Animator newAnimator)
    {
        animator = newAnimator != null ? newAnimator : GetComponent<Animator>();
    }

    public void ApplyLocomotion(LXIIPlayerLocomotionState locomotionState)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetInteger(ModeHash, locomotionState.IsMoving ? 1 : 0);
    }

    public void TriggerAction()
    {
        if (animator != null)
        {
            animator.SetTrigger(ActionTriggerHash);
        }
    }
}
