# Asset Clone Pipeline System

## 개요
이 파이프라인 시스템은 아바타 프리셋 생성 시 특정 타입의 에셋에 대해 커스텀 복제/참조 재연결 로직을 정의할 수 있게 해줍니다.

## 사용 방법

### 1. 커스텀 파이프라인 생성

새로운 에셋 타입에 대한 파이프라인을 추가하려면 `IAssetClonePipeline` 인터페이스를 구현하고 `[AssetClonePipelineFor]` 어트리뷰트를 추가합니다.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NAKZI.AvatarPreset.Pipeline;

// 이 파이프라인이 처리할 타입 지정
[AssetClonePipelineFor(typeof(YourCustomAssetType))]
public class YourCustomPipeline : IAssetClonePipeline
{
    // 우선순위 (낮을수록 먼저 사용됨, 기본값: 0)
    public int Priority => 0;

    // 참조 재연결 로직
    public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
    {
        if (asset is not YourCustomAssetType customAsset) return false;

        bool modified = false;

        // clonedMap을 사용하여 참조 재연결
        // clonedMap은 (원본 경로 -> 복제된 에셋) 매핑입니다.
        
        if (modified)
        {
            EditorUtility.SetDirty(customAsset);
            AssetDatabase.SaveAssetIfDirty(customAsset);
        }

        return modified;
    }

    // 선택적: 복제 전 콜백
    public void OnBeforeClone(Object originalAsset, string targetPath)
    {
        // 복제 전 전처리
    }

    // 선택적: 복제 후 콜백
    public void OnAfterClone(Object originalAsset, Object clonedAsset)
    {
        // 복제 후 후처리
    }
}
```

### 2. 여러 타입 처리

하나의 파이프라인으로 여러 타입을 처리할 수 있습니다:

```csharp
[AssetClonePipelineFor(typeof(TypeA))]
[AssetClonePipelineFor(typeof(TypeB))]
public class MultiTypePipeline : IAssetClonePipeline
{
    public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
    {
        // TypeA와 TypeB 모두 처리
        ...
    }
}
```

### 3. 기존 파이프라인 오버라이드

기존 파이프라인을 오버라이드하려면 더 낮은 우선순위(Priority 값)를 설정합니다:

```csharp
[AssetClonePipelineFor(typeof(Material))]
public class CustomMaterialPipeline : IAssetClonePipeline
{
    // 기본 MaterialPipeline(Priority=0)보다 낮은 값으로 오버라이드
    public int Priority => -1;

    public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
    {
        // 커스텀 Material 처리 로직
        ...
    }
}
```

## 내장 파이프라인

| 타입 | 파이프라인 | 설명 |
|------|-----------|------|
| `AnimatorController` | AnimatorControllerPipeline | State, Motion, BlendTree, AvatarMask 참조 처리 |
| `AnimatorOverrideController` | AnimatorOverrideControllerPipeline | 베이스 컨트롤러 및 오버라이드 클립 처리 |
| `Material` | MaterialPipeline | 텍스처 참조 처리 |
| `VRCExpressionsMenu` | VRCExpressionsMenuPipeline | Parameters, 서브메뉴, 아이콘 참조 처리 |
| `ScriptableObject` | DefaultScriptableObjectPipeline | 일반 ScriptableObject의 기본 처리 (우선순위: 100) |

## 파이프라인 재초기화

새 파이프라인을 추가한 후 Unity가 스크립트를 다시 컴파일하면 자동으로 등록됩니다.
수동으로 재초기화하려면:

```csharp
AssetClonePipelineManager.Reinitialize();
```

## 디버깅

파이프라인 등록 상태를 확인하려면 Unity Console에서 `[AssetClonePipeline]`로 시작하는 로그를 확인하세요.

