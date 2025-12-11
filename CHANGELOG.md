# Changelog

이 프로젝트의 모든 주요 변경 사항이 이 파일에 기록됩니다.

## [1.1.0] - 2024-12-11

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

## [1.0.1] - 2024-12-11

### 수정됨
- **Create Avatar Preset**: VRCExpressionsMenu 참조 재연결 버그 수정
  - SubMenu, Icon, Parameters 참조가 올바르게 복제된 에셋으로 연결되지 않던 문제 해결
  - SerializedProperty Iterator를 사용한 안정적인 참조 재연결 구현

---

## [1.0.0] - 2024-12-10

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

