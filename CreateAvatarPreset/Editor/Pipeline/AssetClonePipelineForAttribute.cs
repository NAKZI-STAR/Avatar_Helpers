#if UNITY_EDITOR
using System;

namespace NAKZI.AvatarPreset.Pipeline
{
    /// <summary>
    /// 이 파이프라인이 처리할 에셋 타입을 지정하는 어트리뷰트입니다.
    /// IAssetClonePipeline을 구현하는 클래스에 사용합니다.
    /// </summary>
    /// <remarks>
    /// 예시:
    /// <code>
    /// [AssetClonePipelineFor(typeof(Material))]
    /// [AssetClonePipelineFor(typeof(PhysicsMaterial))] // 여러 타입 지정 가능
    /// public class MaterialClonePipeline : IAssetClonePipeline { ... }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AssetClonePipelineForAttribute : Attribute
    {
        /// <summary>
        /// 이 파이프라인이 처리할 에셋 타입
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// 파이프라인이 처리할 타입을 지정합니다.
        /// </summary>
        /// <param name="targetType">처리할 에셋 타입 (예: typeof(Material))</param>
        public AssetClonePipelineForAttribute(Type targetType)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }
    }
}
#endif

