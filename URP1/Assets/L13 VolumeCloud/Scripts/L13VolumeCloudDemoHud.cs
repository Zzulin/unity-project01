using UnityEngine;

public sealed class L13VolumeCloudDemoHud : MonoBehaviour
{
    public L13VolumeCloudController cloud;
    public Light sunLight;

    private readonly L13CloudPreset softPreset = new L13CloudPreset(2.0f, 0.48f, 0.58f, 6.2f, 30f, 0.32f, 0.22f, 0.24f, 0.54f, 2.1f, 2.3f, 0.48f, -0.2f, 1.1f, 0.9f, 4.2f, 36, 3);
    private readonly L13CloudPreset ueLikePreset = new L13CloudPreset(3.2f, 0.6f, 0.72f, 10.5f, 38f, 0.4f, 0.18f, 0.22f, 0.62f, 2.6f, 2.9f, 0.58f, -0.28f, 1.55f, 1.15f, 7f, 48, 4);
    private readonly L13CloudPreset stormPreset = new L13CloudPreset(4.2f, 0.68f, 0.84f, 9.2f, 48f, 0.54f, 0.12f, 0.28f, 0.72f, 3.2f, 3.6f, 0.68f, -0.34f, 2.0f, 1.45f, 10.5f, 64, 5);

    private void Update()
    {
        if (cloud == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) cloud.ApplyPreset(softPreset);
        if (Input.GetKeyDown(KeyCode.Alpha2)) cloud.ApplyPreset(ueLikePreset);
        if (Input.GetKeyDown(KeyCode.Alpha3)) cloud.ApplyPreset(stormPreset);

        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            cloud.coverage = Mathf.Clamp01(cloud.coverage - Time.deltaTime * 0.25f);
        }

        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
        {
            cloud.coverage = Mathf.Clamp01(cloud.coverage + Time.deltaTime * 0.25f);
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            cloud.stepCount = Mathf.Max(24, cloud.stepCount - 8);
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            cloud.stepCount = Mathf.Min(96, cloud.stepCount + 8);
        }

        if (sunLight != null)
        {
            if (Input.GetKey(KeyCode.Comma))
            {
                sunLight.transform.Rotate(Vector3.right, -18f * Time.deltaTime, Space.World);
            }

            if (Input.GetKey(KeyCode.Period))
            {
                sunLight.transform.Rotate(Vector3.right, 18f * Time.deltaTime, Space.World);
            }
        }
    }

    private void OnGUI()
    {
        if (cloud == null)
        {
            return;
        }

        const int width = 430;
        GUILayout.BeginArea(new Rect(18, 18, width, 210), GUI.skin.box);
        GUILayout.Label("L13 Raymarched Volume Cloud");
        GUILayout.Label($"Coverage +/-: {cloud.coverage:0.00}    Density: {cloud.density:0.0}");
        GUILayout.Label($"View Steps [ ]: {cloud.stepCount}");
        GUILayout.Label($"Wind: {cloud.windSpeed:0.0}    Silver: {cloud.silverIntensity:0.0}");
        GUILayout.Space(6);
        GUILayout.Label("1 Soft Cumulus   2 UE-like Hero   3 Storm Wall");
        GUILayout.Label("Right mouse orbit, wheel zoom, WASD/QE pan");
        GUILayout.Label(", . rotate sun elevation");
        GUILayout.EndArea();
    }
}
