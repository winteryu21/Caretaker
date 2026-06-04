# 코드 품질 리뷰 보고서 (2026-06-01)

## 범위

- 대상: `Caretaker/Assets/_Project/Scripts`, `Caretaker/Assets/_Project/Tests`
- 제외: `Caretaker/Packages`, `Caretaker/Assets/TextMesh Pro` 같은 외부/샘플 패키지 코드
- 기준 문서:
  - `AGENTS.md`
  - `CONTEXT.md`
  - `docs/technical/coding-standards.md`
  - `docs/technical/architecture.md`
  - `docs/design/DSD.md`

## 정량 요약

| 항목 | 결과 |
| --- | ---: |
| 프로덕션 C# 스크립트 | 75개 |
| 프로덕션 C# 라인 수 | 6,259줄 |
| 테스트 C# 스크립트 | 11개 |
| 테스트 C# 라인 수 | 864줄 |
| `NotImplementedException` | 15건 |
| 네임스페이스 없는 프로덕션 스크립트 | 1개 |

## 검증 결과

```powershell
dotnet build Caretaker/Caretaker.sln --no-restore
```

- 결과: 성공
- 경고: 11개
- 주요 경고 유형:
  - `System.Net.Http`, `System.IO.Compression`, `System.Numerics.Vectors` 버전 충돌
  - `Packages/com.coplaydev.unity-mcp` 내부 deprecated Unity API 사용

```powershell
dotnet test Caretaker/Caretaker.sln --no-build
```

- 결과: 오류 없이 종료
- 단, 별도 .NET test project가 없어 Unity EditMode 테스트를 실제로 실행한 결과로 보기는 어렵다.

```powershell
Unity.exe -batchmode -quit -projectPath D:\Dev\Caretaker\Caretaker -runTests -testPlatform editmode
```

- 결과: 실행 불가
- 사유: 동일 Unity 프로젝트가 이미 다른 Unity 인스턴스에서 열려 있어 batchmode가 중단됨.

## 결론

현재 코드는 컴파일 가능한 상태이고, `Room`, `Inventory`, `Radio` 일부 Module은 테스트와 구현이 맞물려 있다. 다만 전체 프로젝트 품질 관점에서는 아직 "구현된 시스템"과 "명세용 스텁"이 같은 런타임 경로에 섞여 있다.

가장 큰 리스크는 세 가지다.

1. 여러 public Module이 `NotImplementedException`을 그대로 던져 런타임 호출 시 즉시 실패한다.
2. `Runtime State` 타입이 public mutable field를 노출해 서비스의 불변식이 우회될 수 있다.
3. 의존성 탐색, debug UI, 입력 처리, 네트워크/음성 전송이 일부 큰 MonoBehaviour에 모여 있어 Locality가 약하다.

## 주요 발견

### High: 런타임 호출 가능한 스텁이 아직 많다

다음 Module들은 public 메서드가 `NotImplementedException`을 던진다.

- `CheckpointService.CreateCheckpoint`, `LoadLatestSnapshot`: `Caretaker/Assets/_Project/Scripts/Core/CheckpointService.cs:20`, `:29`
- `CausalityService.SubmitTrigger`, `QueryMajorProgress`: `Caretaker/Assets/_Project/Scripts/World/CausalityService.cs:27`, `:37`
- `AlertService.RaiseAlert`: `Caretaker/Assets/_Project/Scripts/Gameplay/AI/AlertService.cs:22`
- `EnemyStateMachine.TickState`: `Caretaker/Assets/_Project/Scripts/Gameplay/AI/EnemyStateMachine.cs:18`
- `EnemyPerception2D.EvaluateSight`: `Caretaker/Assets/_Project/Scripts/Gameplay/AI/EnemyPerception2D.cs:29`
- `CameraDirector.FocusRoom`: `Caretaker/Assets/_Project/Scripts/Presentation/CameraDirector.cs:27`
- `MinimapSystem.RevealVisitedRoom`: `Caretaker/Assets/_Project/Scripts/Presentation/MinimapSystem.cs:28`
- `SplitViewManager.EnableSplitView`, `DisableSplitView`: `Caretaker/Assets/_Project/Scripts/Presentation/SplitViewManager.cs:28`, `:36`
- `InteractionPromptPresenter.ShowPrompt`, `HidePrompt`: `Caretaker/Assets/_Project/Scripts/Presentation/InteractionPromptPresenter.cs:27`, `:35`
- `CausalityIndicatorPresenter.ShowCausalityPulse`: `Caretaker/Assets/_Project/Scripts/Presentation/CausalityIndicatorPresenter.cs:26`
- `CausalReceiver.ApplyState`: `Caretaker/Assets/_Project/Scripts/World/CausalReceiver.cs:27`

