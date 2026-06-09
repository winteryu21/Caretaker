using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Caretaker.Presentation
{

    [AddComponentMenu("Caretaker/Puzzle/Circuit Puzzle UI")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CircuitPuzzleUI : PuzzleUIBase
    {
        private const string MAIN_NODE_NAME = "Mainnode";
        private const string MAIN_NODE_VISUAL_NAME = "Square";
        private static readonly string[] M1_FAULT_NODE_NAMES =
        {
            "A-1",
            "B-1",
            "C-2",
            "C-1"
        };

        [Header("Circuit Puzzle")]
        [SerializeField] private CircuitPuzzleNodeUI[] _nodes;
        [SerializeField] private CircuitPuzzleLink[] _links;
        [SerializeField] private CircuitPuzzleLinkUI[] _linkComponents;
        [SerializeField] private bool _autoCollectChildNodes = true;
        [SerializeField] private bool _autoCollectChildLinks = true;
        [SerializeField] private bool _completeAutomatically = true;
        [SerializeField] private bool _closeOnSolved = true;
        [SerializeField] private bool _logSolutionChecks = true;
        [SerializeField] private bool _fadePanelBackgroundForSpritePuzzle;
        [SerializeField] [Range(0f, 1f)] private float _testPanelBackgroundAlpha = 0.05f;
        [SerializeField] private bool _useCameraCanvasForSpritePuzzle = true;

        private Graphic _panelBackgroundGraphic;
        private Color _panelBackgroundColor;
        private bool _hasPanelBackgroundColor;
        private Canvas _ownerCanvas;
        private RenderMode _originalCanvasRenderMode;
        private Camera _originalCanvasCamera;
        private float _originalCanvasPlaneDistance;
        private bool _hasOriginalCanvasState;

        public event Action<CircuitPuzzleUI> OnCircuitStateChanged;

        public int ConnectedLinkCount { get; private set; }

        public int TotalLinkCount { get; private set; }

        private void OnEnable()
        {
            CollectConfiguredParts();
            SubscribeToNodes();
        }

        private void Start()
        {
            CollectConfiguredParts();
            RefreshPuzzle();
        }

        private void OnDisable()
        {
            UnsubscribeFromNodes();
        }

        private void OnValidate()
        {
            CollectConfiguredParts();
            NormalizeLinks();
        }

        public void ConfigurePuzzle(
            IEnumerable<CircuitPuzzleNodeUI> nodes,
            IEnumerable<CircuitPuzzleLink> links)
        {
            UnsubscribeFromNodes();

            _nodes = nodes != null
                ? new List<CircuitPuzzleNodeUI>(nodes).ToArray()
                : Array.Empty<CircuitPuzzleNodeUI>();

            _links = links != null
                ? new List<CircuitPuzzleLink>(links).ToArray()
                : Array.Empty<CircuitPuzzleLink>();

            _linkComponents = Array.Empty<CircuitPuzzleLinkUI>();
            NormalizeLinks();
            SubscribeToNodes();
            EvaluateConnections();
        }

        public void RefreshPuzzle()
        {
            CollectConfiguredParts();
            SubscribeToNodes();
            EvaluateConnections();

            if (_completeAutomatically)
            {
                TryCompletePuzzle();
            }
        }
        protected override bool IsCorrectSolution()
        {
            if (_links == null || _links.Length == 0)
            {
                LogSolutionCheck("fail: no circuit links collected.");
                return false;
            }

            if (HasM1SubstationNodes())
            {
                return IsM1SubstationSolution();
            }

            for (int i = 0; i < _links.Length; i++)
            {
                CircuitPuzzleLink link = _links[i];
                if (link == null || !link.EvaluateConnection())
                {
                    LogSolutionCheck($"fail: link is disconnected. index={i}, linkId={link?.LinkId}");
                    return false;
                }
            }

            LogSolutionCheck("success: all fallback links are connected.");
            return true;
        }

        private bool HasM1SubstationNodes()
        {
            Dictionary<string, CircuitPuzzleNodeUI> nodesByName = BuildNodeMap();
            for (int i = 0; i < M1_FAULT_NODE_NAMES.Length; i++)
            {
                if (!nodesByName.ContainsKey(M1_FAULT_NODE_NAMES[i]))
                {
                    LogSolutionCheck($"fail: M1 node was not found. node={M1_FAULT_NODE_NAMES[i]}");
                    return false;
                }
            }

            bool hasA2 = nodesByName.ContainsKey("A-2");
            if (!hasA2)
            {
                LogSolutionCheck("fail: M1 node was not found. node=A-2");
            }

            return hasA2;
        }

        private bool IsM1SubstationSolution()
        {
            Dictionary<string, CircuitPuzzleNodeUI> nodesByName = BuildNodeMap();
            Dictionary<string, HashSet<string>> connectedGraph = BuildConnectedGraph();

            if (!AreNodesConnected(connectedGraph, M1_FAULT_NODE_NAMES))
            {
                LogSolutionCheck($"fail: fault node group is not connected. group={string.Join(", ", M1_FAULT_NODE_NAMES)}");
                return false;
            }

            string[] stableNodeNames = GetStableNodeNames(nodesByName);
            if (stableNodeNames.Length > 1 && !AreNodesConnected(connectedGraph, stableNodeNames))
            {
                LogSolutionCheck($"fail: stable node group is not connected. group={string.Join(", ", stableNodeNames)}");
                return false;
            }

            if (HasConnectedLinkBetween(M1_FAULT_NODE_NAMES, stableNodeNames))
            {
                LogSolutionCheck("fail: fault node group is still connected to stable node group.");
                return false;
            }

            if (HasDirectConnectedLink("A-1", "A-2"))
            {
                LogSolutionCheck("fail: A-1 and A-2 are directly connected.");
                return false;
            }

            bool mainNodeHasLinks = HasAnyLinkForNode(MAIN_NODE_NAME);
            if (mainNodeHasLinks &&
                (!HasDirectConnectedLink(MAIN_NODE_NAME, "A-1") ||
                !HasDirectConnectedLink(MAIN_NODE_NAME, "A-2")))
            {
                LogSolutionCheck("fail: Mainnode must be directly connected to both A-1 and A-2.");
                return false;
            }

            LogSolutionCheck("success: M1 substation solution matched.");
            return true;
        }

        private Dictionary<string, CircuitPuzzleNodeUI> BuildNodeMap()
        {
            Dictionary<string, CircuitPuzzleNodeUI> nodesByName = new(StringComparer.Ordinal);
            if (_nodes == null)
            {
                return nodesByName;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                CircuitPuzzleNodeUI node = _nodes[i];
                if (node == null)
                {
                    continue;
                }

                string nodeName = GetNodeName(node);
                if (!string.IsNullOrWhiteSpace(nodeName) && !nodesByName.ContainsKey(nodeName))
                {
                    nodesByName.Add(nodeName, node);
                }
            }

            return nodesByName;
        }

        private Dictionary<string, HashSet<string>> BuildConnectedGraph()
        {
            Dictionary<string, HashSet<string>> connectedGraph = new(StringComparer.Ordinal);
            if (_links == null)
            {
                return connectedGraph;
            }

            for (int i = 0; i < _links.Length; i++)
            {
                CircuitPuzzleLink link = _links[i];
                if (link == null || !link.HasValidNodes || !link.EvaluateConnection())
                {
                    continue;
                }

                string fromName = GetNodeName(link.FromNode);
                string toName = GetNodeName(link.ToNode);
                AddGraphConnection(connectedGraph, fromName, toName);
            }

            return connectedGraph;
        }

        private string[] GetStableNodeNames(Dictionary<string, CircuitPuzzleNodeUI> nodesByName)
        {
            List<string> stableNodeNames = new();
            foreach (KeyValuePair<string, CircuitPuzzleNodeUI> nodeEntry in nodesByName)
            {
                string nodeName = nodeEntry.Key;
                if (nodeName == MAIN_NODE_NAME || IsM1FaultNode(nodeName))
                {
                    continue;
                }

                CircuitPuzzleNodeUI node = nodeEntry.Value;
                if (node != null && node.IsDummyNode)
                {
                    continue;
                }

                stableNodeNames.Add(nodeName);
            }

            return stableNodeNames.ToArray();
        }

        private static bool AreNodesConnected(
            Dictionary<string, HashSet<string>> connectedGraph,
            string[] nodeNames)
        {
            if (nodeNames == null || nodeNames.Length == 0)
            {
                return false;
            }

            HashSet<string> visited = new(StringComparer.Ordinal);
            Queue<string> queue = new();
            visited.Add(nodeNames[0]);
            queue.Enqueue(nodeNames[0]);

            while (queue.Count > 0)
            {
                string currentNodeName = queue.Dequeue();
                if (!connectedGraph.TryGetValue(currentNodeName, out HashSet<string> neighbors))
                {
                    continue;
                }

                foreach (string neighbor in neighbors)
                {
                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            for (int i = 0; i < nodeNames.Length; i++)
            {
                if (!visited.Contains(nodeNames[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasAnyLinkForNode(string nodeName)
        {
            if (_links == null)
            {
                return false;
            }

            for (int i = 0; i < _links.Length; i++)
            {
                CircuitPuzzleLink link = _links[i];
                if (link == null || !link.HasValidNodes)
                {
                    continue;
                }

                if (GetNodeName(link.FromNode) == nodeName || GetNodeName(link.ToNode) == nodeName)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasConnectedLinkBetween(string[] firstNodeNames, string[] secondNodeNames)
        {
            if (_links == null)
            {
                return false;
            }

            HashSet<string> firstNodeSet = new(firstNodeNames, StringComparer.Ordinal);
            HashSet<string> secondNodeSet = new(secondNodeNames, StringComparer.Ordinal);
            for (int i = 0; i < _links.Length; i++)
            {
                CircuitPuzzleLink link = _links[i];
                if (link == null || !link.HasValidNodes || !link.EvaluateConnection())
                {
                    continue;
                }

                string fromName = GetNodeName(link.FromNode);
                string toName = GetNodeName(link.ToNode);
                if ((firstNodeSet.Contains(fromName) && secondNodeSet.Contains(toName)) ||
                    (firstNodeSet.Contains(toName) && secondNodeSet.Contains(fromName)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasDirectConnectedLink(string firstNodeName, string secondNodeName)
        {
            if (_links == null)
            {
                return false;
            }

            for (int i = 0; i < _links.Length; i++)
            {
                CircuitPuzzleLink link = _links[i];
                if (link == null || !link.HasValidNodes || !link.EvaluateConnection())
                {
                    continue;
                }

                string fromName = GetNodeName(link.FromNode);
                string toName = GetNodeName(link.ToNode);
                if ((fromName == firstNodeName && toName == secondNodeName) ||
                    (fromName == secondNodeName && toName == firstNodeName))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddGraphConnection(
            Dictionary<string, HashSet<string>> connectedGraph,
            string fromName,
            string toName)
        {
            if (string.IsNullOrWhiteSpace(fromName) ||
                string.IsNullOrWhiteSpace(toName) ||
                fromName == toName)
            {
                return;
            }

            AddGraphEdge(connectedGraph, fromName, toName);
            AddGraphEdge(connectedGraph, toName, fromName);
        }

        private static void AddGraphEdge(
            Dictionary<string, HashSet<string>> connectedGraph,
            string fromName,
            string toName)
        {
            if (!connectedGraph.TryGetValue(fromName, out HashSet<string> neighbors))
            {
                neighbors = new HashSet<string>(StringComparer.Ordinal);
                connectedGraph.Add(fromName, neighbors);
            }

            neighbors.Add(toName);
        }

        private static bool IsM1FaultNode(string nodeName)
        {
            for (int i = 0; i < M1_FAULT_NODE_NAMES.Length; i++)
            {
                if (nodeName == M1_FAULT_NODE_NAMES[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetNodeName(CircuitPuzzleNodeUI node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            Transform nodeTransform = node.transform;
            if (node.name == MAIN_NODE_VISUAL_NAME &&
                nodeTransform.parent != null &&
                nodeTransform.parent.name == MAIN_NODE_NAME)
            {
                return MAIN_NODE_NAME;
            }

            return node.name.Trim();
        }

        private void HandleNodeDirectionChanged(CircuitPuzzleNodeUI node)
        {
            RefreshPuzzle();
        }

        protected override void HandleOpened()
        {
            ApplySpritePuzzleCanvasMode();
            ApplyTestPanelBackgroundVisibility();
            RefreshPuzzle();
        }

        protected override void HandleClosed()
        {
            RestorePanelBackgroundVisibility();
            RestoreSpritePuzzleCanvasMode();
        }

        protected override void HandleSolved()
        {
            LogSolutionCheck("solved: closing puzzle UI.");

            if (_closeOnSolved)
            {
                Close();
            }
        }

        private void LogSolutionCheck(string message)
        {
            if (!_logSolutionChecks)
            {
                return;
            }

            Debug.Log($"CircuitPuzzleUI '{name}' solution check {message}", this);
        }

        private void ApplyTestPanelBackgroundVisibility()
        {
            if (!_fadePanelBackgroundForSpritePuzzle)
            {
                return;
            }

            if (_panelBackgroundGraphic == null)
            {
                _panelBackgroundGraphic = GetComponent<Graphic>();
            }

            if (_panelBackgroundGraphic == null)
            {
                return;
            }

            if (!_hasPanelBackgroundColor)
            {
                _panelBackgroundColor = _panelBackgroundGraphic.color;
                _hasPanelBackgroundColor = true;
            }

            Color fadedColor = _panelBackgroundColor;
            fadedColor.a = _testPanelBackgroundAlpha;
            _panelBackgroundGraphic.color = fadedColor;
        }

        private void RestorePanelBackgroundVisibility()
        {
            if (_panelBackgroundGraphic != null && _hasPanelBackgroundColor)
            {
                _panelBackgroundGraphic.color = _panelBackgroundColor;
            }
        }

        private void ApplySpritePuzzleCanvasMode()
        {
            if (!_useCameraCanvasForSpritePuzzle)
            {
                return;
            }

            if (_ownerCanvas == null)
            {
                _ownerCanvas = GetComponentInParent<Canvas>();
            }

            if (_ownerCanvas == null || _ownerCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning(
                    $"Puzzle '{name}' needs a MainCamera to render SpriteRenderer puzzle pieces over the UI panel.",
                    this);
                return;
            }

            if (!_hasOriginalCanvasState)
            {
                _originalCanvasRenderMode = _ownerCanvas.renderMode;
                _originalCanvasCamera = _ownerCanvas.worldCamera;
                _originalCanvasPlaneDistance = _ownerCanvas.planeDistance;
                _hasOriginalCanvasState = true;
            }

            _ownerCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _ownerCanvas.worldCamera = mainCamera;
            _ownerCanvas.planeDistance = Mathf.Max(1f, mainCamera.nearClipPlane + 1f);
        }

        private void RestoreSpritePuzzleCanvasMode()
        {
            if (_ownerCanvas == null || !_hasOriginalCanvasState)
            {
                return;
            }

            _ownerCanvas.renderMode = _originalCanvasRenderMode;
            _ownerCanvas.worldCamera = _originalCanvasCamera;
            _ownerCanvas.planeDistance = _originalCanvasPlaneDistance;
        }

        private void CollectConfiguredParts()
        {
            if (_autoCollectChildNodes)
            {
                CircuitPuzzleNodeUI[] childNodes = GetComponentsInChildren<CircuitPuzzleNodeUI>(true);
                if (childNodes.Length > 0)
                {
                    _nodes = childNodes;
                }
            }

            if (!_autoCollectChildLinks)
            {
                return;
            }

            CircuitPuzzleLinkUI[] childLinkComponents = GetComponentsInChildren<CircuitPuzzleLinkUI>(true);
            if (childLinkComponents.Length == 0)
            {
                return;
            }

            _linkComponents = childLinkComponents;
            _links = BuildLinksFromComponents(childLinkComponents);
        }

        private static CircuitPuzzleLink[] BuildLinksFromComponents(CircuitPuzzleLinkUI[] linkComponents)
        {
            if (linkComponents == null || linkComponents.Length == 0)
            {
                return Array.Empty<CircuitPuzzleLink>();
            }

            List<CircuitPuzzleLink> links = new(linkComponents.Length);
            for (int i = 0; i < linkComponents.Length; i++)
            {
                CircuitPuzzleLinkUI linkComponent = linkComponents[i];
                if (linkComponent != null)
                {
                    links.Add(linkComponent.Link);
                }
            }

            return links.ToArray();
        }

        private void EvaluateConnections()
        {
            int connectedLinkCount = 0;
            int totalLinkCount = 0;

            if (_links != null)
            {
                for (int i = 0; i < _links.Length; i++)
                {
                    CircuitPuzzleLink link = _links[i];
                    if (link == null)
                    {
                        continue;
                    }

                    totalLinkCount++;
                    if (link.EvaluateAndApply())
                    {
                        connectedLinkCount++;
                    }
                }
            }

            ConnectedLinkCount = connectedLinkCount;
            TotalLinkCount = totalLinkCount;
            OnCircuitStateChanged?.Invoke(this);
        }

        private void SubscribeToNodes()
        {
            if (_nodes == null)
            {
                return;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                CircuitPuzzleNodeUI node = _nodes[i];
                if (node != null)
                {
                    node.OnDirectionChanged -= HandleNodeDirectionChanged;
                    node.OnDirectionChanged += HandleNodeDirectionChanged;
                }
            }
        }

        private void UnsubscribeFromNodes()
        {
            if (_nodes == null)
            {
                return;
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                CircuitPuzzleNodeUI node = _nodes[i];
                if (node != null)
                {
                    node.OnDirectionChanged -= HandleNodeDirectionChanged;
                }
            }
        }

        private void NormalizeLinks()
        {
            if (_links == null)
            {
                return;
            }

            for (int i = 0; i < _links.Length; i++)
            {
                _links[i]?.Normalize();
            }
        }
    }
}
