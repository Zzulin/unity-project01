using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class LXIINilouHumanoidSetup
{
    private const string ScenePath = "Assets/LXII game 整合/game.unity";
    private const string ModelPath = "Assets/L10.9 learnNPR/43 妮露/NPC_Avatar_Girl_Sword_Nilou.fbx";
    private const string Body1MaterialPath = "Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 1.mat";
    private const string Body2MaterialPath = "Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Body 2.mat";
    private const string Hair1MaterialPath = "Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Hair 1.mat";
    private const string FaceMaterialPath = "Assets/L10.9 learnNPR/43 妮露/Nilou/tex4.23/材质/Face and face_eye.mat";
    private const string ScenePlayerName = "LXII Nilou Player";

    [MenuItem("Tools/LXII/Setup Nilou Humanoid In Game Scene")]
    public static void SetupNilouHumanoidInGameScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!EnsureHumanoidAvatar(out Avatar avatar))
        {
            return;
        }

        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (sceneAsset == null || modelPrefab == null)
        {
            Debug.LogError($"[LXII] 找不到目标场景或妮露模型。\nScene: {ScenePath}\nModel: {ModelPath}");
            return;
        }

        SceneSetupMaterials materials = LoadSetupMaterials();
        if (!materials.IsComplete)
        {
            Debug.LogError("[LXII] 妮露材质加载不完整，已停止场景写入。");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RemoveExistingPlayerRoot();

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, scene);
        if (player == null)
        {
            Debug.LogError("[LXII] 妮露模型实例化失败。");
            return;
        }

        Undo.RegisterCreatedObjectUndo(player, "Setup LXII Nilou Player");
        player.name = ScenePlayerName;
        player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
        player.transform.localScale = Vector3.one;

        ApplyPreferredMaterials(player, materials);
        EnsureAnimator(player, avatar);
        ConfigureShadowSettings(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);

        Debug.Log($"[LXII] 妮露 Humanoid 已写入场景：{ScenePath}\nAvatar: {avatar.name}\n对象名: {ScenePlayerName}");
    }

    [MenuItem("Tools/LXII/Validate Nilou Humanoid Avatar")]
    public static void ValidateNilouHumanoidAvatar()
    {
        if (EnsureHumanoidAvatar(out Avatar avatar))
        {
            Debug.Log($"[LXII] 妮露 Avatar 校验通过。\nAvatar: {avatar.name}\nIsHuman: {avatar.isHuman}\nIsValid: {avatar.isValid}");
        }
    }

    private static bool EnsureHumanoidAvatar(out Avatar avatar)
    {
        avatar = null;

        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"[LXII] 读取妮露 ModelImporter 失败：{ModelPath}");
            return false;
        }

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (ApplyExplicitNilouHumanMapping(importer))
        {
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }
        else
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        }

        avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<Avatar>()
            .FirstOrDefault(candidate => candidate != null);

        if (avatar == null)
        {
            Debug.LogError("[LXII] 重新导入后仍未找到妮露 Avatar 子资源，请在 Rig 面板手动检查 Configure。");
            return false;
        }

        if (!avatar.isHuman || !avatar.isValid)
        {
            Debug.LogError($"[LXII] 妮露 Avatar 仍无效。\nAvatar: {avatar.name}\nIsHuman: {avatar.isHuman}\nIsValid: {avatar.isValid}\n请在 Rig 面板检查骨骼映射。");
            return false;
        }

        return true;
    }

    private static bool ApplyExplicitNilouHumanMapping(ModelImporter importer)
    {
        HumanDescription description = importer.humanDescription;
        HumanBone[] explicitBones =
        {
            CreateHumanBone("Hips", "Bip001 Pelvis"),
            CreateHumanBone("Spine", "Bip001 Spine"),
            CreateHumanBone("Chest", "Bip001 Spine1"),
            CreateHumanBone("UpperChest", "Bip001 Spine2"),
            CreateHumanBone("Neck", "Bip001 Neck"),
            CreateHumanBone("Head", "Bip001 Head"),

            CreateHumanBone("LeftShoulder", "Bip001 L Clavicle"),
            CreateHumanBone("LeftUpperArm", "Bip001 L UpperArm"),
            CreateHumanBone("LeftLowerArm", "Bip001 L Forearm"),
            CreateHumanBone("LeftHand", "Bip001 L Hand"),

            CreateHumanBone("RightShoulder", "Bip001 R Clavicle"),
            CreateHumanBone("RightUpperArm", "Bip001 R UpperArm"),
            CreateHumanBone("RightLowerArm", "Bip001 R Forearm"),
            CreateHumanBone("RightHand", "Bip001 R Hand"),

            CreateHumanBone("LeftUpperLeg", "Bip001 L Thigh"),
            CreateHumanBone("LeftLowerLeg", "Bip001 L Calf"),
            CreateHumanBone("LeftFoot", "Bip001 L Foot"),
            CreateHumanBone("LeftToes", "Bip001 L Toe0"),

            CreateHumanBone("RightUpperLeg", "Bip001 R Thigh"),
            CreateHumanBone("RightLowerLeg", "Bip001 R Calf"),
            CreateHumanBone("RightFoot", "Bip001 R Foot"),
            CreateHumanBone("RightToes", "Bip001 R Toe0")
        };

        bool same = description.human != null
            && description.human.Length == explicitBones.Length
            && description.human.Zip(explicitBones, (left, right) => left.humanName == right.humanName && left.boneName == right.boneName)
                .All(match => match);

        if (same)
        {
            return false;
        }

        description.human = explicitBones;
        importer.humanDescription = description;
        return true;
    }

    private static HumanBone CreateHumanBone(string humanName, string boneName)
    {
        return new HumanBone
        {
            humanName = humanName,
            boneName = boneName,
            limit = new HumanLimit
            {
                useDefaultValues = true
            }
        };
    }

    private static void RemoveExistingPlayerRoot()
    {
        GameObject existing = GameObject.Find(ScenePlayerName);
        if (existing == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(existing);
    }

    private static void EnsureAnimator(GameObject player, Avatar avatar)
    {
        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            animator = Undo.AddComponent<Animator>(player);
        }

        animator.avatar = avatar;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
    }

    private static void ConfigureShadowSettings(GameObject player)
    {
        foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static void ApplyPreferredMaterials(GameObject player, SceneSetupMaterials materials)
    {
        foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            Material[] sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                continue;
            }

            string rendererName = renderer.name.ToLowerInvariant();
            bool changed = false;

            if (sharedMaterials.Length == 3)
            {
                sharedMaterials[0] = materials.Hair1;
                sharedMaterials[1] = materials.Body1;
                sharedMaterials[2] = materials.Body2;
                changed = true;
            }
            else
            {
                for (int i = 0; i < sharedMaterials.Length; i++)
                {
                    string materialName = sharedMaterials[i] != null ? sharedMaterials[i].name.ToLowerInvariant() : string.Empty;
                    Material replacement = ChooseReplacementMaterial(rendererName, materialName, materials);
                    if (replacement == null)
                    {
                        continue;
                    }

                    sharedMaterials[i] = replacement;
                    changed = true;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = sharedMaterials;
            }
        }
    }

    private static Material ChooseReplacementMaterial(string rendererName, string materialName, SceneSetupMaterials materials)
    {
        string combinedName = $"{rendererName} {materialName}";
        if (combinedName.Contains("face") || combinedName.Contains("eye"))
        {
            return materials.Face;
        }

        if (combinedName.Contains("effectmesh") || combinedName.Contains("eyestar"))
        {
            return materials.Face;
        }

        if (combinedName.Contains("hair"))
        {
            return materials.Hair1;
        }

        if (combinedName.Contains("dress") || combinedName.Contains("body 2"))
        {
            return materials.Body2;
        }

        if (combinedName.Contains("body"))
        {
            return materials.Body1;
        }

        return null;
    }

    private static SceneSetupMaterials LoadSetupMaterials()
    {
        return new SceneSetupMaterials(
            AssetDatabase.LoadAssetAtPath<Material>(Body1MaterialPath),
            AssetDatabase.LoadAssetAtPath<Material>(Body2MaterialPath),
            AssetDatabase.LoadAssetAtPath<Material>(Hair1MaterialPath),
            AssetDatabase.LoadAssetAtPath<Material>(FaceMaterialPath));
    }

    private readonly struct SceneSetupMaterials
    {
        public SceneSetupMaterials(Material body1, Material body2, Material hair1, Material face)
        {
            Body1 = body1;
            Body2 = body2;
            Hair1 = hair1;
            Face = face;
        }

        public Material Body1 { get; }
        public Material Body2 { get; }
        public Material Hair1 { get; }
        public Material Face { get; }

        public bool IsComplete => Body1 != null && Body2 != null && Hair1 != null && Face != null;
    }
}
