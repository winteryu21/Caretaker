using NUnit.Framework;

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Shared;
using Caretaker.World;

using Object = UnityEngine.Object;

namespace Caretaker.Tests.Editor
{
    public class InteractionServiceTests
    {
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void PrimaryClick_AcquiresNearbyAcquireTarget()
        {
            InteractionService service = new();
            PlayerController actor = CreateInactiveActor(Vector3.zero);
            InteractableObject target = CreateInteractable(
                "Item",
                Vector3.zero,
                InteractionType.Acquire,
                requiredItemId: string.Empty);

            try
            {
                bool processed = service.TryProcessInteraction(
                    new InteractionRequest(InteractionType.Examine, Vector2.zero),
                    target,
                    null,
                    InteractionType.None,
                    actor,
                    1f,
                    out InteractableObject resolvedTarget,
                    out InteractionType resolvedType);

                Assert.That(processed, Is.True);
                Assert.That(resolvedTarget, Is.SameAs(target));
                Assert.That(resolvedType, Is.EqualTo(InteractionType.Acquire));
            }
            finally
            {
                Object.DestroyImmediate(actor.gameObject);
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public void PrimaryClick_ExaminesNearbyExamineTarget()
        {
            InteractionService service = new();
            PlayerController actor = CreateInactiveActor(Vector3.zero);
            InteractableObject target = CreateInteractable(
                "Sign",
                Vector3.zero,
                InteractionType.Examine,
                requiredItemId: string.Empty);

            try
            {
                bool processed = service.TryProcessInteraction(
                    new InteractionRequest(InteractionType.Examine, Vector2.zero),
                    target,
                    null,
                    InteractionType.None,
                    actor,
                    1f,
                    out InteractableObject resolvedTarget,
                    out InteractionType resolvedType);

                Assert.That(processed, Is.True);
                Assert.That(resolvedTarget, Is.SameAs(target));
                Assert.That(resolvedType, Is.EqualTo(InteractionType.Examine));
            }
            finally
            {
                Object.DestroyImmediate(actor.gameObject);
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public void InteractKey_OnlyProcessesOperateTargets()
        {
            InteractionService service = new();
            PlayerController actor = CreateInactiveActor(Vector3.zero);
            InteractableObject target = CreateInteractable(
                "Item",
                Vector3.zero,
                InteractionType.Acquire,
                requiredItemId: string.Empty);

            try
            {
                bool processed = service.TryProcessInteraction(
                    new InteractionRequest(InteractionType.Operate, Vector2.zero),
                    null,
                    target,
                    InteractionType.Acquire,
                    actor,
                    1f,
                    out InteractableObject resolvedTarget,
                    out InteractionType resolvedType);

                Assert.That(processed, Is.False);
                Assert.That(resolvedTarget, Is.Null);
                Assert.That(resolvedType, Is.EqualTo(default(InteractionType)));
            }
            finally
            {
                Object.DestroyImmediate(actor.gameObject);
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public void InteractKey_RejectsLockedOperateTargetUntilItemSatisfied()
        {
            InteractionService service = new();
            PlayerController actor = CreateInactiveActor(Vector3.zero);
            InteractableObject target = CreateInteractable(
                "LockedDoor",
                Vector3.zero,
                InteractionType.Operate,
                requiredItemId: "ITEM_KEY_CARD");

            try
            {
                bool processed = service.TryProcessInteraction(
                    new InteractionRequest(InteractionType.Operate, Vector2.zero),
                    null,
                    target,
                    InteractionType.UseItem,
                    actor,
                    1f,
                    out InteractableObject resolvedTarget,
                    out InteractionType resolvedType);

                Assert.That(processed, Is.False);
                Assert.That(resolvedTarget, Is.Null);
                Assert.That(resolvedType, Is.EqualTo(default(InteractionType)));
            }
            finally
            {
                Object.DestroyImmediate(actor.gameObject);
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public void SetHighlight_CreatesVisibleFallbackGlowForSpriteRenderer()
        {
            Texture2D texture = new(2, 2);
            texture.SetPixel(0, 0, Color.white);
            texture.SetPixel(0, 1, Color.white);
            texture.SetPixel(1, 0, Color.white);
            texture.SetPixel(1, 1, Color.white);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f),
                2f);
            GameObject targetObject = new("GlowTarget");
            SpriteRenderer sourceRenderer = targetObject.AddComponent<SpriteRenderer>();
            sourceRenderer.sprite = sprite;
            InteractableObject target = targetObject.AddComponent<InteractableObject>();

            try
            {
                target.SetHighlight(true);

                SpriteRenderer glowRenderer = FindFallbackGlowRenderer(targetObject, sourceRenderer);

                Assert.That(glowRenderer, Is.Not.Null);
                Assert.That(glowRenderer.enabled, Is.True);
                Assert.That(glowRenderer.gameObject.activeSelf, Is.True);
                Assert.That(glowRenderer.sprite, Is.SameAs(sprite));
                Assert.That(glowRenderer.sortingOrder, Is.GreaterThan(sourceRenderer.sortingOrder));

                target.SetHighlight(false);

                Assert.That(glowRenderer.enabled, Is.False);
                Assert.That(glowRenderer.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void RoomTransitionOperateAction_AddsOperateTypeAndMovesActor()
        {
            InteractionService service = new();
            PlayerController actor = CreateInactiveActor(Vector3.zero);
            GameObject targetObject = new("TransitionTarget");
            GameObject destinationObject = new("Destination");
            destinationObject.transform.position = new Vector3(5f, 2f, 0f);
            InteractableObject target = targetObject.AddComponent<InteractableObject>();
            RoomTransitionOperateAction action = targetObject.AddComponent<RoomTransitionOperateAction>();

            try
            {
                SerializedObject serializedAction = new(action);
                serializedAction.FindProperty("_destination").objectReferenceValue = destinationObject.transform;
                serializedAction.ApplyModifiedPropertiesWithoutUndo();
                InvokePrivate(action, "Awake");

                bool processed = service.TryProcessInteraction(
                    new InteractionRequest(InteractionType.Operate, Vector2.zero),
                    null,
                    target,
                    InteractionType.Operate,
                    actor,
                    1f,
                    out InteractableObject resolvedTarget,
                    out InteractionType resolvedType);

                Assert.That(processed, Is.True);
                Assert.That(resolvedTarget, Is.SameAs(target));
                Assert.That(resolvedType, Is.EqualTo(InteractionType.Operate));
                Assert.That(actor.GetComponent<Rigidbody2D>().position, Is.EqualTo((Vector2)destinationObject.transform.position));
            }
            finally
            {
                Object.DestroyImmediate(actor.gameObject);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(destinationObject);
            }
        }

        private static PlayerController CreateInactiveActor(Vector3 position)
        {
            GameObject actorObject = new("Player");
            actorObject.SetActive(false);
            actorObject.transform.position = position;
            return actorObject.AddComponent<PlayerController>();
        }

        private static InteractableObject CreateInteractable(
            string objectId,
            Vector3 position,
            InteractionType interactionTypes,
            string requiredItemId)
        {
            GameObject targetObject = new(objectId);
            targetObject.transform.position = position;
            InteractableObject target = targetObject.AddComponent<InteractableObject>();

            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty("_objectId").stringValue = objectId;
            serializedObject.FindProperty("_interactionTypes").intValue = (int)interactionTypes;
            serializedObject.FindProperty("_requiredItemId").stringValue = requiredItemId;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return target;
        }

        private static SpriteRenderer FindFallbackGlowRenderer(
            GameObject rootObject,
            SpriteRenderer sourceRenderer)
        {
            SpriteRenderer[] spriteRenderers = rootObject.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer != sourceRenderer &&
                    spriteRenderer.gameObject.name.StartsWith("InteractionGlow", StringComparison.Ordinal))
                {
                    return spriteRenderer;
                }
            }

            return null;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(methodName, INSTANCE_PRIVATE);
            methodInfo.Invoke(target, null);
        }
    }
}
