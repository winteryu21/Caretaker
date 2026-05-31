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
        public void AcquireItem_RejectsMoreThanFiveItems()
        {
            InventoryService inventoryService = new();

            inventoryService.AcquireItem(PLAYER_ID, "ITEM_1");
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_2");
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_3");
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_4");
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_5");
            bool acquiredSixth = inventoryService.AcquireItem(PLAYER_ID, "ITEM_6");

            Assert.That(acquiredSixth, Is.False);
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
        public void SelectSlot_RejectsEmptyOrOutOfRangeSlot()
        {
            InventoryService inventoryService = new();
            inventoryService.AcquireItem(PLAYER_ID, "ITEM_1");

            bool selected = inventoryService.SelectSlot(PLAYER_ID, 1);

            Assert.That(selected, Is.False);
            Assert.That(inventoryService.GetState(PLAYER_ID).SelectedItemId, Is.EqualTo("ITEM_1"));
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
            Assert.That(state.ConsumedItemIds, Does.Contain("ITEM_BATTERY"));
        }
    }
}
