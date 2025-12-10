#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static NAKZI.EditorHelper.EditorHelper;

internal sealed class AvatarNamingTool : EditorWindow
{
    private GameObject _avatarRootObject = null;
    private string _prefix = string.Empty;
    private string _suffix = string.Empty;

    private string _selectedStr = string.Empty;
    private string _replaceStr = string.Empty;

    [MenuItem("Nakzi Avatar Script/Avatar Naming Tool")]
    private static void Init()
        => (GetWindow(typeof(AvatarNamingTool)) as AvatarNamingTool).Show();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Avatar Naming Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("아바타 루트 오브젝트를 드래그해서 이름을 일괄 변경합니다.\n프리픽스/서픽스 추가 또는 문자열 치환이 가능합니다.", MessageType.Info);
        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            _avatarRootObject = EditorGUILayout.ObjectField(
                "Avatar Root GameObject",
                _avatarRootObject,
                typeof(GameObject),
                true) as GameObject;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("1. Prefix / Suffix 추가", EditorStyles.boldLabel);

        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Prefix");
                _prefix = GUILayout.TextField(_prefix);

                GUILayout.Label("Suffix");
                _suffix = GUILayout.TextField(_suffix);
            }
        }

        bool canGenerate = (IsStrValidated(_prefix) || IsStrValidated(_suffix)) && _avatarRootObject;

        EditorGUI.BeginDisabledGroup(!canGenerate);
        {
            if (GUILayout.Button("Apply Prefix / Suffix To Children"))
            {
                foreach (Transform someObject in _avatarRootObject.GetComponentsInChildren<Transform>(true))
                {
                    someObject.gameObject.name = $"{_prefix}{someObject.gameObject.name}{_suffix}";
                }

                Debug.Log($"[AvatarNamingTool] Applied prefix/suffix to all children under '{_avatarRootObject.name}'.");
            }
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. 문자열 치환 (Replace)", EditorStyles.boldLabel);

        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Selected");
                _selectedStr = GUILayout.TextField(_selectedStr);

                GUILayout.Label("Change To");
                _replaceStr = GUILayout.TextField(_replaceStr);
            }
        }

        bool canReplace = IsStrValidated(_selectedStr) && _replaceStr is not null && _avatarRootObject;

        EditorGUI.BeginDisabledGroup(!canReplace);
        {
            if (GUILayout.Button("Replace In Children Names"))
            {
                foreach (Transform someObject in _avatarRootObject.GetComponentsInChildren<Transform>(true))
                {
                    someObject.gameObject.name = someObject.gameObject.name.Replace(_selectedStr, _replaceStr);
                }

                Debug.Log($"[AvatarNamingTool] Replaced '{_selectedStr}' to '{_replaceStr}' in all children names under '{_avatarRootObject.name}'.");
            }
        }
        EditorGUI.EndDisabledGroup();
    }

    private bool IsStrValidated(string str)
        => !(string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str));
}
#endif