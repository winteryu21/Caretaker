using Caretaker.Gameplay;
using Caretaker.Shared;
using TMPro;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// HUD 상태를 ViewModel 기반으로 렌더링한다.
    /// 정보 격리 필터를 적용하여 허용된 정보만 표시한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.10 — UI / HUD 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class HudPresenter : MonoBehaviour
    {
        [Header("Shell")]
        [SerializeField] private GameObject _hudRoot;
        [SerializeField] private Transform _objectiveArea;
        [SerializeField] private Transform _inventoryArea;
        [SerializeField] private Transform _interactionPromptArea;
        [SerializeField] private Transform _modalOverlayArea;
        [SerializeField] private Transform _statusArea;

        [Header("Objective")]
        [SerializeField] private TMP_Text _objectiveText;
        [SerializeField] private GameObject _objectiveRoot;

        [Header("Interaction Prompt")]
        [SerializeField] private InteractionPromptPresenter _interactionPromptPresenter;

        [Header("Causality")]
        [SerializeField] private CausalityIndicatorPresenter _causalityIndicatorPresenter;

        [Header("Radio")]
        [SerializeField] private TMP_Text _radioStatusText;
        [SerializeField] private GameObject _radioActiveIndicator;

        [Header("Control Mode")]
        [SerializeField] private TMP_Text _controlModeText;
        [SerializeField] private GameObject _normalModeIndicator;
        [SerializeField] private GameObject _combatModeIndicator;

        private string _objective = string.Empty;
        private PlayerControlMode _controlMode = PlayerControlMode.Normal;
        private string _controlModeStatus = "Mode: Investigation";
        private RadioState _radioState = RadioState.Idle;
        private ulong _radioTalkerId = RadioNetworkBridge.NO_TALKER_ID;
        private string _radioStatus = "Waiting for Radio";
        private bool _modalOverlayVisible;

        /// <summary>HUD 전체 루트.</summary>
        public GameObject HudRoot => _hudRoot;

        /// <summary>Objective UI가 배치될 영역.</summary>
        public Transform ObjectiveArea => _objectiveArea;

        /// <summary>Inventory HUD가 배치될 영역.</summary>
        public Transform InventoryArea => _inventoryArea;

        /// <summary>상호작용 프롬프트가 배치될 영역.</summary>
        public Transform InteractionPromptArea => _interactionPromptArea;

        /// <summary>Examine/퍼즐 등 modal UI가 배치될 영역.</summary>
        public Transform ModalOverlayArea => _modalOverlayArea;

        /// <summary>무전기/상태 표시가 배치될 영역.</summary>
        public Transform StatusArea => _statusArea;

        /// <summary>현재 HUD에 표시 중인 목표 문구.</summary>
        public string Objective => _objective;

        /// <summary>modal overlay 표시 여부.</summary>
        public bool IsModalOverlayVisible => _modalOverlayVisible;

        /// <summary>
        /// 현재 HUD에 표시 중인 플레이어 조작 모드 문구를 반환한다.
        /// </summary>
        public string ControlModeStatus => _controlModeStatus;

        /// <summary>
        /// 현재 HUD에 표시 중인 무전기 상태 문구를 반환한다.
        /// </summary>
        public string RadioStatus => _radioStatus;

        private void Awake()
        {
            ResolveDefaultReferences();
            RenderObjective();
            RenderModalOverlay();
            RenderControlMode();
            RenderRadioState();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDefaultReferences();
        }
#endif

        /// <summary>
        /// HUD 전체 표시 여부를 설정한다.
        /// </summary>
        public void SetHudVisible(bool isVisible)
        {
            if (_hudRoot != null)
            {
                _hudRoot.SetActive(isVisible);
            }
        }

        /// <summary>
        /// 현재 목표 문구를 표시한다.
        /// </summary>
        public void SetObjective(string objective)
        {
            _objective = objective ?? string.Empty;
            RenderObjective();
        }

        /// <summary>
        /// 현재 목표 문구를 비우고 숨긴다.
        /// </summary>
        public void ClearObjective()
        {
            SetObjective(string.Empty);
        }

        /// <summary>
        /// 상호작용 프롬프트를 표시한다.
        /// </summary>
        public void ShowInteractionPrompt(string promptText)
        {
            if (_interactionPromptPresenter != null)
            {
                _interactionPromptPresenter.ShowPrompt(promptText);
            }
        }

        /// <summary>
        /// 상호작용 프롬프트를 숨긴다.
        /// </summary>
        public void HideInteractionPrompt()
        {
            if (_interactionPromptPresenter != null)
            {
                _interactionPromptPresenter.HidePrompt();
            }
        }

        /// <summary>
        /// modal overlay 영역을 켜거나 끈다.
        /// </summary>
        public void SetModalOverlayVisible(bool isVisible)
        {
            _modalOverlayVisible = isVisible;
            RenderModalOverlay();
        }

        /// <summary>
        /// 인과 변경 Pulse를 표시한다.
        /// </summary>
        public void ShowCausalityPulse()
        {
            if (_causalityIndicatorPresenter != null)
            {
                _causalityIndicatorPresenter.ShowCausalityPulse();
            }
        }

        /// <summary>
        /// 현재 플레이어 조작 모드를 HUD에 표시한다.
        /// </summary>
        /// <param name="controlMode">로컬 플레이어의 현재 입력 해석 모드.</param>
        public void SetControlMode(PlayerControlMode controlMode)
        {
            _controlMode = controlMode;
            _controlModeStatus = BuildControlModeStatusText(controlMode);
            RenderControlMode();
        }

        /// <summary>
        /// 무전기 상태를 HUD에 표시한다.
        /// </summary>
        /// <param name="state">로컬 플레이어 기준 무전기 상태.</param>
        /// <param name="talkerId">현재 송신권 보유자 ID.</param>
        public void SetRadioState(RadioState state, ulong talkerId)
        {
            _radioState = state;
            _radioTalkerId = talkerId;
            _radioStatus = BuildRadioStatusText(state, talkerId);
            RenderRadioState();
        }

        private void ResolveDefaultReferences()
        {
            if (_hudRoot == null)
            {
                _hudRoot = gameObject;
            }

            if (_interactionPromptPresenter == null)
            {
                _interactionPromptPresenter = GetComponentInChildren<InteractionPromptPresenter>(true);
            }

            if (_causalityIndicatorPresenter == null)
            {
                _causalityIndicatorPresenter = GetComponentInChildren<CausalityIndicatorPresenter>(true);
            }
        }

        private void RenderObjective()
        {
            bool hasObjective = !string.IsNullOrWhiteSpace(_objective);

            if (_objectiveText != null)
            {
                _objectiveText.text = _objective;
            }

            if (_objectiveRoot != null)
            {
                _objectiveRoot.SetActive(hasObjective);
            }
        }

        private void RenderModalOverlay()
        {
            if (_modalOverlayArea != null)
            {
                _modalOverlayArea.gameObject.SetActive(_modalOverlayVisible);
            }
        }

        private void RenderRadioState()
        {
            if (_radioStatusText != null)
            {
                _radioStatusText.text = _radioStatus;
            }

            if (_radioActiveIndicator != null)
            {
                _radioActiveIndicator.SetActive(_radioState is RadioState.Transmitting or RadioState.Receiving);
            }
        }

        private void RenderControlMode()
        {
            bool isCombat = _controlMode == PlayerControlMode.Combat;

            if (_controlModeText != null)
            {
                _controlModeText.text = _controlModeStatus;
            }

            if (_normalModeIndicator != null)
            {
                _normalModeIndicator.SetActive(!isCombat);
            }

            if (_combatModeIndicator != null)
            {
                _combatModeIndicator.SetActive(isCombat);
            }
        }

        private static string BuildControlModeStatusText(PlayerControlMode controlMode)
        {
            return controlMode == PlayerControlMode.Combat
                ? "Mode: Combat"
                : "Mode: Investigation";
        }

        private static string BuildRadioStatusText(RadioState state, ulong talkerId)
        {
            return state switch
            {
                RadioState.Transmitting => "Radio Tx",
                RadioState.Receiving => "Radio Rx",
                RadioState.Blocked => talkerId == RadioNetworkBridge.NO_TALKER_ID
                    ? "Radio Can't use"
                    : "Radio Occupied",
                _ => "Radio Waiting"
            };
        }
    }
}
