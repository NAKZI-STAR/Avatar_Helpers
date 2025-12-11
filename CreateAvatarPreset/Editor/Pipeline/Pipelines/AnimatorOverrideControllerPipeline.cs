#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NAKZI.AvatarPreset.Pipeline.Pipelines
{
    /// <summary>
    /// AnimatorOverrideController의 참조를 재연결하는 파이프라인입니다.
    /// 베이스 컨트롤러와 오버라이드된 애니메이션 클립들을 처리합니다.
    /// </summary>
    [AssetClonePipelineFor(typeof(AnimatorOverrideController))]
    public class AnimatorOverrideControllerPipeline : IAssetClonePipeline
    {
        public int Priority => 0;

        public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
        {
            if (asset is not AnimatorOverrideController overrideController) return false;

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

            return modified;
        }
    }
}
#endif

