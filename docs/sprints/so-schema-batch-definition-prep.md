# SO 스키마 일괄 정의 준비

## 목적

Team B가 Inspector에서 정적 데이터를 입력할 수 있도록 `CausalRuleSO`, `EnemyTuningSO`, `ItemDefinitionSO`, `RoomGraphSO` 4종의 스키마를 확정하고 구현 준비 상태를 정리한다.

## 확인한 기준 문서

| 문서 | 확인 내용 |
| --- | --- |
| `docs/sprints/sprint1-issues.md` | "SO 스키마 일괄 정의" 이슈는 4종 SO 클래스 생성과 빈 에셋 생성 확인을 수용 기준으로 둔다. |
| `docs/design/DSD.md` | DSD §4.2는 4종 외에 `CheckpointDefinitionSO`, `PhaseDefinitionSO`까지 포함한 확장 스키마를 정의한다. |
| `docs/design/game-design.md` | 전체 인과 규칙 13개, 아이템 5개, 룸/오브젝트 배치표가 실제 데이터 입력 대상이다. |
| `docs/technical/coding-standards.md` | ScriptableObject 에셋명은 `SO_` 접두사를 사용하고 `[SerializeField] private` + public getter 패턴을 따른다. |

## 현재 구현 상태

| SO | 파일 | 상태 |
| --- | --- | --- |
| `CausalRuleSO` | `Caretaker/Assets/_Project/Scripts/World/CausalRuleSO.cs` | DSD 우선 스키마로 갱신했다. `requiredRole`, `conditions[]`, `receiverEffects[]`, `InteractionWeight`를 포함하고, 단일 효과 규칙용 호환 getter도 제공한다. |
| `RoomGraphSO` | `Caretaker/Assets/_Project/Scripts/World/RoomGraphSO.cs` | `displayName`을 추가했고 `timeline`은 `TimelineRole` enum으로 정리했다. DSD 확장 필드인 `pairedTimelineRoomId`도 유지한다. |
| `EnemyTuningSO` | `Caretaker/Assets/_Project/Scripts/Gameplay/AI/EnemyTuningSO.cs` | DSD 필드에 더해 스프린트 이슈의 `moveSpeed`, `patrolWaitTime` 의미를 실제 필드로 추가했다. |
| `ItemDefinitionSO` | `Caretaker/Assets/_Project/Scripts/Gameplay/Inventory/ItemDefinitionSO.cs` | DSD 필드에 더해 스프린트 이슈의 `icon` 의미를 `Sprite Icon` 필드로 추가했다. `consumeOnUse`는 `consumable` 의미로 사용한다. |
| `CheckpointDefinitionSO` | `Caretaker/Assets/_Project/Scripts/Core/CheckpointDefinitionSO.cs` | DSD 확장 SO로 이미 존재한다. |
| `PhaseDefinitionSO` | `Caretaker/Assets/_Project/Scripts/Core/PhaseDefinitionSO.cs` | DSD 확장 SO로 이미 존재한다. |
| `PhasePresentationSO` | `Caretaker/Assets/_Project/Scripts/Presentation/PhasePresentationSO.cs` | DSD의 Presentation 데이터 에셋으로 이미 존재한다. |

`Caretaker/Assets/_Project/Data`에는 아직 `.gitkeep`만 있고 실제 SO 에셋은 없다.

## 권장 스키마 기준

스프린트 이슈는 최소 수용 기준으로 보고, C# 클래스는 DSD §4.2를 우선 기준으로 맞춘다. 단, Team B 입력 편의와 현재 이슈 수용 기준을 위해 스프린트 이슈의 필드 의미가 사라지지 않게 별칭 없이 명확한 도메인명으로 통합한다.

### CausalRuleSO

권장 필드:

| 필드 | 타입 후보 | 비고 |
| --- | --- | --- |
| `ruleId` | `string` | `CR_{phase}_{verb}_{target}` 형식 |
| `triggerId` | `string` | `CausalTrigger.TriggerId`와 매칭 |
| `requiredRole` | `TimelineRole` | 기본값 `Past`; Future 전용/비인과 규칙은 별도 정책 필요 |
| `requiredItemId` | `string` | 빈 값이면 아이템 불필요 |
| `conditions` | serializable struct array | 퍼즐 상태, 코드 입력 성공 등 비아이템 조건 표현 |
| `receiverEffects` | serializable struct array | receiverId, stateKey, stateValue를 1개 이상 표현 |
| `interactionWeight` | enum 또는 int | Major/Minor 구분. 단순 bool보다 DSD 표현에 가까움 |

