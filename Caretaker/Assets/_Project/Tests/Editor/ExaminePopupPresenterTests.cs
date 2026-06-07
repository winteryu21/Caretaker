using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using Caretaker.Gameplay;
using Caretaker.Presentation;
using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class ExaminePopupPresenterTests
    {
        [Test]
        public void Open_WithTextOnly_ShowsTextAndHidesImage()
        {
            ExaminePopupPresenter presenter = CreatePresenter(
                out GameObject root,
                out GameObject panel,
                out TMP_Text title,
                out TMP_Text body,
                out GameObject imageRoot,
                out Image image);

            presenter.Open("OBJ_P1_SIGN", "Information", null);

            Assert.That(presenter.IsOpen, Is.True);
            Assert.That(panel.activeSelf, Is.True);
            Assert.That(title.text, Is.EqualTo("OBJ_P1_SIGN"));
            Assert.That(body.text, Is.EqualTo("Information"));
            Assert.That(body.rectTransform.anchorMin.x, Is.EqualTo(0f));
            Assert.That(imageRoot.activeSelf, Is.False);
            Assert.That(image.sprite, Is.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Open_WithImage_ShowsImageAndOffsetsBody()
        {
            ExaminePopupPresenter presenter = CreatePresenter(
                out GameObject root,
                out _,
                out _,
                out TMP_Text body,
                out GameObject imageRoot,
                out Image image);
            Sprite sprite = CreateSprite();

            presenter.Open("OBJ_P1_PANEL", "Wiring diagram", sprite);

            Assert.That(imageRoot.activeSelf, Is.True);
            Assert.That(image.sprite, Is.SameAs(sprite));
            Assert.That(image.preserveAspect, Is.True);
            Assert.That(body.rectTransform.anchorMin.x, Is.EqualTo(0.45f));

            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Close_HidesPanelAndClearsImage()
        {
            ExaminePopupPresenter presenter = CreatePresenter(
                out GameObject root,
                out GameObject panel,
                out _,
                out _,
                out _,
                out Image image);
            Sprite sprite = CreateSprite();
            presenter.Open("OBJ", "Body", sprite);

            presenter.Close();

            Assert.That(presenter.IsOpen, Is.False);
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(image.sprite, Is.Null);

            Object.DestroyImmediate(sprite.texture);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RunInteraction_Examine_OpensPopup()
        {
            ExaminePopupPresenter presenter = CreatePresenter(
                out GameObject root,
                out _,
                out TMP_Text title,
                out TMP_Text body,
                out _,
                out _);
            GameObject targetObject = new("Interactable");
            InteractableObject target = targetObject.AddComponent<InteractableObject>();
            SerializedObject targetSerializedObject = new(target);
            targetSerializedObject.FindProperty("_objectId").stringValue = "OBJ_TEST";
            targetSerializedObject.FindProperty("_examineText").stringValue = "Test information";
            targetSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            bool didRun = target.RunInteraction(InteractionType.Examine, null);

            Assert.That(didRun, Is.True);
            Assert.That(presenter.IsOpen, Is.True);
            Assert.That(title.text, Is.EqualTo("OBJ_TEST"));
            Assert.That(body.text, Is.EqualTo("Test information"));

            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void PlayerController_SetInputBlocked_UpdatesModalState()
        {
            GameObject player = new("Player");
            player.AddComponent<PlayerInput>();
            PlayerController controller = player.AddComponent<PlayerController>();
            InteractionProbe probe = player.GetComponent<InteractionProbe>();

            controller.SetInputBlocked(true);
            Assert.That(controller.IsInputBlocked, Is.True);
            Assert.That(probe.enabled, Is.False);

            controller.SetInputBlocked(false);
            Assert.That(controller.IsInputBlocked, Is.False);
            Assert.That(probe.enabled, Is.True);

            Object.DestroyImmediate(player);
        }

        private static ExaminePopupPresenter CreatePresenter(
            out GameObject root,
            out GameObject panel,
            out TMP_Text title,
            out TMP_Text body,
            out GameObject imageRoot,
            out Image image)
        {
            root = new GameObject("Root");
            root.SetActive(false);
            panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform);
            title = new GameObject("Title").AddComponent<TextMeshProUGUI>();
            title.transform.SetParent(panel.transform);
            body = new GameObject("Body").AddComponent<TextMeshProUGUI>();
            body.transform.SetParent(panel.transform);
            imageRoot = new GameObject("ImageRoot");
            imageRoot.transform.SetParent(panel.transform);
            image = new GameObject("Image").AddComponent<Image>();
            image.transform.SetParent(imageRoot.transform);
            Button closeButton = new GameObject("Close").AddComponent<Button>();
            closeButton.transform.SetParent(panel.transform);

            ExaminePopupPresenter presenter = root.AddComponent<ExaminePopupPresenter>();
            SerializedObject serializedObject = new(presenter);
            serializedObject.FindProperty("_panelRoot").objectReferenceValue = panel;
            serializedObject.FindProperty("_titleText").objectReferenceValue = title;
            serializedObject.FindProperty("_bodyText").objectReferenceValue = body;
            serializedObject.FindProperty("_imageRoot").objectReferenceValue = imageRoot;
            serializedObject.FindProperty("_image").objectReferenceValue = image;
            serializedObject.FindProperty("_closeButton").objectReferenceValue = closeButton;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            return presenter;
        }

        private static Sprite CreateSprite()
        {
            Texture2D texture = new(2, 2);
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
        }
    }
}
