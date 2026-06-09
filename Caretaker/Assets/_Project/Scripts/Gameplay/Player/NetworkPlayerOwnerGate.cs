using System;

using Caretaker.Shared;
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

        private readonly NetworkVariable<bool> _phase3RemoteVisible = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<TimelineRole> _timelineRole = new(
            TimelineRole.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

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

        /// <summary>Raised when any observed network player is spawned on this client.</summary>
        public static event Action<NetworkPlayerOwnerGate> OnObservedPlayerSpawned;

        /// <summary>Raised when any observed network player is despawned on this client.</summary>
        public static event Action<NetworkPlayerOwnerGate> OnObservedPlayerDespawned;

        /// <summary>Timeline represented by this network player.</summary>
        public TimelineRole TimelineRole => _timelineRole.Value;

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
            OnObservedPlayerSpawned?.Invoke(this);

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
            OnObservedPlayerDespawned?.Invoke(this);

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
        public void ConfigureVisibility(
            ulong ownerClientId,
            TimelineRole timelineRole,
            bool allowRemoteObservers)
        {
            _configuredVisibilityOwnerClientId = ownerClientId;
            _hasConfiguredVisibilityOwner = true;
            _timelineRole.Value = timelineRole;
            _phase3RemoteVisible.Value = allowRemoteObservers;
            NetworkObject.CheckObjectVisibility = ShouldShowToClient;
        }

        private void ApplyOwnershipControl(bool isLocalOwner)
        {
            bool presentationEnabled = ShouldEnablePresentation(
                isLocalOwner,
                _phase3RemoteVisible.Value,
                _hideNonOwnerPresentation);
            bool localControlEnabled = ShouldEnableLocalControl(isLocalOwner);

            SetRendererVisibility(presentationEnabled);
            SetPhysicsEnabled(localControlEnabled);

            if (localControlEnabled)
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
            ulong visibleOwnerClientId = _hasConfiguredVisibilityOwner
                ? _configuredVisibilityOwnerClientId
                : OwnerClientId;
            return ShouldObservePlayer(
                clientId,
                visibleOwnerClientId,
                _restrictObserversToOwner,
                _phase3RemoteVisible.Value);
        }

        /// <summary>Returns whether this client should receive the player NetworkObject.</summary>
        public static bool ShouldObservePlayer(
            ulong clientId,
            ulong ownerClientId,
            bool restrictObserversToOwner,
            bool phase3RemoteVisible)
        {
            return !restrictObserversToOwner
                || phase3RemoteVisible
                || clientId == ownerClientId;
        }

        /// <summary>Returns whether this client should render the player.</summary>
        public static bool ShouldEnablePresentation(
            bool isLocalOwner,
            bool phase3RemoteVisible,
            bool hideNonOwnerPresentation)
        {
            return isLocalOwner
                || phase3RemoteVisible
                || !hideNonOwnerPresentation;
        }

        /// <summary>Returns whether local input and physics should control the player.</summary>
        public static bool ShouldEnableLocalControl(bool isLocalOwner)
        {
            return isLocalOwner;
        }

        private void SetRendererVisibility(bool isVisible)
        {
            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    SetEnabled(_renderers[i], isVisible);
                }
            }
        }

        private void SetPhysicsEnabled(bool isEnabled)
        {
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
