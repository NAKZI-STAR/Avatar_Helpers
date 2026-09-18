using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nakzi.AvatarHelper.LocalBlueprint.Editor
{
    internal sealed class LocalBlueprintManagerWindow : EditorWindow
    {
        private Vector2 scroll;

        [MenuItem("Tools/VRChat/Local Blueprint Manager")]
        private static void Open() => GetWindow<LocalBlueprintManagerWindow>("Local Blueprints");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("로컬 ID 파일: UserSettings/LocalBlueprintSettings.asset (Git에서 제외하세요)", MessageType.Info);
            var bindings = BlueprintBindingService.FindSceneBindings().OrderBy(x => x.name).ToArray();
            using (var view = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = view.scrollPosition;
                foreach (var binding in bindings)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.ObjectField(binding.name, binding, typeof(LocalBlueprintBinder), true);
                        EditorGUILayout.SelectableLabel(binding.AvatarId, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        var pipeline = BlueprintBindingService.GetSinglePipeline(binding, out var error);
                        LocalBlueprintSettings.instance.TryGetBlueprintId(binding.AvatarId, out var local);
                        var current = pipeline == null ? null : pipeline.blueprintId;
                        EditorGUILayout.LabelField("Local", string.IsNullOrEmpty(local) ? "-" : local);
                        EditorGUILayout.LabelField("Pipeline", string.IsNullOrEmpty(current) ? "-" : current);
                        EditorGUILayout.LabelField("Status", BlueprintBindingService.GetStatus(local, current).ToString());
                        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Save")) BlueprintBindingService.SaveCurrent(binding);
                        }
                    }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save All")) foreach (var b in bindings) BlueprintBindingService.SaveCurrent(b);
                if (GUILayout.Button("Clear All Pipeline IDs"))
                    foreach (var b in bindings)
                    {
                        var p = BlueprintBindingService.GetSinglePipeline(b, out _);
                        if (p != null) BlueprintBindingService.SetPipelineId(p, string.Empty, "Clear Pipeline Blueprint IDs");
                    }
            }
        }
    }
}
