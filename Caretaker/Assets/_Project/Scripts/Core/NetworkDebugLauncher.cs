using Caretaker.Core;
using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Core
{
    public class NetworkDebugLauncher : MonoBehaviour
    {
        [SerializeField] private NetworkSessionController _sessionController;
        [SerializeField] private SessionRoleManager _roleManager;
        [SerializeField] private bool _showDebugPanel = true;
        [SerializeField] private Vector2 _panelPosition = new Vector2(16f, 16f);
        [SerializeField] private Vector2 _panelSize = new Vector2(360f, 180f);

    private void Awake()
    {
        ResolveDependencies();
        Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveDependencies();
    }
#endif

    public void StartHost()
    {
        if (!TryGetSessionController(out NetworkSessionController sessionController))
        {
            return;
        }

        sessionController.StartHostSession();
        LogNetworkState("StartHost requested");
    }

    public void StartClient()
    {
        if (!TryGetSessionController(out NetworkSessionController sessionController))
        {
            return;
        }

        sessionController.StartClientSession();
        LogNetworkState("StartClient requested");
    }

    public void Ready()
    {
        if (!TryGetSessionController(out NetworkSessionController sessionController))
        {
            return;
        }

        sessionController.MarkLocalReady(true);
        LogNetworkState("Ready requested");
    }

    public void SelectPast()
    {
        if (!TryGetSessionController(out NetworkSessionController sessionController))
        {
            return;
        }

        sessionController.SelectLocalTimelineRole(TimelineRole.Past);
        LogNetworkState("SelectPast requested");
    }

    public void SelectFuture()
    {
        if (!TryGetSessionController(out NetworkSessionController sessionController))
        {
            return;
        }

        sessionController.SelectLocalTimelineRole(TimelineRole.Future);
        LogNetworkState("SelectFuture requested");
    }

    public void StartGame()
    {
        if (!TryGetSessionController(out NetworkSessionController sessionController))
        {
            return;
        }

        sessionController.MarkGameStarting();
        LogNetworkState("StartGame requested");
    }

    public void Shutdown()
    {
        if (!TryGetSessionController(out NetworkSessionController sessionController))
        {
            return;
        }

        sessionController.ShutdownSession();
        LogNetworkState("Shutdown requested");
    }

    private void OnGUI()
    {
        if (!_showDebugPanel)
        {
            return;
        }

        ResolveDependencies();

        Rect rect = new Rect(_panelPosition.x, _panelPosition.y, _panelSize.x, _panelSize.y);
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.Label("Network Debug");
        GUILayout.Label($"Session: {GetSessionStatusText()}");
        GUILayout.Label($"Mode: {GetNetworkModeText()}");
        GUILayout.Label($"Local Client Id: {GetLocalClientIdText()}");
        GUILayout.Label($"Connected Clients: {GetConnectedClientCountText()}");
        GUILayout.Label($"Local Timeline Role: {GetLocalRoleText()}");
        GUILayout.Label($"Role Manager Spawned: {GetRoleManagerSpawnedText()}");
        GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Past"))
        {
            SelectPast();
        }

        if (GUILayout.Button("Future"))
        {
            SelectFuture();
        }

        if (GUILayout.Button("Ready"))
        {
            Ready();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private bool TryGetSessionController(out NetworkSessionController sessionController)
    {
        ResolveDependencies();
        sessionController = _sessionController;

        if (sessionController != null)
        {
            return true;
        }

        Debug.LogError(
            "NetworkDebugLauncher requires a NetworkSessionController in the scene. " +
            "Add NetworkSessionController to the NetworkManager GameObject or assign it in the Inspector.",
            this);
        return false;
    }

    private void ResolveDependencies()
    {
        bool shouldSubscribe = false;

        if (_sessionController == null)
        {
            _sessionController = FindAnyObjectByType<NetworkSessionController>();
            shouldSubscribe = _sessionController != null;
        }

        if (_roleManager == null)
        {
            _roleManager = FindAnyObjectByType<SessionRoleManager>();
            shouldSubscribe = shouldSubscribe || _roleManager != null;
        }

        if (shouldSubscribe)
        {
            Subscribe();
        }
    }

    private void Subscribe()
    {
        if (_sessionController != null)
        {
            _sessionController.OnSessionStatusChanged -= HandleSessionStatusChanged;
            _sessionController.OnClientConnected -= HandleClientConnected;
            _sessionController.OnClientDisconnected -= HandleClientDisconnected;
            _sessionController.OnSessionStatusChanged += HandleSessionStatusChanged;
            _sessionController.OnClientConnected += HandleClientConnected;
            _sessionController.OnClientDisconnected += HandleClientDisconnected;
        }

        if (_roleManager != null)
        {
            _roleManager.OnLocalRoleAssigned -= HandleLocalRoleAssigned;
            _roleManager.OnLocalRoleAssigned += HandleLocalRoleAssigned;
        }
    }

    private void Unsubscribe()
    {
        if (_sessionController != null)
        {
            _sessionController.OnSessionStatusChanged -= HandleSessionStatusChanged;
            _sessionController.OnClientConnected -= HandleClientConnected;
            _sessionController.OnClientDisconnected -= HandleClientDisconnected;
        }

        if (_roleManager != null)
        {
            _roleManager.OnLocalRoleAssigned -= HandleLocalRoleAssigned;
        }
    }

    private void HandleSessionStatusChanged(NetworkSessionStatus status)
    {
        Debug.Log($"[NetworkDebug] Session status changed: {status}", this);
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[NetworkDebug] Client connected: {clientId}", this);
        LogNetworkState("Client connected");
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkDebug] Client disconnected: {clientId}", this);
        LogNetworkState("Client disconnected");
    }

    private void HandleLocalRoleAssigned(TimelineRole role)
    {
        Debug.Log($"[NetworkDebug] Local timeline role assigned: {role}", this);
    }

    private void LogNetworkState(string reason)
    {
        Debug.Log(
            $"[NetworkDebug] {reason} | " +
            $"Session={GetSessionStatusText()}, " +
            $"Mode={GetNetworkModeText()}, " +
            $"LocalClientId={GetLocalClientIdText()}, " +
            $"ConnectedClients={GetConnectedClientCountText()}, " +
            $"LocalRole={GetLocalRoleText()}, " +
            $"RoleManagerSpawned={GetRoleManagerSpawnedText()}",
            this);
    }

    private string GetSessionStatusText()
    {
        return _sessionController != null ? _sessionController.Status.ToString() : "No SessionController";
    }

    private string GetNetworkModeText()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return "No NetworkManager";
        }

        if (!networkManager.IsListening)
        {
            return "Offline";
        }

        if (networkManager.IsHost)
        {
            return "Host";
        }

        if (networkManager.IsServer)
        {
            return "Server";
        }

        return networkManager.IsClient ? "Client" : "Unknown";
    }

    private string GetLocalClientIdText()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager != null && networkManager.IsListening
            ? networkManager.LocalClientId.ToString()
            : "-";
    }

    private string GetConnectedClientCountText()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
        {
            return "0/2";
        }

        return $"{networkManager.ConnectedClientsIds.Count}/2";
    }

    private string GetLocalRoleText()
    {
        if (_roleManager == null)
        {
            return "No RoleManager";
        }

        TimelineRole role = _roleManager.LocalTimelineRole;
        return role == TimelineRole.None ? "Not Assigned" : role.ToString();
    }

    private string GetRoleManagerSpawnedText()
    {
        return _roleManager != null ? _roleManager.IsSpawned.ToString() : "No RoleManager";
    }
    }
}
