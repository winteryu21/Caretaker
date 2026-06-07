using System;

using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Caretaker.Gameplay
{
    /// <summary>
    /// Enables local player control only on the owning Netcode client.
    /// </summary>
    /// <remarks>DSD §3.2, §3.3 - networked player ownership boundary.</remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(InteractionProbe))]
    public sealed class NetworkPlayerOwnerGate : NetworkBehaviour
    {
        [SerializeField] private bool _restrictObserversToOwner = true;
        [SerializeField] private bool _hideNonOwnerPresentation = true;

        private Collider2D[] _colliders;
        private InteractionProbe _interactionProbe;
        private PlayerController _playerController;
        private PlayerInput _playerInput;
        private PlayerInputReader _playerInputReader;
        private Renderer[] _renderers;
        private Rigidbody2D _rigidbody2D;
        private bool _hasConfiguredVisibilityOwner;
        private ulong _configuredVisibilityOwnerClientId;

        /// <summary>
        /// Raised when the local owner's network player is spawned on this client.
        /// </summary>
        public static event Action<GameObject> OnLocalOwnerPlayerSpawned;

        /// <summary>
        /// Raised when the local owner's network player is despawned on this client.
        /// </summary>
        public static event Action<GameObject> OnLocalOwnerPlayerDespawned;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _playerInputReader = GetComponent<PlayerInputReader>();
            _interactionProbe = GetComponent<InteractionProbe>();
            _playerInput = GetComponent<PlayerInput>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _rigidbody2D = GetComponent<Rigidbody2D>();

            NetworkObject.CheckObjectVisibility = ShouldShowToClient;
        }

        public override void OnNetworkSpawn()
        {
            ApplyOwnershipControl(IsOwner);

            if (IsOwner)
            {
                OnLocalOwnerPlayerSpawned?.Invoke(gameObject);
            }
        }

        public override void OnGainedOwnership()
        {
            ApplyOwnershipControl(true);
        }

        public override void OnLostOwnership()
        {
            ApplyOwnershipControl(false);
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                OnLocalOwnerPlayerDespawned?.Invoke(gameObject);
            }

            ApplyOwnershipControl(false);
        }

        /// <summary>
        /// Configures which client can observe this network player before it is spawned.
        /// </summary>
        /// <param name="ownerClientId">Client that owns and observes the player.</param>
        public void ConfigureOwnerOnlyVisibility(ulong ownerClientId)
        {
            _configuredVisibilityOwnerClientId = ownerClientId;
            _hasConfiguredVisibilityOwner = true;
            NetworkObject.CheckObjectVisibility = ShouldShowToClient;
        }

        private void ApplyOwnershipControl(bool isLocalOwner)
        {
            SetPresentationEnabled(isLocalOwner || !_hideNonOwnerPresentation);

            if (isLocalOwner)
            {
                SetEnabled(_playerInput, true);
                SetEnabled(_playerInputReader, true);
                SetEnabled(_interactionProbe, true);
                SetEnabled(_playerController, true);
                return;
            }

            SetEnabled(_playerController, false);
            SetEnabled(_interactionProbe, false);
            SetEnabled(_playerInputReader, false);
            SetEnabled(_playerInput, false);
        }

        private bool ShouldShowToClient(ulong clientId)
        {
            if (!_restrictObserversToOwner)
            {
                return true;
            }

            ulong visibleOwnerClientId = _hasConfiguredVisibilityOwner
                ? _configuredVisibilityOwnerClientId
                : OwnerClientId;
            return clientId == visibleOwnerClientId;
        }

        private void SetPresentationEnabled(bool isEnabled)
        {
            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    SetEnabled(_renderers[i], isEnabled);
                }
            }

            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    SetEnabled(_colliders[i], isEnabled);
                }
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.simulated = isEnabled;
            }
        }

        private static void SetEnabled(Behaviour behaviour, bool isEnabled)
        {
            if (behaviour != null)
            {
                behaviour.enabled = isEnabled;
            }
        }

        private static void SetEnabled(Renderer renderer, bool isEnabled)
        {
            if (renderer != null)
            {
                renderer.enabled = isEnabled;
            }
        }
    }
}
