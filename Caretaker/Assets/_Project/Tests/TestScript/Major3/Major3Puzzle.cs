using UnityEngine;
using UnityEngine.UI;

namespace Caretaker.Presentation
{
    public sealed class BlueprintSelectionPuzzleUI : PuzzleUIBase
    {
        public enum BlueprintOption
        {
            AB,
            AC,
            AD,
            AE,
            BC,
            BD,
            BE,
            CD,
            CE,
            DE
        }

        [Header("정답")]
        [SerializeField] private BlueprintOption _correctOptionA = BlueprintOption.AE;
        [SerializeField] private BlueprintOption _correctOptionB = BlueprintOption.CE;

        [Header("선택지 버튼")]
        [SerializeField] private Button[] _optionButtons = new Button[10];

        [Header("선택 색상")]
        [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.8f, 1f, 0.25f);

        [Header("성공 후 처리")]
        [SerializeField] private bool _closeOnSolved = true;

        private BlueprintOption? _selectedA;
        private BlueprintOption? _selectedB;

        private void OnEnable()
        {
            BindButtons();
            RefreshVisuals();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private void BindButtons()
        {
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                int optionIndex = i;

                if (_optionButtons[i] == null)
                {
                    continue;
                }

                _optionButtons[i].onClick.RemoveAllListeners();
                _optionButtons[i].onClick.AddListener(() => HandleOptionClicked(optionIndex));
            }
        }

        private void UnbindButtons()
        {
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                if (_optionButtons[i] == null)
                {
                    continue;
                }

                _optionButtons[i].onClick.RemoveAllListeners();
            }
        }

        private void HandleOptionClicked(int optionIndex)
        {
            if (IsSolved)
            {
                return;
            }

            BlueprintOption clickedOption = (BlueprintOption)optionIndex;

            // 이미 선택한 버튼을 다시 누르면 선택 해제
            if (_selectedA == clickedOption)
            {
                _selectedA = _selectedB;
                _selectedB = null;
                RefreshVisuals();
                return;
            }

            if (_selectedB == clickedOption)
            {
                _selectedB = null;
                RefreshVisuals();
                return;
            }

            // 첫 번째 선택
            if (_selectedA == null)
            {
                _selectedA = clickedOption;
                RefreshVisuals();
                return;
            }

            // 두 번째 선택
            if (_selectedB == null)
            {
                _selectedB = clickedOption;
                RefreshVisuals();
                return;
            }

            // 이미 2개가 선택된 상태에서 다른 것을 누르면 두 번째 선택만 교체
            _selectedB = clickedOption;
            RefreshVisuals();
        }

        protected override bool IsCorrectSolution()
        {
            if (_selectedA == null || _selectedB == null)
            {
                return false;
            }

            bool normalOrder =
                _selectedA == _correctOptionA &&
                _selectedB == _correctOptionB;

            bool reverseOrder =
                _selectedA == _correctOptionB &&
                _selectedB == _correctOptionA;

            return normalOrder || reverseOrder;
        }

        protected override void HandleOpened()
        {
            ResetSelections();
        }

        protected override void HandleIncorrectSolution()
        {
            ResetSelections();
        }

        protected override void HandleSolved()
        {
            SetButtonsInteractable(false);

            if (_closeOnSolved)
            {
                Close();
            }
        }

        private void ResetSelections()
        {
            _selectedA = null;
            _selectedB = null;

            SetButtonsInteractable(true);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                if (_optionButtons[i] == null)
                {
                    continue;
                }

                Image buttonImage = _optionButtons[i].GetComponent<Image>();
                if (buttonImage == null)
                {
                    continue;
                }

                BlueprintOption option = (BlueprintOption)i;
                bool isSelected = _selectedA == option || _selectedB == option;

                buttonImage.color = isSelected ? _selectedColor : _normalColor;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                if (_optionButtons[i] != null)
                {
                    _optionButtons[i].interactable = interactable;
                }
            }
        }
    }
}