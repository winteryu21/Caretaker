using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 모든 상호작용 가능 오브젝트의 공통 설정과 하이라이트 토글을 제공하는 베이스 컴포넌트입니다.
    /// Team B는 씬 오브젝트에 이 컴포넌트를 붙이고 Inspector에서 유형과 ID를 설정할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractableObject : MonoBehaviour
    {
        [Header("Object Identity")]
        [SerializeField] private string _objectId;
        [SerializeField] private InteractionType _interactionType = InteractionType.Examine;

        [Header("Interaction")]
        [SerializeField] private string _requiredItemId;
        [SerializeField] private string _grantedItemId;
        [SerializeField] [TextArea] private string _examineText;

        [Header("Highlight")]
        [SerializeField] private Behaviour[] _outlineBehaviours;
        [SerializeField] private bool _highlightOnAwake;

        private bool _isHighlighted;

        /// <summary>
        /// game-design 문서의 오브젝트 식별자입니다. 예: OBJ_P1_SIGN
        /// </summary>
        public string ObjectId => _objectId;

        /// <summary>
        /// 이 오브젝트의 기본 상호작용 유형입니다.
        /// </summary>
        public InteractionType InteractionType => _interactionType;

        /// <summary>
        /// 상호작용에 필요한 아이템 ID입니다. 비어 있으면 아이템이 필요하지 않습니다.
        /// </summary>
        public string RequiredItemId => _requiredItemId;

        /// <summary>
        /// 획득 성공 시 인벤토리에 추가할 아이템 ID입니다.
        /// </summary>
        public string GrantedItemId => _grantedItemId;

        /// <summary>
        /// 조사 시 표시할 텍스트입니다.
        /// </summary>
        public string ExamineText => _examineText;

        /// <summary>
        /// 현재 하이라이트가 켜져 있는지 반환합니다.
        /// </summary>
        public bool IsHighlighted => _isHighlighted;

        private void Awake()
        {
            SetHighlight(_highlightOnAwake);
        }

        private void OnValidate()
        {
            _objectId = _objectId?.Trim();
            _requiredItemId = _requiredItemId?.Trim();
            _grantedItemId = _grantedItemId?.Trim();
        }

        /// <summary>
        /// 현재 요청된 상호작용 유형을 이 오브젝트가 처리할 수 있는지 반환합니다.
        /// </summary>
        public bool SupportsInteraction(InteractionType interactionType)
        {
            if (interactionType == InteractionType.Examine && _interactionType == InteractionType.Acquire)
            {
                return true;
            }

            return _interactionType == interactionType;
        }

        /// <summary>
        /// Inspector에 연결된 아웃라인 컴포넌트를 켜거나 꺼서 하이라이트를 토글합니다.
        /// </summary>
        public void SetHighlight(bool isHighlighted)
        {
            _isHighlighted = isHighlighted;

            if (_outlineBehaviours == null)
            {
                return;
            }

            for (int i = 0; i < _outlineBehaviours.Length; i++)
            {
                Behaviour outlineBehaviour = _outlineBehaviours[i];
                if (outlineBehaviour != null)
                {
                    outlineBehaviour.enabled = isHighlighted;
                }
            }
        }
    }
}
