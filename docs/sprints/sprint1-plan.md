# Caretaker — Sprint 1 개발 계획

**Cycle:** 2026.05.22 (목) ~ 2026.06.10 (화) · 3주  
**Sprint Goal:** Phase 1~3 전체를 2인 온라인에서 플레이 가능한 프로토타입으로 완성

> **전략:** AI 에이전트 보조로 코드 구현 속도가 빠르므로, **설계/명세 프론트로딩**에 집중한다. W1에서 전 Phase의 설계를 확정하고, W2~W3에서 구현+통합한다.

---

## 0. 전체 콘텐츠 범위

| Phase | Major Interaction | 룸 | 핵심 시스템 |
|-------|------------------|----------|------------|
| **P1: 진입** | M1 전력 복구, M2 네트워크 침입 | A동 1F 로비, B1 변전실 가는 길/앞 복도/변전실, 1F 보안실 앞 복도/보안실 — 7룸 | 이동, 상호작용, 인과, AI, 인벤토리 |
| **P2: 탐색** | M3 청사진 식별, M4 바이러스/실린더 | A동 2F 연구소장실/2층복도, 구름다리, B동 2F 자료실, 1F 실험실/샘플저장고 — 6룸+ | 위 + 추가 CausalRule, 카드키 |
| **P3: 탈출** | 동시 탈출 시퀀스 | C동 (폐쇄된 저장고) — 3구간 | 스플릿뷰, 로봇 추격, 타이머/동시 판정 |

---

## 1. 팀 편성

| 팀 | 담당 | 정체성 |
|------|------|--------|
| **Team A — System** | 배원일, 유민서(PM), 백승헌 | **C# 코드 전량 담당.** 시스템, 플레이어, 인과, AI, 네트워크, UI 등 모든 스크립트 |
| **Team B — Content** | 김병규, 김도경, 권순표 | **맵·콘텐츠 전량 담당.** 룸 레이아웃, 퍼즐 동선, 오브젝트 배치, 환경 비주얼, 밸런스 |

> **Team B 운영 방식:** 역할 분담 없이 **셋이 함께 설계하고 함께 만드는 협업 방식**을 유지한다.

> **팀 간 인터페이스:** Team A가 만든 컴포넌트(InteractableObject, CausalTrigger, RoomVolume 등)를 Team B가 씬에 배치한다. Team B가 확정한 룸 구조와 퍼즐 동선을 Team A가 RoomGraphSO, CausalRuleSO 데이터로 변환한다.

---

## 2. 주차별 목표

### W1: 설계 확정 + 기반 구축 (5/22 ~ 5/28)

| 팀 | 작업 | 기간 |
|------|------|------|
| **전원** | Phase 2~3 CausalRule 명세, Phase 3 탈출 시퀀스 설계 확정 | 5/22~23 (2일) |
| **전원** | Interaction Spec (M1~M4 퍼즐 상세) 문서화 | 5/22~24 (3일) |
| Team A | 프로젝트 구조 정리 + SO 스키마 정의 | 5/22~23 (2일) |
| Team A | PlayerMotor2D (이동/점프/웅크리기) | 5/22~25 (4일) |
| Team A | InteractableObject + InteractionProbe + Service | 5/23~27 (5일) |
| Team A | RoomVolume + RoomManager + CameraDirector | 5/24~27 (4일) |
| Team A | EnemyController + Waypoint 순찰 | 5/25~27 (3일) |
| Team A | Netcode 세션 (Host/Join + 역할 배정) | 5/24~26 (3일) |
| Team B | 룸별 퍼즐 동선 확정 | 5/22~25 (4일) |
| Team B | 오브젝트 배치 리스트 확정 | 5/23~26 (4일) |
| Team B | Phase 1 룸 화이트박싱 (7룸) — Unity 타일맵 | 5/24~28 (5일) |

**W1 체크포인트 (5/28):**
- [ ] 전 Phase 설계 문서 확정 (Interaction Spec, Phase 3 탈출 설계)
- [ ] 캐릭터가 P1 룸 7개를 이동, 룸 전환 시 카메라 추적
- [ ] 플레이어 주변 오브젝트 하이라이트, 좌클릭 → 조사/획득, 우클릭 → 아이템 사용, E키 → 조작
- [ ] 경비원 waypoint 순찰
- [ ] Netcode Host-Client 세션 연결 확인 (빈 씬)
- [ ] Team B: 전 Phase 룸별 오브젝트 배치 리스트 완성

