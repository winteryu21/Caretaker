using System;
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
        private const string GLOW_OBJECT_PREFIX = "InteractionGlow";

        /// <summary>
        /// 로컬 플레이어가 오브젝트 조사를 완료했을 때 발생합니다.
        /// </summary>
        public static event Action<InteractableObject, PlayerController> OnExamineRequested;

        [Header("Object Identity")]
        [SerializeField] private string _objectId;
        [SerializeField] private InteractionType _interactionTypes = InteractionType.Examine;

        [Header("Interaction")]
        [SerializeField] private string _requiredItemId;
        [SerializeField] private string _grantedItemId;
        [SerializeField] [TextArea] private string _examineText;
        [SerializeField] private Sprite _examineImage;

        [Header("Highlight")]
        [SerializeField] private Behaviour[] _outlineBehaviours;
        [SerializeField] private bool _useFallbackGlow = true;
        [SerializeField] private Color _fallbackGlowColor = new(0.35f, 0.95f, 1f, 0.45f);
        [SerializeField] [Range(1f, 1.5f)] private float _fallbackGlowScale = 1.08f;
        [SerializeField] private int _fallbackGlowSortingOrderOffset = 1;
        [SerializeField] private bool _highlightOnAwake;

        private CausalTrigger _causalTrigger;
        private Collider2D _cachedCollider2D;
        private IOperateAction[] _operateActions;
        private GlowRendererEntry[] _glowRendererEntries = Array.Empty<GlowRendererEntry>();
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
        /// 조사 팝업에 선택적으로 표시할 이미지입니다.
        /// </summary>
        public Sprite ExamineImage => _examineImage;

        /// <summary>
        /// 현재 하이라이트가 켜져 있는지 반환합니다.
        /// </summary>
        public bool IsHighlighted => _isHighlighted;

        private void Awake()
        {
            _cachedCollider2D = GetComponent<Collider2D>();
            _causalTrigger = GetComponent<CausalTrigger>();
            RefreshOperateActions();
            CacheFallbackGlowRenderers();
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

        internal void EnsureInteractionType(InteractionType interactionType)
        {
            if (interactionType == InteractionType.None)
            {
                return;
            }

            _interactionTypes |= interactionType;
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
        /// Inspector에 연결한 아웃라인 컴포넌트와 fallback glow 렌더러를 켜고 꺼서 하이라이트를 적용합니다.
        /// </summary>
        public void SetHighlight(bool isHighlighted)
        {
            if (_isHighlighted == isHighlighted)
            {
                return;
            }

            _isHighlighted = isHighlighted;
            Debug.Log($"Highlight {(isHighlighted ? "enabled" : "disabled")}: object={_objectId}", this);

            if (isHighlighted && _useFallbackGlow && _glowRendererEntries.Length == 0)
            {
                CacheFallbackGlowRenderers();
            }

            SetFallbackGlow(isHighlighted);

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
            OnExamineRequested?.Invoke(this, actor);
        }

        private bool RunAcquire(PlayerController actor)
        {
            Debug.Log($"Acquire interaction: object={_objectId}, grantedItem={_grantedItemId}", this);
            return true;
        }

        private bool RunOperate(PlayerController actor)
        {
            RefreshOperateActions();

            bool executed = false;

            if (_causalTrigger != null)
            {
                _causalTrigger.Fire();
                executed = true;
            }

            for (int i = 0; i < _operateActions.Length; i++)
            {
                IOperateAction operateAction = _operateActions[i];
                if (operateAction != null)
                {
                    executed |= operateAction.Execute(actor);
                }
            }

            if (!executed)
            {
                Debug.LogWarning(
                    $"Operate interaction did not execute. object={_objectId}, actions={_operateActions.Length}, hasCausalTrigger={_causalTrigger != null}",
                    this);
            }

            return executed;
        }

        private void RefreshOperateActions()
        {
            _operateActions = GetComponents<IOperateAction>();
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

        private void CacheFallbackGlowRenderers()
        {
            if (!_useFallbackGlow)
            {
                _glowRendererEntries = Array.Empty<GlowRendererEntry>();
                return;
            }

            SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (spriteRenderers.Length == 0)
            {
                _glowRendererEntries = Array.Empty<GlowRendererEntry>();
                return;
            }

            GlowRendererEntry[] entries = new GlowRendererEntry[spriteRenderers.Length];
            int entryCount = 0;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer sourceRenderer = spriteRenderers[i];
                if (sourceRenderer == null ||
                    sourceRenderer.gameObject.name.StartsWith(GLOW_OBJECT_PREFIX, StringComparison.Ordinal))
                {
                    continue;
                }

                SpriteRenderer glowRenderer = CreateFallbackGlowRenderer(sourceRenderer);
                if (glowRenderer != null)
                {
                    entries[entryCount] = new GlowRendererEntry(sourceRenderer, glowRenderer);
                    entryCount++;
                }
            }

            Array.Resize(ref entries, entryCount);
            _glowRendererEntries = entries;
        }

        private SpriteRenderer CreateFallbackGlowRenderer(SpriteRenderer sourceRenderer)
        {
            if (sourceRenderer.sprite == null)
            {
                return null;
            }

            GameObject glowObject = new($"{GLOW_OBJECT_PREFIX}_{sourceRenderer.gameObject.name}");
            Transform glowTransform = glowObject.transform;
            glowTransform.SetParent(sourceRenderer.transform, false);
            glowTransform.localPosition = Vector3.zero;
            glowTransform.localRotation = Quaternion.identity;
            glowTransform.localScale = Vector3.one * Mathf.Max(1f, _fallbackGlowScale);

            SpriteRenderer glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            SyncFallbackGlowRenderer(sourceRenderer, glowRenderer);
            glowRenderer.enabled = false;
            glowObject.SetActive(false);
            return glowRenderer;
        }

        private void SetFallbackGlow(bool isHighlighted)
        {
            for (int i = 0; i < _glowRendererEntries.Length; i++)
            {
                GlowRendererEntry entry = _glowRendererEntries[i];
                SpriteRenderer sourceRenderer = entry.SourceRenderer;
                SpriteRenderer glowRenderer = entry.GlowRenderer;
                if (sourceRenderer == null || glowRenderer == null)
                {
                    continue;
                }

                bool shouldShow = isHighlighted && sourceRenderer.enabled && sourceRenderer.sprite != null;
                if (shouldShow)
                {
                    SyncFallbackGlowRenderer(sourceRenderer, glowRenderer);
                }

                glowRenderer.gameObject.SetActive(shouldShow);
                glowRenderer.enabled = shouldShow;
            }
        }

        private void SyncFallbackGlowRenderer(SpriteRenderer sourceRenderer, SpriteRenderer glowRenderer)
        {
            glowRenderer.sprite = sourceRenderer.sprite;
            glowRenderer.drawMode = sourceRenderer.drawMode;
            glowRenderer.size = sourceRenderer.size;
            glowRenderer.tileMode = sourceRenderer.tileMode;
            glowRenderer.flipX = sourceRenderer.flipX;
            glowRenderer.flipY = sourceRenderer.flipY;
            glowRenderer.maskInteraction = sourceRenderer.maskInteraction;
            glowRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
            glowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            glowRenderer.sortingOrder = sourceRenderer.sortingOrder + _fallbackGlowSortingOrderOffset;
            glowRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            glowRenderer.color = _fallbackGlowColor;
        }

        private readonly struct GlowRendererEntry
        {
            public GlowRendererEntry(SpriteRenderer sourceRenderer, SpriteRenderer glowRenderer)
            {
                SourceRenderer = sourceRenderer;
                GlowRenderer = glowRenderer;
            }

            public SpriteRenderer SourceRenderer { get; }

            public SpriteRenderer GlowRenderer { get; }
        }
    }
}
