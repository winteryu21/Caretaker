# develop 통합 리뷰 보고서 (2026-05-28)

## 범위

- 대상 브랜치: `develop`
- 기준 커밋: `41204b2 feat(network): DEV-18 Additive Scene (#42)`
- 포함된 최근 작업:
  - DEV-16 RoomGraphSO 데이터
  - DEV-15 RoomVolume + RoomManager
  - 상호작용 판정 및 하이라이트
  - DEV-18 Additive Scene / GameFlow debug phase transition

## 결론

현재 `develop`은 개별 Module 단위로는 일부 진전이 있지만, 아직 "플레이 가능한 GameFlow"라고 보기 어렵다.

핵심 이유는 세 가지다.

1. `GameFlowManager`가 Major Interaction 완료를 Phase 전환으로 연결하지 않는다.
2. 네트워크 세션 이후 실제 플레이어 스폰/배치 계약이 없다.
3. Additive scene 구조와 카메라/UI/플레이어/룸 시스템의 책임 배치가 아직 하나의 통합 동작으로 닫히지 않았다.

다음 이슈를 진행하기 전 올바른 방향은 "기능별 스텁 추가"가 아니라, **세션 -> 플레이어 생성 -> 로컬 Phase scene 로드 -> 룸 진입 -> 상호작용 -> Major 완료 -> Phase 전환**의 얇은 vertical slice를 먼저 닫는 것이다.

## 검증 결과

### 실행한 검증

```powershell
git status --short
git log -5 --oneline --decorate
dotnet build .\Caretaker\Caretaker.sln --no-incremental
rg -n "NotImplementedException" .\Caretaker\Assets\_Project\Scripts
rg -n "RoomParticipant|RoomVolume|RoomManager|PlayerController|InteractionProbe" .\Caretaker\Assets\_Project\Prefabs .\Caretaker\Assets\_Project\Scenes
```

### 결과

- `develop` 워킹트리는 깨끗했다.
- `dotnet build`는 실패했다.
- Unity Test Runner는 이번 리뷰에서 직접 재실행하지 않았다.
- `RoomGraphDataTests`, `RoomManagerTests`는 이전 작업에서 통과 보고를 받았다.

## 주요 발견

### Critical: 현재 로컬 CLI 빌드가 실패한다

`dotnet build .\Caretaker\Caretaker.sln --no-incremental` 결과:

```text
GameFlowManager.cs(21,34): error CS0246: 'SceneLoader' 형식 또는 네임스페이스 이름을 찾을 수 없습니다.
```

근거:

- `Caretaker/Assets/_Project/Scripts/Core/GameFlowManager.cs:21`에서 `SceneLoader`를 참조한다.
- `Caretaker/Assets/_Project/Scripts/Core/SceneLoader.cs` 파일은 존재한다.
- `Caretaker/Assembly-CSharp.csproj`에는 `GameFlowManager.cs`와 `SessionRoleManager.cs`는 포함되어 있지만 `SceneLoader.cs`, `PhaseDebugInput.cs`가 없다.
- `.gitignore:62-64`에서 `*.csproj`, `*.sln`이 ignore되어 있어 Unity가 재생성하는 로컬 파일이라는 전제가 있다.

판단:

- Unity 에디터 안에서는 프로젝트 파일 재생성으로 가려질 가능성이 높다.
- 하지만 현재 팀의 검증 절차에서 `dotnet build`를 쓰고 있으므로, 이 상태는 "재현 가능한 CLI 검증"을 깨뜨린다.

권장 방향:

- CI/로컬 검증 기준을 하나로 정해야 한다.
- Unity batchmode test/build를 공식 검증으로 삼거나, `dotnet build` 전에 Unity 프로젝트 파일 재생성을 강제하는 절차를 문서화해야 한다.
- ignored `.csproj`를 수동 패치하는 방식은 지속 가능하지 않다.

