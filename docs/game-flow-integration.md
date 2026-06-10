# GameFlowManager Integration

퍼즐, 맵 트리거, 탈출 트리거가 `GameFlowManager`에 진행 정보를 전달하는 방법 정리.

`GameFlowManager`는 Phase 진행 상태의 Host-authoritative 중심으로. 퍼즐이나 맵 오브젝트는 완료 사실만 보고하고, Phase 전환, Objective 갱신, 결과 전파는 `GameFlowManager`가 처리한다.

## 기본 원칙

- 진행 상태 변경은 Host에서만.
- 퍼즐/맵 시스템은 정보 전달 여부를 추적하지 않고 완료 이벤트만 전달한다.
- `GameFlowManager`에 직접 상태를 쓰지 말고 public method로 호출.
- Client 전용 오브젝트에서 완료가 발생하면 Host로 RPC를 보낸 뒤 Host에서 `GameFlowManager`를 호출.
- HUD는 `GameFlowManager` 이벤트 또는 getter를 읽는다. 퍼즐이 HUD를 직접 갱신하지 않음.

## Major 완료 보고

Major Interaction 완료 시 호출할 API:

```csharp
gameFlowManager.NotifyMajorComplete(phaseId, majorId);
```

이렇게 호출하면 됩니다:

```csharp
using Caretaker.Core;
using Caretaker.Shared;
using UnityEngine;

public sealed class ExamplePuzzleCompletion : MonoBehaviour
{
    [SerializeField] private GameFlowManager _gameFlowManager;
    [SerializeField] private PhaseId _phaseId = PhaseId.Phase1;
    [SerializeField] private MajorId _majorId = MajorId.M1;

    public void CompletePuzzle()
    {
        _gameFlowManager.NotifyMajorComplete(_phaseId, _majorId);
    }
}
```

현재 Major ID 매핑:

| MajorId | 의미 | Phase 전환 영향 |
| --- | --- | --- |
| `M1` | Phase 1 첫 번째 Major | 기록 및 Objective 갱신 |
| `M2` | Phase 1 전환 Major | 완료 시 Phase 2 진입 |
| `M3` | Phase 2 첫 번째 Major | 기록 및 Objective 갱신 |
| `M4` | Phase 2 전환 Major | 완료 시 Phase 3 진입 |

`NotifyMajorComplete`는 같은 Major를 중복 호출해도 한 번만 처리합니다!

## CausalRule 기반 퍼즐

`CausalTrigger` -> `CausalityManager` -> `CausalityService` 경로를 사용하는 퍼즐은 별도 GameFlow 호출을 추가하지 않는다.

`CausalityManager`가 규칙 성공 후 `result.IsMajorProgress`가 true이면 자동으로 호출한다.

```csharp
_gameFlowManager.NotifyMajorComplete(
    _gameFlowManager.CurrentPhase,
    result.RuleId);
```

`result.RuleId`는 `MajorIdUtility.TryResolve`로 Major ID로 변환된다.

현재 지원되는 CausalRule ID:

| Rule ID | MajorId |
| --- | --- |
| `CR_P1_POWER_PANEL` | `M1` |
| `CR_P1_SEC_HACK` | `M2` |
| `CR_P2_BLUEPRINT_ID` | `M3` |
| `CR_P2_VIRUS_PLANT` | `M4` |

새 Major CausalRule을 추가하면 `MajorIdUtility.TryResolve` 매핑도 수정해 줘야 합니다.

## 순수 퍼즐 또는 맵 트리거

Causality를 거치지 않는 순수 퍼즐, 문, 지역 진입 트리거는 완료 시 Host에서 직접 `NotifyMajorComplete`를 호출한다.

Client에서 완료 판정이 발생하는 경우:

1. Client는 ServerRpc로 완료 요청을 보낸다.
2. Host는 조건을 재검증한다.
3. Host가 `NotifyMajorComplete(CurrentPhase, MajorId.X)`를 호출한다.

