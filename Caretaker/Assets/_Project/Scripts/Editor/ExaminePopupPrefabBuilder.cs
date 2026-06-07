#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Caretaker.Presentation;

namespace Caretaker.Editor
{
    public static class ExaminePopupPrefabBuilder
    {
        private const string PREFAB_FOLDER = "Assets/_Project/Prefabs/UI";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/ExaminePopup.prefab";
        private const string PERSISTENT_SCENE_PATH = "Assets/_Project/Scenes/Persistent.unity";

        [MenuItem("Caretaker/Build Examine Popup")]
        public static void Build()
        {
            EnsureFolder();
            GameObject prefab = CreatePrefab();
            AddToPersistentScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {PREFAB_PATH} and added it to {PERSISTENT_SCENE_PATH}.");
        }

        private static GameObject CreatePrefab()
        {
            GameObject root = CreateUiObject("ExaminePopup", null);
            Stretch(root.GetComponent<RectTransform>());

            ExaminePopupPresenter presenter = root.AddComponent<ExaminePopupPresenter>();

            GameObject panelRoot = CreateUiObject("PanelRoot", root.transform);
            Stretch(panelRoot.GetComponent<RectTransform>());
            Image dimmer = panelRoot.AddComponent<Image>();
            dimmer.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject dialog = CreateUiObject("Dialog", panelRoot.transform);
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(760f, 520f);
            Image dialogBackground = dialog.AddComponent<Image>();
            dialogBackground.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            TMP_Text title = CreateText(
                "Title",
                dialog.transform,
                new Vector2(32f, -24f),
                new Vector2(-104f, 72f),
                30f,
                FontStyles.Bold);
            title.text = "OBJECT_ID";

            GameObject closeObject = CreateUiObject("CloseButton", dialog.transform);
            RectTransform closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-24f, -20f);
            closeRect.sizeDelta = new Vector2(64f, 48f);
            Image closeBackground = closeObject.AddComponent<Image>();
            closeBackground.color = new Color(0.24f, 0.28f, 0.36f, 1f);
            Button closeButton = closeObject.AddComponent<Button>();
            closeButton.targetGraphic = closeBackground;

            TMP_Text closeLabel = CreateText(
                "Label",
                closeObject.transform,
                Vector2.zero,
                Vector2.zero,
                24f,
                FontStyles.Bold);
            Stretch(closeLabel.rectTransform);
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.text = "X";

            GameObject divider = CreateUiObject("Divider", dialog.transform);
            RectTransform dividerRect = divider.GetComponent<RectTransform>();
            dividerRect.anchorMin = new Vector2(0f, 1f);
            dividerRect.anchorMax = new Vector2(1f, 1f);
            dividerRect.pivot = new Vector2(0.5f, 1f);
            dividerRect.anchoredPosition = new Vector2(0f, -88f);
            dividerRect.sizeDelta = new Vector2(-48f, 2f);
            Image dividerImage = divider.AddComponent<Image>();
            dividerImage.color = new Color(0.35f, 0.65f, 0.8f, 0.8f);

            GameObject imageRoot = CreateUiObject("ImageRoot", dialog.transform);
            RectTransform imageRootRect = imageRoot.GetComponent<RectTransform>();
            imageRootRect.anchorMin = new Vector2(0f, 0f);
            imageRootRect.anchorMax = new Vector2(0.45f, 1f);
            imageRootRect.offsetMin = new Vector2(32f, 76f);
            imageRootRect.offsetMax = new Vector2(-12f, -112f);
            Image imageBackground = imageRoot.AddComponent<Image>();
            imageBackground.color = new Color(0.04f, 0.05f, 0.08f, 1f);

            GameObject imageObject = CreateUiObject("ExamineImage", imageRoot.transform);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            Stretch(imageRect);
            imageRect.offsetMin = new Vector2(12f, 12f);
            imageRect.offsetMax = new Vector2(-12f, -12f);
            Image examineImage = imageObject.AddComponent<Image>();
            examineImage.color = Color.white;
            examineImage.preserveAspect = true;

            TMP_Text body = CreateText(
                "Body",
                dialog.transform,
                new Vector2(32f, 76f),
                new Vector2(-32f, -112f),
                22f,
                FontStyles.Normal);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.enableWordWrapping = true;
            body.text = "Examine text";

            GameObject hintObject = CreateUiObject("CloseHint", dialog.transform);
            RectTransform hintRect = hintObject.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 18f);
            hintRect.sizeDelta = new Vector2(-48f, 36f);
            TMP_Text hint = hintObject.AddComponent<TextMeshProUGUI>();
            hint.font = TMP_Settings.defaultFontAsset;
            hint.fontSize = 16f;
            hint.color = new Color(0.7f, 0.75f, 0.82f, 1f);
            hint.alignment = TextAlignmentOptions.Center;
            hint.text = "ESC / E  Close";

            ConfigurePresenter(
                presenter,
                panelRoot,
                title,
                body,
                imageRoot,
                examineImage,
                closeButton);

            panelRoot.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddToPersistentScene(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(PERSISTENT_SCENE_PATH, OpenSceneMode.Single);
            GameObject existing = GameObject.Find("UI Canvas/ExaminePopup");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject canvas = GameObject.Find("UI Canvas");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(canvas.transform, false);
            instance.transform.SetAsLastSibling();
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePresenter(
            ExaminePopupPresenter presenter,
            GameObject panelRoot,
            TMP_Text title,
            TMP_Text body,
            GameObject imageRoot,
            Image image,
            Button closeButton)
        {
            SerializedObject serializedObject = new(presenter);
            serializedObject.FindProperty("_panelRoot").objectReferenceValue = panelRoot;
            serializedObject.FindProperty("_titleText").objectReferenceValue = title;
            serializedObject.FindProperty("_bodyText").objectReferenceValue = body;
            serializedObject.FindProperty("_imageRoot").objectReferenceValue = imageRoot;
            serializedObject.FindProperty("_image").objectReferenceValue = image;
            serializedObject.FindProperty("_closeButton").objectReferenceValue = closeButton;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float fontSize,
            FontStyles fontStyle)
        {
            GameObject textObject = CreateUiObject(name, parent);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
            }
        }
    }
}
#endif
