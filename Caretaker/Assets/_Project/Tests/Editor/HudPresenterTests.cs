using NUnit.Framework;

using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Caretaker.Gameplay;
using Caretaker.Presentation;
using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Tests.Editor
{
    public class HudPresenterTests
    {
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string HUD_SHELL_PREFAB_PATH = "Assets/_Project/Prefabs/UI/HudShell.prefab";
        private const string PERSISTENT_SCENE_PATH = "Assets/_Project/Scenes/Persistent.unity";

        [Test]
        public void HudShellPrefab_HasRequiredPresenterAreas()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_SHELL_PREFAB_PATH);

            Assert.That(prefab, Is.Not.Null);

            HudPresenter presenter = prefab.GetComponent<HudPresenter>();

            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.ObjectiveArea, Is.Not.Null);
            Assert.That(presenter.InventoryArea, Is.Not.Null);
            Assert.That(presenter.InteractionPromptArea, Is.Not.Null);
            Assert.That(presenter.ModalOverlayArea, Is.Not.Null);
            Assert.That(presenter.StatusArea, Is.Not.Null);
            Assert.That(prefab.GetComponent<HudRuntimeBinder>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<ExaminePopupPresenter>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<InventoryPresenter>(true), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<InteractionPromptPresenter>(true), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<CausalityIndicatorPresenter>(true), Is.Not.Null);
        }

        [Test]
        public void HudShellPrefab_InventoryStorageSlotsStayInsidePopupBottomLeft()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_SHELL_PREFAB_PATH);

            Assert.That(prefab, Is.Not.Null);

            Transform storageSlots = prefab.transform.Find("InventoryPopupRoot/InventoryDialog/PopupStorageSlots");

            Assert.That(storageSlots, Is.Not.Null);

            RectTransform storageRect = storageSlots.GetComponent<RectTransform>();
            GridLayoutGroup storageLayout = storageSlots.GetComponent<GridLayoutGroup>();

            Assert.That(storageRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(storageRect.anchorMax, Is.EqualTo(Vector2.zero));
            Assert.That(storageRect.pivot, Is.EqualTo(Vector2.zero));
            Assert.That(storageRect.anchoredPosition, Is.EqualTo(new Vector2(28f, 28f)));
            Assert.That(storageRect.sizeDelta, Is.EqualTo(new Vector2(500f, 176f)));
            Assert.That(storageLayout.childAlignment, Is.EqualTo(TextAnchor.LowerLeft));
            Assert.That(storageLayout.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(storageLayout.constraintCount, Is.EqualTo(5));
        }

        [Test]
        public void HudRuntimeBinder_BuildsPromptAndObjectiveTexts()
        {
            Assert.That(
                HudRuntimeBinder.BuildPromptText(InteractionType.Acquire, string.Empty),
                Is.EqualTo("LMB - Pick Up"));
            Assert.That(
                HudRuntimeBinder.BuildPromptText(InteractionType.Operate, string.Empty),
                Is.EqualTo("E - Operate"));
            Assert.That(
                HudRuntimeBinder.BuildPromptText(InteractionType.UseItem, "Fuse"),
                Is.EqualTo("RMB - Use Fuse"));
            Assert.That(
                HudRuntimeBinder.BuildPromptText(InteractionType.Examine, string.Empty),
                Is.EqualTo("LMB - Examine"));
            Assert.That(
                HudRuntimeBinder.BuildPromptText(null, null, string.Empty),
                Is.Empty);
            Assert.That(
                HudRuntimeBinder.BuildObjectiveText(PhaseId.Phase1),
                Is.EqualTo("Objective: Restore facility power"));
        }

        [Test]
        public void ExaminePopupPresenter_OpensAndClosesModal()
        {
            ExaminePopupPresenter presenter = CreateExaminePopupPresenter(
                out GameObject root,
                out GameObject panel,
                out TMP_Text title,
                out TMP_Text body,
                out GameObject imageRoot,
                out Image image);

            try
            {
                presenter.Open("OBJ_SIGN", "Sign text", null);

                Assert.That(presenter.IsOpen, Is.True);
                Assert.That(panel.activeSelf, Is.True);
                Assert.That(title.text, Is.EqualTo("OBJ_SIGN"));
                Assert.That(body.text, Is.EqualTo("Sign text"));
                Assert.That(imageRoot.activeSelf, Is.False);

                presenter.Close();

                Assert.That(presenter.IsOpen, Is.False);
                Assert.That(panel.activeSelf, Is.False);
                Assert.That(image.sprite, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InteractableObject_ExamineOpensPopup()
        {
            ExaminePopupPresenter presenter = CreateExaminePopupPresenter(
                out GameObject root,
                out _,
                out TMP_Text title,
                out TMP_Text body,
                out _,
                out _);
            GameObject targetObject = new("Inspectable");
            InteractableObject target = targetObject.AddComponent<InteractableObject>();

            try
            {
                SerializedObject serializedObject = new(target);
                serializedObject.FindProperty("_objectId").stringValue = "OBJ_SIGN";
                serializedObject.FindProperty("_examineText").stringValue = "A sign.";
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                bool didRun = target.RunInteraction(InteractionType.Examine, null);

                Assert.That(didRun, Is.True);
                Assert.That(presenter.IsOpen, Is.True);
                Assert.That(title.text, Is.EqualTo("OBJ_SIGN"));
                Assert.That(body.text, Is.EqualTo("A sign."));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PersistentScene_HasHudShellUnderUiCanvas()
        {
            Scene scene = EditorSceneManager.OpenScene(PERSISTENT_SCENE_PATH, OpenSceneMode.Single);

            Assert.That(scene.IsValid(), Is.True);

            GameObject canvas = FindRootObject(scene, "UI Canvas");

            Assert.That(canvas, Is.Not.Null);

            Transform hudShell = canvas.transform.Find("HudShell");

            Assert.That(hudShell, Is.Not.Null);
            Assert.That(hudShell.GetComponent<HudPresenter>(), Is.Not.Null);
            Assert.That(hudShell.GetComponent<HudRuntimeBinder>(), Is.Not.Null);
        }

        [Test]
        public void InteractionPromptPresenter_ShowsAndHidesPrompt()
        {
            GameObject promptRoot = new("PromptRoot");
            TextMeshProUGUI promptText = CreateText(promptRoot.transform, "PromptText");
            InteractionPromptPresenter presenter = promptRoot.AddComponent<InteractionPromptPresenter>();

            try
            {
                presenter.ShowPrompt("E - Operate");

                Assert.That(presenter.IsVisible, Is.True);
                Assert.That(presenter.CurrentPrompt, Is.EqualTo("E - Operate"));
                Assert.That(promptText.text, Is.EqualTo("E - Operate"));
                Assert.That(promptRoot.activeSelf, Is.True);

                presenter.HidePrompt();

                Assert.That(presenter.IsVisible, Is.False);
                Assert.That(presenter.CurrentPrompt, Is.Empty);
                Assert.That(promptRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(promptRoot);
            }
        }

        [Test]
        public void HudPresenter_RendersObjectiveAndRadioState()
        {
            GameObject hudRoot = new("HudRoot");
            GameObject objectiveRoot = new("ObjectiveRoot");
            objectiveRoot.transform.SetParent(hudRoot.transform);
            TextMeshProUGUI objectiveText = CreateText(objectiveRoot.transform, "ObjectiveText");
            TextMeshProUGUI radioText = CreateText(hudRoot.transform, "RadioText");
            GameObject radioIndicator = new("RadioIndicator");
            radioIndicator.transform.SetParent(hudRoot.transform);
            TextMeshProUGUI controlModeText = CreateText(hudRoot.transform, "ControlModeText");
            GameObject normalModeIndicator = new("NormalModeIndicator");
            normalModeIndicator.transform.SetParent(hudRoot.transform);
            GameObject combatModeIndicator = new("CombatModeIndicator");
            combatModeIndicator.transform.SetParent(hudRoot.transform);
            HudPresenter presenter = hudRoot.AddComponent<HudPresenter>();

            try
            {
                SerializedObject serializedPresenter = new(presenter);
                serializedPresenter.FindProperty("_objectiveRoot").objectReferenceValue = objectiveRoot;
                serializedPresenter.FindProperty("_objectiveText").objectReferenceValue = objectiveText;
                serializedPresenter.FindProperty("_radioStatusText").objectReferenceValue = radioText;
                serializedPresenter.FindProperty("_radioActiveIndicator").objectReferenceValue = radioIndicator;
                serializedPresenter.FindProperty("_controlModeText").objectReferenceValue = controlModeText;
                serializedPresenter.FindProperty("_normalModeIndicator").objectReferenceValue = normalModeIndicator;
                serializedPresenter.FindProperty("_combatModeIndicator").objectReferenceValue = combatModeIndicator;
                serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

                presenter.SetObjective("Restore power");
                presenter.SetRadioState(RadioState.Transmitting, 1UL);
                presenter.SetControlMode(PlayerControlMode.Combat);

                Assert.That(presenter.Objective, Is.EqualTo("Restore power"));
                Assert.That(objectiveText.text, Is.EqualTo("Restore power"));
                Assert.That(objectiveRoot.activeSelf, Is.True);
                Assert.That(presenter.RadioStatus, Is.EqualTo("Radio Tx"));
                Assert.That(radioText.text, Is.EqualTo("Radio Tx"));
                Assert.That(radioIndicator.activeSelf, Is.True);
                Assert.That(presenter.ControlModeStatus, Is.EqualTo("Mode: Combat"));
                Assert.That(controlModeText.text, Is.EqualTo("Mode: Combat"));
                Assert.That(normalModeIndicator.activeSelf, Is.False);
                Assert.That(combatModeIndicator.activeSelf, Is.True);

                presenter.ClearObjective();

                Assert.That(presenter.Objective, Is.Empty);
                Assert.That(objectiveRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(hudRoot);
            }
        }

        [Test]
        public void CausalityIndicatorPresenter_ShowPulseMakesRootVisible()
        {
            GameObject pulseRoot = new("PulseRoot");
            CausalityIndicatorPresenter presenter = pulseRoot.AddComponent<CausalityIndicatorPresenter>();

            try
            {
                SerializedObject serializedPresenter = new(presenter);
                serializedPresenter.FindProperty("_visibleSeconds").floatValue = 0f;
                serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

                pulseRoot.SetActive(false);

                presenter.ShowCausalityPulse();

                Assert.That(presenter.IsVisible, Is.True);
                Assert.That(pulseRoot.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(pulseRoot);
            }
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent);
            return textObject.AddComponent<TextMeshProUGUI>();
        }

        private static ExaminePopupPresenter CreateExaminePopupPresenter(
            out GameObject root,
            out GameObject panel,
            out TMP_Text title,
            out TMP_Text body,
            out GameObject imageRoot,
            out Image image)
        {
            root = new("Root");
            panel = new("Panel");
            panel.transform.SetParent(root.transform);
            title = CreateText(panel.transform, "Title");
            body = CreateText(panel.transform, "Body");
            imageRoot = new("ImageRoot");
            imageRoot.transform.SetParent(panel.transform);
            image = new GameObject("Image").AddComponent<Image>();
            image.transform.SetParent(imageRoot.transform);
            Button closeButton = new GameObject("CloseButton").AddComponent<Button>();
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
            panel.SetActive(false);
            InvokeLifecycle(presenter, "OnDisable");
            InvokeLifecycle(presenter, "OnEnable");
            return presenter;
        }

        private static void InvokeLifecycle(ExaminePopupPresenter presenter, string methodName)
        {
            MethodInfo methodInfo = typeof(ExaminePopupPresenter).GetMethod(methodName, INSTANCE_PRIVATE);
            methodInfo.Invoke(presenter, null);
        }

        private static GameObject FindRootObject(Scene scene, string objectName)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i].name == objectName)
                {
                    return rootObjects[i];
                }
            }

            return null;
        }
    }
}
