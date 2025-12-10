#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace NAKZI.EditorHelper
{
    public static class EditorHelper
    {
        public static void ActionHorizontal(Action action, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            action.Invoke();
            GUILayout.EndHorizontal();
        }

        public static void ActionHorizontal(Action action, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(guiStyle, options);
            action.Invoke();
            GUILayout.EndHorizontal();
        }

        public static void ActionEditorHorizontal(Action action, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(guiStyle, options);
            action.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        public static void ActionEditorHorizontal(Action action, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(options);
            action.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        public static void ActionEditorVertical(Action action, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            action.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void ActionEditorVertical(Action action, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(guiStyle, options);
            action.Invoke();
            EditorGUILayout.EndVertical();
        }

        public static void ActionVertical(Action action, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(options);
            action.Invoke();
            GUILayout.EndVertical();
        }

        public static void ActionVertical(Action action, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(options);
            action.Invoke();
            GUILayout.EndVertical();
        }

        public static void ActionEditorVerticalBox(GUIStyle guiStyle, ref Vector2 scrollPosition, Action action, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(guiStyle, options);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            action.Invoke();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        public static void ActionVerticalBox(GUIStyle guiStyle, ref Vector2 scrollPosition, Action action, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(guiStyle, options);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            action.Invoke();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        public static T SelectEnumeratedToggles<T>(IEnumerable<T> targets, Func<T, string> text, T chooseTarget, GUIStyle selectGUIStyle, GUIStyle defaultGUIStyle, params GUILayoutOption[] options)
        {
            T selectingTarget = chooseTarget;

            foreach (T target in targets)
            {
                bool isTargeting = chooseTarget?.Equals(target) ?? false;
                bool isSelected = GUILayout.Toggle(isTargeting, text.Invoke(target), isTargeting ? selectGUIStyle : defaultGUIStyle, options);

                if (isSelected && !isTargeting)
                {
                    selectingTarget = target;
                }
                if (!isSelected && isTargeting)
                {
                    selectingTarget = default;
                }
            }

            return selectingTarget;
        }

        public static Texture2D GetTexture2D(Color32 color, GUIStyle guiStyle)
        {
            Color32[] pix = new Color32[guiStyle.border.horizontal * guiStyle.border.vertical];

            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = color;
            }

            Texture2D texture = new(guiStyle.border.horizontal, guiStyle.border.vertical);
            texture.SetPixels32(pix);
            texture.Apply();

            return texture;
        }

        public static void DisableEditorWindow<T>(ref T editorWindow) where T : EditorWindow
        {
            if (editorWindow)
            {
                editorWindow.Close();
            }

            editorWindow = null;
        }

        public static void OnErrorMessage(ref string errorMessage, GUIStyle labelStyle)
        {
            if (errorMessage is null) return;

            GUILayout.Label(errorMessage, labelStyle);
            Debug.LogWarning(errorMessage);

            if (GUILayout.Button("OK", GUILayout.Width(30)))
            {
                errorMessage = null;
            }
        }
    
        public static string GetBackingFieldName(string propertyName) => $"<{propertyName}>k__BackingField";
    }
}
#endif