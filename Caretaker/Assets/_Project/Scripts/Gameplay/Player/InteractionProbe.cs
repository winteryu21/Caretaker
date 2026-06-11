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
        private const float DEFAULT_TAKEDOWN_RADIUS = 1.5f;
        private const int MAX_NEARBY_RESULTS = 16;
        private const int MAX_RAYCAST_RESULTS = 16;

        [Header("Detection")]
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private LayerMask _interactableLayers = Physics2D.DefaultRaycastLayers;
        [SerializeField] private float _interactionRadius = DEFAULT_INTERACTION_RADIUS;
        [SerializeField] private float _takedownRadius = DEFAULT_TAKEDOWN_RADIUS;

        private readonly Collider2D[] _nearbyResults = new Collider2D[MAX_NEARBY_RESULTS];
        private readonly RaycastHit2D[] _raycastResults = new RaycastHit2D[MAX_RAYCAST_RESULTS];
        private readonly InteractableObject[] _highlightedTargets = new InteractableObject[MAX_NEARBY_RESULTS];
        private readonly InteractableObject[] _nearbyTargets = new InteractableObject[MAX_NEARBY_RESULTS];

        private ContactFilter2D _interactableContactFilter;
        private EnemyController _currentTakedownTarget;
        private InteractableObject _hoverTarget;
        private PlayerInputReader _inputReader;
        private InteractableObject _proximityTarget;
        private int _highlightedTargetCount;
        private int _nearbyTargetCount;
        private InteractionType _hoverInteractionType = InteractionType.None;
        private InteractionType _proximityInteractionType = InteractionType.None;

        /// <summary>
        /// 현재 마우스로 가리키는 근접 대상입니다.
        /// 플레이어 상호작용 반경 밖의 오브젝트는 hover로 인정하지 않습니다.
        /// </summary>
        public InteractableObject HoverTarget => _hoverTarget;

        /// <summary>
        /// 현재 hover 대상에 대해 적용될 마우스 상호작용 타입입니다.
        /// </summary>
        public InteractionType HoverInteractionType => _hoverInteractionType;

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

        /// <summary>
        /// 현재 전투 처형 조건을 만족하는 가장 가까운 적입니다.
        /// </summary>
        public EnemyController CurrentTakedownTarget => _currentTakedownTarget;

        private void Awake()
        {
            RefreshInteractableContactFilter();
            _inputReader = GetComponent<PlayerInputReader>();

            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }
        }

        private void OnValidate()
        {
            RefreshInteractableContactFilter();
        }

        private void Update()
        {
            RefreshNearbyTargets();
            RefreshHoverTarget();
            RefreshHighlights();
        }

        private void OnDisable()
        {
            ClearHighlights();
            _hoverTarget = null;
            _proximityTarget = null;
            _currentTakedownTarget = null;
            _hoverInteractionType = InteractionType.None;
            _proximityInteractionType = InteractionType.None;
        }

        private void RefreshNearbyTargets()
        {
            _nearbyTargetCount = 0;
            _proximityTarget = null;
            _proximityInteractionType = InteractionType.None;
            _currentTakedownTarget = null;

            int hitCount = Physics2D.OverlapCircle(
                transform.position,
                Mathf.Max(_interactionRadius, _takedownRadius),
                _interactableContactFilter,
                _nearbyResults);

            float closestDistance = float.PositiveInfinity;
            float closestTakedownDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D nearbyCollider = _nearbyResults[i];
                if (nearbyCollider == null)
                {
                    continue;
                }

                if (_inputReader != null &&
                    _inputReader.ControlMode == PlayerControlMode.Combat &&
                    nearbyCollider.GetComponentInParent<EnemyController>() is EnemyController enemyController &&
                    enemyController.CanBeTakenDownBy(transform.position))
                {
                    Vector2 closestPoint = nearbyCollider.ClosestPoint(transform.position);
                    float takedownDistance = Vector2.Distance(transform.position, closestPoint);
                    if (takedownDistance < closestTakedownDistance)
                    {
                        _currentTakedownTarget = enemyController;
                        closestTakedownDistance = takedownDistance;
                    }
                }

                if (!nearbyCollider.TryGetComponent(out InteractableObject interactableObject))
                {
                    continue;
                }

                float distance = interactableObject.GetDistanceFrom(transform.position);
                if (distance < 0f || distance > _interactionRadius)
                {
                    continue;
                }

                if (!ContainsNearbyTarget(interactableObject) && _nearbyTargetCount < _nearbyTargets.Length)
                {
                    _nearbyTargets[_nearbyTargetCount] = interactableObject;
                    _nearbyTargetCount++;
                }

                if (TryGetProximityInteractionType(interactableObject, distance, out InteractionType interactionType))
                {
                    bool isBetterPriority =
                        _proximityTarget == null ||
                        GetInteractionPriority(interactionType) < GetInteractionPriority(_proximityInteractionType);
                    bool isSamePriorityCloser =
                        interactionType == _proximityInteractionType && distance < closestDistance;

                    if (isBetterPriority || isSamePriorityCloser)
                    {
                        _proximityTarget = interactableObject;
                        _proximityInteractionType = interactionType;
                        closestDistance = distance;
                    }
                }
            }
        }

        private void RefreshHoverTarget()
        {
            _hoverTarget = null;
            _hoverInteractionType = InteractionType.None;

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

                if (!ContainsNearbyTarget(interactableObject))
                {
                    continue;
                }

                if (!TryGetHoverInteractionType(interactableObject, out InteractionType interactionType))
                {
                    continue;
                }

                if (_raycastResults[i].distance >= closestRayDistance)
                {
                    continue;
                }

                closestRayDistance = _raycastResults[i].distance;
                _hoverTarget = interactableObject;
                _hoverInteractionType = interactionType;
            }
        }

        private void RefreshHighlights()
        {
            int previousHighlightedTargetCount = _highlightedTargetCount;
            for (int i = 0; i < previousHighlightedTargetCount; i++)
            {
                InteractableObject target = _highlightedTargets[i];
                if (target != null && !ContainsNearbyTarget(target))
                {
                    target.SetHighlight(false);
                }

                _highlightedTargets[i] = null;
            }

            _highlightedTargetCount = 0;

            for (int i = 0; i < _nearbyTargetCount; i++)
            {
                InteractableObject target = _nearbyTargets[i];
                if (target == null)
                {
                    continue;
                }

                target.SetHighlight(true);
                if (_highlightedTargetCount < _highlightedTargets.Length)
                {
                    _highlightedTargets[_highlightedTargetCount] = target;
                    _highlightedTargetCount++;
                }
            }
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

            if (interactableObject.IsInteractable(InteractionType.Operate) &&
                (!interactableObject.HasRequiredItem || interactableObject.IsRequiredItemSatisfied))
            {
                interactionType = InteractionType.Operate;
                return true;
            }

            interactionType = InteractionType.None;
            return false;
        }

        private bool TryGetHoverInteractionType(
            InteractableObject interactableObject,
            out InteractionType interactionType)
        {
            if (interactableObject == null)
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

            if (interactableObject.IsInteractable(InteractionType.Examine))
            {
                interactionType = InteractionType.Examine;
                return true;
            }

            interactionType = InteractionType.None;
            return false;
        }

        private bool ContainsNearbyTarget(InteractableObject target)
        {
            for (int i = 0; i < _nearbyTargetCount; i++)
            {
                if (_nearbyTargets[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearHighlights()
        {
            for (int i = 0; i < _highlightedTargetCount; i++)
            {
                if (_highlightedTargets[i] != null)
                {
                    _highlightedTargets[i].SetHighlight(false);
                    _highlightedTargets[i] = null;
                }
            }

            _highlightedTargetCount = 0;
        }

        private void RefreshInteractableContactFilter()
        {
            _interactableContactFilter.useTriggers = Physics2D.queriesHitTriggers;
            _interactableContactFilter.SetLayerMask(_interactableLayers);
        }
    }
}
