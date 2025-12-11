#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NAKZI.AvatarPreset.Pipeline.Pipelines
{
    /// <summary>
    /// Material의 참조를 재연결하는 파이프라인입니다.
    /// 텍스처 참조를 처리합니다.
    /// </summary>
    [AssetClonePipelineFor(typeof(Material))]
    public class MaterialPipeline : IAssetClonePipeline
    {
        public int Priority => 0;

        public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
        {
            if (asset is not Material material) return false;

            bool modified = false;
            Shader shader = material.shader;

            if (!shader) return false;

            // 모든 Texture 프로퍼티를 순회
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    Texture texture = material.GetTexture(propertyName);

                    if (texture)
                    {
                        string texturePath = AssetDatabase.GetAssetPath(texture);
                        if (!string.IsNullOrEmpty(texturePath) && clonedMap.TryGetValue(texturePath, out Object clonedTexture))
                        {
                            material.SetTexture(propertyName, clonedTexture as Texture);
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }

            return modified;
        }
    }
}
#endif