영향:

- 컴파일은 통과하지만 실제 Stage 흐름에서 호출되면 즉시 크래시가 난다.
- DSD의 주요 계약인 Time Causality, Checkpoint, AI Alert, Presentation 계층이 "존재하는 것처럼 보이지만 동작하지 않는" 상태다.

권장:

- 아직 사용할 수 없는 Module은 scene/prefab에 연결하지 않거나, 명확한 no-op과 warning return으로 바꾼다.
- 다음 스프린트에서 public Interface가 노출된 Module부터 최소 동작을 닫는다.

### High: Runtime State가 public mutable field를 그대로 노출한다

코딩 기준은 public field 금지와 `[SerializeField] private`를 요구하지만, 여러 Runtime State가 public field를 사용한다.

- `GameSessionState`: `Caretaker/Assets/_Project/Scripts/Core/GameSessionState.cs:15`
- `CheckpointSnapshot`: `Caretaker/Assets/_Project/Scripts/Core/CheckpointSnapshot.cs:13`
- `CausalResult`: `Caretaker/Assets/_Project/Scripts/Shared/CausalResult.cs:14`
- `CausalityState`: `Caretaker/Assets/_Project/Scripts/World/CausalityState.cs:15`
- `RoomVisitState`: `Caretaker/Assets/_Project/Scripts/World/RoomVisitState.cs:14`
- `InventoryState`: `Caretaker/Assets/_Project/Scripts/Gameplay/Inventory/InventoryState.cs:13`
- `PlayerRuntimeState`: `Caretaker/Assets/_Project/Scripts/Gameplay/Player/PlayerRuntimeState.cs:16`
- `AlertState`: `Caretaker/Assets/_Project/Scripts/Gameplay/AI/AlertState.cs:12`
- `SplitViewState`: `Caretaker/Assets/_Project/Scripts/Presentation/SplitViewState.cs:12`

특히 `InventoryService.GetState()`는 변경 가능한 `InventoryState` 자체를 반환한다. 호출자는 `InventoryService.MAX_SLOT_COUNT`, 중복 방지, 소모 처리 같은 규칙을 거치지 않고 `OwnedItemIds`를 직접 수정할 수 있다.

영향:

- Interface가 Implementation의 내부 컬렉션을 노출한다.
- 테스트 표면도 서비스 메서드가 아니라 내부 상태 구조에 묶인다.
- 저장/네트워크 payload 요구 때문에 public field가 필요하다면, 프로젝트 컨벤션에 예외 규칙이 문서화되어야 한다.

권장:

- Runtime State DTO에 한해 예외를 인정할지 먼저 결정한다.
- 예외를 인정하지 않는다면 읽기 전용 조회와 명령 메서드로 불변식을 서비스 내부에 집중시킨다.

### High: 주요 설계 계약이 아직 GameFlow까지 닫히지 않았다

`GameFlowManager.NotifyMajorComplete`는 Host Authority만 확인하고 로그만 남긴다.

- `Caretaker/Assets/_Project/Scripts/Core/GameFlowManager.cs:87`
- `Caretaker/Assets/_Project/Scripts/Core/GameFlowManager.cs:96`

`RollbackToCheckpoint`도 아직 warning 로그만 남긴다.

- `Caretaker/Assets/_Project/Scripts/Core/GameFlowManager.cs:105`
- `Caretaker/Assets/_Project/Scripts/Core/GameFlowManager.cs:110`

영향:

- Major Interaction 완료가 Phase 전환으로 이어지지 않는다.
- Shared Failure와 Checkpoint Spawn이 Runtime State 복원으로 닫히지 않는다.
- DSD의 Core 흐름이 아직 debug phase advance 경로에 의존한다.

권장:

- `GameFlowManager`가 Phase 진행, Major 완료, Checkpoint 복원을 수렴시키는 Core Module이 되어야 한다.
- debug phase advance는 별도 Adapter로 격리한다.

### Medium: 의존성 탐색이 런타임 Module에 넓게 퍼져 있다

`FindAnyObjectByType` 사용 위치:

