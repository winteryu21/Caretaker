using NUnit.Framework;

using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using Caretaker.Gameplay;
using Caretaker.Presentation;

using Object = UnityEngine.Object;

namespace Caretaker.Tests.Editor
{
    public class InventoryPresenterTests
    {
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string ITEM_ONE_PATH = "Assets/_Project/Data/Inventory/SO_ITEM_KEY_CARD.asset";
        private const string ITEM_TWO_PATH = "Assets/_Project/Data/Inventory/SO_ITEM_BATTERY.asset";
        private const string ITEM_THREE_PATH = "Assets/_Project/Data/Inventory/SO_ITEM_CABLE.asset";

        [Test]
        public void BindInventory_RendersHudSlotsAndSelectedItem()
        {
            InventoryController inventoryController = CreateInventoryController(
                out GameObject inventoryObject,
                out ItemDefinitionSO itemOne,
                out ItemDefinitionSO itemTwo);
            InventoryPresenter presenter = CreateInventoryPresenter(
                out GameObject presenterObject,
                out InventorySlotPresenter[] hudSlots,
                out GameObject[] selectedIndicators,
                out TMP_Text detailNameText,
                out _);

            try
            {
                inventoryController.AcquireItem(itemOne.ItemId);
                inventoryController.AcquireItem(itemTwo.ItemId);
                inventoryController.SelectSlot(1);

                presenter.BindInventory(inventoryController);

                Assert.That(hudSlots[0].ItemId, Is.EqualTo(itemOne.ItemId));
                Assert.That(hudSlots[1].ItemId, Is.EqualTo(itemTwo.ItemId));
                Assert.That(selectedIndicators[0].activeSelf, Is.False);
                Assert.That(selectedIndicators[1].activeSelf, Is.True);
                Assert.That(detailNameText.text, Is.EqualTo(itemTwo.DisplayName));
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(inventoryObject);
            }
        }

        [Test]
        public void HandleSlotClicked_SelectsInventorySlot()
        {
            InventoryController inventoryController = CreateInventoryController(
                out GameObject inventoryObject,
                out ItemDefinitionSO itemOne,
                out ItemDefinitionSO itemTwo);
            InventoryPresenter presenter = CreateInventoryPresenter(
                out GameObject presenterObject,
                out _,
                out _,
                out TMP_Text detailNameText,
                out _);

            try
            {
                inventoryController.AcquireItem(itemOne.ItemId);
                inventoryController.AcquireItem(itemTwo.ItemId);
                presenter.BindInventory(inventoryController);

                presenter.HandleSlotClicked(1);

                Assert.That(inventoryController.SelectedItemId, Is.EqualTo(itemTwo.ItemId));
                Assert.That(detailNameText.text, Is.EqualTo(itemTwo.DisplayName));
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(inventoryObject);
            }
        }

        [Test]
        public void HandleSlotDropped_ReordersInventorySlots()
        {
            InventoryController inventoryController = CreateInventoryController(
                out GameObject inventoryObject,
                out ItemDefinitionSO itemOne,
                out ItemDefinitionSO itemTwo);
            ItemDefinitionSO itemThree = LoadItemDefinition(ITEM_THREE_PATH);
            AddItemDefinitions(inventoryController, itemOne, itemTwo, itemThree);
            InventoryPresenter presenter = CreateInventoryPresenter(
                out GameObject presenterObject,
                out _,
                out _,
                out _,
                out _);

            try
            {
                inventoryController.AcquireItem(itemOne.ItemId);
                inventoryController.AcquireItem(itemTwo.ItemId);
                inventoryController.AcquireItem(itemThree.ItemId);
                presenter.BindInventory(inventoryController);

                presenter.HandleSlotDragStarted(0);
                presenter.HandleSlotDropped(2);
                presenter.HandleSlotDragEnded();

                Assert.That(inventoryController.OwnedItemIds[0], Is.EqualTo(itemTwo.ItemId));
                Assert.That(inventoryController.OwnedItemIds[1], Is.EqualTo(itemThree.ItemId));
                Assert.That(inventoryController.OwnedItemIds[2], Is.EqualTo(itemOne.ItemId));
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(inventoryObject);
            }
        }

