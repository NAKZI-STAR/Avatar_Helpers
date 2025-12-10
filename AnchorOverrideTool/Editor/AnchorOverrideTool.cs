#if UNITY_EDITOR
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static NAKZI.EditorHelper.EditorHelper;

internal sealed class AnchorOverrideTool : EditorWindow
{
    private GameObject _avatarRootObject = null;
    private Transform _pivotAnchorTransform = null;

    [MenuItem("Nakzi Avatar Script/Anchor Override Tool")]
    private static void Init()
    {
        GetWindow<AnchorOverrideTool>().Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Anchor Override Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("아바타 루트와 기준이 될 피벗 Transform을 지정하면,\n모든 Renderer의 Probe Anchor를 한 번에 변경합니다.", MessageType.Info);
        EditorGUILayout.Space();

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            _avatarRootObject = EditorGUILayout.ObjectField(
                "Avatar Root GameObject",
                _avatarRootObject,
                typeof(GameObject),
                true) as GameObject;
        }

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            _pivotAnchorTransform = EditorGUILayout.ObjectField(
                "Pivot Anchor Transform",
                _pivotAnchorTransform,
                typeof(Transform),
                true) as Transform;
        }

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(!_avatarRootObject || !_pivotAnchorTransform);
        {
            if (GUILayout.Button("Set Anchor To Pivot"))
            {
                foreach (Renderer renderer in _avatarRootObject.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.probeAnchor = _pivotAnchorTransform;
                }

                Debug.Log($"[AnchorOverrideTool] Set probeAnchor to '{_pivotAnchorTransform.name}' for all renderers under '{_avatarRootObject.name}'.");
            }
        }
        EditorGUI.EndDisabledGroup();

    }
}
#endif