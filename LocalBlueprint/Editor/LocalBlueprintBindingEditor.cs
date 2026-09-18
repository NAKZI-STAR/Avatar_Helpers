using UnityEditor;
using UnityEngine;

namespace Nakzi.AvatarHelper.LocalBlueprint.Editor
{
    [CustomEditor(typeof(LocalBlueprintBinding))]
    internal sealed class LocalBlueprintBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var binding = (LocalBlueprintBinding)target;
            EditorGUILayout.LabelField("Local Blueprint Binding", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Avatar ID", binding.AvatarId);

            var pipeline = BlueprintBindingService.GetSinglePipeline(binding, out var error);
            LocalBlueprintSettings.instance.TryGetBlueprintId(binding.AvatarId, out var localId);
            var pipelineId = pipeline == null ? null : pipeline.blueprintId;
            EditorGUILayout.LabelField("Local Blueprint ID", string.IsNullOrEmpty(localId) ? "-" : localId);
            EditorGUILayout.LabelField("Pipeline Blueprint ID", string.IsNullOrEmpty(pipelineId) ? "-" : pipelineId);
            EditorGUILayout.LabelField("Status", BlueprintBindingService.GetStatus(localId, pipelineId).ToString());

            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
            if (BlueprintBindingService.HasDuplicateId(binding))
                EditorGUILayout.HelpBox("현재 Scene에 동일한 Avatar ID가 있습니다.", MessageType.Error);
            if (!string.IsNullOrEmpty(pipelineId) && !BlueprintBindingService.IsValidBlueprintId(pipelineId))
                EditorGUILayout.HelpBox("Pipeline Blueprint ID 형식이 올바르지 않습니다.", MessageType.Warning);
            EditorGUILayout.HelpBox("Blueprint ID는 UserSettings에만 저장됩니다. 이 컴포넌트는 플레이 및 VRChat 빌드 대상에서 제거됩니다.", MessageType.Info);

            using (new EditorGUI.DisabledScope(pipeline == null))
            {
                if (GUILayout.Button("Apply Local Blueprint")) BlueprintBindingService.Apply(binding, true);
                if (GUILayout.Button("Save Current Blueprint")) BlueprintBindingService.SaveCurrent(binding);
                if (GUILayout.Button("Clear Pipeline Blueprint"))
                    BlueprintBindingService.SetPipelineId(pipeline, string.Empty, "Clear Pipeline Blueprint ID");
            }
            if (GUILayout.Button("Remove Local Mapping"))
                LocalBlueprintSettings.instance.RemoveBlueprintId(binding.AvatarId);

            EditorGUILayout.Space();
            if (GUILayout.Button("Regenerate Avatar ID") &&
                EditorUtility.DisplayDialog("Avatar ID 재생성", "기존 로컬 매핑과의 연결이 끊어집니다. 계속할까요?", "Regenerate", "Cancel"))
            {
                Undo.RecordObject(binding, "Regenerate Avatar ID");
                binding.RegenerateAvatarId();
                EditorUtility.SetDirty(binding);
            }
        }
    }
}
