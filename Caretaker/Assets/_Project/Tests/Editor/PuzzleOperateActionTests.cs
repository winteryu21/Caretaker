using NUnit.Framework;

using UnityEditor;
using UnityEngine;

using Caretaker.Presentation;
using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class PuzzleOperateActionTests
    {
        [Test]
        public void Execute_OpensPuzzleThroughFramework()
        {
            TestPuzzleUI puzzle = CreateAction(out GameObject root, out PuzzleOperateAction action);

            bool didOpen = action.Execute(null);

            Assert.That(didOpen, Is.True);
            Assert.That(action.IsOpen, Is.True);
            Assert.That(puzzle.OpenedHookCount, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Close_ClosesPuzzleThroughFramework()
        {
            TestPuzzleUI puzzle = CreateAction(out GameObject root, out PuzzleOperateAction action);
            action.Execute(null);

            action.Close();

            Assert.That(action.IsOpen, Is.False);
            Assert.That(puzzle.ClosedHookCount, Is.EqualTo(1));

            Object.DestroyImmediate(root);
        }

        private static TestPuzzleUI CreateAction(
            out GameObject root,
            out PuzzleOperateAction action)
        {
            root = new GameObject("PuzzleOperateActionTest");
            TestPuzzleUI puzzle = root.AddComponent<TestPuzzleUI>();
            action = root.AddComponent<PuzzleOperateAction>();

            SerializedObject serializedObject = new(action);
            serializedObject.FindProperty("_puzzleUi").objectReferenceValue = puzzle;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            puzzle.Close();
            puzzle.ResetHookCounts();

            return puzzle;
        }

        private sealed class TestPuzzleUI : PuzzleUIBase
        {
            public int OpenedHookCount { get; private set; }

            public int ClosedHookCount { get; private set; }

            public void ResetHookCounts()
            {
                OpenedHookCount = 0;
                ClosedHookCount = 0;
            }

            protected override bool IsCorrectSolution()
            {
                return false;
            }

            protected override void HandleOpened()
            {
                OpenedHookCount++;
            }

            protected override void HandleClosed()
            {
                ClosedHookCount++;
            }
        }
    }
}
