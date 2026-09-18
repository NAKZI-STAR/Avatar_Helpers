using System.Linq;
using UnityEditor;

namespace Nakzi.AvatarHelper.LocalBlueprint.Editor
{
    [InitializeOnLoad]
    internal static class LocalBlueprintAutoSynchronizer
    {
        private static bool scheduled;

        static LocalBlueprintAutoSynchronizer()
        {
            EditorApplication.hierarchyChanged += Schedule;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) Schedule();
            };
            Schedule();
        }

        private static void Schedule()
        {
            if (scheduled) return;
            scheduled = true;
            EditorApplication.delayCall += ApplyAll;
        }

        private static void ApplyAll()
        {
            scheduled = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            foreach (var binding in BlueprintBindingService.FindSceneBindings().ToArray())
                BlueprintBindingService.Apply(binding);
        }

        internal static void ApplyAvatarId(string avatarId)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var binding in BlueprintBindingService.FindSceneBindings()
                         .Where(item => item.AvatarId == avatarId).ToArray())
                BlueprintBindingService.Apply(binding);
        }
    }
}
