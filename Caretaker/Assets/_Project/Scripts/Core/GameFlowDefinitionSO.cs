using System;
using System.Collections.Generic;

using Caretaker.Shared;
using UnityEngine;

namespace Caretaker.Core
{
    /// <summary>
    /// Defines phase transition triggers and role-specific objective lookup data.
    /// </summary>
    /// <remarks>DSD §2.3, §3.9, §4.2 - game flow data schema.</remarks>
    [CreateAssetMenu(fileName = "SO_GameFlowDefinition", menuName = "Caretaker SO/Flow/GameFlowDefinition")]
    public sealed class GameFlowDefinitionSO : ScriptableObject
    {
        [SerializeField] private PhaseFlowDefinition[] _phases;
        [SerializeField] private ObjectiveDefinition[] _objectives;

        /// <summary>
        /// Configured phase flow definitions.
        /// </summary>
        public IReadOnlyList<PhaseFlowDefinition> Phases => _phases;

        /// <summary>
        /// Configured objective definitions.
        /// </summary>
        public IReadOnlyList<ObjectiveDefinition> Objectives => _objectives;

        /// <summary>
        /// Attempts to find the Major Interaction that triggers transition out of the phase.
        /// </summary>
        /// <param name="phaseId">Phase to inspect.</param>
        /// <param name="transitionMajorId">Transition trigger Major ID.</param>
        /// <returns>True when the phase has a configured transition Major.</returns>
        public bool TryGetTransitionMajorId(PhaseId phaseId, out MajorId transitionMajorId)
        {
            transitionMajorId = MajorId.None;
            if (!TryGetPhaseFlow(phaseId, out PhaseFlowDefinition phaseFlow))
            {
                return false;
            }

            transitionMajorId = phaseFlow.TransitionMajorId;
            return transitionMajorId != MajorId.None;
        }

        /// <summary>
        /// Attempts to find transition data for the phase.
        /// </summary>
        /// <param name="phaseId">Phase to inspect.</param>
        /// <param name="transitionMajorId">Transition trigger Major ID.</param>
        /// <param name="nextPhaseId">Next phase after transition.</param>
        /// <returns>True when the phase has configured transition data.</returns>
        public bool TryGetTransition(
            PhaseId phaseId,
            out MajorId transitionMajorId,
            out PhaseId nextPhaseId)
        {
            transitionMajorId = MajorId.None;
            nextPhaseId = phaseId;

            if (!TryGetPhaseFlow(phaseId, out PhaseFlowDefinition phaseFlow))
            {
                return false;
            }

            transitionMajorId = phaseFlow.TransitionMajorId;
            nextPhaseId = phaseFlow.NextPhaseId;
            return transitionMajorId != MajorId.None && nextPhaseId != phaseId;
        }

