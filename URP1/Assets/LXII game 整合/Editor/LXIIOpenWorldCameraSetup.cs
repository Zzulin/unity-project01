using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LXIIOpenWorldCameraSetup
{
    private const string ScenePath = "Assets/LXII game 整合/game.unity";
    private const string PlayerName = "LXII Nilou Player";

    [MenuItem("Tools/LXII/Setup Open World Camera Framing")]
    public static void SetupOpenWorldCameraFraming()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = GameObject.Find(PlayerName);
        Camera mainCamera = Camera.main;
        if (player == null || mainCamera == null)
        {
            Debug.LogError("[LXII] 大世界相机构图失败：未找到玩家或 Main Camera。");
            return;
        }

        LXIIThirdPersonCameraFollow follow = mainCamera.GetComponent<LXIIThirdPersonCameraFollow>();
        if (follow == null)
        {
            follow = Undo.AddComponent<LXIIThirdPersonCameraFollow>(mainCamera.gameObject);
        }

        Undo.RecordObject(mainCamera.transform, "Setup LXII Open World Camera Transform");
        Undo.RecordObject(mainCamera, "Setup LXII Open World Camera");
        Undo.RecordObject(follow, "Setup LXII Open World Camera Follow");

        follow.SetTarget(player.transform);
        follow.ConfigureOpenWorldFraming();
        follow.SnapBehindTarget();

        mainCamera.fieldOfView = 55f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 1200f;

        EditorUtility.SetDirty(mainCamera.transform);
        EditorUtility.SetDirty(mainCamera);
        EditorUtility.SetDirty(follow);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = mainCamera.gameObject;
        Debug.Log("[LXII] 大世界相机构图已应用：更远视距、更低俯角、更多天空和远景。");
    }
}
