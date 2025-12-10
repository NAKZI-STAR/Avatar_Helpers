#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static NAKZI.EditorHelper.EditorHelper;

internal sealed class CreateAvatarPreset : EditorWindow
{
    private const string PRESET_PATH = "Assets/AvatarPreset/Resources/Presets/";
    private const string CREATED_AVATAR_PATH = "Assets/Avatar Setting/";
    private VRCAvatarDescriptor _avatarRootObject;
    private VRCAvatarDescriptor _selectedAvatar = null;
    private VRCAvatarDescriptor _keepAvatar = null;
    private GUIStyle _selectedStyle = null;
    private GUIStyle _togglesBoxStyle = null;
    private Vector2 _windowScrollPosition = Vector2.zero;
    private Vector2 _selectedAvatarScrollPosition = Vector2.zero;
    private Vector2 _dependencyScrollPosition = Vector2.zero;
    private string _presetName = string.Empty;
    private string _dependencySearch = string.Empty;
    private int _selectedAvatarIndex = -1;

    private bool _showPresetSection = true;
    private bool _showCreateSection = true;

    // 의존성 타입 구분용
    private enum DependencyType
    {
        AnimatorController,
        AnimatorOverrideController,
        AnimationClip,
        BlendTree,
        AvatarMask,
        VRCExpressionsMenu,
        VRCExpressionParameters,
        Material,
        Mesh,
        Texture,
        AudioClip,
        ScriptableObject,
    }

    private DependencyType _selectedDependencyType = DependencyType.AnimatorController;
    private readonly Dictionary<DependencyType, List<string>> _dependencyPathsByType = new();
    private readonly HashSet<string> _ignoredDependencyPaths = new();
    private bool _includeBuiltInAndPackages = false;

    [MenuItem("Nakzi Avatar Script/Create Avatar Preset")]
    private static void Init()
    {
        GetWindow<CreateAvatarPreset>().Show();
    }

    private void OnGUI()
    {
        if (_selectedStyle is null)
        {
            _selectedStyle = new(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold
            };
            _selectedStyle.normal.background = GetTexture2D(new Color32(70, 120, 200, 255), _selectedStyle);
            _selectedStyle.normal.textColor = Color.white;
        }

        if (_togglesBoxStyle is null)
        {
            _togglesBoxStyle = new(GUI.skin.box);
            _togglesBoxStyle.normal.background = GetTexture2D(new Color32(30, 30, 30, 255), _togglesBoxStyle);
            _togglesBoxStyle.normal.textColor = Color.white;
        }

        using var scroll = new EditorGUILayout.ScrollViewScope(_windowScrollPosition);
        _windowScrollPosition = scroll.scrollPosition;

        // Header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Avatar Preset Builder", EditorStyles.largeLabel);
        EditorGUILayout.LabelField("아바타 프리셋을 기반으로, 의존성이 분리된 새로운 아바타를 생성합니다.", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        // Section 1: Preset Registration
        _showPresetSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showPresetSection, "1. Preset Setup");
        if (_showPresetSection)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Source Avatar", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("씬에 배치된 아바타를 선택하고, 프리셋 이름을 지정한 뒤 저장합니다.", MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _avatarRootObject = EditorGUILayout.ObjectField(
                                    "Avatar Root (Scene)",
                        _avatarRootObject,
                                    typeof(VRCAvatarDescriptor),
                                    true) as VRCAvatarDescriptor;

