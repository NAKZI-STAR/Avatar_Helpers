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
        ActionEditorVertical(()
           => _avatarRootObject = EditorGUILayout.ObjectField("Dragged GameObject : ", _avatarRootObject, typeof(GameObject), true) as GameObject, GUI.skin.box);

        ActionEditorVertical(()
            => _pivotAnchorTransform = EditorGUILayout.ObjectField("Dragged Transform : ", _pivotAnchorTransform, typeof(Transform), true) as Transform, GUI.skin.box);

        if (GUILayout.Button("Set .. ") && 
            _avatarRootObject && 
            _pivotAnchorTransform)
        {
            foreach (Renderer renderer in _avatarRootObject.GetComponentsInChildren<Renderer>(true))
            {
                renderer.probeAnchor = _pivotAnchorTransform;
            }
        }
    }
}
#endif