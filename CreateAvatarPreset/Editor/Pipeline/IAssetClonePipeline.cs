#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace NAKZI.AvatarPreset.Pipeline
{
    /// <summary>
    /// 에셋 복제 및 참조 재연결 파이프라인 인터페이스.
    /// 특정 타입의 에셋에 대해 커스텀 복제/재연결 로직을 정의할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 사용 방법:
    /// 1. 이 인터페이스를 구현하는 클래스를 만듭니다.
    /// 2. [AssetClonePipelineFor(typeof(YourAssetType))] 어트리뷰트를 클래스에 추가합니다.
    /// 3. 파이프라인은 자동으로 등록되어 해당 타입의 에셋 처리 시 사용됩니다.
    /// 
    /// 예시:
    /// <code>
    /// [AssetClonePipelineFor(typeof(Material))]
    /// public class MaterialClonePipeline : IAssetClonePipeline
    /// {
    ///     public void OnRemap(Object asset, Dictionary&lt;string, Object&gt; clonedMap) { ... }
    /// }
    /// </code>
    /// </remarks>
    public interface IAssetClonePipeline
    {
        /// <summary>
        /// 파이프라인 우선순위. 낮을수록 먼저 실행됩니다.
        /// 기본값은 0입니다. 같은 타입에 여러 파이프라인이 있을 경우 우선순위가 높은 것이 사용됩니다.
        /// </summary>
        int Priority => 0;

        /// <summary>
        /// 복제된 에셋의 내부 참조를 재연결합니다.
        /// </summary>
        /// <param name="asset">재연결할 복제된 에셋</param>
        /// <param name="clonedMap">원본 경로 -> 복제된 에셋 매핑</param>
        /// <returns>재연결이 수행되었으면 true</returns>
        bool OnRemap(Object asset, Dictionary<string, Object> clonedMap);

        /// <summary>
        /// 에셋 복제 전에 호출됩니다. (선택적 구현)
        /// 복제 전 전처리가 필요한 경우 구현합니다.
        /// </summary>
        /// <param name="originalAsset">복제할 원본 에셋</param>
        /// <param name="targetPath">복제될 경로</param>
        void OnBeforeClone(Object originalAsset, string targetPath) { }

        /// <summary>
        /// 에셋 복제 후에 호출됩니다. (선택적 구현)
        /// 복제 후 후처리가 필요한 경우 구현합니다.
        /// </summary>
        /// <param name="originalAsset">원본 에셋</param>
        /// <param name="clonedAsset">복제된 에셋</param>
        void OnAfterClone(Object originalAsset, Object clonedAsset) { }
    }
}
#endif

