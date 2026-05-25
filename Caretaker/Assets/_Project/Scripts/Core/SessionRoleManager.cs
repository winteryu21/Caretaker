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

        public event Action<PlayerSessionData> OnPlayerRegistered;
        public event Action<ulong> OnPlayerUnregistered;
        public event Action<PlayerSessionData> OnPlayerReadyChanged;
        public event Action<PlayerSessionData> OnPlayerRoleChanged;
        public event Action<TimelineRole> OnLocalRoleAssigned;
        public event Action<NetworkSessionStatus> OnSessionStatusReceived;

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

            OnPlayerUnregistered?.Invoke(clientId);
        }

        public void Clear()
        {
            _players.Clear();
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

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SubmitReadyRpc(bool isReady, RpcParams rpcParams = default)
        {
            SetReady(rpcParams.Receive.SenderClientId, isReady);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SubmitTimelineRoleRpc(TimelineRole requestedRole, RpcParams rpcParams = default)
        {
            SetTimelineRole(rpcParams.Receive.SenderClientId, requestedRole);
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
    }
}