### W2: Phase 1~2 콘텐츠 + 네트워크 + AI (5/29 ~ 6/04)

| 팀 | 작업 | 기간 |
|------|------|------|
| Team A | CausalityManager + CausalityService | 5/29~31 (3일) |
| Team A | CausalRuleSO — M1~M4 전 규칙 작성 | 5/30~6/1 (3일) |
| Team A | InventorySystem + 아이템-환경 조합 | 5/29~31 (3일) |
| Team A | EnemyPerception2D + FSM + AlertService | 5/29~6/2 (5일) |
| Team A | Netcode 플레이어 동기화 + 정보 격리 | 6/1~3 (3일) |
| Team A | 과거/미래 월드 분리 렌더링 (Layer/Scene) | 5/29~31 (3일) |
| Team A | GameFlowManager (Phase 1→2 전환) | 6/2~4 (3일) |
| Team B | Phase 2 룸 화이트박싱 (6룸) | 5/29~6/1 (4일) |
| Team B | P1~P2 룸에 InteractableObject 배치 + 연결 | 5/31~6/3 (4일) |
| Team B | 과거/미래 환경 차이 비주얼 적용 | 6/1~4 (4일) |
| Team B | AI 순찰 경로 설정 + 은신 포인트 배치 | 6/2~4 (3일) |

**W2 체크포인트 (6/04):**
- [ ] M1~M4 인과 규칙이 로컬에서 전부 작동
- [ ] 2인 온라인 접속 + 과거/미래 역할 배정
- [ ] 인과 상태가 네트워크로 전파
- [ ] AI가 감지/추격/탐색/복귀 전 상태 동작
- [ ] Phase 1 → Phase 2 전환 작동
- [ ] Team B: P1~P2 전 룸에 오브젝트 배치 완료, 과거/미래 비주얼 차이 적용

### W3: Phase 3 + 통합 + 폴리싱 (6/05 ~ 6/10)

| 팀 | 작업 | 기간 |
|------|------|------|
| Team A | SplitViewManager (상하 5:5 RenderTexture) | 6/5~7 (3일) |
| Team A | CheckpointService + 공동 실패 → 체크포인트 복귀 | 6/5~7 (3일) |
| Team A | Netcode 인과 동기화 (ServerRpc/ClientRpc) | 6/5~6 (2일) |
| Team A | Phase 3 강화 AI + 타이머 판정 | 6/6~8 (3일) |
| Team A | HUD (인벤토리바, 프롬프트, 무전기 UI) | 6/6~8 (3일) |
| Team A | GameFlowManager (Phase 2→3 전환 + 결과) | 6/7~8 (2일) |
| Team B | Phase 3 C동 탈출 루트 구축 (3구간) + 장애물 배치 | 6/5~7 (3일) |
| Team B | 레벨 밸런스 조정 (전 Phase) | 6/7~9 (3일) |
| Team B | 인과 시각 피드백 + 환경 디테일 | 6/7~9 (3일) |
| **전체** | **통합 플레이테스트 (2인 온라인)** | **6/8~10 (3일)** |

**W3 / Sprint 종료 체크포인트 (6/10):**
- [ ] Phase 1~3 전체 흐름 플레이 가능
- [ ] Phase 3 스플릿뷰에서 양쪽 시간대 동시 표시
- [ ] 공동 실패 → 체크포인트 복귀 정상 작동
- [ ] 2인 온라인 플레이테스트 2회 이상 완료

---

## 3. 이슈 요약

전체 이슈 리스트 및 상세는 [`sprint1-issues.md`](sprint1-issues.md) 참조.

| Epic | 이슈 수 | 팀 |
|------|:-------:|:---:|
| E0: Design & Spec | 3 | 전원 |
| E1: Player & Interaction | 6 | A |
| E2: Causality | 4 | A |
| E3: Room & Camera | 4 | A |
| E4: AI & Alert | 4 | A |
| E5: Level Content | 10 | B |
| E6: Network | 3 | A |
| E7: Game Flow & UI | 6 | A |
| **합계** | **40** | |

