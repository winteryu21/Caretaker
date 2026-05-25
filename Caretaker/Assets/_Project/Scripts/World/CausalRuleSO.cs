using UnityEngine;

namespace Caretaker.World
{
    /// <summary>
    /// 인과 규칙을 정의하는 데이터 에셋.
    /// Trigger, 조건, Receiver 결과, Major/Minor 구분을 포함한다.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_CausalRule", menuName = "Caretaker/Causality/CausalRule")]
    public class CausalRuleSO : ScriptableObject
    {
        [SerializeField] private string _ruleId;
        [SerializeField] private string _triggerId;
        [SerializeField] private string _requiredRole;
        [SerializeField] private string _requiredItemId;
        [SerializeField] private string _receiverId;
        [SerializeField] private string _receiverStateKey;
        [SerializeField] private string _receiverStateValue;
        [SerializeField] private bool _isMajor;

        /// <summary>규칙 고유 식별자. (예: CR_P1_POWER_LEVER)</summary>
        public string RuleId => _ruleId;

        /// <summary>이 규칙을 활성화하는 트리거 ID.</summary>
        public string TriggerId => _triggerId;

        /// <summary>필요 아이템 ID. 빈 문자열이면 아이템 불필요.</summary>
        public string RequiredItemId => _requiredItemId;

        /// <summary>결과를 적용할 리시버 ID.</summary>
        public string ReceiverId => _receiverId;

        /// <summary>Major Interaction 여부.</summary>
        public bool IsMajor => _isMajor;
    }
}
