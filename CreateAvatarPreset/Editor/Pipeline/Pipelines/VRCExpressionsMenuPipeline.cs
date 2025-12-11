#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace NAKZI.AvatarPreset.Pipeline.Pipelines
{
    /// <summary>
    /// VRCExpressionsMenu의 참조를 재연결하는 파이프라인입니다.
    /// Parameters, 서브메뉴, 아이콘 등의 참조를 처리합니다.
    /// </summary>
    [AssetClonePipelineFor(typeof(VRCExpressionsMenu))]
    public class VRCExpressionsMenuPipeline : IAssetClonePipeline
    {
        public int Priority => 0;

        public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
        {
            if (asset is not VRCExpressionsMenu menu) return false;

            SerializedObject menuSO = new SerializedObject(menu);
            bool modified = false;

            // 복제된 VRCExpressionParameters 찾기
            VRCExpressionParameters clonedParameters = null;
            foreach (var pair in clonedMap)
            {
                if (pair.Value is VRCExpressionParameters param)
                {
                    clonedParameters = param;
                    break;
                }
            }

            // Next(true)를 사용하여 숨겨진 필드까지 모두 순회
            SerializedProperty iterator = menuSO.GetIterator();
            bool enterChildren = true;

            while (iterator.Next(enterChildren))
            {
                enterChildren = true;

                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;

                Object originalRef = iterator.objectReferenceValue;

                // Parameters 필드가 None인 경우에도 복제된 Parameters로 설정
                if (originalRef == null)
                {
                    // 필드 타입이 VRCExpressionParameters인 경우 복제된 것으로 설정
                    if (clonedParameters != null &&
                        (iterator.name.Contains("arameter") || iterator.type.Contains("VRCExpressionParameters")))
                    {
                        iterator.objectReferenceValue = clonedParameters;
                        modified = true;
                    }
                    continue;
                }

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
                menuSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menu);
                AssetDatabase.SaveAssetIfDirty(menu);
            }

            return modified;
        }
    }
}
#endif

