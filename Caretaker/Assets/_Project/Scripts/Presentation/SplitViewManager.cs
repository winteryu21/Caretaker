using Caretaker.Core;
using Caretaker.Gameplay;
using Caretaker.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 두 카메라를 직접 사용해 Past를 상단, Future를 하단에 표시한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SplitViewManager : MonoBehaviour
    {
        private static readonly Rect TOP_VIEWPORT = new(0f, 0.5f, 1f, 0.5f);
        private static readonly Rect BOTTOM_VIEWPORT = new(0f, 0f, 1f, 0.5f);

        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private SessionRoleManager _roleManager;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] [Min(0f)] private float _maximumPlayerSeparation = 7f;
        [SerializeField] [Min(0f)] private float _cameraBoundaryPadding = 1f;
        [SerializeField] [Min(0.1f)] private float _referenceViewportAspect = 16f / 9f;

        private RemoteTimelineView _remoteTimelineView;
        private TimelineCameraRig _mainCameraRig;
        private Transform _pastPlayer;
        private Transform _futurePlayer;
        private bool _futureSceneLoaded;
        private bool _pastSceneLoaded;
        private bool _isSplitViewActive;

        /// <summary>현재 Phase 3 스플릿뷰가 활성화되어 있는지 여부.</summary>
        public bool IsActive => _isSplitViewActive;

        private void Awake()
        {
            ResolveDependencies();
            EnsureCameraComponents();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            EnsureCameraComponents();

            if (_sceneLoader != null)
            {
                _sceneLoader.OnPhaseSceneLoaded += HandlePhaseSceneLoaded;
            }

            NetworkPlayerOwnerGate.OnObservedPlayerSpawned += HandleObservedPlayerSpawned;
            NetworkPlayerOwnerGate.OnObservedPlayerDespawned += HandleObservedPlayerDespawned;
            RefreshLoadedScenes();
            RefreshObservedPlayers();
            EnableSplitView();
        }

        private void OnDisable()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.OnPhaseSceneLoaded -= HandlePhaseSceneLoaded;
            }

            NetworkPlayerOwnerGate.OnObservedPlayerSpawned -= HandleObservedPlayerSpawned;
            NetworkPlayerOwnerGate.OnObservedPlayerDespawned -= HandleObservedPlayerDespawned;
            DisableSplitView();
        }

        /// <summary>두 시간대 씬과 플레이어가 모두 준비되면 상하 스플릿뷰를 활성화한다.</summary>
        public void EnableSplitView()
        {
            ResolveDependencies();
            EnsureCameraComponents();

            if (!_pastSceneLoaded
                || !_futureSceneLoaded
                || _pastPlayer == null
                || _futurePlayer == null
                || _mainCamera == null
                || !HasTimelineRoleSource()
                || _remoteTimelineView == null)
            {
                return;
            }

            TimelineRole localRole = ResolveLocalTimelineRole();
            if (localRole is not (TimelineRole.Past or TimelineRole.Future))
            {
                return;
            }

            TimelineRole remoteRole = localRole == TimelineRole.Past
                ? TimelineRole.Future
                : TimelineRole.Past;
            Camera localTemplate = FindSceneCamera(SceneLoader.GetPhaseSceneName(PhaseId.Phase3, localRole));
            Camera remoteTemplate = FindSceneCamera(SceneLoader.GetPhaseSceneName(PhaseId.Phase3, remoteRole));
            if (localTemplate == null || remoteTemplate == null)
            {
                return;
            }

            Rect localViewport = localRole == TimelineRole.Past ? TOP_VIEWPORT : BOTTOM_VIEWPORT;
            Rect remoteViewport = remoteRole == TimelineRole.Past ? TOP_VIEWPORT : BOTTOM_VIEWPORT;
            Vector3 localBasePosition = ResolveCameraBasePosition(localTemplate, localRole);
            Vector3 remoteBasePosition = ResolveCameraBasePosition(remoteTemplate, remoteRole);

            _mainCamera.CopyFrom(localTemplate);
            _mainCamera.targetTexture = null;
            _mainCamera.rect = localViewport;
            _mainCamera.depth = -1f;
            _mainCamera.transform.SetPositionAndRotation(
                localBasePosition,
                localTemplate.transform.rotation);
            _mainCameraRig.Configure(
                _pastPlayer,
                _futurePlayer,
                localBasePosition,
                true,
                CalculateMaximumPlayerSeparation(localTemplate, remoteTemplate));

            _remoteTimelineView.Show(
                remoteTemplate,
                _pastPlayer,
                _futurePlayer,
                remoteViewport,
                remoteBasePosition);
            _isSplitViewActive = true;
        }

        /// <summary>Persistent 메인 카메라를 전체 화면으로 복원한다.</summary>
        public void DisableSplitView()
        {
            if (_mainCamera != null)
            {
                _mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }

            _mainCameraRig?.StopFollowing();
            _remoteTimelineView?.Hide();
            _isSplitViewActive = false;
            _pastSceneLoaded = false;
            _futureSceneLoaded = false;
        }

        private void HandlePhaseSceneLoaded(PhaseId phaseId, TimelineRole timelineRole, string sceneName)
        {
            if (phaseId != PhaseId.Phase3)
            {
                DisableSplitView();
                return;
            }

            if (timelineRole == TimelineRole.Past)
            {
                _pastSceneLoaded = true;
            }
            else if (timelineRole == TimelineRole.Future)
            {
                _futureSceneLoaded = true;
            }

            EnableSplitView();
        }

        private void HandleObservedPlayerSpawned(NetworkPlayerOwnerGate player)
        {
            RegisterObservedPlayer(player);
            EnableSplitView();
        }

        private void HandleObservedPlayerDespawned(NetworkPlayerOwnerGate player)
        {
            bool trackedPlayerDespawned = false;
            if (player != null && _pastPlayer == player.transform)
            {
                _pastPlayer = null;
                trackedPlayerDespawned = true;
            }

            if (player != null && _futurePlayer == player.transform)
            {
                _futurePlayer = null;
                trackedPlayerDespawned = true;
            }

            if (trackedPlayerDespawned)
            {
                SuspendSplitViewForMissingPlayer();
            }
        }

        private void SuspendSplitViewForMissingPlayer()
        {
            if (_mainCamera != null)
            {
                _mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }

            _mainCameraRig?.StopFollowing();
            _remoteTimelineView?.Hide();
            _isSplitViewActive = false;
        }

        private void RefreshObservedPlayers()
        {
            NetworkPlayerOwnerGate[] players = FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                RegisterObservedPlayer(players[i]);
            }
        }

        private void RefreshLoadedScenes()
        {
            _pastSceneLoaded = SceneManager
                .GetSceneByName(SceneLoader.GetPhaseSceneName(PhaseId.Phase3, TimelineRole.Past))
                .isLoaded;
            _futureSceneLoaded = SceneManager
                .GetSceneByName(SceneLoader.GetPhaseSceneName(PhaseId.Phase3, TimelineRole.Future))
                .isLoaded;
        }

        private void RegisterObservedPlayer(NetworkPlayerOwnerGate player)
        {
            if (player == null)
            {
                return;
            }

            if (player.TimelineRole == TimelineRole.Past)
            {
                _pastPlayer = player.transform;
            }
            else if (player.TimelineRole == TimelineRole.Future)
            {
                _futurePlayer = player.transform;
            }
        }

        private TimelineRole ResolveLocalTimelineRole()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Phase3DebugBootstrap.IsOfflineSandboxActive)
            {
                return Phase3DebugBootstrap.OfflineSandboxLocalRole;
            }
