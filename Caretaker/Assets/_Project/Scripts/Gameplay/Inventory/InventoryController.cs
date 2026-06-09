using System;
using System.Collections.Generic;

using Unity.Netcode;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 플레이어 입력과 오브젝트 상호작용 콜백을 인벤토리 상태에 연결한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryController : NetworkBehaviour
    {
        private const string ITEM_DEFINITION_FOLDER = "Assets/_Project/Data/Inventory";

        [Header("Inventory")]
        [SerializeField] private ulong _playerId;
        [SerializeField] private ItemDefinitionSO[] _itemDefinitions;

        private readonly InventoryService _inventoryService = new();

        private PlayerController _playerController;

        /// <summary>
        /// 로컬 인벤토리 상태가 변경된 뒤 발생한다.
        /// </summary>
        public event Action<InventoryState> OnInventoryChanged;

        /// <summary>
        /// 이 인벤토리 상태가 속한 플레이어 ID.
        /// </summary>
        public ulong PlayerId => _playerId;

        /// <summary>
        /// 현재 플레이어의 인벤토리 상태.
        /// </summary>
        public InventoryState State => _inventoryService.GetState(_playerId);

        /// <summary>
        /// 현재 플레이어가 보유한 아이템 ID 목록.
        /// </summary>
        public IReadOnlyList<string> OwnedItemIds => State.OwnedItemIds;

        /// <summary>
        /// 현재 선택된 아이템 ID.
        /// </summary>
        public string SelectedItemId => State.SelectedItemId;

        /// <summary>
        /// Sets the local inventory owner ID used to keep player inventory state private.
        /// </summary>
        /// <param name="playerId">Owner client ID.</param>
        public void SetPlayerId(ulong playerId)
        {
            _playerId = playerId;
        }

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();

            if (_itemDefinitions == null || _itemDefinitions.Length == 0)
            {
                Debug.LogWarning("Item definitions are not assigned.", this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            string[] assetGuids = AssetDatabase.FindAssets(
                "t:ItemDefinitionSO",
                new[] { ITEM_DEFINITION_FOLDER });
            List<ItemDefinitionSO> itemDefinitions = new(assetGuids.Length);

            for (int i = 0; i < assetGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                ItemDefinitionSO itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(assetPath);
                if (itemDefinition != null)
                {
                    itemDefinitions.Add(itemDefinition);
                }
            }

            _itemDefinitions = itemDefinitions.ToArray();
        }
#endif

        private void OnEnable()
        {
            _playerController.OnInventorySlotSelected += HandleInventorySlotSelected;
            _playerController.OnInteractionResolved += HandleInteractionResolved;
        }

        private void OnDisable()
        {
            _playerController.OnInventorySlotSelected -= HandleInventorySlotSelected;
            _playerController.OnInteractionResolved -= HandleInteractionResolved;
        }

        /// <summary>
        /// 아이템 ID로 획득을 시도한다.
        /// </summary>
        public bool AcquireItem(string itemId)
        {
            if (GetItemDefinition(itemId) == null)
            {
                Debug.LogWarning($"Item definition not found: itemId={itemId}", this);
                return false;
            }

            if (!ApplyAcquireItem(itemId))
            {
                return false;
            }

            if (ShouldMirrorToServer())
            {
                AcquireItemServerRpc(itemId);
            }

            return true;
        }

        /// <summary>
        /// 0부터 시작하는 인벤토리 슬롯 선택을 시도한다.
        /// </summary>
        public bool SelectSlot(int slotIndex)
        {
            if (!ApplySelectSlot(slotIndex))
            {
                return false;
            }

            if (ShouldMirrorToServer())
            {
                SelectSlotServerRpc(slotIndex);
            }

            return true;
        }

        /// <summary>
        /// 보유 아이템 슬롯 순서를 변경한다.
        /// </summary>
        public bool MoveSlot(int fromSlotIndex, int toSlotIndex)
        {
            if (!ApplyMoveSlot(fromSlotIndex, toSlotIndex))
            {
                return false;
            }

            if (ShouldMirrorToServer())
            {
                MoveSlotServerRpc(fromSlotIndex, toSlotIndex);
            }

            return true;
        }

        /// <summary>
        /// 아이템 ID에 해당하는 표시 데이터 조회를 시도한다.
        /// </summary>
        public bool TryGetItemDefinition(string itemId, out ItemDefinitionSO itemDefinition)
        {
            itemDefinition = GetItemDefinition(itemId);
            return itemDefinition != null;
        }

        /// <summary>
        /// 선택된 아이템을 상호작용 대상에 사용해 본다.
        /// </summary>
        public bool UseSelectedItemOn(InteractableObject target)
        {
            if (target == null || string.IsNullOrWhiteSpace(SelectedItemId))
            {
                Debug.Log(
                    $"Use selected item failed: target or selected item is missing. target={(target != null ? target.ObjectId : "null")}, selectedItem={SelectedItemId}",
                    this);
                return false;
            }

            string selectedItemId = SelectedItemId;
            ItemDefinitionSO itemDefinition = GetItemDefinition(selectedItemId);
            if (itemDefinition == null)
            {
                Debug.LogWarning($"Item definition not found: itemId={selectedItemId}", this);
                return false;
            }

            bool consumable = itemDefinition.Consumable;
            if (!ApplyUseItem(selectedItemId, target.RequiredItemId, consumable))
            {
                Debug.Log(
                    $"Use selected item failed: item does not satisfy target requirement. playerId={_playerId}, itemId={selectedItemId}, target={target.ObjectId}, requiredItem={target.RequiredItemId}",
                    this);
                return false;
            }

            if (ShouldMirrorToServer())
            {
                UseItemServerRpc(selectedItemId, target.RequiredItemId);
            }

            return true;
        }

        private void HandleInventorySlotSelected(int slotIndex)
        {
            SelectSlot(slotIndex);
        }

        private void HandleInteractionResolved(InteractableObject target, InteractionType interactionType)
        {
            if (interactionType != InteractionType.Acquire || target == null)
            {
                return;
            }

            if (AcquireItem(target.GrantedItemId))
            {
                target.MarkItemAcquired();
            }
        }

        private ItemDefinitionSO GetItemDefinition(string itemId)
        {
            if (_itemDefinitions == null)
            {
                return null;
            }

            for (int i = 0; i < _itemDefinitions.Length; i++)
            {
                ItemDefinitionSO itemDefinition = _itemDefinitions[i];
                if (itemDefinition != null && itemDefinition.ItemId == itemId)
                {
                    return itemDefinition;
                }
            }

            return null;
        }

        private bool ApplyAcquireItem(string itemId)
        {
            if (!_inventoryService.AcquireItem(_playerId, itemId))
            {
                return false;
            }

            NotifyInventoryChanged();
            return true;
        }

        private bool ApplySelectSlot(int slotIndex)
        {
            if (!_inventoryService.SelectSlot(_playerId, slotIndex))
            {
                return false;
            }

            NotifyInventoryChanged();
            return true;
        }

        private bool ApplyMoveSlot(int fromSlotIndex, int toSlotIndex)
        {
            if (!_inventoryService.MoveItem(_playerId, fromSlotIndex, toSlotIndex))
            {
                return false;
            }

            NotifyInventoryChanged();
            return true;
        }

        private bool ApplyUseItem(string itemId, string requiredItemId, bool consumable)
        {
            if (!_inventoryService.UseItem(_playerId, itemId, requiredItemId, consumable))
            {
                return false;
            }

            NotifyInventoryChanged();
            return true;
        }

        private bool ShouldMirrorToServer()
        {
            return IsSpawned && !IsServer;
        }

        [Rpc(SendTo.Server)]
        private void AcquireItemServerRpc(string itemId)
        {
            AcquireItem(itemId);
        }

        [Rpc(SendTo.Server)]
        private void SelectSlotServerRpc(int slotIndex)
        {
            SelectSlot(slotIndex);
        }

        [Rpc(SendTo.Server)]
        private void MoveSlotServerRpc(int fromSlotIndex, int toSlotIndex)
        {
            MoveSlot(fromSlotIndex, toSlotIndex);
        }

        [Rpc(SendTo.Server)]
        private void UseItemServerRpc(string itemId, string requiredItemId)
        {
            ItemDefinitionSO itemDefinition = GetItemDefinition(itemId);
            if (itemDefinition == null)
            {
                Debug.LogWarning($"Item definition not found: itemId={itemId}", this);
                return;
            }

            ApplyUseItem(itemId, requiredItemId, itemDefinition.Consumable);
        }

        private void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke(State);
        }
    }
}
