# Nakzi Avatar Helper

VRChat 아바타 작업을 위한 유틸리티 도구 모음입니다.

## 📦 설치 방법

### VCC (VRChat Creator Companion)를 통한 설치 (권장)

1. VCC 상단 메뉴 → **Settings** → **Packages** → **Add Repository**
2. 다음 URL을 복사하여 입력:
   ```
   https://nakzi-star.github.io/Avatar_Helpers/index.json
   ```
3. 프로젝트에서 **Manage Project** → **Nakzi Avatar Helper** 패키지 추가

### UPM (Unity Package Manager)을 통한 설치

#### 방법 1: Git URL로 추가
1. Unity 메뉴 → **Window** → **Package Manager**
2. 좌측 상단 **+** 버튼 → **Add package from git URL...**
3. 다음 URL 입력:
   ```
   https://github.com/NAKZI-STAR/Avatar_Helpers.git
   ```

#### 방법 2: manifest.json 직접 수정
프로젝트의 `Packages/manifest.json` 파일에 다음을 추가:
```json
{
  "dependencies": {
    "com.nakzi.avatarhelper": "https://github.com/NAKZI-STAR/Avatar_Helpers.git",
    ...
  }
}
```

> 💡 **특정 버전 설치**: URL 끝에 `#v1.0.0` 형식으로 태그를 지정할 수 있습니다.
> ```
> https://github.com/NAKZI-STAR/Avatar_Helpers.git#v1.0.0
> ```

### 수동 설치

1. [Releases](https://github.com/NAKZI-STAR/Avatar_Helpers/releases)에서 최신 `.unitypackage` 다운로드
2. Unity 프로젝트에 임포트

## 🛠️ 기능

### Local Blueprint Manager

여러 작업자가 같은 Avatar Scene/Prefab을 공유할 때 VRChat Blueprint ID를 프로젝트 로컬로 분리합니다.

- Avatar Root에 `LocalBlueprintBinding`을 추가하면 Git에 공유 가능한 고유 Avatar ID가 생성됩니다.
- 실제 `avtr_...` 값은 `UserSettings/LocalBlueprintSettings.asset`에만 저장됩니다.
- 관리 창: **Tools > VRChat > Local Blueprint Manager**
- 바인딩 컴포넌트는 플레이 모드 및 VRChat Avatar 빌드 대상에서 자동 제거됩니다. 원본 Scene/Prefab에는 유지됩니다.
- `VRC.SDKBase.IEditorOnly`를 구현하므로 VRChat SDK의 허용되지 않은 컴포넌트 경고 대상에서 제외됩니다.
- Apply 및 VRChat Avatar 빌드 시 로컬 DB의 Blueprint ID가 Pipeline 값을 항상 덮어씁니다.

Unity 프로젝트 루트의 `.gitignore`에 다음 항목을 추가하세요(일반 Unity `.gitignore`의 `/[Uu]ser[Ss]ettings/`도 동일하게 보호합니다).

```gitignore
/UserSettings/LocalBlueprintSettings.asset
```

### 1. Create Avatar Preset
아바타 프리셋을 생성하고 관리하는 도구입니다.

- **프리셋 저장**: 씬에 있는 아바타를 프리셋으로 저장
- **프리셋 기반 아바타 생성**: 저장된 프리셋에서 의존성이 분리된 새 아바타 생성
- **의존성 관리**: 복제할 에셋 타입 선택 및 개별 에셋 제외 가능
- **다중 선택**: Ctrl+클릭으로 개별 선택, Shift+클릭으로 범위 선택
- **Built-in/Packages 에셋**: 토글로 포함/제외 설정
- **자동 참조 재연결**: Material, Animator, Expression Menu/Parameters 등 자동 재연결
- **진행 상태 표시**: 복제/재연결 작업 진행률 표시

#### 🔧 파이프라인 시스템 (개발자용)
커스텀 에셋 타입에 대한 복제/재연결 로직을 확장할 수 있는 파이프라인 시스템을 제공합니다.

```csharp
using NAKZI.AvatarPreset.Pipeline;

[AssetClonePipelineFor(typeof(MyCustomAsset))]
public class MyCustomPipeline : IAssetClonePipeline
{
    public int Priority => 0;
    
    public bool OnRemap(Object asset, Dictionary<string, Object> clonedMap)
    {
        // 커스텀 재연결 로직
    }
}
```

자세한 내용은 `Editor/Pipeline/README_Pipeline.md`를 참조하세요.

**메뉴 위치**: `Nakzi Avatar Script > Create Avatar Preset`

### 2. Anchor Override Tool
SkinnedMeshRenderer의 Anchor Override를 일괄 설정하는 도구입니다.

**메뉴 위치**: `Nakzi Avatar Script > Anchor Override Tool`

### 3. Object Naming Tool
아바타 오브젝트의 이름을 일괄 변경하는 도구입니다.

**메뉴 위치**: `Nakzi Avatar Script > Object Naming Tool`

### 4. Avatar Bone Retargetor
의상 본 매칭/머지 도구

- **의상 본 매칭/머지 기능**: 의상 본과 아바타 본 1대1 매칭/머지 기능
- **접두사/접미사 제외 기능**: 사용자가 제외할 접두사/접미사 추가 기능
- **Undo기능**: 안정성 보장

## 📋 요구 사항

- Unity 2022.3 이상
- VRChat Avatars SDK 3.10.1 이상

## 📄 라이선스

MIT License - 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

## 👤 작성자

- **NAKZI-STAR**
- 📧 icho117118@gmail.com
- 🔗 [GitHub](https://github.com/NAKZI-STAR)

## 🐛 버그 리포트 & 기능 제안

[Issues](https://github.com/NAKZI-STAR/Avatar_Helpers/issues)에서 버그 리포트나 기능 제안을 해주세요.

