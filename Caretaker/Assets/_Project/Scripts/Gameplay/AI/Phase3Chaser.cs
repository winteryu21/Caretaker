using Caretaker.Core;
using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Moves a single wall across both Phase 3 timelines and fails the escape when it catches either player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Phase3Chaser : MonoBehaviour
    {
        [SerializeField] [Min(0f)] private float _chaseSpeed = 4f;
        [SerializeField] [Min(0f)] private float _startDistance = 8f;
        [SerializeField] [Min(0f)] private float _captureDistance = 0.75f;

        private GameFlowManager _gameFlowManager;
        private Transform _futurePlayer;
        private Transform _pastPlayer;
        private float _startTime;
        private float _startX;
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

            float wallX = CalculateSharedX(
                _startX,
                _chaseSpeed,
                Time.fixedTime - _startTime);
            Vector3 position = transform.position;
            position.x = wallX;
            transform.position = position;

            if (!IsHostAuthority() || _failureReported)
            {
                return;
            }

            if (IsPlayerCaught(wallX, GetPlayerX(_pastPlayer), _captureDistance)
                || IsPlayerCaught(wallX, GetPlayerX(_futurePlayer), _captureDistance))
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

            float slowerPlayerX = Mathf.Min(_pastPlayer.position.x, _futurePlayer.position.x);
            _startX = slowerPlayerX - _startDistance;
            _startTime = Time.fixedTime;
            _failureReported = false;
            _isChaseStarted = true;

            Vector3 position = transform.position;
            position.x = _startX;
            transform.position = position;
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

        private static float GetPlayerX(Transform player)
        {
            return player != null ? player.position.x : float.PositiveInfinity;
        }

        private static bool IsHostAuthority()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        }
    }
}
