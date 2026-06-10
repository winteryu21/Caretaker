using System;
using System.Collections.Generic;
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
        [SerializeField] private GameFlowDefinitionSO _flowDefinition;
        [SerializeField] private PhaseId _initialPhase = PhaseId.Phase1;
        [SerializeField] private bool _loadInitialPhaseOnSpawn = true;

        // 3. private 필드
        private readonly PhaseService _phaseService = new();
        private readonly HashSet<MajorId> _completedMajorIds = new();
        private readonly Dictionary<TimelineRole, ObjectiveId> _currentObjectiveByRole = new();
        private bool _futurePhaseAdvanceReady;
        private bool _pastPhaseAdvanceReady;
        private bool _futureExitReached;
        private bool _pastExitReached;
        private PhaseId _currentPhase;
        private GameResult _gameResult = GameResult.None;

        // 4. 프로퍼티
        /// <summary>
        /// 현재 진행 중인 Phase를 반환한다.
        /// </summary>
        public PhaseId CurrentPhase => _currentPhase;

        /// <summary>
        /// 완료된 Major Interaction ID 목록을 반환한다.
        /// </summary>
        public IReadOnlyCollection<MajorId> CompletedMajorIds => _completedMajorIds;

        /// <summary>
        /// 현재 게임 결과를 반환한다.
        /// </summary>
        public GameResult CurrentGameResult => _gameResult;

        /// <summary>
        /// Past 플레이어가 Phase3 출구에 도달했는지 반환한다.
        /// </summary>
        public bool PastExitReached => _pastExitReached;

        /// <summary>
        /// Future 플레이어가 Phase3 출구에 도달했는지 반환한다.
        /// </summary>
        public bool FutureExitReached => _futureExitReached;

        // 5. 이벤트
        /// <summary>
        /// 로컬 클라이언트에서 Phase 전환 이벤트를 수신했을 때 발생한다.
        /// </summary>
        public event Action<PhaseId> OnPhaseChanged;

        /// <summary>
        /// Major Interaction 완료가 확정되었을 때 발생한다.
        /// </summary>
        public event Action<PhaseId, MajorId> OnMajorCompleted;

        /// <summary>
        /// 역할별 현재 Objective가 변경되었을 때 발생한다.
        /// </summary>
        public event Action<TimelineRole, ObjectiveId> OnObjectiveChanged;

        /// <summary>
        /// 게임 결과가 확정되었을 때 발생한다.
        /// </summary>
        public event Action<GameResult> OnGameResult;

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
                _roleManager.OnMajorCompletedReceived += HandleMajorCompletedReceived;
                _roleManager.OnObjectiveChangedReceived += HandleObjectiveChangedReceived;
                _roleManager.OnGameResultReceived += HandleGameResultReceived;
            }
        }

        private void Start()
        {
            _currentPhase = _initialPhase;

            if (_loadInitialPhaseOnSpawn)
            {
                LoadLocalPhase(_currentPhase);
            }

            if (IsHostAuthority())
            {
                UpdateObjectivesForAllRoles();
            }
        }

        private void OnDisable()
        {
            if (_roleManager != null)
            {
                _roleManager.OnLocalRoleAssigned -= HandleLocalRoleAssigned;
                _roleManager.OnPhaseAdvanceReadySubmitted -= HandlePhaseAdvanceReadySubmitted;
                _roleManager.OnPhaseTransitionReceived -= HandlePhaseTransitionReceived;
                _roleManager.OnMajorCompletedReceived -= HandleMajorCompletedReceived;
                _roleManager.OnObjectiveChangedReceived -= HandleObjectiveChangedReceived;
                _roleManager.OnGameResultReceived -= HandleGameResultReceived;
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
            if (!MajorIdUtility.TryResolve(majorId, out MajorId resolvedMajorId))
            {
                Debug.LogWarning($"Unknown Major Interaction ID: {majorId}", this);
                return;
            }

            NotifyMajorComplete(phaseId, resolvedMajorId);
        }

        /// <summary>
        /// Major Interaction 완료를 수신하고 Phase 완료 조건을 검사한다.
        /// </summary>
        /// <param name="phaseId">현재 Phase.</param>
        /// <param name="majorId">완료된 Major Interaction ID.</param>
        public void NotifyMajorComplete(PhaseId phaseId, MajorId majorId)
        {
            if (!IsHostAuthority())
            {
                return;
            }

            if (phaseId != _currentPhase)
            {
                Debug.LogWarning(
                    $"Ignored Major completion from inactive phase. current={_currentPhase}, received={phaseId}, major={majorId}",
                    this);
                return;
            }

            if (majorId == MajorId.None || !_completedMajorIds.Add(majorId))
            {
                return;
            }

            ApplyMajorCompleted(phaseId, majorId);
            _roleManager?.BroadcastMajorCompleted(phaseId, majorId);
            UpdateObjectivesForAllRoles();
            TryTransitionAfterMajor(phaseId, majorId);
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

        /// <summary>Phase 3 포획 또는 타임아웃을 공동 탈출 실패로 확정합니다.</summary>
        public void ReportEscapeFailure(string reason)
        {
            if (!IsHostAuthority() || _currentPhase != PhaseId.Phase3)
            {
                return;
            }

            if (!ApplyGameResult(GameResult.EscapeFail))
            {
                return;
            }

            Debug.LogWarning($"Phase 3 escape failed. Reason={reason}", this);
            _roleManager?.BroadcastGameResult(GameResult.EscapeFail);
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
            ResetExitReachedFlags();
            _roleManager?.BroadcastPhaseTransition(targetPhase);
            UpdateObjectivesForAllRoles();
        }

        /// <summary>
        /// 로컬 플레이어가 기능검증용 Phase 전환 준비를 완료했음을 Host에 제출한다.
        /// </summary>
        public void SubmitLocalPhaseAdvanceReady()
        {
            ResolveDependencies();
            _roleManager?.SubmitLocalPhaseAdvanceReady();
        }

        /// <summary>
        /// 역할별 현재 Objective ID를 반환한다.
        /// </summary>
        /// <param name="timelineRole">조회할 시간대 역할.</param>
        /// <returns>현재 Objective ID, 없으면 <see cref="ObjectiveId.None"/>.</returns>
        public ObjectiveId GetCurrentObjective(TimelineRole timelineRole)
        {
            return _currentObjectiveByRole.TryGetValue(timelineRole, out ObjectiveId objectiveId)
                ? objectiveId
                : ObjectiveId.None;
        }

        /// <summary>
        /// 현재 역할별 Objective 표시 문구를 반환한다.
        /// </summary>
        /// <param name="timelineRole">조회할 시간대 역할.</param>
        /// <param name="displayText">HUD에 표시할 Objective 문구.</param>
        /// <returns>표시 가능한 Objective가 있으면 true.</returns>
        public bool TryGetCurrentObjectiveDisplayText(TimelineRole timelineRole, out string displayText)
        {
            displayText = string.Empty;

            ObjectiveId objectiveId = GetCurrentObjective(timelineRole);
            if (objectiveId == ObjectiveId.None &&
                _flowDefinition != null &&
                _flowDefinition.TryResolveObjective(
                    _currentPhase,
                    timelineRole,
                    _completedMajorIds,
                    out ObjectiveId resolvedObjectiveId))
            {
                objectiveId = resolvedObjectiveId;
            }

            return TryGetObjectiveDisplayText(objectiveId, out displayText);
        }

        /// <summary>
        /// Objective ID에 대응하는 HUD 표시 문구를 반환한다.
        /// </summary>
        /// <param name="objectiveId">Objective ID.</param>
        /// <param name="displayText">HUD에 표시할 Objective 문구.</param>
        /// <returns>표시 가능한 Objective가 있으면 true.</returns>
        public bool TryGetObjectiveDisplayText(ObjectiveId objectiveId, out string displayText)
        {
            displayText = string.Empty;
            if (objectiveId == ObjectiveId.None ||
                _flowDefinition == null ||
                !_flowDefinition.TryGetObjective(objectiveId, out GameFlowDefinitionSO.ObjectiveDefinition objective))
            {
                return false;
            }

            displayText = objective.DisplayText;
            return !string.IsNullOrWhiteSpace(displayText);
        }

        /// <summary>
        /// Phase3 출구 도달 상태를 보고한다.
        /// </summary>
        /// <param name="timelineRole">출구에 도달한 시간대 역할.</param>
        public void ReportExitReached(TimelineRole timelineRole)
        {
            if (!IsHostAuthority() || _currentPhase != PhaseId.Phase3)
            {
                return;
            }

            switch (timelineRole)
            {
                case TimelineRole.Past:
                    _pastExitReached = true;
                    break;
                case TimelineRole.Future:
                    _futureExitReached = true;
                    break;
                default:
                    Debug.LogWarning($"Cannot report exit reached for role: {timelineRole}", this);
                    return;
            }

            Debug.Log(
                $"GameFlow exit reached: role={timelineRole}, past={_pastExitReached}, future={_futureExitReached}",
                this);
            TryResolveEscapeSuccess();
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

        private void ResetExitReachedFlags()
        {
            _pastExitReached = false;
            _futureExitReached = false;
        }

        private void HandleLocalRoleAssigned(TimelineRole role)
        {
            LoadLocalPhase(_currentPhase);

            if (IsHostAuthority())
            {
                UpdateObjectivesForAllRoles();
            }
        }

        private void HandlePhaseAdvanceReadySubmitted(ulong clientId)
        {
            MarkPhaseAdvanceReady(clientId);
        }

        private void HandlePhaseTransitionReceived(PhaseId targetPhase)
        {
            _currentPhase = targetPhase;
            Debug.Log($"GameFlowManager received phase transition: target={targetPhase}", this);
            LoadLocalPhase(targetPhase);
            OnPhaseChanged?.Invoke(targetPhase);
        }

        private void HandleMajorCompletedReceived(PhaseId phaseId, MajorId majorId)
        {
            if (!_completedMajorIds.Add(majorId))
            {
                return;
            }

            ApplyMajorCompleted(phaseId, majorId);
        }

        private void HandleObjectiveChangedReceived(TimelineRole timelineRole, ObjectiveId objectiveId)
        {
            ApplyObjectiveChanged(timelineRole, objectiveId);
        }

        private void HandleGameResultReceived(GameResult gameResult)
        {
            ApplyGameResult(gameResult);
        }

        private void TryTransitionAfterMajor(PhaseId phaseId, MajorId majorId)
        {
            if (_flowDefinition == null)
            {
                Debug.LogWarning("GameFlowManager requires GameFlowDefinitionSO to evaluate phase transitions.", this);
                return;
            }

            if (!_flowDefinition.TryGetTransition(
                    phaseId,
                    out MajorId transitionMajorId,
                    out PhaseId nextPhaseId))
            {
                return;
            }

            if (majorId != transitionMajorId)
            {
                return;
            }

            TransitionPhase(nextPhaseId);
        }

        private void UpdateObjectivesForAllRoles()
        {
            UpdateObjectiveForRole(TimelineRole.Past);
            UpdateObjectiveForRole(TimelineRole.Future);
        }

        private void UpdateObjectiveForRole(TimelineRole timelineRole)
        {
            if (_flowDefinition == null)
            {
                return;
            }

            if (!_flowDefinition.TryResolveObjective(
                    _currentPhase,
                    timelineRole,
                    _completedMajorIds,
                    out ObjectiveId objectiveId))
            {
                return;
            }

            if (!ApplyObjectiveChanged(timelineRole, objectiveId))
            {
                return;
            }

            _roleManager?.BroadcastObjectiveChanged(timelineRole, objectiveId);
        }

        private void ApplyMajorCompleted(PhaseId phaseId, MajorId majorId)
        {
            Debug.Log($"Major Interaction completed: phase={phaseId}, major={majorId}", this);
            OnMajorCompleted?.Invoke(phaseId, majorId);
        }

        private bool ApplyObjectiveChanged(TimelineRole timelineRole, ObjectiveId objectiveId)
        {
            if (_currentObjectiveByRole.TryGetValue(timelineRole, out ObjectiveId currentObjectiveId)
                && currentObjectiveId == objectiveId)
            {
                return false;
            }

            _currentObjectiveByRole[timelineRole] = objectiveId;
            Debug.Log(
                $"GameFlow objective changed: role={timelineRole}, objective={objectiveId}, text=\"{GetObjectiveDisplayText(objectiveId)}\"",
                this);
            OnObjectiveChanged?.Invoke(timelineRole, objectiveId);
            return true;
        }

        private string GetObjectiveDisplayText(ObjectiveId objectiveId)
        {
            return TryGetObjectiveDisplayText(objectiveId, out string displayText)
                ? displayText
                : string.Empty;
        }

        private bool ApplyGameResult(GameResult gameResult)
        {
            if (_gameResult == gameResult)
            {
                return false;
            }

            _gameResult = gameResult;
            Debug.Log($"GameFlow result changed: result={gameResult}", this);
            OnGameResult?.Invoke(gameResult);
            return true;
        }

        private void TryResolveEscapeSuccess()
        {
            if (!_pastExitReached || !_futureExitReached)
            {
                return;
            }

            if (!ApplyGameResult(GameResult.EscapeSuccess))
            {
                return;
            }

            _roleManager?.BroadcastGameResult(GameResult.EscapeSuccess);
        }

        private void LoadLocalPhase(PhaseId phaseId)
        {
            ResolveDependencies();

            if (_sceneLoader == null || _roleManager == null)
            {
                Debug.LogWarning("GameFlowManager requires SceneLoader and SessionRoleManager to load local phase scenes.", this);
                return;
            }

            Debug.Log(
                $"GameFlowManager loading local phase: phase={phaseId}, role={_roleManager.LocalTimelineRole}",
                this);
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
