#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Caretaker.Core
{
    /// <summary>
    /// 개발 빌드와 Editor Play Mode에서 어느 씬에서든 호출 가능한 개발자 도구 오버레이입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeveloperToolsOverlay : MonoBehaviour
    {
        private const string TOOL_OBJECT_NAME = "Caretaker Developer Tools";
        private const int WINDOW_ID = 7311;

        [SerializeField] private bool _isVisible;
        [SerializeField] private Rect _windowRect = new(16f, 16f, 420f, 260f);

        private GameFlowManager _gameFlowManager;
        private SessionRoleManager _roleManager;
        private Vector2 _scrollPosition;

        /// <summary>현재 개발자 도구 패널이 표시 중인지 반환합니다.</summary>
        public bool IsVisible => _isVisible;

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void RegisterEditorPlayModeHook()
        {
            EditorApplication.playModeStateChanged -= HandleEditorPlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandleEditorPlayModeStateChanged;
        }

        private static void HandleEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EnsureRuntimeInstance();
            }
        }

        [MenuItem("Caretaker/Developer Tools/Skip To Phase 3", priority = 100)]
        private static void SkipToPhase3FromEditorMenu()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Phase 3 developer skip requires Play Mode.");
                return;
            }

            GameFlowManager gameFlowManager = FindAnyObjectByType<GameFlowManager>();
            if (gameFlowManager == null)
            {
                Debug.LogWarning("Developer Tools cannot find GameFlowManager.");
                return;
            }

            Phase3DebugBootstrap.Arm();
            if (!gameFlowManager.TryDebugTransitionPhase(PhaseId.Phase3))
            {
                Debug.LogWarning("Developer Tools failed to transition to Phase 3.");
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterRuntimeSceneHook()
        {
            SceneManager.sceneLoaded -= HandleRuntimeSceneLoaded;
            SceneManager.sceneLoaded += HandleRuntimeSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeInstance()
        {
            if (FindAnyObjectByType<DeveloperToolsOverlay>() != null)
            {
                return;
            }

            GameObject toolObject = new(TOOL_OBJECT_NAME);
            toolObject.AddComponent<DeveloperToolsOverlay>();
        }

        private static void HandleRuntimeSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRuntimeInstance();
        }

        private void Awake()
        {
            DeveloperToolsOverlay existing = FindAnyObjectByType<DeveloperToolsOverlay>();
            if (existing != null && existing != this)
            {
                Destroy(gameObject);
                return;
            }

            ResolveDependencies();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.backslashKey.wasPressedThisFrame)
            {
                _isVisible = !_isVisible;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            _windowRect = GUILayout.Window(
                WINDOW_ID,
                _windowRect,
                DrawWindow,
                "Caretaker Developer Tools");
        }

        private void DrawWindow(int windowId)
        {
            ResolveDependencies();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Label("Toggle: \\");
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");
            GUILayout.Label($"Network: {GetNetworkModeText()}");
            GUILayout.Label($"Local Role: {GetLocalRoleText()}");
            GUILayout.Label($"GameFlow: {GetGameFlowText()}");

            GUILayout.Space(8f);
            GUILayout.Label("Phase");
            DrawPhaseControls();

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 24f));
        }

        private void DrawPhaseControls()
        {
            bool canRequestPhase = CanRequestPhaseChange(out string disabledReason);
            using (new GuiEnabledScope(canRequestPhase))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Phase 1"))
                {
                    RequestPhaseChange(PhaseId.Phase1);
                }

                if (GUILayout.Button("Phase 2"))
                {
                    RequestPhaseChange(PhaseId.Phase2);
                }

                if (GUILayout.Button("Phase 3"))
                {
                    RequestPhaseChange(PhaseId.Phase3);
                }

                GUILayout.EndHorizontal();

                if (GUILayout.Button("Skip To Next Phase"))
                {
                    RequestPhaseChange(GetNextPhase());
                }
            }

            if (!canRequestPhase)
            {
                GUILayout.Label(disabledReason);
            }
        }

        private void RequestPhaseChange(PhaseId targetPhase)
        {
            if (_gameFlowManager == null)
            {
                Debug.LogWarning("Developer Tools cannot find GameFlowManager.", this);
                return;
            }

            if (targetPhase == PhaseId.Phase3)
            {
                Phase3DebugBootstrap.Arm();
            }
            else
            {
                Phase3DebugBootstrap.DisarmAndCleanup();
            }

            if (!_gameFlowManager.TryDebugTransitionPhase(targetPhase))
            {
                Debug.LogWarning($"Developer Tools failed to transition phase: target={targetPhase}", this);
            }
        }

        private PhaseId GetNextPhase()
        {
            if (_gameFlowManager == null)
            {
                return PhaseId.Phase1;
            }

            return _gameFlowManager.CurrentPhase switch
            {
                PhaseId.Phase1 => PhaseId.Phase2,
                PhaseId.Phase2 => PhaseId.Phase3,
                _ => PhaseId.Phase3
            };
        }

        private bool CanRequestPhaseChange(out string disabledReason)
        {
            disabledReason = string.Empty;
            if (_gameFlowManager == null)
            {
                disabledReason = "GameFlowManager not found.";
                return false;
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || networkManager.IsServer)
            {
                return true;
            }

            disabledReason = "Phase controls are Host-only during network play.";
            return false;
        }

        private string GetGameFlowText()
        {
            return _gameFlowManager != null
                ? $"Current Phase: {_gameFlowManager.CurrentPhase}"
                : "GameFlowManager not found";
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

        private static string GetNetworkModeText()
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
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _gameFlowManager = null;
            _roleManager = null;
            ResolveDependencies();
        }

        private readonly struct GuiEnabledScope : System.IDisposable
        {
            private readonly bool _wasEnabled;

            public GuiEnabledScope(bool isEnabled)
            {
                _wasEnabled = GUI.enabled;
                GUI.enabled = isEnabled;
            }

            public void Dispose()
            {
                GUI.enabled = _wasEnabled;
            }
        }
    }
}
#endif
