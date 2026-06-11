using System.Collections;

using UnityEngine;
using UnityEngine.Events;

using Caretaker.World;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Breaks a past timeline platform when the player lands on it, then fires its causal trigger.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(CausalTrigger))]
    public sealed class PastBreakableCausalTile : MonoBehaviour
    {
        private const int CONTACT_BUFFER_SIZE = 8;

        [Header("Break Timing")]
        [SerializeField] [Min(0f)] private float _breakDelay = 0.15f;
        [SerializeField] [Min(0f)] private float _shakeDuration = 0.2f;
        [SerializeField] [Min(0f)] private float _shakeDistance = 0.04f;

        [Header("Collision")]
        [SerializeField] [Range(0f, 1f)] private float _landingNormalThreshold = 0.5f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onBroken;
        [SerializeField] private UnityEvent _onRestored;

        private readonly ContactPoint2D[] _contactBuffer = new ContactPoint2D[CONTACT_BUFFER_SIZE];

        private CausalTrigger _causalTrigger;
        private Collider2D[] _colliders;
        private Renderer[] _renderers;
        private Vector3 _startLocalPosition;
        private Coroutine _breakRoutine;
        private bool _isBroken;

        /// <summary>
        /// Returns whether this tile has already broken during the current run.
        /// </summary>
        public bool IsBroken => _isBroken;

        private void Awake()
        {
            _causalTrigger = GetComponent<CausalTrigger>();
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            _startLocalPosition = transform.localPosition;
        }

        private void OnValidate()
        {
            if (_breakDelay < 0f)
            {
                _breakDelay = 0f;
            }

            if (_shakeDuration < 0f)
            {
                _shakeDuration = 0f;
            }

            if (_shakeDistance < 0f)
            {
                _shakeDistance = 0f;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isBroken || !HasPlayerController(collision.collider) || !HasLandingContact(collision))
            {
                return;
            }

            BeginBreak();
        }

        /// <summary>
        /// Breaks the tile immediately and submits its connected CausalTrigger, if present.
        /// </summary>
        public void BreakNow()
        {
            if (_isBroken)
            {
                return;
            }

            _isBroken = true;
            SetTileVisibleAndSolid(false);

            if (_causalTrigger != null)
            {
                _causalTrigger.Fire();
            }

            _onBroken?.Invoke();
        }

        /// <summary>
        /// Restores the tile for checkpoint reset or editor iteration.
        /// </summary>
        public void Restore()
        {
            if (_breakRoutine != null)
            {
                StopCoroutine(_breakRoutine);
                _breakRoutine = null;
            }

            _isBroken = false;
            transform.localPosition = _startLocalPosition;
            SetTileVisibleAndSolid(true);
            _onRestored?.Invoke();
        }

        private void BeginBreak()
        {
            if (_breakRoutine != null)
            {
                return;
            }

            _breakRoutine = StartCoroutine(BreakRoutine());
        }

        private IEnumerator BreakRoutine()
        {
            if (_shakeDuration > 0f && _shakeDistance > 0f)
            {
                yield return ShakeRoutine();
            }

            if (_breakDelay > 0f)
            {
                yield return new WaitForSeconds(_breakDelay);
            }

            _breakRoutine = null;
            BreakNow();
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;

            while (elapsed < _shakeDuration)
            {
                float offsetX = Random.Range(-_shakeDistance, _shakeDistance);
                transform.localPosition = _startLocalPosition + new Vector3(offsetX, 0f, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = _startLocalPosition;
        }

        private bool HasLandingContact(Collision2D collision)
        {
            int contactCount = collision.GetContacts(_contactBuffer);
            for (int i = 0; i < contactCount; i++)
            {
                if (_contactBuffer[i].normal.y <= -_landingNormalThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPlayerController(Collider2D collider2D)
        {
            return collider2D != null && collider2D.GetComponentInParent<PlayerController>() != null;
        }

        private void SetTileVisibleAndSolid(bool isEnabled)
        {
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
        }
    }
}
