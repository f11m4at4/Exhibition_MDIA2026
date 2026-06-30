# Task 01. `HO_PlayerController`

## 목적
- 전시 프로토타입의 기본 조작을 담당하는 플레이어 컨트롤러를 만든다.
- `WASD` 이동, `Mouse` 시점 회전, `E` 입력 수신, 조작 잠금 상태를 한 클래스 안에서 처리한다.

## 구현 대상
- 클래스명/파일명: `HO_PlayerController`
- 타입: `MonoBehaviour`
- 클래스 상단에 "플레이어 이동과 작품 상호작용 입력을 처리한다" 주석을 작성한다.
- 주요 함수에 입력 처리, 이동 처리, 대상 감지 역할 주석을 작성한다.

## 선행 조건
- `Exhibition` 씬이 존재한다.
- 플레이어 루트 오브젝트, 카메라, 충돌 처리를 위한 `CharacterController`를 씬에 배치할 수 있다.

## 이번 Task의 결과물
- `HO_PlayerController.cs`
- 플레이어 루트 오브젝트 1개
- 자식 카메라 1개
- `CharacterController` 기반 1인칭 이동 설정

## 구현 범위
- Old Input System의 `Horizontal`, `Vertical`, `Mouse X`, `Mouse Y` 축을 사용한다.
- 걷기만 지원하고 달리기, 점프는 넣지 않는다.
- 마우스 상하 회전은 카메라에, 좌우 회전은 플레이어 루트에 적용한다.
- 조작 잠금 상태에서는 입력을 읽더라도 이동과 회전을 실제 반영하지 않는다.
- `E` 입력은 우선 수신 구조를 넣고, 실제 상호작용 호출은 다음 Task 연결을 고려해 메서드 자리만 잡아둔다.
- 전방 Raycast 또는 SphereCast로 현재 상호작용 대상 1개만 유지하는 구조를 준비한다.

## 권장 직렬화 필드
- `CharacterController`
- 카메라 루트 또는 자식 카메라 참조
- 이동 속도
- 마우스 감도
- 중력
- 시점 상하 회전 제한값
- 상호작용 거리
- 상호작용 레이어 마스크
- 이후 연결할 `HO_UIManager`, `HO_CuratorPresenter` 참조

## 권장 공개 메서드
- `SetControlLocked(bool isLocked)`
- 조작 잠금 상태 확인 메서드 1개

## 구현 단계
1. `Assets/Scripts` 아래에 `HO_PlayerController.cs` 파일을 만들고 클래스/주요 함수 주석을 작성한다.
2. `CharacterController`를 요구하는 직렬화 필드를 선언하고 이동 속도, 마우스 감도, 중력 값을 `[SerializeField]`로 노출한다.
3. `Update`에서 입력 읽기, 회전 처리, 이동 처리, `E` 입력 체크를 분리된 함수로 호출한다.
4. 카메라 상하 회전 각도를 누적 관리하고 과도한 상하 각도는 제한한다.
5. 조작 가능 여부를 제어하는 `SetControlLocked(bool isLocked)` 같은 공개 메서드를 만든다.
6. `CharacterController` 누락 시 로그를 남기고 동작을 중단한다.
7. 현재 감상 대상 저장용 필드를 미리 준비하되, 실제 대상 데이터 타입 연결은 뒤 Task에서 마무리한다.
8. 감지 대상이 바뀌면 이전 대상 안내를 끄고 새 대상 안내를 요청할 수 있게 분기 자리를 마련한다.

## 씬/인스펙터 작업
1. `Player` 루트 오브젝트를 만들고 `CharacterController`와 `HO_PlayerController`를 붙인다.
2. `Player` 자식으로 `CameraRoot`와 `Main Camera`를 두고 눈높이 위치로 올린다.
3. `HO_PlayerController`의 카메라 참조 필드에 자식 카메라 또는 카메라 루트를 연결한다.
4. `Interact Layer Mask`는 작품/방 소개 트리거만 감지하도록 맞춘다.
5. 시작 위치를 프롤로그 홀 고정 지점에 둔다.

## 완료 기준
- 키보드와 마우스로 1인칭 이동/회전이 가능하다.
- 조작 잠금 호출 시 이동과 회전이 멈춘다.
- `E` 입력 수신용 분리 함수가 존재한다.
- 상호작용 대상 1개를 유지하는 구조가 코드에 준비돼 있다.
- 이후 Task에서 작품 데이터와 UI를 연결할 수 있도록 참조 구조가 비어 있더라도 안전하게 유지된다.

## 다음 Task로 넘길 연결 포인트
- `HO_ExhibitData` 연결 전까지는 현재 감상 대상 필드를 비운 상태로 둔다.
- 이후 `HO_UIManager`, `HO_CuratorPresenter` 참조를 인스펙터로 받을 수 있게 직렬화 필드 자리를 남긴다.
