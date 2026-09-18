using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nakzi.AvatarHelper.LocalBlueprint.Editor
{
    [FilePath("UserSettings/LocalBlueprintSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class LocalBlueprintSettings : ScriptableSingleton<LocalBlueprintSettings>
    {
        [SerializeField] private List<BlueprintEntry> entries = new List<BlueprintEntry>();

        public bool TryGetBlueprintId(string avatarId, out string blueprintId)
        {
            blueprintId = null;
            if (string.IsNullOrWhiteSpace(avatarId)) return false;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry != null && entry.avatarId == avatarId && !string.IsNullOrWhiteSpace(entry.blueprintId))
                {
                    blueprintId = entry.blueprintId;
                    return true;
                }
            }
            return false;
        }

        public bool Contains(string avatarId) => TryGetBlueprintId(avatarId, out _);

        public void SetBlueprintId(string avatarId, string blueprintId)
        {
            if (string.IsNullOrWhiteSpace(avatarId) || string.IsNullOrWhiteSpace(blueprintId)) return;
            BlueprintEntry found = null;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i] == null || entries[i].avatarId != avatarId) continue;
                if (found == null) found = entries[i];
                else entries.RemoveAt(i);
            }
            if (found == null)
            {
                found = new BlueprintEntry { avatarId = avatarId };
                entries.Add(found);
            }
            found.blueprintId = blueprintId.Trim();
            Save(true);
        }

        public void RemoveBlueprintId(string avatarId)
        {
            var removed = entries.RemoveAll(entry => entry == null || entry.avatarId == avatarId) > 0;
            if (removed) Save(true);
        }
    }
}