준비 메모:

- 기존 `receiverId`, `receiverStateKey`, `receiverStateValue` 단일 필드는 `receiverEffects[]`로 승격했다. 단일 효과 규칙을 위한 호환 getter는 유지한다.
- `CR_P2_SAFE_OPEN`, `CR_P2_ARCHIVE_ENTER`, `CR_P2_SHIELD_DOWN`은 game-design상 "과거 전용" 또는 "인과 없음"이라, CausalRuleSO에 넣을지 별도 `InteractableObject`/Inventory 로직으로 둘지 결정이 필요하다.

### EnemyTuningSO

권장 필드:

| 필드 | 타입 후보 | 비고 |
| --- | --- | --- |
| `enemyType` | `string` | 과거 경비원, 미래 로봇, CCTV 등 |
| `moveSpeed` | `float` | 순찰 이동 속도. 스프린트 이슈의 필드 반영 |
| `patrolWaitTime` | `float` | Waypoint 대기 시간. 스프린트 이슈의 필드 반영 |
| `sightDistance` | `float` | 스프린트 이슈의 `detectionRange` 의미 |
| `fovDegrees` | `float` | 스프린트 이슈의 `fov` 의미 |
| `chaseSpeed` | `float` | 추격 이동 속도 |
| `loseSightSeconds` | `float` | 수색/복귀 기준 |
| `alertDuration` | `float` | 경보 지속 시간 |

### ItemDefinitionSO

권장 필드:

| 필드 | 타입 후보 | 비고 |
| --- | --- | --- |
| `itemId` | `string` | `ITEM_{category}_{name}` 형식 |
| `displayName` | `string` | 로컬라이즈 전 임시 표시명 |
| `icon` | `Sprite` | 스프린트 이슈의 필드 반영 |
| `category` | `string` 또는 enum | DSD 필드 |
| `usableTargetTags` | `string[]` | 대상 검증용 |
| `consumeOnUse` | `bool` | 스프린트 이슈의 `consumable` 의미 |

### RoomGraphSO

권장 필드:

| 필드 | 타입 후보 | 비고 |
| --- | --- | --- |
| `roomId` | `string` | `{timeline}_{area}_{index}` 형식 |
| `displayName` | `string` | Team B와 플레이어 커뮤니케이션용 이름 |
| `timeline` | `TimelineRole` | Past/Future/None 중 하나로 제한한다. |
| `adjacentRoomIds` | `string[]` | 룸 전환/경보 전파 기준 |
| `pairedTimelineRoomId` | `string` | 병렬 시설의 대응 룸 |
| `spawnPointIds` | `string[]` | 체크포인트/룸 진입 스폰 후보 |

## 실제 데이터 입력 대상

### CausalRuleSO 후보

game-design 기준 전체 후보는 13개다.

- Phase 1: `CR_P1_POWER_LEVER`, `CR_P1_POWER_PANEL`, `CR_P1_VENT_OPEN`, `CR_P1_SEC_HACK`
- Phase 2: `CR_P2_SAFE_OPEN`, `CR_P2_ARCHIVE_ENTER`, `CR_P2_BLUEPRINT_ID`, `CR_P2_VIRUS_PLANT`, `CR_P2_SHIELD_DOWN`
- Phase 3: `CR_P3_BRIDGE_DROP`, `CR_P3_DOOR_UNLOCK`, `CR_P3_PLATFORM_LOWER`, `CR_P3_TOOL_PLACE`

인과 SO로 확정하기 전 검토할 후보:

- `CR_P2_SAFE_OPEN`: 카드키 획득 이벤트라 Inventory/Interactable 쪽 데이터일 수 있다.
- `CR_P2_ARCHIVE_ENTER`: 아이템 사용 잠금 해제라 CausalRule보다 Interaction 조건일 수 있다.
- `CR_P2_SHIELD_DOWN`: game-design에 "미래 전용, 인과 없음"으로 명시되어 CausalRuleSO 대상에서 제외하는 편이 자연스럽다.
- `CR_P3_TOOL_PLACE`: `ITEM_P3_TOOL`이 아직 TBD다.

