using System;
using System.Collections.Generic;

using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;

namespace Caretaker.Core
{
    public sealed class SessionRoleManager : NetworkBehaviour
    {
        [SerializeField] private bool _assignHostAsPast = true;
        [SerializeField] private bool _persistAcrossScenes = true;

        private readonly Dictionary<ulong, PlayerSessionData> _players = new();
        private readonly HashSet<ulong> _phaseRestartReadyClientIds = new();

        private PhaseId _restartingPhase = PhaseId.Phase1;
        private bool _isPhaseRestartPending;

        public event Action<PlayerSessionData> OnPlayerRegistered;
        public event Action<ulong> OnPlayerUnregistered;
        public event Action<PlayerSessionData> OnPlayerReadyChanged;
        public event Action<PlayerSessionData> OnPlayerRoleChanged;
        public event Action<TimelineRole> OnLocalRoleAssigned;
        public event Action<NetworkSessionStatus> OnSessionStatusReceived;
        public event Action<ulong> OnPhaseAdvanceReadySubmitted;
        public event Action<PhaseId> OnPhaseTransitionReceived;
        public event Action<PhaseId> OnPhaseRestartReceived;
        public event Action<PhaseId> OnPhaseRestartLoadReceived;
        public event Action<PhaseId, MajorId> OnMajorCompletedReceived;
        public event Action<TimelineRole, ObjectiveId> OnObjectiveChangedReceived;
        public event Action<GameResult> OnGameResultReceived;

        public IReadOnlyDictionary<ulong, PlayerSessionData> Players => _players;
        public TimelineRole LocalTimelineRole { get; private set; } = TimelineRole.None;
        public NetworkSessionStatus LastReceivedStatus { get; private set; } = NetworkSessionStatus.Offline;
        public bool HasBothPlayers => _players.Count == NetworkSessionController.MAX_PLAYERS;
        public bool AreBothPlayersReady => HasBothPlayers && AreAllPlayersReady();

        public override void OnNetworkSpawn()
        {
            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        public void RegisterClient(ulong clientId)
        {
            if (!IsServer || _players.ContainsKey(clientId))
            {
                return;
            }

            TimelineRole role = GetRoleForNewClient(clientId);
            if (role == TimelineRole.None)
            {
                Debug.LogWarning($"No timeline role available for client {clientId}.");
                return;
            }

            PlayerSessionData player = new PlayerSessionData(clientId, role, false);
            _players.Add(clientId, player);

            OnPlayerRegistered?.Invoke(player);
            SendRoleAssignment(clientId, role);
        }

        public void UnregisterClient(ulong clientId)
        {
            if (!IsServer || !_players.Remove(clientId))
            {
                return;
            }

            _phaseRestartReadyClientIds.Remove(clientId);
            OnPlayerUnregistered?.Invoke(clientId);
            TryCompletePhaseRestart();
        }

        public void Clear()
        {
            _players.Clear();
            _phaseRestartReadyClientIds.Clear();
            _isPhaseRestartPending = false;
            LocalTimelineRole = TimelineRole.None;
        }

        public bool TryGetPlayer(ulong clientId, out PlayerSessionData player)
        {
            return _players.TryGetValue(clientId, out player);
        }

        public void BroadcastSessionStatus(NetworkSessionStatus status)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            ReceiveSessionStatusClientRpc(status);
        }

        public void SetLocalReady(bool isReady)
        {
            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                return;
            }

            if (IsServer)
            {
                SetReady(NetworkManager.LocalClientId, isReady);
                return;
            }

            SubmitReadyRpc(isReady);
        }

        public void SetLocalTimelineRole(TimelineRole requestedRole)
        {
            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                return;
            }

            if (IsServer)
            {
                SetTimelineRole(NetworkManager.LocalClientId, requestedRole);
                return;
            }

