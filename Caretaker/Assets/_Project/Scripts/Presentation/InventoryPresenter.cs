using Caretaker.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Caretaker.Presentation
{
    /// <summary>
    /// 로컬 플레이어 인벤토리를 HUD 슬롯 바와 상세 팝업으로 표시한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryPresenter : MonoBehaviour
    {
        private const int HUD_SLOT_COUNT = InventoryService.MAX_SLOT_COUNT;
        private const int POPUP_STORAGE_SLOT_COUNT = 10;
        private const string EMPTY_DETAIL_NAME = "No Item";
        private const string EMPTY_DETAIL_DESCRIPTION = "Select an item slot.";

        [Header("HUD Slots")]
        [SerializeField] private InventorySlotPresenter[] _hudSlots;

        [Header("Popup")]
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private InventorySlotPresenter[] _popupPrimarySlots;
        [SerializeField] private InventorySlotPresenter[] _popupStorageSlots;
        [SerializeField] private TMP_Text _detailNameText;
        [SerializeField] private TMP_Text _detailDescriptionText;
        [SerializeField] private Button _closeButton;

        private InventoryController _inventoryController;
        private InventoryState _currentState;
        private int _detailSlotIndex = -1;
        private int _dragSourceSlotIndex = -1;

        /// <summary>인벤토리 팝업이 열려 있는지 반환한다.</summary>
        public bool IsPopupOpen => _popupRoot != null && _popupRoot.activeSelf;

        private void Awake()
        {
            ConfigureSlots();
            SetPopupVisible(false);
            RenderInventory(null);
        }

        private void OnEnable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(ClosePopup);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.iKey.wasPressedThisFrame)
            {
                TogglePopup();
            }
        }

        private void OnDisable()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(ClosePopup);
            }

            UnbindInventory();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ConfigureSlots();
        }
