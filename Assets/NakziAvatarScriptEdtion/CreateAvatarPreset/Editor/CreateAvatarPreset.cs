#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;
using static NAKZI.EditorHelper.EditorHelper;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;

internal sealed class CreateAvatarPreset : EditorWindow
{
    private const string PRESET_PATH = "Assets/AvatarPreset/Resources/Presets/";
    private const string CREATED_AVATAR_PATH = "Assets/Avatar Setting/";

    private readonly HashSet<AnimatorController> _ignoreAnimatorControllers = new();
    private readonly List<AnimatorController> _selectedAvatarControllers = new();
    private AnimatorController _selectedIgnoreController = null;
    private AnimatorController _selectedAnimatorController = null;
    private VRCAvatarDescriptor _avatarRootObject;
    private VRCAvatarDescriptor _selectedAvatar = null;
    private VRCAvatarDescriptor _keepAvatar = null;
    private GUIStyle _selectedStyle = null;
    private GUIStyle _togglesBoxStyle = null;
    private Vector2 _selectedAvatarScrollPosition = Vector2.zero;
    private Vector2 _selectedControllerScrollPosition = Vector2.zero;
    private Vector2 _ignoreControllerScrollPosition = Vector2.zero;
    private string _presetName = string.Empty;

    [MenuItem("Nakzi Avatar Script/Create Avatar Preset")]
    private static void Init()
    {
        GetWindow<CreateAvatarPreset>().Show();
    }

