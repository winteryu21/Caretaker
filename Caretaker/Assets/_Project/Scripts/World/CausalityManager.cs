using System;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

using Caretaker.Core;
using Caretaker.Gameplay;
using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 인과 시스템의 Host RPC 진입점과 ClientRpc 결과 적용을 담당한다.
    /// CausalityService에 판정을 위임하고 네트워크로 결과를 전파한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.1 — 시간 인과 시스템
    /// 계층: Unity Component / Network Boundary
    ///
    /// 배치: Persistent 씬의 NetworkObject에 부착한다.
    /// Inspector에서 _causalRules에 Data/Causality/*.asset를 모두 연결한다.
    /// </remarks>
    public class CausalityManager : NetworkBehaviour
    {
        // ── Serialize 필드 ──
        [Header("Causal Rules")]
        [Tooltip("Data/Causality 폴더의 CausalRuleSO 에셋을 모두 연결한다.")]
        [SerializeField] private CausalRuleSO[] _causalRules = Array.Empty<CausalRuleSO>();

        // ── private 필드 ──
        private readonly CausalityService _service = new();
        private readonly Dictionary<string, CausalReceiver> _receiverRegistry = new();
        private GameFlowManager _gameFlowManager;
        private SessionRoleManager _roleManager;
        private SceneLoader _sceneLoader;
        private CausalityState _phase3RestartState;

        // ── 이벤트 ──
        /// <summary>
        /// 인과 변경이 발생했을 때 양쪽 클라이언트에서 발생한다. (Pulse UI용)
        /// </summary>
        public event Action OnCausalityPulse;

        /// <summary>
        /// Actor에게 피드백이 수신되었을 때 발생한다. (성공/실패 메시지)
        /// </summary>
        public event Action<bool, string> OnActorFeedbackReceived;

        // ── Unity 생명주기 ──
        private void Awake()
        {
            _service.Initialize(_causalRules);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ResolveDependencies();

            // Phase 씬 로드 완료 시 Receiver 재수집
            SceneManager.sceneLoaded += HandleSceneLoaded;

            if (_sceneLoader != null)
            {
                _sceneLoader.OnPhaseSceneLoaded += HandlePhaseSceneLoaded;
            }

            if (_roleManager != null)
            {
                _roleManager.OnPhaseRestartReceived += HandlePhaseRestartReceived;
            }

            if (IsServer)
            {
                RebuildReceiverRegistry();
            }
        }

        public override void OnNetworkDespawn()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (_sceneLoader != null)
            {
                _sceneLoader.OnPhaseSceneLoaded -= HandlePhaseSceneLoaded;
            }

            if (_roleManager != null)
            {
                _roleManager.OnPhaseRestartReceived -= HandlePhaseRestartReceived;
            }

            base.OnNetworkDespawn();
        }

        // ── public: Receiver 등록/해제 ──

        /// <summary>
        /// CausalReceiver를 레지스트리에 등록한다.
        /// CausalReceiver.OnEnable()에서 자동 호출된다.
        /// </summary>
        /// <param name="receiver">등록할 리시버.</param>
        public void RegisterReceiver(CausalReceiver receiver)
        {
            if (receiver == null || string.IsNullOrWhiteSpace(receiver.ReceiverId))
            {
                return;
            }

            _receiverRegistry[receiver.ReceiverId] = receiver;
        }

        /// <summary>
        /// CausalReceiver를 레지스트리에서 해제한다.
        /// CausalReceiver.OnDisable()에서 자동 호출된다.
        /// </summary>
        /// <param name="receiver">해제할 리시버.</param>
        public void UnregisterReceiver(CausalReceiver receiver)
        {
            if (receiver == null || string.IsNullOrWhiteSpace(receiver.ReceiverId))
            {
                return;
            }

            _receiverRegistry.Remove(receiver.ReceiverId);
        }

        // ── public: 조건 설정 ──

        /// <summary>
        /// 퍼즐 조건을 수동으로 충족시킨다.
        /// 무전기를 통한 정보 교환 후 UI에서 정답 입력이 확인되었을 때 호출한다.
        /// Host에서만 유효하다.
        /// </summary>
        /// <param name="conditionKey">조건 키. (예: breakerCombination)</param>
        /// <param name="value">조건 값. (예: Correct)</param>
        public void SetCondition(string conditionKey, string value)
        {
            if (!IsServer)
            {
                SetConditionServerRpc(conditionKey, value);
                return;
            }

            _service.SetConditionState(conditionKey, value);
            Debug.Log($"Condition set: {conditionKey}={value}", this);
        }

        // ── public: 상태 조회 ──

        /// <summary>
        /// 현재 인과 상태의 스냅샷을 반환한다.
        /// 체크포인트 생성 시 사용한다.
        /// </summary>
        public CausalityState GetCurrentState()
        {
            return _service.GetCurrentState();
        }

        /// <summary>
        /// 인과 상태를 스냅샷으로 복원한다.
        /// 체크포인트 복귀 시 사용한다. Host에서만 호출한다.
        /// </summary>
        public void ResetToState(CausalityState snapshot)
        {
            if (!IsServer)
            {
                return;
            }

            _service.ResetToState(snapshot);
        }

        // ── RPC: 트리거 요청 (Client → Host) ──

        /// <summary>
        /// 인과 트리거 요청을 Host에 전송한다.
        /// CausalTrigger.Fire()에서 호출된다.
        /// </summary>
        /// <param name="triggerId">활성화할 트리거 ID.</param>
        /// <param name="rpcParams">RPC 메타데이터.</param>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SubmitTriggerServerRpc(string triggerId, RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            // Host에서 역할 조회
            ResolveDependencies();

            if (_roleManager == null ||
                !_roleManager.TryGetPlayer(senderClientId, out PlayerSessionData player))
            {
                Debug.LogWarning(
                    $"CausalityManager: Unknown sender client {senderClientId}.", this);
                return;
            }

            // 아이템 목록 조회 (Host 측 InventoryState)
            IReadOnlyList<string> ownedItems = GetPlayerOwnedItems(senderClientId);

            // 판정
            CausalResult result = _service.SubmitTrigger(
                triggerId, player.TimelineRole, ownedItems);

            if (!result.Success)
            {
                Debug.Log(
                    $"CausalityManager: Trigger rejected — {result.RejectionReason}", this);

                // Actor에게 실패 피드백
                SendFeedbackToActorClientRpc(
                    result.RejectionReason ?? "Unknown error",
                    false,
                    BuildTargetRpcParams(senderClientId));
                return;
            }

            Debug.Log(
                $"CausalityManager: Trigger accepted — rule={result.RuleId}, " +
                $"effects={result.Effects?.Length ?? 0}, major={result.IsMajorProgress}", this);

            // ── 성공: 3종 전파 ──

            // 1. Receiver에 결과 적용
            ApplyResultsOnServer(result);

            // 2. Actor에게 성공 피드백
            SendFeedbackToActorClientRpc(
                "Trigger accepted",
                true,
                BuildTargetRpcParams(senderClientId));

            // 3. 양쪽에 Pulse
            BroadcastCausalityPulseClientRpc();

            // 4. Major 진행 보고
            if (result.IsMajorProgress && _gameFlowManager != null)
            {
                _gameFlowManager.NotifyMajorComplete(
                    _gameFlowManager.CurrentPhase,
                    result.RuleId);
            }
        }

        // ── RPC: 조건 설정 (Client → Host) ──

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SetConditionServerRpc(string conditionKey, string value)
        {
            _service.SetConditionState(conditionKey, value);
            Debug.Log($"Condition set via RPC: {conditionKey}={value}", this);
        }

        // ── RPC: Receiver 상태 적용 (Host → Client) ──

        [ClientRpc]
        private void ApplyReceiverStateClientRpc(
            string receiverId,
            string stateKey,
            string stateValue,
            ClientRpcParams rpcParams = default)
        {
            if (!_receiverRegistry.TryGetValue(receiverId, out CausalReceiver receiver))
            {
                // Phase 1~2에서는 자기 시간대 씬만 로드하므로,
                // 상대 시간대의 Receiver가 없는 것은 정상 동작이다.
                return;
            }

            receiver.ApplyState(stateKey, stateValue);
        }

        // ── RPC: Actor 피드백 (Host → Actor Owner) ──

        [ClientRpc]
        private void SendFeedbackToActorClientRpc(
            string message,
            bool success,
            ClientRpcParams rpcParams = default)
        {
            Debug.Log($"Causal feedback: success={success}, msg={message}", this);
            OnActorFeedbackReceived?.Invoke(success, message);
        }

        // ── RPC: 양쪽 Pulse (Host → All) ──

        [ClientRpc]
        private void BroadcastCausalityPulseClientRpc()
        {
            Debug.Log("CausalityPulse: 인과 변경 발생", this);
            OnCausalityPulse?.Invoke();
        }

        // ── private ──

        private void ApplyResultsOnServer(CausalResult result)
        {
            if (result.Effects == null)
            {
                return;
            }

            for (int i = 0; i < result.Effects.Length; i++)
            {
                CausalResult.ReceiverEffect effect = result.Effects[i];

                // 서버 로컬에도 적용 (Host가 Future 역할일 수 있음)
                if (_receiverRegistry.TryGetValue(effect.ReceiverId, out CausalReceiver receiver))
                {
                    receiver.ApplyState(effect.StateKey, effect.StateValue);
                }

                // 모든 클라이언트에 전파
                // TODO: Phase 1~2에서는 Receiver Owner에게만 전송 (정보 격리)
                ApplyReceiverStateClientRpc(
                    effect.ReceiverId,
                    effect.StateKey,
                    effect.StateValue);
            }
        }

        /// <summary>
        /// 현재 씬에 존재하는 모든 CausalReceiver를 레지스트리에 등록한다.
        /// OnNetworkSpawn 시 호출되며, Phase 씬 로드 후에도 CausalReceiver.OnEnable()에서 개별 등록된다.
        /// </summary>
        private void RebuildReceiverRegistry()
        {
            _receiverRegistry.Clear();

            CausalReceiver[] receivers = FindObjectsByType<CausalReceiver>(FindObjectsSortMode.None);

            for (int i = 0; i < receivers.Length; i++)
            {
                RegisterReceiver(receivers[i]);
            }

            Debug.Log(
                $"CausalityManager: Receiver registry rebuilt with {_receiverRegistry.Count} receivers.", this);
        }

        private IReadOnlyList<string> GetPlayerOwnedItems(ulong clientId)
        {
            InventoryController[] inventoryControllers = FindObjectsByType<InventoryController>();

            for (int i = 0; i < inventoryControllers.Length; i++)
            {
                InventoryController inventoryController = inventoryControllers[i];
                if (inventoryController != null && inventoryController.PlayerId == clientId)
                {
                    return inventoryController.OwnedItemIds;
                }
            }

            return Array.Empty<string>();
        }

        private void HandlePhaseSceneLoaded(PhaseId phaseId, TimelineRole role, string sceneName)
        {
            if (!IsServer)
            {
                return;
            }

            if (phaseId == PhaseId.Phase3)
            {
                _phase3RestartState ??= _service.GetCurrentState();
            }
            else
            {
                _phase3RestartState = null;
            }

            // 새 씬의 OnEnable이 완료된 다음 Receiver 목록을 다시 구성한다.
            StartCoroutine(RebuildReceiverRegistryDelayed());
        }

        private void HandlePhaseRestartReceived(PhaseId phaseId)
        {
            if (!IsServer || phaseId != PhaseId.Phase3 || _phase3RestartState == null)
            {
                return;
            }

            _service.ResetToState(_phase3RestartState);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsServer && mode == LoadSceneMode.Additive)
            {
                StartCoroutine(RebuildReceiverRegistryDelayed());
            }
        }

        private System.Collections.IEnumerator RebuildReceiverRegistryDelayed()
        {
            yield return null; // 1프레임 대기 — OnEnable() 실행 보장
            RebuildReceiverRegistry();
        }

        private void ResolveDependencies()
        {
            if (_gameFlowManager == null)
            {
                _gameFlowManager = FindAnyObjectByType<GameFlowManager>();
            }

            if (_roleManager == null)
            {
                _roleManager = FindAnyObjectByType<SessionRoleManager>();
            }

            if (_sceneLoader == null)
            {
                _sceneLoader = FindAnyObjectByType<SceneLoader>();
            }
        }

        private static ClientRpcParams BuildTargetRpcParams(ulong clientId)
        {
            return new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };
        }
    }
}
