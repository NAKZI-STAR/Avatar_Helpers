using System;

namespace Nakzi.AvatarHelper.LocalBlueprint.Editor
{
    [Serializable]
    internal sealed class BlueprintEntry
    {
        public string avatarId;
        public string blueprintId;
    }

    internal enum BlueprintBindingStatus
    {
        Unregistered,
        PipelineOnly,
        LocalOnly,
        Synced,
        Mismatch
    }
}