- `GameFlowManager`: `Caretaker/Assets/_Project/Scripts/Core/GameFlowManager.cs:230`, `:235`
- `NetworkSessionController`: `Caretaker/Assets/_Project/Scripts/Core/NetworkSessionController.cs:182`, `:187`
- `NetworkDebugLauncher`: `Caretaker/Assets/_Project/Scripts/Core/NetworkDebugLauncher.cs:173`, `:179`
- `PhaseDebugInput`: `Caretaker/Assets/_Project/Scripts/Core/PhaseDebugInput.cs:41`
- `LocalWorldPlayerSpawner`: `Caretaker/Assets/_Project/Scripts/Gameplay/Player/LocalWorldPlayerSpawner.cs:107`
- `RadioInputController`: `Caretaker/Assets/_Project/Scripts/Gameplay/Radio/RadioInputController.cs:88`
- `LocalPlayerCameraFollow`: `Caretaker/Assets/_Project/Scripts/Presentation/LocalPlayerCameraFollow.cs:46`
- `RadioHudPresenter`: `Caretaker/Assets/_Project/Scripts/Presentation/RadioHudPresenter.cs:57`, `:62`

영향:

- `FindObjectOfType` 금지 의도와 같은 계열의 씬 전역 탐색이다.
- Scene 구성 순서와 singleton 존재 여부가 암묵 Interface가 된다.
- 테스트에서는 통과해도 실제 씬 배치가 바뀌면 깨질 수 있다.

권장:

- scene/prefab에서 명시 참조를 연결하거나 bootstrap Module이 필요한 Adapter를 주입한다.
- debug Module은 예외로 남기되 이름과 폴더, 빌드 포함 여부를 명확히 한다.

### Medium: `MicrophoneVoiceTransport`의 책임이 과도하게 넓다

`MicrophoneVoiceTransport` 한 파일이 마이크 캡처, Opus/mu-law 인코딩 선택, NGO named message 송수신, 서버 relay, playback ring buffer, resampling, `OnAudioFilterRead`, debug `OnGUI`까지 담당한다.

- 파일 크기: 484줄
- named message 등록/해제: `Caretaker/Assets/_Project/Scripts/Gameplay/Radio/MicrophoneVoiceTransport.cs:107`, `:120`
- network frame writer: `Caretaker/Assets/_Project/Scripts/Gameplay/Radio/MicrophoneVoiceTransport.cs:281`
- message handler: `Caretaker/Assets/_Project/Scripts/Gameplay/Radio/MicrophoneVoiceTransport.cs:294`
- audio thread callback: `Caretaker/Assets/_Project/Scripts/Gameplay/Radio/MicrophoneVoiceTransport.cs:378`
- debug overlay 기본 활성화: `Caretaker/Assets/_Project/Scripts/Gameplay/Radio/MicrophoneVoiceTransport.cs:32`, `:438`

영향:

- 테스트 가능한 Interface가 좁지 않다.
- 오디오 품질 문제, 네트워크 relay 문제, UI debug 문제가 한 Module의 Implementation 안에서 섞인다.
- 여러 `RadioSystem` 인스턴스가 생기면 같은 `VOICE_FRAME_MESSAGE` handler 등록/해제가 충돌할 가능성이 있다.

권장:

- 송신권 판정은 이미 `RadioService`로 잘 분리되어 있으므로, 음성 전송도 캡처/코덱/네트워크 relay/재생 버퍼의 Seam을 분리한다.
- debug overlay는 개발 전용 Adapter로 빼거나 기본 비활성화한다.

### Medium: `InteractionService`가 Domain Service라고 보기 어렵다

`InteractionService`는 DSD상 Domain Service로 표시되어 있지만, 실제 Interface가 `PlayerController`, `InteractableObject`, `Transform` 위치 계산에 의존한다.

- `TryProcessInteraction` 인자: `Caretaker/Assets/_Project/Scripts/World/InteractionService.cs:29`
- `PlayerController actor` 직접 의존: `Caretaker/Assets/_Project/Scripts/World/InteractionService.cs:34`
- 아이템 검증 TODO: `Caretaker/Assets/_Project/Scripts/World/InteractionService.cs:105`

영향:

- 물리/scene Adapter와 순수 판정 Implementation이 섞여 있다.
- 아이템 요구 조건은 일부 `PlayerController.HandleUseItemRequested`와 `InventoryController`에 흩어져 있다.
- Interaction의 Interface가 얕아져 호출자가 hover/proximity/type/distance/actor를 모두 알고 있어야 한다.

권장:

