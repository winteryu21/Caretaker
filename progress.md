# Development Progress

> Caretaker 네트워크 시스템 작업 진행 기록.
> 현재 범위: Network System Phase 0

---

## 1. 현재 상태

`phase.md` 기준 **Phase 0. 네트워크 기반 세팅**은 기본 동작 테스트를 통과한 상태다.

목표:

- 2인 Host / Client 온라인 세션 생성 및 참가
- 접속한 두 플레이어에게 Past / Future 역할 배정
- 로비에서 양쪽 Ready 후 게임 씬 로드
- Host 권위 기반 세션 상태 관리

현재 테스트 결과:

- Host 시작 정상
- Client 참가 정상
- 2인 접속 제한 기반 세션 흐름 정상
- Past / Future 역할 배정 정상
- 로비 역할 선택 및 역할 swap 정상
- 양쪽 Ready 후 `Game` 씬 전환 정상

---

## 2. 현재 코드 위치

develop 병합 후 실제 Unity 프로젝트 구조는 `Caretaker/Assets/_Project/...` 기준이다.

네트워크 핵심 코드:

```text
Caretaker/Assets/_Project/Scripts/Core/
├── NetworkSessionController.cs
├── SessionRoleManager.cs
├── NetworkSessionStatus.cs
├── PlayerSessionData.cs
└── NetworkDebugLauncher.cs
```

공유 역할 enum:

```text
Caretaker/Assets/_Project/Scripts/Shared/
└── TimelineRole.cs
```

테스트 씬:

```text
Caretaker/Assets/_Project/Scenes/
├── NetworkLobby.unity
└── Game.unity
```

---

## 3. 구현 내용

### NetworkSessionController

- Host / Client 세션 시작
- 세션 종료
- Connection Approval 설정
- 2인 접속 제한
- 세션 상태 관리
- Client 시작 시에도 Host와 동일한 approval / timeout 설정 적용
- 양쪽 Ready 완료 시 Host 권위로 `Game` 씬 로드
- 게임 시작 전 로컬 Past / Future 역할 선택 요청 전달

세션 상태:

```text
Offline
WaitingForPlayers
BothConnected
BothReady
GameStarting
InGame
ShuttingDown
```

### SessionRoleManager

- 접속 Client 등록 / 해제
- Past / Future 역할 배정
- 로비 역할 선택 및 역할 swap 처리
- 역할 변경 시 Ready 상태 해제
- Ready 상태 제출 및 Host 판정
- 세션 상태 Client 브로드캐스트
- 게임 씬 로드 이후에도 역할 맵 유지를 위해 spawn 후 `DontDestroyOnLoad` 적용

### NetworkDebugLauncher

- Host / Client / Ready / Shutdown 테스트 버튼 진입점
- Past / Future 역할 선택 버튼 진입점
- 화면 좌상단 `OnGUI` 디버그 패널 표시
- 콘솔 로그로 세션 상태 출력

버튼 연결:

```text
Host 버튼     -> NetworkDebugLauncher.StartHost()
Client 버튼   -> NetworkDebugLauncher.StartClient()
Past 버튼     -> NetworkDebugLauncher.SelectPast()
Future 버튼   -> NetworkDebugLauncher.SelectFuture()
Ready 버튼    -> NetworkDebugLauncher.Ready()
Start 버튼    -> NetworkDebugLauncher.StartGame()
Shutdown 버튼 -> NetworkDebugLauncher.Shutdown()
```

---

## 4. 테스트 기준

Host만 시작했을 때:

```text
Mode: Host
Connected Clients: 1/2
Session: WaitingForPlayers
Local Timeline Role: Past
```

Client가 접속했을 때:

```text
Connected Clients: 2/2
Session: BothConnected
Host: Past
Client: Future
```

역할 선택:

```text
Past 버튼     -> 로컬 플레이어가 Past 요청
Future 버튼   -> 로컬 플레이어가 Future 요청
상대가 이미 가진 역할을 선택하면 Past / Future swap
역할 변경 시 Ready 상태 해제
```

양쪽 Ready 후:

```text
Session: BothReady
Scene: Game
```

---

## 5. 발생했던 문제와 조치

### NetworkManager와 NetworkBehaviour 동시 사용 오류

원인:

- `NetworkManager` 오브젝트 또는 자식에 `SessionRoleManager` / `NetworkObject`를 붙였기 때문.

조치:

- `NetworkManager` 오브젝트에는 `NetworkManager`, `UnityTransport`, `NetworkSessionController`만 둔다.
- `SessionRoleManager`는 루트의 별도 오브젝트로 분리한다.

### NetworkConfig mismatch

원인:

- Host와 Client의 connection approval 설정이 서로 달랐다.

조치:

- `StartClientSession()`에서도 Host와 동일하게 connection approval / timeout 설정을 적용한다.

### RequireOwnership obsolete 경고

조치:

- `ServerRpc`를 NGO 2.x 권장 `Rpc` API로 변경했다.

### develop 병합 후 구조 변경

조치:

- 기존 `Assets/Script/...` 기준 네트워크 코드를 `Assets/_Project/Scripts/Core/...`로 이동했다.
- `TimelineRole`은 `Caretaker.Shared` 기준으로 통합했다.
- `NetworkLobby.unity`, `Game.unity`를 `Assets/_Project/Scenes/...` 기준으로 정리했다.

---

## 6. 검증 상태

확인된 것:

- Netcode for GameObjects `2.11.2` 설치 확인
- Host / Client 로컬 접속 확인
- Past / Future 역할 배정 확인
- 역할 선택 / swap 확인
- 양쪽 Ready 후 `Game` 씬 전환 확인
- Host 접속 해제 시 Client를 `Offline`으로 정리하는 방어 로직 추가
- develop 최신 변경사항 병합 완료

제한 사항:

- `dotnet build Caretaker.sln`은 Unity 생성 `.sln` 내부 `Unity.Timeline` 프로젝트명 중복 때문에 실패한다.
- `dotnet build Assembly-CSharp.csproj`는 Unity 패키지 / generated csproj 상태에 의존해 안정적인 검증 수단으로 보기 어렵다.
- 최종 검증은 Unity Editor 컴파일 및 Multiplayer Play Mode 기준으로 보는 것이 맞다.

---

## 7. 다음 작업

Phase 0 잔여 확인:

- 접속 인원 2명 초과 시 Connection Approval 거부 확인
- Host 종료 / Client 종료 시 상태 전환 추가 확인
- 현재 디버그 UI 기반 버튼을 실제 로비 UI로 교체

Phase 1 준비:

- `NetworkPlayerSpawner`
- `NetworkPlayerState`
- Past / Future spawn point
- Owner-only 입력
- Phase 1~2 상대 캐릭터 비노출 정책
