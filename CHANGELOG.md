# Changelog

이 프로젝트의 모든 주요 변경 사항이 이 파일에 기록됩니다.

## [1.3.5] - 2026-09-19

### 수정됨

- 자동 동기화 시 Pipeline Blueprint ID가 이미 동일한 경우 Undo 및 Dirty 처리를 생략
- Scene을 저장한 뒤 변경사항 표시가 반복해서 다시 생기는 문제 수정

## [1.3.4] - 2026-09-19

### 변경됨

- 컴포넌트 이름을 `LocalBlueprintBinding`에서 `LocalBlueprintBinder`로 변경하고 기존 직렬화 데이터 마이그레이션 정보 추가
- VRChat SDK/Modular Avatar의 `IEditorOnly` 제거 단계와 중복되던 빌드 콜백의 수동 컴포넌트 제거 코드 삭제
- 빌드 콜백은 로컬 Blueprint ID 적용만 담당하도록 단순화
- Gesture Manager는 `IEditorOnly`를 제거하지 않으므로 일반 Play Mode 제거 로직은 유지

## [1.3.3] - 2026-09-19

### 변경됨

- Scene 또는 Prefab Stage에 로컬 매핑이 존재하면 Blueprint ID를 PipelineManager에 즉시 자동 적용
- 로컬 DB에 Blueprint ID를 저장한 직후 같은 Avatar ID를 가진 바인딩을 자동 동기화
- 자동 동기화로 대체된 Inspector Apply, Manager Apply 및 Apply All 버튼 제거
- Save, Clear Pipeline ID, Remove Local Mapping 등 나머지 관리 버튼은 유지

## [1.3.2] - 2026-09-19

### 수정됨

- `LocalBlueprintBinding`에 `VRC.SDKBase.IEditorOnly`를 적용하여 VRChat SDK의 허용되지 않은 컴포넌트 Auto Fix 경고에서 제외
- 기존 플레이 모드 및 빌드 전처리 제거 로직은 이중 안전장치로 유지

## [1.3.1] - 2026-09-19

### 변경됨

- Local Blueprint 적용 시 Pipeline의 기존 Blueprint ID와 관계없이 로컬 DB 값을 항상 덮어쓰도록 변경
- VRChat Avatar 빌드 전에도 로컬 DB 값을 단일 기준으로 강제 적용

## [1.3.0] - 2026-09-19

### 추가됨

- **Local Blueprint Manager**: 여러 작업자가 공유하는 Avatar의 Blueprint ID를 작업자별 로컬 설정으로 분리
- Avatar별 영구 GUID와 `UserSettings/LocalBlueprintSettings.asset` 기반 로컬 매핑
- 개별/일괄 Apply, Save, Clear 및 충돌 방지 UI
- 중복 GUID, 누락/복수 PipelineManager, 잘못된 Blueprint ID 검증
- VRChat Avatar 빌드 직전 빈 Pipeline ID 자동 적용
- 플레이 모드와 VRChat 빌드 대상에서 `LocalBlueprintBinding` 컴포넌트 자동 제거

### 문서화

- 로컬 Blueprint 설정 파일의 Git 제외 방법과 사용 흐름 추가

## [1.2.1] - 2026-09-14

### 수정됨

- VRChat Avatars SDK 의존성을 `3.10.1` 고정에서 `>=3.10.1 <4.0.0` 범위로 변경하여 SDK 업데이트 시 발생하는 VPM 버전 제한 충돌 완화
- 이 변경은 의존성 허용 범위 조정이며, 각 SDK 버전의 Unity 동작 호환성은 별도 확인 필요

---

## [1.2.0] - 2026-02-19

### 추가됨

- **Avatar Bone Retargetor**: 아바타 본 서칭/머지 도구
  - 아바타 의상 본 매칭/머지 기능
  - 접미사/접두사 제외 기능
  - 머지 할 본의 위치 사용자 커스텀 기능
  - Undo 기능 제공 (안정성 보장)

---

## [1.1.1] - 2026-02-14

### 수정됨

- **Object Naming Tool**: 오브젝트 네이밍 도구
  - 타겟 오브젝트 필드 GUI를 제공하지 않던 문제 수정

---

## [1.1.0] - 2025-12-11

### 추가됨
- **파이프라인 시스템**: 에셋 복제/재연결 로직의 확장성 개선
  - `IAssetClonePipeline` 인터페이스로 커스텀 파이프라인 정의 가능
  - `[AssetClonePipelineFor]` 어트리뷰트로 처리할 타입 지정
  - `AssetClonePipelineManager`가 자동으로 파이프라인 등록 및 실행
  - 우선순위 시스템으로 기존 파이프라인 오버라이드 가능
  - 내장 파이프라인: AnimatorController, AnimatorOverrideController, Material, VRCExpressionsMenu, ScriptableObject

- **다중 선택 기능**: Dependency Filters에서 에셋 다중 선택
  - Ctrl+클릭: 개별 항목 선택/해제
  - Shift+클릭: 범위 선택
  - 선택된 항목 일괄 제외/포함 버튼

- **진행 상태 표시**: Unity 네이티브 프로그레스 바로 복제/재연결 진행률 표시

- **작업 중 버튼 비활성화**: 복제 작업 중 UI 버튼 자동 비활성화

### 개선됨
- 디버그 로그 간소화 (필수 정보와 에러만 출력)
- 코드 구조 개선 및 모듈화

---

## [1.0.1] - 2025-12-11

### 수정됨
- **Create Avatar Preset**: VRCExpressionsMenu 참조 재연결 버그 수정
  - SubMenu, Icon, Parameters 참조가 올바르게 복제된 에셋으로 연결되지 않던 문제 해결
  - SerializedProperty Iterator를 사용한 안정적인 참조 재연결 구현

---

## [1.0.0] - 2025-12-10

### 추가됨
- **Create Avatar Preset**: 아바타 프리셋 생성 및 관리 도구
  - 씬 아바타를 프리셋으로 저장
  - 프리셋 기반 새 아바타 생성 (의존성 분리)
  - 의존성 필터링 기능 (타입별 제외 설정)
  - Built-in/Packages 에셋 포함 토글
  - Material, Animator, Expression Menu/Parameters 자동 재연결
  - VRCExpressionsMenu의 Parameters 필드 자동 연결

- **Anchor Override Tool**: Anchor Override 일괄 설정 도구
  - SkinnedMeshRenderer의 Anchor Override 일괄 변경

- **Object Naming Tool**: 오브젝트 네이밍 도구
  - 아바타 오브젝트 이름 일괄 변경

- **Editor Helper**: 에디터 유틸리티 함수 모음
  - Texture2D 생성 헬퍼
  - 기타 에디터 유틸리티

### 의존성
- Unity 2022.3 이상
- VRChat Avatars SDK 3.10.1 이상

