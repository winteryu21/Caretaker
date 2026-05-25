# Sprint 1 — Linear 이슈 일괄 등록용

> 이 문서는 Codex Linear MCP 에이전트가 이슈를 일괄 등록할 때 참조하는 구조화된 데이터다.
> 각 이슈는 아래 필드를 포함한다.

## 등록 규칙

- **Team:** Caretaker
- **Project:** Sprint 1
- **Milestone:** `W1` (5/22~28), `W2` (5/29~6/4), `W3` (6/5~10)
- **Priority 매핑:**
  - 🔴 Urgent → `Urgent`
  - 🟠 High → `High`
  - 🟡 Medium → `Medium`
- **Label:**
  - 팀: `Team A`, `Team B`, `Design`
  - Epic: `E0-Design`, `E1-Player`, `E2-Causality`, `E3-Room`, `E4-AI`, `E5-Level`, `E6-Network`, `E7-Flow`
- **담당자 매핑:** 보류


## 참조 문서

| 문서 | 경로 |
|------|------|
| 게임 설계서 | `docs/design/game-design.md` |
| 설계 명세 (DSD) | `docs/design/DSD.md` |
| 스프린트 계획 | `docs/sprints/sprint1-plan.md` |
| 아키텍처 | `docs/technical/architecture.md` |

---

## E0: Design & Spec

### game-flow 기반 퍼즐 상세 설계 (M1~M4)
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Design, E0-Design
- **Description:**
- 목표: game-design.md의 11개 Step에 대해, 각 Major Interaction의 퍼즐 내부 메커니즘을 확정한다
- 배경: game-design.md에 "퍼즐 (별도 설계)"로 표시된 M1~M4의 구체적 단계가 아직 미정. 코드 구현 전 반드시 확정 필요
- 상세 작업 내용:
  - M1 전력 복구: 배선 정보 교환 방식, 스위치 조합 UI, 실패 시나리오
  - M2 네트워크 침입: 취약점 코드 전달 방식, 터미널 UI
  - M3 청사진 식별: 조합 실험 UI, 3개 후보 매뉴얼-실험 대조 메커니즘
  - M4 바이러스/실린더: 미래 터미널 고장 → 힌트 전달 → 과거 퍼즐 입력 UI, 실린더 식별
- 참고자료: `game-design.md` §3~§6, `game-design.md` Step 4, 7, 9, 10
- 수용 기준: game-design.md의 상세 단계 테이블이 빈칸 없이 완성됨

### Phase 3 C동 탈출 시퀀스 상세 설계
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Design, E0-Design
- **Description:**
- 목표: C동(폐쇄된 저장고) 3구간의 장애물, 인과 협동 요소, 정보 흐름을 확정한다
- 배경: game-design.md §7에 구간별 장애물이 초안 수준. 실제 레벨 제작 전 확정 필요
- 상세 작업 내용:
  - 3구간 각각의 장애물 종류, 개인/인과/역인과 분류
  - 과거 프로토타입 로봇 / 미래 진보한 로봇의 추격 파라미터
  - 최종 봉쇄문 동시 조작 판정 방식
  - 타임아웃 존재 여부 및 값
- 참고자료: `game-design.md` §7, `game-design.md` Step 11
- 수용 기준: 구간별 장애물 테이블 완성, 판정 조건 명확, fallback 범위 확정

### CausalRule 전체 목록 최종 확정
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Design, E0-Design
- **Description:**
- 목표: P1~P3 전체 CausalRuleSO의 triggerId, receiverId, 조건, 효과를 최종 확정한다
- 배경: game-design.md §1에 P1 4개, P2 5개, P3 4개 = 13개 규칙이 정의됨. 일부는 구체적 조건이 TBD
- 상세 작업 내용:
  - P1: CR_P1_POWER_LEVER, CR_P1_POWER_PANEL, CR_P1_VENT_OPEN, CR_P1_SEC_HACK
  - P2: CR_P2_SAFE_OPEN, CR_P2_ARCHIVE_ENTER, CR_P2_BLUEPRINT_ID, CR_P2_VIRUS_PLANT, CR_P2_SHIELD_DOWN
  - P3: CR_P3_BRIDGE_DROP, CR_P3_DOOR_UNLOCK, CR_P3_PLATFORM_LOWER, CR_P3_TOOL_PLACE
  - 각 규칙의 조건 필드에서 "TBD"를 제거
