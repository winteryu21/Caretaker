using UnityEditor;
using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.World.Editor
{
    [CustomEditor(typeof(InteractableObject))]
    public sealed class InteractableObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemId;
        private SerializedProperty _interactionTypes;
        private SerializedProperty _grantedItemId;
        private SerializedProperty _requiredItemId;
        private SerializedProperty _isDestroy;
        private SerializedProperty _examineText;
        private SerializedProperty _examineImage;
        private SerializedProperty _outlineBehaviours;
        private SerializedProperty _useFallbackGlow;
        private SerializedProperty _fallbackGlowColor;
        private SerializedProperty _fallbackGlowScale;
        private SerializedProperty _fallbackGlowSortingOrderOffset;
        private SerializedProperty _highlightOnAwake;

        private void OnEnable()
        {
            _itemId = serializedObject.FindProperty("_itemId");
            _interactionTypes = serializedObject.FindProperty("_interactionTypes");
            _grantedItemId = serializedObject.FindProperty("_grantedItemId");
            _requiredItemId = serializedObject.FindProperty("_requiredItemId");
            _isDestroy = serializedObject.FindProperty("_isDestroy");
            _examineText = serializedObject.FindProperty("_examineText");
            _examineImage = serializedObject.FindProperty("_examineImage");
            _outlineBehaviours = serializedObject.FindProperty("_outlineBehaviours");
            _useFallbackGlow = serializedObject.FindProperty("_useFallbackGlow");
            _fallbackGlowColor = serializedObject.FindProperty("_fallbackGlowColor");
            _fallbackGlowScale = serializedObject.FindProperty("_fallbackGlowScale");
            _fallbackGlowSortingOrderOffset = serializedObject.FindProperty("_fallbackGlowSortingOrderOffset");
            _highlightOnAwake = serializedObject.FindProperty("_highlightOnAwake");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Object Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_itemId, new GUIContent("Item ID"));
            EditorGUILayout.PropertyField(_interactionTypes);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_grantedItemId, new GUIContent("Grant Item ID"));
            EditorGUILayout.PropertyField(_requiredItemId, new GUIContent("Required Item ID"));

            InteractionType interactionTypes = (InteractionType)_interactionTypes.intValue;
            if ((interactionTypes & InteractionType.Acquire) != 0)
            {
                EditorGUILayout.PropertyField(_isDestroy);
            }

            EditorGUILayout.PropertyField(_examineText);
            EditorGUILayout.PropertyField(_examineImage);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Highlight", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_outlineBehaviours);
            EditorGUILayout.PropertyField(_useFallbackGlow);
            EditorGUILayout.PropertyField(_fallbackGlowColor);
            EditorGUILayout.PropertyField(_fallbackGlowScale);
            EditorGUILayout.PropertyField(_fallbackGlowSortingOrderOffset);
            EditorGUILayout.PropertyField(_highlightOnAwake);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
