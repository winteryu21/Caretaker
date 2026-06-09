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
        /// 플레이어가 사용할 수 있는 최대 인벤토리 슬롯 수.
        /// </summary>
        public const int MAX_SLOT_COUNT = 5;

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
            if (state.OwnedItemIds.Contains(itemId) || state.OwnedItemIds.Count >= MAX_SLOT_COUNT)
            {
                return false;
            }

            state.OwnedItemIds.Add(itemId);
            if (string.IsNullOrEmpty(state.SelectedItemId))
            {
                state.SelectedItemId = itemId;
            }

            return true;
        }

        /// <summary>
        /// 0부터 시작하는 슬롯 인덱스로 보유 아이템을 선택한다.
        /// </summary>
        public bool SelectSlot(ulong playerId, int slotIndex)
        {
            InventoryState state = GetOrCreateState(playerId);
            if (slotIndex < 0 || slotIndex >= state.OwnedItemIds.Count)
            {
                return false;
            }

            state.SelectedItemId = state.OwnedItemIds[slotIndex];
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
            if (!state.OwnedItemIds.Contains(itemId))
            {
                return false;
            }

            state.SelectedItemId = itemId;
            return true;
        }

        /// <summary>
        /// 보유 아이템 목록 안에서 슬롯 위치를 바꾼다.
        /// </summary>
        public bool MoveItem(ulong playerId, int fromSlotIndex, int toSlotIndex)
        {
            InventoryState state = GetOrCreateState(playerId);
            if (fromSlotIndex < 0 ||
                fromSlotIndex >= state.OwnedItemIds.Count ||
                toSlotIndex < 0 ||
                toSlotIndex >= state.OwnedItemIds.Count ||
                fromSlotIndex == toSlotIndex)
            {
                return false;
            }

            string itemId = state.OwnedItemIds[fromSlotIndex];
            state.OwnedItemIds.RemoveAt(fromSlotIndex);
            state.OwnedItemIds.Insert(toSlotIndex, itemId);
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

            if (!state.ConsumedItemIds.Contains(itemId))
            {
                state.ConsumedItemIds.Add(itemId);
            }

            if (state.SelectedItemId == itemId)
            {
                state.SelectedItemId = state.OwnedItemIds.Count > 0 ? state.OwnedItemIds[0] : null;
            }

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
                return state;
            }

            state = new InventoryState
            {
                PlayerId = playerId
            };
            _statesByPlayerId.Add(playerId, state);
            return state;
        }
    }
}
