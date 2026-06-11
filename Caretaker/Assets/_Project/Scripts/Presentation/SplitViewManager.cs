using Caretaker.Core;
using Caretaker.Gameplay;
using Caretaker.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private const string DIVIDER_CANVAS_NAME = "Split View Divider Canvas";
        private const string DIVIDER_IMAGE_NAME = "Split View Divider";

        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private SessionRoleManager _roleManager;
        [SerializeField] private Camera _mainCamera;
        [Tooltip("0이면 카메라 시야 안에서 움직일 수 있는 최대 간격을 자동으로 사용합니다.")]
        [SerializeField] [Min(0f)] private float _maximumPlayerSeparation = 7f;
        [SerializeField] [Min(0f)] private float _cameraBoundaryPadding = 1f;
        [SerializeField] [Min(0f)] private float _dividerThicknessPixels = 16f;
        [SerializeField] private Color _dividerColor = Color.black;

        private RemoteTimelineView _remoteTimelineView;
        private TimelineCameraRig _mainCameraRig;
        private GameObject _dividerCanvasObject;
        private RectTransform _dividerRect;
        private Image _dividerImage;
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
            EnsureDividerComponents();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            EnsureCameraComponents();
            EnsureDividerComponents();

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
            Transform localPlayer = localRole == TimelineRole.Past ? _pastPlayer : _futurePlayer;
            Transform remotePlayer = remoteRole == TimelineRole.Past ? _pastPlayer : _futurePlayer;
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
                localPlayer,
                localBasePosition,
                true,
                CalculateMaximumPlayerSeparation(
                    localTemplate,
                    localViewport,
                    remoteTemplate,
                    remoteViewport));

            _remoteTimelineView.Show(
                remoteTemplate,
                _pastPlayer,
                _futurePlayer,
                remotePlayer,
                remoteViewport,
                remoteBasePosition);
            ShowDivider();
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
            HideDivider();
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
            HideDivider();
            _isSplitViewActive = false;
        }

        private void RefreshObservedPlayers()
        {
            NetworkPlayerOwnerGate[] players =
                FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsInactive.Include);
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

        private void EnsureDividerComponents()
        {
            if (_dividerCanvasObject == null)
            {
                _dividerCanvasObject = new GameObject(DIVIDER_CANVAS_NAME);
                _dividerCanvasObject.transform.SetParent(transform, false);

                Canvas canvas = _dividerCanvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;
            }

            if (_dividerImage == null)
            {
                GameObject dividerObject = new(DIVIDER_IMAGE_NAME);
                dividerObject.transform.SetParent(_dividerCanvasObject.transform, false);
                _dividerRect = dividerObject.AddComponent<RectTransform>();
                _dividerImage = dividerObject.AddComponent<Image>();
                _dividerImage.raycastTarget = false;
            }

            RefreshDividerStyle();
            _dividerCanvasObject.SetActive(false);
        }

        private void ShowDivider()
        {
            EnsureDividerComponents();
            RefreshDividerStyle();
            _dividerCanvasObject.SetActive(true);
        }

        private void HideDivider()
        {
            if (_dividerCanvasObject != null)
            {
                _dividerCanvasObject.SetActive(false);
            }
        }

        private void RefreshDividerStyle()
        {
            if (_dividerRect == null || _dividerImage == null)
            {
                return;
            }

            _dividerRect.anchorMin = new Vector2(0f, 0.5f);
            _dividerRect.anchorMax = new Vector2(1f, 0.5f);
            _dividerRect.pivot = new Vector2(0.5f, 0.5f);
            _dividerRect.anchoredPosition = Vector2.zero;
            _dividerRect.sizeDelta = new Vector2(0f, _dividerThicknessPixels);
            _dividerImage.color = _dividerColor;
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

        private float CalculateMaximumPlayerSeparation(
            Camera localCamera,
            Rect localViewport,
            Camera remoteCamera,
            Rect remoteViewport)
        {
            float localHalfWidth = GetResolutionIndependentHalfWidth(
                localCamera,
                localViewport);
            float remoteHalfWidth = GetResolutionIndependentHalfWidth(
                remoteCamera,
                remoteViewport);
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
            float visibleSeparation = Mathf.Max(
                0f,
                narrowestHalfWidth - boundaryPadding);

            if (configuredSeparation <= 0f)
            {
                return visibleSeparation;
            }

            return Mathf.Min(configuredSeparation, visibleSeparation);
        }

        private static float GetResolutionIndependentHalfWidth(
            Camera camera,
            Rect viewport)
        {
            if (camera == null || !camera.orthographic)
            {
                return 8f;
            }

            float screenAspect = Screen.height > 0
                ? (float)Screen.width / Screen.height
                : 16f / 9f;
            return ResolveVisibleHalfWidth(
                camera.orthographicSize,
                screenAspect,
                viewport);
        }

        /// <summary>실제 화면 비율과 viewport를 기준으로 직교 카메라의 가로 반경을 계산합니다.</summary>
        public static float ResolveVisibleHalfWidth(
            float orthographicSize,
            float screenAspect,
            Rect viewport)
        {
            float viewportAspectMultiplier = viewport.height > 0f
                ? viewport.width / viewport.height
                : 1f;

            // 현재 화면과 스플릿 viewport 비율을 함께 반영해 실제 가시 경계를 계산합니다.
            return Mathf.Max(0f, orthographicSize)
                * Mathf.Max(0.1f, screenAspect)
                * Mathf.Max(0.1f, viewportAspectMultiplier);
        }
    }
}
