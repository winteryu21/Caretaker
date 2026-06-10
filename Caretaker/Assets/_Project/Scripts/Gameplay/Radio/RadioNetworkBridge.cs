using System;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 송신권 RPC와 상태 동기화를 담당한다.
    /// Host가 송신권을 부여/회수한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.6 — 무전기 통신 시스템
    /// 계층: Network Boundary
    /// </remarks>
    public class RadioNetworkBridge : NetworkBehaviour
    {
        public const ulong NO_TALKER_ID = ulong.MaxValue;

        [SerializeField] private float _talkArbitrationWindowSeconds = 0.1f;

        private readonly NetworkVariable<ulong> _currentTalkerId = new(
            NO_TALKER_ID,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly RadioService _radioService = new();
        private readonly List<TalkRequest> _pendingTalkRequests = new();
        private bool _isTalkArbitrationActive;
        private double _talkArbitrationDeadline;

        /// <summary>
        /// 로컬 플레이어 기준 무전기 상태가 변경될 때 발생한다.
        /// </summary>
        public event Action<RadioState, ulong> OnLocalRadioStateChanged;

        /// <summary>
        /// 현재 송신권 보유자 ID를 반환한다. 송신자가 없으면 <see cref="NO_TALKER_ID"/>다.
        /// </summary>
        public ulong CurrentTalkerId => _currentTalkerId.Value;

        /// <summary>
        /// 로컬 플레이어 기준 현재 무전기 상태를 반환한다.
        /// </summary>
        public RadioState LocalRadioState => GetLocalRadioState();

        /// <summary>
        /// 현재 송신권이 비어 있는지 반환한다.
        /// </summary>
        public bool IsRadioIdle => _currentTalkerId.Value == NO_TALKER_ID;

        public override void OnNetworkSpawn()
        {
            _currentTalkerId.OnValueChanged += HandleCurrentTalkerChanged;

            if (IsServer)
            {
                _radioService.Clear();
                _currentTalkerId.Value = NO_TALKER_ID;
                ClearPendingTalkRequests();

                if (NetworkManager != null)
                {
                    NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
                }
            }

            PublishLocalState();
        }

        public override void OnNetworkDespawn()
        {
            _currentTalkerId.OnValueChanged -= HandleCurrentTalkerChanged;

            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void Update()
        {
            if (!IsServer || !_isTalkArbitrationActive || !CanUseNetwork())
            {
                return;
            }

            if (GetServerTimeSeconds() < _talkArbitrationDeadline)
            {
                return;
            }

            ResolvePendingTalkRequests();
        }

        /// <summary>
        /// 로컬 플레이어의 송신권 요청을 Host에 제출한다.
        /// </summary>
        public void RequestLocalTalk()
        {
            if (!CanUseNetwork())
            {
                return;
            }

            if (IsServer)
            {
                QueueTalkRequest(NetworkManager.LocalClientId, GetServerTimeSeconds());
                return;
            }

            RequestTalkRpc(GetServerTimeSeconds());
        }

        /// <summary>
        /// 로컬 플레이어의 송신권 해제 요청을 Host에 제출한다.
        /// </summary>
        public void ReleaseLocalTalk()
        {
            if (!CanUseNetwork())
            {
                return;
            }

            if (IsServer)
            {
                ProcessTalkRelease(NetworkManager.LocalClientId);
                return;
            }

            ReleaseTalkRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestTalkRpc(double requestedServerTime, RpcParams rpcParams = default)
        {
            QueueTalkRequest(rpcParams.Receive.SenderClientId, requestedServerTime);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseTalkRpc(RpcParams rpcParams = default)
        {
            ProcessTalkRelease(rpcParams.Receive.SenderClientId);
        }

        private void QueueTalkRequest(ulong clientId, double requestedServerTime)
        {
            if (!IsServer)
            {
                return;
            }

            if (!_radioService.IsIdle)
            {
                bool isCurrentTalker = _radioService.CurrentTalkerId.HasValue
                    && _radioService.CurrentTalkerId.Value == clientId;
                if (!isCurrentTalker)
                {
                    SendTalkDenied(clientId);
                }

                return;
            }

            double serverNow = GetServerTimeSeconds();
            double clampedRequestTime = ClampRequestTime(requestedServerTime, serverNow);
            AddOrUpdatePendingTalkRequest(clientId, clampedRequestTime);

            if (!_isTalkArbitrationActive)
            {
                _isTalkArbitrationActive = true;
                _talkArbitrationDeadline = serverNow + Mathf.Max(0f, _talkArbitrationWindowSeconds);
            }
        }

        private void ResolvePendingTalkRequests()
        {
            if (!IsServer)
            {
                return;
            }

            if (_pendingTalkRequests.Count == 0)
            {
                _isTalkArbitrationActive = false;
                return;
            }

            int winnerIndex = GetWinningPendingTalkRequestIndex();
            ulong winnerClientId = _pendingTalkRequests[winnerIndex].ClientId;

            bool wasGranted = _radioService.RequestTalk(winnerClientId);
            if (wasGranted)
            {
                _currentTalkerId.Value = winnerClientId;
                PublishLocalState();
            }

            for (int i = 0; i < _pendingTalkRequests.Count; i++)
            {
                ulong clientId = _pendingTalkRequests[i].ClientId;
                if (clientId == winnerClientId)
                {
                    continue;
                }

                SendTalkDenied(clientId);
            }

            ClearPendingTalkRequests();
        }

        private void ProcessTalkRelease(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            RemovePendingTalkRequest(clientId);

            if (_radioService.ReleaseTalk(clientId))
            {
                _currentTalkerId.Value = NO_TALKER_ID;
                PublishLocalState();
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            RemovePendingTalkRequest(clientId);

            if (_radioService.ForceReleaseIfOwnedBy(clientId))
            {
                _currentTalkerId.Value = NO_TALKER_ID;
                PublishLocalState();
            }
        }

        private void AddOrUpdatePendingTalkRequest(ulong clientId, double requestedServerTime)
        {
            for (int i = 0; i < _pendingTalkRequests.Count; i++)
            {
                if (_pendingTalkRequests[i].ClientId != clientId)
                {
                    continue;
                }

                if (requestedServerTime < _pendingTalkRequests[i].RequestedServerTime)
                {
                    _pendingTalkRequests[i] = new TalkRequest(clientId, requestedServerTime);
                }

                return;
            }

            _pendingTalkRequests.Add(new TalkRequest(clientId, requestedServerTime));
        }

        private void RemovePendingTalkRequest(ulong clientId)
        {
            for (int i = _pendingTalkRequests.Count - 1; i >= 0; i--)
            {
                if (_pendingTalkRequests[i].ClientId == clientId)
                {
                    _pendingTalkRequests.RemoveAt(i);
                }
            }

            if (_pendingTalkRequests.Count == 0)
            {
                _isTalkArbitrationActive = false;
            }
        }

        private int GetWinningPendingTalkRequestIndex()
        {
            int winnerIndex = 0;
            TalkRequest winner = _pendingTalkRequests[0];

            for (int i = 1; i < _pendingTalkRequests.Count; i++)
            {
                TalkRequest candidate = _pendingTalkRequests[i];
                if (candidate.RequestedServerTime < winner.RequestedServerTime ||
                    (Math.Abs(candidate.RequestedServerTime - winner.RequestedServerTime) <= double.Epsilon &&
                     candidate.ClientId < winner.ClientId))
                {
                    winnerIndex = i;
                    winner = candidate;
                }
            }

            return winnerIndex;
        }

        private double ClampRequestTime(double requestedServerTime, double serverNow)
        {
            double maxPastOffset = Mathf.Max(0f, _talkArbitrationWindowSeconds);
            double earliestAcceptedTime = serverNow - maxPastOffset;
            if (requestedServerTime < earliestAcceptedTime)
            {
                return earliestAcceptedTime;
            }

            return requestedServerTime > serverNow ? serverNow : requestedServerTime;
        }

        private void ClearPendingTalkRequests()
        {
            _pendingTalkRequests.Clear();
            _isTalkArbitrationActive = false;
            _talkArbitrationDeadline = 0d;
        }

        private void HandleCurrentTalkerChanged(ulong previousTalkerId, ulong currentTalkerId)
        {
            PublishLocalState();
        }

        private void SendTalkDenied(ulong clientId)
        {
            ClientRpcParams clientRpcParams = new()
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };

            ReceiveTalkDeniedClientRpc(clientRpcParams);
        }

        [ClientRpc]
        private void ReceiveTalkDeniedClientRpc(ClientRpcParams clientRpcParams = default)
        {
            OnLocalRadioStateChanged?.Invoke(RadioState.Blocked, _currentTalkerId.Value);
        }

        private void PublishLocalState()
        {
            OnLocalRadioStateChanged?.Invoke(GetLocalRadioState(), _currentTalkerId.Value);
        }

        private RadioState GetLocalRadioState()
        {
            ulong talkerId = _currentTalkerId.Value;
            if (talkerId == NO_TALKER_ID)
            {
                return RadioState.Idle;
            }

            return CanUseNetwork() && talkerId == NetworkManager.LocalClientId
                ? RadioState.Transmitting
                : RadioState.Receiving;
        }

        private bool CanUseNetwork()
        {
            return NetworkManager != null && NetworkManager.IsListening && IsSpawned;
        }

        private double GetServerTimeSeconds()
        {
            return NetworkManager != null ? NetworkManager.ServerTime.Time : Time.unscaledTimeAsDouble;
        }

        private struct TalkRequest
        {
            public TalkRequest(ulong clientId, double requestedServerTime)
            {
                ClientId = clientId;
                RequestedServerTime = requestedServerTime;
            }

            public ulong ClientId { get; }
            public double RequestedServerTime { get; }
        }
    }
}
