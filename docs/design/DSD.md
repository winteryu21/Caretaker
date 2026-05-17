# Caretaker DSD — Part 1: 요약문 / 서론 / 시스템 설계

> 본 파일은 DSD를 팀원별로 분할한 파트입니다. 최종적으로 `DSD.md`에 병합됩니다.

---

# Caretaker — Design Specification Document

| 버전 | 내용 | 작업자 |
| :---: | :--- | :---: |
| 260507 | DSD 초안 구조 정리 | 유민서 |
| 260510 | DSD 작성 | 전원 |
| 260514 | DSD 작성 | 전원 |

> 본 문서는 6월 프로토타입 기준의 소프트웨어 설계 명세서다. 세부 시나리오와 레벨 디자인은 시스템 설계에 필요한 예시 수준으로만 다루며, 핵심 기능의 구조, 모듈, 인터페이스, 데이터, 동기화 흐름을 명세한다.

---

## 개요 / 바로가기

| 구분 | 바로가기 | 목적 |
| :--- | :--- | :--- |
| 문서 요약 | [요약문](#요약문) | 목적, 기술 스택, 제품 요약 확인 |
| 기본 전제 | [1. 서론](#1-서론) | 범위, 용어, 참조 문서, 주요 사양 확인 |
| 전체 구조 | [2. 시스템 설계](#2-시스템-설계) | 전체 아키텍처, 시스템 의존성, 상태 흐름, 설계 원칙 확인 |
| 세부 시스템 | [3. 세부 설계](#3-세부-설계) | 인과, 네트워크, 플레이어, 룸, AI, 무전기, 인벤토리, 스플릿뷰, 체크포인트, UI 설계 확인 |
| 데이터 / 메시지 | [4. 데이터 설계](#4-데이터-설계) | ScriptableObject, Runtime State, 네트워크 메시지 계약, ID 규칙 확인 |
| 요구사항 검증 | [5. 요구사항 추적 매트릭스](#5-요구사항-추적-매트릭스) | DRD 요구사항 매핑, 리스크, 핵심 설계 계약 검증 확인 |
| 참고 자료 | [6. 참고 자료](#6-참고-자료) | 문서 작성에 사용한 외부 기준 확인 |

**핵심 설계 바로가기**

| 항목 | 바로가기 |
| :--- | :--- |
| 시간 인과 시스템 | [3.1 시간 인과 시스템](#31-시간-인과-시스템) |
| 네트워크 공개 범위 | [3.2 네트워크 동기화 시스템](#32-네트워크-동기화-시스템) |
| Phase 3 스플릿뷰 | [3.8 Phase 3 스플릿뷰 시스템](#38-phase-3-스플릿뷰-시스템) |
| 네트워크 메시지 계약 | [4.4 네트워크 메시지](#44-네트워크-메시지) |
| 핵심 설계 계약 검증 | [5.4 핵심 설계 계약 검증](#54-핵심-설계-계약-검증) |

---

## 요약문

**목적**

본 문서는 Caretaker 프로토타입의 핵심 기능을 구현하기 위한 소프트웨어 설계를 정의한다. DRD에서 제시한 요구사항을 시스템 단위로 분해하고, 각 시스템의 책임, 주요 컴포넌트, 인터페이스, 데이터 구조, 네트워크 동기화 방식을 명세하여 개발 기준으로 사용한다.

**관련 기술**

| 기술 | 상세 |
| :--- | :--- |
| Unity 6.4 | 2D 게임 엔진 및 URP 2D 렌더링 |
| C# | 게임 로직 구현 언어 |
| Netcode for GameObjects | 2인 Host-Client 네트워크 동기화 |
| Unity Input System | 키보드/마우스 입력 처리 |
| ScriptableObject | 인과 규칙, 적 파라미터, 아이템 등 데이터 정의 |

**제품 요약**

Caretaker는 과거와 미래에 분리된 두 플레이어가 제한된 정보와 인게임 무전기 통신을 통해 협력하는 2인 비대칭 협동 퍼즐 어드벤처 게임이다. 프로토타입은 하나의 연구소 스테이지와 3개 페이즈로 구성되며, 과거 플레이어의 행동이 미래 플레이어의 환경을 즉시 변화시키는 시간 인과 시스템을 핵심으로 한다.

---

## 1. 서론

### 1.1 개요

프로토타입은 연구소 스테이지 1개를 대상으로 한다. 두 플레이어는 각각 과거와 미래의 동일한 병렬 시설에 배치되며, Phase 1~2에서는 상대 화면을 볼 수 없다. 플레이어는 인게임 half-duplex 무전기를 통해 정보를 교환하고, 과거에서 발생한 물리적 인과가 미래 월드 상태를 즉시 변경한다. Phase 3에서는 예외적으로 상하 스플릿뷰를 사용해 서로 다른 시간대의 탈출 경로를 동시에 보여준다.

### 1.2 용어 정의

| 용어 | 정의 |
| :--- | :--- |
| 프로토타입 | 6월 둘째 주까지 제작할 연구소 1스테이지 기준 플레이 가능 버전 |
| 스테이지 | 3개 페이즈로 구성된 하나의 완전한 연구소 플레이 공간 |
| 페이즈 | 스테이지 내부의 큰 진행 구간. Phase 1, Phase 2, Phase 3로 구분 |
| 시간대 역할 | 플레이어의 게임 역할. Past 또는 Future |
| 네트워크 역할 | 세션 권한 역할. Host 또는 Client |
| 정보 격리 | 플레이어가 상대의 상태, 위치, 화면, 단서, 진행 정보를 시스템 화면이나 UI로 직접 알 수 없고, 인게임 무전기 대화로만 공유받는 플레이 경험 규칙 |
| 복제 필터링 | 정보 격리를 보장하기 위해 Phase 1~2에서 상대 위치, 룸, 방문 기록, 아바타 상태, 단서 상태를 비소유 Client에 전송하지 않는 네트워크 정책 |
| 표시 필터링 | 수신한 상태 중 로컬 플레이어에게 허용된 정보만 HUD, 미니맵, 프롬프트에 노출하는 UI 정책 |
| 물리적 인과 | 과거 행동이 미래 월드의 물리 상태를 바꾸는 단방향 인과 |
| Major Interaction | 두 플레이어가 같은 단기 목표를 위해 반드시 상호 의존해야 하는 주요 협동 상호작용 |
| Minor Interaction | 짧지만 결과를 명확히 인지할 수 있는 보조 인과 상호작용 |
| 룸 | 방, 복도, 계단 등 플레이와 카메라 전환의 기본 공간 단위 |
| 경보 상태 | 발각 후 현재 방과 인접 방의 적이 강화 감시 상태로 전환되는 상태 |
| 공동 실패 | 한 플레이어의 실패가 양쪽 플레이어의 체크포인트 복귀로 이어지는 규칙 |

### 1.3 참조 문서

| 문서 | 경로 |
| :--- | :--- |
| DRD | [`../DRD.md`](../DRD.md) |
| 레벨 디자인 가이드 | [`../meetings/260504-level-design-guide.md`](../meetings/260504-level-design-guide.md) |

### 1.4 설계 제한사항

| 제한 분류 | 설계 반영 |
| :--- | :--- |
| 2인 전용 | 모든 네트워크/역할/동기화 구조는 2인 Host-Client를 기준으로 한다. |
| PC 전용 | 키보드/마우스 입력을 우선 지원한다. |
| 2D 사이드뷰 | 2D Rigidbody/Collider, URP 2D 카메라, 2D Raycast를 기준으로 한다. |
| 정보 격리 | Phase 1~2에서 상대 정보는 무전기 대화로만 알 수 있어야 하며, 복제 필터링과 표시 필터링으로 이를 보장한다. |
| 프로토타입 우선 | 가능한 핵심 시스템을 먼저 명세하고, 구현 리스크는 별도 검토 대상으로 둔다. |

### 1.5 주요 사양

| 항목 | 사양 |
| :--- | :--- |
| 플레이 인원 | 2명 |
| 네트워크 구조 | Host-Client |
| 시간대 역할 | Past / Future 고정 |
| 물리적 인과 방향 | Past → Future |
| 인과 반영 | 즉시 반영 |
| 통신 방식 | 인게임 Push-to-Talk, half-duplex |
| 캐릭터 이동 속도 | 5 units/sec |
| 점프 높이 | 2 units |
| 상호작용 반경 | 1 unit |
| 기본 AI 감지 거리 / FOV | 10 units / 45도 |
| 목표 프레임레이트 | 60 FPS |
| 화면 비율 | 16:9 |

---

## 2. 시스템 설계

### 2.1 전체 아키텍처

```mermaid
block-beta
  columns 3

  block:Core["Core"]:3
    GameFlow["GameFlowManager\n(Phase / Checkpoint)"]
    NetworkSync["NetworkSyncManager\n(RPC / State Sync)"]
    RoleSession["SessionRoleManager\n(NetworkRole / TimelineRole)"]
  end

  block:World["World"]:3
    Room["RoomManager\n(Room Transition)"]
    Causality["CausalityManager\n(Past -> Future)"]
    Interaction["InteractionSystem\n(Object Interaction)"]
  end

  block:Gameplay["Gameplay"]:3
    Player["PlayerController\n(Move / Interact)"]
    Inventory["InventorySystem\n(Key Items)"]
    Enemy["EnemySystem\n(Base / Alert / Chase)"]
  end

  block:Communication["Communication"]:3
    Radio["RadioSystem\n(PTT / Lock)"]
    Voice["VoiceTransport\n(Audio Stream)"]
    UI["UIManager\n(HUD / Prompt)"]
  end

  block:Presentation["Presentation"]:3
    Camera["CameraDirector\n(Room Camera)"]
    Minimap["MinimapSystem\n(Personal Map)"]
    Split["SplitViewManager\n(Phase 3)"]
  end
```

### 2.2 시스템 간 의존성

| 발신 시스템 | 수신 시스템 | 데이터 / 이벤트 | 목적 |
| :--- | :--- | :--- | :--- |
| PlayerController | InteractionSystem | `InteractionRequest` | 오브젝트 조사, 사용, 아이템 적용 요청 |
| InteractionSystem | CausalityManager | `CausalTriggerRequest` | 과거 트리거 활성화 요청 |
| CausalityManager | NetworkSyncManager | `CausalStateChanged` | 미래 월드 상태 변경 동기화 |
| CausalityManager | RoomManager | `ApplyReceiverState` | 미래 리시버 상태 반영 |
| EnemySystem | GameFlowManager | `PlayerCaught` | 공동 실패 및 체크포인트 복귀 |
| EnemySystem | RoomManager | `AlertRoomsRequested` | 현재 방 및 인접 방 경계 상태 전파 |
| RadioSystem | NetworkSyncManager | `RadioLockStateChanged` | 송신권 상태 동기화 |
| VoiceTransport | RadioSystem | `VoiceFrame` | 송신권 보유 중 음성 전송 |
| GameFlowManager | SplitViewManager | `Phase3Started` | Phase 3 스플릿뷰 활성화 |
| RoomManager | MinimapSystem | `RoomVisited` | 개인 방문 기반 미니맵 갱신 |

### 2.3 씬 및 상태 흐름

```mermaid
stateDiagram-v2
    [*] --> MainMenu
    MainMenu --> Lobby : Start
    Lobby --> Game : BothReady

    state Game {
        [*] --> Phase1
        Phase1 --> Phase2 : Phase1Complete
        Phase2 --> Phase3 : Phase2Complete
        Phase3 --> [*] : EscapeSuccess / EscapeFail
    }

    Game --> Result
    Result --> MainMenu
```

### 2.4 설계 원칙

| 원칙 | 설명 |
| :--- | :--- |
| Host Authority | 게임 진행, 인과, 실패, 경보 판정은 Host가 권위 있게 처리한다. |
| Event-Based Sync | 모든 상태를 매 프레임 동기화하지 않고, 상호작용/인과/경보/체크포인트 이벤트 단위로 전파한다. |
| Data-Driven Rules | 인과 규칙, 적 파라미터, 방 연결, 체크포인트는 데이터로 정의한다. |
| Role Separation | Host/Client와 Past/Future를 분리하여 네트워크 권한과 게임 역할을 혼동하지 않는다. |
| Room-Scoped Pressure | Phase 1~2의 직접 추적은 방 단위로 제한하고, 경보는 인접 방까지 전파한다. |

---

### 3.5 AI / 경보 시스템

**책임**

- 적의 기본/경계/추적 상태를 관리한다.
- 플레이어 감지, 은신 회피, 추적 해제 지연을 판정한다.
- 발각 시 현재 방과 인접 방에 경보를 전파하고, 포획 시 공동 실패를 발생시킨다.

**상태 전이**

```mermaid
stateDiagram-v2
    [*] --> Patrol
    Patrol --> Alert : room alert received
    Patrol --> Chase : player detected
    Alert --> Chase : player detected
    Chase --> Search : lost sight
    Search --> Patrol : 10s elapsed and no alert
    Search --> Chase : reacquired target
    Chase --> [*] : player caught
```

**구성 요소**

| 요소 | 계층 | 설명 |
| :--- | :--- | :--- |
| `EnemyController` | Unity Component | Nav/이동/애니메이션 연결 |
| `EnemyPerception2D` | Unity Component | 10 unit, 45도 시야와 장애물 Raycast |
| `EnemyStateMachine` | Domain Service | Patrol/Alert/Chase/Search 전이 판정 |
| `AlertService` | Domain Service | 현재 방과 인접 방 경보 대상 계산 |
| `EnemyTuningSO` | Data Asset | 감지 거리, FOV, 추적 속도, 해제 지연 |
| `AlertState` | Runtime State | 방별 경보 상태와 만료 시간 |

**인터페이스**

| Name | Input | Process | Output | Authority |
| :--- | :--- | :--- | :--- | :--- |
| `EvaluateSight` | enemy pose, player pose, crouch/hidden state | FOV, 거리, 차폐 검사 | detected / not detected | Host |
| `RaiseAlert` | source room id | 현재 방과 인접 방 계산 | `AlertRoomsChanged` | Host |
| `TickEnemyState` | enemy state, perception, time | 상태 전이와 목표 선택 | new enemy state | Host |
| `ReportPlayerCaught` | enemy id, player id | 공동 실패 요청 | checkpoint rollback | Host |

**경보 전파 흐름**

```mermaid
sequenceDiagram
    participant E as EnemyPerception2D
    participant AI as EnemyStateMachine
    participant Alert as AlertService
    participant Room as RoomService
    participant Net as NetworkSyncManager
    participant Flow as GameFlowManager

    E->>AI: PlayerDetected(playerId, roomId)
    AI->>Alert: RaiseAlert(sourceRoomId)
    Alert->>Room: GetAdjacentRooms(sourceRoomId)
    Room-->>Alert: current + adjacent rooms
    Alert->>Net: AlertRoomsClientRpc(alertRoomIds)
    AI->>Flow: ReportPlayerCaught when capture confirmed
```

**설계 메모**

- 룸 전환을 넘어 직접 추적하지 않는 규칙은 `EnemyStateMachine`이 아닌 `RoomService`와의 계약으로 둔다.
- AI 이동 구현은 프로토타입에서 단순 waypoint와 line-of-sight를 우선하고, NavMesh 계열 도입은 TBD로 남긴다.

### 3.6 무전기 통신 시스템

**책임**

- Push-to-Talk 입력과 half-duplex 송신권을 관리한다.
- 한 명만 송신할 수 있도록 Host가 송신권을 부여/회수한다.
- 음성 스트림과 통신 상태 UI를 분리한다.

**구성 요소**

| 요소 | 계층 | 설명 |
| :--- | :--- | :--- |
| `RadioInputController` | Unity Component | PTT 키 입력 감지 |
| `RadioService` | Domain Service | 송신권 요청, 충돌, timeout 규칙 |
| `RadioNetworkBridge` | Network Boundary | 송신권 RPC, 상태 동기화 |
| `VoiceTransport` | Infrastructure | 실제 음성 프레임 송수신 |
| `RadioHudPresenter` | Unity Component | 송신/수신/점유 상태 표시 |

**동작 흐름**

```mermaid
sequenceDiagram
    participant A as Player A
    participant H as Host RadioService
    participant B as Player B
    participant V as VoiceTransport

    A->>H: RequestTalkServerRpc(playerA)
    H-->>A: TalkGrantedClientRpc
    H-->>B: RadioLockedClientRpc(playerA)
    A->>V: Send voice frames while key held
    A->>H: ReleaseTalkServerRpc
    H-->>A: TalkReleasedClientRpc
    H-->>B: RadioIdleClientRpc
```

**인터페이스**

| Name | Input | Process | Output | Authority |
| :--- | :--- | :--- | :--- | :--- |
| `RequestTalk` | player id | 송신권 공석 여부 확인 | granted / denied | Host |
| `ReleaseTalk` | player id | 현재 소유자 확인 후 해제 | idle state | Host |
| `TransmitVoiceFrame` | encoded audio frame | 송신권 보유자만 전달 | remote audio playback | Current talker |
| `UpdateRadioHud` | radio state | 아이콘/게이지/점유자 표시 | UI state | Client local |

### 3.7 인벤토리 시스템

**책임**

- 플레이어별 개인 가방과 키 아이템 상태를 관리한다.
- 직접 아이템 공유를 금지하고, 아이템 사용은 대상 오브젝트와의 상호작용으로만 발생한다.
- 체크포인트 복원을 위해 소지/소모 상태를 직렬화한다.

**구성 요소**

| 요소 | 계층 | 설명 |
| :--- | :--- | :--- |
| `InventoryService` | Domain Service | 획득, 선택, 사용, 소모 규칙 |
| `InventoryController` | Unity Component | 아이템 줍기 콜백과 UI 연결 |
| `ItemDefinitionSO` | Data Asset | 아이템 ID, 표시명, 사용 가능 태그 |
| `InventoryState` | Runtime State | 플레이어별 아이템 목록, 선택 아이템 |

**인터페이스**

| Name | Input | Process | Output | Authority |
| :--- | :--- | :--- | :--- | :--- |
| `AcquireItem` | player id, item id | 중복/소지 제한 확인 | inventory changed | Host |
| `SelectItem` | player id, item id | 소지 여부 확인 | selected item changed | Owner Client |
| `UseItemOnTarget` | player id, item id, target id | 대상 태그와 필요 조건 검증 | interaction request | Host |
| `RestoreInventory` | checkpoint inventory state | 소지품 복원 | inventory changed | Host |