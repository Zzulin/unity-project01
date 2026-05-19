using UnityEngine;

public sealed class LXIIAnimationTestDriver : MonoBehaviour
{
    public const string ModeParameter = "Mode";
    public const string ActionTriggerParameter = "ActionTrigger";

    [SerializeField] private Animator animator;
    [SerializeField] private KeyCode idleKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode runKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode actionKey = KeyCode.Alpha3;

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

    private void Start()
    {
        if (animator != null)
        {
            animator.SetInteger(ModeParameter, 0);
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        if (Input.GetKeyDown(idleKey))
        {
            animator.SetInteger(ModeParameter, 0);
        }

        if (Input.GetKeyDown(runKey))
        {
            animator.SetInteger(ModeParameter, 1);
        }

        if (Input.GetKeyDown(actionKey))
        {
            animator.SetTrigger(ActionTriggerParameter);
        }
    }
}
