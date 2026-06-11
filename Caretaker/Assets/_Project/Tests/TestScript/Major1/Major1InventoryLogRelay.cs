using System.Collections.Generic;

using UnityEngine;

using Caretaker.Gameplay;

namespace Caretaker.Presentation
{
    [AddComponentMenu("Caretaker/Test/Major1/Inventory Log Relay")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventoryController))]
    public sealed class Major1InventoryLogRelay : MonoBehaviour
    {
        [SerializeField] private string _batteryItemId = "ITEM_BATTERY";
        [SerializeField] private string _cableItemId = "ITEM_CABLE";

        private readonly HashSet<string> _knownItemIds = new();
        private InventoryController _inventoryController;

        private void Awake()
        {
            _inventoryController = GetComponent<InventoryController>();
            SnapshotKnownItems();
        }

        private void OnEnable()
        {
            if (_inventoryController == null)
            {
                _inventoryController = GetComponent<InventoryController>();
            }

            _inventoryController.OnInventoryChanged += HandleInventoryChanged;
        }

        private void OnDisable()
        {
            if (_inventoryController != null)
            {
                _inventoryController.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        private void HandleInventoryChanged(InventoryState state)
        {
            if (state == null)
            {
                return;
            }

            for (int i = 0; i < state.OwnedItemIds.Count; i++)
            {
                string itemId = state.OwnedItemIds[i];
                if (string.IsNullOrWhiteSpace(itemId) || !_knownItemIds.Add(itemId))
                {
                    continue;
                }

                if (itemId == _batteryItemId)
                {
                    Debug.Log("Major1: 배터리를 획득했습니다.", this);
                }
                else if (itemId == _cableItemId)
                {
                    Debug.Log("Major1: 케이블을 획득했습니다.", this);
                }
            }
        }

        private void SnapshotKnownItems()
        {
            if (_inventoryController == null)
            {
                return;
            }

            IReadOnlyList<string> ownedItemIds = _inventoryController.OwnedItemIds;
            for (int i = 0; i < ownedItemIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(ownedItemIds[i]))
                {
                    _knownItemIds.Add(ownedItemIds[i]);
                }
            }
        }
    }
}
