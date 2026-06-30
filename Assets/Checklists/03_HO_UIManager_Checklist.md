# Checklist 03. `HO_UIManager`

## 문서/정적 확인
- [x] Task 문서 기준으로 `PromptPanel`, `NarrationPanel`, `EndingPanel` 3개 패널만 우선 구성한다.
- [x] 한 번에 하나의 주요 패널만 열리도록 보호 로직을 구현한다.
- [x] `HasOpenBlockingPanel()` 공개 메서드를 만든다.
- [x] `단계별수행절차.md`의 Canvas, 패널, 텍스트 배치 이름을 맞춰 반영한다.

## 코드/구성 확인
- [x] `HO_UIManager.cs` 파일명과 클래스명이 일치한다.
- [x] 클래스와 주요 공개/비공개 함수에 역할 설명 주석이 있다.
- [x] 프롬프트, 내레이션, 종료 메시지 표시/닫기 메서드를 분리했다.
- [x] 필수 참조 누락 시 오류 로그 후 `enabled = false`로 안전 중단되도록 처리했다.
- [x] 시작 시 모든 주요 패널이 꺼진 상태가 되도록 `Awake()`에서 정리한다.

## 인스펙터 확인
- [x] `Canvas_ExhibitionUI` 아래에 프롬프트 UI를 배치했다.
- [x] `Canvas_ExhibitionUI` 아래에 내레이션 UI를 배치했다.
- [x] `Canvas_ExhibitionUI` 아래에 종료 메시지 UI를 배치했다.
- [x] `HO_UIManager`의 패널/텍스트 참조 필드를 모두 연결했다.

## Play Mode 검증
- [ ] 미검증 시작 시 모든 UI 패널이 닫힘 상태인지 확인
- [ ] 미검증 상호작용 가능 상태에서만 프롬프트가 표시되는지 확인
- [ ] 미검증 내레이션 패널이 열릴 때 다른 주요 패널과 겹치지 않는지 확인
- [ ] 미검증 종료 메시지가 다른 패널보다 우선 표시되는지 확인
