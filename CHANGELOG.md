# Changelog

이 프로젝트의 모든 주요 변경 사항이 이 파일에 기록됩니다.

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
- VRChat Avatars SDK 3.7.0 이상

