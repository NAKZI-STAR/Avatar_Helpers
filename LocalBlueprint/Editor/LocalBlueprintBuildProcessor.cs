using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Nakzi.AvatarHelper.LocalBlueprint.Editor
{
    internal sealed class LocalBlueprintBuildProcessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -10000;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            var bindings = avatarGameObject.GetComponentsInChildren<LocalBlueprintBinding>(true);
            foreach (var binding in bindings)
            {
                var pipeline = BlueprintBindingService.GetSinglePipeline(binding, out var error);
                if (pipeline == null)
                {
                    Debug.LogWarning($"[Local Blueprint] {binding.name}: {error}", binding);
                }
                else if (LocalBlueprintSettings.instance.TryGetBlueprintId(binding.AvatarId, out var local))
                {
                    pipeline.blueprintId = local;
                }
                Object.DestroyImmediate(binding);
            }
            return true;
        }
    }

    [InitializeOnLoad]
    internal static class LocalBlueprintPlayModeStripper
    {
        static LocalBlueprintPlayModeStripper()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                foreach (var binding in Resources.FindObjectsOfTypeAll<LocalBlueprintBinding>())
                    if (binding != null && binding.gameObject.scene.IsValid()) Object.DestroyImmediate(binding);
            };
        }
    }
}