            SubmitTimelineRoleRpc(requestedRole);
        }

        /// <summary>
        /// 기능검증용 Phase 전환 준비 입력을 Host에 제출한다.
        /// </summary>
        public void SubmitLocalPhaseAdvanceReady()
        {
            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                return;
            }

            if (IsServer)
            {
                NotifyPhaseAdvanceReady(NetworkManager.LocalClientId);
                return;
            }

            SubmitPhaseAdvanceReadyRpc();
        }

        /// <summary>
        /// Host가 결정한 Phase 전환을 모든 클라이언트에 전파한다.
        /// </summary>
        /// <param name="targetPhase">전환 대상 Phase.</param>
        public void BroadcastPhaseTransition(PhaseId targetPhase)
        {
            if (!IsServer || !IsSpawned)
            {
                Debug.LogWarning(
                    $"Cannot broadcast phase transition. isServer={IsServer}, isSpawned={IsSpawned}, target={targetPhase}",
                    this);
                return;
            }

            Debug.Log($"SessionRoleManager broadcasting phase transition: target={targetPhase}", this);
            ReceivePhaseTransitionClientRpc(targetPhase);
        }

        /// <summary>
        /// Host가 요청한 Phase 재시작을 모든 클라이언트에 전파한다.
        /// </summary>
        /// <returns>재시작 RPC를 전파했는지 여부.</returns>
        public bool BroadcastPhaseRestart(PhaseId phaseId)
        {
            if (!IsServer || !IsSpawned)
            {
                Debug.LogWarning(
                    $"Cannot broadcast phase restart. isServer={IsServer}, isSpawned={IsSpawned}, phase={phaseId}",
                    this);
                return false;
            }

            _phaseRestartReadyClientIds.Clear();
            _restartingPhase = phaseId;
            _isPhaseRestartPending = true;
            Debug.Log($"SessionRoleManager broadcasting phase restart: phase={phaseId}", this);
            ReceivePhaseRestartClientRpc(phaseId);
            return true;
        }

        /// <summary>로컬 클라이언트의 Phase 씬 언로드 완료를 Host에 제출한다.</summary>
        public void SubmitLocalPhaseRestartReady(PhaseId phaseId)
        {
            if (NetworkManager == null || !NetworkManager.IsListening)
            {
                return;
            }

            if (IsServer)
            {
                NotifyPhaseRestartReady(NetworkManager.LocalClientId, phaseId);
                return;
            }

            SubmitPhaseRestartReadyRpc(phaseId);
        }

        /// <summary>
        /// Host가 확정한 Major 완료를 모든 클라이언트에 전파한다.
        /// </summary>
        /// <param name="phaseId">완료가 발생한 Phase.</param>
        /// <param name="majorId">완료된 Major ID.</param>
        public void BroadcastMajorCompleted(PhaseId phaseId, MajorId majorId)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            ReceiveMajorCompletedClientRpc(phaseId, majorId);
        }

        /// <summary>
        /// Host가 계산한 역할별 Objective 변경을 모든 클라이언트에 전파한다.
        /// </summary>
        /// <param name="timelineRole">Objective 대상 시간대 역할.</param>
        /// <param name="objectiveId">현재 Objective ID.</param>
        public void BroadcastObjectiveChanged(TimelineRole timelineRole, ObjectiveId objectiveId)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            ReceiveObjectiveChangedClientRpc(timelineRole, objectiveId);
        }

        /// <summary>
        /// Host가 확정한 게임 결과를 모든 클라이언트에 전파한다.
        /// </summary>
        /// <param name="gameResult">게임 결과.</param>
        public void BroadcastGameResult(GameResult gameResult)
        {
            if (!IsServer || !IsSpawned)
            {
                Debug.LogWarning(
                    $"Cannot broadcast game result. isServer={IsServer}, isSpawned={IsSpawned}, result={gameResult}",
                    this);
                return;
            }

            Debug.Log($"SessionRoleManager broadcasting game result: result={gameResult}", this);
            ReceiveGameResultClientRpc(gameResult);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitReadyRpc(bool isReady, RpcParams rpcParams = default)
        {
            SetReady(rpcParams.Receive.SenderClientId, isReady);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitTimelineRoleRpc(TimelineRole requestedRole, RpcParams rpcParams = default)
        {
            SetTimelineRole(rpcParams.Receive.SenderClientId, requestedRole);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitPhaseAdvanceReadyRpc(RpcParams rpcParams = default)
        {
            NotifyPhaseAdvanceReady(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitPhaseRestartReadyRpc(PhaseId phaseId, RpcParams rpcParams = default)
        {
            NotifyPhaseRestartReady(rpcParams.Receive.SenderClientId, phaseId);
        }

        private void SetReady(ulong clientId, bool isReady)
        {
            if (!_players.TryGetValue(clientId, out PlayerSessionData player))
            {
                return;
            }

            PlayerSessionData updatedPlayer = player.WithReady(isReady);
            _players[clientId] = updatedPlayer;
            OnPlayerReadyChanged?.Invoke(updatedPlayer);
        }

        private void SetTimelineRole(ulong clientId, TimelineRole requestedRole)
        {
            if (!IsServer || requestedRole is not (TimelineRole.Past or TimelineRole.Future))
            {
                return;
            }

            if (!_players.TryGetValue(clientId, out PlayerSessionData player))
            {
                return;
            }

            if (player.TimelineRole == requestedRole)
            {
                return;
            }

            ulong? otherClientId = FindClientWithRole(requestedRole);
            PlayerSessionData updatedPlayer = player.WithTimelineRole(requestedRole);
            _players[clientId] = updatedPlayer;
            OnPlayerRoleChanged?.Invoke(updatedPlayer);
            SendRoleAssignment(clientId, requestedRole);

            if (otherClientId.HasValue && _players.TryGetValue(otherClientId.Value, out PlayerSessionData otherPlayer))
            {
                PlayerSessionData updatedOtherPlayer = otherPlayer.WithTimelineRole(player.TimelineRole);
                _players[otherClientId.Value] = updatedOtherPlayer;
                OnPlayerRoleChanged?.Invoke(updatedOtherPlayer);
                SendRoleAssignment(otherClientId.Value, updatedOtherPlayer.TimelineRole);
            }
        }

        private TimelineRole GetRoleForNewClient(ulong clientId)
        {
            if (_players.Count == 0)
            {
                bool isHost = NetworkManager != null && clientId == NetworkManager.ServerClientId;
                if (isHost)
                {
                    return _assignHostAsPast ? TimelineRole.Past : TimelineRole.Future;
                }

                return TimelineRole.Past;
            }

            bool pastTaken = IsRoleTaken(TimelineRole.Past);
            bool futureTaken = IsRoleTaken(TimelineRole.Future);

            if (!pastTaken)
            {
                return TimelineRole.Past;
            }

            return futureTaken ? TimelineRole.None : TimelineRole.Future;
        }

        private bool IsRoleTaken(TimelineRole role)
        {
            foreach (PlayerSessionData player in _players.Values)
            {
                if (player.TimelineRole == role)
                {
                    return true;
                }
            }

            return false;
        }

        private ulong? FindClientWithRole(TimelineRole role)
        {
            foreach (KeyValuePair<ulong, PlayerSessionData> pair in _players)
            {
                if (pair.Value.TimelineRole == role)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        private bool AreAllPlayersReady()
        {
            foreach (PlayerSessionData player in _players.Values)
            {
                if (!player.IsReady)
                {
                    return false;
                }
            }

            return true;
        }

        private void SendRoleAssignment(ulong clientId, TimelineRole role)
        {
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };

            ReceiveRoleAssignmentClientRpc(role, rpcParams);
        }

        [ClientRpc]
        private void ReceiveRoleAssignmentClientRpc(TimelineRole role, ClientRpcParams rpcParams = default)
        {
            LocalTimelineRole = role;
            OnLocalRoleAssigned?.Invoke(role);
        }

        [ClientRpc]
        private void ReceiveSessionStatusClientRpc(NetworkSessionStatus status)
        {
            LastReceivedStatus = status;
            OnSessionStatusReceived?.Invoke(status);
        }

        private void NotifyPhaseAdvanceReady(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            OnPhaseAdvanceReadySubmitted?.Invoke(clientId);
        }

        private void NotifyPhaseRestartReady(ulong clientId, PhaseId phaseId)
        {
            if (!IsServer
                || !_isPhaseRestartPending
                || phaseId != _restartingPhase
                || !_players.ContainsKey(clientId)
                || !_phaseRestartReadyClientIds.Add(clientId))
            {
                return;
            }

            TryCompletePhaseRestart();
        }

        private void TryCompletePhaseRestart()
        {
            if (!_isPhaseRestartPending
                || _players.Count == 0
                || _phaseRestartReadyClientIds.Count < _players.Count)
            {
                return;
            }

            PhaseId phaseId = _restartingPhase;
            _isPhaseRestartPending = false;
            _phaseRestartReadyClientIds.Clear();
            Debug.Log($"All clients are ready to reload phase: phase={phaseId}", this);
            ReceivePhaseRestartLoadClientRpc(phaseId);
        }

        [ClientRpc]
        private void ReceivePhaseTransitionClientRpc(PhaseId targetPhase)
        {
            Debug.Log($"SessionRoleManager received phase transition RPC: target={targetPhase}", this);
            OnPhaseTransitionReceived?.Invoke(targetPhase);
        }

        [ClientRpc]
        private void ReceivePhaseRestartClientRpc(PhaseId phaseId)
        {
            Debug.Log($"SessionRoleManager received phase restart RPC: phase={phaseId}", this);
            OnPhaseRestartReceived?.Invoke(phaseId);
        }

        [ClientRpc]
        private void ReceivePhaseRestartLoadClientRpc(PhaseId phaseId)
        {
            Debug.Log($"SessionRoleManager received phase restart load RPC: phase={phaseId}", this);
            OnPhaseRestartLoadReceived?.Invoke(phaseId);
        }

        [ClientRpc]
        private void ReceiveMajorCompletedClientRpc(PhaseId phaseId, MajorId majorId)
        {
            OnMajorCompletedReceived?.Invoke(phaseId, majorId);
        }

        [ClientRpc]
        private void ReceiveObjectiveChangedClientRpc(TimelineRole timelineRole, ObjectiveId objectiveId)
        {
            OnObjectiveChangedReceived?.Invoke(timelineRole, objectiveId);
        }

        [ClientRpc]
        private void ReceiveGameResultClientRpc(GameResult gameResult)
        {
            Debug.Log($"SessionRoleManager received game result RPC: result={gameResult}", this);
            OnGameResultReceived?.Invoke(gameResult);
        }
    }
}
