using UnityEngine;

using Caretaker.Shared;

namespace Caretaker.World
{
    /// <summary>
    /// 모든 상호작용 가능 오브젝트의 베이스 클래스.
    /// 조사 텍스트, 필요 아이템, 조작 가능 여부를 제공한다.
    /// Team B가 Inspector에서 유형/ID를 설정할 수 있는 형태.
    /// </summary>
    /// <remarks>
    /// DSD §3.3 — 플레이어 제어 및 상호작용 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class InteractableObject : MonoBehaviour
    {
        [Header("Object Identity")]
        [SerializeField] private string _objectId;
        [SerializeField] private InteractionType _interactionType;

        [Header("Interaction")]
        [SerializeField] private string _requiredItemId;
        [SerializeField] private string _examineText;

        /// <summary>오브젝트 고유 식별자. (예: OBJ_P1_SIGN)</summary>
        public string ObjectId => _objectId;

        /// <summary>이 오브젝트의 상호작용 유형.</summary>
        public InteractionType InteractionType => _interactionType;

        /// <summary>상호작용에 필요한 아이템 ID. 빈 문자열이면 아이템 불필요.</summary>
        public string RequiredItemId => _requiredItemId;

        /// <summary>조사 시 표시할 텍스트.</summary>
        public string ExamineText => _examineText;
    }
}
