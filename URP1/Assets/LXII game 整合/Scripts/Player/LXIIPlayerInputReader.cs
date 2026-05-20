using UnityEngine;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public sealed class LXIIPlayerInputReader : MonoBehaviour
{
    [SerializeField, HideInInspector] private string horizontalAxis = "Horizontal";
    [SerializeField, HideInInspector] private string verticalAxis = "Vertical";
    [SerializeField, HideInInspector] private KeyCode actionKey = KeyCode.Alpha3;
    [SerializeField, HideInInspector] private KeyCode sprintKey = KeyCode.LeftShift;

    public void Configure(string horizontalAxisName, string verticalAxisName, KeyCode action, KeyCode sprint)
    {
        horizontalAxis = string.IsNullOrWhiteSpace(horizontalAxisName) ? "Horizontal" : horizontalAxisName;
        verticalAxis = string.IsNullOrWhiteSpace(verticalAxisName) ? "Vertical" : verticalAxisName;
        actionKey = action;
        sprintKey = sprint;
    }

    public LXIIPlayerInputFrame ReadFrame()
    {
        return new LXIIPlayerInputFrame(
            new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis)),
            Input.GetKeyDown(actionKey),
            Input.GetKey(sprintKey));
    }
}