#endif

            return _roleManager != null ? _roleManager.LocalTimelineRole : TimelineRole.None;
        }

        private bool HasTimelineRoleSource()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Phase3DebugBootstrap.IsOfflineSandboxActive)
            {
                return true;
            }
#endif

            return _roleManager != null;
        }

        private void ResolveDependencies()
        {
            if (_sceneLoader == null)
            {
                _sceneLoader = FindAnyObjectByType<SceneLoader>();
            }

            if (_roleManager == null)
            {
                _roleManager = FindAnyObjectByType<SessionRoleManager>();
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        private void EnsureCameraComponents()
        {
            if (_mainCamera != null && _mainCameraRig == null)
            {
                _mainCameraRig = _mainCamera.GetComponent<TimelineCameraRig>();
                if (_mainCameraRig == null)
                {
                    _mainCameraRig = _mainCamera.gameObject.AddComponent<TimelineCameraRig>();
                }
            }

            if (_remoteTimelineView == null)
            {
                _remoteTimelineView = GetComponentInChildren<RemoteTimelineView>(true);
            }

            if (_remoteTimelineView == null)
            {
                GameObject remoteViewObject = new("Remote Timeline View");
                remoteViewObject.transform.SetParent(transform, false);
                _remoteTimelineView = remoteViewObject.AddComponent<RemoteTimelineView>();
            }

            _remoteTimelineView.Initialize();
        }

        private static Camera FindSceneCamera(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            Camera fallback = null;
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Camera[] cameras = rootObjects[i].GetComponentsInChildren<Camera>(true);
                for (int j = 0; j < cameras.Length; j++)
                {
                    fallback ??= cameras[j];
                    if (cameras[j].CompareTag("MainCamera"))
                    {
                        return cameras[j];
                    }
                }
            }

            return fallback;
        }

        private float CalculateMaximumPlayerSeparation(Camera localCamera, Camera remoteCamera)
        {
            float localHalfWidth = GetResolutionIndependentHalfWidth(
                localCamera,
                _referenceViewportAspect);
            float remoteHalfWidth = GetResolutionIndependentHalfWidth(
                remoteCamera,
                _referenceViewportAspect);
            return ResolveMaximumPlayerSeparation(
                _maximumPlayerSeparation,
                localHalfWidth,
                remoteHalfWidth,
                _cameraBoundaryPadding);
        }

        private Vector3 ResolveCameraBasePosition(Camera templateCamera, TimelineRole timelineRole)
        {
            Vector3 basePosition = templateCamera.transform.position;
            Transform player = timelineRole == TimelineRole.Past ? _pastPlayer : _futurePlayer;
            if (player == null || !templateCamera.orthographic)
            {
                return basePosition;
            }

            float verticalDistance = Mathf.Abs(player.position.y - basePosition.y);
            if (verticalDistance > templateCamera.orthographicSize)
            {
                basePosition.y = player.position.y;
            }

            return basePosition;
        }

        /// <summary>설정된 거리와 두 카메라 중 좁은 월드 범위를 기준으로 최대 간격을 계산합니다.</summary>
        public static float ResolveMaximumPlayerSeparation(
            float configuredSeparation,
            float localHalfWidth,
            float remoteHalfWidth,
            float boundaryPadding)
        {
            float narrowestHalfWidth = Mathf.Min(localHalfWidth, remoteHalfWidth);
            return Mathf.Max(
                0f,
                Mathf.Min(configuredSeparation, narrowestHalfWidth - boundaryPadding));
        }

        private static float GetResolutionIndependentHalfWidth(
            Camera camera,
            float referenceViewportAspect)
        {
            if (camera == null || !camera.orthographic)
            {
                return 8f;
            }

            // 실제 해상도 대신 공통 기준 aspect를 사용해 모든 클라이언트가 같은 경계를 갖습니다.
            return camera.orthographicSize * Mathf.Max(0.1f, referenceViewportAspect);
        }
    }
}