### High: GameFlow가 Major Interaction 완료를 Phase 전환으로 연결하지 않는다

근거:

- `Caretaker/Assets/_Project/Scripts/Core/GameFlowManager.cs:89` `NotifyMajorComplete`는 Host Authority만 확인한 뒤 로그만 남긴다.
- `Caretaker/Assets/_Project/Scripts/Core/PhaseService.cs`에는 `IsPhaseComplete`가 있지만 `GameFlowManager`에서 사용되지 않는다.
- 실제 Phase 전환은 `PhaseDebugInput`의 E키 입력 -> `SubmitLocalPhaseAdvanceReady` -> 양쪽 ready flag -> `TransitionPhase` 경로에 의존한다.

영향:

- M1/M2/M3/M4가 완료되어도 GameFlow는 진행하지 않는다.
- 현재 구현은 "디버그 Phase 전환"이지 "게임 진행"이 아니다.
- Causality, Inventory, Interaction 작업이 붙어도 Major 완료가 GameFlow로 수렴하는 Interface가 없다.

권장 방향:

- `GameFlowManager`는 Stage 진행 상태의 authoritative Module이어야 한다.
- 최소 Interface:
  - `NotifyMajorComplete(PhaseId phaseId, string majorId)`
  - 내부 completed major set 기록
  - `PhaseService.IsPhaseComplete`로 완료 판정
  - 완료 시 `TransitionPhase`
- `PhaseDebugInput`은 Core 진행 규칙이 아니라 debug Adapter로 격리해야 한다.

### High: 네트워크 세션 이후 실제 플레이어가 없다

근거:

- `Caretaker/Assets/_Project/Scenes/NetworkLobby.unity:643`의 `PlayerPrefab`은 `{fileID: 0}`이다.
- `Caretaker/Assets/_Project/Scripts/Core/NetworkSessionController.cs:263`에서 `response.CreatePlayerObject = false`로 설정한다.
- `rg` 기준 Prefab/Scene 안에 `PlayerController`, `InteractionProbe`, `RoomParticipant`, `RoomVolume`, `RoomManager` 런타임 배치가 없다.

영향:

- Host/Client 세션은 열릴 수 있어도 플레이어 객체가 생성되지 않는다.
- RoomVolume, RoomManager, InteractionProbe는 테스트로는 통과하지만 실제 GameFlow에서는 호출 주체가 없다.
- Room Transition, Personal Inventory, Interaction, Enemy detection의 기반이 되는 player identity가 아직 없다.

권장 방향:

- 다음 통합 작업은 Network Player prefab 또는 명시적 player spawn Module이어야 한다.
- 플레이어 prefab에는 최소한 `NetworkObject`, `PlayerController`, `PlayerMotor2D`, `PlayerInputReader`, `InteractionProbe`, `RoomParticipant`, collider/rigidbody가 함께 있어야 한다.
- `TimelineRole`과 spawn point를 연결해 Past/Future 플레이어가 각자의 Phase scene에 배치되는 계약을 먼저 닫아야 한다.

### High: Additive phase scene과 Persistent scene의 카메라/UI 책임이 겹친다

근거:

- `Caretaker/Assets/_Project/Scenes/Persistent.unity`에 `Main Camera`와 `UI Canvas`가 있다.
- `Phase1_Past`, `Phase1_Future`, `Phase2_*`, `Phase3_*` scene에도 각각 `Main Camera`, `Canvas`, 일부 `EventSystem`이 있다.
- `Caretaker/Assets/_Project/Scripts/Gameplay/Player/InteractionProbe.cs:60`은 `Camera.main`을 Awake에서 캐시한다.

영향:

- Additive load 후 MainCamera가 여러 개 존재할 수 있다.
- `Camera.main` 선택이 의도한 로컬 timeline camera가 아닐 수 있다.
- AudioListener/UI/EventSystem 중복 경고와 입력 라우팅 혼란 가능성이 있다.
- Information Isolation을 카메라/씬 구성으로 보장하려면 카메라 책임이 더 명확해야 한다.

