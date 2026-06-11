#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Caretaker.Gameplay;
using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Core
{
    /// <summary>
    /// 개발자도구의 Phase 3 스킵을 실제 플레이 검증 가능한 상태로 보정합니다.
    /// </summary>
    /// <remarks>
    /// 네트워크 세션이 없을 때는 Past/Future 디버그 플레이어를 생성하고,
    /// Phase 3 추격 벽이 씬에 없으면 개발용 화이트박스 벽을 생성합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Phase3DebugBootstrap : MonoBehaviour
    {
        private const string BOOTSTRAP_OBJECT_NAME = "Phase 3 Debug Bootstrap";
        private const string CHASER_OBJECT_NAME = "Debug Phase 3 Chase Wall";
        private const float FUTURE_WORLD_Y_OFFSET = 1000f;
        private static readonly Vector3 WALL_VISUAL_SCALE = new(0.5f, 12f, 1f);
        private static readonly Color WALL_COLOR = new(1f, 0.15f, 0.08f, 0.85f);

        private static Phase3DebugBootstrap _instance;
        private static Sprite _wallSprite;

        private GameObject _debugChaser;
        private Transform _futureWallVisual;
        private bool _futureSceneLoaded;
        private bool _isArmed;
        private Transform _pastWallVisual;
        private bool _pastSceneLoaded;

        /// <summary>오프라인 Phase 3 스킵 샌드박스가 활성화되었는지 반환합니다.</summary>
        public static bool IsOfflineSandboxActive =>
            _instance != null && _instance._isArmed && !IsNetworkSessionActive();

        /// <summary>오프라인 샌드박스에서 기준으로 사용할 로컬 시간대 역할입니다.</summary>
        public static TimelineRole OfflineSandboxLocalRole => TimelineRole.Past;

        /// <summary>다음 Phase 3 로드가 테스트 가능한 상태가 되도록 준비합니다.</summary>
        public static void Arm()
        {
            EnsureInstance().ArmInternal();
        }

        /// <summary>Phase 3 디버그 샌드박스 상태와 생성물을 정리합니다.</summary>
        public static void DisarmAndCleanup()
        {
            if (_instance == null)
            {
                return;
            }

            _instance.DisarmInternal();
        }

        private static Phase3DebugBootstrap EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            Phase3DebugBootstrap existing = FindAnyObjectByType<Phase3DebugBootstrap>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            GameObject bootstrapObject = new(BOOTSTRAP_OBJECT_NAME);
            _instance = bootstrapObject.AddComponent<Phase3DebugBootstrap>();
            return _instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterRuntimeSceneHook()
        {
            SceneManager.sceneLoaded -= HandleRuntimeSceneLoaded;
            SceneManager.sceneLoaded += HandleRuntimeSceneLoaded;
        }

        private static void HandleRuntimeSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPhase3SceneName(scene.name))
            {
                Arm();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            NetworkPlayerOwnerGate.OnObservedPlayerSpawned += HandleObservedPlayerSpawned;
            NetworkPlayerOwnerGate.OnObservedPlayerDespawned += HandleObservedPlayerDespawned;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            NetworkPlayerOwnerGate.OnObservedPlayerSpawned -= HandleObservedPlayerSpawned;
            NetworkPlayerOwnerGate.OnObservedPlayerDespawned -= HandleObservedPlayerDespawned;
        }

        private void ArmInternal()
        {
            _isArmed = true;
            RefreshLoadedPhase3Scenes();
            EnableOfflineSpawnerSandbox();
            EnsureDebugChaser();
            RefreshWallVisualVerticalPositions();
        }

        private void DisarmInternal()
        {
            _isArmed = false;
            _pastSceneLoaded = false;
            _futureSceneLoaded = false;

            LocalWorldPlayerSpawner spawner = FindAnyObjectByType<LocalWorldPlayerSpawner>();
            spawner?.DisablePhase3DebugSandbox();

            if (_debugChaser != null)
            {
                Destroy(_debugChaser);
                _debugChaser = null;
            }

            _pastWallVisual = null;
            _futureWallVisual = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_isArmed)
            {
                return;
            }

            MarkPhase3SceneLoaded(scene.name, true);
            EnableOfflineSpawnerSandbox();
            EnsureDebugChaser();
            RefreshWallVisualVerticalPositions();
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            MarkPhase3SceneLoaded(scene.name, false);
        }

        private void HandleObservedPlayerSpawned(NetworkPlayerOwnerGate player)
        {
            RefreshWallVisualVerticalPositions();
        }

        private void HandleObservedPlayerDespawned(NetworkPlayerOwnerGate player)
        {
            RefreshWallVisualVerticalPositions();
        }

        private void RefreshLoadedPhase3Scenes()
        {
            _pastSceneLoaded = SceneManager
                .GetSceneByName(SceneLoader.GetPhaseSceneName(PhaseId.Phase3, TimelineRole.Past))
                .isLoaded;
            _futureSceneLoaded = SceneManager
                .GetSceneByName(SceneLoader.GetPhaseSceneName(PhaseId.Phase3, TimelineRole.Future))
                .isLoaded;
        }

        private void MarkPhase3SceneLoaded(string sceneName, bool isLoaded)
        {
            if (sceneName == SceneLoader.GetPhaseSceneName(PhaseId.Phase3, TimelineRole.Past))
            {
                _pastSceneLoaded = isLoaded;
            }
            else if (sceneName == SceneLoader.GetPhaseSceneName(PhaseId.Phase3, TimelineRole.Future))
            {
                _futureSceneLoaded = isLoaded;
            }
        }

        private static void EnableOfflineSpawnerSandbox()
        {
            if (IsNetworkSessionActive())
            {
                return;
            }

            LocalWorldPlayerSpawner spawner = FindAnyObjectByType<LocalWorldPlayerSpawner>();
            spawner?.EnablePhase3DebugSandbox();
        }

        private void EnsureDebugChaser()
        {
            if (!_isArmed || !_pastSceneLoaded || !_futureSceneLoaded)
            {
                return;
            }

            if (_debugChaser != null)
            {
                RefreshWallVisualVerticalPositions();
                return;
            }

            if (FindAnyObjectByType<Phase3Chaser>() != null)
            {
                return;
            }

            _debugChaser = new GameObject(CHASER_OBJECT_NAME);
            Phase3Chaser chaser = _debugChaser.AddComponent<Phase3Chaser>();
            _pastWallVisual = CreateWallVisual(_debugChaser.transform, "Past Chase Wall Visual", 0f);
            _futureWallVisual = CreateWallVisual(
                _debugChaser.transform,
                "Future Chase Wall Visual",
                FUTURE_WORLD_Y_OFFSET);
            chaser.ConfigureTimelineVisuals(_pastWallVisual, _futureWallVisual);
            RefreshWallVisualVerticalPositions();
        }

        private void RefreshWallVisualVerticalPositions()
        {
            if (_debugChaser == null)
            {
                return;
            }

            NetworkPlayerOwnerGate[] players =
                FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsInactive.Include);
            for (int i = 0; i < players.Length; i++)
            {
                NetworkPlayerOwnerGate player = players[i];
                if (player == null)
                {
                    continue;
                }

                switch (player.TimelineRole)
                {
                    case TimelineRole.Past:
                        MoveWallVisualToWorldY(_pastWallVisual, player.transform.position.y);
                        break;
                    case TimelineRole.Future:
                        MoveWallVisualToWorldY(_futureWallVisual, player.transform.position.y);
                        break;
                }
            }
        }

        private void MoveWallVisualToWorldY(Transform wallVisual, float worldY)
        {
            if (wallVisual == null)
            {
                return;
            }

            Vector3 localPosition = wallVisual.localPosition;
            localPosition.y = worldY - _debugChaser.transform.position.y;
            wallVisual.localPosition = localPosition;
        }

        private static bool IsPhase3SceneName(string sceneName)
        {
            return sceneName == SceneLoader.GetPhaseSceneName(PhaseId.Phase3, TimelineRole.Past)
                || sceneName == SceneLoader.GetPhaseSceneName(PhaseId.Phase3, TimelineRole.Future);
        }

        private static Transform CreateWallVisual(Transform parent, string name, float yOffset)
        {
            GameObject visual = new(name);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = new Vector3(0f, yOffset, 0f);
            visual.transform.localScale = WALL_VISUAL_SCALE;

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWallSprite();
            renderer.color = WALL_COLOR;
            renderer.sortingOrder = 100;

            return visual.transform;
        }

        private static Sprite GetWallSprite()
        {
            if (_wallSprite != null)
            {
                return _wallSprite;
            }

            Texture2D texture = new(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _wallSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            _wallSprite.hideFlags = HideFlags.HideAndDontSave;
            return _wallSprite;
        }

        private static bool IsNetworkSessionActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }
    }
}
#endif
