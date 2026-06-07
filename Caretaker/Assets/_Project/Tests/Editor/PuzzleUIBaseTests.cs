using NUnit.Framework;

using UnityEditor;
using UnityEngine;

using Caretaker.Presentation;

namespace Caretaker.Tests.Editor
{
    public class PuzzleUIBaseTests
    {
        [Test]
        public void Open_ActivatesPanelRoot()
        {
            TestPuzzleUI puzzle = CreatePuzzle(out GameObject root, out GameObject panel);
            panel.SetActive(false);

            puzzle.Open();

            Assert.That(puzzle.IsOpen, Is.True);
            Assert.That(panel.activeSelf, Is.True);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Close_DeactivatesPanelRoot()
        {
            TestPuzzleUI puzzle = CreatePuzzle(out GameObject root, out GameObject panel);
            panel.SetActive(true);

            puzzle.Close();

            Assert.That(puzzle.IsOpen, Is.False);
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Submit_WithCorrectSolution_MarksSolvedAndRaisesSolvedEventOnce()
        {
            TestPuzzleUI puzzle = CreatePuzzle(out GameObject root, out _);
            int solvedCount = 0;
            PuzzleUIBase solvedPuzzle = null;

            puzzle.ShouldAcceptSolution = true;
            puzzle.OnPuzzleSolved += solved =>
            {
                solvedCount++;
                solvedPuzzle = solved;
            };

            puzzle.Submit();
            puzzle.Submit();

            Assert.That(puzzle.IsSolved, Is.True);
            Assert.That(solvedCount, Is.EqualTo(1));
            Assert.That(solvedPuzzle, Is.SameAs(puzzle));
            Assert.That(puzzle.SolvedHookCount, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Submit_WithIncorrectSolution_KeepsPuzzleUnsolved()
        {
            TestPuzzleUI puzzle = CreatePuzzle(out GameObject root, out _);
            int solvedCount = 0;

            puzzle.ShouldAcceptSolution = false;
            puzzle.OnPuzzleSolved += _ => solvedCount++;

            puzzle.Submit();

            Assert.That(puzzle.IsSolved, Is.False);
            Assert.That(solvedCount, Is.Zero);
            Assert.That(puzzle.IncorrectSolutionCount, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryCompletePuzzle_WithCorrectSolution_MarksSolvedAndRaisesSolvedEvent()
        {
            TestPuzzleUI puzzle = CreatePuzzle(out GameObject root, out _);
            int solvedCount = 0;

            puzzle.ShouldAcceptSolution = true;
            puzzle.OnPuzzleSolved += _ => solvedCount++;

            bool didComplete = puzzle.TryCompleteFromInputChanged();

            Assert.That(didComplete, Is.True);
            Assert.That(puzzle.IsSolved, Is.True);
            Assert.That(solvedCount, Is.EqualTo(1));
            Assert.That(puzzle.SolvedHookCount, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryCompletePuzzle_WithIncorrectSolution_DoesNotRunIncorrectFeedback()
        {
            TestPuzzleUI puzzle = CreatePuzzle(out GameObject root, out _);

            puzzle.ShouldAcceptSolution = false;

            bool didComplete = puzzle.TryCompleteFromInputChanged();

            Assert.That(didComplete, Is.False);
            Assert.That(puzzle.IsSolved, Is.False);
            Assert.That(puzzle.IncorrectSolutionCount, Is.Zero);

            Object.DestroyImmediate(root);
        }

        private static TestPuzzleUI CreatePuzzle(out GameObject root, out GameObject panel)
        {
            root = new GameObject("PuzzleRoot");
            panel = new GameObject("PuzzlePanel");
            panel.transform.SetParent(root.transform);

            TestPuzzleUI puzzle = root.AddComponent<TestPuzzleUI>();
            SerializedObject serializedObject = new(puzzle);
            serializedObject.FindProperty("_panelRoot").objectReferenceValue = panel;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return puzzle;
        }

        private sealed class TestPuzzleUI : PuzzleUIBase
        {
            public bool ShouldAcceptSolution { get; set; }

            public int IncorrectSolutionCount { get; private set; }

            public int SolvedHookCount { get; private set; }

            public bool TryCompleteFromInputChanged()
            {
                return TryCompletePuzzle();
            }

            protected override bool IsCorrectSolution()
            {
                return ShouldAcceptSolution;
            }

            protected override void HandleIncorrectSolution()
            {
                IncorrectSolutionCount++;
            }

            protected override void HandleSolved()
            {
                SolvedHookCount++;
            }
        }
    }
}
