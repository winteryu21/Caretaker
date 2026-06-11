using Caretaker.Core;
using Caretaker.Gameplay;
using Caretaker.Shared;
using Caretaker.World;
using UnityEngine;

namespace Caretaker.Presentation
{
    /// <summary>
    /// HUD Shell을 실제 런타임 시스템에 연결한다.
    /// </summary>
    /// <remarks>
    /// DEV-42의 범위는 최상위 HUD 연결 지점이다.
    /// Inventory 슬롯 UI, 상호작용 상세 UI, objective 상세 문구는 DEV-47/49/50에서
    /// 각 전용 Presenter가 이 컴포넌트 아래에 추가로 연결한다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class HudRuntimeBinder : MonoBehaviour
    {
        private const float RADIO_REBIND_INTERVAL_SECONDS = 0.5f;

        [Header("HUD")]
        [SerializeField] private HudPresenter _hudPresenter;
        [SerializeField] private InventoryPresenter _inventoryPresenter;

        [Header("Runtime Sources")]
        [SerializeField] private LocalWorldPlayerSpawner _playerSpawner;
        [SerializeField] private SessionRoleManager _roleManager;
        [SerializeField] private GameFlowManager _gameFlowManager;
        [SerializeField] private CausalityManager _causalityManager;
        [SerializeField] private RadioNetworkBridge _radioNetworkBridge;
        [SerializeField] private bool _autoFindDependencies = true;

        private GameObject _currentPlayer;
        private InteractionProbe _interactionProbe;
        private InventoryController _inventoryController;
        private PlayerInputReader _playerInputReader;
        private RadioNetworkBridge _subscribedRadioNetworkBridge;
        private string _lastPromptText = string.Empty;
        private float _nextRadioResolveTime;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            LocalWorldPlayerSpawner.OnCurrentPlayerChanged += HandleCurrentPlayerChanged;

            if (_roleManager != null)
            {
                _roleManager.OnLocalRoleAssigned += HandleLocalRoleAssigned;
            }

            if (_gameFlowManager != null)
            {
                _gameFlowManager.OnPhaseChanged += HandlePhaseChanged;
                _gameFlowManager.OnObjectiveChanged += HandleObjectiveChanged;
                RefreshObjectiveFromGameFlow();
            }

            if (_causalityManager != null)
            {
                _causalityManager.OnCausalityPulse += HandleCausalityPulse;
            }

            BindRadioNetworkBridge(_radioNetworkBridge);
            BindCurrentPlayer(_playerSpawner != null ? _playerSpawner.CurrentPlayer : null);
        }

        private void OnDisable()
        {
            LocalWorldPlayerSpawner.OnCurrentPlayerChanged -= HandleCurrentPlayerChanged;

            if (_roleManager != null)
            {
                _roleManager.OnLocalRoleAssigned -= HandleLocalRoleAssigned;
            }

            if (_gameFlowManager != null)
            {
                _gameFlowManager.OnPhaseChanged -= HandlePhaseChanged;
                _gameFlowManager.OnObjectiveChanged -= HandleObjectiveChanged;
            }

            if (_causalityManager != null)
            {
                _causalityManager.OnCausalityPulse -= HandleCausalityPulse;
            }

            UnbindRadioNetworkBridge();
            UnbindInventory();
            UnbindPlayerInput();
            _interactionProbe = null;
            _currentPlayer = null;
            _lastPromptText = string.Empty;
        }

        private void Update()
        {
            RefreshRadioBinding();
            RefreshInteractionPrompt();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_hudPresenter == null)
            {
                _hudPresenter = GetComponent<HudPresenter>();
            }

            if (_inventoryPresenter == null)
            {
                _inventoryPresenter = GetComponentInChildren<InventoryPresenter>(true);
            }
        }
