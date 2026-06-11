using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Caretaker.World;

namespace Caretaker.Presentation
{
    /// <summary>
    /// Past-side Major 4 computer UI that selects one storage address and sends an open request.
    /// </summary>
    [AddComponentMenu("Caretaker/Puzzle/Major 4 Past Computer UI")]
    [DisallowMultipleComponent]
    public sealed class Major4PastComputerUI : PuzzleUIBase
    {
        private const string SOLVED_TRIGGER_ID = "CR_P2_VIRUS_PLANT";
        private const string SOLVED_CONDITION_KEY = "virusPuzzle";
        private const string SOLVED_CONDITION_VALUE = "Solved";
        private static readonly WaitForSeconds BRIDGE_RESOLVE_INTERVAL = new(0.25f);

        [Header("Major 4")]
        [SerializeField] private Major4StoragePuzzleBridge _bridge;

        [Header("Controls")]
        [SerializeField] private Button[] _cellButtons = new Button[Major4StoragePuzzleBridge.CELL_COUNT];
        [SerializeField] private Button _openButton;
        [SerializeField] private TMP_Text _selectedAddressText;
        [SerializeField] private TMP_Text _attemptsText;

        [Header("Button Colors")]
        [SerializeField] private Color _normalColor = new(1f, 1f, 1f, 0f);
        [SerializeField] private Color _selectedColor = new(0.1f, 0.8f, 1f, 0.25f);

        [Header("Success")]
        [SerializeField] private bool _closeOnSolved = true;
        [SerializeField] private bool _submitSolvedCausality = true;

        private readonly Image[] _cellButtonImages = new Image[Major4StoragePuzzleBridge.CELL_COUNT];
        private Coroutine _resolveBridgeRoutine;
        private int _selectedCellIndex = -1;

        protected override bool WarnWhenSolvedTriggerMissing => false;

        private void OnValidate()
        {
            if (_cellButtons == null || _cellButtons.Length != Major4StoragePuzzleBridge.CELL_COUNT)
            {
                System.Array.Resize(ref _cellButtons, Major4StoragePuzzleBridge.CELL_COUNT);
            }
        }

        private void OnEnable()
        {
            CacheButtonImages();
            BindButtons();
            ResolveBridge();
            SubscribeToBridge();
            RefreshVisuals();
        }

        private void OnDisable()
        {
            UnbindButtons();
            StopResolveBridgeRoutine();
            UnsubscribeFromBridge();
        }

        protected override bool IsCorrectSolution()
        {
            return _bridge != null && _bridge.IsSolved;
        }

        protected override void HandleSolved()
        {
            SetButtonsInteractable(false);
            SubmitSolvedCausality();

            if (_closeOnSolved)
            {
                Close();
            }
        }

        private void BindButtons()
        {
            for (int i = 0; i < _cellButtons.Length; i++)
            {
                int cellIndex = i;
                Button button = _cellButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectCell(cellIndex));
            }

            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
                _openButton.onClick.AddListener(OpenSelectedCell);
            }
        }

        private void UnbindButtons()
        {
            for (int i = 0; i < _cellButtons.Length; i++)
            {
                if (_cellButtons[i] != null)
                {
                    _cellButtons[i].onClick.RemoveAllListeners();
                }
            }

            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
            }
        }

        private void SelectCell(int cellIndex)
        {
            if (IsSolved)
            {
                return;
            }

            _selectedCellIndex = cellIndex;
            RefreshVisuals();
        }

        private void OpenSelectedCell()
        {
            if (_bridge == null || _selectedCellIndex < 0 || IsSolved)
            {
                return;
            }

            _bridge.RequestOpenPastCell(_selectedCellIndex);
        }

        private void HandleBridgeStateChanged(Major4StoragePuzzleBridge bridge)
        {
            if (!bridge.IsSolved &&
                bridge.AttemptsUsed == 0 &&
                bridge.LastOpenedFutureCell < 0)
            {
                _selectedCellIndex = -1;
            }

            RefreshVisuals();
            TryCompletePuzzle();
        }

        private void CacheButtonImages()
        {
            for (int i = 0; i < _cellButtons.Length && i < _cellButtonImages.Length; i++)
            {
                if (_cellButtons[i] != null)
                {
                    _cellButtonImages[i] = _cellButtons[i].GetComponent<Image>();
                }
            }
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < _cellButtonImages.Length; i++)
            {
                if (_cellButtonImages[i] != null)
                {
                    bool isSelected = i == _selectedCellIndex;
                    bool isOpened = _bridge != null && _bridge.IsPastCellAttempted(i);
                    _cellButtonImages[i].color = isSelected || isOpened ? _selectedColor : _normalColor;
                }
            }

            if (_selectedAddressText != null)
            {
                _selectedAddressText.text = _selectedCellIndex >= 0
                    ? GetPastAddressLabel(_selectedCellIndex)
                    : string.Empty;
            }

            if (_attemptsText != null)
            {
                _attemptsText.text = _bridge != null
                    ? $"{_bridge.RemainingAttempts}/{_bridge.MaxAttempts}"
                    : string.Empty;
            }

            if (_openButton != null)
            {
                _openButton.interactable = !IsSolved && _selectedCellIndex >= 0 && _bridge != null;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < _cellButtons.Length; i++)
            {
                if (_cellButtons[i] != null)
                {
                    _cellButtons[i].interactable = interactable;
                }
            }

            if (_openButton != null)
            {
                _openButton.interactable = interactable && _selectedCellIndex >= 0 && _bridge != null;
            }
        }

        private void SubmitSolvedCausality()
        {
            if (!_submitSolvedCausality)
            {
                return;
            }

            CausalityManager causalityManager = FindAnyObjectByType<CausalityManager>();
            if (causalityManager == null)
            {
                Debug.LogWarning("Major4 solved but no CausalityManager was found.", this);
                return;
            }

            causalityManager.SubmitPuzzleSolvedTrigger(
                SOLVED_TRIGGER_ID,
                SOLVED_CONDITION_KEY,
                SOLVED_CONDITION_VALUE);
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

        private static string GetPastAddressLabel(int cellIndex)
        {
            int row = cellIndex / Major4StoragePuzzleBridge.GRID_SIZE;
            int column = cellIndex % Major4StoragePuzzleBridge.GRID_SIZE;
            int regionIndex = row / Major4StoragePuzzleBridge.REGION_SIZE * 2 + column / Major4StoragePuzzleBridge.REGION_SIZE;
            char regionLabel = (char)('A' + regionIndex);
            char rowLabel = (char)('a' + row % Major4StoragePuzzleBridge.REGION_SIZE);
            int columnLabel = column % Major4StoragePuzzleBridge.REGION_SIZE + 1;

            return $"{regionLabel}-{rowLabel}{columnLabel}";
        }
    }
}
