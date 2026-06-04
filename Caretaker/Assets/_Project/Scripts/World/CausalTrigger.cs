using UnityEngine;

namespace Caretaker.World
{
    /// <summary>
    /// Past 월드의 클릭/사용 대상. Trigger ID와 상호작용 타입을 가진다.
    /// 플레이어 상호작용을 감지하고 CausalityManager에 트리거 요청을 전달한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Unity Component
    ///
    /// 사용법:
    /// 1. InteractableObject와 같은 GameObject에 부착한다.
    /// 2. Inspector에서 _causalRule에 CausalRuleSO를 드래그하면 triggerId가 자동 설정된다.
    /// 3. InteractableObject.RunOperate() 시 자동으로 Fire()가 호출된다.
    /// </remarks>
    public class CausalTrigger : MonoBehaviour
    {
        [Header("Causal Rule")]
        [Tooltip("CausalRuleSO를 드래그하면 triggerId가 자동 설정됩니다.")]
        [SerializeField] private CausalRuleSO _causalRule;

        [Header("Resolved ID")]
        [Tooltip("CausalRuleSO에서 자동 추출된 Trigger ID입니다.")]
        [SerializeField] private string _triggerId;

        private CausalityManager _causalityManager;

        /// <summary>이 트리거의 고유 식별자. (예: CR_P1_POWER_LEVER)</summary>
        public string TriggerId => _triggerId;

        /// <summary>연결된 CausalRuleSO 에셋.</summary>
        public CausalRuleSO CausalRule => _causalRule;

        private void Awake()
        {
            CacheCausalityManager();
        }

        private void OnValidate()
        {
            if (_causalRule != null)
            {
                _triggerId = _causalRule.TriggerId;
            }

            if (_triggerId != null)
            {
                _triggerId = _triggerId.Trim();
            }
        }

        /// <summary>
        /// InteractableObject.RunOperate()에서 호출된다.
        /// CausalityManager에 ServerRpc 트리거 요청을 전송한다.
        /// </summary>
        public void Fire()
        {
            CacheCausalityManager();

            if (_causalityManager == null)
            {
                Debug.LogError(
                    $"CausalityManager not found. Cannot fire trigger '{_triggerId}'.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(_triggerId))
            {
                Debug.LogWarning("CausalTrigger has empty triggerId.", this);
                return;
            }

            _causalityManager.SubmitTriggerServerRpc(_triggerId);
        }

        private void CacheCausalityManager()
        {
            if (_causalityManager == null)
            {
                _causalityManager = FindAnyObjectByType<CausalityManager>();
            }
        }
    }
}