Client에서 바로 `NotifyMajorComplete`를 호출해도 Host가 아니면 무시된다.

## Phase 3 출구 도달 보고

Phase 3에서 각 시간대 플레이어가 출구에 도달하면 호출한다.

```csharp
gameFlowManager.ReportExitReached(timelineRole);
```

예시:

```csharp
using Caretaker.Core;
using Caretaker.Shared;
using UnityEngine;

public sealed class ExitTrigger : MonoBehaviour
{
    [SerializeField] private GameFlowManager _gameFlowManager;
    [SerializeField] private TimelineRole _timelineRole;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _gameFlowManager.ReportExitReached(_timelineRole);
    }
}
```

`Past`와 `Future`가 모두 보고되면 `GameResult.EscapeSuccess`가 확정되고 모든 클라이언트에 전파된다.

## HUD와 외부 시스템이 읽는 값

읽기 전용 상태:

```csharp
gameFlowManager.CurrentPhase
gameFlowManager.CompletedMajorIds
gameFlowManager.CurrentGameResult
gameFlowManager.PastExitReached
gameFlowManager.FutureExitReached
```

Objective 조회:

```csharp
ObjectiveId objectiveId = gameFlowManager.GetCurrentObjective(localRole);

if (gameFlowManager.TryGetCurrentObjectiveDisplayText(localRole, out string text))
{
    // HUD 표시
}
```

구독 가능한 이벤트:

```csharp
gameFlowManager.OnPhaseChanged += HandlePhaseChanged;
gameFlowManager.OnMajorCompleted += HandleMajorCompleted;
gameFlowManager.OnObjectiveChanged += HandleObjectiveChanged;
gameFlowManager.OnGameResult += HandleGameResult;
```

구독은 `OnEnable`, 해제는 `OnDisable`에서 처리한다.

## 설정 파일

Phase 전환과 Objective 문구는 `GameFlowDefinitionSO`가 정의한다.

기본 위치:

```text
Assets/_Project/Data/Core/SO_GameFlowDefinition.asset
```

새 Objective가 필요하면 다음을 함께 갱신한다.

- `Assets/_Project/Scripts/Shared/ObjectiveId.cs`
- `SO_GameFlowDefinition.asset`의 Objectives 배열
- 필요하면 HUD 표시 문구

새 Major ID 또는 CausalRule 매핑이 필요하면 다음을 갱신한다.

- `Assets/_Project/Scripts/Shared/MajorId.cs`
- `SO_GameFlowDefinition.asset`의 Phase transition 설정
- Major CausalRule 사용 시 `CausalRuleSO.Interaction Weight = Major`

## 확인 절차

1. Host와 Client가 로비에서 역할을 선택하고 게임에 진입한다.
2. Host Console에서 Major 완료 로그를 확인한다.

```text
Major Interaction completed: phase=Phase1, major=M1
```

3. Objective 변경 로그가 Host와 Client 양쪽에 출력되는지 확인한다.
4. `M2` 완료 시 Phase 2로 전환되는지 확인한다.
5. `M4` 완료 시 Phase 3로 전환되는지 확인한다.
6. Phase 3에서는 양쪽 출구 보고 후 결과 이벤트가 발생하는지 확인한다.

## 주의사항

- `TransitionPhase`는 일반 퍼즐 코드에서 직접 호출하지 마세요. 디버그나 특수 운영 코드로만 사용할 것.
- `CompletedMajorIds` 컬렉션을 외부에서 수정하지 마세요 .
- Phase ID는 현재 `GameFlowManager.CurrentPhase`와 일치해야 한다. 다른 Phase에서 온 완료 보고는 무시.
- `CausalRuleSO`가 Major로 설정되어 있으면 퍼즐 코드에서 중복으로 `NotifyMajorComplete`를 호출하지 않아야 합니다.
