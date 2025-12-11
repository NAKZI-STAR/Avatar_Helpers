#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace NAKZI.AvatarPreset.Pipeline.Pipelines
{
    /// <summary>
    /// AnimatorController의 참조를 재연결하는 파이프라인입니다.
    /// State의 Motion, BlendTree 내부 애니메이션, Avatar Mask 등을 처리합니다.
    /// </summary>
    [AssetClonePipelineFor(typeof(AnimatorController))]
    public class AnimatorControllerPipeline : IAssetClonePipeline
    {
        public int Priority => 0;

        public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
        {
            if (asset is not AnimatorController controller) return false;

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

            return modified;
        }

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
    }
}
#endif

