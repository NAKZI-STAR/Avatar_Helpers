using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using VRC.SDKBase;

namespace Nakzi.AvatarHelper.LocalBlueprint
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Nakzi Avatar Helper/Local Blueprint Binding")]
    [MovedFrom(true, "Nakzi.AvatarHelper.LocalBlueprint", "Nakzi.AvatarHelper.LocalBlueprint", "LocalBlueprintBinding")]
    public sealed class LocalBlueprintBinder : MonoBehaviour, IEditorOnly
    {
        [SerializeField, HideInInspector] private string avatarId;

        public string AvatarId => avatarId;

        private void Reset()
        {
            EnsureAvatarId();
        }

        private void OnValidate()
        {
            EnsureAvatarId();
        }

        private void EnsureAvatarId()
        {
            if (string.IsNullOrWhiteSpace(avatarId))
                avatarId = Guid.NewGuid().ToString("N");
        }

#if UNITY_EDITOR
        public void RegenerateAvatarId()
        {
            avatarId = Guid.NewGuid().ToString("N");
        }
#endif
    }
}
