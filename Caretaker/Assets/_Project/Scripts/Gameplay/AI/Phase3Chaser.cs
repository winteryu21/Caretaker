using Caretaker.Core;
using Caretaker.Shared;
using Unity.Netcode;
using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Phase 3에서 역할별 플레이어를 추격하며 두 시간대 로봇의 공통 X 진행도를 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Phase3Chaser : MonoBehaviour
    {
        [SerializeField] private TimelineRole _timelineRole;
        [SerializeField] [Min(0f)] private float _chaseSpeed = 4f;
        [SerializeField] [Min(0f)] private float _startDistance = 8f;
        [SerializeField] [Min(0f)] private float _captureDistance = 0.75f;
        [SerializeField] [Min(0f)] private float _initialSectionTimeoutSeconds;

        private static Phase3Chaser s_futureChaser;
        private static Phase3Chaser s_pastChaser;
        private static float s_sectionDeadline;
        private static float s_sharedStartTime;
        private static float s_sharedStartX;
        private static bool s_failureReported;
        private static bool s_hasSectionDeadline;
        private static bool s_isSharedChaseStarted;

        private GameFlowManager _gameFlowManager;
        private Transform _targetPlayer;
        private bool _hasAlignedToTargetY;

        /// <summary>이 로봇이 추적하는 시간대 역할입니다.</summary>
        public TimelineRole TimelineRole => _timelineRole;

        private bool IsLeader => _timelineRole == TimelineRole.Past;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedState()
        {
            s_futureChaser = null;
            s_pastChaser = null;
            s_sectionDeadline = 0f;
            s_sharedStartTime = 0f;
            s_sharedStartX = 0f;
            s_failureReported = false;
            s_hasSectionDeadline = false;
            s_isSharedChaseStarted = false;
        }

        private void OnEnable()
        {
            RegisterChaser();
            ResolveDependencies();
            NetworkPlayerOwnerGate.OnObservedPlayerSpawned += HandleObservedPlayerSpawned;
            NetworkPlayerOwnerGate.OnObservedPlayerDespawned += HandleObservedPlayerDespawned;
            RefreshObservedPlayers();
            TryStartSharedChase();
        }

        private void OnDisable()
        {
            NetworkPlayerOwnerGate.OnObservedPlayerSpawned -= HandleObservedPlayerSpawned;
            NetworkPlayerOwnerGate.OnObservedPlayerDespawned -= HandleObservedPlayerDespawned;
            UnregisterChaser();
        }

        private void FixedUpdate()
        {
            TryStartSharedChase();
            if (!s_isSharedChaseStarted)
            {
                return;
            }

            float sharedX = CalculateSharedX(
                s_sharedStartX,
                GetSharedChaseSpeed(),
                Time.fixedTime - s_sharedStartTime);
            Vector3 position = transform.position;
            position.x = sharedX;
            transform.position = position;

            if (!IsHostAuthority() || s_failureReported)
            {
                return;
            }

            if (IsPlayerCaught(sharedX, GetTargetPlayerX(), _captureDistance))
            {
                ReportSharedFailure($"Phase3 player caught: role={_timelineRole}");
                return;
            }

            if (IsLeader && IsSectionTimedOut(Time.fixedTime, s_hasSectionDeadline, s_sectionDeadline))
            {
                ReportSharedFailure("Phase3 section timeout");
            }
        }

        /// <summary>TBD 구간 제한 시간을 시작합니다. 0 이하면 타이머를 비활성화합니다.</summary>
        public void BeginSectionTimer(float timeoutSeconds)
        {
            if (!IsLeader)
            {
                s_pastChaser?.BeginSectionTimer(timeoutSeconds);
                return;
            }

            s_hasSectionDeadline = timeoutSeconds > 0f;
            s_sectionDeadline = s_hasSectionDeadline
                ? Time.fixedTime + timeoutSeconds
                : 0f;
        }

        /// <summary>시작 위치와 속도, 경과 시간으로 공통 추격 X를 계산합니다.</summary>
        public static float CalculateSharedX(float startX, float speed, float elapsedSeconds)
        {
            return startX + (Mathf.Max(0f, speed) * Mathf.Max(0f, elapsedSeconds));
        }

        /// <summary>로봇이 역할별 플레이어의 포획 거리 안에 도달했는지 반환합니다.</summary>
        public static bool IsPlayerCaught(float chaserX, float playerX, float captureDistance)
        {
            return chaserX >= playerX - Mathf.Max(0f, captureDistance);
        }

        /// <summary>활성 구간 타이머가 만료되었는지 반환합니다.</summary>
        public static bool IsSectionTimedOut(float currentTime, bool hasDeadline, float deadline)
        {
            return hasDeadline && currentTime >= deadline;
        }

        private void TryStartSharedChase()
        {
            if (s_isSharedChaseStarted
                || s_pastChaser == null
                || s_futureChaser == null
                || s_pastChaser._targetPlayer == null
                || s_futureChaser._targetPlayer == null)
            {
                return;
            }

            float slowerPlayerX = Mathf.Min(
                s_pastChaser._targetPlayer.position.x,
                s_futureChaser._targetPlayer.position.x);
            float sharedStartDistance = Mathf.Max(
                s_pastChaser._startDistance,
                s_futureChaser._startDistance);
            s_sharedStartX = slowerPlayerX - sharedStartDistance;
            s_sharedStartTime = Time.fixedTime;
            s_failureReported = false;
            s_isSharedChaseStarted = true;

            s_pastChaser.BeginSectionTimer(s_pastChaser._initialSectionTimeoutSeconds);
        }

        private void RegisterChaser()
        {
            if (_timelineRole == TimelineRole.Past)
            {
                s_pastChaser = this;
            }
            else if (_timelineRole == TimelineRole.Future)
            {
                s_futureChaser = this;
            }
            else
            {
                Debug.LogWarning("Phase3Chaser requires a Past or Future timeline role.", this);
            }
        }

        private void UnregisterChaser()
        {
            if (s_pastChaser == this)
            {
                s_pastChaser = null;
            }

            if (s_futureChaser == this)
            {
                s_futureChaser = null;
            }

            if (s_pastChaser != null || s_futureChaser != null)
            {
                return;
            }

            s_isSharedChaseStarted = false;
            s_failureReported = false;
            s_hasSectionDeadline = false;
            s_sectionDeadline = 0f;
        }

        private void HandleObservedPlayerSpawned(NetworkPlayerOwnerGate player)
        {
            TryBindTarget(player);
            TryStartSharedChase();
        }

        private void HandleObservedPlayerDespawned(NetworkPlayerOwnerGate player)
        {
            if (player != null && player.transform == _targetPlayer)
            {
                _targetPlayer = null;
                _hasAlignedToTargetY = false;
                ResetSharedChaseForMissingPlayer();
            }
        }

        private void RefreshObservedPlayers()
        {
            NetworkPlayerOwnerGate[] players =
                FindObjectsByType<NetworkPlayerOwnerGate>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                TryBindTarget(players[i]);
            }
        }

        private void TryBindTarget(NetworkPlayerOwnerGate player)
        {
            if (player == null || player.TimelineRole != _timelineRole)
            {
                return;
            }

            _targetPlayer = player.transform;
            if (_hasAlignedToTargetY)
            {
                return;
            }

            Vector3 position = transform.position;
            position.y = _targetPlayer.position.y;
            transform.position = position;
            _hasAlignedToTargetY = true;
        }

        private float GetSharedChaseSpeed()
        {
            return s_pastChaser != null
                ? s_pastChaser._chaseSpeed
                : _chaseSpeed;
        }

        private float GetTargetPlayerX()
        {
            return _targetPlayer != null
                ? _targetPlayer.position.x
                : float.PositiveInfinity;
        }

        private void ReportSharedFailure(string reason)
        {
            s_failureReported = true;
            ResolveDependencies();
            _gameFlowManager?.ReportEscapeFailure(reason);
        }

        private static void ResetSharedChaseForMissingPlayer()
        {
            s_isSharedChaseStarted = false;
            s_failureReported = false;
            s_hasSectionDeadline = false;
            s_sectionDeadline = 0f;
        }

        private void ResolveDependencies()
        {
            if (_gameFlowManager == null)
            {
                _gameFlowManager = FindAnyObjectByType<GameFlowManager>();
            }
        }

        private static bool IsHostAuthority()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        }
    }
}
