using UnityEngine;

using Caretaker.Gameplay;
using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 모든 상호작용 오브젝트의 공통 설정과 하이라이트 기능을 제공하는 베이스 컴포넌트입니다.
    /// Inspector에서 ID와 설명, 지원하는 상호작용 타입을 설정합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public class InteractableObject : MonoBehaviour
    {
        [Header("Object Identity")]
        [SerializeField] private string _objectId;
        [SerializeField] private InteractionType _interactionTypes = InteractionType.Examine;

        [Header("Interaction")]
        [SerializeField] private string _requiredItemId;
        [SerializeField] private string _grantedItemId;
        [SerializeField] [TextArea] private string _examineText;

        [Header("Highlight")]
        [SerializeField] private Behaviour[] _outlineBehaviours;
        [SerializeField] private bool _highlightOnAwake;

        private CausalTrigger _causalTrigger;
        private Collider2D _cachedCollider2D;
        private bool _isHighlighted;
        private bool _isItemAcquired;
        private bool _isRequiredItemSatisfied;

        /// <summary>
        /// game-design 문서상의 오브젝트 식별자입니다. 예: OBJ_P1_SIGN
        /// </summary>
        public string ObjectId => _objectId;

        /// <summary>
        /// 이 오브젝트가 지원하는 상호작용 타입 집합입니다.
        /// </summary>
        public InteractionType InteractionTypes => _interactionTypes;

        /// <summary>
        /// 상호작용에 필요한 아이템 ID입니다. 비어 있으면 아이템이 필요하지 않습니다.
        /// </summary>
        public string RequiredItemId => _requiredItemId;

        /// <summary>
        /// 아이템 요구 조건이 있는 오브젝트인지 반환합니다.
        /// </summary>
        public bool HasRequiredItem => !string.IsNullOrWhiteSpace(_requiredItemId);

        /// <summary>
        /// 필요한 아이템 조건이 런타임에서 충족되었는지 반환합니다.
        /// </summary>
        public bool IsRequiredItemSatisfied => _isRequiredItemSatisfied;

        /// <summary>
        /// 습득 성공 시 인벤토리에 추가할 아이템 ID입니다.
        /// </summary>
        public string GrantedItemId => _grantedItemId;

        /// <summary>
        /// 이 오브젝트에서 아이템을 이미 획득했는지 반환합니다.
        /// </summary>
        public bool IsItemAcquired => _isItemAcquired;

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
            _cachedCollider2D = GetComponent<Collider2D>();
            _causalTrigger = GetComponent<CausalTrigger>();
            ConfigureInteractionCollider();
            SetHighlight(_highlightOnAwake);
        }

        private void OnValidate()
        {
            _objectId = _objectId?.Trim();
            _requiredItemId = _requiredItemId?.Trim();
            _grantedItemId = _grantedItemId?.Trim();
            ConfigureInteractionCollider();
        }

        /// <summary>
        /// 요청된 상호작용 타입을 이 오브젝트가 처리할 수 있는지 반환합니다.
        /// </summary>
        public bool IsInteractable(InteractionType interactionType)
        {
            if (interactionType == InteractionType.None)
            {
                return false;
            }

            return (_interactionTypes & interactionType) == interactionType;
        }

        /// <summary>
        /// 지정한 월드 위치에서 이 오브젝트까지의 최근접 거리를 반환합니다.
        /// </summary>
        public float GetDistanceFrom(Vector2 worldPosition)
        {
            if (_cachedCollider2D == null)
            {
                return Vector2.Distance(worldPosition, transform.position);
            }

            Vector2 closestPoint = _cachedCollider2D.ClosestPoint(worldPosition);
            return Vector2.Distance(worldPosition, closestPoint);
        }

        /// <summary>
        /// Inspector에 연결한 아웃라인 컴포넌트를 켜고 꺼서 하이라이트를 적용합니다.
        /// </summary>
        public void SetHighlight(bool isHighlighted)
        {
            if (_isHighlighted == isHighlighted)
            {
                return;
            }

            _isHighlighted = isHighlighted;
            Debug.Log($"Highlight {(isHighlighted ? "enabled" : "disabled")}: object={_objectId}", this);

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

        /// <summary>
        /// 필요한 아이템 조건을 충족된 상태로 표시합니다.
        /// </summary>
        public void MarkRequiredItemSatisfied()
        {
            if (!HasRequiredItem)
            {
                return;
            }

            _isRequiredItemSatisfied = true;
        }

        /// <summary>
        /// 이 오브젝트의 아이템이 획득된 상태로 표시합니다.
        /// </summary>
        public void MarkItemAcquired()
        {
            _isItemAcquired = true;
        }

        /// <summary>
        /// 요청된 상호작용 타입에 맞는 후속 조치를 실행합니다.
        /// </summary>
        /// <param name="interactionType">실행할 상호작용 타입입니다.</param>
        /// <param name="actor">상호작용을 실행한 플레이어입니다.</param>
        /// <returns>상호작용 후속 조치가 실행되었는지 여부입니다.</returns>
        public bool RunInteraction(InteractionType interactionType, PlayerController actor)
        {
            if (!IsInteractable(interactionType))
            {
                return false;
            }

            switch (interactionType)
            {
                case InteractionType.Examine:
                    RunExamine(actor);
                    return true;

                case InteractionType.Acquire:
                    return RunAcquire(actor);

                case InteractionType.Operate:
                    return RunOperate(actor);

                default:
                    return false;
            }
        }

        private void RunExamine(PlayerController actor)
        {
            Debug.Log($"Examine interaction: object={_objectId}, text={_examineText}", this);
        }

        private bool RunAcquire(PlayerController actor)
        {
            Debug.Log($"Acquire interaction: object={_objectId}, grantedItem={_grantedItemId}", this);
            return true;
        }

        private bool RunOperate(PlayerController actor)
        {
            // 인과 트리거가 부착되어 있으면 인과 파이프라인으로 전달
            if (_causalTrigger != null)
            {
                _causalTrigger.Fire();
                return true;
            }

            // 인과 트리거가 없는 일반 조작 (문 열기, 레버 등)
            Debug.Log($"Operate interaction (non-causal): object={_objectId}", this);
            return true;
        }

        private void ConfigureInteractionCollider()
        {
            if (_cachedCollider2D == null)
            {
                _cachedCollider2D = GetComponent<Collider2D>();
            }

            if (_cachedCollider2D != null)
            {
                _cachedCollider2D.isTrigger = true;
            }
        }
    }
}
