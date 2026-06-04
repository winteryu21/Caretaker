using System;
using System.Collections.Generic;

using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 인과 조건 검증, 규칙 실행, 완료 상태 계산을 담당한다.
    /// MonoBehaviour에 의존하지 않는 순수 C# 클래스.
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Domain Service
    /// </remarks>
    public class CausalityService
    {
        private Dictionary<string, CausalRuleSO> _rulesByTriggerId;
        private CausalityState _state;

        /// <summary>
        /// CausalRuleSO 목록으로 TriggerId → Rule 매핑을 구축하고 상태를 초기화한다.
        /// </summary>
        /// <param name="rules">프로젝트에 존재하는 전체 인과 규칙 목록.</param>
        public void Initialize(CausalRuleSO[] rules)
        {
            _rulesByTriggerId = new Dictionary<string, CausalRuleSO>();
            _state = new CausalityState();

            if (rules == null)
            {
                return;
            }

            for (int i = 0; i < rules.Length; i++)
            {
                CausalRuleSO rule = rules[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.TriggerId))
                {
                    continue;
                }

                if (_rulesByTriggerId.ContainsKey(rule.TriggerId))
                {
                    UnityEngine.Debug.LogWarning(
                        $"Duplicate triggerId '{rule.TriggerId}' found. Rule '{rule.RuleId}' will be ignored.");
                    continue;
                }

                _rulesByTriggerId[rule.TriggerId] = rule;
            }
        }

        /// <summary>
        /// 트리거 요청을 검증하고 규칙을 실행한다.
        /// 거리, 역할, 페이즈, 필요 아이템 조건을 검증한다.
        /// </summary>
        /// <param name="triggerId">활성화된 트리거 ID.</param>
        /// <param name="actorRole">요청자의 시간대 역할.</param>
        /// <param name="ownedItemIds">요청자 소지 아이템 ID 목록.</param>
        /// <returns>인과 처리 결과.</returns>
        public CausalResult SubmitTrigger(string triggerId, TimelineRole actorRole, IReadOnlyList<string> ownedItemIds)
        {
            // 1. Rule 조회
            if (_rulesByTriggerId == null ||
                !_rulesByTriggerId.TryGetValue(triggerId, out CausalRuleSO rule))
            {
                return Reject($"Unknown trigger ID: {triggerId}");
            }

            // 2. 중복 실행 방지 (idempotent)
            if (_state.AppliedRuleIds.Contains(rule.RuleId))
            {
                return Reject($"Rule already applied: {rule.RuleId}");
            }

            // 3. 역할 검증 — Past 전용 트리거를 Future가 시도하는 경우 등
            if (actorRole != rule.RequiredRole)
            {
                return Reject(
                    $"Role mismatch: required={rule.RequiredRole}, actual={actorRole}");
            }

            // 4. 필요 아이템 검증
            if (!string.IsNullOrWhiteSpace(rule.RequiredItemId))
            {
                if (ownedItemIds == null || !ContainsItem(ownedItemIds, rule.RequiredItemId))
                {
                    return Reject(
                        $"Missing required item: {rule.RequiredItemId}");
                }
            }

            // 5. 추가 조건 검증 (CausalRuleCondition)
            if (rule.Conditions != null)
            {
                for (int i = 0; i < rule.Conditions.Length; i++)
                {
                    CausalRuleSO.CausalRuleCondition condition = rule.Conditions[i];
                    if (!EvaluateCondition(condition))
                    {
                        return Reject(
                            $"Condition not met: {condition.ConditionKey}={condition.ExpectedValue}");
                    }
                }
            }

            // 6. 성공 — ReceiverEffect 배열 생성
            CausalResult.ReceiverEffect[] effects = BuildReceiverEffects(rule);

            // 7. 상태 기록
            _state.AppliedRuleIds.Add(rule.RuleId);

            if (rule.IsMajor)
            {
                _state.CompletedMajorIds.Add(rule.RuleId);
            }

            return new CausalResult
            {
                RuleId = rule.RuleId,
                Effects = effects,
                IsMajorProgress = rule.IsMajor,
                Success = true,
                RejectionReason = null
            };
        }

        /// <summary>
        /// 주어진 Phase의 Major Interaction 완료 여부를 조회한다.
        /// </summary>
        /// <param name="phaseId">조회 대상 Phase.</param>
        /// <param name="requiredMajorIds">Phase 완료에 필요한 Major Rule ID 목록.</param>
        /// <returns>모든 Major가 완료되었으면 true.</returns>
        public bool QueryMajorProgress(PhaseId phaseId, IReadOnlyList<string> requiredMajorIds)
        {
            if (requiredMajorIds == null || requiredMajorIds.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < requiredMajorIds.Count; i++)
            {
                if (!_state.CompletedMajorIds.Contains(requiredMajorIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 특정 조건 키를 수동으로 충족 상태로 설정한다.
        /// 무전기를 통한 정보 교환 결과를 시스템에 반영할 때 사용한다.
        /// </summary>
        /// <param name="conditionKey">충족시킬 조건 키.</param>
        /// <param name="value">설정할 값.</param>
        public void SetConditionState(string conditionKey, string value)
        {
            if (_state == null)
            {
                return;
            }

            // conditionKey=value 형태로 AppliedRuleIds에 기록하여 조건 검증에 활용
            string conditionEntry = FormatConditionEntry(conditionKey, value);

            if (!_state.AppliedRuleIds.Contains(conditionEntry))
            {
                _state.AppliedRuleIds.Add(conditionEntry);
            }
        }

        /// <summary>
        /// 체크포인트 복원을 위해 인과 상태를 스냅샷으로 교체한다.
        /// </summary>
        /// <param name="snapshot">복원할 인과 상태. null이면 초기 상태로 리셋한다.</param>
        public void ResetToState(CausalityState snapshot)
        {
            if (snapshot == null)
            {
                _state = new CausalityState();
                return;
            }

            // 깊은 복사로 외부 참조와 분리
            _state = new CausalityState
            {
                AppliedRuleIds = new List<string>(snapshot.AppliedRuleIds),
                CompletedMajorIds = new List<string>(snapshot.CompletedMajorIds)
            };
        }

        /// <summary>
        /// 현재 인과 상태의 스냅샷을 반환한다.
        /// 체크포인트 생성 시 사용한다.
        /// </summary>
        /// <returns>현재 상태의 깊은 복사본.</returns>
        public CausalityState GetCurrentState()
        {
            if (_state == null)
            {
                return new CausalityState();
            }

            return new CausalityState
            {
                AppliedRuleIds = new List<string>(_state.AppliedRuleIds),
                CompletedMajorIds = new List<string>(_state.CompletedMajorIds)
            };
        }

        // ── private ──

        private static CausalResult.ReceiverEffect[] BuildReceiverEffects(CausalRuleSO rule)
        {
            if (rule.ReceiverEffects == null || rule.ReceiverEffects.Length == 0)
            {
                return Array.Empty<CausalResult.ReceiverEffect>();
            }

            CausalResult.ReceiverEffect[] effects =
                new CausalResult.ReceiverEffect[rule.ReceiverEffects.Length];

            for (int i = 0; i < rule.ReceiverEffects.Length; i++)
            {
                CausalRuleSO.CausalReceiverEffect source = rule.ReceiverEffects[i];
                effects[i] = new CausalResult.ReceiverEffect
                {
                    ReceiverId = source.ReceiverId,
                    StateKey = source.StateKey,
                    StateValue = source.StateValue
                };
            }

            return effects;
        }

        private bool EvaluateCondition(CausalRuleSO.CausalRuleCondition condition)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.ConditionKey))
            {
                return true;
            }

            // 조건 충족 여부를 AppliedRuleIds에서 "conditionKey=expectedValue" 형태로 확인
            string conditionEntry = FormatConditionEntry(condition.ConditionKey, condition.ExpectedValue);
            return _state.AppliedRuleIds.Contains(conditionEntry);
        }

        private static string FormatConditionEntry(string key, string value)
        {
            return $"COND:{key}={value}";
        }

        private static bool ContainsItem(IReadOnlyList<string> items, string itemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static CausalResult Reject(string reason)
        {
            return new CausalResult
            {
                RuleId = null,
                Effects = null,
                IsMajorProgress = false,
                Success = false,
                RejectionReason = reason
            };
        }
    }
}
