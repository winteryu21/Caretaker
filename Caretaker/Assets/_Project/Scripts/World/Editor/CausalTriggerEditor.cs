using UnityEditor;
using UnityEngine;

namespace Caretaker.World.Editor
{
    /// <summary>
    /// CausalTrigger의 Inspector를 커스터마이즈한다.
    /// CausalRuleSO를 드래그하면 triggerId를 읽기 전용으로 표시한다.
    /// </summary>
    [CustomEditor(typeof(CausalTrigger))]
    public class CausalTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty _causalRuleProp;
        private SerializedProperty _triggerIdProp;

        private void OnEnable()
        {
            _causalRuleProp = serializedObject.FindProperty("_causalRule");
            _triggerIdProp = serializedObject.FindProperty("_triggerId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Causal Rule 필드
            EditorGUILayout.PropertyField(_causalRuleProp);

            CausalRuleSO rule = _causalRuleProp.objectReferenceValue as CausalRuleSO;

            if (rule != null)
            {
                _triggerIdProp.stringValue = rule.TriggerId;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Trigger ID", rule.TriggerId);
                EditorGUILayout.EnumPopup("Required Role", rule.RequiredRole);

                if (!string.IsNullOrWhiteSpace(rule.RequiredItemId))
                {
                    EditorGUILayout.TextField("Required Item", rule.RequiredItemId);
                }

                EditorGUILayout.LabelField("Weight", rule.IsMajor ? "★ Major" : "Minor");
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                // SO 미지정 시 수동 입력 허용
                EditorGUILayout.PropertyField(_triggerIdProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
