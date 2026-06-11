using Caretaker.Core;
using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Moves a single wall across both Phase 3 timelines and fails the escape when it catches either player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Phase3Chaser : MonoBehaviour
    {
        private const string SPAWN_POINT_NAME = "SpawnPoint";

        [SerializeField] [Min(0f)] private float _chaseSpeed = 4f;
        [SerializeField] [Min(0f)] private float _startDistance = 8f;
        [SerializeField] [Min(0f)] private float _captureDistance = 0.75f;

        private GameFlowManager _gameFlowManager;
        private Transform _futurePlayer;
        private float _futureProgressOriginX;
        private Transform _futureTimelineVisual;
        private Transform _pastPlayer;
        private float _pastProgressOriginX;
        private Transform _pastTimelineVisual;
        private float _startTime;
        private float _startProgressX;
        private bool _failureReported;
        private bool _isChaseStarted;

        private void OnEnable()
        {
            ResolveDependencies();
            NetworkPlayerOwnerGate.OnObservedPlayerSpawned += HandleObservedPlayerSpawned;
            NetworkPlayerOwnerGate.OnObservedPlayerDespawned += HandleObservedPlayerDespawned;
            RefreshObservedPlayers();
            TryStartChase();
        }

        private void FixedUpdate()
        {
            TryStartChase();
            if (!_isChaseStarted)
            {
                return;
            }

            float wallProgressX = CalculateSharedX(
                _startProgressX,
                _chaseSpeed,
                Time.fixedTime - _startTime);
            Vector3 position = transform.position;
            position.x = ResolveTimelineWorldX(TimelineRole.Past, wallProgressX);
            transform.position = position;
            ApplyTimelineVisualOffsets();

            if (!IsHostAuthority() || _failureReported)
            {
                return;
            }

            if (IsPlayerCaught(wallProgressX, GetPlayerProgressX(_pastPlayer, TimelineRole.Past), _captureDistance)
                || IsPlayerCaught(wallProgressX, GetPlayerProgressX(_futurePlayer, TimelineRole.Future), _captureDistance))
            {
                ReportFailure();
            }
        }

        private void OnDisable()
        {
            NetworkPlayerOwnerGate.OnObservedPlayerSpawned -= HandleObservedPlayerSpawned;
            NetworkPlayerOwnerGate.OnObservedPlayerDespawned -= HandleObservedPlayerDespawned;
            ResetChase();
        }

        /// <summary>Calculates the wall position from its start position, speed, and elapsed time.</summary>
        public static float CalculateSharedX(float startX, float speed, float elapsedSeconds)
        {
            return startX + (Mathf.Max(0f, speed) * Mathf.Max(0f, elapsedSeconds));
        }

        /// <summary>Returns whether the wall has reached a player.</summary>
        public static bool IsPlayerCaught(float wallX, float playerX, float captureDistance)
        {
            return wallX >= playerX - Mathf.Max(0f, captureDistance);
        }

        /// <summary>시간대별 추격벽 시각 요소를 설정한다.</summary>
        public void ConfigureTimelineVisuals(Transform pastTimelineVisual, Transform futureTimelineVisual)
        {
            _pastTimelineVisual = pastTimelineVisual;
            _futureTimelineVisual = futureTimelineVisual;
            ApplyTimelineVisualOffsets();
        }

        private void HandleObservedPlayerSpawned(NetworkPlayerOwnerGate player)
        {
            BindPlayer(player);
            TryStartChase();
        }

        private void HandleObservedPlayerDespawned(NetworkPlayerOwnerGate player)
        {
            if (player == null)
            {
                return;
            }

            if (player.transform == _pastPlayer)
            {
                _pastPlayer = null;
            }

            if (player.transform == _futurePlayer)
            {
                _futurePlayer = null;
            }

            if (_pastPlayer == null || _futurePlayer == null)
            {
                ResetChase();
            }
        }

        private void RefreshObservedPlayers()
        {
            NetworkPlayerOwnerGate[] players =
                FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                BindPlayer(players[i]);
            }
        }

        private void BindPlayer(NetworkPlayerOwnerGate player)
        {
            if (player == null)
            {
                return;
            }

            switch (player.TimelineRole)
            {
                case TimelineRole.Past:
                    _pastPlayer = player.transform;
                    break;
                case TimelineRole.Future:
                    _futurePlayer = player.transform;
                    break;
            }
        }

        private void TryStartChase()
        {
            if (_isChaseStarted || _pastPlayer == null || _futurePlayer == null)
            {
                return;
            }

            RefreshProgressOrigins();
            float slowerPlayerProgressX = Mathf.Min(
                GetPlayerProgressX(_pastPlayer, TimelineRole.Past),
                GetPlayerProgressX(_futurePlayer, TimelineRole.Future));
            _startProgressX = slowerPlayerProgressX - _startDistance;
            _startTime = Time.fixedTime;
            _failureReported = false;
            _isChaseStarted = true;

            Vector3 position = transform.position;
            position.x = ResolveTimelineWorldX(TimelineRole.Past, _startProgressX);
            transform.position = position;
            ApplyTimelineVisualOffsets();
        }

        private void ReportFailure()
        {
            ResolveDependencies();
            if (_gameFlowManager != null
                && _gameFlowManager.ReportEscapeFailure("Phase 3 chase wall caught a player."))
            {
                _failureReported = true;
            }
        }

        private void ResetChase()
        {
            _isChaseStarted = false;
            _failureReported = false;
        }

        private void ResolveDependencies()
        {
            if (_gameFlowManager == null)
            {
                _gameFlowManager = FindAnyObjectByType<GameFlowManager>();
            }
        }

        private void RefreshProgressOrigins()
        {
            _pastProgressOriginX = ResolveProgressOriginX(TimelineRole.Past, _pastPlayer);
            _futureProgressOriginX = ResolveProgressOriginX(TimelineRole.Future, _futurePlayer);
        }

        private float GetPlayerProgressX(Transform player, TimelineRole timelineRole)
        {
            return player != null
                ? player.position.x - ResolveOriginX(timelineRole)
                : float.PositiveInfinity;
        }

        private float ResolveTimelineWorldX(TimelineRole timelineRole, float progressX)
        {
            return ResolveOriginX(timelineRole) + progressX;
        }

        private float ResolveOriginX(TimelineRole timelineRole)
        {
            return timelineRole == TimelineRole.Future
                ? _futureProgressOriginX
                : _pastProgressOriginX;
        }

        private void ApplyTimelineVisualOffsets()
        {
            SetLocalX(_pastTimelineVisual, 0f);
            SetLocalX(_futureTimelineVisual, _futureProgressOriginX - _pastProgressOriginX);
        }

        private static void SetLocalX(Transform target, float localX)
        {
            if (target == null)
            {
                return;
            }

            Vector3 localPosition = target.localPosition;
            localPosition.x = localX;
            target.localPosition = localPosition;
        }

        private static float ResolveProgressOriginX(TimelineRole timelineRole, Transform fallbackPlayer)
        {
            Scene scene = SceneManager.GetSceneByName(SceneLoader.GetPhaseSceneName(PhaseId.Phase3, timelineRole));
            if (TryFindSpawnPoint(scene, out Transform spawnPoint))
            {
                return spawnPoint.position.x;
            }

            return fallbackPlayer != null ? fallbackPlayer.position.x : 0f;
        }

        private static bool TryFindSpawnPoint(Scene scene, out Transform spawnPoint)
        {
            spawnPoint = null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Transform[] transforms = rootObjects[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == SPAWN_POINT_NAME)
                    {
                        spawnPoint = transforms[j];
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsHostAuthority()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        }
    }
}
