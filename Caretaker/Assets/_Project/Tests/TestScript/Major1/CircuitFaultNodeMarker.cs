using TMPro;
using UnityEngine;

namespace Caretaker.Presentation
{
    [AddComponentMenu("Caretaker/Test/Major1/Circuit Fault Node Marker")]
    [DisallowMultipleComponent]
    public sealed class CircuitFaultNodeMarker : MonoBehaviour
    {
        [SerializeField] private string _nodeId;
        [SerializeField] private bool _isFaultNode;
        [SerializeField] private GameObject _faultMarkerRoot;
        [SerializeField] private TMP_Text _labelText;

        /// <summary>Applies the configured node label and fault marker visibility.</summary>
        public void Refresh()
        {
            string resolvedNodeId = ResolveNodeId();

            if (_labelText != null)
            {
                _labelText.text = resolvedNodeId;
            }

            if (_faultMarkerRoot != null)
            {
                _faultMarkerRoot.SetActive(_isFaultNode);
            }
        }

        private void Awake()
        {
            Refresh();
        }

        private void OnValidate()
        {
            Refresh();
        }

        private string ResolveNodeId()
        {
            if (!string.IsNullOrWhiteSpace(_nodeId))
            {
                return _nodeId.Trim();
            }

            return name.Trim();
        }
    }
}
