using UnityEngine;

public sealed class L14SnowCharacterRig : MonoBehaviour
{
    public L14SnowWalker walker;
    public Transform leftFoot;
    public Transform rightFoot;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftArm;
    public Transform rightArm;

    [Header("身体锚点")]
    public Vector3 leftHip = new Vector3(-0.24f, 0.24f, 0.03f);
    public Vector3 rightHip = new Vector3(0.24f, 0.24f, 0.03f);
    public Vector3 leftShoulder = new Vector3(-0.44f, 0.88f, 0.02f);
    public Vector3 rightShoulder = new Vector3(0.44f, 0.88f, 0.02f);

    [Header("肢体")]
    [Min(0.02f)] public float legRadius = 0.11f;
    [Min(0.02f)] public float armRadius = 0.075f;
    public float armLength = 0.58f;
    public float armSwing = 0.34f;

    private void LateUpdate()
    {
        if (walker == null)
        {
            walker = GetComponent<L14SnowWalker>();
        }

        PositionCapsule(leftLeg, transform.TransformPoint(leftHip), FootAnchor(leftFoot), legRadius);
        PositionCapsule(rightLeg, transform.TransformPoint(rightHip), FootAnchor(rightFoot), legRadius);

        float speed01 = walker != null ? Mathf.Clamp01(walker.CurrentSpeed / Mathf.Max(walker.moveSpeed * walker.sprintMultiplier, 0.01f)) : 0f;
        float phase = Time.time * (walker != null ? walker.stepFrequency : 6.8f);
        Vector3 forward = transform.forward;
        Vector3 down = -transform.up;
        Vector3 leftHand = transform.TransformPoint(leftShoulder) + down * armLength + forward * (Mathf.Sin(phase + Mathf.PI) * armSwing * speed01);
        Vector3 rightHand = transform.TransformPoint(rightShoulder) + down * armLength + forward * (Mathf.Sin(phase) * armSwing * speed01);
        PositionCapsule(leftArm, transform.TransformPoint(leftShoulder), leftHand, armRadius);
        PositionCapsule(rightArm, transform.TransformPoint(rightShoulder), rightHand, armRadius);
    }

    private Vector3 FootAnchor(Transform foot)
    {
        if (foot == null)
        {
            return transform.position + Vector3.down * 0.72f;
        }

        return foot.position + transform.up * 0.18f;
    }

    private static void PositionCapsule(Transform capsule, Vector3 a, Vector3 b, float radius)
    {
        if (capsule == null)
        {
            return;
        }

        Vector3 axis = b - a;
        float length = axis.magnitude;
        if (length <= 0.001f)
        {
            return;
        }

        capsule.position = (a + b) * 0.5f;
        capsule.rotation = Quaternion.FromToRotation(Vector3.up, axis / length);
        capsule.localScale = new Vector3(radius, Mathf.Max(length * 0.5f, radius), radius);
    }
}
