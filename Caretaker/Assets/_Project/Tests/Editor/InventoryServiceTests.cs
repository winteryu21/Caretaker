using NUnit.Framework;

using Caretaker.Gameplay;

namespace Caretaker.Tests.Editor
{
    public class InventoryServiceTests
    {
        private const ulong PLAYER_ID = 1;

        [Test]
        public void AcquireItem_AddsItemAndSelectsFirstItem()
        {
            InventoryService inventoryService = new();

            bool acquired = inventoryService.AcquireItem(PLAYER_ID, "ITEM_KEY_CARD");
            InventoryState state = inventoryService.GetState(PLAYER_ID);

            Assert.That(acquired, Is.True);
            Assert.That(state.OwnedItemIds, Does.Contain("ITEM_KEY_CARD"));
            Assert.That(state.SlotItemIds[0], Is.EqualTo("ITEM_KEY_CARD"));
            Assert.That(state.SelectedItemId, Is.EqualTo("ITEM_KEY_CARD"));
        }

        [Test]
        public void AcquireItem_RejectsDuplicateItem()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_KEY_CARD");

            bool acquiredAgain = inventoryService.AcquireItem(PLAYER_ID, "ITEM_KEY_CARD");

            Assert.That(acquiredAgain, Is.False);
            Assert.That(inventoryService.GetState(PLAYER_ID).OwnedItemIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void AcquireItem_AllowsStorageSlots()
        {
            InventoryService inventoryService = new();

            for (int i = 1; i <= InventoryService.MAX_SLOT_COUNT; i++)
            {
                inventoryService.AcquireItem(PLAYER_ID, $"ITEM_{i}");
            }

            InventoryState state = inventoryService.GetState(PLAYER_ID);

            Assert.That(state.OwnedItemIds.Count, Is.EqualTo(InventoryService.MAX_SLOT_COUNT));
            Assert.That(state.SlotItemIds[InventoryService.HOTBAR_SLOT_COUNT], Is.EqualTo("ITEM_6"));
        }

        [Test]
        public void AcquireItem_RejectsMoreThanInventoryCapacity()
        {
            InventoryService inventoryService = new();

            for (int i = 1; i <= InventoryService.MAX_SLOT_COUNT; i++)
            {
                inventoryService.AcquireItem(PLAYER_ID, $"ITEM_{i}");
            }

            bool acquiredOverflow = inventoryService.AcquireItem(PLAYER_ID, "ITEM_OVERFLOW");

            Assert.That(acquiredOverflow, Is.False);
            Assert.That(inventoryService.GetState(PLAYER_ID).OwnedItemIds.Count, Is.EqualTo(InventoryService.MAX_SLOT_COUNT));
        }

        [Test]
        public void SelectSlot_SelectsOwnedItemByIndex()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_1");
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_2");

            bool selected = inventoryService.SelectSlot(PLAYER_ID, 1);

            Assert.That(selected, Is.True);
            Assert.That(inventoryService.GetState(PLAYER_ID).SelectedItemId, Is.EqualTo("ITEM_2"));
        }

        [Test]
        public void SelectSlot_RejectsStorageSlot()
        {
            InventoryService inventoryService = new();
            for (int i = 1; i <= InventoryService.HOTBAR_SLOT_COUNT + 1; i++)
            {
                inventoryService.AcquireItem(PLAYER_ID, $"ITEM_{i}");
            }

            bool selected = inventoryService.SelectSlot(PLAYER_ID, InventoryService.HOTBAR_SLOT_COUNT);

            Assert.That(selected, Is.False);
            Assert.That(inventoryService.GetState(PLAYER_ID).SelectedItemId, Is.EqualTo("ITEM_1"));
        }

        [Test]
        public void SelectSlot_RejectsEmptyOrOutOfRangeSlot()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_1");

            bool selected = inventoryService.SelectSlot(PLAYER_ID, 1);

            Assert.That(selected, Is.False);
            Assert.That(inventoryService.GetState(PLAYER_ID).SelectedItemId, Is.EqualTo("ITEM_1"));
        }

        [Test]
        public void MoveItem_MovesItemIntoEmptyStorageSlot()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_KEY_CARD");

            bool moved = inventoryService.MoveItem(PLAYER_ID, 0, InventoryService.HOTBAR_SLOT_COUNT);
            InventoryState state = inventoryService.GetState(PLAYER_ID);

            Assert.That(moved, Is.True);
            Assert.That(state.SlotItemIds[0], Is.Empty);
            Assert.That(state.SlotItemIds[InventoryService.HOTBAR_SLOT_COUNT], Is.EqualTo("ITEM_KEY_CARD"));
            Assert.That(state.OwnedItemIds, Does.Contain("ITEM_KEY_CARD"));
            Assert.That(state.SelectedItemId, Is.Null);
        }

        [Test]
        public void MoveItem_SwapsItemsBetweenStorageAndHotbar()
        {
            InventoryService inventoryService = new();
            for (int i = 1; i <= InventoryService.HOTBAR_SLOT_COUNT + 1; i++)
            {
                inventoryService.AcquireItem(PLAYER_ID, $"ITEM_{i}");
            }

            bool moved = inventoryService.MoveItem(PLAYER_ID, InventoryService.HOTBAR_SLOT_COUNT, 1);
            InventoryState state = inventoryService.GetState(PLAYER_ID);

            Assert.That(moved, Is.True);
            Assert.That(state.SlotItemIds[1], Is.EqualTo("ITEM_6"));
            Assert.That(state.SlotItemIds[InventoryService.HOTBAR_SLOT_COUNT], Is.EqualTo("ITEM_2"));
        }

        [Test]
        public void MoveItem_RejectsEmptyOrOutOfRangeSlot()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_1");

            bool movedEmpty = inventoryService.MoveItem(PLAYER_ID, 1, 2);
            bool movedOutOfRange = inventoryService.MoveItem(PLAYER_ID, 0, InventoryService.MAX_SLOT_COUNT);

            Assert.That(movedEmpty, Is.False);
            Assert.That(movedOutOfRange, Is.False);
            Assert.That(inventoryService.GetState(PLAYER_ID).SlotItemIds[0], Is.EqualTo("ITEM_1"));
        }

        [Test]
        public void UseItem_SucceedsWhenRequiredItemMatches()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_KEY_CARD");

            bool used = inventoryService.UseItem(PLAYER_ID, "ITEM_KEY_CARD", "ITEM_KEY_CARD", false);

            Assert.That(used, Is.True);
            Assert.That(inventoryService.GetState(PLAYER_ID).OwnedItemIds, Does.Contain("ITEM_KEY_CARD"));
        }

        [Test]
        public void UseItem_RejectsMismatchedRequiredItem()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_KEY_CARD");

            bool used = inventoryService.UseItem(PLAYER_ID, "ITEM_KEY_CARD", "ITEM_BATTERY", false);

            Assert.That(used, Is.False);
            Assert.That(inventoryService.GetState(PLAYER_ID).OwnedItemIds, Does.Contain("ITEM_KEY_CARD"));
        }

        [Test]
        public void UseItem_RemovesConsumableItem()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_BATTERY");

            bool used = inventoryService.UseItem(PLAYER_ID, "ITEM_BATTERY", "ITEM_BATTERY", true);
            InventoryState state = inventoryService.GetState(PLAYER_ID);

            Assert.That(used, Is.True);
            Assert.That(state.OwnedItemIds, Does.Not.Contain("ITEM_BATTERY"));
            Assert.That(state.SlotItemIds[0], Is.Empty);
            Assert.That(state.ConsumedItemIds, Does.Contain("ITEM_BATTERY"));
        }
    }
}