        private bool TryGetPhaseFlow(PhaseId phaseId, out PhaseFlowDefinition phaseFlow)
        {
            phaseFlow = default;

            if (_phases == null)
            {
                return false;
            }

            for (int i = 0; i < _phases.Length; i++)
            {
                if (_phases[i].PhaseId != phaseId)
                {
                    continue;
                }

                phaseFlow = _phases[i];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves the currently unlocked objective for a phase and timeline role.
        /// </summary>
        /// <param name="phaseId">Current phase.</param>
        /// <param name="timelineRole">Local timeline role.</param>
        /// <param name="completedMajorIds">Completed Major IDs.</param>
        /// <param name="objectiveId">Resolved objective ID.</param>
        /// <returns>True when an objective is configured for the state.</returns>
        public bool TryResolveObjective(
            PhaseId phaseId,
            TimelineRole timelineRole,
            IReadOnlyCollection<MajorId> completedMajorIds,
            out ObjectiveId objectiveId)
        {
            objectiveId = ObjectiveId.None;
            if (_objectives == null)
            {
                return false;
            }

            int bestOrder = int.MinValue;
            for (int i = 0; i < _objectives.Length; i++)
            {
                ObjectiveDefinition objective = _objectives[i];
                if (objective.PhaseId != phaseId || objective.TimelineRole != timelineRole)
                {
                    continue;
                }

                if (!IsUnlocked(objective.UnlockAfterMajorId, completedMajorIds))
                {
                    continue;
                }

                if (objective.SortOrder < bestOrder)
                {
                    continue;
                }

                bestOrder = objective.SortOrder;
                objectiveId = objective.ObjectiveId;
            }

            return objectiveId != ObjectiveId.None;
        }

        /// <summary>
        /// Attempts to find display data for an objective.
        /// </summary>
        /// <param name="objectiveId">Objective ID.</param>
        /// <param name="objective">Objective definition.</param>
        /// <returns>True when the objective is configured.</returns>
        public bool TryGetObjective(ObjectiveId objectiveId, out ObjectiveDefinition objective)
        {
            objective = default;
            if (_objectives == null)
            {
                return false;
            }

            for (int i = 0; i < _objectives.Length; i++)
            {
                if (_objectives[i].ObjectiveId != objectiveId)
                {
                    continue;
                }

                objective = _objectives[i];
                return true;
            }

            return false;
        }

        private static bool IsUnlocked(MajorId unlockAfterMajorId, IReadOnlyCollection<MajorId> completedMajorIds)
        {
            if (unlockAfterMajorId == MajorId.None)
            {
                return true;
            }

            if (completedMajorIds == null)
            {
                return false;
            }

            foreach (MajorId completedMajorId in completedMajorIds)
            {
                if (completedMajorId == unlockAfterMajorId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Defines the Major ID that advances a phase.
        /// </summary>
        [Serializable]
        public struct PhaseFlowDefinition
        {
            [SerializeField] private PhaseId _phaseId;
            [SerializeField] private MajorId _transitionMajorId;
            [SerializeField] private PhaseId _nextPhaseId;

            /// <summary>
            /// Creates a phase flow definition.
            /// </summary>
            /// <param name="phaseId">Source phase.</param>
            /// <param name="transitionMajorId">Major ID that triggers transition.</param>
            /// <param name="nextPhaseId">Next phase.</param>
            public PhaseFlowDefinition(PhaseId phaseId, MajorId transitionMajorId, PhaseId nextPhaseId)
            {
                _phaseId = phaseId;
                _transitionMajorId = transitionMajorId;
                _nextPhaseId = nextPhaseId;
            }

            /// <summary>Source phase.</summary>
            public PhaseId PhaseId => _phaseId;

            /// <summary>Major ID that triggers transition.</summary>
            public MajorId TransitionMajorId => _transitionMajorId;

            /// <summary>Next phase after transition.</summary>
            public PhaseId NextPhaseId => _nextPhaseId;
        }

        /// <summary>
        /// Defines objective display data for one role and phase state.
        /// </summary>
        [Serializable]
        public struct ObjectiveDefinition
        {
            [SerializeField] private ObjectiveId _objectiveId;
            [SerializeField] private PhaseId _phaseId;
            [SerializeField] private TimelineRole _timelineRole;
            [SerializeField] private MajorId _unlockAfterMajorId;
            [SerializeField] private int _sortOrder;
            [SerializeField] private string _displayText;

            /// <summary>
            /// Creates an objective definition.
            /// </summary>
            /// <param name="objectiveId">Objective ID.</param>
            /// <param name="phaseId">Phase ID.</param>
            /// <param name="timelineRole">Timeline role.</param>
            /// <param name="unlockAfterMajorId">Major ID required before this objective becomes current.</param>
            /// <param name="sortOrder">Ordering among objectives in the same phase and role.</param>
            /// <param name="displayText">HUD display text.</param>
            public ObjectiveDefinition(
                ObjectiveId objectiveId,
                PhaseId phaseId,
                TimelineRole timelineRole,
                MajorId unlockAfterMajorId,
                int sortOrder,
                string displayText)
            {
                _objectiveId = objectiveId;
                _phaseId = phaseId;
                _timelineRole = timelineRole;
                _unlockAfterMajorId = unlockAfterMajorId;
                _sortOrder = sortOrder;
                _displayText = displayText;
            }

            /// <summary>Objective ID.</summary>
            public ObjectiveId ObjectiveId => _objectiveId;

            /// <summary>Phase ID.</summary>
            public PhaseId PhaseId => _phaseId;

            /// <summary>Timeline role.</summary>
            public TimelineRole TimelineRole => _timelineRole;

            /// <summary>Major ID required before this objective becomes current.</summary>
            public MajorId UnlockAfterMajorId => _unlockAfterMajorId;

            /// <summary>Ordering among objectives in the same phase and role.</summary>
            public int SortOrder => _sortOrder;

            /// <summary>HUD display text.</summary>
            public string DisplayText => _displayText;
        }
    }
}
