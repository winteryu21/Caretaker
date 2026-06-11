using System;

using UnityEngine;

namespace Caretaker.Presentation
{

    [Serializable]
    public sealed class CircuitPuzzleLink
    {
        [Header("Nodes")]
        [SerializeField] private CircuitPuzzleNodeUI _fromNode;
        [SerializeField] private CircuitPuzzleNodeUI _toNode;

        [Header("Direction Rules")]
        [Tooltip("When enabled, required directions are inferred from the UI positions of the two nodes.")]
        [SerializeField] private bool _useNodePositions = true;
        [SerializeField] private CircuitNodeDirection _fromRequiredDirection = CircuitNodeDirection.Right;
        [SerializeField] private CircuitNodeDirection _toRequiredDirection = CircuitNodeDirection.Left;

        [Header("Circuit Visuals")]
        [Tooltip("Future connected wire/trace asset root. Enabled only while the link is connected.")]
        [SerializeField] private GameObject _connectedCircuitRoot;
        [Tooltip("Optional broken/off wire asset root. Enabled only while the link is disconnected.")]
        [SerializeField] private GameObject _disconnectedCircuitRoot;

        private bool _isConnected;

        public CircuitPuzzleNodeUI FromNode => _fromNode;

        public CircuitPuzzleNodeUI ToNode => _toNode;

        public bool IsConnected => _isConnected;

        public bool HasValidNodes => _fromNode != null && _toNode != null && _fromNode != _toNode;

        public void Configure(
            CircuitPuzzleNodeUI fromNode,
            CircuitPuzzleNodeUI toNode,
            bool useNodePositions,
            CircuitNodeDirection fromRequiredDirection,
            CircuitNodeDirection toRequiredDirection,
            GameObject connectedCircuitRoot,
            GameObject disconnectedCircuitRoot)
        {
            _fromNode = fromNode;
            _toNode = toNode;
            _useNodePositions = useNodePositions;
            _fromRequiredDirection = fromRequiredDirection;
            _toRequiredDirection = toRequiredDirection;
            _connectedCircuitRoot = connectedCircuitRoot;
            _disconnectedCircuitRoot = disconnectedCircuitRoot;
        }

        public bool EvaluateAndApply()
        {
            bool isConnected = EvaluateConnection();
            SetConnected(isConnected);
            return isConnected;
        }

        public bool EvaluateConnection()
        {
            if (!HasValidNodes)
            {
                return false;
            }

            GetRequiredDirections(out CircuitNodeDirection fromDirection, out CircuitNodeDirection toDirection);
            return _fromNode.HasPort(fromDirection) && _toNode.HasPort(toDirection);
        }

        private void SetConnected(bool isConnected)
        {
            _isConnected = isConnected;

            if (_connectedCircuitRoot != null)
            {
                _connectedCircuitRoot.SetActive(isConnected);
            }

            if (_disconnectedCircuitRoot != null)
            {
                _disconnectedCircuitRoot.SetActive(!isConnected);
            }
        }

        private void GetRequiredDirections(
            out CircuitNodeDirection fromDirection,
            out CircuitNodeDirection toDirection)
        {
            if (!_useNodePositions)
            {
                fromDirection = _fromRequiredDirection;
                toDirection = _toRequiredDirection;
                return;
            }

            Vector3 worldOffset = _toNode.transform.position - _fromNode.transform.position;
            Vector2 toNodeOffset = new(worldOffset.x, worldOffset.y);
            fromDirection = CircuitNodeDirectionUtility.FromVector(toNodeOffset, _fromRequiredDirection);
            toDirection = CircuitNodeDirectionUtility.GetOpposite(fromDirection);
        }
    }
}
