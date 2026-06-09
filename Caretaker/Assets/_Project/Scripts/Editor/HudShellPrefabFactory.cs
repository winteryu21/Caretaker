using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Caretaker.Presentation;

namespace Caretaker.Editor
{
    /// <summary>
    /// Persistent Canvas 아래에 배치할 HUD Shell prefab을 생성한다.
    /// </summary>
    public static class HudShellPrefabFactory
    {
        private const string PREFAB_FOLDER = "Assets/_Project/Prefabs/UI";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/HudShell.prefab";
        private const string PERSISTENT_SCENE_PATH = "Assets/_Project/Scenes/Persistent.unity";
        private const string UI_CANVAS_NAME = "UI Canvas";
        private const string HUD_SHELL_NAME = "HudShell";

        /// <summary>
        /// HUD Shell prefab을 재생성한다.
        /// </summary>
        [MenuItem("Caretaker/UI/Rebuild HUD Shell Prefab")]
        public static void RebuildPrefab()
        {
            EnsurePrefabFolder();

            GameObject root = CreateArea(
                "HudShell",
                null,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            HudPresenter hudPresenter = root.AddComponent<HudPresenter>();
            HudRuntimeBinder runtimeBinder = root.AddComponent<HudRuntimeBinder>();
            ExaminePopupPresenter examinePopupPresenter = root.AddComponent<ExaminePopupPresenter>();

            GameObject objectiveArea = CreateArea(
                "ObjectiveArea",
                root.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, -16f),
                new Vector2(0f, 72f));
            GameObject inventoryArea = CreateArea(
                "InventoryArea",
                root.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 24f),
                new Vector2(520f, 96f));
            GameObject promptArea = CreateArea(
                "InteractionPromptArea",
                root.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 128f),
                new Vector2(420f, 48f));
            GameObject modalArea = CreateArea(
                "ModalOverlayArea",
                root.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            Image modalBackdrop = modalArea.AddComponent<Image>();
            modalBackdrop.color = new Color(0f, 0f, 0f, 0.45f);

            GameObject statusArea = CreateArea(
                "StatusArea",
                root.transform,
                Vector2.one,
                Vector2.one,
                Vector2.one,
                new Vector2(-16f, -16f),
                new Vector2(260f, 96f));
            GameObject causalityArea = CreateArea(
                "CausalityArea",
                root.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -96f),
                new Vector2(320f, 48f));

