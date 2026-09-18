using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace Nakzi.AvatarHelper.LocalBlueprint.Editor
{
    internal static class BlueprintBindingService
    {
        public static bool IsValidBlueprintId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("avtr_", StringComparison.Ordinal)) return false;
            return Guid.TryParse(value.Substring(5), out _);
        }

        public static BlueprintBindingStatus GetStatus(string localId, string pipelineId)
        {
            var hasLocal = !string.IsNullOrWhiteSpace(localId);
            var hasPipeline = !string.IsNullOrWhiteSpace(pipelineId);
            if (!hasLocal && !hasPipeline) return BlueprintBindingStatus.Unregistered;
            if (!hasLocal) return BlueprintBindingStatus.PipelineOnly;
            if (!hasPipeline) return BlueprintBindingStatus.LocalOnly;
            return localId == pipelineId ? BlueprintBindingStatus.Synced : BlueprintBindingStatus.Mismatch;
        }

        public static PipelineManager GetSinglePipeline(LocalBlueprintBinding binding, out string error)
        {
            error = null;
            var pipelines = binding.GetComponents<PipelineManager>();
            if (pipelines.Length == 1) return pipelines[0];
            error = pipelines.Length == 0
                ? "같은 GameObject에 PipelineManager가 없습니다."
                : "같은 GameObject에 PipelineManager가 여러 개 있어 자동 선택할 수 없습니다.";
            return null;
        }

        public static bool HasDuplicateId(LocalBlueprintBinding binding)
        {
            return FindSceneBindings().Any(other => other != binding && other.AvatarId == binding.AvatarId);
        }

        public static IEnumerable<LocalBlueprintBinding> FindSceneBindings()
        {
            return Resources.FindObjectsOfTypeAll<LocalBlueprintBinding>()
                .Where(item => item != null && item.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(item));
        }

        public static void SetPipelineId(PipelineManager pipeline, string value, string undoName)
        {
            Undo.RecordObject(pipeline, undoName);
            pipeline.blueprintId = value ?? string.Empty;
            EditorUtility.SetDirty(pipeline);
        }

        public static void Apply(LocalBlueprintBinding binding, bool allowMismatchDialog)
        {
            var pipeline = GetSinglePipeline(binding, out _);
            if (pipeline == null || !LocalBlueprintSettings.instance.TryGetBlueprintId(binding.AvatarId, out var local)) return;
            if (string.IsNullOrWhiteSpace(pipeline.blueprintId) || pipeline.blueprintId == local)
            {
                SetPipelineId(pipeline, local, "Apply Local Blueprint ID");
                return;
            }
            if (!allowMismatchDialog) return;
            var choice = EditorUtility.DisplayDialogComplex("Blueprint ID 불일치",
                $"Local Blueprint ID:\n{local}\n\nCurrent Pipeline Blueprint ID:\n{pipeline.blueprintId}\n\n사용할 값을 선택하세요.",
                "Apply Local", "Cancel", "Save Current as Local");
            if (choice == 0) SetPipelineId(pipeline, local, "Apply Local Blueprint ID");
            else if (choice == 2 && IsValidBlueprintId(pipeline.blueprintId))
                LocalBlueprintSettings.instance.SetBlueprintId(binding.AvatarId, pipeline.blueprintId);
        }

        public static void SaveCurrent(LocalBlueprintBinding binding)
        {
            var pipeline = GetSinglePipeline(binding, out _);
            if (pipeline != null && IsValidBlueprintId(pipeline.blueprintId))
                LocalBlueprintSettings.instance.SetBlueprintId(binding.AvatarId, pipeline.blueprintId);
        }
    }
}