- 참고자료: `game-design.md` §1
- 수용 기준: 13개 규칙 전부 조건·효과·필요 아이템 확정, TBD 0건

---

## E1: Player & Interaction

### 프로젝트 폴더 구조 정리
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Team A, E1-Player
- **Description:**
- 목표: architecture.md 기준 Unity 프로젝트 폴더 구조를 생성한다
- 배경: 코드 작성 전 폴더 컨벤션 확립이 필요
- 상세 작업 내용:
  - `Assets/_Project/` 래퍼 하위에 전체 구조 배치
  - `Scripts/Core`, `Scripts/World`, `Scripts/Gameplay`, `Scripts/Presentation`, `Scripts/Shared`
  - `Data/`, `Prefabs/`, `Scenes/` 구조
- 참고자료: `docs/technical/architecture.md`
- 수용 기준: 팀원 전원이 동일한 구조에서 작업 시작 가능

### PlayerMotor2D 구현
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Team A, E1-Player
- **Description:**
- 목표: 2D 플레이어 캐릭터의 이동/점프/웅크리기를 구현한다
- 배경: game-flow의 모든 Step에서 사용되는 기본 이동 시스템
- 상세 작업 내용:
  - WASD 이동 5u/s, Space 점프 2u, Shift 웅크리기 (콜라이더 축소, 속도 60%)
  - Rigidbody2D + BoxCollider2D (1×2u)
  - Unity Input System 기반
- 참고자료: `DSD.md` §3.3
- 수용 기준: 테스트 씬에서 이동/점프/웅크리기가 정상 작동, 지면 판정 정확

### InteractableObject 베이스 클래스
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** High
- **Label:** Team A, E1-Player
- **Description:**
- 목표: 모든 상호작용 가능 오브젝트의 베이스 클래스를 구현한다
- 배경: game-design.md §8에 정의된 40개+ 오브젝트를 Team B가 씬에 배치할 수 있어야 함
- 상세 작업 내용:
  - InteractionType enum: Examine(조사), UseItem(아이템 사용), Operate(즉시 조작)
  - 하이라이트 아웃라인 on/off
  - objectId 필드 (OBJ_P1_SIGN 등 interaction-spec §8의 ID와 매핑)
  - Team B가 Inspector에서 설정 가능한 형태
- 참고자료: `DSD.md` §3.4, `game-design.md` §8
- 수용 기준: Team B가 씬에 빈 오브젝트 배치 → Inspector에서 유형/ID 설정 가능

### InteractionProbe + InteractionService
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** High
- **Label:** Team A, E1-Player
- **Description:**
- 목표: 플레이어의 오브젝트 감지 및 상호작용 처리 시스템을 구현한다
- 배경: game-flow의 모든 오브젝트 상호작용(안내판 조사, 레버 조작, 아이템 사용 등)의 입력 처리
- 상세 작업 내용:
  - 마우스 hover Raycast로 대상 감지 (1u 범위)
  - 좌클릭 → 조사, 우클릭 → 아이템 사용, E키 → 즉시 조작
  - InteractableObject와 연동, 범위 밖 시 비활성화
- 참고자료: `DSD.md` §3.4
- 수용 기준: hover 시 하이라이트, 각 입력에 대응하는 상호작용이 정상 발동

### InventorySystem
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team A, E1-Player
- **Description:**
- 목표: 아이템 획득/선택/사용/소모 시스템을 구현한다
- 배경: game-design.md §2에 정의된 아이템(드라이버, 케이블, 배터리, 카드키 등) 관리
- 상세 작업 내용:
  - 최대 5슬롯, 1~5 숫자키 슬롯 선택
  - AcquireItem / UseItem / ConsumeItem API
  - ItemDefinitionSO 스키마: itemId, displayName, icon, consumable
- 참고자료: `DSD.md` §3.7, `game-design.md` §2
- 수용 기준: 아이템 획득 → 인벤토리 표시, 선택 → 오브젝트에 사용 → 소모 아이템 제거

