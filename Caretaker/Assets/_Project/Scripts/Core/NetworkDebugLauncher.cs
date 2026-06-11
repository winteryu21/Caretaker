using Caretaker.Shared;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Caretaker.Core
{
    public class NetworkDebugLauncher : MonoBehaviour
    {
        [SerializeField] private NetworkSessionController _sessionController;
        [SerializeField] private SessionRoleManager _roleManager;

        [Header("Lobby UI")]
        [SerializeField] private TMP_Text _yourIpText;
        [SerializeField] private TMP_InputField _hostIpInput;
        [SerializeField] private TMP_InputField _hostPortInput;
        [SerializeField] private TMP_Text _hostIpLabel;
        [SerializeField] private TMP_Text _hostPortLabel;

        [Header("Debug Panel")]
        [SerializeField] private bool _showDebugPanel = false;
        [SerializeField] private Vector2 _panelPosition = new Vector2(16f, 16f);
        [SerializeField] private Vector2 _panelSize = new Vector2(420f, 310f);

        private string _debugClientAddressInput = "127.0.0.1";
        private string _debugPortInput = "7777";

        private void Awake()
        {
            ResolveDependencies();
            RefreshConnectionUi();
            Subscribe();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                _showDebugPanel = !_showDebugPanel;
            }

            RefreshClientConnectionInputState();
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
            RefreshConnectionUi();
            LogNetworkState("StartHost requested");
        }

        public void StartClient()
        {
            if (!ApplyConnectionInput())
            {
                return;
            }

            if (!TryGetSessionController(out NetworkSessionController sessionController))
            {
                return;
            }

            sessionController.StartClientSession();
            RefreshClientConnectionInputState();
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
            RefreshConnectionUi();
            RefreshClientConnectionInputState();
            LogNetworkState("Shutdown requested");
        }

        public void RefreshConnectionUi()
        {
            ResolveDependencies();
            ResolveUiReferences();

            if (_sessionController == null)
            {
                return;
            }

            string localIp = _sessionController.GetLocalLanAddress();
            if (_yourIpText != null)
            {
                _yourIpText.text = $"Your IP : {localIp}";
            }

            if (_hostIpInput != null && string.IsNullOrWhiteSpace(_hostIpInput.text))
            {
                _hostIpInput.text = _sessionController.ConnectionAddress;
            }

            if (_hostPortInput != null && string.IsNullOrWhiteSpace(_hostPortInput.text))
            {
                _hostPortInput.text = _sessionController.ConnectionPort.ToString();
            }

            _debugClientAddressInput = _sessionController.ConnectionAddress;
            _debugPortInput = _sessionController.ConnectionPort.ToString();
            RefreshClientConnectionInputState();
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
            GUILayout.Label($"Your IP: {GetLocalIpText()}");
            GUILayout.Label($"Target: {GetConnectionAddressText()}:{GetPortText()}");
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Host"))
            {
                StartHost();
            }

            if (GUILayout.Button("Client"))
            {
                ApplyDebugConnectionInput();
                StartClient();
            }

            if (GUILayout.Button("Shutdown"))
            {
                Shutdown();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Client Connect");
            GUILayout.BeginHorizontal();
            GUILayout.Label("IP", GUILayout.Width(32f));
            _debugClientAddressInput = GUILayout.TextField(_debugClientAddressInput);
            GUILayout.Label("Port", GUILayout.Width(40f));
            _debugPortInput = GUILayout.TextField(_debugPortInput, GUILayout.Width(72f));
            GUILayout.EndHorizontal();

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

        private bool ApplyConnectionInput()
        {
            if (!TryGetSessionController(out NetworkSessionController sessionController))
            {
                return false;
            }

            string address = _hostIpInput != null ? _hostIpInput.text : _debugClientAddressInput;
            string portText = _hostPortInput != null ? _hostPortInput.text : _debugPortInput;
            return ApplyConnectionInput(sessionController, address, portText);
        }

        private void ApplyDebugConnectionInput()
        {
            if (!TryGetSessionController(out NetworkSessionController sessionController))
            {
                return;
            }

            ApplyConnectionInput(sessionController, _debugClientAddressInput, _debugPortInput);
            if (_hostIpInput != null)
            {
                _hostIpInput.text = _debugClientAddressInput;
            }

            if (_hostPortInput != null)
            {
                _hostPortInput.text = _debugPortInput;
            }
        }

        private bool ApplyConnectionInput(
            NetworkSessionController sessionController,
            string address,
            string portText)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogWarning("Host IP is empty.", this);
                return false;
            }

            if (!ushort.TryParse(portText, out ushort port))
            {
                Debug.LogWarning($"Invalid network port: {portText}", this);
                return false;
            }

            sessionController.SetConnectionAddress(address);
            sessionController.SetConnectionPort(port);
            _debugClientAddressInput = sessionController.ConnectionAddress;
            _debugPortInput = sessionController.ConnectionPort.ToString();
            return true;
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

        private void ResolveUiReferences()
        {
            if (_yourIpText == null)
            {
                TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].gameObject.name == "IP" || texts[i].gameObject.name == "Your IP")
                    {
                        _yourIpText = texts[i];
                        break;
                    }
                }
            }
        }

        private void RefreshClientConnectionInputState()
        {
            bool interactable = ShouldEnableClientConnectionInput();
            if (_hostIpInput != null)
            {
                _hostIpInput.interactable = interactable;
            }

            if (_hostPortInput != null)
            {
                _hostPortInput.interactable = interactable;
            }

            SetLabelEnabled(_hostIpLabel, interactable);
            SetLabelEnabled(_hostPortLabel, interactable);
        }

        private static void SetLabelEnabled(TMP_Text label, bool enabled)
        {
            if (label == null)
            {
                return;
            }

            Color color = label.color;
            color.a = enabled ? 1f : 0.45f;
            label.color = color;
        }

        private static bool ShouldEnableClientConnectionInput()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
            {
                return true;
            }

            return networkManager.IsClient && !networkManager.IsHost && !networkManager.IsServer;
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
                $"RoleManagerSpawned={GetRoleManagerSpawnedText()}, " +
                $"Target={GetConnectionAddressText()}:{GetPortText()}",
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

        private string GetLocalIpText()
        {
            return _sessionController != null ? _sessionController.GetLocalLanAddress() : "No SessionController";
        }

        private string GetConnectionAddressText()
        {
            return _sessionController != null ? _sessionController.ConnectionAddress : "-";
        }

        private string GetPortText()
        {
            return _sessionController != null ? _sessionController.ConnectionPort.ToString() : "-";
        }
    }
}
