using UnityEngine;
using UnityEngine.InputSystem;

using Caretaker.Shared;
using Caretaker.World;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// 마우스 hover 대상과 근접 상호작용 대상을 지속적으로 탐지하고 하이라이트를 갱신합니다.
    /// </summary>
    /// <remarks>
    /// DSD §3.4 - 상호작용 시스템
    /// 계층: Unity Component
    /// </remarks>
    public class InteractionProbe : MonoBehaviour
    {
        private const float DEFAULT_INTERACTION_RADIUS = 1f;
        private const int MAX_NEARBY_RESULTS = 16;
        private const int MAX_RAYCAST_RESULTS = 16;

        [Header("Detection")]
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private LayerMask _interactableLayers = Physics2D.DefaultRaycastLayers;
        [SerializeField] private float _interactionRadius = DEFAULT_INTERACTION_RADIUS;

        private readonly Collider2D[] _nearbyResults = new Collider2D[MAX_NEARBY_RESULTS];
        private readonly RaycastHit2D[] _raycastResults = new RaycastHit2D[MAX_RAYCAST_RESULTS];

        private InteractableObject _highlightedHoverTarget;
        private InteractableObject _highlightedProximityTarget;
        private InteractableObject _hoverTarget;
        private InteractableObject _proximityTarget;
        private InteractionType _proximityInteractionType = InteractionType.None;

        /// <summary>
        /// 현재 마우스로 가리키는 조사 대상입니다.
        /// </summary>
        public InteractableObject HoverTarget => _hoverTarget;

        /// <summary>
        /// 현재 플레이어 반경 안에서 가장 우선순위가 높은 상호작용 대상입니다.
        /// </summary>
        public InteractableObject ProximityTarget => _proximityTarget;

        /// <summary>
        /// 현재 근접 대상에 대해 적용될 상호작용 타입입니다.
        /// </summary>
        public InteractionType ProximityInteractionType => _proximityInteractionType;

        /// <summary>
        /// 현재 허용되는 상호작용 반경입니다.
        /// </summary>
        public float InteractionRadius => _interactionRadius;

        private void Awake()
        {
            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }
        }

        private void Update()
        {
            RefreshHoverTarget();
            RefreshProximityTarget();
            RefreshHighlights();
        }

        private void OnDisable()
        {
            ClearHighlight(ref _highlightedHoverTarget);
            ClearHighlight(ref _highlightedProximityTarget);
            _hoverTarget = null;
            _proximityTarget = null;
            _proximityInteractionType = InteractionType.None;
        }

        private void RefreshHoverTarget()
        {
            _hoverTarget = null;

            if (_worldCamera == null || Mouse.current == null)
            {
                return;
            }

            Ray ray = _worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            int hitCount = Physics2D.GetRayIntersectionNonAlloc(
                ray,
                _raycastResults,
                float.PositiveInfinity,
                _interactableLayers);

            float closestRayDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = _raycastResults[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                if (!hitCollider.TryGetComponent(out InteractableObject interactableObject))
                {
                    continue;
                }

                if (!interactableObject.IsInteractable(InteractionType.Examine))
                {
                    continue;
                }

                if (_raycastResults[i].distance >= closestRayDistance)
                {
                    continue;
                }

                closestRayDistance = _raycastResults[i].distance;
                _hoverTarget = interactableObject;
            }
        }

        private void RefreshProximityTarget()
        {
            _proximityTarget = null;
            _proximityInteractionType = InteractionType.None;

            int hitCount = Physics2D.OverlapCircleNonAlloc(
                transform.position,
                _interactionRadius,
                _nearbyResults,
                _interactableLayers);

            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D nearbyCollider = _nearbyResults[i];
                if (nearbyCollider == null)
                {
                    continue;
                }

                if (!nearbyCollider.TryGetComponent(out InteractableObject interactableObject))
                {
                    continue;
                }

                float distance = interactableObject.GetDistanceFrom(transform.position);
                if (!TryGetProximityInteractionType(interactableObject, distance, out InteractionType interactionType))
                {
                    continue;
                }

                bool isBetterPriority =
                    _proximityTarget == null ||
                    GetInteractionPriority(interactionType) < GetInteractionPriority(_proximityInteractionType);
                bool isSamePriorityCloser =
                    interactionType == _proximityInteractionType && distance < closestDistance;

                if (!isBetterPriority && !isSamePriorityCloser)
                {
                    continue;
                }

                _proximityTarget = interactableObject;
                _proximityInteractionType = interactionType;
                closestDistance = distance;
            }
        }

        private void RefreshHighlights()
        {
            InteractableObject previousHoverTarget = _highlightedHoverTarget;
            InteractableObject previousProximityTarget = _highlightedProximityTarget;

            _highlightedHoverTarget = _hoverTarget;
            _highlightedProximityTarget = _proximityTarget;

            SyncHighlight(previousHoverTarget);
            SyncHighlight(previousProximityTarget);
            SyncHighlight(_highlightedHoverTarget);
            SyncHighlight(_highlightedProximityTarget);
        }

        private int GetInteractionPriority(InteractionType interactionType)
        {
            return interactionType switch
            {
                InteractionType.Acquire => 0,
                InteractionType.UseItem => 1,
                InteractionType.Operate => 2,
                InteractionType.Examine => 3,
                _ => int.MaxValue
            };
        }

        private bool TryGetProximityInteractionType(
            InteractableObject interactableObject,
            float distance,
            out InteractionType interactionType)
        {
            if (interactableObject == null || distance < 0f || distance > _interactionRadius)
            {
                interactionType = InteractionType.None;
                return false;
            }

            if (interactableObject.IsInteractable(InteractionType.Acquire) &&
                !interactableObject.IsItemAcquired &&
                (!interactableObject.HasRequiredItem || interactableObject.IsRequiredItemSatisfied))
            {
                interactionType = InteractionType.Acquire;
                return true;
            }

            if (interactableObject.HasRequiredItem && !interactableObject.IsRequiredItemSatisfied)
            {
                interactionType = InteractionType.UseItem;
                return true;
            }

            if (interactableObject.IsInteractable(InteractionType.Operate))
            {
                interactionType = InteractionType.Operate;
                return true;
            }

            if (interactableObject.IsInteractable(InteractionType.Examine))
            {
                interactionType = InteractionType.Examine;
                return true;
            }

            interactionType = InteractionType.None;
            return false;
        }

        private void SyncHighlight(InteractableObject target)
        {
            if (target == null)
            {
                return;
            }

            bool shouldHighlight =
                target == _highlightedHoverTarget ||
                target == _highlightedProximityTarget;
            target.SetHighlight(shouldHighlight);
        }

        private void ClearHighlight(ref InteractableObject currentTarget)
        {
            if (currentTarget != null)
            {
                currentTarget.SetHighlight(false);
                currentTarget = null;
            }
        }
    }
}
