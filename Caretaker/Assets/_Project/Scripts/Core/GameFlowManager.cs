using System;
using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.Core
{
    /// <summary>
    /// Phase 전환, Major Interaction 완료 판정, 공동 실패 처리를 관리한다.
    /// Host Authority 기반으로 게임 상태 전이와 RPC 전파를 담당한다.
    /// </summary>
    /// <remarks>
    /// DSD §3.9 — 게임 진행 / 체크포인트 시스템
    /// 계층: Network Boundary / Unity Component
    /// </remarks>
    public class GameFlowManager : MonoBehaviour
    {
        // 1. 상수

        // 2. Serialize 필드
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private SessionRoleManager _roleManager;
        [SerializeField] private PhaseId _initialPhase = PhaseId.Phase1;
        [SerializeField] private bool _loadInitialPhaseOnSpawn = true;

        // 3. private 필드
        private readonly PhaseService _phaseService = new();
        private bool _futurePhaseAdvanceReady;
        private bool _pastPhaseAdvanceReady;
        private PhaseId _currentPhase;

        // 4. 프로퍼티
        /// <summary>
        /// 현재 진행 중인 Phase를 반환한다.
        /// </summary>
        public PhaseId CurrentPhase => _currentPhase;

        // 5. 이벤트
        /// <summary>
        /// 로컬 클라이언트에서 Phase 전환 이벤트를 수신했을 때 발생한다.
        /// </summary>
        public event Action<PhaseId> OnPhaseChanged;

        // 6. Unity 생명주기
        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (_roleManager != null)
            {
                _roleManager.OnLocalRoleAssigned += HandleLocalRoleAssigned;
                _roleManager.OnPhaseAdvanceReadySubmitted += HandlePhaseAdvanceReadySubmitted;
                _roleManager.OnPhaseTransitionReceived += HandlePhaseTransitionReceived;
            }
        }

        private void Start()
        {
            _currentPhase = _initialPhase;

            if (_loadInitialPhaseOnSpawn)
            {
                LoadLocalPhase(_currentPhase);
            }
        }

        private void OnDisable()
        {
            if (_roleManager != null)
            {
                _roleManager.OnLocalRoleAssigned -= HandleLocalRoleAssigned;
                _roleManager.OnPhaseAdvanceReadySubmitted -= HandlePhaseAdvanceReadySubmitted;
                _roleManager.OnPhaseTransitionReceived -= HandlePhaseTransitionReceived;
            }
        }

        // 7. public 메서드

        /// <summary>
        /// Major Interaction 완료를 수신하고 Phase 완료 조건을 검사한다.
        /// </summary>
        /// <param name="phaseId">현재 Phase.</param>
        /// <param name="majorId">완료된 Major Interaction ID.</param>
        public void NotifyMajorComplete(PhaseId phaseId, string majorId)
        {
            if (!IsHostAuthority())
            {
                return;
            }

            Debug.Log($"Major Interaction completed: phase={phaseId}, major={majorId}", this);
        }

        /// <summary>
        /// 공동 실패 후 체크포인트 복귀를 실행한다.
        /// </summary>
        /// <param name="reason">실패 사유.</param>
        public void RollbackToCheckpoint(string reason)
        {
            if (!IsHostAuthority())
            {
                return;
            }

            Debug.LogWarning($"Checkpoint rollback is not implemented yet. Reason={reason}", this);
        }

        /// <summary>
        /// 대상 Phase로 전환한다.
        /// </summary>
        /// <param name="targetPhase">전환 대상 Phase.</param>
        public void TransitionPhase(PhaseId targetPhase)
        {
            if (!IsHostAuthority())
            {
                return;
            }

            _currentPhase = targetPhase;
            ResetPhaseAdvanceReadyFlags();
            _roleManager?.BroadcastPhaseTransition(targetPhase);
        }

        /// <summary>
        /// 로컬 플레이어가 기능검증용 Phase 전환 준비를 완료했음을 Host에 제출한다.
        /// </summary>
        public void SubmitLocalPhaseAdvanceReady()
        {
            ResolveDependencies();
            _roleManager?.SubmitLocalPhaseAdvanceReady();
        }

        // 8. private 메서드
        private void MarkPhaseAdvanceReady(ulong clientId)
        {
            if (!IsHostAuthority())
            {
                return;
            }

            ResolveDependencies();
            if (_roleManager == null || !_roleManager.TryGetPlayer(clientId, out PlayerSessionData player))
            {
                Debug.LogWarning($"Cannot mark phase advance ready. Unknown client id: {clientId}", this);
                return;
            }

            SetPhaseAdvanceReadyFlag(player.TimelineRole);
            Debug.Log(
                $"Phase advance debug ready: role={player.TimelineRole}, past={_pastPhaseAdvanceReady}, future={_futurePhaseAdvanceReady}",
                this);

            if (!_pastPhaseAdvanceReady || !_futurePhaseAdvanceReady)
            {
                return;
            }

            PhaseId nextPhase = _phaseService.GetNextPhase(_currentPhase);
            if (nextPhase == _currentPhase)
            {
                ResetPhaseAdvanceReadyFlags();
                Debug.Log("Already at the final phase. Debug phase advance ignored.", this);
                return;
            }

            TransitionPhase(nextPhase);
        }

        private void SetPhaseAdvanceReadyFlag(TimelineRole timelineRole)
        {
            switch (timelineRole)
            {
                case TimelineRole.Past:
                    _pastPhaseAdvanceReady = true;
                    break;
                case TimelineRole.Future:
                    _futurePhaseAdvanceReady = true;
                    break;
                default:
                    Debug.LogWarning($"Cannot set phase advance ready for role: {timelineRole}", this);
                    break;
            }
        }

        private void ResetPhaseAdvanceReadyFlags()
        {
            _pastPhaseAdvanceReady = false;
            _futurePhaseAdvanceReady = false;
        }

        private void HandleLocalRoleAssigned(TimelineRole role)
        {
            LoadLocalPhase(_currentPhase);
        }

        private void HandlePhaseAdvanceReadySubmitted(ulong clientId)
        {
            MarkPhaseAdvanceReady(clientId);
        }

        private void HandlePhaseTransitionReceived(PhaseId targetPhase)
        {
            _currentPhase = targetPhase;
            LoadLocalPhase(targetPhase);
            OnPhaseChanged?.Invoke(targetPhase);
        }

        private void LoadLocalPhase(PhaseId phaseId)
        {
            ResolveDependencies();

            if (_sceneLoader == null || _roleManager == null)
            {
                Debug.LogWarning("GameFlowManager requires SceneLoader and SessionRoleManager to load local phase scenes.", this);
                return;
            }

            _sceneLoader.TryLoadPhase(phaseId, _roleManager.LocalTimelineRole);
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
        }

        private static bool IsHostAuthority()
        {
            return Unity.Netcode.NetworkManager.Singleton != null
                && Unity.Netcode.NetworkManager.Singleton.IsServer;
        }
    }
}