    private void OnGUI()
    {
        if (_selectedStyle is null)
        {
            _selectedStyle = new(GUI.skin.box);
            _selectedStyle.normal.background = GetTexture2D(Color.gray, _selectedStyle);
            _selectedStyle.normal.textColor = Color.black;
        }

        if (_togglesBoxStyle is null)
        {
            _togglesBoxStyle = new(GUI.skin.box);
            _togglesBoxStyle.normal.background = GetTexture2D(Color.black, _togglesBoxStyle);
            _togglesBoxStyle.normal.textColor = Color.white;
        }

        ActionEditorVertical(() =>
            _avatarRootObject = EditorGUILayout.ObjectField(
                "Dragged Avatar Root Object .. ", 
                _avatarRootObject, 
                typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor,
                GUI.skin.box);

        ActionHorizontal(() =>
        {
            GUILayout.Label("Preset Name .. ");
            ActionVertical(() => _presetName = GUILayout.TextField(_presetName));
        }); 

        if (GUILayout.Button("Regist Preset .. ") && _avatarRootObject)
        {
            createFolder(PRESET_PATH);

            PrefabUtility.SaveAsPrefabAsset(
                _avatarRootObject.gameObject, 
                $"{PRESET_PATH}{(_presetName == string.Empty ? _avatarRootObject.gameObject.name : _presetName)}.prefab");

            Debug.Log($"�ƹ�Ÿ �������� �����Ͽ����ϴ�! {_avatarRootObject.gameObject}");
        }

        var avatars = Resources.LoadAll<VRCAvatarDescriptor>("Presets");

        GUILayout.Label("Avatar Preset");
        ActionVerticalBox(_togglesBoxStyle, ref _selectedAvatarScrollPosition, () => 
            _selectedAvatar = SelectEnumeratedToggles(avatars, avatar => avatar.gameObject.name, _selectedAvatar, _selectedStyle, GUI.skin.box, GUILayout.ExpandWidth(true)));

        if (GUILayout.Button("Create Avatar") && _selectedAvatar)
        {
            VRCAvatarDescriptor newAvatar = Instantiate(_selectedAvatar);
            newAvatar.name = newAvatar.name.Replace("(Clone)", string.Empty);

            string newAvatarPath = $"{CREATED_AVATAR_PATH}{_selectedAvatar.gameObject.name}/";
            string newAnimatorPath = $"{newAvatarPath}Animator/";
            string newAnimatorBaseLayerPath = $"{newAnimatorPath}Base Animator Layer/";
            string newAnimationPath = $"{newAvatarPath}Animation/";
            string newAnimationCustomPath =  $"{newAnimationPath}/Custom/";
            string newExpressionsMenuPath = $"{newAvatarPath}Expressions Menu/";
            string newExpressionsParametersPath = $"{newAvatarPath}Expressions Parameters/";

            createFolder(CREATED_AVATAR_PATH);
            createFolder(newAvatarPath);
            createFolder(newAnimationPath);
            createFolder(newAnimatorPath);
            createFolder(newAnimatorBaseLayerPath);
            createFolder(newAnimationCustomPath);
            createFolder($"{newAvatarPath}Material/");
            createFolder(newExpressionsMenuPath);
            createFolder(newExpressionsParametersPath);

            foreach (Renderer renderer in newAvatar.gameObject.GetComponentsInChildren<Renderer>(true))
            {
                string materialPath = $"{newAvatarPath}Material/{renderer.gameObject.name.Replace("(Clone)", string.Empty)}/";
                createFolder(materialPath);

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{materialPath}{materials[i].name}.mat");

                    if (!newMaterial)
                    {
                        newMaterial = new(materials[i]);
                        AssetDatabase.CreateAsset(newMaterial, $"{materialPath}{newMaterial.name}.mat");
                    }

                    materials[i] = newMaterial;
                }

                renderer.sharedMaterials = materials;
            }

            for (int i = 0; i < newAvatar.baseAnimationLayers.Length; i++)
            {
                if (!newAvatar.baseAnimationLayers[i].animatorController ||
                    _ignoreAnimatorControllers.Any(ignoreController => ignoreController.name == newAvatar.baseAnimationLayers[i].animatorController.name)) continue;

                newAvatar.baseAnimationLayers[i].animatorController = createNewAnimatorController(
                    $"{newAnimatorPath}Base Animator Layer/",
                    $"{newAnimationPath}Base Animator Layer/",
                    newAvatar.baseAnimationLayers[i].animatorController as AnimatorController);
            }

            foreach (Animator originalAnimator in newAvatar.gameObject.GetComponentsInChildren<Animator>(true))
            {
                if (!originalAnimator.runtimeAnimatorController ||
                    originalAnimator.gameObject.name == newAvatar.gameObject.name ||
                    _ignoreAnimatorControllers.Any(ignoreController => ignoreController.name == originalAnimator.name)) continue;

                string animatorFolderPath = $"{newAnimatorPath}{originalAnimator.gameObject.name}/";
                createFolder(animatorFolderPath);

                originalAnimator.runtimeAnimatorController = createNewAnimatorController(
                    animatorFolderPath,
                    newAnimationCustomPath,
                    originalAnimator.runtimeAnimatorController as AnimatorController);
            }

            newAvatar.expressionParameters = getLoadAssetAtPath(newAvatar.expressionParameters, $"{newExpressionsParametersPath}{newAvatar.expressionParameters.name}.asset");
            newAvatar.expressionsMenu = getLoadAssetAtPath(newAvatar.expressionsMenu, $"{newExpressionsMenuPath}{newAvatar.expressionsMenu.name}.asset");

            convertNewExpressionsMenu(newExpressionsMenuPath, newAvatar.expressionsMenu);
        }

        if (_selectedAvatar && _selectedAvatar != _keepAvatar)
        {
            _selectedAvatarControllers.Clear();

            _selectedAvatarControllers.AddRange(_selectedAvatar.baseAnimationLayers
                .Select(layer => layer.animatorController as AnimatorController)
                .Concat(_selectedAvatar.GetComponentsInChildren<Animator>(true)
                .Select(animator => animator.runtimeAnimatorController as AnimatorController))
                .Where(controller => controller && !_ignoreAnimatorControllers.Contains(controller)));
        }

        _keepAvatar = _selectedAvatar;

        if (_selectedAvatar)
        {
            GUILayout.Label("������ ������ ���� ������ų �ִϸ����� �Դϴ�. ���ܽ�ų �ִϸ����͸� ����ּ���");
            ActionVerticalBox(_togglesBoxStyle, ref _selectedControllerScrollPosition, () => 
            {
                _selectedAnimatorController = SelectEnumeratedToggles(
                    _selectedAvatarControllers,
                    selectedAvatarController => selectedAvatarController.name,
                    _selectedAnimatorController,
                    _selectedStyle,
                    GUI.skin.box,
                    GUILayout.ExpandWidth(true));
            });

            if (GUILayout.Button("����") && _selectedAnimatorController)
            {
                _selectedAvatarControllers.Remove(_selectedAnimatorController);
                _ignoreAnimatorControllers.Add(_selectedAnimatorController);
                _selectedAnimatorController = null;
            }

            GUILayout.Label("���ܵ� �ִϸ����� ��� �Դϴ�.");
            ActionVerticalBox(_togglesBoxStyle, ref _ignoreControllerScrollPosition, () =>
            {
                _selectedIgnoreController = SelectEnumeratedToggles(
                    _ignoreAnimatorControllers,
                    controller => controller.name,
                    _selectedIgnoreController,
                    _selectedStyle,
                    GUI.skin.box,
                    GUILayout.ExpandWidth(true));
            });

            if (GUILayout.Button("����") && _selectedIgnoreController)
            {
                _ignoreAnimatorControllers.Remove(_selectedIgnoreController);
                _selectedAvatarControllers.Add(_selectedIgnoreController);
                _selectedIgnoreController = null;
            }
        }
    }

