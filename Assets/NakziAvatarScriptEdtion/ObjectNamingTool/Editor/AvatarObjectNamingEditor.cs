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
        EditorGUILayout.LabelField("Drag GameObject Here . . .");

        ActionEditorVertical(() 
            => _avatarRootObject = EditorGUILayout.ObjectField("Dragged GameObject : ", _avatarRootObject, typeof(GameObject), true) as GameObject, GUI.skin.box);

        ActionVertical(() =>
        {
            ActionHorizontal(() =>
            {
                GUILayout.Label("Prefix ..");
                _prefix = GUILayout.TextField(_prefix);

                GUILayout.Label("Suffix ..");
                _suffix = GUILayout.TextField(_suffix);
            }, GUI.skin.box);
        }, GUI.skin.box);


        if (GUILayout.Button("Getnerate .. ") && (IsStrValidated(_prefix) || IsStrValidated(_suffix)) && _avatarRootObject)
        {
            foreach (Transform someObject in _avatarRootObject.GetComponentsInChildren<Transform>(true))
            {
                someObject.gameObject.name = $"{_prefix}{someObject.gameObject.name}{_suffix}";
            }
        }

        ActionVertical(() => 
        {
            ActionHorizontal(() => 
            {
                GUILayout.Label("Selected .. ");
                _selectedStr = GUILayout.TextField(_selectedStr);

                GUILayout.Label("Change .. ");
                _replaceStr = GUILayout.TextField(_replaceStr);
            }, GUI.skin.box);
        }, GUI.skin.box);

        if (GUILayout.Button("Replace ..") && IsStrValidated(_selectedStr) && _replaceStr is not null && _avatarRootObject)
        {
            foreach (Transform someObject in _avatarRootObject.GetComponentsInChildren<Transform>(true))
            {
                someObject.gameObject.name = someObject.gameObject.name.Replace(_selectedStr, _replaceStr); 
            }
        }
    }

    private bool IsStrValidated(string str)
        => !(string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str));
}
#endif