### 아이템-환경 조합 로직
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team A, E1-Player
- **Description:**
- 목표: 인벤토리 아이템을 환경 오브젝트에 사용하는 검증 로직을 구현한다
- 배경: 카드키→자료실 리더, 드라이버→환풍구 등 interaction-spec의 아이템 사용 흐름
- 상세 작업 내용:
  - 선택한 아이템을 InteractableObject에 우클릭 → requiredItemId 매칭 검증
  - 성공 시 CausalTrigger 발동 또는 상태 변경
  - 실패 시 "사용할 수 없습니다" 피드백
- 참고자료: `game-design.md` §3~§6 상세 단계의 UseItemOnTarget
- 수용 기준: 올바른 아이템 사용 시 인과 트리거 발동, 잘못된 아이템 시 거부 피드백

---

## E2: Causality

### CausalityService 도메인 로직
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** Urgent
- **Label:** Team A, E2-Causality
- **Description:**
- 목표: 인과 규칙 검증 및 실행 로직을 순수 C#으로 구현한다
- 배경: DSD §3.1의 시간 인과 시스템. 과거 행동 → 미래 환경 변경의 핵심 엔진
- 상세 작업 내용:
  - CausalRuleSO 목록 순회 → triggerId 매칭 → 조건 확인 → 효과 적용
  - MonoBehaviour 비의존 (순수 C# 클래스)
  - 규칙 완료 상태 추적 (중복 발동 방지)
- 참고자료: `DSD.md` §3.1
- 수용 기준: 단위 테스트에서 규칙 조건 충족/미충족 시 올바른 결과 반환

### CausalityManager + 트리거/리시버 MonoBehaviour
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** Urgent
- **Label:** Team A, E2-Causality
- **Description:**
- 목표: CausalTrigger / CausalReceiver 컴포넌트를 구현하여 씬 오브젝트에 부착 가능하게 한다
- 배경: Team B가 변전실 레버, 차단기 패널 등에 이 컴포넌트를 붙여 인과 연결을 설정
- 상세 작업 내용:
  - CausalTrigger: 활성화 시 CausalityService 호출
  - CausalReceiver: 규칙 실행 결과를 받아 오브젝트 상태 변경 (문 열림, 전력 켜짐 등)
  - 이벤트 기반 전파
- 참고자료: `DSD.md` §3.1
- 수용 기준: 트리거 활성화 → 서비스 호출 → 리시버 상태 변경이 Inspector에서 확인 가능

### CausalRuleSO — Phase 1 규칙 작성
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team A, E2-Causality
- **Description:**
- 목표: Phase 1의 인과 규칙 4개를 ScriptableObject 에셋으로 작성한다
- 배경: M1(전력 복구) + M2(네트워크 침입)에 필요한 인과 데이터
- 상세 작업 내용:
  - CR_P1_POWER_LEVER: 변전실 메인 레버 → 차단기 패널 전력 공급
  - CR_P1_POWER_PANEL: 스위치 조합 → 1층 전력+보안 가동 (Major 완료)
  - CR_P1_VENT_OPEN: 환풍구 나사 해체 → 우회 경로 (ITEM_TOOL_DRIVER 필요)
  - CR_P1_SEC_HACK: 보안 터미널 접속 → 인사 기록 열람 (Major 완료)
- 참고자료: `game-design.md` §1 Phase 1
- 수용 기준: 4개 SO 에셋 생성, Inspector에서 모든 필드 입력 완료

### CausalRuleSO — Phase 2 규칙 작성
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team A, E2-Causality
- **Description:**
- 목표: Phase 2의 인과 규칙 5개를 ScriptableObject 에셋으로 작성한다
- 배경: M3(청사진 식별) + M4(바이러스/실린더)에 필요한 인과 데이터
- 상세 작업 내용:
  - CR_P2_SAFE_OPEN: 소장실 금고 다이얼 → 카드키 획득
  - CR_P2_ARCHIVE_ENTER: 자료실(B동 2F) 카드키 → 진입 (ITEM_KEY_CARD 필요)
  - CR_P2_BLUEPRINT_ID: 청사진 선택 확정 → Major 완료
  - CR_P2_VIRUS_PLANT: 바이러스 설치 → 실린더 개방 (미래 해킹 힌트 필요)
  - CR_P2_SHIELD_DOWN: 산업용 로봇 → 방호 셔터 돌파 (미래 전용)
- 참고자료: `game-design.md` §1 Phase 2
- 수용 기준: 5개 SO 에셋 생성, Inspector에서 모든 필드 입력 완료

---

## E3: Room & Camera

### RoomVolume + RoomManager
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Team A, E3-Room
- **Description:**
- 목표: 룸 진입/이탈 감지 및 현재 룸 상태 관리 시스템을 구현한다
- 배경: game-design.md의 16+ 룸 간 이동을 처리. Team B가 각 룸에 배치
- 상세 작업 내용:
  - Trigger Collider로 진입/이탈 감지
  - 현재 룸 기록, 인접 룸 조회
  - RoomChangedEvent 발행
- 참고자료: `DSD.md` §3.5
- 수용 기준: 룸 간 이동 시 RoomChangedEvent 발행, 현재 룸 ID 정확히 추적

### RoomGraphSO 데이터 작성
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** High
- **Label:** Team A, E3-Room
- **Description:**
- 목표: 전 Phase 룸 그래프를 ScriptableObject 데이터로 작성한다
- 배경: game-design.md 📍 연결 정보를 기반으로 한 룸 간 연결 데이터
- 상세 작업 내용:
  - RoomGraphSO 스키마: roomId, displayName, adjacentRoomIds[], spawnPointIds[]
  - P1: 로비, 변전실 가는 길, 변전실 앞 복도, 변전실, 보안실 앞 복도, 보안실 (7룸)
  - P2: 연구소장실, 2층 복도, 구름다리, 자료실, 실험실, 샘플 저장고 (6룸+)
  - P3: C동 3구간
- 참고자료: `game-design.md` 📍 룸 정보
- 수용 기준: 모든 룸 간 연결이 game-design.md와 일치

### CameraDirector 룸 추적
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Team A, E3-Room
- **Description:**
- 목표: 룸 전환 시 카메라가 해당 룸 영역으로 이동하는 시스템을 구현한다
- 배경: 각 룸이 독립된 공간이므로 카메라가 룸 경계를 벗어나지 않아야 함
- 상세 작업 내용:
  - RoomManager의 RoomChangedEvent 구독
  - Cinemachine Confiner 또는 수동 Bounds 방식
  - 전환 시 부드러운 이동
- 참고자료: `DSD.md` §3.5
- 수용 기준: 룸 전환 시 카메라가 새 룸 범위로 정확히 이동

### 과거/미래 월드 분리 렌더링
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team A, E3-Room
- **Description:**
- 목표: 같은 씬 내에서 과거/미래 환경을 분리하여 각 플레이어에게 자기 시간대만 보여준다
- 배경: game-design.md에서 과거(정돈된 시설)와 미래(폐허)는 같은 룸이지만 다른 비주얼
- 상세 작업 내용:
  - Layer 기반 분리 우선 시도
  - 각 플레이어에게 자기 시간대 레이어만 렌더링
  - 실패 시 Additive Scene 방식 폴백
- 참고자료: `DSD.md` §3.5
- 수용 기준: 과거 플레이어에겐 과거 오브젝트만, 미래 플레이어에겐 미래 오브젝트만 표시

---

## E4: AI & Alert

### EnemyController + Waypoint 순찰
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** High
- **Label:** Team A, E4-AI
- **Description:**
- 목표: 경비 AI의 기본 이동 및 순찰 시스템을 구현한다
- 배경: game-flow Step 6에서 과거 경비원/CCTV, 미래 로봇/CCTV가 보안실 앞 복도를 순찰
- 상세 작업 내용:
  - Waypoint 배열 순회 순찰, 방향 전환
  - EnemyTuningSO: moveSpeed, patrolWaitTime
  - Team B가 Waypoint 좌표 설정
- 참고자료: `DSD.md` §3.6
- 수용 기준: 적 캐릭터가 지정된 Waypoint를 순회하며 순찰

### EnemyPerception2D + FSM
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team A, E4-AI
- **Description:**
- 목표: 적 AI의 감지/추격/탐색/복귀 상태 머신을 구현한다
- 배경: 보안실 앞 복도(Step 6)에서 은신 플레이 지원
- 상세 작업 내용:
  - 10u 감지 거리, 45° FOV, 장애물 Raycast 차폐
  - 웅크리기 시 감지 거리 50%
  - FSM: Patrol → Chase → Search(10초) → Patrol
- 참고자료: `DSD.md` §3.6
- 수용 기준: 시야 내 플레이어 감지 → 추격 → 시야 이탈 → 탐색 → 복귀

### AlertService (경보 전파)
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** Medium
- **Label:** Team A, E4-AI
- **Description:**
- 목표: 현재 방 + 인접 방에 경보 상태를 전파하는 시스템을 구현한다
- 배경: M1 완료 후 경비 시스템 가동(game-flow Step 5), 잘못된 퍼즐 입력 시 경보
- 상세 작업 내용:
  - AlertState: None / Caution / Alert
  - RoomManager 연동 (인접 룸 조회)
  - 경보 감쇠 타이머
- 참고자료: `DSD.md` §3.6
- 수용 기준: 경보 발생 시 현재 룸 + 인접 룸의 적 AI가 Alert 상태로 전환

### Phase 3 로봇 추격 AI
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** Medium
- **Label:** Team A, E4-AI
- **Description:**
- 목표: C동 탈출 시퀀스의 추격 AI를 구현한다
- 배경: game-flow Step 11 — 과거: 프로토타입 로봇, 미래: 진보한 로봇이 좌측에서 추격
- 상세 작업 내용:
  - 이동속도 증가, 감지 범위 확대, 추격 지속 (FSM에서 복귀 없음)
  - 구간별 타이머 판정 (TBD 값)
  - 공동 실패 조건 연동 (한쪽 포획 → 양쪽 실패)
- 참고자료: `game-design.md` §7, `game-design.md` Step 11
- 수용 기준: 로봇이 좌측에서 일정 속도로 추격, 플레이어 포획 시 실패 판정

---

## E5: Level Content

### game-flow 기반 룸별 퍼즐 동선 확정
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Team B, E5-Level
- **Description:**
- 목표: game-design.md의 11개 Step을 기반으로, 각 룸에서의 플레이어 동선과 퍼즐 순서를 확정한다
- 배경: game-design.md에 큰 흐름은 확정됐지만, 룸 내부에서의 세부 동선과 오브젝트 접근 순서가 미확정
- 상세 작업 내용:
  - 각 룸의 진입점 → 오브젝트 접근 순서 → 퍼즐 수행 → 퇴장점
  - 과거/미래 플레이어가 같은 룸에서 다른 동선을 탈 경우의 분기
  - game-design.md §3~§7의 상세 단계와 매핑
- 참고자료: `game-design.md`, `game-design.md` §3~§7
- 수용 기준: 모든 룸에 대해 동선 다이어그램 또는 순서 목록 완성

### interaction-spec §8 기반 오브젝트 배치 리스트 확정
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Team B, E5-Level
- **Description:**
- 목표: 각 오브젝트의 정확한 룸 내 위치, 크기, 시간대(과거/미래/양쪽)를 확정한다
- 배경: game-design.md §8에 룸별 오브젝트 ID가 정의됨. 실제 Unity 좌표로 변환 필요
- 상세 작업 내용:
  - P1: 16개 오브젝트 (로비 3, 가는 길 2, 복도 1, 변전실 7, 보안실 3)
  - P2: 10개 오브젝트 (연구소장실 1, 2층복도 2, 자료실 2, 실험실 2, 샘플저장고 3)
  - P3: 6개 오브젝트 (3구간)
  - 각 오브젝트의 과거/미래 차이 명시
- 참고자료: `game-design.md` §8
- 수용 기준: 오브젝트별 위치 좌표(또는 상대 위치), 크기, 시간대가 문서화됨

### Phase 1 룸 화이트박싱 (7룸)
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** Urgent
- **Label:** Team B, E5-Level
- **Description:**
- 목표: Phase 1의 7개 룸을 Unity 타일맵으로 화이트박스 구축한다
- 배경: game-flow Step 1~7에 해당하는 A동 전체 룸
- 상세 작업 내용:
  - A동 1F 연구소 로비
  - A동 B1 변전실로 가는 길
  - A동 B1 변전실 앞 복도
  - A동 B1 변전실
  - A동 1F 보안실 앞 복도
  - A동 1F 보안실
  - (B1↔1F 연결 통로/계단/엘리베이터)
  - 20~30u × 10~14u 기준, Rule Tile 적용
- 참고자료: `game-design.md` Step 1~7 📍 룸 정보
- 수용 기준: 7개 룸이 씬에 존재, 플레이어가 룸 간 이동 가능

### Phase 2 룸 화이트박싱 (6룸+)
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team B, E5-Level
- **Description:**
- 목표: Phase 2의 6개+ 룸을 Unity 타일맵으로 화이트박스 구축한다
- 배경: game-flow Step 8~10에 해당하는 A동 2F + 구름다리 + B동 전체
- 상세 작업 내용:
  - A동 2F 연구소장실
  - A동 2F 2층 복도
  - 구름다리 (A→B)
  - B동 2F 자료실
  - B동 1F 실험실
  - B동 1F 샘플 저장고
  - (B동 1F/2F 복도)
- 참고자료: `game-design.md` Step 8~10 📍 룸 정보
- 수용 기준: 6개+ 룸이 씬에 존재, A동↔B동 구름다리 연결

### P1~P2 오브젝트 배치 + 컴포넌트 연결
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team B, E5-Level
- **Description:**
- 목표: 확정된 배치 리스트에 따라 P1~P2 룸에 InteractableObject 프리팹을 배치하고 연결한다
- 배경: Team A가 제공하는 InteractableObject, CausalTrigger, CausalReceiver 컴포넌트를 씬 오브젝트에 부착
- 상세 작업 내용:
  - game-design.md §8의 26개 오브젝트 배치
  - 각 오브젝트에 올바른 InteractionType, objectId, requiredItemId 설정
  - CausalTrigger/Receiver 연결
- 참고자료: `game-design.md` §8
- 수용 기준: 모든 P1~P2 오브젝트가 씬에 배치되고, Inspector에서 설정 완료

### 과거/미래 환경 차이 비주얼 적용
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team B, E5-Level
- **Description:**
- 목표: 동일 룸 구조에 시간대별 비주얼 차이를 적용한다
- 배경: game-design.md 룸 정보에서 과거(정돈)/미래(폐허)의 오브젝트 차이가 명시됨
- 상세 작업 내용:
  - 미래: 균열, 덩굴, 부식, 어두운 조명, 고장난 장비
  - 과거: 정돈된 시설, 밝은 조명, 작동하는 장비
  - Team A의 분리 렌더링 시스템(Layer) 연동
- 참고자료: `game-design.md` 📍 룸 정보의 과거/미래 열
- 수용 기준: 과거/미래 레이어 전환 시 시각적 차이가 명확히 구분됨

### AI 순찰 경로 + 은신 포인트 배치
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team B, E5-Level
- **Description:**
- 목표: 각 룸의 AI 순찰 Waypoint와 플레이어 은신 포인트를 배치한다
- 배경: game-flow Step 6 — 보안실 앞 복도에서 과거 경비원/미래 로봇 순찰, 은신 필요
- 상세 작업 내용:
  - 보안실 앞 복도: 순찰 Waypoint 좌표, CCTV 위치
  - 은신 포인트: 책상 아래, 선반 뒤 등
  - Team A의 EnemyController 연동
- 참고자료: `game-design.md` Step 5~6
- 수용 기준: AI가 설정된 경로를 순찰, 플레이어가 은신 포인트에서 감지 회피 가능

### Phase 3 C동 탈출 루트 구축 + 장애물 배치
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** Medium
- **Label:** Team B, E5-Level
- **Description:**
- 목표: C동(폐쇄된 저장고) 3구간의 횡스크롤 탈출 레이아웃과 장애물을 배치한다
- 배경: game-flow Step 11, interaction-spec §7. A/B동과 별개인 독립 공간
- 상세 작업 내용:
  - 구간 1: 무너진 바닥, 잠긴 비상문, 레이저 그리드, 높은 플랫폼
  - 구간 2: 봉쇄된 통로, 바리케이드, 전기 함정, 무너진 다리
  - 구간 3: 최종 봉쇄문 (동시 조작)
  - 인과 장치(레버, 잠금 해제) 배치
- 참고자료: `game-design.md` §7, `game-design.md` Step 11
- 수용 기준: 3구간 레이아웃 완성, 장애물 배치, 인과 장치 연결

### 레벨 밸런스 조정
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** Medium
- **Label:** Team B, E5-Level
- **Description:**
- 목표: 플레이테스트 기반으로 전 Phase의 밸런스를 조정한다
- 배경: 통합 테스트 후 난이도, 은신 포인트, 퍼즐 힌트 등 조정 필요
- 상세 작업 내용:
  - AI 순찰 경로 난이도 (속도, 시야)
  - 은신 포인트 수/위치
  - 퍼즐 힌트 명확성
  - Phase 3 장애물 간격
- 수용 기준: 2회 이상 플레이테스트 후 피드백 반영 완료

### 인과 시각 피드백 + 환경 디테일
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** Medium
- **Label:** Team B, E5-Level
- **Description:**
- 목표: 인과 트리거/리시버 활성화 시의 시각 피드백과 환경 소품을 추가한다
- 배경: 인과 발동 시 플레이어에게 시각적 확인이 필요 (예: 전력 복구 → 조명 켜짐)
- 상세 작업 내용:
  - 트리거 활성화: 스파크/파티클
  - 리시버 변경: 환경 전환 애니메이션 (문 열림, 조명 점등)
  - 룸별 소품 디테일 추가
- 수용 기준: 인과 발동 시 시각적 변화가 명확히 인지됨

---

## E6: Network

### Netcode 세션 (Host/Join + 역할 배정)
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** High
- **Label:** Team A, E6-Network
- **Description:**
- 목표: 2인 온라인 세션 생성, 참가, 역할(과거/미래) 배정을 구현한다
- 배경: game-flow의 모든 흐름이 과거/미래 2인 동시 플레이 전제
- 상세 작업 내용:
  - Unity Netcode for GameObjects 기반
  - Host 생성, Client 참가, 2인 제한
  - 접속 시 TimelineRole(Past/Future) 배정
  - 로비 씬에서 양쪽 Ready → 게임 시작
- 참고자료: `DSD.md` §3.2
- 수용 기준: 2인 접속 → 역할 배정 → 게임 씬 로드

### 플레이어 위치 동기화 + 정보 격리
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team A, E6-Network
- **Description:**
- 목표: 네트워크 상에서 플레이어 위치를 동기화하되, 시간대 간 정보를 격리한다
- 배경: 과거 플레이어는 미래 환경을 볼 수 없고, 그 반대도 마찬가지
- 상세 작업 내용:
  - NetworkTransform으로 위치 동기화
  - 정보 격리: 자기 시간대 환경/오브젝트만 표시
  - 상대 플레이어 인벤토리 비공개
- 참고자료: `DSD.md` §3.2
- 수용 기준: 양 플레이어가 자기 시간대만 보면서 동시에 이동 가능

### 인과 상태 네트워크 전파
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** Urgent
- **Label:** Team A, E6-Network
- **Description:**
- 목표: 인과 트리거 활성화 시 네트워크를 통해 리시버 상태를 전파한다
- 배경: 과거 플레이어가 레버를 당기면 → 미래 플레이어의 환경이 변해야 함 (핵심 메커니즘)
- 상세 작업 내용:
  - CausalTrigger → ServerRpc → Host에서 CausalityService 실행
  - Host → ClientRpc → 리시버 상태 변경 전파
  - Host Authority 원칙 준수
- 참고자료: `DSD.md` §3.1, §3.2
- 수용 기준: 과거 트리거 → 미래 리시버 상태 변경이 네트워크 상에서 정확히 동기화

---

## E7: Game Flow & UI

### SO 스키마 일괄 정의
- **Project:** Sprint 1
- **Milestone:** W1
- **Priority:** High
- **Label:** Team A, E7-Flow
- **Description:**
- 목표: CausalRuleSO, EnemyTuningSO, ItemDefinitionSO, RoomGraphSO 4종의 C# 클래스를 작성한다
- 배경: Team B가 Inspector에서 데이터를 입력하려면 SO 클래스가 먼저 존재해야 함
- 상세 작업 내용:
  - CausalRuleSO: ruleId, triggerId, receiverId, requiredItemId, condition, effect
  - EnemyTuningSO: moveSpeed, patrolWaitTime, detectionRange, fov
  - ItemDefinitionSO: itemId, displayName, icon, consumable
  - RoomGraphSO: roomId, displayName, adjacentRoomIds[], spawnPointIds[]
- 참고자료: `DSD.md` §4
- 수용 기준: 4종 SO 클래스 생성, 빈 에셋 생성 확인, Inspector에서 필드 편집 가능

### GameFlowManager (P1→P2→P3 + 결과)
- **Project:** Sprint 1
- **Milestone:** W2
- **Priority:** High
- **Label:** Team A, E7-Flow
- **Description:**
- 목표: Phase 상태 관리 및 전환 시스템을 구현한다
- 배경: game-design.md의 Phase 1→2→3 전환 트리거가 Major Interaction 완료에 의존
- 상세 작업 내용:
  - P1: M1+M2 완료 → P2 진입
  - P2: M3+M4 완료 → P3 진입
  - P3: 양쪽 출구 도달 → 결과 화면
  - Phase 전환 이벤트 발행
- 참고자료: `DSD.md` §2.3, `game-design.md` Phase 구분
- 수용 기준: Major 완료 시 자동 Phase 전환, 결과 화면 표시

### CheckpointService + 공동 실패
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** High
- **Label:** Team A, E7-Flow
- **Description:**
- 목표: 체크포인트 저장/복원 및 공동 실패 판정을 구현한다
- 배경: interaction-spec §7 판정 — 한쪽이라도 포획되면 공동 실패 → 구간 시작점 복귀
- 상세 작업 내용:
  - 스냅샷: 플레이어 위치, 인벤토리, 인과 완료 상태
  - 자동 저장 시점: Phase 전환, Major 완료
  - 공동 실패 시 마지막 체크포인트 복원
- 참고자료: `DSD.md` §3.9, `game-design.md` §7 판정
- 수용 기준: AI 포획 → 체크포인트 복원이 양 플레이어에서 동시 작동

### SplitViewManager (Phase 3)
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** Medium
- **Label:** Team A, E7-Flow
- **Description:**
- 목표: Phase 3 진입 시 화면을 상하 분할하여 양쪽 시간대를 동시 표시한다
- 배경: game-flow Step 11 — "스플릿뷰 전환 — 상: 과거, 하: 미래"
- 상세 작업 내용:
  - 상하 5:5 분할
  - 상단: 과거 플레이어 카메라 / 하단: 미래 플레이어 카메라
  - RenderTexture + 보조 Camera 기반
- 참고자료: `DSD.md` §3.8
- 수용 기준: Phase 3에서 양쪽 시간대가 동시에 표시됨

### HUD (인벤토리, 프롬프트, Objective)
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** Medium
- **Label:** Team A, E7-Flow
- **Description:**
- 목표: 인게임 HUD를 구현한다
- 배경: game-design.md의 🎯 Objective 텍스트, 오브젝트 상호작용 프롬프트
- 상세 작업 내용:
  - 하단 인벤토리 5슬롯 바
  - 상호작용 프롬프트 ("E — 조사" / "RMB — [아이템] 사용")
  - 🎯 Objective 텍스트 표시 (game-flow의 각 Step Objective)
  - 무전기 상태 표시 (PTT 활성/비활성)
- 참고자료: `DSD.md` §3.10, `game-design.md` 🎯
- 수용 기준: Objective 변경 시 HUD 갱신, 상호작용 프롬프트 정확히 표시

### 통합 플레이테스트 (2인 온라인)
- **Project:** Sprint 1
- **Milestone:** W3
- **Priority:** Urgent
- **Label:** Team A, Team B, E7-Flow
- **Description:**
- 목표: Phase 1~3 전체 흐름을 2인 온라인으로 테스트한다
- 배경: 스프린트 최종 산출물 검증
- 상세 작업 내용:
  - 최소 2회 수행
  - game-design.md Step 1~11 전체 시나리오 따라가기
  - 버그, 밸런스, UX 피드백 기록
  - Sprint 2 반영 사항 정리
- 참고자료: `game-design.md` 전체, `sprint1-plan.md` §2 W3 체크포인트
- 수용 기준: 2회 이상 온라인 플레이테스트 완료, 피드백 문서화
