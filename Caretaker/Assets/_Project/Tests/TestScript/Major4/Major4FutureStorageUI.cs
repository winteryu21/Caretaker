using System.Collections;
using System.Collections.Generic;
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
    public sealed class Major4FutureStorageUI : PuzzleUIBase
    {
        private static readonly WaitForSeconds BRIDGE_RESOLVE_INTERVAL = new(0.25f);

        [Header("Major 4")]
        [SerializeField] private Major4StoragePuzzleBridge _bridge;

        [Header("Opened Cell Overlays")]
        [SerializeField] private Image[] _openedCellImages = new Image[Major4StoragePuzzleBridge.CELL_COUNT];
        [SerializeField] private Sprite _emptyOpenedCellSprite;
        [SerializeField] private Sprite _targetOpenedCellSprite;
        [SerializeField] private Color _closedColor = new(1f, 1f, 1f, 0f);
        [SerializeField] private Color _openedColor = Color.white;

        [Header("Status")]
        [SerializeField] private TMP_Text _openedTargetCountText;
        [SerializeField] private TMP_Text _remainingAttemptsText;

        [Header("Success")]
        [SerializeField] private bool _closeOnSolved = true;

        private readonly List<Image> _openedCellImageBuffer = new(Major4StoragePuzzleBridge.CELL_COUNT);
        private Coroutine _resolveBridgeRoutine;

        protected override bool WarnWhenSolvedTriggerMissing => false;

        private void OnValidate()
        {
            NormalizeOpenedCellImages();
            TryAutoBindOpenedCellImages(false);
        }

        private void OnEnable()
        {
            NormalizeOpenedCellImages();
            TryAutoBindOpenedCellImages(true);
            ResolveBridge();
            SubscribeToBridge();
            RefreshVisuals();
            TryCompletePuzzle();
        }

        private void OnDisable()
        {
            StopResolveBridgeRoutine();
            UnsubscribeFromBridge();
        }

        private void HandleBridgeStateChanged(Major4StoragePuzzleBridge bridge)
        {
            RefreshVisuals();
            TryCompletePuzzle();
        }

        protected override bool IsCorrectSolution()
        {
            return _bridge != null && _bridge.IsSolved;
        }

        protected override void HandleSolved()
        {
            if (_closeOnSolved)
            {
                Close();
            }
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

                bool isOpen = _bridge != null && _bridge.IsFutureCellOpen(i);
                bool isTarget = _bridge != null && _bridge.IsOpenedTargetCell(i);

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
                _openedTargetCountText.text = $"{openedTargetCount}/{Major4StoragePuzzleBridge.TARGET_COUNT}";
            }

            if (_remainingAttemptsText != null)
            {
                _remainingAttemptsText.text = _bridge != null
                    ? $"{_bridge.RemainingAttempts}/{_bridge.MaxAttempts}"
                    : string.Empty;
            }
        }

        private void ResolveBridge()
        {
            if (_bridge != null)
            {
                return;
            }

            _bridge = Major4StoragePuzzleBridge.ActiveBridge;
            if (_bridge == null)
            {
                _bridge = FindAnyObjectByType<Major4StoragePuzzleBridge>(FindObjectsInactive.Include);
            }

            if (_bridge == null && _resolveBridgeRoutine == null && isActiveAndEnabled)
            {
                _resolveBridgeRoutine = StartCoroutine(ResolveBridgeRoutine());
            }
        }

        private void NormalizeOpenedCellImages()
        {
            if (_openedCellImages == null || _openedCellImages.Length != Major4StoragePuzzleBridge.CELL_COUNT)
            {
                System.Array.Resize(ref _openedCellImages, Major4StoragePuzzleBridge.CELL_COUNT);
            }
        }

        private void TryAutoBindOpenedCellImages(bool warnOnFailure)
        {
            if (HasCompleteOpenedCellImages())
            {
                return;
            }

            _openedCellImageBuffer.Clear();
            Image[] childImages = GetComponentsInChildren<Image>(true);

            for (int i = 0; i < childImages.Length; i++)
            {
                Image childImage = childImages[i];
                if (childImage != null && childImage.gameObject.name.StartsWith("Cell_", System.StringComparison.Ordinal))
                {
                    _openedCellImageBuffer.Add(childImage);
                }
            }

            if (_openedCellImageBuffer.Count != Major4StoragePuzzleBridge.CELL_COUNT)
            {
                if (warnOnFailure)
                {
                    Debug.LogWarning(
                        $"Major4 future storage expected {Major4StoragePuzzleBridge.CELL_COUNT} opened cell images, " +
                        $"but found {_openedCellImageBuffer.Count}.",
                        this);
                }

                return;
            }

            for (int i = 0; i < Major4StoragePuzzleBridge.CELL_COUNT; i++)
            {
                _openedCellImages[i] = _openedCellImageBuffer[i];
            }
        }

        private bool HasCompleteOpenedCellImages()
        {
            if (_openedCellImages == null || _openedCellImages.Length != Major4StoragePuzzleBridge.CELL_COUNT)
            {
                return false;
            }

            for (int i = 0; i < _openedCellImages.Length; i++)
            {
                if (_openedCellImages[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator ResolveBridgeRoutine()
        {
            while (_bridge == null)
            {
                yield return BRIDGE_RESOLVE_INTERVAL;
                _bridge = Major4StoragePuzzleBridge.ActiveBridge;
            }

            _resolveBridgeRoutine = null;
            SubscribeToBridge();
            RefreshVisuals();
        }

        private void StopResolveBridgeRoutine()
        {
            if (_resolveBridgeRoutine == null)
            {
                return;
            }

            StopCoroutine(_resolveBridgeRoutine);
            _resolveBridgeRoutine = null;
        }

        private void SubscribeToBridge()
        {
            if (_bridge == null)
            {
                return;
            }

            _bridge.OnStateChanged -= HandleBridgeStateChanged;
            _bridge.OnStateChanged += HandleBridgeStateChanged;
        }

        private void UnsubscribeFromBridge()
        {
            if (_bridge != null)
            {
                _bridge.OnStateChanged -= HandleBridgeStateChanged;
            }
        }
    }
}