- 후보 수집, 판정, 실행 라우팅의 책임을 명확히 나눈다.
- Domain Service가 유지되어야 한다면 Unity 타입을 넘기지 않는 입력 모델로 좁힌다.

### Medium: `PlayerMotor2D`만 네임스페이스와 일부 스타일이 다르다

`PlayerMotor2D`는 프로덕션 스크립트 중 유일하게 `namespace`가 없다.

- 파일 주석: `Caretaker/Assets/_Project/Scripts/Gameplay/Player/PlayerMotor2D.cs:3`
- 클래스 선언: `Caretaker/Assets/_Project/Scripts/Gameplay/Player/PlayerMotor2D.cs:7`
- 들여쓰기 불일치: `Caretaker/Assets/_Project/Scripts/Gameplay/Player/PlayerMotor2D.cs:254`
- `GetComponent` fallback: `Caretaker/Assets/_Project/Scripts/Gameplay/Player/PlayerMotor2D.cs:387`

영향:

- 같은 `Gameplay.Player` 폴더의 `PlayerController`, `PlayerInputReader`, `InteractionProbe`와 namespace 일관성이 깨진다.
- Unity serialization 때문에 namespace 변경은 prefab/script reference 영향을 확인하면서 처리해야 한다.

권장:

- Unity guid는 `.meta`로 유지되므로 namespace 추가 자체는 가능하지만, prefab과 컴파일 참조를 함께 검증해야 한다.
- 수정 시 `PlayerController`의 `RequireComponent(typeof(PlayerMotor2D))`도 같은 namespace 안에서 정상 컴파일되는지 확인한다.

### Medium: 테스트 커버리지가 위험 지점보다 안정 지점에 치우쳐 있다

현재 테스트는 `InventoryService`, `RadioService`, `RoomManager`, `RoomGraphData`, `EnemyController` 순찰, prefab 구성 검증에 집중되어 있다.

커버리지 공백:

- `CausalityService`, `CausalityManager`
- `CheckpointService`, `GameFlowManager.RollbackToCheckpoint`
- `AlertService`, `EnemyPerception2D`, `EnemyStateMachine`
- `CameraDirector`, `MinimapSystem`, `SplitViewManager`
- `InteractionService`의 아이템 요구 조건
- `MicrophoneVoiceTransport`의 relay/playback buffer

영향:

- 가장 중요한 Time Causality, Shared Failure, Information Isolation, Split View Escape가 테스트로 보호되지 않는다.
- 현재 테스트는 "있는 Module"의 일부 동작은 잡지만, Prototype의 end-to-end 계약은 잡지 못한다.

권장:

- 다음 테스트 우선순위는 `CausalityService`, `GameFlowManager`, `CheckpointService`, `InteractionService`다.
- Unity batchmode 실행 가능성을 확보해 PR 검증 명령을 고정한다.

### Low: 주석은 전반적으로 성실하지만 템플릿/분류 주석이 남아 있다

좋은 점:

- public API의 XML summary가 많은 파일에 존재한다.
- 복잡한 이동/점프/순찰 로직에는 "왜"를 설명하는 주석이 꽤 있다.

아쉬운 점:

- 빈 스텁에 `// 1. Serialize 필드`, `// 2. private 필드` 같은 템플릿 주석이 남아 있다.
- 예: `CausalityManager.cs:15`, `CameraDirector.cs:15`, `SplitViewManager.cs:16`, `MinimapSystem.cs:15`
- `EnemyController`와 `PlayerMotor2D`에는 메서드 이름만으로 알 수 있는 분류 주석도 많다.

권장:

- 빈 구조 설명 주석은 구현 전에는 제거하거나 이슈 링크가 있는 TODO로 바꾼다.
- "무엇"을 반복하는 주석보다, 규칙 선택의 이유와 설계 제약만 남긴다.

### Low: public API XML 문서화가 일부 핵심 Module에서 빠져 있다

`NetworkSessionController`는 public class와 public 메서드가 많지만 XML 주석이 없다.

- class: `Caretaker/Assets/_Project/Scripts/Core/NetworkSessionController.cs:11`
- public methods: `:55`, `:74`, `:93`, `:115`, `:126`, `:137`, `:147`

`SessionRoleManager`도 public 이벤트와 public 메서드가 많은 핵심 네트워크 Module인데 XML 주석이 일부만 있다.

영향:

- 외부 호출자가 알아야 하는 ordering, authority, error mode가 코드에서 바로 드러나지 않는다.
- DSD의 Host Authority 계약을 실제 Interface 주석으로 강화하지 못한다.

