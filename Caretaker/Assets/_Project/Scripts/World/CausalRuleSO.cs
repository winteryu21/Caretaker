using System;

using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 인과 규칙을 정의하는 데이터 에셋.
    /// Trigger, 조건, Receiver 결과, Major/Minor 구분을 포함한다.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_CausalRule", menuName = "Caretaker SO/Causality/CausalRule")]
    public class CausalRuleSO : ScriptableObject
    {
        [SerializeField] private string _ruleId;
        [SerializeField] private string _triggerId;
        [SerializeField] private TimelineRole _requiredRole = TimelineRole.Past;
        [SerializeField] private string _requiredItemId;
        [SerializeField] private CausalRuleCondition[] _conditions = Array.Empty<CausalRuleCondition>();
        [SerializeField] private CausalReceiverEffect[] _receiverEffects = Array.Empty<CausalReceiverEffect>();
        [SerializeField] private InteractionWeight _interactionWeight = InteractionWeight.Minor;

        /// <summary>
        /// 인과 규칙이 진행에 미치는 가중치를 정의한다.
        /// </summary>
        public enum InteractionWeight
        {
            /// <summary>진행 보조 또는 이동 경로 개방용 인과.</summary>
            Minor = 0,

            /// <summary>Major Interaction 완료로 이어지는 핵심 인과.</summary>
            Major = 1
        }

        /// <summary>
        /// 아이템 외 퍼즐 상태 조건을 정의한다.
        /// </summary>
        [Serializable]
        public class CausalRuleCondition
        {
            [SerializeField] private string _conditionKey;
            [SerializeField] private string _expectedValue;

            /// <summary>검증할 조건 키.</summary>
            public string ConditionKey => _conditionKey;

            /// <summary>조건 충족으로 인정할 값.</summary>
            public string ExpectedValue => _expectedValue;
        }

        /// <summary>
        /// 인과 규칙 실행 시 Receiver에 적용할 상태 변경을 정의한다.
        /// </summary>
        [Serializable]
        public class CausalReceiverEffect
        {
            [SerializeField] private string _receiverId;
            [SerializeField] private string _stateKey;
            [SerializeField] private string _stateValue;

            /// <summary>결과를 적용할 리시버 ID.</summary>
            public string ReceiverId => _receiverId;

            /// <summary>리시버에 적용할 상태 키.</summary>
            public string StateKey => _stateKey;

            /// <summary>리시버에 적용할 상태 값.</summary>
            public string StateValue => _stateValue;
        }

        /// <summary>규칙 고유 식별자. (예: CR_P1_POWER_LEVER)</summary>
        public string RuleId => _ruleId;

        /// <summary>이 규칙을 활성화하는 트리거 ID.</summary>
        public string TriggerId => _triggerId;

        /// <summary>이 규칙을 실행할 수 있는 시간대 역할.</summary>
        public TimelineRole RequiredRole => _requiredRole;

        /// <summary>필요 아이템 ID. 빈 문자열이면 아이템 불필요.</summary>
        public string RequiredItemId => _requiredItemId;

        /// <summary>아이템 외 퍼즐 상태 조건 목록.</summary>
        public CausalRuleCondition[] Conditions => _conditions;

        /// <summary>Receiver에 적용할 상태 변경 목록.</summary>
        public CausalReceiverEffect[] ReceiverEffects => _receiverEffects;

        /// <summary>규칙의 진행 가중치.</summary>
        public InteractionWeight Weight => _interactionWeight;

        /// <summary>Major Interaction 여부.</summary>
        public bool IsMajor => _interactionWeight == InteractionWeight.Major;

        /// <summary>단일 효과 규칙에서 첫 번째 리시버 ID를 가져온다.</summary>
        public string ReceiverId => _receiverEffects.Length > 0 ? _receiverEffects[0].ReceiverId : string.Empty;

        /// <summary>단일 효과 규칙에서 첫 번째 상태 키를 가져온다.</summary>
        public string ReceiverStateKey => _receiverEffects.Length > 0 ? _receiverEffects[0].StateKey : string.Empty;

        /// <summary>단일 효과 규칙에서 첫 번째 상태 값을 가져온다.</summary>
        public string ReceiverStateValue => _receiverEffects.Length > 0 ? _receiverEffects[0].StateValue : string.Empty;
    }
}
