using UnityEngine;
using UnityEngine.Events;

namespace Caretaker.World
{
    /// <summary>
    /// Future 월드의 변경 대상. Receiver ID와 상태 적용 어댑터를 가진다.
    /// 인과 규칙 실행 결과를 받아 오브젝트 상태를 변경한다. (문 열림, 전원 공급 등)
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Unity Component
    ///
    /// 사용법:
    /// 1. Inspector에서 _receiverId를 CausalRuleSO의 receiverEffects[].receiverId와 동일하게 설정한다.
    /// 2. _onActivated / _onDeactivated UnityEvent에 Animator, Light2D, Collider2D 등을 연결한다.
    /// 3. CausalityManager가 Phase 씬 로드 시 자동으로 레지스트리에 등록한다.
    /// </remarks>
    public class CausalReceiver : MonoBehaviour
    {
        private static readonly Color DEBUG_COLOR_ACTIVATED = Color.green;
        private static readonly Color DEBUG_COLOR_DEACTIVATED = Color.red;

        [SerializeField] private string _receiverId;

        [Header("State Change Events")]
        [Tooltip("인과 결과로 활성화될 때 호출됩니다. (문 열림, 전력 공급 등)")]
        [SerializeField] private UnityEvent _onActivated;

        [Tooltip("체크포인트 복원 등으로 비활성화될 때 호출됩니다.")]
        [SerializeField] private UnityEvent _onDeactivated;

        [Header("Debug")]
        [Tooltip("SpriteRenderer가 있으면 활성화/비활성화 시 색상을 자동 변경합니다.")]
        [SerializeField] private bool _debugColorFeedback = true;

        private SpriteRenderer _spriteRenderer;
        private bool _isActivated;
        private string _lastStateKey;
        private string _lastStateValue;

        /// <summary>이 리시버의 고유 식별자. (예: RCV_P1_BREAKER_PANEL)</summary>
        public string ReceiverId => _receiverId;

        /// <summary>현재 활성화 상태.</summary>
        public bool IsActivated => _isActivated;

        /// <summary>마지막으로 적용된 상태 키.</summary>
        public string LastStateKey => _lastStateKey;

        /// <summary>마지막으로 적용된 상태 값.</summary>
        public string LastStateValue => _lastStateValue;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_debugColorFeedback && _spriteRenderer != null)
            {
                _spriteRenderer.color = DEBUG_COLOR_DEACTIVATED;
            }
        }

        private void OnEnable()
        {
            RegisterSelf();
        }

        private void OnDisable()
        {
            UnregisterSelf();
        }

        private void OnValidate()
        {
            if (_receiverId != null)
            {
                _receiverId = _receiverId.Trim();
            }
        }

        /// <summary>
        /// 인과 결과에 따른 상태를 적용한다.
        /// CausalityManager의 ClientRpc에서 호출된다.
        /// </summary>
        /// <param name="stateKey">변경할 상태 키. (예: powerState, lockState)</param>
        /// <param name="stateValue">적용할 상태 값. (예: Powered, Unlocked)</param>
        public void ApplyState(string stateKey, string stateValue)
        {
            _lastStateKey = stateKey;
            _lastStateValue = stateValue;

            Debug.Log(
                $"CausalReceiver '{_receiverId}': {stateKey}={stateValue}", this);

            // 상태 값이 비어 있지 않으면 활성화로 판단
            bool shouldActivate = !string.IsNullOrWhiteSpace(stateValue)
                && stateValue != "Off"
                && stateValue != "Locked"
                && stateValue != "false"
                && stateValue != "Inactive";

            SetActivated(shouldActivate);
        }

        /// <summary>
        /// 체크포인트 복원 시 상태를 초기화한다.
        /// </summary>
        public void ResetState()
        {
            _lastStateKey = null;
            _lastStateValue = null;
            SetActivated(false);
        }

        private void SetActivated(bool activated)
        {
            if (_isActivated == activated)
            {
                return;
            }

            _isActivated = activated;

            // 디버그: SpriteRenderer 색상 자동 변경 (빨강 → 초록)
            if (_debugColorFeedback && _spriteRenderer != null)
            {
                _spriteRenderer.color = activated
                    ? DEBUG_COLOR_ACTIVATED
                    : DEBUG_COLOR_DEACTIVATED;
            }

            if (activated)
            {
                _onActivated?.Invoke();
            }
            else
            {
                _onDeactivated?.Invoke();
            }
        }

        private void RegisterSelf()
        {
            CausalityManager manager = FindAnyObjectByType<CausalityManager>();
            if (manager != null)
            {
                manager.RegisterReceiver(this);
            }
        }

        private void UnregisterSelf()
        {
            CausalityManager manager = FindAnyObjectByType<CausalityManager>();
            if (manager != null)
            {
                manager.UnregisterReceiver(this);
            }
        }
    }
}