권장:

- public API에는 XML summary뿐 아니라 authority, 호출 가능 시점, 실패 시 동작을 짧게 적는다.

### Low: `Tests/TestScript`는 프로젝트 컨벤션 밖에 있다

`Tests/TestScript/FallingTile.cs`와 `BridgeButton.cs`는 namespace가 없고, public field, 태그 문자열, 약어 필드명, 단문 return 스타일을 사용한다.

- `FallingPlatform` public field: `Caretaker/Assets/_Project/Tests/TestScript/FallingTile.cs:7`
- 태그 문자열: `Caretaker/Assets/_Project/Tests/TestScript/FallingTile.cs:36`
- `BridgeButton` 태그 문자열: `Caretaker/Assets/_Project/Tests/TestScript/BridgeButton.cs:17`

영향:

- 테스트용 실험 코드라면 문제는 작다.
- 하지만 Prefab/Scene에서 참조되기 시작하면 컨벤션 위반이 프로덕션으로 넘어온다.

권장:

- 실험용이면 `Tests/TestScript` 대신 archive/test fixture 성격을 명확히 한다.
- 실제 게임 오브젝트라면 `Scripts/World` 또는 `Scripts/Gameplay`로 옮기고 컨벤션에 맞춘다.

## 체크리스트 평가

| 항목 | 평가 | 근거 |
| --- | --- | --- |
| 형식 일관성 | 보통 | 대부분 namespace/폴더는 맞지만 `PlayerMotor2D`, using 순서, 템플릿 주석이 흔들린다. |
| 코딩 컨벤션 | 보통 이하 | `[SerializeField] private`는 대체로 지켜지지만 public mutable state, `FindAnyObjectByType`, 일부 public API 문서 누락이 있다. |
| 주석 처리 | 보통 | XML 주석은 많은 편이나 빈 템플릿 주석과 설명 과잉이 섞여 있다. |
| 가독성 | 보통 | 작은 서비스는 읽기 좋고, 큰 MonoBehaviour는 책임이 많아 추적 비용이 크다. |
| 코드 품질 | 보통 | 컴파일은 통과하나 스텁과 런타임 구현이 섞여 있다. |
| 단일 책임 원칙 | 보통 이하 | `MicrophoneVoiceTransport`, `InteractionService`, 일부 Core debug/runtime Module에서 책임이 겹친다. |
| 테스트성 | 보통 이하 | 일부 Domain Service 테스트는 좋지만 핵심 Prototype 계약 테스트가 비어 있다. |

## 아키텍처 Deepening 후보

아래 후보는 구체 Interface 설계가 아니라, 다음에 더 깊게 탐색할 수 있는 개선 방향이다.

### 1. Runtime State Interface 깊게 만들기

Files:

- `InventoryState`, `RoomVisitState`, `CausalityState`, `GameSessionState`, `PlayerRuntimeState`
- `InventoryService`, `RoomManager`, 향후 `CausalityService`, `CheckpointService`

Problem:

- State Module의 Interface가 public mutable field라서 Implementation 세부가 그대로 노출된다.
- 삭제 테스트를 하면 state class를 없앤 복잡도가 여러 caller로 퍼질 가능성이 있지만, 현재는 불변식을 보호하는 Leverage가 약하다.

Solution:

- Runtime State를 직렬화 payload로만 쓸지, gameplay Interface로도 쓸지 역할을 분리한다.
- gameplay caller가 직접 컬렉션을 수정하지 못하게 하고, 변경은 해당 Domain Module을 통해서만 일어나게 한다.

Benefits:

- Locality: slot count, duplicate item, visited room, completed major 같은 규칙이 한 곳에 모인다.
- Leverage: caller는 "상태를 어떻게 바꾸는지" 대신 "무슨 명령을 요청하는지"만 알면 된다.
- Tests: 내부 필드 검증보다 public Interface의 불변식 테스트로 바뀐다.

### 2. Radio voice Module 깊게 만들기

Files:

- `MicrophoneVoiceTransport.cs`
- `ConcentusOpusVoiceCodec.cs`
- `MuLawVoiceCodec.cs`
- `RadioNetworkBridge.cs`
- `RadioService.cs`

Problem:

- 송신권 판정 Module은 깊지만, 음성 전송 Module은 캡처/인코딩/네트워크/재생/debug가 한 Implementation에 있다.
- `OnAudioFilterRead`와 NGO named message handler가 같은 Module에 있어 테스트와 디버깅 Locality가 낮다.