            GameObject objectiveRoot = CreateArea(
                "ObjectiveRoot",
                objectiveArea.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            TextMeshProUGUI objectiveText = CreateText("ObjectiveText", objectiveRoot.transform, string.Empty);

            GameObject promptRoot = CreateArea(
                "PromptRoot",
                promptArea.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            TextMeshProUGUI promptText = CreateText("PromptText", promptRoot.transform, string.Empty);
            InteractionPromptPresenter promptPresenter = promptRoot.AddComponent<InteractionPromptPresenter>();
            Assign(promptPresenter, "_root", promptRoot);
            Assign(promptPresenter, "_promptText", promptText);

            GameObject examineDialog = CreateArea(
                "ExamineDialog",
                modalArea.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 520f));
            Image examineDialogImage = examineDialog.AddComponent<Image>();
            examineDialogImage.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            GameObject closeButtonObject = CreateArea(
                "CloseButton",
                examineDialog.transform,
                Vector2.one,
                Vector2.one,
                Vector2.one,
                new Vector2(-24f, -20f),
                new Vector2(64f, 48f));
            Image closeButtonImage = closeButtonObject.AddComponent<Image>();
            closeButtonImage.color = new Color(0.24f, 0.28f, 0.36f, 1f);
            Button closeButton = closeButtonObject.AddComponent<Button>();
            closeButton.targetGraphic = closeButtonImage;
            TextMeshProUGUI closeButtonText = CreateText("CloseLabel", closeButtonObject.transform, "X");
            closeButtonText.fontSize = 24f;

            TextMeshProUGUI examineTitleText = CreateText("ExamineTitle", examineDialog.transform, "OBJECT_ID");
            RectTransform examineTitleRect = examineTitleText.rectTransform;
            examineTitleRect.anchorMin = new Vector2(0f, 1f);
            examineTitleRect.anchorMax = new Vector2(1f, 1f);
            examineTitleRect.pivot = new Vector2(0.5f, 1f);
            examineTitleRect.anchoredPosition = new Vector2(-36f, -24f);
            examineTitleRect.sizeDelta = new Vector2(-136f, 56f);
            examineTitleText.alignment = TextAlignmentOptions.Left;
            examineTitleText.fontSize = 30f;
            examineTitleText.fontStyle = FontStyles.Bold;

            GameObject examineImageRoot = CreateArea(
                "ExamineImageRoot",
                examineDialog.transform,
                new Vector2(0f, 0f),
                new Vector2(0.45f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(10f, -18f),
                new Vector2(-44f, -188f));
            Image examineImageBackground = examineImageRoot.AddComponent<Image>();
            examineImageBackground.color = new Color(0.04f, 0.05f, 0.08f, 1f);
            GameObject examineImageObject = CreateArea(
                "ExamineImage",
                examineImageRoot.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            Image examineImage = examineImageObject.AddComponent<Image>();
            examineImage.color = Color.white;
            examineImage.preserveAspect = true;

            TextMeshProUGUI examineBodyText = CreateText("ExamineBody", examineDialog.transform, "Examine text");
            RectTransform examineBodyRect = examineBodyText.rectTransform;
            examineBodyRect.anchorMin = new Vector2(0f, 0f);
            examineBodyRect.anchorMax = new Vector2(1f, 1f);
            examineBodyRect.anchoredPosition = new Vector2(0f, -18f);
            examineBodyRect.sizeDelta = new Vector2(-64f, -188f);
            examineBodyText.alignment = TextAlignmentOptions.TopLeft;
            examineBodyText.fontSize = 22f;
            examineBodyText.textWrappingMode = TextWrappingModes.Normal;

            GameObject controlModeRoot = CreateArea(
                "ControlModeRoot",
                statusArea.transform,
                new Vector2(0f, 0.5f),
                Vector2.one,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            TextMeshProUGUI controlModeText =
                CreateText("ControlModeText", controlModeRoot.transform, "Mode: Investigation");
            controlModeText.alignment = TextAlignmentOptions.Right;
            GameObject normalModeIndicator = CreateArea(
                "NormalModeIndicator",
                controlModeRoot.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(8f, 0f),
                new Vector2(12f, 12f));
            Image normalModeImage = normalModeIndicator.AddComponent<Image>();
            normalModeImage.color = new Color(0.35f, 0.85f, 1f, 1f);
            GameObject combatModeIndicator = CreateArea(
                "CombatModeIndicator",
                controlModeRoot.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(8f, 0f),
                new Vector2(12f, 12f));
            Image combatModeImage = combatModeIndicator.AddComponent<Image>();
            combatModeImage.color = new Color(1f, 0.3f, 0.2f, 1f);

            GameObject radioRoot = CreateArea(
                "RadioRoot",
                statusArea.transform,
                Vector2.zero,
                new Vector2(1f, 0.5f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            TextMeshProUGUI radioText = CreateText("RadioStatusText", radioRoot.transform, "Radio Waiting");
            radioText.alignment = TextAlignmentOptions.Right;
            GameObject radioIndicator = CreateArea(
                "RadioActiveIndicator",
                radioRoot.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-8f, 0f),
                new Vector2(12f, 12f));

            GameObject causalityRoot = CreateArea(
                "CausalityPulseRoot",
                causalityArea.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            CreateText("CausalityPulseText", causalityRoot.transform, "Causality Shift");
            CausalityIndicatorPresenter causalityPresenter =
                causalityRoot.AddComponent<CausalityIndicatorPresenter>();
            Assign(causalityPresenter, "_root", causalityRoot);

            promptRoot.SetActive(false);
            modalArea.SetActive(false);
            objectiveRoot.SetActive(false);
            radioIndicator.SetActive(false);
            combatModeIndicator.SetActive(false);
            causalityRoot.SetActive(false);

            Assign(hudPresenter, "_hudRoot", root);
            Assign(hudPresenter, "_objectiveArea", objectiveArea.transform);
            Assign(hudPresenter, "_inventoryArea", inventoryArea.transform);
            Assign(hudPresenter, "_interactionPromptArea", promptArea.transform);
            Assign(hudPresenter, "_modalOverlayArea", modalArea.transform);
            Assign(hudPresenter, "_statusArea", statusArea.transform);
            Assign(hudPresenter, "_objectiveRoot", objectiveRoot);
            Assign(hudPresenter, "_objectiveText", objectiveText);
            Assign(hudPresenter, "_interactionPromptPresenter", promptPresenter);
            Assign(hudPresenter, "_causalityIndicatorPresenter", causalityPresenter);
            Assign(hudPresenter, "_radioStatusText", radioText);
            Assign(hudPresenter, "_radioActiveIndicator", radioIndicator);
            Assign(hudPresenter, "_controlModeText", controlModeText);
            Assign(hudPresenter, "_normalModeIndicator", normalModeIndicator);
            Assign(hudPresenter, "_combatModeIndicator", combatModeIndicator);
            Assign(runtimeBinder, "_hudPresenter", hudPresenter);
            Assign(examinePopupPresenter, "_panelRoot", modalArea);
            Assign(examinePopupPresenter, "_titleText", examineTitleText);
            Assign(examinePopupPresenter, "_bodyText", examineBodyText);
            Assign(examinePopupPresenter, "_imageRoot", examineImageRoot);
            Assign(examinePopupPresenter, "_image", examineImage);
            Assign(examinePopupPresenter, "_closeButton", closeButton);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Persistent 씬의 UI Canvas 아래에 HUD Shell prefab instance를 배치한다.
        /// </summary>
        [MenuItem("Caretaker/UI/Install HUD Shell In Persistent Scene")]
        public static void InstallInPersistentScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                RebuildPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            }

            if (prefab == null)
            {
                Debug.LogError($"HUD Shell prefab was not found: {PREFAB_PATH}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(PERSISTENT_SCENE_PATH, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Persistent scene was not found: {PERSISTENT_SCENE_PATH}");
                return;
            }

            GameObject canvas = FindRootObject(scene, UI_CANVAS_NAME);
            if (canvas == null)
            {
                Debug.LogError($"Persistent scene requires root object: {UI_CANVAS_NAME}");
                return;
            }

            Transform existing = canvas.transform.Find(HUD_SHELL_NAME);
            GameObject instance = existing != null
                ? existing.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);

            instance.name = HUD_SHELL_NAME;
            StretchToParent(instance);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
            }
        }

        private static GameObject CreateArea(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject area = new(name, typeof(RectTransform));
            RectTransform rectTransform = area.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            return area;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text)
        {
            GameObject textObject = CreateArea(
                name,
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
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

        private static void StretchToParent(GameObject instance)
        {
            if (!instance.TryGetComponent(out RectTransform rectTransform))
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private static void Assign(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