권장 방향:

- Persistent scene은 session/gameflow/network/bootstrap만 보유한다.
- Phase scene은 world content만 보유하거나, 반대로 timeline camera rig를 Phase scene이 소유한다면 Persistent의 camera/UI를 제거한다.
- 어느 쪽이든 "카메라 소유자"는 하나여야 한다.
- `InteractionProbe`는 `Camera.main`에 기대지 말고 `TimelineCameraRig` 또는 `CameraDirector`가 제공하는 명시적 Adapter를 통해 camera를 받는 편이 낫다.

### Medium: Room 시스템은 데이터와 이벤트는 좋지만 실제 배치/소유권 계약이 없다

근거:

- `RoomManager`는 플레이어별 current/visited state와 `OnRoomChanged`, `OnRoomVisited`를 제공한다.
- `RoomVolume`은 `RoomParticipant`를 통해 playerId를 해석한다.
- 그러나 Scene/Prefab 안에 `RoomVolume`, `RoomManager`, `RoomParticipant` 배치가 없다.
- `RoomVolume`은 serialized `_roomManager` 또는 parent lookup에 의존한다.

영향:

- Team B가 RoomVolume을 임의 위치에 배치하면 RoomManager 참조 누락이 쉽게 발생한다.
- `ReportRoomExit`은 현재 룸과 같은 roomId에서 나갈 때 current room을 빈 문자열로 지운다. 인접 RoomVolume 사이에 gap이 있거나 플레이어 콜라이더가 여러 개가 되면 transient empty room이 생긴다.

권장 방향:

- RoomVolume prefab과 배치 가이드를 만들어야 한다.
- RoomManager는 scene bootstrap에서 하나만 명확히 공급하고, RoomVolume은 inspector 참조를 필수로 검증하거나 parent 구조를 prefab으로 강제해야 한다.
- 플레이어 콜라이더가 복수화될 가능성이 생기면 participant별 contact count 방식으로 보강한다.

### Medium: Interaction Module은 판정과 실행 사이가 아직 닫히지 않았다

근거:

- `PlayerController`는 `InteractionService.TryProcessInteraction` 성공 시 `OnInteractionResolved`만 발행한다.
- 이 이벤트를 구독해 실제 Causality/Inventory/Presentation으로 넘기는 Adapter가 없다.
- `InteractionRequest.PointerScreenPosition`은 저장되지만 현재 판정에서는 쓰이지 않는다.
- `InteractionProbe`는 collider에 직접 붙은 `InteractableObject`만 찾는다. 자식 collider/부모 interactable 구조에서는 누락될 수 있다.

영향:

- "hover -> highlight"와 "입력 -> resolved event"는 가능하지만, 조사 텍스트 표시/아이템 획득/조작/인과 트리거 실행까지 이어지지 않는다.
- 상호작용 관련 정책이 `PlayerController`, `InteractionProbe`, `InteractionService`, `InteractableObject`에 흩어져 있어 다음 기능 추가 시 Interface가 얕아질 위험이 있다.

권장 방향:

- 상호작용은 세 Module로 분리하면 좋다.
  - Physics/Input Adapter: hover/proximity 후보 수집
  - InteractionResolver: 후보와 요청을 판정하는 순수 Module
  - InteractionExecutor: Causality/Inventory/UI로 실행을 라우팅하는 Module
- 삭제 테스트 관점에서 `InteractionService`가 단순 판정 wrapper로 남으면 얕은 Module이 된다. 실행 계약까지 포함할 때 Depth와 Leverage가 생긴다.

### Medium: Debug phase input이 runtime scene에 활성화되어 있다

근거:

- `Caretaker/Assets/_Project/Scripts/Core/PhaseDebugInput.cs:26`에서 매 프레임 E키를 검사한다.
- `Caretaker/Assets/_Project/Scenes/Persistent.unity:374-376`에 `PhaseDebugInput`이 있고 `_enableDebugInput: 1`이다.
- E키는 플레이어 상호작용 입력과도 겹친다.

영향:

- 개발 중에는 편하지만, 실제 플레이어 입력과 Phase 전환 debug 입력이 충돌할 수 있다.
- 빌드에 들어가면 비정상 Phase 전환 경로가 남는다.

권장 방향:

- `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싸거나 debug scene/prefab으로 분리한다.
- 최소한 기본값은 꺼두고 명시적으로 활성화해야 한다.

### Medium: 테스트 표면이 RoomGraph/RoomManager에 편중되어 있다

근거:

- 현재 Editor tests는 `RoomGraphDataTests`, `RoomManagerTests` 중심이다.
- `GameFlowManager`, `SceneLoader`, `SessionRoleManager` phase transition, `InteractionService` 통합 경로 테스트가 없다.

영향:

- 현재 가장 위험한 GameFlow/scene/player 통합 문제가 테스트로 잡히지 않는다.
- 개별 Module 테스트는 통과하지만 플레이 루프는 실패할 수 있다.

권장 방향:

- 다음 테스트는 "두 플레이어가 ready -> Persistent load -> local Phase scene load request"를 검증하는 PlayMode/Integration 테스트가 필요하다.
- Interaction은 `InteractionService` 단위 테스트와 `PlayerController -> InteractionResolved` 경로 테스트를 분리해서 잡는다.

### Low: 스캐폴딩 NotImplementedException이 많다

근거:

`rg` 기준 `CheckpointService`, `CausalityService`, `InventoryService`, `RadioService`, `CameraDirector`, `MinimapSystem`, `Enemy*`, `CausalReceiver`, `SplitViewManager` 등에 `NotImplementedException`이 남아 있다.

판단:

- 스프린트상 아직 구현 전 Module이 많다는 점은 자연스럽다.
- 다만 Scene에 연결되는 순간 즉시 런타임 crash가 된다.

권장 방향:

- 아직 배치하지 않을 Module은 scene/prefab에 연결하지 않는다.
- 배치해야 한다면 no-op 상태 또는 명확한 warning return으로 바꾼다.

## 온톨로지 질문과 답

### 이 프로젝트에서 "Room"은 무엇인가?

Room은 scene이 아니라 bounded gameplay space다. 따라서 RoomGraphSO는 데이터 원천이고, RoomVolume은 scene 안 감지 Adapter이며, RoomManager는 runtime state Module이다. 현재 이 구분은 코드상 대체로 맞지만, Scene 배치 계약이 없어 실제 Stage 안에서는 아직 작동하지 않는다.

### "Phase"는 scene인가, 진행 상태인가?

Phase는 진행 상태다. Additive scene은 Phase를 표현하는 Adapter일 뿐이다. 현재 코드는 `PhaseId -> sceneName`으로 직접 묶여 있어 단순하고 좋지만, GameFlow가 Major 완료를 처리하지 않아서 Phase가 진행 상태라기보다 debug scene 선택값처럼 동작한다.

### "Timeline Role"은 player identity인가, view filter인가?

Timeline Role은 gameplay assignment다. Player identity는 Netcode client id이고, view filter와 scene loading은 Timeline Role에서 파생된다. 현재 `SessionRoleManager`는 이 구분을 유지하고 있으나, 실제 player spawn/RoomParticipant 연결이 없어 identity가 gameplay object까지 도달하지 않는다.

### "Interaction"은 판정인가, 효과 실행인가?

Prototype 관점에서 Interaction은 판정만으로 충분하지 않다. Mutual Dependency와 Time Causality를 만들려면 Interaction은 Causality/Inventory/UI 실행까지 이어져야 한다. 현재 구현은 판정 Module까지이고, 실행 Module이 없다.

### "GameFlow"는 디버그 진행인가, Stage Contract의 권위자인가?

GameFlow는 Stage Contract의 권위자여야 한다. 지금은 Phase debug input을 받아 scene을 넘기는 성격이 강하다. 다음 단계에서 Major 완료, checkpoint, shared failure가 GameFlow로 수렴해야 한다.

## 소프트웨어 공학적 방향성

### 1. Vertical slice를 먼저 닫기

다음 이슈는 개별 시스템 확장보다 다음 흐름을 한 번 통과시키는 데 집중해야 한다.

```text
NetworkLobby
-> both ready
-> Persistent scene load
-> Past/Future player spawn
-> local Phase scene additive load
-> RoomVolume enter
-> InteractableObject resolve
-> MajorComplete event
-> GameFlowManager phase transition
```

### 2. Core Module의 Interface를 깊게 만들기

현재 일부 Module은 얕다. 호출자가 너무 많은 실행 순서와 주변 지식을 알아야 한다.

우선 깊게 만들어야 하는 Module:

- `GameFlowManager`: Major 완료, Phase 전환, scene load 지시를 한 Interface 뒤에 숨겨야 한다.
- `PlayerSpawn/PlayerSession`: client id, Timeline Role, spawn point, RoomParticipant를 묶어야 한다.
- `Interaction`: 후보 수집, 판정, 실행 라우팅의 책임을 명확히 나눠야 한다.

### 3. Scene은 Adapter로 취급하기

Scene 이름과 Build Settings는 구현 상세다. Domain은 Phase/Timeline/Room이다.

권장 구조:

- Persistent: NetworkManager 생존, SessionRoleManager, GameFlowManager, SceneLoader
- Phase scenes: timeline별 world content, RoomVolume, interactables, enemies
- Player prefab: 네트워크 객체와 gameplay identity
- UI/Camera: Persistent 소유 또는 Phase 소유 중 하나로 고정

### 4. 검증 기준을 고정하기

현재 `dotnet build`는 ignored Unity-generated project file에 의존한다. 팀이 계속 이 명령을 검증으로 쓸 거라면 재생성 절차가 필요하다.

권장:

- 공식 검증: Unity batchmode EditMode/PlayMode tests
- 보조 검증: `dotnet build`는 프로젝트 파일 재생성 후 실행
- PR template에 어떤 검증을 수행했는지 명시

## 다음 이슈 전 권장 작업 순서

1. Build/verification 경로 정리
   - Unity-generated `.csproj` 의존성을 인정하고 검증 명령을 정한다.

2. Player spawn/identity vertical slice
   - Network Player prefab 생성
   - `RoomParticipant`와 `TimelineRole` 연결
   - Past/Future spawn point 선택

3. GameFlowManager를 실제 Major 완료 기반으로 전환
   - completed major IDs 저장
   - `PhaseService.IsPhaseComplete` 사용
   - debug ready path는 별도 Adapter로 격리

4. Persistent/Phase scene 책임 정리
   - 중복 Main Camera/Canvas/EventSystem 제거
   - local timeline camera를 명시적으로 공급

5. Interaction 실행 Adapter 추가
   - `OnInteractionResolved` 이후 Causality/Inventory/UI 중 하나로 이어지는 최소 실행 경로 구현

## 최종 판단

현재 `develop`은 "데이터와 일부 런타임 Module이 병렬로 붙기 시작한 상태"다. 방향은 맞지만, 통합 축이 아직 약하다.

다음 작업의 목표는 새 시스템을 더 많이 추가하는 것이 아니라, 이미 merge된 Module들이 하나의 Stage 흐름에서 서로를 실제로 호출하게 만드는 것이다. 이를 닫지 않으면 이후 Causality, Inventory, AI, Minimap을 붙일수록 shallow Module과 임시 Adapter가 늘어날 가능성이 높다.