---

## 4. 팀별 주차 요약

### Team A — System (배원일, 유민서, 백승헌)

| 팀원 | W1 (5/22~28) | W2 (5/29~6/4) | W3 (6/5~10) |
|------|-------------|---------------|-------------|
| **유민서** (PM) | 설계 확정, SO 스키마 | CausalityService/Manager, CausalRuleSO, GameFlow P1→P2 | Checkpoint, GameFlow P2→P3, Netcode 인과, **통합 테스트** |
| **백승헌** | PlayerMotor2D, InteractableObject, InteractionService | InventorySystem, 아이템 조합 | HUD, 버그픽스, QA |
| **배원일** | RoomManager, CameraDirector, EnemyController, Netcode 세션 | Perception, FSM, Alert, 플레이어 동기화, 분리 렌더링 | SplitView, P3 강화 AI |

### Team B — Content (김병규, 김도경, 권순표)

> 셋이 함께 작업. 개인 분담 없이 공동 진행.

| W1 (5/22~28) | W2 (5/29~6/4) | W3 (6/5~10) |
|-------------|---------------|-------------|
| 룸별 퍼즐 동선 확정 | Phase 2 화이트박싱 (6룸) | Phase 3 C동 탈출 루트 (3구간) |
| 오브젝트 배치 리스트 확정 | 오브젝트 배치 + 컴포넌트 연결 | 레벨 밸런스 조정 |
| Phase 1 화이트박싱 (7룸) | 과거/미래 환경 차이 비주얼 | 인과 시각 피드백 + 디테일 |
| | AI 순찰 경로 + 은신 포인트 | 통합 플레이테스트 참여 |

---

## 5. 팀 간 인터페이스 규약

| 시점 | Team A → Team B | Team B → Team A |
|------|----------------|----------------|
| **W1 중반** | InteractableObject 베이스 클래스 + 사용법 가이드 | 룸별 오브젝트 배치 리스트 (이름, 위치, 유형) |
| **W1 말** | RoomVolume Prefab + 사용법 | 확정된 룸 레이아웃 + 진입점 좌표 |
| **W2 초** | CausalTrigger/Receiver Prefab | 퍼즐 동선 확정본 |
| **W2 중반** | InventorySystem + ItemDefinitionSO | AI 순찰 Waypoint 좌표 |
| **W3** | 완성된 시스템 | 완성된 레벨에 시스템 컴포넌트 배치 완료 |

> 매일 15분 크로스 체크: Team A가 만든 Prefab/컴포넌트를 Team B가 씬에 올바르게 배치하고 있는지 확인한다.

---

## 6. 리스크 & 대응

| 리스크 | 확률 | 영향 | 대응 |
|--------|:----:|:----:|------|
| Phase 3 설계 미확정 | 높음 | 높음 | W1에 반드시 확정. 미확정 시 최소 탈출 시퀀스로 축소 |
| Team A 코드 부하 과중 | 중 | 높음 | AI 에이전트 적극 활용 |
| Team B 배치 ↔ Team A 컴포넌트 불일치 | 중 | 중 | 매일 크로스 체크 + W1 말 Prefab 가이드 |
| Netcode 통합 지연 | 중 | 높음 | W1에 세션 기초만 확인. 최악 시 로컬 2인 폴백 |
| 스플릿뷰 기술 난이도 | 중 | 중 | 단순 구현 우선, 연출은 후순위 |
| 레벨 볼륨 (16+ 룸) | 높음 | 중 | 화이트박스 최소 기하. P3는 C동 독립 공간으로 신규 구축 |

---

## 7. Phase 3 최소 보장 범위 (Fallback)

| 수준 | 구현 범위 |
|------|----------|
| **최소** | 스플릿뷰 ON → C동 직선 탈출 루트 1개 → 로봇 추격 → 출구 도달 판정 |
| **목표** | 스플릿뷰 ON → C동 3구간 → 과거/미래 인과 연동 장애물 → 동시 탈출 판정 |
| **이상적** | 위 + 인과 기반 경로 분기 + 타이머 연출 |

최소 수준이라도 "Phase 3이 존재하고, 스플릿뷰가 작동하고, 게임이 끝난다"를 보장한다.