#endif

        /// <summary>
        /// 상호작용 타입과 선택 아이템을 HUD 프롬프트 문구로 변환한다.
        /// </summary>
        public static string BuildPromptText(InteractionType interactionType, string selectedItemId)
        {
            return interactionType switch
            {
                InteractionType.Acquire => "LMB - Pick Up",
                InteractionType.Operate => "E - Operate",
                InteractionType.UseItem => string.IsNullOrWhiteSpace(selectedItemId)
                    ? "RMB - Use Item"
                    : $"RMB - Use {selectedItemId}",
                InteractionType.Examine => "LMB - Examine",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 전투 처형이 가능할 때 표시할 안내 문구를 생성합니다.
        /// </summary>
        public static string BuildTakedownPromptText()
        {
            return "F - Takedown";
        }

        /// <summary>
        /// 현재 hover/근접 상호작용 대상에서 가능한 입력 프롬프트를 구성한다.
        /// </summary>
        public static string BuildPromptText(
            InteractableObject hoverTarget,
            InteractableObject proximityTarget,
            string selectedItemId)
        {
            string hoverPrompt = BuildHoverPromptText(hoverTarget, selectedItemId);
            if (!string.IsNullOrWhiteSpace(hoverPrompt))
            {
                return hoverPrompt;
            }

            return BuildProximityPromptText(proximityTarget, selectedItemId);
        }

        /// <summary>
        /// 마우스로 올린 대상에서 가능한 입력 프롬프트를 구성한다.
        /// </summary>
        public static string BuildHoverPromptText(InteractableObject target, string selectedItemId)
        {
            if (target == null)
            {
                return string.Empty;
            }

            string primaryClickPrompt = string.Empty;
            if (target.IsInteractable(InteractionType.Acquire) &&
                !target.IsItemAcquired &&
                (!target.HasRequiredItem || target.IsRequiredItemSatisfied))
            {
                primaryClickPrompt = BuildPromptText(InteractionType.Acquire, selectedItemId);
            }
            else if (target.IsInteractable(InteractionType.Examine))
            {
                primaryClickPrompt = BuildPromptText(InteractionType.Examine, selectedItemId);
            }

            string secondaryPrompt = string.Empty;
            if (target.HasRequiredItem && !target.IsRequiredItemSatisfied)
            {
                secondaryPrompt = BuildPromptText(InteractionType.UseItem, selectedItemId);
            }
            if (string.IsNullOrWhiteSpace(primaryClickPrompt))
            {
                return secondaryPrompt;
            }

            if (string.IsNullOrWhiteSpace(secondaryPrompt))
            {
                return primaryClickPrompt;
            }

            return $"{primaryClickPrompt} | {secondaryPrompt}";
        }

        /// <summary>
        /// 접근한 map interaction 대상에서 가능한 입력 프롬프트를 구성한다.
        /// </summary>
        public static string BuildProximityPromptText(InteractableObject target, string selectedItemId)
        {
            if (target == null ||
                !target.IsInteractable(InteractionType.Operate) ||
                (target.HasRequiredItem && !target.IsRequiredItemSatisfied))
            {
                return string.Empty;
            }

            return BuildPromptText(InteractionType.Operate, selectedItemId);
        }

        /// <summary>
        /// 현재 Phase를 임시 objective 문구로 변환한다.
        /// </summary>
        public static string BuildObjectiveText(PhaseId phaseId)
        {
            return phaseId switch
            {
                PhaseId.Phase1 => "Objective: Restore facility power",
                PhaseId.Phase2 => "Objective: Identify the archive and sample",
                PhaseId.Phase3 => "Objective: Escape the facility",
                _ => "Objective: Stand by"
            };
        }

        private void ResolveDependencies()
        {
            if (_hudPresenter == null)
            {
                _hudPresenter = GetComponent<HudPresenter>();
            }

            if (_inventoryPresenter == null)
            {
                _inventoryPresenter = GetComponentInChildren<InventoryPresenter>(true);
            }

            if (!_autoFindDependencies)
            {
                return;
            }

            if (_hudPresenter == null)
            {
                _hudPresenter = FindAnyObjectByType<HudPresenter>();
            }

            if (_playerSpawner == null)
            {
                _playerSpawner = FindAnyObjectByType<LocalWorldPlayerSpawner>();
            }

            if (_gameFlowManager == null)
            {
                _gameFlowManager = FindAnyObjectByType<GameFlowManager>();
            }

            if (_roleManager == null)
            {
                _roleManager = FindAnyObjectByType<SessionRoleManager>();
            }

            if (_causalityManager == null)
            {
                _causalityManager = FindAnyObjectByType<CausalityManager>();
            }

            if (_radioNetworkBridge == null)
            {
                _radioNetworkBridge = FindAnyObjectByType<RadioNetworkBridge>();
            }
        }

        private void HandleCurrentPlayerChanged(GameObject currentPlayer)
        {
            BindCurrentPlayer(currentPlayer);
        }

        private void BindCurrentPlayer(GameObject currentPlayer)
        {
            if (_currentPlayer == currentPlayer)
            {
                return;
            }

            UnbindInventory();
            UnbindPlayerInput();

            _currentPlayer = currentPlayer;
            _interactionProbe = currentPlayer != null
                ? currentPlayer.GetComponent<InteractionProbe>()
                : null;
            _inventoryController = currentPlayer != null
                ? currentPlayer.GetComponent<InventoryController>()
                : null;
            _playerInputReader = currentPlayer != null
                ? currentPlayer.GetComponent<PlayerInputReader>()
                : null;

            if (_inventoryController != null)
            {
                _inventoryController.OnInventoryChanged += HandleInventoryChanged;
            }

            if (_inventoryPresenter != null)
            {
                _inventoryPresenter.BindInventory(_inventoryController);
            }

            if (_playerInputReader != null)
            {
                _playerInputReader.OnControlModeChanged += HandleControlModeChanged;
                HandleControlModeChanged(_playerInputReader.ControlMode);
            }
            else if (_hudPresenter != null)
            {
                _hudPresenter.SetControlMode(PlayerControlMode.Normal);
            }

            RefreshInteractionPrompt();
        }

        private void UnbindInventory()
        {
            if (_inventoryPresenter != null)
            {
                _inventoryPresenter.BindInventory(null);
            }

            if (_inventoryController != null)
            {
                _inventoryController.OnInventoryChanged -= HandleInventoryChanged;
                _inventoryController = null;
            }
        }

        private void UnbindPlayerInput()
        {
            if (_playerInputReader != null)
            {
                _playerInputReader.OnControlModeChanged -= HandleControlModeChanged;
                _playerInputReader = null;
            }
        }

        private void HandleInventoryChanged(InventoryState state)
        {
            RefreshInteractionPrompt();
        }

        private void HandleControlModeChanged(PlayerControlMode controlMode)
        {
            if (_hudPresenter != null)
            {
                _hudPresenter.SetControlMode(controlMode);
            }

            RefreshInteractionPrompt();
        }

        private void HandlePhaseChanged(PhaseId phaseId)
        {
            RefreshObjectiveFromGameFlow();
        }

        private void HandleObjectiveChanged(TimelineRole timelineRole, ObjectiveId objectiveId)
        {
            if (timelineRole != GetLocalTimelineRole())
            {
                return;
            }

            RefreshObjectiveFromGameFlow();
        }

        private void HandleLocalRoleAssigned(TimelineRole timelineRole)
        {
            RefreshObjectiveFromGameFlow();
        }

        private void HandleLocalRadioStateChanged(RadioState state, ulong talkerId)
        {
            if (_hudPresenter != null)
            {
                _hudPresenter.SetRadioState(state, talkerId);
            }
        }

        private void HandleCausalityPulse()
        {
            if (_hudPresenter != null)
            {
                _hudPresenter.ShowCausalityPulse();
            }
        }

        private void RefreshObjectiveFromGameFlow()
        {
            if (_hudPresenter == null)
            {
                return;
            }

            TimelineRole localRole = GetLocalTimelineRole();
            if (localRole == TimelineRole.None ||
                _gameFlowManager == null ||
                !_gameFlowManager.TryGetCurrentObjectiveDisplayText(localRole, out string objectiveText))
            {
                _hudPresenter.SetObjective("Objective: Stand by");
                return;
            }

            _hudPresenter.SetObjective(objectiveText);
        }

        private TimelineRole GetLocalTimelineRole()
        {
            return _roleManager != null ? _roleManager.LocalTimelineRole : TimelineRole.None;
        }

        private void RefreshRadioBinding()
        {
            if (!_autoFindDependencies || _subscribedRadioNetworkBridge != null)
            {
                return;
            }

            if (Time.unscaledTime < _nextRadioResolveTime)
            {
                return;
            }

            _nextRadioResolveTime = Time.unscaledTime + RADIO_REBIND_INTERVAL_SECONDS;
            BindRadioNetworkBridge(FindAnyObjectByType<RadioNetworkBridge>());
        }

        private void BindRadioNetworkBridge(RadioNetworkBridge radioNetworkBridge)
        {
            if (_subscribedRadioNetworkBridge == radioNetworkBridge)
            {
                RefreshRadioState();
                return;
            }

            UnbindRadioNetworkBridge();
            _radioNetworkBridge = radioNetworkBridge;
            _subscribedRadioNetworkBridge = radioNetworkBridge;

            if (_subscribedRadioNetworkBridge == null)
            {
                RefreshRadioState();
                return;
            }

            _subscribedRadioNetworkBridge.OnLocalRadioStateChanged += HandleLocalRadioStateChanged;
            RefreshRadioState();
        }

        private void UnbindRadioNetworkBridge()
        {
            if (_subscribedRadioNetworkBridge != null)
            {
                _subscribedRadioNetworkBridge.OnLocalRadioStateChanged -= HandleLocalRadioStateChanged;
            }

            _subscribedRadioNetworkBridge = null;
        }

        private void RefreshRadioState()
        {
            if (_hudPresenter == null)
            {
                return;
            }

            if (_subscribedRadioNetworkBridge == null)
            {
                _hudPresenter.SetRadioState(RadioState.Idle, RadioNetworkBridge.NO_TALKER_ID);
                return;
            }

            _hudPresenter.SetRadioState(
                _subscribedRadioNetworkBridge.LocalRadioState,
                _subscribedRadioNetworkBridge.CurrentTalkerId);
        }

        private void RefreshInteractionPrompt()
        {
            if (_hudPresenter == null)
            {
                return;
            }

            string promptText = ResolvePromptText();
            if (promptText == _lastPromptText)
            {
                return;
            }

            _lastPromptText = promptText;
            if (string.IsNullOrWhiteSpace(promptText))
            {
                _hudPresenter.HideInteractionPrompt();
                return;
            }

            _hudPresenter.ShowInteractionPrompt(promptText);
        }

        private string ResolvePromptText()
        {
            if (_interactionProbe == null)
            {
                return string.Empty;
            }

            string selectedItemId = _inventoryController != null
                ? _inventoryController.SelectedItemId
                : string.Empty;
            if (_playerInputReader != null && _playerInputReader.ControlMode == PlayerControlMode.Combat)
            {
                if (_interactionProbe.CurrentTakedownTarget != null)
                {
                    return BuildTakedownPromptText();
                }

                return BuildPromptText(
                    null,
                    _interactionProbe.ProximityTarget,
                    selectedItemId);
            }

            return BuildPromptText(
                _interactionProbe.HoverTarget,
                _interactionProbe.ProximityTarget,
                selectedItemId);
        }
    }
}
