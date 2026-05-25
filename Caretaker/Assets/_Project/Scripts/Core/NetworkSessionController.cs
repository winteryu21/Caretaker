using System;
using System.Collections.Generic;

using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Core
{
    public sealed class NetworkSessionController : MonoBehaviour
    {
        public const int MAX_PLAYERS = 2;

        [Header("Dependencies")]
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private SessionRoleManager _roleManager;

        [Header("Session")]
        [SerializeField] private float _connectionTimeoutSeconds = 10f;
        [SerializeField] private bool _useConnectionApproval = true;

        [Header("Game Start")]
        [SerializeField] private bool _autoStartGameWhenBothReady = true;
        [SerializeField] private string _gameSceneName = "Game";

        private bool _callbacksRegistered;
        private bool _gameStartRequested;
        private bool _sceneCallbacksRegistered;

        public event Action<NetworkSessionStatus> OnSessionStatusChanged;
        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;

        public NetworkSessionStatus Status { get; private set; } = NetworkSessionStatus.Offline;
        public float ConnectionTimeoutSeconds => _connectionTimeoutSeconds;
        public string GameSceneName => _gameSceneName;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            RegisterCallbacks();
        }

        private void OnDisable()
        {
            UnregisterSceneCallbacks();
            UnregisterCallbacks();
        }

        public bool StartHostSession()
        {
            if (!CanStartSession())
            {
                return false;
            }

            ConfigureConnectionApproval();
            RegisterCallbacks();

            bool started = _networkManager.StartHost();
            if (started)
            {
                SetStatus(NetworkSessionStatus.WaitingForPlayers);
            }

            return started;
        }

        public bool StartClientSession()
        {
            if (!CanStartSession())
            {
                return false;
            }

            ConfigureConnectionApproval();
            RegisterCallbacks();

            bool started = _networkManager.StartClient();
            if (started)
            {
                SetStatus(NetworkSessionStatus.WaitingForPlayers);
            }

            return started;
        }

        public void ShutdownSession()
        {
            ResolveDependencies();

            if (_networkManager == null)
            {
                SetStatus(NetworkSessionStatus.Offline);
                return;
            }

            SetStatus(NetworkSessionStatus.ShuttingDown);

            if (_networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }

            _gameStartRequested = false;
            _roleManager?.Clear();
            SetStatus(NetworkSessionStatus.Offline);
        }

        public void MarkLocalReady(bool isReady)
        {
            if (Status is NetworkSessionStatus.GameStarting or NetworkSessionStatus.InGame)
            {
                Debug.LogWarning("Cannot change ready state after game start.", this);
                return;
            }

            _roleManager?.SetLocalReady(isReady);
        }

        public void SelectLocalTimelineRole(TimelineRole timelineRole)
        {
            if (Status is NetworkSessionStatus.GameStarting or NetworkSessionStatus.InGame)
            {
                Debug.LogWarning("Cannot change timeline role after game start.", this);
                return;
            }

            _roleManager?.SetLocalTimelineRole(timelineRole);
        }

        public void MarkGameStarting()
        {
            if (_networkManager == null || !_networkManager.IsServer)
            {
                return;
            }

            TryStartGameSceneLoad();
        }

        public void MarkInGame()
        {
            if (_networkManager == null || !_networkManager.IsServer)
            {
                return;
            }

            SetStatus(NetworkSessionStatus.InGame);
        }

        private bool CanStartSession()
        {
            ResolveDependencies();

            if (_networkManager == null)
            {
                Debug.LogError("NetworkSessionController requires a NetworkManager.");
                return false;
            }

            if (_networkManager.IsListening)
            {
                Debug.LogWarning("Network session is already running.");
                return false;
            }

            return true;
        }

        private void ResolveDependencies()
        {
            if (_networkManager == null)
            {
                _networkManager = NetworkManager.Singleton != null
                    ? NetworkManager.Singleton
                    : FindAnyObjectByType<NetworkManager>();
            }

            if (_roleManager == null)
            {
                _roleManager = FindAnyObjectByType<SessionRoleManager>();
            }
        }

        private void ConfigureConnectionApproval()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.NetworkConfig.ConnectionApproval = _useConnectionApproval;
            _networkManager.NetworkConfig.ClientConnectionBufferTimeout =
                Mathf.CeilToInt(_connectionTimeoutSeconds);

            if (_useConnectionApproval)
            {
                _networkManager.ConnectionApprovalCallback = HandleConnectionApproval;
            }
        }

        private void RegisterCallbacks()
        {
            ResolveDependencies();

            if (_callbacksRegistered || _networkManager == null)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback += HandleClientConnected;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnected;

            if (_roleManager != null)
            {
                _roleManager.OnPlayerRegistered += HandlePlayerRegistered;
                _roleManager.OnPlayerUnregistered += HandlePlayerUnregistered;
                _roleManager.OnPlayerReadyChanged += HandlePlayerReadyChanged;
                _roleManager.OnPlayerRoleChanged += HandlePlayerRoleChanged;
                _roleManager.OnSessionStatusReceived += HandleRemoteSessionStatusReceived;
            }

            _callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (!_callbacksRegistered || _networkManager == null)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback -= HandleClientConnected;
            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;

            if (_roleManager != null)
            {
                _roleManager.OnPlayerRegistered -= HandlePlayerRegistered;
                _roleManager.OnPlayerUnregistered -= HandlePlayerUnregistered;
                _roleManager.OnPlayerReadyChanged -= HandlePlayerReadyChanged;
                _roleManager.OnPlayerRoleChanged -= HandlePlayerRoleChanged;
                _roleManager.OnSessionStatusReceived -= HandleRemoteSessionStatusReceived;
            }

            _callbacksRegistered = false;
        }

        private void HandleConnectionApproval(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            int connectedClients = _networkManager.ConnectedClientsIds.Count;
            bool sessionAcceptsJoin = Status is NetworkSessionStatus.Offline or NetworkSessionStatus.WaitingForPlayers;
            bool hasSlot = connectedClients < MAX_PLAYERS;

            response.Approved = sessionAcceptsJoin && hasSlot;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = response.Approved ? string.Empty : "Session is full or already started.";
        }

        private void HandleClientConnected(ulong clientId)
        {
            OnClientConnected?.Invoke(clientId);

            if (_networkManager.IsServer)
            {
                _roleManager?.RegisterClient(clientId);
                RefreshStatusFromRoleState();
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            OnClientDisconnected?.Invoke(clientId);

            if (!_networkManager.IsServer && clientId == NetworkManager.ServerClientId)
            {
                _gameStartRequested = false;
                _roleManager?.Clear();
                SetStatus(NetworkSessionStatus.Offline);
                return;
            }

            if (_networkManager.IsServer)
            {
                _roleManager?.UnregisterClient(clientId);
            }

            if (!_networkManager.IsListening)
            {
                _gameStartRequested = false;
                _roleManager?.Clear();
                SetStatus(NetworkSessionStatus.Offline);
                return;
            }

            RefreshStatusFromRoleState();
        }

        private void HandlePlayerRegistered(PlayerSessionData player)
        {
            RefreshStatusFromRoleState();
        }

        private void HandlePlayerUnregistered(ulong clientId)
        {
            RefreshStatusFromRoleState();
        }

        private void HandlePlayerReadyChanged(PlayerSessionData player)
        {
            RefreshStatusFromRoleState();
        }

        private void HandlePlayerRoleChanged(PlayerSessionData player)
        {
            RefreshStatusFromRoleState();
        }

        private void HandleRemoteSessionStatusReceived(NetworkSessionStatus status)
        {
            if (_networkManager != null && _networkManager.IsServer)
            {
                return;
            }

            SetStatus(status);
        }

        private void RefreshStatusFromRoleState()
        {
            if (_roleManager == null)
            {
                _gameStartRequested = false;
                SetStatus(NetworkSessionStatus.WaitingForPlayers);
                return;
            }

            if (_roleManager.AreBothPlayersReady)
            {
                SetStatus(NetworkSessionStatus.BothReady);

                if (_autoStartGameWhenBothReady)
                {
                    TryStartGameSceneLoad();
                }

                return;
            }

            _gameStartRequested = false;
            SetStatus(_roleManager.HasBothPlayers
                ? NetworkSessionStatus.BothConnected
                : NetworkSessionStatus.WaitingForPlayers);
        }

        private void SetStatus(NetworkSessionStatus status)
        {
            if (Status == status)
            {
                return;
            }

            Status = status;
            OnSessionStatusChanged?.Invoke(status);

            if (_networkManager != null && _networkManager.IsServer)
            {
                _roleManager?.BroadcastSessionStatus(status);
            }
        }

        private void TryStartGameSceneLoad()
        {
            if (_networkManager == null || !_networkManager.IsServer)
            {
                return;
            }

            if (_gameStartRequested || Status == NetworkSessionStatus.InGame)
            {
                return;
            }

            if (_roleManager == null || !_roleManager.AreBothPlayersReady)
            {
                Debug.LogWarning("Cannot start game scene until both players are ready.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(_gameSceneName))
            {
                Debug.LogError("Game scene name is empty.", this);
                return;
            }

            if (_networkManager.SceneManager == null)
            {
                Debug.LogError("Network scene management is disabled or unavailable.", this);
                return;
            }

            RegisterSceneCallbacks();
            SceneEventProgressStatus progressStatus =
                _networkManager.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);

            if (progressStatus != SceneEventProgressStatus.Started)
            {
                Debug.LogError(
                    $"Failed to load game scene '{_gameSceneName}'. SceneEventProgressStatus={progressStatus}",
                    this);
                return;
            }

            _gameStartRequested = true;
            SetStatus(NetworkSessionStatus.GameStarting);
        }

        private void RegisterSceneCallbacks()
        {
            if (_sceneCallbacksRegistered || _networkManager == null || _networkManager.SceneManager == null)
            {
                return;
            }

            _networkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
            _sceneCallbacksRegistered = true;
        }

        private void UnregisterSceneCallbacks()
        {
            if (!_sceneCallbacksRegistered || _networkManager == null || _networkManager.SceneManager == null)
            {
                return;
            }

            _networkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            _sceneCallbacksRegistered = false;
        }

        private void HandleLoadEventCompleted(
            string sceneName,
            LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted,
            List<ulong> clientsTimedOut)
        {
            if (!_networkManager.IsServer || sceneName != _gameSceneName)
            {
                return;
            }

            if (clientsTimedOut.Count > 0)
            {
                Debug.LogWarning(
                    $"Game scene '{sceneName}' loaded with {clientsTimedOut.Count} timed out client(s).",
                    this);
            }

            SetStatus(NetworkSessionStatus.InGame);
        }
    }
}
