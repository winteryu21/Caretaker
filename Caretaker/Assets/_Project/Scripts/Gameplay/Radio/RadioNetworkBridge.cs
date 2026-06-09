using System;

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

        private readonly NetworkVariable<ulong> _currentTalkerId = new(
            NO_TALKER_ID,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly RadioService _radioService = new();

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
                ProcessTalkRequest(NetworkManager.LocalClientId);
                return;
            }

            RequestTalkRpc();
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
        private void RequestTalkRpc(RpcParams rpcParams = default)
        {
            ProcessTalkRequest(rpcParams.Receive.SenderClientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseTalkRpc(RpcParams rpcParams = default)
        {
            ProcessTalkRelease(rpcParams.Receive.SenderClientId);
        }

        private void ProcessTalkRequest(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            bool wasGranted = _radioService.RequestTalk(clientId);
            if (wasGranted)
            {
                _currentTalkerId.Value = clientId;
                PublishLocalState();
                return;
            }

            SendTalkDenied(clientId);
        }

        private void ProcessTalkRelease(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            if (_radioService.ReleaseTalk(clientId))
            {
                _currentTalkerId.Value = NO_TALKER_ID;
                PublishLocalState();
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (_radioService.ForceReleaseIfOwnedBy(clientId))
            {
                _currentTalkerId.Value = NO_TALKER_ID;
                PublishLocalState();
            }
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
    }
}