Solution:

- 음성 캡처, voice frame encode/decode, network relay, playback queue, debug Adapter를 분리한다.
- `RadioNetworkBridge`는 송신권과 전송 상태의 Seam을 유지하고, 실제 전송 Adapter는 교체 가능하게 둔다.

Benefits:

- Locality: 오디오 underrun과 network drop 원인을 분리해서 볼 수 있다.
- Leverage: Vivox 같은 외부 voice transport로 바꿀 때 gameplay 쪽 Interface를 덜 흔든다.
- Tests: codec, queue, relay 정책을 Unity audio thread 없이 검증할 수 있다.

### 3. Interaction Module 깊게 만들기

Files:

- `InteractionProbe.cs`
- `InteractionService.cs`
- `PlayerController.cs`
- `InventoryController.cs`
- `InteractableObject.cs`
- `CausalTrigger.cs`, `CausalReceiver.cs`

Problem:

- 후보 탐지, 판정, 실행 효과가 여러 Module에 나뉘어 있지만 Interface가 명확하지 않다.
- `InteractionService`는 Domain Service라고 쓰여 있지만 Unity 객체를 받는다.

Solution:

- Physics/Input Adapter는 후보만 만들고, 판정 Module은 Unity 객체 지식 없이 요청을 해석한다.
- 실행 Adapter는 Inventory, Time Causality, UI 피드백으로 라우팅한다.

Benefits:

- Locality: 아이템 조건, 거리 조건, 상호작용 우선순위가 한 Interface 뒤에 모인다.
- Leverage: 새 상호작용 타입을 추가할 때 Player/Input/World 전반을 덜 수정한다.
- Tests: 후보 선택, 아이템 조건, 인과 트리거 연결을 각각 독립적으로 검증할 수 있다.

### 4. GameFlow를 Stage 진행의 깊은 Module로 만들기

Files:

- `GameFlowManager.cs`
- `PhaseService.cs`
- `CheckpointService.cs`
- `SceneLoader.cs`
- `SessionRoleManager.cs`

Problem:

- 현재 GameFlow는 Major 완료와 Checkpoint 복원을 실제 Stage 진행으로 닫지 못한다.
- Phase 전환은 debug ready path와 SceneLoader 호출에 가까워, Stage Contract의 권위 Module로서 Depth가 부족하다.

Solution:

- Major Interaction 완료, Phase 완료 조건, Shared Failure, Checkpoint Spawn을 `GameFlowManager`로 수렴시킨다.
- Scene loading은 GameFlow의 결과를 표현하는 Adapter로 둔다.

Benefits:

- Locality: Prototype 진행 규칙이 한 Module에서 추적된다.
- Leverage: Causality, AI, Inventory가 모두 같은 진행 Interface를 호출하게 된다.
- Tests: Major 완료 후 Phase 전환, 실패 후 스냅샷 복원을 테스트할 수 있다.

## 우선순위 권장 작업

1. `NotImplementedException` public API를 정리한다.
   - 사용 예정이면 최소 동작 구현.
   - 아직 미사용이면 scene/prefab 연결 금지와 TODO 이슈 명시.

2. `GameFlowManager`와 `CheckpointService`부터 닫는다.
   - Major 완료, Phase 전환, Rollback 흐름이 Prototype의 중심축이다.

3. Runtime State 노출 정책을 정한다.
   - public field를 예외로 둘지, private/properties로 맞출지 결정한다.

4. `FindAnyObjectByType` 사용을 debug/runtime으로 분리한다.
   - debug Module은 허용하되, runtime Module은 명시 참조 또는 bootstrap 주입으로 바꾼다.

5. Unity EditMode 테스트 실행 경로를 복구한다.
   - 현재는 프로젝트가 열려 있어 batchmode 테스트가 막혔다.
   - PR 검증 기준으로 Unity batchmode 명령을 고정하는 것이 좋다.

## 최종 판단

Caretaker의 코드 방향성은 DSD의 계층 구분과 도메인 언어를 꽤 잘 따라가고 있다. `RoomService`, `InventoryService`, `RadioService`처럼 작은 Domain Module은 읽기 쉽고 테스트도 있다.

다만 현재 품질의 병목은 구현 스타일보다 통합 완성도다. 스텁 Module, mutable state, 전역 탐색, 큰 MonoBehaviour가 누적되기 전에 Core 진행 흐름과 Runtime State 정책을 먼저 닫는 편이 이후 Prototype 구현 비용을 줄인다.
