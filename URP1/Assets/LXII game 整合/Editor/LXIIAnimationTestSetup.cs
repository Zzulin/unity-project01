using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LXIIAnimationTestSetup
{
    private const string ScenePath = "Assets/LXII game 整合/game.unity";
    private const string PlayerName = "LXII Nilou Player";
    private const string ControllerPath = "Assets/LXII game 整合/Settings/LXII_Nilou_LXI_Test.controller";

    private const string IdleClipPath = "Assets/LXI 动作测试/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx";
    private const string RunClipPath = "Assets/LXI 动作测试/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx";
    private const string ActionClipPath = "Assets/LXI 动作测试/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx";

    private const string IdleClipName = "HumanF@Idle01";
    private const string RunClipName = "HumanF@Run01_Forward";
    private const string ActionClipName = "HumanF@Attack1H01_R";

    [MenuItem("Tools/LXII/Setup LXI Animation Test In Game Scene")]
    public static void SetupLxiAnimationTestInGameScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        AnimationClip idleClip = LoadClip(IdleClipPath, IdleClipName);
        AnimationClip runClip = LoadClip(RunClipPath, RunClipName);
        AnimationClip actionClip = LoadClip(ActionClipPath, ActionClipName);
        if (idleClip == null || runClip == null || actionClip == null)
        {
            Debug.LogError("[LXII] LXI 动作测试 clip 加载失败，已停止。");
            return;
        }

        EnsureFolder("Assets/LXII game 整合", "Settings");
        EnsureFolder("Assets/LXII game 整合", "Scripts");
        EnsureFolder("Assets/LXII game 整合/Scripts", "Animation");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = GameObject.Find(PlayerName);
        if (player == null)
        {
            LXIINilouHumanoidSetup.SetupNilouHumanoidInGameScene();
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            player = GameObject.Find(PlayerName);
        }

        if (player == null)
        {
            Debug.LogError("[LXII] game.unity 中仍未找到 LXII Nilou Player。");
            return;
        }

        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            animator = Undo.AddComponent<Animator>(player);
        }

        AnimatorController controller = BuildOrUpdateController(idleClip, runClip, actionClip);
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        LXIIAnimationTestDriver driver = player.GetComponent<LXIIAnimationTestDriver>();
        if (driver == null)
        {
            driver = Undo.AddComponent<LXIIAnimationTestDriver>(player);
        }

        ConfigureMainCamera();

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = player;
        Debug.Log("[LXII] 已在 game.unity 中接入 LXI 动作测试。按 1=Idle，2=Run，3=Action。");
    }

    private static AnimatorController BuildOrUpdateController(AnimationClip idleClip, AnimationClip runClip, AnimationClip actionClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.parameters = new[]
        {
            new AnimatorControllerParameter
            {
                name = LXIIAnimationTestDriver.ModeParameter,
                type = AnimatorControllerParameterType.Int,
                defaultInt = 0
            },
            new AnimatorControllerParameter
            {
                name = LXIIAnimationTestDriver.ActionTriggerParameter,
                type = AnimatorControllerParameterType.Trigger
            }
        };

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ClearStateMachine(stateMachine);

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState runState = stateMachine.AddState("Run");
        runState.motion = runClip;

        AnimatorState actionState = stateMachine.AddState("Action");
        actionState.motion = actionClip;

        stateMachine.defaultState = idleState;

        AddModeTransition(idleState, runState, 1);
        AddModeTransition(runState, idleState, 0);

        AnimatorStateTransition anyToAction = stateMachine.AddAnyStateTransition(actionState);
        anyToAction.hasExitTime = false;
        anyToAction.duration = 0.05f;
        anyToAction.AddCondition(AnimatorConditionMode.If, 0f, LXIIAnimationTestDriver.ActionTriggerParameter);

        AnimatorStateTransition actionToIdle = actionState.AddTransition(idleState);
        actionToIdle.hasExitTime = true;
        actionToIdle.exitTime = 0.95f;
        actionToIdle.duration = 0.05f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return controller;
    }

    private static void AddModeTransition(AnimatorState from, AnimatorState to, int modeValue)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.AddCondition(AnimatorConditionMode.Equals, modeValue, LXIIAnimationTestDriver.ModeParameter);
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
        {
            stateMachine.RemoveState(childState.state);
        }

        foreach (ChildAnimatorStateMachine childMachine in stateMachine.stateMachines.ToArray())
        {
            stateMachine.RemoveStateMachine(childMachine.stateMachine);
        }

        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }
    }

    private static AnimationClip LoadClip(string assetPath, string clipName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == clipName)
            ?? AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
    }

    private static void EnsureFolder(string parent, string child)
    {
        string combined = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(combined))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void ConfigureMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Main Camera");
            if (cameraObject != null)
            {
                mainCamera = cameraObject.GetComponent<Camera>();
            }
        }

        if (mainCamera == null)
        {
            return;
        }

        Transform cameraTransform = mainCamera.transform;
        Undo.RecordObject(cameraTransform, "Frame LXII Animation Test Camera");
        cameraTransform.SetPositionAndRotation(new Vector3(0f, 1.18f, -3.6f), Quaternion.Euler(5f, 0f, 0f));

        Undo.RecordObject(mainCamera, "Adjust LXII Animation Test Camera");
        mainCamera.fieldOfView = 40f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 100f;
    }
}
