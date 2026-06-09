using UnityEngine;

namespace Caretaker.Presentation
{
    [AddComponentMenu("Caretaker/Puzzle/Circuit Puzzle Link UI")]
    [DisallowMultipleComponent]
    public sealed class CircuitPuzzleLinkUI : MonoBehaviour
    {
        [Header("Nodes")]
        [SerializeField] private CircuitPuzzleNodeUI _fromNode;
        [SerializeField] private CircuitPuzzleNodeUI _toNode;

        [Header("Direction Rules")]
        [Tooltip("When enabled, required directions are inferred from the nearest circuit pieces in this link group.")]
        [SerializeField] private bool _useNodePositions = true;
        [SerializeField] private CircuitNodeDirection _fromRequiredDirection = CircuitNodeDirection.Right;
        [SerializeField] private CircuitNodeDirection _toRequiredDirection = CircuitNodeDirection.Left;

        [Header("Circuit Visuals")]
        [Tooltip("Use this GameObject as the connected circuit visual when Connected Circuit Root is empty.")]
        [SerializeField] private bool _useSelfAsConnectedCircuitRoot = true;
        [SerializeField] private GameObject _connectedCircuitRoot;
        [SerializeField] private GameObject _disconnectedCircuitRoot;

        private readonly CircuitPuzzleLink _link = new();

        public CircuitPuzzleLink Link
        {
            get
            {
                ApplySerializedState();
                return _link;
            }
        }

        public CircuitPuzzleNodeUI FromNode => _fromNode;

        public CircuitPuzzleNodeUI ToNode => _toNode;

        private void Awake()
        {
            ApplySerializedState();
        }

        private void OnValidate()
        {
            ApplySerializedState();
        }

        public void ApplySerializedState()
        {
            GameObject connectedRoot = _connectedCircuitRoot;
            if (connectedRoot == null && _useSelfAsConnectedCircuitRoot)
            {
                connectedRoot = gameObject;
            }

            bool useNodePositions = _useNodePositions;
            CircuitNodeDirection fromRequiredDirection = _fromRequiredDirection;
            CircuitNodeDirection toRequiredDirection = _toRequiredDirection;
            if (_useNodePositions &&
                TryGetGroupedRequiredDirections(
                    out CircuitNodeDirection groupedFromDirection,
                    out CircuitNodeDirection groupedToDirection))
            {
                useNodePositions = false;
                fromRequiredDirection = groupedFromDirection;
                toRequiredDirection = groupedToDirection;
            }

            _link.Configure(
                _fromNode,
                _toNode,
                useNodePositions,
                fromRequiredDirection,
                toRequiredDirection,
                connectedRoot,
                _disconnectedCircuitRoot);
        }

        private bool TryGetGroupedRequiredDirections(
            out CircuitNodeDirection fromDirection,
            out CircuitNodeDirection toDirection)
        {
            fromDirection = _fromRequiredDirection;
            toDirection = _toRequiredDirection;

            if (_fromNode == null || _toNode == null)
            {
                return false;
            }

            Transform groupRoot = transform.parent != null ? transform.parent : transform;
            CircuitPuzzleLinkUI[] groupedLinks = groupRoot.GetComponentsInChildren<CircuitPuzzleLinkUI>(true);
            if (groupedLinks.Length == 0)
            {
                return false;
            }

            Transform fromEndpoint = GetClosestGroupedLinkTransform(groupedLinks, _fromNode);
            Transform toEndpoint = GetClosestGroupedLinkTransform(groupedLinks, _toNode);
            if (fromEndpoint == null || toEndpoint == null)
            {
                return false;
            }

            fromDirection = GetDirectionFromNodeToTransform(
                _fromNode,
                fromEndpoint,
                _fromRequiredDirection);
            toDirection = GetDirectionFromNodeToTransform(
                _toNode,
                toEndpoint,
                _toRequiredDirection);
            return true;
        }

        private Transform GetClosestGroupedLinkTransform(
            CircuitPuzzleLinkUI[] groupedLinks,
            CircuitPuzzleNodeUI node)
        {
            Transform closestTransform = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < groupedLinks.Length; i++)
            {
                CircuitPuzzleLinkUI groupedLink = groupedLinks[i];
                if (groupedLink == null ||
                    groupedLink._fromNode != _fromNode ||
                    groupedLink._toNode != _toNode)
                {
                    continue;
                }

                float distance = (groupedLink.transform.position - node.transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTransform = groupedLink.transform;
                }
            }

            return closestTransform;
        }

        private static CircuitNodeDirection GetDirectionFromNodeToTransform(
            CircuitPuzzleNodeUI node,
            Transform target,
            CircuitNodeDirection fallback)
        {
            Vector3 worldOffset = target.position - node.transform.position;
            Vector2 planarOffset = new(worldOffset.x, worldOffset.y);
            return CircuitNodeDirectionUtility.FromVector(planarOffset, fallback);
        }
    }
}
