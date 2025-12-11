#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NAKZI.AvatarPreset.Pipeline.Pipelines
{
    /// <summary>
    /// ScriptableObject의 기본 참조 재연결 파이프라인입니다.
    /// SerializedObject를 통해 모든 ObjectReference 필드를 순회합니다.
    /// 
    /// 이 파이프라인은 우선순위가 낮아(100) 더 구체적인 파이프라인이 없을 때만 사용됩니다.
    /// </summary>
    [AssetClonePipelineFor(typeof(ScriptableObject))]
    public class DefaultScriptableObjectPipeline : IAssetClonePipeline
    {
        public int Priority => 100; // 낮은 우선순위 (다른 파이프라인이 우선)

        public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
        {
            if (!asset) return false;

            SerializedObject so = new SerializedObject(asset);
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
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }

            return modified;
        }
    }
}
#endif