                    GUILayout.Space(8);

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField("Preset Name", GUILayout.Width(100));
                    _presetName = EditorGUILayout.TextField(_presetName);
                    EditorGUILayout.EndVertical();
                }

                bool canRegisterPreset = _avatarRootObject;

                EditorGUI.BeginDisabledGroup(!canRegisterPreset);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Save As Preset", GUILayout.Height(24), GUILayout.MaxWidth(200)))
                    {
                        createFolder(PRESET_PATH);

                        PrefabUtility.SaveAsPrefabAsset(
                            _avatarRootObject.gameObject,
                            $"{PRESET_PATH}{(_presetName == string.Empty ? _avatarRootObject.gameObject.name : _presetName)}.prefab");

                        Debug.Log($"[CreateAvatarPreset] Preset saved: '{_avatarRootObject.gameObject.name}'.");
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        // Section 2 & 3: Preset Library + Dependency Exclusion (Side by Side)
        _showCreateSection = EditorGUILayout.BeginFoldoutHeaderGroup(_showCreateSection, "2. Preset Library & Dependency Options");
        if (_showCreateSection)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Left: Preset Library & Create Button
                    using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(280)))
                    {
                        var avatars = Resources.LoadAll<VRCAvatarDescriptor>("Presets");

                        EditorGUILayout.LabelField("Preset Library", EditorStyles.boldLabel);
                        EditorGUILayout.HelpBox("저장된 프리셋을 선택하고, 오른쪽 옵션과 함께 새 아바타를 생성합니다.", MessageType.None);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Preset", GUILayout.Width(60));

                            if (avatars.Length == 0)
                            {
                                EditorGUILayout.LabelField("등록된 프리셋이 없습니다.", EditorStyles.miniLabel);
                                _selectedAvatarIndex = -1;
                                _selectedAvatar = null;
                            }
                            else
                            {
                                string[] presetNames = avatars.Select(a => a.gameObject.name).ToArray();

                                // 현재 선택이 없으면 기존 선택 아바타 또는 0번으로 초기화
                                if (_selectedAvatarIndex < 0 || _selectedAvatarIndex >= avatars.Length)
                                {
                                    _selectedAvatarIndex = System.Array.IndexOf(avatars, _selectedAvatar);
                                    if (_selectedAvatarIndex < 0) _selectedAvatarIndex = 0;
                                }

                                _selectedAvatarIndex = EditorGUILayout.Popup(_selectedAvatarIndex, presetNames);
                                _selectedAvatar = avatars.Length > 0 && _selectedAvatarIndex >= 0 && _selectedAvatarIndex < avatars.Length
                                    ? avatars[_selectedAvatarIndex]
                                    : null;
                            }
                        }

                        if (_selectedAvatar)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label("Selected:", GUILayout.Width(60));
                                EditorGUILayout.LabelField(_selectedAvatar.gameObject.name, EditorStyles.boldLabel);
                            }
                        }

                        bool canCreateAvatar = _selectedAvatar;

                        EditorGUILayout.Space();
                        EditorGUI.BeginDisabledGroup(!canCreateAvatar);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Create Avatar Assets", GUILayout.Height(26), GUILayout.MaxWidth(200)))
                            {
                                // 선택된 프리셋 프리팹을 새로운 아바타 전용 폴더로 복사하고,
                                // 복제된 프리셋과 그 의존성들을 모두 새 리소스로 연결
                                string sourceAvatarPath = AssetDatabase.GetAssetPath(_selectedAvatar.gameObject);
                                if (string.IsNullOrEmpty(sourceAvatarPath))
                                {
                                    Debug.LogError("[CreateAvatarPreset] Selected preset does not have a valid asset path.");
                                }
                                else
                                {
                                    string newAvatarPath = $"{CREATED_AVATAR_PATH}{_selectedAvatar.gameObject.name}/";
                                    createFolder(CREATED_AVATAR_PATH);
                                    createFolder(newAvatarPath);

                                    string newPrefabPath = AssetDatabase.GenerateUniqueAssetPath(
                                        $"{newAvatarPath}{_selectedAvatar.gameObject.name}.prefab");

                                    if (!AssetDatabase.CopyAsset(sourceAvatarPath, newPrefabPath))
                                    {
                                        Debug.LogError($"[CreateAvatarPreset] Failed to copy avatar prefab from '{sourceAvatarPath}' to '{newPrefabPath}'.");
                                    }
                                    else
                                    {
                                        AssetDatabase.ImportAsset(newPrefabPath);
                                        GameObject newAvatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);

                                        if (!newAvatarPrefab)
                                        {
                                            Debug.LogError($"[CreateAvatarPreset] Failed to load new avatar prefab at '{newPrefabPath}'.");
                                        }
                                        else
                                        {
                                            VRCAvatarDescriptor newAvatarAsset = newAvatarPrefab.GetComponent<VRCAvatarDescriptor>();
                                            if (!newAvatarAsset)
                                            {
                                                Debug.LogError($"[CreateAvatarPreset] No VRCAvatarDescriptor found on prefab at '{newPrefabPath}'.");
                                            }
                                            else
                                            {
                                                    // 아바타 프리셋이 참조하는 모든 에셋 의존성을 통합된 흐름으로 복제하고,
                                                // 새 아바타 및 관련 에셋들의 참조를 모두 복제본으로 재연결
                                                cloneAdditionalDependenciesForAvatar(newAvatarAsset, newAvatarPath);

                                                Debug.Log($"[CreateAvatarPreset] ✓ Avatar created: '{_selectedAvatar.gameObject.name}' at '{newAvatarPath}'");
                                                EditorGUIUtility.PingObject(newAvatarPrefab);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                    }

                    GUILayout.Space(10);

                    // Right: Dependency Exclusion
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        if (_selectedAvatar)
                        {
                            DrawDependencyExclusionGUI();
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Dependency Options", EditorStyles.boldLabel);
                            EditorGUILayout.HelpBox("프리셋을 선택하면 복제에서 제외할 의존성을 설정할 수 있습니다.", MessageType.Info);
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // 선택된 프리셋이 변경되었으면 의존성 캐시를 다시 빌드
        if (_selectedAvatar && _selectedAvatar != _keepAvatar)
        {
            BuildDependencyCacheForSelectedAvatar();
        }
        _keepAvatar = _selectedAvatar;

    }


    // 아바타 프리셋 자산에서 에셋 의존성을 수집하고,
    // Material / AnimationClip / AnimatorController / AnimatorOverrideController / BlendTree / AvatarMask /
    // VRCExpressionsMenu / VRCExpressionParameters / Mesh / AudioClip / ScriptableObject / Texture 를 복제한 뒤
    // (사용자가 제외하지 않은 에셋만) 새 아바타 및 관련 에셋들이 해당 복제본을 참조하도록 재연결한다.
    private void cloneAdditionalDependenciesForAvatar(VRCAvatarDescriptor avatar, string newAvatarPath)
    {
        // 원본 에셋 경로 -> 복제된 에셋 인스턴스
        var clonedMap = new Dictionary<string, Object>();

        // 1) 프리셋으로 사용된 새 아바타 프리팹(복사본)에서 모든 에셋 의존성을 수집
        string sourceAvatarPath = AssetDatabase.GetAssetPath(avatar.gameObject);
        
        // 복제 통계
        Dictionary<string, int> cloneStats = new Dictionary<string, int>();
        
        if (!string.IsNullOrEmpty(sourceAvatarPath))
        {
            string[] dependencies = AssetDatabase.GetDependencies(sourceAvatarPath, true);

            foreach (string depPath in dependencies)
            {
                if (string.IsNullOrEmpty(depPath)) continue;

                // Built-in 에셋이나 Packages 폴더의 에셋은 토글이 꺼져있으면 복제하지 않는다
                if (!_includeBuiltInAndPackages && !depPath.StartsWith("Assets/"))
                {
                    continue;
                }

                Object depAsset = AssetDatabase.LoadMainAssetAtPath(depPath);
                if (!depAsset) continue;

                // Shader나 코드 에셋(MonoScript)은 복제하지 않는다
                if (depAsset is Shader) continue;
                if (depAsset is MonoScript) continue;

                // 의존성 타입 분류 (지원하지 않는 타입은 스킵)
                if (!TryGetDependencyType(depAsset, out DependencyType depType)) continue;

                // 사용자가 제외한 에셋은 복제하지 않는다
                if (_ignoredDependencyPaths.Contains(depPath)) continue;

                if (clonedMap.ContainsKey(depPath)) continue;

                Object cloned = cloneAssetForAvatar(depAsset, newAvatarPath);
                if (!cloned) continue;

                clonedMap.Add(depPath, cloned);
                
                // 통계 업데이트
                string typeName = depType.ToString();
                if (!cloneStats.ContainsKey(typeName))
                    cloneStats[typeName] = 0;
                cloneStats[typeName]++;
            }
            
            // 복제 통계 요약 출력
            var statsStr = string.Join(", ", cloneStats.Select(s => $"{s.Key}: {s.Value}"));
            Debug.Log($"[CreateAvatarPreset] Cloned {clonedMap.Count} assets ({statsStr})");
        }

        // 2) 아바타 전용 폴더 내의 모든 에셋에서, 외부 에셋 참조를 복제본으로 재연결
        string avatarFolder = newAvatarPath.TrimEnd('/');
        string[] allGuids = AssetDatabase.FindAssets(string.Empty, new[] { avatarFolder });
        foreach (string guid in allGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!asset) continue;

            remapObjectReferences(asset, clonedMap);
        }

        AssetDatabase.SaveAssets();

        // 3) 새 아바타 프리팹을 로드하고 컴포넌트들의 레퍼런스 재연결
        string avatarPrefabPath = AssetDatabase.GetAssetPath(avatar.gameObject);
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(avatarPrefabPath);
        
        if (prefabContents)
        {
            bool prefabModified = false;

            // VRCAvatarDescriptor의 Expression Parameters, Expressions Menu 재연결
            VRCAvatarDescriptor descriptor = prefabContents.GetComponent<VRCAvatarDescriptor>();
            if (descriptor)
            {
                SerializedObject descriptorSO = new SerializedObject(descriptor);
                bool descriptorModified = false;

                // Expression Parameters 재연결
                if (descriptor.expressionParameters)
                {
                    string paramPath = AssetDatabase.GetAssetPath(descriptor.expressionParameters);
                    if (!string.IsNullOrEmpty(paramPath) && clonedMap.TryGetValue(paramPath, out Object clonedParam))
                    {
                        SerializedProperty paramProp = descriptorSO.FindProperty("expressionParameters");
                        if (paramProp != null)
                        {
                            paramProp.objectReferenceValue = clonedParam;
                            descriptorModified = true;
                            prefabModified = true;
                        }
                    }
                }

                // Expressions Menu 재연결
                if (descriptor.expressionsMenu)
                {
                    string menuPath = AssetDatabase.GetAssetPath(descriptor.expressionsMenu);
                    if (!string.IsNullOrEmpty(menuPath) && clonedMap.TryGetValue(menuPath, out Object clonedMenu))
                    {
                        SerializedProperty menuProp = descriptorSO.FindProperty("expressionsMenu");
                        if (menuProp != null)
                        {
                            menuProp.objectReferenceValue = clonedMenu;
                            descriptorModified = true;
                            prefabModified = true;
                        }
                    }
                }

                // VRCAvatarDescriptor SerializedObject 저장
                if (descriptorModified)
                {
                    descriptorSO.ApplyModifiedPropertiesWithoutUndo();
                    descriptorSO.Update();
                    EditorUtility.SetDirty(descriptor);
                    EditorUtility.SetDirty(prefabContents);
                }

                // FX, Gesture, Action 등 Animator Controller 재연결
                if (descriptor.baseAnimationLayers != null)
                {
                    for (int i = 0; i < descriptor.baseAnimationLayers.Length; i++)
                    {
                        var layer = descriptor.baseAnimationLayers[i];
                        if (layer.animatorController)
                        {
                            string controllerPath = AssetDatabase.GetAssetPath(layer.animatorController);
                            if (!string.IsNullOrEmpty(controllerPath) && clonedMap.TryGetValue(controllerPath, out Object clonedController))
                            {
                                layer.animatorController = clonedController as RuntimeAnimatorController;
                                descriptor.baseAnimationLayers[i] = layer;
                                descriptorModified = true;
                                prefabModified = true;
                            }
                        }
                    }
                }

                if (descriptor.specialAnimationLayers != null)
                {
                    for (int i = 0; i < descriptor.specialAnimationLayers.Length; i++)
                    {
                        var layer = descriptor.specialAnimationLayers[i];
                        if (layer.animatorController)
                        {
                            string controllerPath = AssetDatabase.GetAssetPath(layer.animatorController);
                            if (!string.IsNullOrEmpty(controllerPath) && clonedMap.TryGetValue(controllerPath, out Object clonedController))
                            {
                                layer.animatorController = clonedController as RuntimeAnimatorController;
                                descriptor.specialAnimationLayers[i] = layer;
                                descriptorModified = true;
                                prefabModified = true;
                            }
                        }
                    }
                }
            }

            // SkinnedMeshRenderer와 MeshRenderer의 Material 배열 재연결
            foreach (Renderer renderer in prefabContents.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer) continue;

                Material[] sharedMats = renderer.sharedMaterials;
                bool materialsChanged = false;

                for (int i = 0; i < sharedMats.Length; i++)
                {
                    if (!sharedMats[i]) continue;

                    string matPath = AssetDatabase.GetAssetPath(sharedMats[i]);
                    if (!string.IsNullOrEmpty(matPath) && clonedMap.TryGetValue(matPath, out Object clonedMat))
                    {
                        sharedMats[i] = clonedMat as Material;
                        materialsChanged = true;
                        prefabModified = true;
                    }
                }

                if (materialsChanged)
                {
                    renderer.sharedMaterials = sharedMats;
                }
            }

            // 모든 컴포넌트의 SerializedObject를 통한 재연결
            foreach (Component component in prefabContents.GetComponentsInChildren<Component>(true))
            {
                if (!component) continue;

                // Renderer는 이미 위에서 처리했으므로 스킵
                if (component is Renderer) continue;

                SerializedObject so = new SerializedObject(component);
                SerializedProperty iterator = so.GetIterator();
                bool enterChildren = true;
                bool componentModified = false;

                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;

                    Object originalRef = iterator.objectReferenceValue;
                    if (!originalRef) continue;

                    string originalPath = AssetDatabase.GetAssetPath(originalRef);
                    if (string.IsNullOrEmpty(originalPath)) continue;

                    if (clonedMap.TryGetValue(originalPath, out Object cloned))
                    {
                        iterator.objectReferenceValue = cloned;
                        componentModified = true;
                        prefabModified = true;
                    }
                }

                if (componentModified)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Prefab 저장
            if (prefabModified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabContents, avatarPrefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabContents);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // 현재 선택된 프리셋 아바타에 대해, 타입별 의존성 목록을 캐싱
    private void BuildDependencyCacheForSelectedAvatar()
    {
        _dependencyPathsByType.Clear();
        _ignoredDependencyPaths.Clear();

        foreach (DependencyType type in System.Enum.GetValues(typeof(DependencyType)))
        {
            _dependencyPathsByType[type] = new List<string>();
        }

        string sourceAvatarPath = AssetDatabase.GetAssetPath(_selectedAvatar?.gameObject);
        if (string.IsNullOrEmpty(sourceAvatarPath)) return;

        string[] dependencies = AssetDatabase.GetDependencies(sourceAvatarPath, true);

        foreach (string depPath in dependencies)
        {
            if (string.IsNullOrEmpty(depPath)) continue;

            // Built-in 및 Packages 에셋 필터링 (토글이 꺼져있으면 제외)
            if (!_includeBuiltInAndPackages && !depPath.StartsWith("Assets/"))
            {
                continue;
            }

            Object depAsset = AssetDatabase.LoadMainAssetAtPath(depPath);
            if (!depAsset) continue;

            if (depAsset is Shader || depAsset is MonoScript) continue;

            if (!TryGetDependencyType(depAsset, out DependencyType type)) continue;

            _dependencyPathsByType[type].Add(depPath);
        }
    }

    // 의존성 타입 판별 헬퍼
    private bool TryGetDependencyType(Object asset, out DependencyType type)
    {
        if (asset is AnimatorController)
        {
            type = DependencyType.AnimatorController;
            return true;
        }

        if (asset is AnimatorOverrideController)
        {
            type = DependencyType.AnimatorOverrideController;
            return true;
        }

        if (asset is AnimationClip)
        {
            type = DependencyType.AnimationClip;
            return true;
        }

        if (asset is BlendTree)
        {
            type = DependencyType.BlendTree;
            return true;
        }

        if (asset is AvatarMask)
        {
            type = DependencyType.AvatarMask;
            return true;
        }

        if (asset is VRCExpressionsMenu)
        {
            type = DependencyType.VRCExpressionsMenu;
            return true;
        }

        if (asset is VRCExpressionParameters)
        {
            type = DependencyType.VRCExpressionParameters;
            return true;
        }

        if (asset is Material)
        {
            type = DependencyType.Material;
            return true;
        }

        if (asset is Mesh)
        {
            type = DependencyType.Mesh;
            return true;
        }

        if (asset is Texture)
        {
            type = DependencyType.Texture;
            return true;
        }

        if (asset is AudioClip)
        {
            type = DependencyType.AudioClip;
            return true;
        }

        if (asset is ScriptableObject)
        {
            type = DependencyType.ScriptableObject;
            return true;
        }

        type = default;
        return false;
    }

    // SerializedObject를 통해 ObjectReference 타입 필드들을 순회하면서,
    // clonedMap(원본 경로 -> 복제본)에 등록된 경우 복제된 에셋으로 치환해 준다.
    private void remapObjectReferences(Object target, Dictionary<string, Object> clonedMap)
    {
        if (!target) return;

        // AnimatorController는 특별 처리
        if (target is AnimatorController animController)
        {
            RemapAnimatorController(animController, clonedMap);
            return;
        }

        // AnimatorOverrideController도 특별 처리
        if (target is AnimatorOverrideController overrideController)
        {
            RemapAnimatorOverrideController(overrideController, clonedMap);
            return;
        }

        // Material도 특별 처리 (Texture 참조)
        if (target is Material material)
        {
            RemapMaterial(material, clonedMap);
            return;
        }

        // VRCExpressionsMenu도 특별 처리 (서브메뉴 참조)
        if (target is VRCExpressionsMenu expressionsMenu)
        {
            RemapExpressionsMenu(expressionsMenu, clonedMap);
            return;
        }

        SerializedObject so = new(target);
        SerializedProperty iterator = so.GetIterator();
        bool enterChildren = true;
        bool modified = false;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;

            Object originalRef = iterator.objectReferenceValue;
            if (!originalRef) continue;

            string originalPath = AssetDatabase.GetAssetPath(originalRef);
            if (string.IsNullOrEmpty(originalPath)) continue;

            if (clonedMap.TryGetValue(originalPath, out Object cloned))
            {
                iterator.objectReferenceValue = cloned;
                modified = true;
            }
        }

        if (modified)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }
    }

    // AnimatorController의 레퍼런스를 재연결
    private void RemapAnimatorController(AnimatorController controller, Dictionary<string, Object> clonedMap)
    {
        if (!controller) return;

        bool modified = false;

        // 각 레이어의 상태머신을 순회
        foreach (var layer in controller.layers)
        {
            if (layer.avatarMask)
            {
                string maskPath = AssetDatabase.GetAssetPath(layer.avatarMask);
                if (!string.IsNullOrEmpty(maskPath) && clonedMap.TryGetValue(maskPath, out Object clonedMask))
                {
                    layer.avatarMask = clonedMask as AvatarMask;
                    modified = true;
                }
            }

            modified |= RemapStateMachine(layer.stateMachine, clonedMap);
        }

        if (modified)
        {
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
        }
    }

    // StateMachine과 State들의 레퍼런스를 재연결
    private bool RemapStateMachine(AnimatorStateMachine stateMachine, Dictionary<string, Object> clonedMap)
    {
        if (!stateMachine) return false;

        bool modified = false;

        // 모든 State의 Motion을 체크
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion)
            {
                string motionPath = AssetDatabase.GetAssetPath(state.state.motion);
                if (!string.IsNullOrEmpty(motionPath) && clonedMap.TryGetValue(motionPath, out Object clonedMotion))
                {
                    state.state.motion = clonedMotion as Motion;
                    modified = true;
                }
            }

            // BlendTree인 경우 내부 애니메이션도 체크
            if (state.state.motion is BlendTree blendTree)
            {
                modified |= RemapBlendTree(blendTree, clonedMap);
            }
        }

        // 하위 StateMachine도 재귀적으로 처리
        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            modified |= RemapStateMachine(subStateMachine.stateMachine, clonedMap);
        }

        return modified;
    }

    // BlendTree의 레퍼런스를 재연결
    private bool RemapBlendTree(BlendTree blendTree, Dictionary<string, Object> clonedMap)
    {
        if (!blendTree) return false;

        bool modified = false;
        var children = blendTree.children;

        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];

            if (child.motion)
            {
                string motionPath = AssetDatabase.GetAssetPath(child.motion);
                if (!string.IsNullOrEmpty(motionPath) && clonedMap.TryGetValue(motionPath, out Object clonedMotion))
                {
                    child.motion = clonedMotion as Motion;
                    children[i] = child;
                    modified = true;
                }
            }

            // 중첩된 BlendTree
            if (child.motion is BlendTree childBlendTree)
            {
                modified |= RemapBlendTree(childBlendTree, clonedMap);
            }
        }

        if (modified)
        {
            blendTree.children = children;
        }

        return modified;
    }

    // AnimatorOverrideController의 레퍼런스를 재연결
    private void RemapAnimatorOverrideController(AnimatorOverrideController overrideController, Dictionary<string, Object> clonedMap)
    {
        if (!overrideController) return;

        bool modified = false;

        // Runtime Controller 재연결
        if (overrideController.runtimeAnimatorController)
        {
            string controllerPath = AssetDatabase.GetAssetPath(overrideController.runtimeAnimatorController);
            if (!string.IsNullOrEmpty(controllerPath) && clonedMap.TryGetValue(controllerPath, out Object clonedController))
            {
                overrideController.runtimeAnimatorController = clonedController as RuntimeAnimatorController;
                modified = true;
            }
        }

        // Override된 애니메이션 클립들 재연결
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            var pair = overrides[i];
            if (pair.Value)
            {
                string clipPath = AssetDatabase.GetAssetPath(pair.Value);
                if (!string.IsNullOrEmpty(clipPath) && clonedMap.TryGetValue(clipPath, out Object clonedClip))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, clonedClip as AnimationClip);
                    modified = true;
                }
            }
        }

        if (modified)
        {
            overrideController.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideController);
            AssetDatabase.SaveAssetIfDirty(overrideController);
        }
    }

    // Material의 Texture 레퍼런스를 재연결
    private void RemapMaterial(Material material, Dictionary<string, Object> clonedMap)
    {
        if (!material) return;

        bool modified = false;
        Shader shader = material.shader;

        // 모든 Texture 프로퍼티를 순회
        for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                Texture texture = material.GetTexture(propertyName);

                if (texture)
                {
                    string texturePath = AssetDatabase.GetAssetPath(texture);
                    if (!string.IsNullOrEmpty(texturePath) && clonedMap.TryGetValue(texturePath, out Object clonedTexture))
                    {
                        material.SetTexture(propertyName, clonedTexture as Texture);
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
    }

    // VRCExpressionsMenu의 서브메뉴, 아이콘, Parameters 레퍼런스를 재연결
    private void RemapExpressionsMenu(VRCExpressionsMenu menu, Dictionary<string, Object> clonedMap)
    {
        if (!menu) return;

        bool modified = false;
        SerializedObject menuSO = new SerializedObject(menu);

        // VRCExpressionsMenu의 Parameters 필드 재연결
        SerializedProperty parametersProp = menuSO.FindProperty("Parameters");
        
        if (parametersProp != null)
        {
            if (parametersProp.objectReferenceValue == null)
            {
                // Parameters가 null인 경우, clonedMap에서 VRCExpressionParameters를 찾아 할당
                foreach (var pair in clonedMap)
                {
                    if (pair.Value is VRCExpressionParameters clonedParams)
                    {
                        parametersProp.objectReferenceValue = clonedParams;
                        modified = true;
                        break;
                    }
                }
            }
            else
            {
                string parametersPath = AssetDatabase.GetAssetPath(parametersProp.objectReferenceValue);
                
                if (!string.IsNullOrEmpty(parametersPath) && clonedMap.TryGetValue(parametersPath, out Object clonedParameters))
                {
                    parametersProp.objectReferenceValue = clonedParameters;
                    modified = true;
                }
                else if (!string.IsNullOrEmpty(parametersPath))
                {
                    // clonedMap에 없으면 복제된 Parameters를 찾아서 할당
                    foreach (var pair in clonedMap)
                    {
                        if (pair.Value is VRCExpressionParameters clonedParams)
                        {
                            parametersProp.objectReferenceValue = clonedParams;
                            modified = true;
                            break;
                        }
                    }
                }
            }
        }

        // 각 컨트롤의 서브메뉴와 아이콘을 체크
        if (menu.controls != null)
        {
            for (int i = 0; i < menu.controls.Count; i++)
            {
                var control = menu.controls[i];

                // 서브메뉴 재연결
                if (control.subMenu)
                {
                    string subMenuPath = AssetDatabase.GetAssetPath(control.subMenu);
                    if (!string.IsNullOrEmpty(subMenuPath) && clonedMap.TryGetValue(subMenuPath, out Object clonedSubMenu))
                    {
                        control.subMenu = clonedSubMenu as VRCExpressionsMenu;
                        menu.controls[i] = control;
                        modified = true;
                    }
                }

                // 아이콘 재연결
                if (control.icon)
                {
                    string iconPath = AssetDatabase.GetAssetPath(control.icon);
                    if (!string.IsNullOrEmpty(iconPath) && clonedMap.TryGetValue(iconPath, out Object clonedIcon))
                    {
                        control.icon = clonedIcon as Texture2D;
                        menu.controls[i] = control;
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            menuSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);
            AssetDatabase.SaveAssetIfDirty(menu);
        }
    }

    // 의존성 제외 GUI
    private void DrawDependencyExclusionGUI()
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("Dependency Filters", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("프리셋이 참조하는 에셋 중 복제에서 제외할 타입과 에셋을 선택합니다.\n제외된 에셋은 원본을 그대로 참조합니다.", MessageType.None);

            // Built-in / Packages 에셋 포함 토글
            bool prevIncludeBuiltIn = _includeBuiltInAndPackages;
            _includeBuiltInAndPackages = EditorGUILayout.ToggleLeft(
                "Include Built-in & Packages Assets (Unity 기본 에셋 및 패키지 에셋 포함)",
                _includeBuiltInAndPackages);

            // 토글 상태가 변경되면 의존성 캐시를 다시 빌드
            if (prevIncludeBuiltIn != _includeBuiltInAndPackages)
            {
                BuildDependencyCacheForSelectedAvatar();
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                _selectedDependencyType = (DependencyType)EditorGUILayout.EnumPopup("Type", _selectedDependencyType);
                GUILayout.Space(8);
                _dependencySearch = EditorGUILayout.TextField("Search", _dependencySearch);
            }

            if (!_dependencyPathsByType.TryGetValue(_selectedDependencyType, out List<string> paths) || paths.Count == 0)
            {
                EditorGUILayout.LabelField("선택된 타입에 해당하는 에셋이 없습니다.");
                    return;
            }

            string searchLower = string.IsNullOrEmpty(_dependencySearch) ? null : _dependencySearch.ToLower();
            int visibleCount = 0;

            using (new EditorGUILayout.VerticalScope(_togglesBoxStyle ?? GUI.skin.box))
            {
                using var scroll = new EditorGUILayout.ScrollViewScope(_dependencyScrollPosition);
                _dependencyScrollPosition = scroll.scrollPosition;

                foreach (string path in paths)
                {
                    Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                    string name = asset ? asset.name : System.IO.Path.GetFileName(path);

                    if (searchLower != null)
                    {
                        string nameLower = name.ToLower();
                        string pathLower = path.ToLower();
                        if (!nameLower.Contains(searchLower) && !pathLower.Contains(searchLower))
                            continue;
                    }

                    visibleCount++;

                    bool excluded = _ignoredDependencyPaths.Contains(path);

                    bool newExcluded = EditorGUILayout.ToggleLeft(
                        $"{name}  ({path})",
                        excluded);

                    if (newExcluded != excluded)
                    {
                        if (newExcluded) _ignoredDependencyPaths.Add(path);
                        else _ignoredDependencyPaths.Remove(path);
                    }
                }
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"에셋 개수: {paths.Count} / 표시: {visibleCount}", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("현재 타입 전체 제외"))
                {
                    foreach (string path in paths)
                    {
                        _ignoredDependencyPaths.Add(path);
                    }
                }

                if (GUILayout.Button("현재 타입 전체 포함"))
                {
                    foreach (string path in paths)
                    {
                        _ignoredDependencyPaths.Remove(path);
                    }
                }
            }
        }
    }

    // 단일 에셋을 아바타 전용 폴더 하위로 복제
    private Object cloneAssetForAvatar(Object originalAsset, string newAvatarPath)
    {
        string sourcePath = AssetDatabase.GetAssetPath(originalAsset);
        if (string.IsNullOrEmpty(sourcePath))
        {
            Debug.LogWarning($"[CreateAvatarPreset] Cannot clone asset without path: {originalAsset}");
            return null;
        }

        string typeFolder;
        if (originalAsset is Material)
        {
            typeFolder = "Material/";
        }
        else if (originalAsset is AnimatorController || originalAsset is AnimatorOverrideController || originalAsset is BlendTree || originalAsset is AvatarMask)
        {
            typeFolder = "Animator/";
        }
        else if (originalAsset is AnimationClip)
        {
            typeFolder = "Animation/";
        }
        else if (originalAsset is VRCExpressionsMenu)
        {
            typeFolder = "Expressions Menu/";
        }
        else if (originalAsset is VRCExpressionParameters)
        {
            typeFolder = "Expressions Parameters/";
        }
        else if (originalAsset is Mesh)
        {
            typeFolder = "Mesh/";
        }
        else if (originalAsset is AudioClip)
        {
            typeFolder = "Audio/";
        }
        else if (originalAsset is Texture)
        {
            typeFolder = "Textures/";
        }
        else if (originalAsset is ScriptableObject)
        {
            typeFolder = "ScriptableObjects/";
        }
        else
        {
            // 여기에 올 일은 없지만, 방어적으로 처리
            typeFolder = "Assets/";
        }

        string targetFolder = $"{newAvatarPath}{typeFolder}";
        createFolder(targetFolder);

        string fileName = Path.GetFileName(sourcePath);
        string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}{fileName}");

        if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
        {
            Debug.LogWarning($"[CreateAvatarPreset] Failed to copy asset from '{sourcePath}' to '{targetPath}'.");
            return null;
        }

        AssetDatabase.ImportAsset(targetPath);
        return AssetDatabase.LoadAssetAtPath<Object>(targetPath);
    }

    private void createFolder(string path)
    {
        if (Directory.Exists(path)) return;

        Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
    }
}
#endif