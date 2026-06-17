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
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
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
        EnsureFolder("Assets/LXII game 整合/Scripts", "Camera");
        EnsureFolder("Assets/LXII game 整合/Scripts", "Player");

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
        EnsurePlayerControlChain(player);
        LXIIPlayerController playerController = player.GetComponent<LXIIPlayerController>();

        ConfigureMainCamera(player.transform, playerController);

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = player;
        Debug.Log("[LXII] 已在 game.unity 中接入正式第三人称角色控制。WASD 移动，Left Shift 加速，按住鼠标右键转视角，滚轮缩放，3=Action。");
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
                name = LXIIPlayerAnimationDriver.ModeParameter,
                type = AnimatorControllerParameterType.Int,
                defaultInt = 0
            },
            new AnimatorControllerParameter
            {
                name = LXIIPlayerAnimationDriver.ActionTriggerParameter,
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
        anyToAction.AddCondition(AnimatorConditionMode.If, 0f, LXIIPlayerAnimationDriver.ActionTriggerParameter);

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
        transition.AddCondition(AnimatorConditionMode.Equals, modeValue, LXIIPlayerAnimationDriver.ModeParameter);
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

    private static void EnsurePlayerControlChain(GameObject player)
    {
        LXIIAnimationTestDriver legacyDriver = player.GetComponent<LXIIAnimationTestDriver>();
        if (legacyDriver != null)
        {
            Undo.DestroyObjectImmediate(legacyDriver);
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = Undo.AddComponent<CharacterController>(player);
            ConfigureCharacterController(player, characterController);
        }

        LXIIPlayerInputReader inputReader = player.GetComponent<LXIIPlayerInputReader>();
        if (inputReader == null)
        {
            inputReader = Undo.AddComponent<LXIIPlayerInputReader>(player);
        }

        LXIIPlayerMotor motor = player.GetComponent<LXIIPlayerMotor>();
        if (motor == null)
        {
            motor = Undo.AddComponent<LXIIPlayerMotor>(player);
        }

        LXIIPlayerAnimationDriver animationDriver = player.GetComponent<LXIIPlayerAnimationDriver>();
        if (animationDriver == null)
        {
            animationDriver = Undo.AddComponent<LXIIPlayerAnimationDriver>(player);
        }

        LXIIPlayerController playerController = player.GetComponent<LXIIPlayerController>();
        if (playerController == null)
        {
            playerController = Undo.AddComponent<LXIIPlayerController>(player);
        }

        inputReader.hideFlags = HideFlags.HideInInspector;
        motor.hideFlags = HideFlags.HideInInspector;
        animationDriver.hideFlags = HideFlags.HideInInspector;

        EditorUtility.SetDirty(inputReader);
        EditorUtility.SetDirty(motor);
        EditorUtility.SetDirty(animationDriver);
        EditorUtility.SetDirty(playerController);
    }

    private static void ConfigureCharacterController(GameObject player, CharacterController characterController)
    {
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers.Length > 0
            ? renderers[0].bounds
            : new Bounds(player.transform.position + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float height = Mathf.Clamp(bounds.size.y * 0.9f, 1.5f, 2.1f);
        float radius = Mathf.Clamp(Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.35f, 0.22f, 0.38f);
        characterController.height = height;
        characterController.radius = radius;
        characterController.center = new Vector3(0f, height * 0.5f, 0f);
        characterController.skinWidth = 0.03f;
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 50f;
        characterController.minMoveDistance = 0f;

        Vector3 position = player.transform.position;
        position.y = 0f;
        player.transform.position = position;
    }

    private static void ConfigureMainCamera(Transform player, LXIIPlayerController playerController)
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

        foreach (MonoBehaviour behaviour in mainCamera.GetComponents<MonoBehaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == "SimpleCameraController")
            {
                Undo.RecordObject(behaviour, "Disable Free Camera Controller");
                behaviour.enabled = false;
                EditorUtility.SetDirty(behaviour);
            }
        }

        LXIIThirdPersonCameraFollow follow = mainCamera.GetComponent<LXIIThirdPersonCameraFollow>();
        if (follow == null)
        {
            follow = Undo.AddComponent<LXIIThirdPersonCameraFollow>(mainCamera.gameObject);
        }

        playerController?.ConfigureForScene(mainCamera.transform);
        follow.SetTarget(player);
        follow.ConfigureOpenWorldFraming();
        follow.SnapBehindTarget();

        Undo.RecordObject(mainCamera, "Adjust LXII Animation Test Camera");
        mainCamera.fieldOfView = 55f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 1200f;

        if (playerController != null)
        {
            EditorUtility.SetDirty(playerController);
        }
        EditorUtility.SetDirty(follow);
    }
}
