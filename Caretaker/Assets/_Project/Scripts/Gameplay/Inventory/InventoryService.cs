using System.Collections.Generic;

using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 아이템 획득, 선택, 사용, 소모 규칙을 관리한다.
    /// </summary>
    public class InventoryService
    {
        /// <summary>
        /// 숫자키로 선택할 수 있는 빠른 슬롯 수.
        /// </summary>
        public const int HOTBAR_SLOT_COUNT = 5;

        /// <summary>
        /// 인벤토리 팝업에서 사용할 보관 슬롯 수.
        /// </summary>
        public const int STORAGE_SLOT_COUNT = 10;

        /// <summary>
        /// 플레이어가 사용할 수 있는 전체 인벤토리 슬롯 수.
        /// </summary>
        public const int MAX_SLOT_COUNT = HOTBAR_SLOT_COUNT + STORAGE_SLOT_COUNT;

        private readonly Dictionary<ulong, InventoryState> _statesByPlayerId = new();

        /// <summary>
        /// 플레이어 인벤토리에 아이템 추가를 시도한다.
        /// </summary>
        public bool AcquireItem(ulong playerId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            InventoryState state = GetOrCreateState(playerId);
            int slotIndex = FindFirstEmptySlot(state);
            if (state.OwnedItemIds.Contains(itemId) || slotIndex < 0)
            {
                return false;
            }

            state.OwnedItemIds.Add(itemId);
            state.SlotItemIds[slotIndex] = itemId;
            if (string.IsNullOrEmpty(state.SelectedItemId))
            {
                state.SelectedItemId = FindFirstHotbarItem(state);
            }

            return true;
        }

        /// <summary>
        /// 0부터 시작하는 슬롯 인덱스로 보유 아이템을 선택한다.
        /// </summary>
        public bool SelectSlot(ulong playerId, int slotIndex)
        {
            InventoryState state = GetOrCreateState(playerId);
            if (!IsHotbarSlot(slotIndex))
            {
                return false;
            }

            string itemId = state.SlotItemIds[slotIndex];
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            state.SelectedItemId = itemId;
            return true;
        }

        /// <summary>
        /// 아이템 ID로 보유 아이템을 선택한다.
        /// </summary>
        public bool SelectItem(ulong playerId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            InventoryState state = GetOrCreateState(playerId);
            if (!state.OwnedItemIds.Contains(itemId) || !IsItemInHotbar(state, itemId))
            {
                return false;
            }

            state.SelectedItemId = itemId;
            return true;
        }

        /// <summary>
        /// 전체 인벤토리 슬롯 사이에서 아이템 위치를 옮기거나 교환한다.
        /// </summary>
        public bool MoveItem(ulong playerId, int fromSlotIndex, int toSlotIndex)
        {
            InventoryState state = GetOrCreateState(playerId);
            if (!IsInventorySlot(fromSlotIndex) ||
                !IsInventorySlot(toSlotIndex) ||
                fromSlotIndex == toSlotIndex)
            {
                return false;
            }

            string fromItemId = state.SlotItemIds[fromSlotIndex];
            if (string.IsNullOrWhiteSpace(fromItemId))
            {
                return false;
            }

            string toItemId = state.SlotItemIds[toSlotIndex];
            state.SlotItemIds[toSlotIndex] = fromItemId;
            state.SlotItemIds[fromSlotIndex] = toItemId;
            RefreshSelectedHotbarItem(state);
            return true;
        }

        /// <summary>
        /// 보유 아이템을 대상 요구 조건에 맞춰 사용하고, 필요하면 소모한다.
        /// </summary>
        public bool UseItem(ulong playerId, string itemId, string requiredItemId, bool consumable)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.Log("Use item failed: item id is empty.");
                return false;
            }

            InventoryState state = GetOrCreateState(playerId);
            if (!state.OwnedItemIds.Contains(itemId))
            {
                Debug.Log($"Use item failed: player does not own item. playerId={playerId}, itemId={itemId}");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requiredItemId) && itemId != requiredItemId)
            {
                Debug.Log(
                    $"Use item failed: wrong item for target. playerId={playerId}, itemId={itemId}, requiredItem={requiredItemId}");
                return false;
            }

            return !consumable || ConsumeItem(playerId, itemId);
        }

        /// <summary>
        /// 보유 아이템을 제거하고 소모 목록에 기록한다.
        /// </summary>
        public bool ConsumeItem(ulong playerId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.Log("Consume item failed: item id is empty.");
                return false;
            }

            InventoryState state = GetOrCreateState(playerId);
            if (!state.OwnedItemIds.Remove(itemId))
            {
                Debug.Log($"Consume item failed: player does not own item. playerId={playerId}, itemId={itemId}");
                return false;
            }

            ClearItemFromSlots(state, itemId);

            if (!state.ConsumedItemIds.Contains(itemId))
            {
                state.ConsumedItemIds.Add(itemId);
            }

            RefreshSelectedHotbarItem(state);

            return true;
        }

        /// <summary>
        /// 플레이어의 변경 가능한 인벤토리 상태를 반환한다.
        /// </summary>
        public InventoryState GetState(ulong playerId)
        {
            return GetOrCreateState(playerId);
        }

        private InventoryState GetOrCreateState(ulong playerId)
        {
            if (_statesByPlayerId.TryGetValue(playerId, out InventoryState state))
            {
                EnsureSlotState(state);
                return state;
            }

            state = new InventoryState
            {
                PlayerId = playerId
            };
            EnsureSlotState(state);
            _statesByPlayerId.Add(playerId, state);
            return state;
        }

        private static void EnsureSlotState(InventoryState state)
        {
            while (state.SlotItemIds.Count < MAX_SLOT_COUNT)
            {
                state.SlotItemIds.Add(string.Empty);
            }

            if (state.SlotItemIds.Count > MAX_SLOT_COUNT)
            {
                state.SlotItemIds.RemoveRange(MAX_SLOT_COUNT, state.SlotItemIds.Count - MAX_SLOT_COUNT);
            }

            for (int i = 0; i < state.OwnedItemIds.Count; i++)
            {
                string itemId = state.OwnedItemIds[i];
                if (!string.IsNullOrWhiteSpace(itemId) && FindSlotIndex(state, itemId) < 0)
                {
                    int emptySlotIndex = FindFirstEmptySlot(state);
                    if (emptySlotIndex >= 0)
                    {
                        state.SlotItemIds[emptySlotIndex] = itemId;
                    }
                }
            }

            for (int i = 0; i < state.SlotItemIds.Count; i++)
            {
                string itemId = state.SlotItemIds[i];
                if (!string.IsNullOrWhiteSpace(itemId) && !state.OwnedItemIds.Contains(itemId))
                {
                    state.OwnedItemIds.Add(itemId);
                }
            }

            RefreshSelectedHotbarItem(state);
        }

        private static void RefreshSelectedHotbarItem(InventoryState state)
        {
            if (!string.IsNullOrWhiteSpace(state.SelectedItemId) &&
                IsItemInHotbar(state, state.SelectedItemId))
            {
                return;
            }

            state.SelectedItemId = FindFirstHotbarItem(state);
        }

        private static string FindFirstHotbarItem(InventoryState state)
        {
            for (int i = 0; i < HOTBAR_SLOT_COUNT; i++)
            {
                string itemId = state.SlotItemIds[i];
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    return itemId;
                }
            }

            return null;
        }

        private static int FindFirstEmptySlot(InventoryState state)
        {
            for (int i = 0; i < state.SlotItemIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(state.SlotItemIds[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindSlotIndex(InventoryState state, string itemId)
        {
            for (int i = 0; i < state.SlotItemIds.Count; i++)
            {
                if (state.SlotItemIds[i] == itemId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void ClearItemFromSlots(InventoryState state, string itemId)
        {
            for (int i = 0; i < state.SlotItemIds.Count; i++)
            {
                if (state.SlotItemIds[i] == itemId)
                {
                    state.SlotItemIds[i] = string.Empty;
                }
            }
        }

        private static bool IsItemInHotbar(InventoryState state, string itemId)
        {
            for (int i = 0; i < HOTBAR_SLOT_COUNT; i++)
            {
                if (state.SlotItemIds[i] == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsHotbarSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < HOTBAR_SLOT_COUNT;
        }

        private static bool IsInventorySlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MAX_SLOT_COUNT;
        }
    }
}
