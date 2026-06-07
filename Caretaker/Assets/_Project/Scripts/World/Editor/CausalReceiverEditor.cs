using UnityEditor;
using UnityEngine;

namespace Caretaker.World.Editor
{
    /// <summary>
    /// CausalReceiver의 Inspector를 커스터마이즈한다.
    /// CausalRuleSO에 다수의 ReceiverEffect가 있을 때 드롭다운으로 선택하게 한다.
    /// </summary>
    [CustomEditor(typeof(CausalReceiver))]
    public class CausalReceiverEditor : UnityEditor.Editor
    {
        private SerializedProperty _causalRuleProp;
        private SerializedProperty _receiverEffectIndexProp;
        private SerializedProperty _receiverIdProp;

        private void OnEnable()
        {
            _causalRuleProp = serializedObject.FindProperty("_causalRule");
            _receiverEffectIndexProp = serializedObject.FindProperty("_receiverEffectIndex");
            _receiverIdProp = serializedObject.FindProperty("_receiverId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Causal Rule 필드
            EditorGUILayout.PropertyField(_causalRuleProp);

            CausalRuleSO rule = _causalRuleProp.objectReferenceValue as CausalRuleSO;

            if (rule != null && rule.ReceiverEffects != null && rule.ReceiverEffects.Length > 0)
            {
                CausalRuleSO.CausalReceiverEffect[] effects = rule.ReceiverEffects;

                if (effects.Length == 1)
                {
                    // 단일 효과 — 자동 선택, 읽기 전용 표시
                    _receiverEffectIndexProp.intValue = 0;
                    _receiverIdProp.stringValue = effects[0].ReceiverId;

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("Receiver ID", effects[0].ReceiverId);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    // 다수 효과 — 드롭다운 표시
                    string[] options = new string[effects.Length];
                    for (int i = 0; i < effects.Length; i++)
                    {
                        options[i] = $"[{i}] {effects[i].ReceiverId} ({effects[i].StateKey}={effects[i].StateValue})";
                    }

                    int currentIndex = Mathf.Clamp(
                        _receiverEffectIndexProp.intValue, 0, effects.Length - 1);

                    int newIndex = EditorGUILayout.Popup(
                        "Receiver Effect", currentIndex, options);

                    _receiverEffectIndexProp.intValue = newIndex;
                    _receiverIdProp.stringValue = effects[newIndex].ReceiverId;

                    // 선택된 효과 정보 읽기 전용 표시
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("Receiver ID", effects[newIndex].ReceiverId);
                    EditorGUILayout.TextField("State", $"{effects[newIndex].StateKey} = {effects[newIndex].StateValue}");
                    EditorGUI.EndDisabledGroup();
                }
            }
            else if (rule == null)
            {
                // SO 미지정 시 수동 입력 허용
                EditorGUILayout.PropertyField(_receiverIdProp);
            }

            EditorGUILayout.Space();

            // 나머지 프로퍼티 (UnityEvent, Debug 등)
            DrawPropertiesExcluding(
                serializedObject,
                "_causalRule",
                "_receiverEffectIndex",
                "_receiverId",
                "m_Script");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
