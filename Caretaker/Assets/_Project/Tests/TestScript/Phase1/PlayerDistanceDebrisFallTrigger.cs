using UnityEngine;
using UnityEngine.Events;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Drops future debris when a player reaches a configured horizontal grid-cell distance.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerDistanceDebrisFallTrigger : MonoBehaviour
    {
        [Header("Debris")]
        [SerializeField] private GameObject _debrisRoot;
        [SerializeField] private Transform _distanceOrigin;
        [SerializeField] [Min(0f)] private float _fallGravityScale = 1f;
        [SerializeField] [Min(0f)] private float _initialDownwardSpeed = 2f;
        [SerializeField] private bool _armAsKinematic = true;

        [Header("Trigger Distance")]
        [SerializeField] [Min(0.01f)] private float _gridUnit = 1f;
        [SerializeField] [Min(0f)] private float _minTriggerCells = 2f;
        [SerializeField] [Min(0f)] private float _maxTriggerCells = 3f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onFallStarted;
        [SerializeField] private UnityEvent _onReset;

        private Collider2D _triggerCollider;
        private Rigidbody2D[] _debrisRigidbodies;
        private DebrisBodyState[] _initialBodyStates;
        private bool _hasFallen;

        /// <summary>
        /// Returns whether the debris has already started falling.
        /// </summary>
        public bool HasFallen => _hasFallen;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            ConfigureTriggerCollider();
            CacheDebris();

            if (_armAsKinematic)
            {
                ArmDebris();
            }
        }

        private void OnValidate()
        {
            if (_debrisRoot == null)
            {
                _debrisRoot = gameObject;
            }

            if (_distanceOrigin == null && _debrisRoot != null)
            {
                _distanceOrigin = _debrisRoot.transform;
            }

            if (_maxTriggerCells < _minTriggerCells)
            {
                _maxTriggerCells = _minTriggerCells;
            }

            if (_gridUnit <= 0f)
            {
                _gridUnit = 1f;
            }

            ConfigureTriggerCollider();
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_hasFallen || !HasPlayerController(other))
            {
                return;
            }

            if (IsWithinFallWindow(other.transform.position.x))
            {
                StartFall();
            }
        }

        /// <summary>
        /// Starts the debris fall immediately.
        /// </summary>
        public void StartFall()
        {
            if (_hasFallen)
            {
                return;
            }

            if (_debrisRigidbodies == null || _debrisRigidbodies.Length == 0)
            {
                CacheDebris();
            }

            for (int i = 0; i < _debrisRigidbodies.Length; i++)
            {
                Rigidbody2D debrisRigidbody = _debrisRigidbodies[i];
                if (debrisRigidbody == null)
                {
                    continue;
                }

                debrisRigidbody.bodyType = RigidbodyType2D.Dynamic;
                debrisRigidbody.gravityScale = Mathf.Abs(_fallGravityScale);
                debrisRigidbody.linearVelocity = new Vector2(0f, -_initialDownwardSpeed);
                debrisRigidbody.angularVelocity = 0f;
                debrisRigidbody.WakeUp();
            }

            _hasFallen = true;
            _onFallStarted?.Invoke();
        }

        /// <summary>
        /// Restores debris bodies to their initial transform and physics state.
        /// </summary>
        public void ResetDebris()
        {
            if (_debrisRigidbodies == null || _initialBodyStates == null)
            {
                CacheDebris();
            }

            for (int i = 0; i < _debrisRigidbodies.Length; i++)
            {
                Rigidbody2D debrisRigidbody = _debrisRigidbodies[i];
                if (debrisRigidbody == null)
                {
                    continue;
                }

                DebrisBodyState initialState = _initialBodyStates[i];
                debrisRigidbody.linearVelocity = Vector2.zero;
                debrisRigidbody.angularVelocity = 0f;
                debrisRigidbody.transform.SetPositionAndRotation(initialState.Position, initialState.Rotation);
                debrisRigidbody.bodyType = initialState.BodyType;
                debrisRigidbody.gravityScale = initialState.GravityScale;
                debrisRigidbody.simulated = initialState.Simulated;
            }

            _hasFallen = false;

            if (_armAsKinematic)
            {
                ArmDebris();
            }

            _onReset?.Invoke();
        }

        private bool IsWithinFallWindow(float playerX)
        {
            float originX = _distanceOrigin != null ? _distanceOrigin.position.x : transform.position.x;
            float distanceInCells = Mathf.Abs(playerX - originX) / _gridUnit;
            return distanceInCells >= _minTriggerCells && distanceInCells <= _maxTriggerCells;
        }

        private void ConfigureTriggerCollider()
        {
            if (_triggerCollider == null)
            {
                _triggerCollider = GetComponent<Collider2D>();
            }

            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
        }

        private void CacheDebris()
        {
            if (_debrisRoot == null)
            {
                _debrisRoot = gameObject;
            }

            if (_distanceOrigin == null && _debrisRoot != null)
            {
                _distanceOrigin = _debrisRoot.transform;
            }

            _debrisRigidbodies = _debrisRoot.GetComponentsInChildren<Rigidbody2D>(true);
            _initialBodyStates = new DebrisBodyState[_debrisRigidbodies.Length];

            for (int i = 0; i < _debrisRigidbodies.Length; i++)
            {
                Rigidbody2D debrisRigidbody = _debrisRigidbodies[i];
                _initialBodyStates[i] = new DebrisBodyState(
                    debrisRigidbody.transform.position,
                    debrisRigidbody.transform.rotation,
                    debrisRigidbody.bodyType,
                    debrisRigidbody.gravityScale,
                    debrisRigidbody.simulated);
            }

            if (_debrisRigidbodies.Length == 0)
            {
                Debug.LogWarning("Debris fall trigger has no Rigidbody2D targets.", this);
            }
        }

        private void ArmDebris()
        {
            for (int i = 0; i < _debrisRigidbodies.Length; i++)
            {
                Rigidbody2D debrisRigidbody = _debrisRigidbodies[i];
                if (debrisRigidbody == null)
                {
                    continue;
                }

                debrisRigidbody.linearVelocity = Vector2.zero;
                debrisRigidbody.angularVelocity = 0f;
                debrisRigidbody.bodyType = RigidbodyType2D.Kinematic;
                debrisRigidbody.gravityScale = 0f;
            }
        }

        private static bool HasPlayerController(Collider2D collider2D)
        {
            return collider2D != null && collider2D.GetComponentInParent<PlayerController>() != null;
        }

        private readonly struct DebrisBodyState
        {
            public DebrisBodyState(
                Vector3 position,
                Quaternion rotation,
                RigidbodyType2D bodyType,
                float gravityScale,
                bool simulated)
            {
                Position = position;
                Rotation = rotation;
                BodyType = bodyType;
                GravityScale = gravityScale;
                Simulated = simulated;
            }

            public Vector3 Position { get; }

            public Quaternion Rotation { get; }

            public RigidbodyType2D BodyType { get; }

            public float GravityScale { get; }

            public bool Simulated { get; }
        }
    }
}