### ItemDefinitionSO 후보

- `ITEM_TOOL_DRIVER`: 비소모
- `ITEM_CABLE`: 소모
- `ITEM_BATTERY`: 소모
- `ITEM_KEY_CARD`: 비소모
- `ITEM_P3_TOOL`: TBD

## Seed 생성 유틸

`Caretaker/Assets/_Project/Scripts/Editor/SoSeedAssetGenerator.cs`에 초기 SO 에셋 생성 메뉴를 추가했다.

Unity 메뉴:

```text
Caretaker SO > Seed > Create Missing Seed Data Assets
```

동작:

- `Assets/_Project/Data/Causality`, `Inventory`, `Rooms`, `AI` 폴더가 없으면 생성한다.
- 기존 에셋은 덮어쓰지 않고 건너뛴다.
- `CausalRuleSO`는 확정된 Past→Future 물리 인과 seed만 만든다.
- `CR_P3_TOOL_PLACE`는 `ITEM_P3_TOOL`/효과가 TBD라 아직 CausalRule seed에서 제외한다.
- `ITEM_P3_TOOL`은 후속 설계 입력을 위해 placeholder ItemDefinition seed로 생성한다.

### RoomGraphSO 후보

최소 입력 대상:

- Phase 1: A동 1F 로비, A동 B1 변전실로 가는 길, A동 B1 변전실 앞 복도, A동 B1 변전실, A동 1F 보안실 앞 복도, A동 1F 보안실
- Phase 2: A동 2F 연구소장실, A동 2F 2층 복도, 구름다리, B동 2F 자료실, B동 1F 실험실, B동 1F 샘플 저장고
- Phase 3: 폐쇄된 저장고 C동 구간 1, 구간 2, 구간 3

각 룸은 Past/Future 병렬 룸이 필요하므로, 실제 `RoomGraphSO` 에셋 수는 최소 룸 개수의 2배가 될 수 있다. Phase 3은 스플릿뷰 Escape Room 정책에 맞춰 구간 단위로 쪼갤지 하나의 긴 룸으로 둘지 결정이 필요하다.

## 구현 작업 체크리스트

1. 완료: 스키마 기준 확정. DSD §4.2 우선, 스프린트 이슈 필드 의미 포함.
2. 완료: `CausalRuleSO`에 조건/효과용 `[Serializable]` nested type 추가.
3. 완료: `EnemyTuningSO`에 `moveSpeed`, `patrolWaitTime` 추가하고 기존 `sightDistance`, `fovDegrees` 명칭 유지.
4. 완료: `ItemDefinitionSO`에 `Sprite icon` 추가.
5. 완료: `RoomGraphSO.Timeline`을 string에서 `TimelineRole`로 변경.
6. 완료: public getter 누락 필드 보강.
7. 남음: `Assets/_Project/Data` 하위에 SO 종류별 폴더 생성. 실제 에셋을 만들 때만 생성한다.
8. 남음: Unity 에디터에서 4종 빈 에셋 생성 가능 여부 확인.
9. 후속: ID 중복 검사용 Editor 유틸리티는 별도 작업으로 분리한다.

## 검증 기준

- `dotnet build Caretaker/Caretaker.sln` 오류 0개 유지.
- Unity Inspector에서 4종 SO 에셋 생성 및 필드 편집 가능.
- Team B가 입력할 `ruleId`, `itemId`, `roomId`가 DSD §4.5 ID 규칙을 따른다.
- `CausalRuleSO`는 Future 리시버 상세를 Past 쪽 공개 데이터로 오해하지 않도록 네트워크 메시지와 분리한다.

## 현재 기준선

- `dotnet build Caretaker/Caretaker.sln`: 성공.
- 경고 3개: Netcode `RequireOwnership` deprecation 2건, `RoomManager.OnRoomChanged` 미사용 1건.
- SO 스키마 코드 구현 완료 후에도 오류 0개를 유지한다.