#endif

        /// <summary>
        /// 표시할 인벤토리 컨트롤러를 연결한다.
        /// </summary>
        public void BindInventory(InventoryController inventoryController)
        {
            if (_inventoryController == inventoryController)
            {
                return;
            }

            UnbindInventory();
            _inventoryController = inventoryController;

            if (_inventoryController != null)
            {
                _inventoryController.OnInventoryChanged += HandleInventoryChanged;
                RenderInventory(_inventoryController.State);
            }
            else
            {
                RenderInventory(null);
            }
        }

        /// <summary>
        /// 현재 연결된 인벤토리 컨트롤러를 해제한다.
        /// </summary>
        public void UnbindInventory()
        {
            if (_inventoryController != null)
            {
                _inventoryController.OnInventoryChanged -= HandleInventoryChanged;
                _inventoryController = null;
            }
        }

        /// <summary>
        /// 팝업 표시 여부를 전환한다.
        /// </summary>
        public void TogglePopup()
        {
            SetPopupVisible(!IsPopupOpen);
        }

        /// <summary>
        /// 인벤토리 팝업을 닫는다.
        /// </summary>
        public void ClosePopup()
        {
            SetPopupVisible(false);
        }

        /// <summary>
        /// 슬롯 클릭 입력을 처리한다.
        /// </summary>
        public void HandleSlotClicked(int slotIndex)
        {
            if (!HasItemAt(slotIndex))
            {
                _detailSlotIndex = -1;
                RenderDetail();
                return;
            }

            _detailSlotIndex = slotIndex;
            if (_inventoryController != null)
            {
                _inventoryController.SelectSlot(slotIndex);
                return;
            }

            RenderInventory(_currentState);
        }

        /// <summary>
        /// 슬롯 드래그 시작 입력을 처리한다.
        /// </summary>
        public void HandleSlotDragStarted(int slotIndex)
        {
            _dragSourceSlotIndex = HasItemAt(slotIndex) ? slotIndex : -1;
        }

        /// <summary>
        /// 현재 드래그 중인 슬롯을 대상 슬롯에 놓는 입력을 처리한다.
        /// </summary>
        public void HandleSlotDropped(int targetSlotIndex)
        {
            if (_dragSourceSlotIndex < 0 ||
                targetSlotIndex < 0 ||
                _dragSourceSlotIndex == targetSlotIndex ||
                !HasItemAt(targetSlotIndex))
            {
                return;
            }

            _detailSlotIndex = targetSlotIndex;
            _inventoryController?.MoveSlot(_dragSourceSlotIndex, targetSlotIndex);
        }

        /// <summary>
        /// 슬롯 드래그 종료 입력을 처리한다.
        /// </summary>
        public void HandleSlotDragEnded()
        {
            _dragSourceSlotIndex = -1;
        }

        private void ConfigureSlots()
        {
            ConfigureSlotArray(_hudSlots, isSelectable: true, acceptsDrop: true);
            ConfigureSlotArray(_popupPrimarySlots, isSelectable: true, acceptsDrop: true);
            ConfigureSlotArray(_popupStorageSlots, isSelectable: false, acceptsDrop: false);
        }

        private void ConfigureSlotArray(
            InventorySlotPresenter[] slots,
            bool isSelectable,
            bool acceptsDrop)
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].Configure(this, i, isSelectable, acceptsDrop);
                }
            }
        }

        private void HandleInventoryChanged(InventoryState state)
        {
            RenderInventory(state);
        }

        private void RenderInventory(InventoryState state)
        {
            _currentState = state;
            int selectedSlotIndex = ResolveSelectedSlotIndex(state);

            RenderInteractiveSlots(_hudSlots, selectedSlotIndex);
            RenderInteractiveSlots(_popupPrimarySlots, selectedSlotIndex);
            RenderStorageSlots();

            if (_detailSlotIndex < 0 && selectedSlotIndex >= 0)
            {
                _detailSlotIndex = selectedSlotIndex;
            }

            RenderDetail();
        }

        private void RenderInteractiveSlots(
            InventorySlotPresenter[] slots,
            int selectedSlotIndex)
        {
            if (slots == null)
            {
                return;
            }

            int slotCount = Mathf.Min(HUD_SLOT_COUNT, slots.Length);
            for (int i = 0; i < slotCount; i++)
            {
                string itemId = GetItemIdAt(i);
                TryGetItemDefinition(itemId, out ItemDefinitionSO itemDefinition);
                slots[i].Render(
                    itemId,
                    itemDefinition,
                    i == selectedSlotIndex,
                    (i + 1).ToString(),
                    isEnabled: true);
            }
        }

        private void RenderStorageSlots()
        {
            if (_popupStorageSlots == null)
            {
                return;
            }

            int slotCount = Mathf.Min(POPUP_STORAGE_SLOT_COUNT, _popupStorageSlots.Length);
            for (int i = 0; i < slotCount; i++)
            {
                _popupStorageSlots[i].Render(
                    string.Empty,
                    null,
                    isSelected: false,
                    string.Empty,
                    isEnabled: false);
            }
        }

        private void RenderDetail()
        {
            string itemId = HasItemAt(_detailSlotIndex)
                ? GetItemIdAt(_detailSlotIndex)
                : null;

            TryGetItemDefinition(itemId, out ItemDefinitionSO itemDefinition);
            if (_detailNameText != null)
            {
                _detailNameText.text = BuildDetailName(itemId, itemDefinition);
            }

            if (_detailDescriptionText != null)
            {
                _detailDescriptionText.text = BuildDetailDescription(itemDefinition);
            }
        }

        private void SetPopupVisible(bool isVisible)
        {
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(isVisible);
            }
        }

        private bool HasItemAt(int slotIndex)
        {
            return !string.IsNullOrWhiteSpace(GetItemIdAt(slotIndex));
        }

        private string GetItemIdAt(int slotIndex)
        {
            if (_currentState == null ||
                slotIndex < 0 ||
                slotIndex >= _currentState.OwnedItemIds.Count)
            {
                return string.Empty;
            }

            return _currentState.OwnedItemIds[slotIndex];
        }

        private bool TryGetItemDefinition(string itemId, out ItemDefinitionSO itemDefinition)
        {
            itemDefinition = null;
            return _inventoryController != null &&
                !string.IsNullOrWhiteSpace(itemId) &&
                _inventoryController.TryGetItemDefinition(itemId, out itemDefinition);
        }

        private static int ResolveSelectedSlotIndex(InventoryState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.SelectedItemId))
            {
                return -1;
            }

            for (int i = 0; i < state.OwnedItemIds.Count; i++)
            {
                if (state.OwnedItemIds[i] == state.SelectedItemId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string BuildDetailName(string itemId, ItemDefinitionSO itemDefinition)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return EMPTY_DETAIL_NAME;
            }

            if (itemDefinition != null && !string.IsNullOrWhiteSpace(itemDefinition.DisplayName))
            {
                return itemDefinition.DisplayName;
            }

            return itemId;
        }

        private static string BuildDetailDescription(ItemDefinitionSO itemDefinition)
        {
            if (itemDefinition == null || string.IsNullOrWhiteSpace(itemDefinition.Description))
            {
                return EMPTY_DETAIL_DESCRIPTION;
            }

            return itemDefinition.Description;
        }
    }
}
