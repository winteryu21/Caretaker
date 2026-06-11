using UnityEngine;
using UnityEngine.Events;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Toggles a platform's renderers and colliders from a CausalReceiver UnityEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CausalPlatformStateAction : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private GameObject _targetRoot;
        [SerializeField] private bool _startRemoved;

        [Header("Events")]
        [SerializeField] private UnityEvent _onRemoved;
        [SerializeField] private UnityEvent _onRestored;

        private Collider2D[] _colliders;
        private Renderer[] _renderers;
        private bool _isRemoved;

        /// <summary>
        /// Returns whether the controlled platform is currently removed.
        /// </summary>
        public bool IsRemoved => _isRemoved;

        private void Awake()
        {
            CacheTargetComponents();
            SetRemoved(_startRemoved, false);
        }

        private void OnValidate()
        {
            if (_targetRoot == null)
            {
                _targetRoot = gameObject;
            }
        }

        /// <summary>
        /// Removes the controlled platform from the future timeline.
        /// </summary>
        public void ApplyRemoved()
        {
            SetRemoved(true, true);
        }

        /// <summary>
        /// Restores the controlled platform for checkpoint reset or editor iteration.
        /// </summary>
        public void Restore()
        {
            SetRemoved(false, true);
        }

        private void CacheTargetComponents()
        {
            if (_targetRoot == null)
            {
                _targetRoot = gameObject;
            }

            _colliders = _targetRoot.GetComponentsInChildren<Collider2D>(true);
            _renderers = _targetRoot.GetComponentsInChildren<Renderer>(true);
        }

        private void SetRemoved(bool isRemoved, bool notify)
        {
            _isRemoved = isRemoved;
            bool isEnabled = !isRemoved;

            if (_colliders == null || _renderers == null)
            {
                CacheTargetComponents();
            }

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                {
                    _colliders[i].enabled = isEnabled;
                }
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = isEnabled;
                }
            }

            if (!notify)
            {
                return;
            }

            if (isRemoved)
            {
                _onRemoved?.Invoke();
            }
            else
            {
                _onRestored?.Invoke();
            }
        }
    }
}
