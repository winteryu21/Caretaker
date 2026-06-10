using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Future-side Major 4 storage UI that overlays opened-cell sprites on the storage image.
    /// </summary>
    [AddComponentMenu("Caretaker/Puzzle/Major 4 Future Storage UI")]
    [DisallowMultipleComponent]
    public sealed class Major4FutureStorageUI : MonoBehaviour
    {
        [Header("Major 4")]
        [SerializeField] private Major4StoragePuzzleState _puzzleState;

        [Header("Opened Cell Overlays")]
        [SerializeField] private Image[] _openedCellImages = new Image[Major4StoragePuzzleState.CELL_COUNT];
        [SerializeField] private Sprite _emptyOpenedCellSprite;
        [SerializeField] private Sprite _targetOpenedCellSprite;
        [SerializeField] private Color _closedColor = new(1f, 1f, 1f, 0f);
        [SerializeField] private Color _openedColor = Color.white;

        [Header("Status")]
        [SerializeField] private TMP_Text _openedTargetCountText;
        [SerializeField] private TMP_Text _remainingAttemptsText;

        private void OnValidate()
        {
            if (_openedCellImages == null || _openedCellImages.Length != Major4StoragePuzzleState.CELL_COUNT)
            {
                System.Array.Resize(ref _openedCellImages, Major4StoragePuzzleState.CELL_COUNT);
            }
        }

        private void OnEnable()
        {
            if (_puzzleState != null)
            {
                _puzzleState.OnStateChanged += HandlePuzzleStateChanged;
            }

            RefreshVisuals();
        }

        private void OnDisable()
        {
            if (_puzzleState != null)
            {
                _puzzleState.OnStateChanged -= HandlePuzzleStateChanged;
            }
        }

        private void HandlePuzzleStateChanged(Major4StoragePuzzleState puzzleState)
        {
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            int openedTargetCount = 0;

            for (int i = 0; i < _openedCellImages.Length; i++)
            {
                Image openedCellImage = _openedCellImages[i];
                if (openedCellImage == null)
                {
                    continue;
                }

                bool isOpen = _puzzleState != null && _puzzleState.IsFutureCellOpen(i);
                bool isTarget = _puzzleState != null && _puzzleState.IsOpenedTargetCell(i);

                openedCellImage.enabled = isOpen;
                openedCellImage.color = isOpen ? _openedColor : _closedColor;
                openedCellImage.sprite = isTarget ? _targetOpenedCellSprite : _emptyOpenedCellSprite;

                if (isTarget)
                {
                    openedTargetCount++;
                }
            }

            if (_openedTargetCountText != null)
            {
                _openedTargetCountText.text = $"{openedTargetCount}/{Major4StoragePuzzleState.TARGET_COUNT}";
            }

            if (_remainingAttemptsText != null && _puzzleState != null)
            {
                _remainingAttemptsText.text = $"{_puzzleState.RemainingAttempts}/{_puzzleState.MaxAttempts}";
            }
        }
    }
}
