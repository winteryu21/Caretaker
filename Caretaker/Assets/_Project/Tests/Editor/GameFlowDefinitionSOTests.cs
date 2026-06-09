using System.Collections.Generic;
using System.Reflection;

using Caretaker.Core;
using Caretaker.Shared;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Caretaker.Tests.Editor
{
    public sealed class GameFlowDefinitionSOTests
    {
        private const string DEFAULT_DEFINITION_PATH =
            "Assets/_Project/Data/Flow/SO_GameFlowDefinition.asset";

        [Test]
        public void TryGetTransitionMajorId_ReturnsConfiguredMajor()
        {
            GameFlowDefinitionSO definition = CreateDefinition(
                new[]
                {
                    new GameFlowDefinitionSO.PhaseFlowDefinition(
                        PhaseId.Phase1,
                        MajorId.M2,
                        PhaseId.Phase2)
                },
                new GameFlowDefinitionSO.ObjectiveDefinition[0]);

            bool found = definition.TryGetTransitionMajorId(PhaseId.Phase1, out MajorId transitionMajorId);

            Assert.That(found, Is.True);
            Assert.That(transitionMajorId, Is.EqualTo(MajorId.M2));
        }

        [Test]
        public void TryGetTransition_ReturnsConfiguredMajorAndNextPhase()
        {
            GameFlowDefinitionSO definition = CreateDefinition(
                new[]
                {
                    new GameFlowDefinitionSO.PhaseFlowDefinition(
                        PhaseId.Phase2,
                        MajorId.M4,
                        PhaseId.Phase3)
                },
                new GameFlowDefinitionSO.ObjectiveDefinition[0]);

            bool found = definition.TryGetTransition(
                PhaseId.Phase2,
                out MajorId transitionMajorId,
                out PhaseId nextPhaseId);

            Assert.That(found, Is.True);
            Assert.That(transitionMajorId, Is.EqualTo(MajorId.M4));
            Assert.That(nextPhaseId, Is.EqualTo(PhaseId.Phase3));
        }

        [Test]
        public void TryResolveObjective_ReturnsLatestUnlockedObjectiveForRole()
        {
            GameFlowDefinitionSO definition = CreateDefinition(
                new GameFlowDefinitionSO.PhaseFlowDefinition[0],
                new[]
                {
                    new GameFlowDefinitionSO.ObjectiveDefinition(
                        ObjectiveId.P1_Past_ExploreLobby,
                        PhaseId.Phase1,
                        TimelineRole.Past,
                        MajorId.None,
                        0,
                        "Explore"),
                    new GameFlowDefinitionSO.ObjectiveDefinition(
                        ObjectiveId.P1_Past_GoToSecurityRoom,
                        PhaseId.Phase1,
                        TimelineRole.Past,
                        MajorId.M1,
                        1,
                        "Go security")
                });

            bool found = definition.TryResolveObjective(
                PhaseId.Phase1,
                TimelineRole.Past,
                new HashSet<MajorId> { MajorId.M1 },
                out ObjectiveId objectiveId);

            Assert.That(found, Is.True);
            Assert.That(objectiveId, Is.EqualTo(ObjectiveId.P1_Past_GoToSecurityRoom));
        }

        [Test]
        public void TryGetObjective_ReturnsDisplayText()
        {
            GameFlowDefinitionSO definition = CreateDefinition(
                new GameFlowDefinitionSO.PhaseFlowDefinition[0],
                new[]
                {
                    new GameFlowDefinitionSO.ObjectiveDefinition(
                        ObjectiveId.P3_Future_ReachExit,
                        PhaseId.Phase3,
                        TimelineRole.Future,
                        MajorId.None,
                        0,
                        "Reach the exit")
                });

            bool found = definition.TryGetObjective(
                ObjectiveId.P3_Future_ReachExit,
                out GameFlowDefinitionSO.ObjectiveDefinition objective);

            Assert.That(found, Is.True);
            Assert.That(objective.DisplayText, Is.EqualTo("Reach the exit"));
        }

        [Test]
        public void DefaultDefinitionAsset_ContainsDev39PhaseFlow()
        {
            GameFlowDefinitionSO definition = AssetDatabase.LoadAssetAtPath<GameFlowDefinitionSO>(
                DEFAULT_DEFINITION_PATH);

            Assert.That(definition, Is.Not.Null);

            bool hasPhase1Transition = definition.TryGetTransitionMajorId(
                PhaseId.Phase1,
                out MajorId phase1TransitionMajorId);
            bool hasPhase2Transition = definition.TryGetTransitionMajorId(
                PhaseId.Phase2,
                out MajorId phase2TransitionMajorId);

            Assert.That(hasPhase1Transition, Is.True);
            Assert.That(phase1TransitionMajorId, Is.EqualTo(MajorId.M2));
            Assert.That(hasPhase2Transition, Is.True);
            Assert.That(phase2TransitionMajorId, Is.EqualTo(MajorId.M4));
        }

        [Test]
        public void DefaultDefinitionAsset_ResolvesRoleObjectives()
        {
            GameFlowDefinitionSO definition = AssetDatabase.LoadAssetAtPath<GameFlowDefinitionSO>(
                DEFAULT_DEFINITION_PATH);

            bool foundInitialPastObjective = definition.TryResolveObjective(
                PhaseId.Phase1,
                TimelineRole.Past,
                new HashSet<MajorId>(),
                out ObjectiveId initialPastObjective);
            bool foundFutureObjectiveAfterM3 = definition.TryResolveObjective(
                PhaseId.Phase2,
                TimelineRole.Future,
                new HashSet<MajorId> { MajorId.M3 },
                out ObjectiveId futureObjectiveAfterM3);

            Assert.That(foundInitialPastObjective, Is.True);
            Assert.That(initialPastObjective, Is.EqualTo(ObjectiveId.P1_Past_ExploreLobby));
            Assert.That(foundFutureObjectiveAfterM3, Is.True);
            Assert.That(futureObjectiveAfterM3, Is.EqualTo(ObjectiveId.P2_Future_RunBlueprintExperiment));
        }

        private static GameFlowDefinitionSO CreateDefinition(
            GameFlowDefinitionSO.PhaseFlowDefinition[] phases,
            GameFlowDefinitionSO.ObjectiveDefinition[] objectives)
        {
            GameFlowDefinitionSO definition = ScriptableObject.CreateInstance<GameFlowDefinitionSO>();
            SetPrivateField(definition, "_phases", phases);
            SetPrivateField(definition, "_objectives", objectives);
            return definition;
        }

        private static void SetPrivateField<T>(GameFlowDefinitionSO definition, string fieldName, T value)
        {
            typeof(GameFlowDefinitionSO)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(definition, value);
        }
    }
}
