#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace NAKZI.AvatarPreset.Pipeline
{
    /// <summary>
    /// 에셋 복제 파이프라인을 관리하고 실행하는 매니저 클래스입니다.
    /// 모든 IAssetClonePipeline 구현체를 자동으로 수집하고 적절한 타입에 대해 실행합니다.
    /// </summary>
    public static class AssetClonePipelineManager
    {
        // 타입 -> 파이프라인 인스턴스 매핑
        private static Dictionary<Type, IAssetClonePipeline> _pipelines;
        private static bool _initialized = false;

        /// <summary>
        /// 등록된 모든 파이프라인 타입 목록
        /// </summary>
        public static IEnumerable<Type> RegisteredTypes => _pipelines?.Keys ?? Enumerable.Empty<Type>();

        /// <summary>
        /// 파이프라인 시스템을 초기화합니다.
        /// 모든 IAssetClonePipeline 구현체를 찾아 등록합니다.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _pipelines = new Dictionary<Type, IAssetClonePipeline>();

            // 모든 어셈블리에서 IAssetClonePipeline 구현체 찾기
            var pipelineInterface = typeof(IAssetClonePipeline);
            var pipelineTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(type =>
                    !type.IsAbstract &&
                    !type.IsInterface &&
                    pipelineInterface.IsAssignableFrom(type));

            foreach (var pipelineType in pipelineTypes)
            {
                // AssetClonePipelineFor 어트리뷰트 확인
                var attributes = pipelineType.GetCustomAttributes<AssetClonePipelineForAttribute>(false);

                if (!attributes.Any())
                {
                    Debug.LogWarning($"[AssetClonePipeline] {pipelineType.Name}에 [AssetClonePipelineFor] 어트리뷰트가 없습니다.");
                    continue;
                }

                // 파이프라인 인스턴스 생성
                IAssetClonePipeline pipelineInstance;
                try
                {
                    pipelineInstance = (IAssetClonePipeline)Activator.CreateInstance(pipelineType);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AssetClonePipeline] {pipelineType.Name} 인스턴스 생성 실패: {ex.Message}");
                    continue;
                }

                // 각 타겟 타입에 대해 등록
                foreach (var attr in attributes)
                {
                    var targetType = attr.TargetType;

                    // 이미 등록된 타입이면 우선순위 비교
                    if (_pipelines.TryGetValue(targetType, out var existingPipeline))
                    {
                        if (pipelineInstance.Priority < existingPipeline.Priority)
                        {
                            _pipelines[targetType] = pipelineInstance;
                            Debug.Log($"[AssetClonePipeline] {targetType.Name} 파이프라인을 {pipelineType.Name}(우선순위: {pipelineInstance.Priority})로 교체했습니다.");
                        }
                    }
                    else
                    {
                        _pipelines[targetType] = pipelineInstance;
                        Debug.Log($"[AssetClonePipeline] {targetType.Name} -> {pipelineType.Name} 등록됨");
                    }
                }
            }

            _initialized = true;
            Debug.Log($"[AssetClonePipeline] 초기화 완료. {_pipelines.Count}개 타입에 대한 파이프라인이 등록되었습니다.");
        }

        /// <summary>
        /// 파이프라인 시스템을 다시 초기화합니다.
        /// 새로운 파이프라인이 추가되었을 때 호출합니다.
        /// </summary>
        public static void Reinitialize()
        {
            _initialized = false;
            _pipelines?.Clear();
            Initialize();
        }

        /// <summary>
        /// 특정 타입에 대한 파이프라인이 등록되어 있는지 확인합니다.
        /// </summary>
        public static bool HasPipeline(Type assetType)
        {
            EnsureInitialized();
            return _pipelines.ContainsKey(assetType) || 
                   _pipelines.Keys.Any(t => t.IsAssignableFrom(assetType));
        }

        /// <summary>
        /// 특정 타입에 대한 파이프라인을 가져옵니다.
        /// </summary>
        public static IAssetClonePipeline GetPipeline(Type assetType)
        {
            EnsureInitialized();

            // 정확한 타입 매칭
            if (_pipelines.TryGetValue(assetType, out var pipeline))
                return pipeline;

            // 상속 타입 매칭 (가장 구체적인 타입 우선)
            var matchingType = _pipelines.Keys
                .Where(t => t.IsAssignableFrom(assetType))
                .OrderByDescending(t => GetInheritanceDepth(assetType, t))
                .FirstOrDefault();

            return matchingType != null ? _pipelines[matchingType] : null;
        }

        /// <summary>
        /// 에셋에 대한 재연결을 수행합니다.
        /// </summary>
        /// <param name="asset">재연결할 에셋</param>
        /// <param name="clonedMap">원본 경로 -> 복제된 에셋 매핑</param>
        /// <returns>재연결이 수행되었으면 true, 파이프라인이 없으면 false</returns>
        public static bool TryRemap(UnityEngine.Object asset, Dictionary<string, UnityEngine.Object> clonedMap)
        {
            if (!asset) return false;

            var pipeline = GetPipeline(asset.GetType());
            if (pipeline == null) return false;

            return pipeline.OnRemap(asset, clonedMap);
        }

        /// <summary>
        /// 복제 전 콜백을 실행합니다.
        /// </summary>
        public static void InvokeBeforeClone(UnityEngine.Object originalAsset, string targetPath)
        {
            if (!originalAsset) return;

            var pipeline = GetPipeline(originalAsset.GetType());
            pipeline?.OnBeforeClone(originalAsset, targetPath);
        }

        /// <summary>
        /// 복제 후 콜백을 실행합니다.
        /// </summary>
        public static void InvokeAfterClone(UnityEngine.Object originalAsset, UnityEngine.Object clonedAsset)
        {
            if (!originalAsset || !clonedAsset) return;

            var pipeline = GetPipeline(originalAsset.GetType());
            pipeline?.OnAfterClone(originalAsset, clonedAsset);
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                Initialize();
        }

        private static int GetInheritanceDepth(Type derived, Type baseType)
        {
            int depth = 0;
            var current = derived;
            while (current != null && current != baseType)
            {
                depth++;
                current = current.BaseType;
            }
            return depth;
        }
    }
}
#endif

