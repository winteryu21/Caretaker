using Caretaker.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caretaker.Presentation
{
    /// <summary>
    /// HUD와 인벤토리 팝업의 단일 아이템 슬롯을 렌더링하고 클릭/드래그 입력을 전달한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventorySlotPresenter :
        MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        [Header("View")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _keyText;
        [SerializeField] private GameObject _selectedIndicator;

        [Header("Colors")]
        [SerializeField] private Color _emptyColor = new(0.08f, 0.09f, 0.12f, 0.88f);
        [SerializeField] private Color _filledColor = new(0.13f, 0.16f, 0.21f, 0.96f);
        [SerializeField] private Color _selectedColor = new(0.32f, 0.72f, 1f, 1f);
        [SerializeField] private Color _disabledColor = new(0.04f, 0.04f, 0.05f, 0.55f);

        private InventoryPresenter _owner;
        private int _slotIndex = -1;
        private bool _acceptsDrop;
        private bool _isSelectable;
        private bool _hasItem;
        private string _itemId = string.Empty;

        /// <summary>이 슬롯이 표시하는 인벤토리 인덱스.</summary>
        public int SlotIndex => _slotIndex;

        /// <summary>현재 표시 중인 아이템 ID.</summary>
        public string ItemId => _itemId;

        /// <summary>현재 슬롯에 아이템이 있는지 반환한다.</summary>
        public bool HasItem => _hasItem;

        /// <summary>
        /// 슬롯 소유자와 동작 속성을 설정한다.
        /// </summary>
        public void Configure(
            InventoryPresenter owner,
            int slotIndex,
            bool isSelectable,
            bool acceptsDrop)
        {
            _owner = owner;
            _slotIndex = slotIndex;
            _isSelectable = isSelectable;
            _acceptsDrop = acceptsDrop;
        }

        /// <summary>
        /// 슬롯 내용을 렌더링한다.
        /// </summary>
        public void Render(
            string itemId,
            ItemDefinitionSO itemDefinition,
            bool isSelected,
            string keyLabel,
            bool isEnabled)
        {
            _itemId = itemId ?? string.Empty;
            _hasItem = !string.IsNullOrWhiteSpace(_itemId);

            if (_backgroundImage != null)
            {
                _backgroundImage.color = !isEnabled
                    ? _disabledColor
                    : _hasItem
                        ? _filledColor
                        : _emptyColor;
            }

            if (_iconImage != null)
            {
                Sprite icon = itemDefinition != null ? itemDefinition.Icon : null;
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
                _iconImage.preserveAspect = true;
            }

            if (_nameText != null)
            {
                _nameText.text = BuildItemLabel(_itemId, itemDefinition, isEnabled);
                _nameText.alpha = isEnabled ? 1f : 0.45f;
            }

            if (_keyText != null)
            {
                _keyText.text = keyLabel ?? string.Empty;
            }

            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(isSelected);
                Image selectedImage = _selectedIndicator.GetComponent<Image>();
                if (selectedImage != null)
                {
                    selectedImage.color = _selectedColor;
                }
            }
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isSelectable || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _owner?.HandleSlotClicked(_slotIndex);
        }

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_acceptsDrop || !_hasItem)
            {
                return;
            }

            _owner?.HandleSlotDragStarted(_slotIndex);
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
        }

        /// <inheritdoc />
        public void OnEndDrag(PointerEventData eventData)
        {
            _owner?.HandleSlotDragEnded();
        }

        /// <inheritdoc />
        public void OnDrop(PointerEventData eventData)
        {
            if (!_acceptsDrop)
            {
                return;
            }

            _owner?.HandleSlotDropped(_slotIndex);
        }

        private static string BuildItemLabel(
            string itemId,
            ItemDefinitionSO itemDefinition,
            bool isEnabled)
        {
            if (!isEnabled)
            {
                return "Locked";
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                return string.Empty;
            }

            if (itemDefinition != null && !string.IsNullOrWhiteSpace(itemDefinition.DisplayName))
            {
                return itemDefinition.DisplayName;
            }

            return itemId;
        }
    }
}
