using UnityEngine;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 아이템 ID, 표시명, 사용 가능 태그, 소모 여부를 정의하는 데이터 에셋.
    /// </summary>
    /// <remarks>DSD §4.2 — ScriptableObject 스키마</remarks>
    [CreateAssetMenu(fileName = "SO_ItemDefinition", menuName = "Caretaker/Inventory/ItemDefinition")]
    public class ItemDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _category;
        [SerializeField] private string[] _usableTargetTags;
        [SerializeField] private bool _consumeOnUse;

        /// <summary>아이템 고유 식별자. (예: ITEM_KEY_LAB_A)</summary>
        public string ItemId => _itemId;

        /// <summary>UI에 표시할 아이템 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>아이템 분류.</summary>
        public string Category => _category;

        /// <summary>사용 가능한 대상 태그 목록.</summary>
        public string[] UsableTargetTags => _usableTargetTags;

        /// <summary>사용 시 소모 여부.</summary>
        public bool ConsumeOnUse => _consumeOnUse;
    }
}