        [Test]
        public void TogglePopup_ShowsAndHidesInventoryPopup()
        {
            InventoryPresenter presenter = CreateInventoryPresenter(
                out GameObject presenterObject,
                out _,
                out _,
                out _,
                out GameObject popupRoot);

            try
            {
                Assert.That(popupRoot.activeSelf, Is.False);

                presenter.TogglePopup();

                Assert.That(presenter.IsPopupOpen, Is.True);
                Assert.That(popupRoot.activeSelf, Is.True);

                presenter.TogglePopup();

                Assert.That(presenter.IsPopupOpen, Is.False);
                Assert.That(popupRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
            }
        }

        private static InventoryController CreateInventoryController(
            out GameObject inventoryObject,
            out ItemDefinitionSO itemOne,
            out ItemDefinitionSO itemTwo)
        {
            inventoryObject = new GameObject("InventoryController");
            inventoryObject.SetActive(false);
            InventoryController inventoryController = inventoryObject.AddComponent<InventoryController>();
            itemOne = LoadItemDefinition(ITEM_ONE_PATH);
            itemTwo = LoadItemDefinition(ITEM_TWO_PATH);
            AddItemDefinitions(inventoryController, itemOne, itemTwo);
            return inventoryController;
        }

        private static void AddItemDefinitions(
            InventoryController inventoryController,
            params ItemDefinitionSO[] itemDefinitions)
        {
            SerializedObject serializedObject = new(inventoryController);
            SerializedProperty property = serializedObject.FindProperty("_itemDefinitions");
            property.arraySize = itemDefinitions.Length;
            for (int i = 0; i < itemDefinitions.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = itemDefinitions[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemDefinitionSO LoadItemDefinition(string path)
        {
            ItemDefinitionSO itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(path);
            Assert.That(itemDefinition, Is.Not.Null);
            return itemDefinition;
        }

        private static InventoryPresenter CreateInventoryPresenter(
            out GameObject presenterObject,
            out InventorySlotPresenter[] hudSlots,
            out GameObject[] selectedIndicators,
            out TMP_Text detailNameText,
            out GameObject popupRoot)
        {
            presenterObject = new GameObject("InventoryPresenter");
            InventoryPresenter presenter = presenterObject.AddComponent<InventoryPresenter>();
            hudSlots = CreateSlots(presenterObject.transform, "HudSlot", 5, out selectedIndicators);
            InventorySlotPresenter[] popupSlots = CreateSlots(
                presenterObject.transform,
                "PopupSlot",
                5,
                out _);
            InventorySlotPresenter[] storageSlots = CreateSlots(
                presenterObject.transform,
                "StorageSlot",
                10,
                out _);
            popupRoot = new GameObject("PopupRoot");
            popupRoot.transform.SetParent(presenterObject.transform);
            detailNameText = CreateText(presenterObject.transform, "DetailName");
            TMP_Text detailDescriptionText = CreateText(presenterObject.transform, "DetailDescription");

            SerializedObject serializedObject = new(presenter);
            AssignArray(serializedObject, "_hudSlots", hudSlots);
            serializedObject.FindProperty("_popupRoot").objectReferenceValue = popupRoot;
            AssignArray(serializedObject, "_popupPrimarySlots", popupSlots);
            AssignArray(serializedObject, "_popupStorageSlots", storageSlots);
            serializedObject.FindProperty("_detailNameText").objectReferenceValue = detailNameText;
            serializedObject.FindProperty("_detailDescriptionText").objectReferenceValue = detailDescriptionText;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            InvokePrivate(presenter, "ConfigureSlots");
            popupRoot.SetActive(false);
            return presenter;
        }

        private static InventorySlotPresenter[] CreateSlots(
            Transform parent,
            string namePrefix,
            int count,
            out GameObject[] selectedIndicators)
        {
            InventorySlotPresenter[] slots = new InventorySlotPresenter[count];
            selectedIndicators = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                GameObject slotObject = new($"{namePrefix}{i + 1}");
                slotObject.transform.SetParent(parent);
                Image backgroundImage = slotObject.AddComponent<Image>();
                GameObject selectedIndicator = new("SelectedIndicator");
                selectedIndicator.transform.SetParent(slotObject.transform);
                selectedIndicator.AddComponent<Image>();
                Image iconImage = new GameObject("Icon").AddComponent<Image>();
                iconImage.transform.SetParent(slotObject.transform);
                TMP_Text keyText = CreateText(slotObject.transform, "KeyText");
                TMP_Text nameText = CreateText(slotObject.transform, "NameText");
                InventorySlotPresenter slotPresenter = slotObject.AddComponent<InventorySlotPresenter>();

                SerializedObject serializedObject = new(slotPresenter);
                serializedObject.FindProperty("_backgroundImage").objectReferenceValue = backgroundImage;
                serializedObject.FindProperty("_iconImage").objectReferenceValue = iconImage;
                serializedObject.FindProperty("_nameText").objectReferenceValue = nameText;
                serializedObject.FindProperty("_keyText").objectReferenceValue = keyText;
                serializedObject.FindProperty("_selectedIndicator").objectReferenceValue = selectedIndicator;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                selectedIndicator.SetActive(false);
                slots[i] = slotPresenter;
                selectedIndicators[i] = selectedIndicator;
            }

            return slots;
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent);
            return textObject.AddComponent<TextMeshProUGUI>();
        }

        private static void AssignArray(
            SerializedObject serializedObject,
            string propertyName,
            Object[] values)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void InvokePrivate(Object target, string methodName)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(methodName, INSTANCE_PRIVATE);
            methodInfo.Invoke(target, null);
        }
    }
}
