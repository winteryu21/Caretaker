using UnityEngine;

namespace Caretaker.Presentation
{
    [AddComponentMenu("Caretaker/Test/Major1/Puzzle Solved Relay")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PuzzleUIBase))]
    public sealed class Major1PuzzleSolvedRelay : MonoBehaviour
    {
        [SerializeField] private Major1PowerRestorationController _controller;

        private PuzzleUIBase _puzzleUi;

        private void Awake()
        {
            _puzzleUi = GetComponent<PuzzleUIBase>();
        }

        private void OnEnable()
        {
            if (_puzzleUi == null)
            {
                _puzzleUi = GetComponent<PuzzleUIBase>();
            }

            _puzzleUi.OnPuzzleSolved += HandlePuzzleSolved;
        }

        private void OnDisable()
        {
            if (_puzzleUi != null)
            {
                _puzzleUi.OnPuzzleSolved -= HandlePuzzleSolved;
            }
        }

        private void HandlePuzzleSolved(PuzzleUIBase puzzleUi)
        {
            if (_controller == null)
            {
                _controller = FindFirstObjectByType<Major1PowerRestorationController>();
            }

            if (_controller != null)
            {
                _controller.MarkPastCircuitSolved();
            }
        }
    }
}