    private void convertNewExpressionsMenu(string path, VRCExpressionsMenu menu)
    {
        Stack<VRCExpressionsMenu> menuStack = new();
        Queue<KeyValuePair<string, VRCExpressionsMenu.Control>> successedQueue = new();
        menuStack.Push(menu);

        while (menuStack.Count > 0)
        {
            VRCExpressionsMenu currentMenu = menuStack.Pop();

            foreach (VRCExpressionsMenu.Control expressionsMenu in currentMenu.controls)
            {
                if (expressionsMenu.type != VRCExpressionsMenu.Control.ControlType.SubMenu) continue;

                Debug.Log("�ͽ������� �޴� ����");

                successedQueue.Enqueue(new($"{path}{expressionsMenu.name}.asset", expressionsMenu));
                menuStack.Push(expressionsMenu.subMenu);
            }
        }

        while (successedQueue.Count > 0)
        {
            KeyValuePair<string, VRCExpressionsMenu.Control> pathMenuPair = successedQueue.Dequeue();
            pathMenuPair.Value.subMenu = getLoadAssetAtPath(pathMenuPair.Value.subMenu, pathMenuPair.Key);
        }
    }

    private T getLoadAssetAtPath<T>(T originalAsset, string path) where T : Object, new()
    {
        T newAsset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (!newAsset)
        {
            Debug.Log(AssetDatabase.GetAssetPath(originalAsset));
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(originalAsset), path);
            AssetDatabase.Refresh();
            newAsset = AssetDatabase.LoadAssetAtPath<T>(path);
            Debug.Log(path);
        }

        return newAsset;
    }

    private AnimatorController createNewAnimatorController(string controllerPath, string animationPath, AnimatorController originalController)
    {
        string newControllerPath = $"{controllerPath}{originalController.name}.controller";
        string animationControllerPath = $"{animationPath}{originalController.name}/";

        AnimatorController newAnimatorController = getLoadAssetAtPath(originalController, newControllerPath);

        createFolder(animationControllerPath);

        for (int i = 0; i < newAnimatorController.layers.Length; i++)
        {
            string layerFolderPath = $"{animationControllerPath}{newAnimatorController.layers[i].name}/";
            createFolder(layerFolderPath);

            // �� ���̾��� ���¿� ���� ���� ���� �� �ִϸ��̼� Ŭ�� ����
            newAnimatorController.layers[i].stateMachine.stateMachines = createStateMachine(newAnimatorController.layers[i].stateMachine, layerFolderPath);
        }

        return newAnimatorController;
    }

    private ChildAnimatorStateMachine[] createStateMachine(AnimatorStateMachine stateMachine, string path)
    {
        for (int i = 0; i < stateMachine.states.Length; i++)
        {
            switch (stateMachine.states[i].state.motion)
            {
                case AnimationClip clip:
                    stateMachine.states[i].state.motion = createNewClip(clip, path);
                    break;
                case BlendTree blendTree:
                    BlendTree newBlendTree = getLoadAssetAtPath(blendTree, $"{path}{blendTree.name}.asset");
                    stateMachine.states[i].state.motion = newBlendTree;
                    createNewMotionFromBlendTree(newBlendTree, path);
                    break;
                default:
                    break;
            }
        }

        for (int i = 0; i < stateMachine.stateMachines.Length; i++)
        {
            string stateMachinePath = $"{path}{stateMachine.stateMachines[i].stateMachine.name}/";
            createFolder(stateMachinePath);
            stateMachine.stateMachines = createStateMachine(stateMachine.stateMachines[i].stateMachine, stateMachinePath);
        }

        return stateMachine.stateMachines;
    }

    private AnimationClip createNewClip(AnimationClip originalClip, string path)
        => getLoadAssetAtPath(originalClip, $"{path}{originalClip.name}.anim");

    /// <summary>
    /// .. ����Ʈ Ʈ���� �� Ÿ������ �����ؼ� �������Ƿ� ����� ������ Ʈ���� �ڽĵ��� ������ �� ���ο� ������ Ʈ���� �������־�� �Ѵ�.
    /// RemoveChild�� ������ ������ Ʈ�� ���� �� AddChild�޼���� ����
    /// </summary>
    /// <param name="blendTree"></param>
    /// <param name="path"></param>
    private void createNewMotionFromBlendTree(BlendTree blendTree, string path)
    {
        string blendPath = $"{path}{blendTree.name}(Blend Tree)/";
        createFolder(blendPath);

        ChildMotion[] childMotions = blendTree.children;
        int count = childMotions.Length;

        while(count > 0)
        {
            blendTree.RemoveChild(0);
            count--;
        }

        for (int i = 0; i < childMotions.Length; i++)
        {
            switch (childMotions[i].motion)
            {
                case AnimationClip clip:
                    childMotions[i].motion = createNewClip(clip, blendPath);
                    break;
                case BlendTree childBlendTree:
                    BlendTree newBlendTree = getLoadAssetAtPath(childBlendTree, $"{blendPath}{childBlendTree.name}.asset");
                    childMotions[i].motion = newBlendTree;
                    createNewMotionFromBlendTree(newBlendTree, blendPath);
                    break;
                default:
                    return;
            }

            blendTree.AddChild(childMotions[i].motion);
        }
    }

    private void createFolder(string path)
    {
        if (Directory.Exists(path)) return;

        Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
    }
}
#endif