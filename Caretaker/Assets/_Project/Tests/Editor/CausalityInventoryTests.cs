using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using Unity.Netcode;
using UnityEditor;
using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class CausalityInventoryTests
    {
        private const ulong PLAYER_ID = 42;
        private const string REQUIRED_ITEM_ID = "ITEM_TOOL_DRIVER";

        [Test]
        public void CausalityService_RejectsTriggerWhenRequiredItemIsMissing()
        {
            CausalityService service = new();
            service.Initialize(new[] { CreateRequiredItemRule() });

            CausalResult result = service.SubmitTrigger(
                "CR_TEST_REQUIRED_ITEM",
                TimelineRole.Past,
                System.Array.Empty<string>());

            Assert.That(result.Success, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo($"Missing required item: {REQUIRED_ITEM_ID}"));
        }

        [Test]
        public void CausalityService_AcceptsTriggerWhenRequiredItemIsOwned()
        {
            CausalityService service = new();
            service.Initialize(new[] { CreateRequiredItemRule() });

            CausalResult result = service.SubmitTrigger(
                "CR_TEST_REQUIRED_ITEM",
                TimelineRole.Past,
                new[] { REQUIRED_ITEM_ID });

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void CausalityManager_ReadsOwnedItemsFromMatchingInventoryController()
        {
            GameObject managerObject = new("CausalityManager");
            GameObject playerObject = new("PlayerInventory");

            try
            {
                CausalityManager manager = managerObject.AddComponent<CausalityManager>();
                playerObject.AddComponent<NetworkObject>();
                InventoryController inventoryController = playerObject.AddComponent<InventoryController>();
                inventoryController.SetPlayerId(PLAYER_ID);
                inventoryController.State.OwnedItemIds.Add(REQUIRED_ITEM_ID);

                IReadOnlyList<string> ownedItems = InvokeGetPlayerOwnedItems(manager, PLAYER_ID);

                Assert.That(ownedItems, Does.Contain(REQUIRED_ITEM_ID));
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(managerObject);
            }
        }

        private static CausalRuleSO CreateRequiredItemRule()
        {
            CausalRuleSO rule = ScriptableObject.CreateInstance<CausalRuleSO>();
            SerializedObject serializedRule = new(rule);

            serializedRule.FindProperty("_ruleId").stringValue = "CR_TEST_REQUIRED_ITEM";
            serializedRule.FindProperty("_triggerId").stringValue = "CR_TEST_REQUIRED_ITEM";
            serializedRule.FindProperty("_requiredRole").enumValueIndex = (int)TimelineRole.Past;
            serializedRule.FindProperty("_requiredItemId").stringValue = REQUIRED_ITEM_ID;
            serializedRule.ApplyModifiedPropertiesWithoutUndo();

            return rule;
        }

        private static IReadOnlyList<string> InvokeGetPlayerOwnedItems(
            CausalityManager manager,
            ulong playerId)
        {
            MethodInfo methodInfo = typeof(CausalityManager).GetMethod(
                "GetPlayerOwnedItems",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(methodInfo, Is.Not.Null);

            return (IReadOnlyList<string>)methodInfo.Invoke(manager, new object[] { playerId });
        }
    }
}
